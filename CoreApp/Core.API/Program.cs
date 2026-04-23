using Core.API.Authorization;
using Core.API.Services;
using Core.Data;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using NServiceBus;
using Scalar.AspNetCore;
using Serilog;
using System.Text;

// ─────────────────────────────────────────────────────────────────────────────
// Bootstrap Serilog
// ─────────────────────────────────────────────────────────────────────────────
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console(outputTemplate:
        "[{Timestamp:HH:mm:ss} {Level:u3}] {SourceContext} | {Message:lj}{NewLine}{Exception}")
    .WriteTo.Seq(Environment.GetEnvironmentVariable("SEQ_URL") ?? "http://localhost:5341")
    .Enrich.FromLogContext()
    .MinimumLevel.Information()
    .CreateLogger();

try
{
    Log.Information("🚀 Iniciando Core API — RadiadoresSprings P2");

    var builder = WebApplication.CreateBuilder(args);
    builder.Host.UseSerilog();

    // ── NServiceBus + RabbitMQ ────────────────────────────────────────────────
    var rabbitUrl = builder.Configuration["NServiceBus:RabbitMqUrl"]
        ?? "amqps://gaqafkho:zd53UuzWiYpnH2jb90tkHRDMB1eMCsfm@woodpecker.rmq.cloudamqp.com/gaqafkho";

    builder.Host.UseNServiceBus(ctx =>
    {
        var endpoint = new EndpointConfiguration("Core.API");

        // RabbitMQ Transport (compartido con IntegrationApp)
        var transport = endpoint.UseTransport(new RabbitMQTransport(
            RoutingTopology.Conventional(QueueType.Quorum),
            rabbitUrl));

        // Convenciones de mensajes (igual que IntegrationApp)
        var conventions = endpoint.Conventions();
        conventions.DefiningCommandsAs(t => t.Namespace?.Contains("Commands") == true);
        conventions.DefiningEventsAs(t => t.Namespace?.Contains("Events") == true);

        // Serializador requerido por NServiceBus 9 + RabbitMQ
        endpoint.UseSerialization<SystemJsonSerializer>();

        endpoint.SendFailedMessagesTo("error");
        endpoint.EnableInstallers();

        Log.Information("📬 NServiceBus configurado | Transport: RabbitMQ | Host: {Host}",
            new Uri(rabbitUrl).Host);
        return endpoint;
    });

    // ── Base de datos SQL Server ───────────────────────────────────────────────
    var connStr = builder.Configuration.GetConnectionString("CoreDb")
        ?? "Server=MSI;Database=RadiadoresSpringsCore;Trusted_Connection=True;TrustServerCertificate=True;";

    builder.Services.AddDbContext<CoreDbContext>(opt =>
        opt.UseSqlServer(connStr, sql => sql.EnableRetryOnFailure(3)));

    Log.Information("🗄️ SQL Server configurado: {Conn}", connStr.Split(';')[0]);

    // ── JWT Auth ──────────────────────────────────────────────────────────────
    var jwtKey = builder.Configuration["Jwt:Secret"] ?? "RadiadoresSpringsSecretKey2026!XYZ";
    var jwtIssuer = builder.Configuration["Jwt:Issuer"] ?? "CoreApi";
    var jwtAudience = builder.Configuration["Jwt:Audience"] ?? "IntegrationApp";

    builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
        .AddJwtBearer(opt =>
        {
            opt.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer = jwtIssuer,
                ValidAudience = jwtAudience,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
            };
        });

    // ── Políticas de autorización por rol ─────────────────────────────────────
    // ApiPolicies es la fuente única de verdad: nombres de política + grupos de roles.
    builder.Services.AddAuthorization(opt =>
    {
        opt.AddPolicy(ApiPolicies.Autenticado,
            p => p.RequireAuthenticatedUser()
                  .RequireRole(ApiPolicies.Roles_Autenticado));

        opt.AddPolicy(ApiPolicies.AdminSistema,
            p => p.RequireAuthenticatedUser()
                  .RequireRole(ApiPolicies.Roles_AdminSistema));

        opt.AddPolicy(ApiPolicies.GestionInventario,
            p => p.RequireAuthenticatedUser()
                  .RequireRole(ApiPolicies.Roles_GestionInventario));

        opt.AddPolicy(ApiPolicies.SincronizacionOffline,
            p => p.RequireAuthenticatedUser()
                  .RequireRole(ApiPolicies.Roles_SincronizacionOffline));

        opt.AddPolicy(ApiPolicies.GestionVentas,
            p => p.RequireAuthenticatedUser()
                  .RequireRole(ApiPolicies.Roles_GestionVentas));

        opt.AddPolicy(ApiPolicies.CancelarVentas,
            p => p.RequireAuthenticatedUser()
                  .RequireRole(ApiPolicies.Roles_CancelarVentas));

        opt.AddPolicy(ApiPolicies.GestionCaja,
            p => p.RequireAuthenticatedUser()
                  .RequireRole(ApiPolicies.Roles_GestionCaja));

        opt.AddPolicy(ApiPolicies.GestionOrdenes,
            p => p.RequireAuthenticatedUser()
                  .RequireRole(ApiPolicies.Roles_GestionOrdenes));

        opt.AddPolicy(ApiPolicies.GestionClientes,
            p => p.RequireAuthenticatedUser()
                  .RequireRole(ApiPolicies.Roles_GestionClientes));

        opt.AddPolicy(ApiPolicies.GestionPagos,
            p => p.RequireAuthenticatedUser()
                  .RequireRole(ApiPolicies.Roles_GestionPagos));
    });

    Log.Information("🔐 Políticas de autorización registradas ({Count} políticas)", 10);

    // ── Servicios N-Tier ──────────────────────────────────────────────────────
    builder.Services.AddScoped<IAuthService, AuthService>();
    builder.Services.AddScoped<IProductoService, ProductoService>();
    builder.Services.AddScoped<ICategoriaService, CategoriaService>();
    builder.Services.AddScoped<IClienteService, ClienteService>();
    builder.Services.AddScoped<IVentaService, VentaService>();
    builder.Services.AddScoped<ICajaService, CajaService>();
    builder.Services.AddScoped<IOrdenService, OrdenService>();
    builder.Services.AddScoped<IPagoService, PagoService>();
    builder.Services.AddScoped<ICuentaCobrarService, CuentaCobrarService>();

    // ── Controllers + OpenAPI ─────────────────────────────────────────────────
    builder.Services.AddControllers();
    builder.Services.AddOpenApi(opt =>
    {
        opt.AddDocumentTransformer((document, context, ct) =>
        {
            document.Components ??= new OpenApiComponents();
            document.Components.SecuritySchemes.Add("Bearer", new OpenApiSecurityScheme
            {
                Type = SecuritySchemeType.Http,
                Scheme = "bearer",
                BearerFormat = "JWT",
                Description = "Ingresa tu Token JWT puro (sin escribir la palabra Bearer)."
            });
            document.SecurityRequirements.Add(new OpenApiSecurityRequirement
            {
                [new OpenApiSecurityScheme
                {
                    Reference = new OpenApiReference
                    {
                        Type = ReferenceType.SecurityScheme,
                        Id = "Bearer"
                    }
                }] = Array.Empty<string>()
            });
            return Task.CompletedTask;
        });
    });

    // ── Health Checks ─────────────────────────────────────────────────────────
    builder.Services.AddHealthChecks()
        .AddSqlServer(
            connectionString: connStr,
            healthQuery: "SELECT 1;",
            name: "sql-server",
            failureStatus: Microsoft.Extensions.Diagnostics.HealthChecks.HealthStatus.Unhealthy);

    // ── CORS (permite ngrok y cualquier origen) ───────────────────────────────
    builder.Services.AddCors(opt => opt.AddPolicy("AllowAll", p =>
        p.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader()
         .WithExposedHeaders("X-Correlation-Id")));

    // ── Forwarded Headers (para ngrok y proxies inversos) ─────────────────────
    builder.Services.Configure<ForwardedHeadersOptions>(opt =>
    {
        opt.ForwardedHeaders = ForwardedHeaders.XForwardedFor
                             | ForwardedHeaders.XForwardedProto
                             | ForwardedHeaders.XForwardedHost;
        // Confiar en cualquier proxy (incluye ngrok)
        opt.KnownNetworks.Clear();
        opt.KnownProxies.Clear();
    });

    // ─────────────────────────────────────────────────────────────────────────
    var app = builder.Build();
    // ─────────────────────────────────────────────────────────────────────────

    // ── Creación automática de BD + Seed inicial ──────────────────────────────
    using (var scope = app.Services.CreateScope())
    {
        var db = scope.ServiceProvider.GetRequiredService<CoreDbContext>();
        Log.Information("⚙️ Creando/verificando base de datos SQL Server...");
        await db.Database.EnsureCreatedAsync();
        await SeedAsync(db);
        Log.Information("✅ Base de datos lista.");
    }

    // Forwarded headers primero (ngrok envía X-Forwarded-Proto: https)
    app.UseForwardedHeaders();

    app.UseSerilogRequestLogging();
    app.UseCors("AllowAll");

    if (app.Environment.IsDevelopment())
    {
        app.MapOpenApi();
        app.MapScalarApiReference(opt => opt.WithTitle("Core API — RadiadoresSprings"));
    }

    app.UseAuthentication();
    app.UseAuthorization();
    app.MapControllers();
    app.MapHealthChecks("/health");

    Log.Information("✅ Core API lista en https://localhost:5001");
    await app.RunAsync();
}
catch (Exception ex)
{
    Log.Fatal(ex, "💀 Core API terminó inesperadamente.");
}
finally
{
    Log.CloseAndFlush();
}

// ─────────────────────────────────────────────────────────────────────────────
// Seed inicial — idempotente (solo inserta si las tablas están vacías)
// ─────────────────────────────────────────────────────────────────────────────
static async Task SeedAsync(CoreDbContext db)
{
    // Sucursal
    if (!await db.Sucursales.AnyAsync())
    {
        db.Sucursales.Add(new Core.Domain.Entities.Sucursal
        {
            Nombre = "Sucursal Principal",
            Direccion = "Av. Principal #1",
            Telefono = "555-0001",
            EsActiva = true
        });
        await db.SaveChangesAsync();
        Log.Information("🌱 Seed: Sucursal creada");
    }

    var sucursal = await db.Sucursales.FirstAsync();

    // Categorías
    if (!await db.Categorias.AnyAsync())
    {
        db.Categorias.AddRange(
            new Core.Domain.Entities.Categoria { Nombre = "Radiadores",   Descripcion = "Radiadores para vehículos",  EsActiva = true },
            new Core.Domain.Entities.Categoria { Nombre = "Mangueras",    Descripcion = "Mangueras de enfriamiento",   EsActiva = true },
            new Core.Domain.Entities.Categoria { Nombre = "Ventiladores", Descripcion = "Ventiladores para radiador",  EsActiva = true },
            new Core.Domain.Entities.Categoria { Nombre = "Accesorios",   Descripcion = "Accesorios varios",          EsActiva = true }
        );
        await db.SaveChangesAsync();
        Log.Information("🌱 Seed: Categorías creadas");
    }

    // Caja
    if (!await db.Cajas.AnyAsync())
    {
        db.Cajas.Add(new Core.Domain.Entities.Caja
        {
            SucursalId = sucursal.Id,
            Numero = "C001",
            Nombre = "Caja Principal",
            EsActiva = true
        });
        await db.SaveChangesAsync();
        Log.Information("🌱 Seed: Caja creada");
    }

    // Usuario admin
    if (!await db.Usuarios.AnyAsync())
    {
        db.Usuarios.Add(new Core.Domain.Entities.Usuario
        {
            Username     = "admin",
            Nombre       = "Administrador",
            Apellido     = "Sistema",
            Email        = "admin@radiadores.com",
            SucursalId   = sucursal.Id,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Admin123!"),
            Rol          = Core.Domain.Enums.RolUsuario.Administrador,
            EsActivo     = true
        });
        await db.SaveChangesAsync();
        Log.Information("🌱 Seed: Usuario admin creado (admin / Admin123!)");
    }

    // ── Usuario de servicio para IntegrationApp (M2M) ────────────────────────
    // Se crea independientemente del admin: puede existir el admin sin el servicio
    // y viceversa. La password debe rotarse en producción vía variable de entorno.
    if (!await db.Usuarios.AnyAsync(u => u.Username == "servicio_web"))
    {
        var passServicio = Environment.GetEnvironmentVariable("SERVICIO_WEB_PASSWORD")
                           ?? "S3rv1c10W3b!2026";

        db.Usuarios.Add(new Core.Domain.Entities.Usuario
        {
            Username     = "servicio_web",
            Nombre       = "Servicio Web",
            Apellido     = "IntegrationApp",
            Email        = "servicio@radiadores.com",
            SucursalId   = sucursal.Id,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(passServicio),
            Rol          = Core.Domain.Enums.RolUsuario.Cliente,
            EsActivo     = true
        });
        await db.SaveChangesAsync();
        Log.Information("🌱 Seed: Usuario de servicio creado (servicio_web) — configura SERVICIO_WEB_PASSWORD en producción");
    }
}
