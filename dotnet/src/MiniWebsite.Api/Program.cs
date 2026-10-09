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

    // Local secrets (gitignored) — e.g. production DB credentials for Development.
    builder.Configuration.AddJsonFile("appsettings.Development.local.json", optional: true, reloadOnChange: true);

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

    // Skip HTTPS redirect in Development so Swagger on http://localhost:5209
    // is not bounced to https://localhost:7236 (Failed to fetch / SSL trust).
    if (!app.Environment.IsDevelopment())
        app.UseHttpsRedirection();

    app.UseCors("Frontend");
    app.UseAuthentication();
    app.UseAuthorization();
    app.MapControllers();
    app.MapHealthChecks("/health");

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
