using System.Net.Http.Headers;
using System.Text.Json;
using finance_tracker_backend.Middleware;
using finance_tracker_backend.Repositories;
using finance_tracker_backend.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers().AddJsonOptions(options =>
{
    options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
});
builder.Services.AddOpenApi();
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<ApiExceptionHandler>();

// CORS: policy name and origins from config; in dev with no origins, allow any.
var corsPolicyName = builder.Configuration["Cors:PolicyName"] ?? "DefaultCors";
builder.Services.AddCors(options =>
{
    options.AddPolicy(corsPolicyName, policy =>
    {
        var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>();
        if (allowedOrigins is { Length: > 0 })
            policy.WithOrigins(allowedOrigins).AllowAnyMethod().AllowAnyHeader();
        else if (builder.Environment.IsDevelopment())
            policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader();
    });
});

// User JWTs: validate via Supabase Auth OIDC metadata → JWKS (JWT signing keys). No legacy JwtSecret in config.
// See https://supabase.com/docs/guides/auth/jwts — use asymmetric signing keys in Dashboard; legacy-only HS256 may need migration.
// appsettings: PublishableKey (client) + SecretKey (server Supabase.Client). OAuth callback = {url}/auth/v1/callback.
// Auth email (confirm signup, reset password, magic link): Supabase Dashboard (Authentication → Emails) + Resend.
// Product welcome after first profile row: Resend template from this API — see Resend:WelcomeTemplateId, Resend:From, IEmailService, email_logs.
var jwtAudience = builder.Configuration["Supabase:Authentication:JwtAudience"] ?? "authenticated";
var url = builder.Configuration["Supabase:Connection:Url"]?.TrimEnd('/');
var secretKey = builder.Configuration["Supabase:Authentication:SecretKey"];
if (string.IsNullOrWhiteSpace(url))
    throw new InvalidOperationException("Supabase:Connection:Url is required for JWT validation and Supabase client.");
if (string.IsNullOrWhiteSpace(secretKey))
    throw new InvalidOperationException(
        "Supabase:Authentication:SecretKey is required for server-side Supabase client (publishable/secret or legacy service_role). Use User Secrets.");

var authIssuer = $"{url}/auth/v1";
builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.MapInboundClaims = false;
        options.Authority = authIssuer;
        options.Audience = jwtAudience;
        options.RequireHttpsMetadata = true;
        options.MetadataAddress = $"{authIssuer}/.well-known/openid-configuration";
        options.TokenValidationParameters = new TokenValidationParameters
        {
            NameClaimType = "sub",
            ValidateAudience = true,
            ValidateIssuer = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidAudience = jwtAudience,
            ValidIssuer = authIssuer,
            ClockSkew = TimeSpan.FromMinutes(1)
        };
    });
builder.Services.AddAuthorization();

// Service-role Supabase client for server-side DB (bypasses RLS). Initialized once after Build.
builder.Services.AddSingleton(_ => new Supabase.Client(
    url,
    secretKey,
    new Supabase.SupabaseOptions
    {
        AutoRefreshToken = false,
        AutoConnectRealtime = false
    }));

// Repositories
builder.Services.AddScoped<IProfileRepository, ProfileRepository>();
builder.Services.AddScoped<IProfileSubscriptionRepository, ProfileSubscriptionRepository>();
builder.Services.AddScoped<IEmailLogRepository, EmailLogRepository>();

// Services
builder.Services.AddHttpClient<IResendTemplateEmailSender, ResendTemplateEmailSender>((sp, client) =>
{
    var configuration = sp.GetRequiredService<IConfiguration>();

    var baseUrl = configuration["Resend:BaseUrl"] ?? "https://api.resend.com/";
    client.BaseAddress = new Uri(baseUrl);

    var key = configuration["Resend:ApiKey"];
    if (string.IsNullOrWhiteSpace(key))
        throw new InvalidOperationException("Resend API key is not configured.");

    client.DefaultRequestHeaders.Authorization =
        new AuthenticationHeaderValue("Bearer", key);

    client.DefaultRequestHeaders.Accept.Add(
        new MediaTypeWithQualityHeaderValue("application/json"));

    client.Timeout = TimeSpan.FromSeconds(10);
});
builder.Services.AddScoped<IProfileService, ProfileService>();
builder.Services.AddScoped<IProfileSubscriptionService, ProfileSubscriptionService>();
builder.Services.AddScoped<IEmailService, EmailService>();
builder.Services.AddScoped<IEnsureUserService, EnsureUserService>();

var app = builder.Build();

app.UseExceptionHandler();

await app.Services.GetRequiredService<Supabase.Client>().InitializeAsync();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();
app.UseCors(corsPolicyName);
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
