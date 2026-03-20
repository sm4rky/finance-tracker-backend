using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddOpenApi();

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

// Supabase JWT (HS256): Supabase:Connection:Url + Supabase:Authentication:JwtSecret (User Secrets / env).
// OAuth redirect = {Url}/auth/v1/callback (handled by Supabase, not this API).
var jwtAudience = builder.Configuration["Supabase:Authentication:JwtAudience"] ?? "authenticated";
var supabaseUrl = builder.Configuration["Supabase:Connection:Url"]?.TrimEnd('/');
var jwtSecret = builder.Configuration["Supabase:Authentication:JwtSecret"];
if (string.IsNullOrWhiteSpace(supabaseUrl))
    throw new InvalidOperationException("Supabase:Connection:Url is required for JWT validation.");
if (string.IsNullOrWhiteSpace(jwtSecret))
    throw new InvalidOperationException(
        "Supabase:Authentication:JwtSecret is required (Dashboard → Settings → API → JWT Secret).");

var issuer = $"{supabaseUrl}/auth/v1";
var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret));
builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = signingKey,
            ValidateIssuer = true,
            ValidIssuer = issuer,
            ValidateAudience = true,
            ValidAudience = jwtAudience,
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromMinutes(1)
        };
    });
builder.Services.AddAuthorization();

var app = builder.Build();

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
