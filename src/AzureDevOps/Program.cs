using AzureDevOps;
using AzureDevOps.Services;
using AzureDevOps.Web;
using AzureDevOps.Web.Pages;
using RazorComponentHelpers;

var builder = WebApplication.CreateBuilder(args);

// Add configuration from environment variables with AzureDevOps_ prefix
builder.Configuration.AddEnvironmentVariables(prefix: "AzureDevOps_");

// Configure strongly typed settings
builder.Services.Configure<AppSettings>(builder.Configuration);

var appSettings = new AppSettings();
builder.Configuration.Bind(appSettings);

// Register the HttpClient and AzureDevOpsService
builder.Services.AddHttpClient<AzureDevOpsService>();
builder.Services.AddScoped<AzureDevOpsService>();
builder.Services.AddScoped<AzureDevOpsQueryProxy>();
builder.Services.AddScoped<IAzureDevOpsQuery>(svc => svc.GetRequiredService<AzureDevOpsQueryProxy>());
builder.Services.AddScoped<IAzureDevOpsCommand>(svc => svc.GetRequiredService<AzureDevOpsQueryProxy>());

builder.Services.AddSingleton<Worker>();
builder.Services.AddHostedService<Worker>(svc => svc.GetRequiredService<Worker>());

// Add services to the container.
builder.Services.AddRazorComponents();
builder.Services.AddScoped<Renderer>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseStaticFiles();
app.MapStaticAssets();
app.UseAntiforgery();

app.MapRazorComponents<App>();

Main.AddEndpoints(app);

app.Run();
