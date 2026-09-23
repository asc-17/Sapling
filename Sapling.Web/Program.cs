using Anthropic.SDK;
using Azure.Communication.Email;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.AI;
using Sapling.Shared.Contracts;
using Sapling.Shared.Services;
using Sapling.Shared.Theming;
using Sapling.Web.Ai;
using Sapling.Web.Api;
using Sapling.Web.Components;
using Sapling.Web.Data;
using Sapling.Web.Services;

var builder = WebApplication.CreateBuilder(args);

const string BrowserUserPolicy = "BrowserUser";

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents()
    .AddHubOptions(options =>
    {
        options.MaximumReceiveMessageSize = 10 * 1024 * 1024;
    });

builder.Services.AddCascadingAuthenticationState();
builder.Services.AddHttpContextAccessor();

builder.Services.AddDbContext<SaplingDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("Sapling") ?? "Data Source=sapling.db"));

builder.Services.AddIdentityApiEndpoints<AppUser>(options =>
    {
        options.Password.RequireNonAlphanumeric = false;
        options.Password.RequiredLength = 8;
        options.User.RequireUniqueEmail = true;
        options.SignIn.RequireConfirmedAccount = false;
    })
    .AddEntityFrameworkStores<SaplingDbContext>()
    .AddClaimsPrincipalFactory<AppUserClaimsPrincipalFactory>();

// Pages challenge through the cookie handler so a signed-out visitor is redirected, not 401'd.
builder.Services.AddAuthorizationBuilder()
    .AddPolicy(BrowserUserPolicy, policy => policy
        .AddAuthenticationSchemes(IdentityConstants.ApplicationScheme)
        .RequireAuthenticatedUser());

// Registered only when configured, so a checkout without credentials still starts; the button then says so.
var googleClientId = builder.Configuration["Authentication:Google:ClientId"];
var googleClientSecret = builder.Configuration["Authentication:Google:ClientSecret"];
if (!string.IsNullOrWhiteSpace(googleClientId) && !string.IsNullOrWhiteSpace(googleClientSecret))
{
    builder.Services.AddAuthentication().AddGoogle(options =>
    {
        options.ClientId = googleClientId;
        options.ClientSecret = googleClientSecret;
        ExternalAuthEndpoints.ConfigureGoogle(options);
    });
}

builder.Services.AddMemoryCache();

builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/signin";
    options.LogoutPath = "/signout";
    options.AccessDeniedPath = "/signin";
    options.ExpireTimeSpan = TimeSpan.FromDays(14);
    options.SlidingExpiration = true;
});

var anthropicKey = builder.Configuration["Ai:Anthropic:ApiKey"];
if (!string.IsNullOrWhiteSpace(anthropicKey))
{
    var model = builder.Configuration["Ai:Anthropic:Model"] ?? "claude-sonnet-4-5-20250929";
    builder.Services.AddChatClient(new AnthropicClient(anthropicKey).Messages)
        .ConfigureOptions(options => options.ModelId ??= model);
}
else
{
    builder.Services.AddSingleton<IChatClient, ScriptedChatClient>();
}

builder.Services.AddScoped<StudentContext>();
builder.Services.AddScoped<ScoreService>();
builder.Services.AddScoped<IProfileService, ProfileService>();
builder.Services.AddScoped<IScoreService>(sp => sp.GetRequiredService<ScoreService>());
builder.Services.AddScoped<ICareerService, CareerService>();
builder.Services.AddScoped<ISkillGapService, SkillGapService>();
builder.Services.AddScoped<IRoadmapService, RoadmapService>();
builder.Services.AddScoped<IOpportunityService, OpportunityService>();
builder.Services.AddScoped<IResumeService, ResumeService>();
builder.Services.AddScoped<IInterviewService, InterviewService>();
builder.Services.AddScoped<IGovtService, GovtService>();
builder.Services.AddScoped<IQuizService, QuizService>();
builder.Services.AddScoped<ICommunityService, CommunityService>();
builder.Services.AddScoped<AccountFlowService>();

var azureEmailConnection = builder.Configuration["Email:Azure:ConnectionString"];
var azureEmailSender = builder.Configuration["Email:Azure:SenderAddress"];
if (!string.IsNullOrWhiteSpace(azureEmailConnection) && !string.IsNullOrWhiteSpace(azureEmailSender))
{
    builder.Services.AddSingleton(new EmailClient(azureEmailConnection));
    builder.Services.AddSingleton<IEmailDelivery>(sp => new AzureEmailDelivery(
        sp.GetRequiredService<EmailClient>(), azureEmailSender, sp.GetRequiredService<ILogger<AzureEmailDelivery>>()));
}
else if (builder.Environment.IsDevelopment())
{
    builder.Services.AddSingleton<IEmailDelivery, LoggedEmailDelivery>();
}
else
{
    builder.Services.AddSingleton<IEmailDelivery, UnconfiguredEmailDelivery>();
}

builder.Services.AddSingleton<IFormFactor, DesktopFormFactor>();
builder.Services.AddScoped<IThemeService, BrowserThemeService>();
builder.Services.AddScoped<IRecentFeaturesService, BrowserRecentFeaturesService>();
builder.Services.AddSingleton<ICollegeCatalogService, CollegeCatalogService>();

var app = builder.Build();

if (string.IsNullOrWhiteSpace(googleClientId) != string.IsNullOrWhiteSpace(googleClientSecret))
{
    app.Logger.LogWarning(
        "Google sign-in is off: set both Authentication:Google:ClientId and Authentication:Google:ClientSecret ({Missing} is missing).",
        string.IsNullOrWhiteSpace(googleClientId) ? "ClientId" : "ClientSecret");
}

// Pre-initialize in-memory college catalog
_ = Task.Run(() => app.Services.GetRequiredService<ICollegeCatalogService>().InitializeAsync());

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();

    // Left off in development so the MAUI head can reach the API over plain HTTP.
    app.UseHttpsRedirection();
}

app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseAuthentication();
app.UseAuthorization();
app.UseAntiforgery();

app.MapStaticAssets();
app.MapGroup("/api/identity").MapIdentityApi<AppUser>().RetireUnverifiedIdentityRoutes();
app.MapAccountApi();
app.MapExternalAuth();
app.MapSaplingApi();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode()
    .AddAdditionalAssemblies(typeof(Sapling.Shared._Imports).Assembly)
    .RequireAuthorization(BrowserUserPolicy);

await DemoSeeder.SeedAsync(app.Services);

app.Run();
