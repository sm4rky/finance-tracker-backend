using System.Net.Http.Headers;
using System.Text.Json;
using finance_tracker_backend.Infrastructure;
using finance_tracker_backend.Jobs;
using finance_tracker_backend.Middleware;
using finance_tracker_backend.Repositories;
using finance_tracker_backend.Services;
using Going.Plaid;
using Hangfire;
using Hangfire.Common;
using Hangfire.PostgreSql;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

// Startup map — where integrations begin:
// • Supabase: (1) JWT Bearer validates access tokens your frontend got from Supabase Auth (issuer = {Supabase:Connection:Url}/auth/v1, JWKS from OIDC metadata).
//   (2) Supabase.Client singleton uses Supabase:Authentication:SecretKey (service_role / secret) — server-side PostgREST; bypasses RLS. Repositories inject this client.
//   After Build: InitializeAsync() loads PostgREST schema. Config: Supabase:Connection:Url, Supabase:Authentication:*.
// • Plaid: Going.Plaid PlaidClient registered below → IPlaidConnectionService + IPlaidTransactionSyncService → PlaidController (api/Plaid/*).
//   Config: Infrastructure/PlaidConfiguration.cs + appsettings Plaid:*.
// • Hangfire: PostgreSQL on ConnectionStrings:Default, schema "hangfire"; AddHangfireServer runs workers with the web app.
//   After Build: IRecurringJobManager → ExpiredPlaidLinkSessionsCleanupJob (Hangfire:ExpiredPlaidLinkSessionsCleanupCron, default 04:00 UTC);
//   PlaidTransactionSyncJob (Hangfire:PlaidTransactionSyncCron, default 02:00 UTC on day 1 of the month; Hangfire:PlaidSyncBatchSize);
//   MonthlyNetWorthJob (Hangfire:MonthlyNetWorthCron, default 03:00 UTC on day 1 of the month; Hangfire:MonthlyNetWorthBatchSize);
//   RecurringCashflowPredictedDateAdvanceJob (Hangfire:RecurringCashflowPredictedDateAdvanceCron, default 01:00 UTC daily; batch Hangfire:RecurringCashflowAdvanceBatchSize; rows with predicted_next_date <= UTC run date, multi-step catch-up per row).
// • Optional: /hangfire dashboard — add UseHangfireDashboard in Development if you want the UI (not enabled by default).

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

// Supabase Auth — validate API JWTs (same project as Supabase:Connection:Url). Docs: https://supabase.com/docs/guides/auth/jwts
var jwtAudience = builder.Configuration["Supabase:Authentication:JwtAudience"] ?? "authenticated";
var url = builder.Configuration["Supabase:Connection:Url"]?.TrimEnd('/');
var secretKey = builder.Configuration["Supabase:Authentication:SecretKey"];
if (string.IsNullOrWhiteSpace(url))
    throw new InvalidOperationException("Supabase:Connection:Url is required for JWT validation and Supabase client.");
if (string.IsNullOrWhiteSpace(secretKey))
    throw new InvalidOperationException(
        "Supabase:Authentication:SecretKey is required for server-side Supabase client. Use User Secrets.");

var authIssuer = $"{url}/auth/v1";
// JWT: Supabase Auth issuer + JWKS (see https://supabase.com/docs/guides/auth/jwts).
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

// Supabase server client (PostgREST + service key). Injected into *Repository; RLS is bypassed — enforce profile_id in queries.
builder.Services.AddSingleton(_ => new Supabase.Client(
    url,
    secretKey,
    new Supabase.SupabaseOptions
    {
        AutoRefreshToken = false,
        AutoConnectRealtime = false
    }));

// Data protection (for Plaid access tokens).
builder.Services.AddDataProtection();
builder.Services.AddSingleton<PlaidAccessTokenProtector>();

// Plaid API client (singleton). Used only by PlaidConnectionService.
// Secrets/env: Infrastructure/PlaidConfiguration.cs + appsettings Plaid:* (ClientId, Environment, SandboxSecret|ProductionSecret).
builder.Services.AddSingleton(sp =>
{
    var configuration = sp.GetRequiredService<IConfiguration>();
    var secret = PlaidConfiguration.ResolveSecret(configuration);
    var env = PlaidConfiguration.MapApiEnvironment(configuration["Plaid:Environment"]);
    var clientId = (configuration["Plaid:ClientId"] ?? string.Empty).Trim();
    return new PlaidClient(env, secret: secret, clientId: clientId);
});

// Hangfire: same Postgres as the app; creates schema "hangfire" for job storage.
var hangfireConnection = builder.Configuration.GetConnectionString("Default");
if (string.IsNullOrWhiteSpace(hangfireConnection))
{
    throw new InvalidOperationException(
        "ConnectionStrings:Default must be set to a PostgreSQL connection string for Hangfire.");
}

// Hangfire: same Postgres as the app; creates schema "hangfire" for job storage.
builder.Services.AddHangfire(configuration => configuration
    .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
    .UseSimpleAssemblyNameTypeSerializer()
    .UseRecommendedSerializerSettings()
    .UsePostgreSqlStorage(
        o => o.UseNpgsqlConnection(hangfireConnection),
        new PostgreSqlStorageOptions
        {
            SchemaName = "hangfire",
            PrepareSchemaIfNecessary = true
        }));
builder.Services.AddHangfireServer();
builder.Services.AddTransient<ExpiredPlaidLinkSessionsCleanupJob>();
builder.Services.AddTransient<PlaidTransactionSyncJob>();
builder.Services.AddTransient<MonthlyNetWorthJob>();
builder.Services.AddTransient<RecurringCashflowPredictedDateAdvanceJob>();

// Repositories
builder.Services.AddScoped<IProfileRepository, ProfileRepository>();
builder.Services.AddScoped<IProfileSubscriptionRepository, ProfileSubscriptionRepository>();
builder.Services.AddScoped<IEmailLogRepository, EmailLogRepository>();
builder.Services.AddScoped<IPlaidLinkSessionRepository, PlaidLinkSessionRepository>();
builder.Services.AddScoped<ILinkedBankRepository, LinkedBankRepository>();
builder.Services.AddScoped<ILinkedBankAccountRepository, LinkedBankAccountRepository>();
builder.Services.AddScoped<ITransactionRepository, TransactionRepository>();
builder.Services.AddScoped<IProfileMonthlyNetWorthRepository, ProfileMonthlyNetWorthRepository>();
builder.Services.AddScoped<IProfileRecurringCashflowRepository, ProfileRecurringCashflowRepository>();
builder.Services.AddScoped<IPlaidFinanceCategoryPrimaryRepository, PlaidFinanceCategoryPrimaryRepository>();
builder.Services.AddScoped<IPlanRepository, PlanRepository>();

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
builder.Services.AddScoped<IPlaidConnectionService, PlaidConnectionService>();
builder.Services.AddScoped<IPlaidTransactionSyncService, PlaidTransactionSyncService>();
builder.Services.AddScoped<IPlaidRecurringCashflowRefreshService, PlaidRecurringCashflowRefreshService>();
builder.Services.AddScoped<IProfileRecurringCashflowService, ProfileRecurringCashflowService>();
builder.Services.AddScoped<IRecurringCashflowAdvanceService, RecurringCashflowAdvanceService>();
builder.Services.AddScoped<ITransactionService, TransactionService>();
builder.Services.AddScoped<IPlaidFinanceCategoryPrimaryReadService, PlaidFinanceCategoryPrimaryReadService>();
builder.Services.AddScoped<INetWorthService, NetWorthService>();
builder.Services.AddScoped<ICashflowService, CashflowService>();
builder.Services.AddScoped<IPfcPrimaryExpenseDistributionService, PfcPrimaryExpenseDistributionService>();
builder.Services.AddScoped<IStackedExpensesByPfcPrimaryService, StackedExpensesByPfcPrimaryService>();
builder.Services.AddScoped<IGroupedExpensesByAccountService, GroupedExpensesByAccountService>();
builder.Services.AddScoped<IPlanService, PlanService>();

var app = builder.Build();

app.UseExceptionHandler();

// Loads PostgREST table/column metadata for the Supabase.Client (required before repository calls).
await app.Services.GetRequiredService<Supabase.Client>().InitializeAsync();

// Hangfire recurring jobs must be registered after Build (needs IRecurringJobManager from DI).
var expiredPlaidLinkSessionsCleanupCron =
    app.Configuration["Hangfire:ExpiredPlaidLinkSessionsCleanupCron"] ?? "0 4 * * *";
app.Services.GetRequiredService<IRecurringJobManager>().AddOrUpdate(
    "expired-plaid-link-sessions-cleanup",
    Job.FromExpression<ExpiredPlaidLinkSessionsCleanupJob>(job => job.RunAsync()),
    expiredPlaidLinkSessionsCleanupCron,
    new RecurringJobOptions { TimeZone = TimeZoneInfo.Utc });

var plaidTransactionSyncCron =
    app.Configuration["Hangfire:PlaidTransactionSyncCron"] ?? "0 2 1 * *";
app.Services.GetRequiredService<IRecurringJobManager>().AddOrUpdate(
    "plaid-transaction-sync",
    Job.FromExpression<PlaidTransactionSyncJob>(job => job.RunAsync()),
    plaidTransactionSyncCron,
    new RecurringJobOptions { TimeZone = TimeZoneInfo.Utc });

var monthlyNetWorthCron =
    app.Configuration["Hangfire:MonthlyNetWorthCron"] ?? "0 3 1 * *";
app.Services.GetRequiredService<IRecurringJobManager>().AddOrUpdate(
    "monthly-net-worth",
    Job.FromExpression<MonthlyNetWorthJob>(job => job.RunAsync()),
    monthlyNetWorthCron,
    new RecurringJobOptions { TimeZone = TimeZoneInfo.Utc });

var recurringCashflowAdvanceCron =
    app.Configuration["Hangfire:RecurringCashflowPredictedDateAdvanceCron"] ?? "0 1 * * *";
app.Services.GetRequiredService<IRecurringJobManager>().AddOrUpdate(
    "recurring-cashflow-predicted-date-advance",
    Job.FromExpression<RecurringCashflowPredictedDateAdvanceJob>(job => job.RunAsync()),
    recurringCashflowAdvanceCron,
    new RecurringJobOptions { TimeZone = TimeZoneInfo.Utc });

if (app.Environment.IsDevelopment())
    app.MapOpenApi();

app.UseHttpsRedirection();
app.UseCors(corsPolicyName);
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
