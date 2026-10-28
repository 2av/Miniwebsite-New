using MiniWebsite.Application;
using MiniWebsite.Api.Middleware;
using MiniWebsite.Infrastructure;
using MiniWebsite.Infrastructure.Persistence;
using Microsoft.OpenApi.Models;
using Serilog;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);

    // Local secrets (gitignored), e.g. appsettings.Development.local.json or a server-only appsettings.Production.local.json.
    builder.Configuration.AddJsonFile(
        $"appsettings.{builder.Environment.EnvironmentName}.local.json", optional: true, reloadOnChange: true);

    builder.Host.UseSerilog((context, services, configuration) => configuration
        .ReadFrom.Configuration(context.Configuration)
        .ReadFrom.Services(services)
        .Enrich.FromLogContext()
        .WriteTo.Console());

    builder.Services.AddApplication();
    builder.Services.AddInfrastructure(builder.Configuration);

    builder.Services.AddControllers();
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen(c =>
    {
        c.SwaggerDoc("v1", new OpenApiInfo
        {
            Title = "ShareBizz API",
            Version = "v1",
            Description = "Backend API for ShareBizz — Auth, CRUD, Payments, Email."
        });

        // Bearer auth temporarily removed from Swagger — re-add AddSecurityDefinition later.
    });

    builder.Services.AddCors(options =>
    {
        options.AddPolicy("Frontend", policy =>
            policy.AllowAnyHeader()
                  .AllowAnyMethod()
                  .AllowAnyOrigin());
    });

    builder.Services.AddHealthChecks();
    builder.Services.AddMemoryCache();

    var app = builder.Build();

    using (var scope = app.Services.CreateScope())
    {
        try
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var seeder = scope.ServiceProvider.GetRequiredService<IDataSeeder>();
            var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("DatabaseBootstrap");
            await DatabaseBootstrap.MigrateAndSeedAsync(db, seeder, logger);
            Log.Information("Database migrate + seed completed.");
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Database migrate/seed failed — API will still start. Check ConnectionStrings:Default.");
        }
    }

    app.UseMiddleware<ExceptionHandlingMiddleware>();
    app.UseSerilogRequestLogging();

    var enableSwagger = app.Environment.IsDevelopment()
        || app.Configuration.GetValue("App:EnableSwagger", false);
    if (enableSwagger)
    {
        app.UseSwagger();
        app.UseSwaggerUI();
    }

    // CORS must run before HTTPS redirect. A redirected preflight is what the browser reports as "Network Error".
    app.UseCors("Frontend");

    // Opt in only. Forcing HTTPS inside Kestrel breaks hosts that terminate SSL in front of the app.
    if (app.Configuration.GetValue("App:UseHttpsRedirection", false))
        app.UseHttpsRedirection();

    app.UseAuthentication();
    app.UseAuthorization();

    var webRoot = app.Environment.WebRootPath ?? "";
    var userIndex = Path.Combine(webRoot, "index.html");
    var adminIndex = Path.Combine(webRoot, "admin", "index.html");
    var hostUserSite = File.Exists(userIndex);
    var hostAdminSite = File.Exists(adminIndex);
    if (hostUserSite || hostAdminSite)
    {
        app.UseDefaultFiles();
        app.UseStaticFiles();
    }

    app.MapControllers();
    app.MapHealthChecks("/health");

    if (hostAdminSite)
        app.MapGet("/admin", () => Results.Redirect("/admin/"));

    if (hostUserSite || hostAdminSite)
    {
        app.MapFallback(async (HttpContext context) =>
        {
            var path = context.Request.Path.Value ?? "/";
            if (path.StartsWith("/api", StringComparison.OrdinalIgnoreCase)
                || path.StartsWith("/health", StringComparison.OrdinalIgnoreCase))
            {
                context.Response.StatusCode = StatusCodes.Status404NotFound;
                return;
            }

            var file = hostAdminSite && path.StartsWith("/admin", StringComparison.OrdinalIgnoreCase)
                ? adminIndex
                : userIndex;
            if (!File.Exists(file))
            {
                context.Response.StatusCode = StatusCodes.Status404NotFound;
                return;
            }

            context.Response.ContentType = "text/html; charset=utf-8";
            await context.Response.SendFileAsync(file);
        });
    }

    Log.Information("MiniWebsite.Api starting (Phase 1 — Auth + Users)");
    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}

public partial class Program;
