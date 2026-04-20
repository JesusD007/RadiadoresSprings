using FluentValidation;
using FluentValidation.AspNetCore;
using IntegrationApp.BackgroundServices;
using IntegrationApp.Data;
using IntegrationApp.Hubs;
using IntegrationApp.Infrastructure;
using IntegrationApp.Middleware;
using IntegrationApp.Services;
using IntegrationApp.Validators;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Http.Resilience;
using Microsoft.IdentityModel.Tokens;
using Polly;
using Scalar.AspNetCore;
using Serilog;
using Serilog.Events;
using System.Text;

// ─────────────────────────────────────────────────────────────────────────────
// BOOTSTRAP SERILOG (antes del host para capturar errores de startup)
// ─────────────────────────────────────────────────────────────────────────────
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
    .MinimumLevel.Override("Microsoft.EntityFrameworkCore", LogEventLevel.Warning)
    .Enrich.FromLogContext()
    .Enrich.WithProperty("Application", "IntegrationApp")
    .Enrich.WithProperty("Layer", "Integracion")
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    Log.Information("=== Iniciando IntegrationApp ===");

    var builder = WebApplication.CreateBuilder(args);

    // ─────────────────────────────────────────────────────────────────────────
    // LOCALDB FILES — solo en Development; en producción usa la connection string del entorno
    // ─────────────────────────────────────────────────────────────────────────
    if (builder.Environment.IsDevelopment())
    {
        var dataDir = Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), "DataBases", "Integracion"));
        Directory.CreateDirectory(dataDir);

        var targetMdf = Path.Combine(dataDir, "IntegrationAppDb.mdf");
        var targetLdf = Path.Combine(dataDir, "IntegrationAppDb_log.ldf");

        try
        {
            using var masterConn = new Microsoft.Data.SqlClient.SqlConnection(
                "Server=(localdb)\\MSSQLLocalDB;Database=master;Trusted_Connection=True;");
            masterConn.Open();

            using var checkCmd = masterConn.CreateCommand();
            checkCmd.CommandText = "SELECT DB_ID('IntegrationAppDb')";
            var dbId = checkCmd.ExecuteScalar();
            var dbExists = dbId is not DBNull && dbId is not null;

            if (dbExists && !File.Exists(targetMdf))
            {
                using var detachCmd = masterConn.CreateCommand();
                detachCmd.CommandText = "EXEC master.sys.sp_detach_db @dbname = N'IntegrationAppDb', @skipchecks = N'true'";
                detachCmd.ExecuteNonQuery();
                dbExists = false;
                Log.Information("[DB] BD anterior desvinculada del catálogo LocalDB");
            }

            if (!dbExists)
            {
                using var createCmd = masterConn.CreateCommand();
                createCmd.CommandText = $"""
                    CREATE DATABASE IntegrationAppDb
                    ON  PRIMARY (NAME = N'IntegrationAppDb', FILENAME = N'{targetMdf}')
                    LOG ON      (NAME = N'IntegrationAppDb_log', FILENAME = N'{targetLdf}')
                    """;
                createCmd.ExecuteNonQuery();
                Log.Information("[DB] Base de datos creada en {Dir}", dataDir);
            }
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "[DB] No se pudo gestionar la BD en la carpeta del proyecto");
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    // SERILOG FULL (reconfigura con sinks desde appsettings)
    // ─────────────────────────────────────────────────────────────────────────
    builder.Host.UseSerilog((ctx, services, config) =>
    {
        var seqUrl = ctx.Configuration["Serilog:SeqUrl"] ?? "http://localhost:5341";
        var connStr = ctx.Configuration.GetConnectionString("IntegrationDb")!;

        config
            .MinimumLevel.Information()
            .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
            .MinimumLevel.Override("Microsoft.EntityFrameworkCore", LogEventLevel.Warning)
            .Enrich.FromLogContext()
            .Enrich.WithProperty("Application", "IntegrationApp")
            .Enrich.WithProperty("Layer", "Integracion")
            .WriteTo.Console()
            .WriteTo.Seq(seqUrl, restrictedToMinimumLevel: LogEventLevel.Information)
            .WriteTo.MSSqlServer(connStr,
                sinkOptions: new Serilog.Sinks.MSSqlServer.MSSqlServerSinkOptions
                {
                    TableName = "SerilogEvents",
                    AutoCreateSqlTable = true
                },
                restrictedToMinimumLevel: LogEventLevel.Warning);
    });

    // ─────────────────────────────────────────────────────────────────────────
    // NSERVICEBUS
    // ─────────────────────────────────────────────────────────────────────────
    builder.Host.UseNServiceBus(ctx =>
    {
        var endpointConfig = new EndpointConfiguration("IntegrationApp");

        var transport = endpointConfig.UseTransport<LearningTransport>();

        endpointConfig.EnableInstallers();
        endpointConfig.UseSerialization<NServiceBus.SystemJsonSerializer>();

        // Saga persistence (Learning para dev)
        var persistence = endpointConfig.UsePersistence<LearningPersistence>();

        // Dead letter: mensajes fallidos van a error queue
        endpointConfig.SendFailedMessagesTo("IntegrationApp.Error");

        // Licencia: en desarrollo usar trial sin bloqueo
        endpointConfig.License(
            "<aws:License Id=\"Trial\" Expiry=\"9999-01-01\" Edition=\"Developer\" xmlns:aws=\"http://particular.net/license\" />");

        return endpointConfig;
    });

    // BackgroundService: no derribar el host si un servicio de fondo falla
    builder.Services.Configure<HostOptions>(opts =>
    {
        opts.BackgroundServiceExceptionBehavior = BackgroundServiceExceptionBehavior.Ignore;
    });

    // ─────────────────────────────────────────────────────────────────────────
    // FORWARDED HEADERS (ngrok / reverse proxy)
    // ─────────────────────────────────────────────────────────────────────────
    builder.Services.Configure<ForwardedHeadersOptions>(opts =>
    {
        opts.ForwardedHeaders = Microsoft.AspNetCore.HttpOverrides.ForwardedHeaders.XForwardedFor
                              | Microsoft.AspNetCore.HttpOverrides.ForwardedHeaders.XForwardedProto
                              | Microsoft.AspNetCore.HttpOverrides.ForwardedHeaders.XForwardedHost;
        opts.KnownNetworks.Clear();
        opts.KnownProxies.Clear();
    });

    // ─────────────────────────────────────────────────────────────────────────
    // ENTITY FRAMEWORK CORE
    // ─────────────────────────────────────────────────────────────────────────
    var connString = builder.Configuration.GetConnectionString("IntegrationDb")
        ?? throw new InvalidOperationException("Connection string 'IntegrationDb' not found.");

    builder.Services.AddDbContext<IntegrationDbContext>(options =>
        options.UseSqlServer(connString));

    // ─────────────────────────────────────────────────────────────────────────
    // HTTP CLIENT + POLLY v8 RESILIENCE PIPELINE
    // ─────────────────────────────────────────────────────────────────────────
    var coreUrl = builder.Configuration["CoreApi:BaseUrl"]
        ?? throw new InvalidOperationException("CoreApi:BaseUrl no configurado");

    builder.Services.AddHttpClient("CoreApi", c =>
    {
        c.BaseAddress = new Uri(coreUrl);
        c.Timeout = TimeSpan.FromSeconds(30);
        c.DefaultRequestHeaders.Add("X-Source", "IntegrationApp");
    })
    .AddStandardResilienceHandler(options =>
    {
        options.Retry.MaxRetryAttempts = 2;
        options.Retry.Delay = TimeSpan.FromSeconds(1);
        options.Retry.BackoffType = DelayBackoffType.Exponential;
        options.Retry.UseJitter = true;

        options.CircuitBreaker.SamplingDuration = TimeSpan.FromSeconds(30);
        options.CircuitBreaker.FailureRatio = 0.5;
        options.CircuitBreaker.MinimumThroughput = 3;
        options.CircuitBreaker.BreakDuration = TimeSpan.FromSeconds(
            builder.Configuration.GetValue<int>("CoreApi:CircuitBreakerDurationSeconds", 60));

        options.AttemptTimeout.Timeout = TimeSpan.FromSeconds(15);
    });

    // ─────────────────────────────────────────────────────────────────────────
    // AUTENTICACIÓN JWT (validar tokens del Core)
    // ─────────────────────────────────────────────────────────────────────────
    var jwtKey = builder.Configuration["Jwt:Key"]
        ?? throw new InvalidOperationException("Jwt:Key no configurado");

    builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
        .AddJwtBearer(options =>
        {
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer = builder.Configuration["Jwt:Issuer"],
                ValidAudience = builder.Configuration["Jwt:Audience"],
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
            };
        });

    builder.Services.AddAuthorization();

    // ─────────────────────────────────────────────────────────────────────────
    // SIGNALR
    // ─────────────────────────────────────────────────────────────────────────
    builder.Services.AddSignalR();

    // ─────────────────────────────────────────────────────────────────────────
    // SERVICIOS DE INTEGRACIÓN
    // ─────────────────────────────────────────────────────────────────────────
    builder.Services.AddSingleton<ICircuitBreakerStateService, CircuitBreakerStateService>();
    builder.Services.AddScoped<ICoreApiClient, CoreApiClient>();

    // ─────────────────────────────────────────────────────────────────────────
    // BACKGROUND SERVICES
    // ─────────────────────────────────────────────────────────────────────────
    builder.Services.AddHostedService<CoreHealthCheckService>();
    builder.Services.AddHostedService<MirrorSyncService>();

    // ─────────────────────────────────────────────────────────────────────────
    // FLUENTVALIDATION
    // ─────────────────────────────────────────────────────────────────────────
    builder.Services.AddFluentValidationAutoValidation();
    builder.Services.AddValidatorsFromAssemblyContaining<LoginRequestValidator>();

    // ─────────────────────────────────────────────────────────────────────────
    // CONTROLLERS + SCALAR (OpenAPI nativo .NET 9)
    // ─────────────────────────────────────────────────────────────────────────
    builder.Services.AddControllers()
        .AddJsonOptions(opts =>
        {
            opts.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
        });

    builder.Services.AddOpenApi("v1", options =>
    {
        options.AddDocumentTransformer((doc, ctx, ct) =>
        {
            doc.Info = new()
            {
                Title = "IntegrationApp — API Gateway",
                Version = "v1",
                Description = "Capa de Integración del sistema Tienda de Radiadores. " +
                              "Actúa como API Gateway entre Caja POS / Website y el Core backend."
            };
            return Task.CompletedTask;
        });

        // Soporte JWT Bearer en Scalar
        options.AddDocumentTransformer<BearerSecuritySchemeTransformer>();
    });

    // ─────────────────────────────────────────────────────────────────────────
    // HEALTH CHECKS
    // ─────────────────────────────────────────────────────────────────────────
    builder.Services.AddHealthChecks()
        .AddSqlServer(connString, name: "sqlserver", tags: ["db", "ready"]);

    builder.Services.AddMemoryCache();
    builder.Services.AddCors(options =>
    {
        options.AddDefaultPolicy(policy =>
            policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader());
    });

    // ═════════════════════════════════════════════════════════════════════════
    var app = builder.Build();
    // ═════════════════════════════════════════════════════════════════════════

    // Auto-migrar BD en startup (dev convenience)
    using (var scope = app.Services.CreateScope())
    {
        var db = scope.ServiceProvider.GetRequiredService<IntegrationDbContext>();
        db.Database.Migrate();
        Log.Information("Migraciones EF Core aplicadas correctamente");
    }

    // Pipeline HTTP
    app.UseForwardedHeaders();
    app.UseSerilogRequestLogging();
    app.UseMiddleware<CorrelationIdMiddleware>();
    app.UseMiddleware<IntegrationLoggingMiddleware>();

    // OpenAPI JSON + Scalar UI (solo en desarrollo; en producción se puede restringir)
    app.MapOpenApi();
    app.MapScalarApiReference(options =>
    {
        options.Title = "IntegrationApp — API Gateway";
        options.Theme = ScalarTheme.DeepSpace;
        options.DefaultHttpClient = new(ScalarTarget.CSharp, ScalarClient.HttpClient);
        options.AddPreferredSecuritySchemes("Bearer");
    });

    app.UseCors();
    app.UseAuthentication();
    app.UseAuthorization();

    app.MapControllers();
    app.MapHub<NotificacionesHub>("/hubs/notificaciones");
    app.MapHealthChecks("/health/ready");

    Log.Information("IntegrationApp iniciado en: {Urls}", string.Join(", ", app.Urls));
    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "IntegrationApp terminó inesperadamente durante el startup");
}
finally
{
    Log.CloseAndFlush();
}
