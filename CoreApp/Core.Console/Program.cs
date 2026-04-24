using Core.API.Services;
using Core.ConsoleUI.Auth;
using Core.ConsoleUI.Menus;
using Core.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NServiceBus;
using Serilog;
using Spectre.Console;

// ─────────────────────────────────────────────────────────────────────────────
// Configuración
// ─────────────────────────────────────────────────────────────────────────────
var env = Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT") ?? "Production";

var config = new ConfigurationBuilder()
    .SetBasePath(AppContext.BaseDirectory)
    .AddJsonFile("appsettings.json", optional: true)
    .AddJsonFile($"appsettings.{env}.json", optional: true)
    .AddEnvironmentVariables()
    .Build();

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}")
    .MinimumLevel.Warning()
    .CreateLogger();

var connStr = config.GetConnectionString("CoreDb")
    ?? "Server=MSI;Database=RadiadoresSpringsCore;Trusted_Connection=True;TrustServerCertificate=True;";

// ─────────────────────────────────────────────────────────────────────────────
// DI Container
// ─────────────────────────────────────────────────────────────────────────────
var services = new ServiceCollection();

services.AddDbContext<CoreDbContext>(opt =>
    opt.UseSqlServer(connStr, sql => sql.EnableRetryOnFailure(3)));

services.AddSingleton<IMessageSession>(_ => new NoOpMessageSession());
services.AddSingleton<IConfiguration>(config);
services.AddLogging(lb => lb.AddSerilog());

services.AddScoped<IAuthService, AuthService>();
services.AddScoped<IProductoService, ProductoService>();
services.AddScoped<ICategoriaService, CategoriaService>();
services.AddScoped<IClienteService, ClienteService>();
services.AddScoped<IVentaService, VentaService>();
services.AddScoped<ICajaService, CajaService>();
services.AddScoped<IOrdenService, OrdenService>();
services.AddScoped<IPagoService, PagoService>();
services.AddScoped<ICuentaCobrarService, CuentaCobrarService>();

var provider = services.BuildServiceProvider();

// ─────────────────────────────────────────────────────────────────────────────
// Verificar conexión a la base de datos
// ─────────────────────────────────────────────────────────────────────────────
AnsiConsole.Clear();

await AnsiConsole.Status()
    .Spinner(Spinner.Known.Dots)
    .SpinnerStyle(Style.Parse("cyan"))
    .StartAsync("Conectando a SQL Server...", async _ =>
    {
        using var scope = provider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<CoreDbContext>();
        await db.Database.EnsureCreatedAsync();
        await Task.Delay(400);
    });

// ─────────────────────────────────────────────────────────────────────────────
// PANTALLA DE LOGIN — gate de acceso obligatorio
// ─────────────────────────────────────────────────────────────────────────────
ConsoleSession? session;
using (var scope = provider.CreateScope())
{
    var authSvc = scope.ServiceProvider.GetRequiredService<IAuthService>();
    session = await LoginScreen.ShowAsync(authSvc);
}

if (session is null)
{
    // Intentos agotados — salir
    AnsiConsole.MarkupLine("\n[grey]Cerrando aplicación...[/]\n");
    return;
}

// ─────────────────────────────────────────────────────────────────────────────
// MENÚ PRINCIPAL (filtrado por rol)
// ─────────────────────────────────────────────────────────────────────────────
while (true)
{
    AnsiConsole.Clear();

    AnsiConsole.Write(new FigletText("RadiadoresSprings")
        .Centered().Color(Color.DeepSkyBlue1));

    // Header con usuario activo
    AnsiConsole.Write(new Rule(
        $"[bold grey]Core Management Console — P2[/]   " +
        $"[grey]|[/]   " +
        $"[bold]{Markup.Escape(session.NombreCompleto)}[/]  " +
        $"{session.RolBadge}  " +
        $"[grey]{Markup.Escape(session.NombreSucursal)}[/]")
    .RuleStyle(Style.Parse("grey")));

    // Dashboard de estado
    await MostrarDashboardAsync(provider);

    AnsiConsole.Write(new Rule().RuleStyle(Style.Parse("grey")));

    // Menú filtrado según rol
    var opcionesMenu = PermisoConsola.MenuPermitido(session.Rol);

    var opcion = AnsiConsole.Prompt(
        new SelectionPrompt<string>()
            .Title("[deepskyblue1 bold]¿Qué módulo deseas gestionar?[/]")
            .PageSize(12)
            .AddChoices(opcionesMenu));

    using var scope = provider.CreateScope();
    var sp = scope.ServiceProvider;

    switch (opcion)
    {
        case PermisoConsola.MProductos:
            await ProductosMenu.ShowAsync(
                sp.GetRequiredService<IProductoService>(),
                sp.GetRequiredService<ICategoriaService>(),
                session);
            break;

        case PermisoConsola.MVentas:
            await VentasMenu.ShowAsync(
                sp.GetRequiredService<IVentaService>(),
                sp.GetRequiredService<IProductoService>(),
                sp.GetRequiredService<IClienteService>(),
                session);
            break;

        case PermisoConsola.MClientes:
            await ClientesMenu.ShowAsync(
                sp.GetRequiredService<IClienteService>(),
                sp.GetRequiredService<ICuentaCobrarService>(),
                sp.GetRequiredService<IPagoService>(),
                session);
            break;

        case PermisoConsola.MCaja:
            await CajaMenu.ShowAsync(sp.GetRequiredService<ICajaService>(), session);
            break;

        case PermisoConsola.MOrdenes:
            await MostrarOrdenesAsync(sp.GetRequiredService<IOrdenService>());
            break;

        case PermisoConsola.MReportes:
            await MostrarReportesAsync(provider);
            break;

        case PermisoConsola.MBus:
            await MostrarBusMonitorAsync();
            break;

        case PermisoConsola.MUsuarios:
            await MostrarUsuariosAsync(sp.GetRequiredService<IAuthService>(), session);
            break;

        case PermisoConsola.MSalir:
            AnsiConsole.MarkupLine(
                $"\n[grey]Sesión cerrada. Hasta luego, {Markup.Escape(session.NombreCompleto)}.[/]\n");
            return;
    }
}

// ─────────────────────────────────────────────────────────────────────────────
// Dashboard de estado en tiempo real
// ─────────────────────────────────────────────────────────────────────────────
static async Task MostrarDashboardAsync(IServiceProvider provider)
{
    using var scope = provider.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<CoreDbContext>();

    var totalProductos  = await db.Productos.CountAsync(p => p.EsActivo);
    var stockBajo       = await db.Productos.CountAsync(p => p.EsActivo && p.Stock <= p.StockMinimo);
    var ventasHoy       = await db.Ventas.CountAsync(v => v.Fecha.Date == DateTime.UtcNow.Date);
    var totalHoy        = await db.Ventas
        .Where(v => v.Fecha.Date == DateTime.UtcNow.Date &&
                    v.Estado == Core.Domain.Enums.EstadoVenta.Completada)
        .SumAsync(v => (decimal?)v.Total) ?? 0;
    var cuentasVencidas = await db.CuentasCobrar.CountAsync(cc =>
        cc.FechaVencimiento < DateTime.UtcNow &&
        cc.Estado != Core.Domain.Enums.EstadoCuentaCobrar.Pagada &&
        cc.Estado != Core.Domain.Enums.EstadoCuentaCobrar.Cancelada);
    var ordenesActivas  = await db.Ordenes.CountAsync(o =>
        o.Estado != Core.Domain.Enums.EstadoOrden.Entregada &&
        o.Estado != Core.Domain.Enums.EstadoOrden.Cancelada);

    var grid = new Grid().AddColumn().AddColumn().AddColumn().AddColumn();
    grid.AddRow(
        CreateStatPanel("📦 Productos",     totalProductos.ToString(),  Color.Cyan1),
        CreateStatPanel("⚠️ Stock Bajo",    stockBajo.ToString(),       stockBajo > 0 ? Color.Red : Color.Green),
        CreateStatPanel("🧾 Ventas Hoy",    ventasHoy.ToString(),       Color.Yellow),
        CreateStatPanel("💰 Total Hoy",     $"${totalHoy:N2}",          Color.Gold1)
    );
    grid.AddRow(
        CreateStatPanel("💳 Ctas. Vencidas",  cuentasVencidas.ToString(), cuentasVencidas > 0 ? Color.Red : Color.Green),
        CreateStatPanel("📋 Órdenes Activas", ordenesActivas.ToString(),  Color.Blue),
        CreateStatPanel("🗄️ SQL Server",      "Conectado",               Color.Green),
        CreateStatPanel("⏰ Hora",            DateTime.Now.ToString("HH:mm:ss"), Color.Grey)
    );

    AnsiConsole.Write(grid);
}

static Panel CreateStatPanel(string titulo, string valor, Color color) =>
    new Panel(
        $"[grey]{titulo}[/]\n[{color.ToMarkup()} bold]{valor}[/]")
        .Border(BoxBorder.Rounded)
        .BorderColor(color)
        .Padding(1, 0);

// ─────────────────────────────────────────────────────────────────────────────
// Órdenes (vista rápida)
// ─────────────────────────────────────────────────────────────────────────────
static async Task MostrarOrdenesAsync(IOrdenService svc)
{
    AnsiConsole.Clear();
    AnsiConsole.Write(new FigletText("Ordenes").Color(Color.Blue));

    var result = await svc.GetPagedAsync(1, 50, null);
    var table  = new Table().Border(TableBorder.Rounded)
        .AddColumn("[bold]ID[/]").AddColumn("[bold]Número[/]")
        .AddColumn("[bold]Cliente[/]").AddColumn("[bold]Estado[/]")
        .AddColumn("[bold]Fecha[/]")
        .AddColumn(new TableColumn("[bold]Total[/]").RightAligned());

    foreach (var o in result.Items)
    {
        var estadoColor = o.Estado switch
        {
            "Entregada" => "[green]",
            "Cancelada" => "[red]",
            "Enviada"   => "[blue]",
            _           => "[yellow]"
        };
        table.AddRow(
            o.Id.ToString(),
            o.NumeroOrden,
            Markup.Escape(o.NombreCliente),
            $"{estadoColor}{o.Estado}[/]",
            o.Fecha.ToLocalTime().ToString("dd/MM/yyyy"),
            $"[yellow]${o.TotalOrden:F2}[/]");
    }

    AnsiConsole.Write(table);
    AnsiConsole.MarkupLine("\n[grey]ENTER para continuar...[/]");
    Console.ReadLine();
}

// ─────────────────────────────────────────────────────────────────────────────
// Reportes
// ─────────────────────────────────────────────────────────────────────────────
static async Task MostrarReportesAsync(IServiceProvider provider)
{
    AnsiConsole.Clear();
    AnsiConsole.Write(new FigletText("Reportes").Color(Color.Purple));

    using var scope = provider.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<CoreDbContext>();

    var ventasPorMetodo = await db.Ventas
        .Where(v => v.Estado == Core.Domain.Enums.EstadoVenta.Completada)
        .GroupBy(v => v.MetodoPago)
        .Select(g => new { Metodo = g.Key.ToString(), Total = g.Sum(v => v.Total), Cantidad = g.Count() })
        .ToListAsync();

    AnsiConsole.MarkupLine("[bold]📊 Ventas por método de pago:[/]\n");
    if (ventasPorMetodo.Any())
    {
        var barChart = new BarChart().Width(60).Label("[bold]Total por método[/]");
        foreach (var vm in ventasPorMetodo)
            barChart.AddItem(vm.Metodo, (double)vm.Total, Color.Cyan1);
        AnsiConsole.Write(barChart);
    }
    else
    {
        AnsiConsole.MarkupLine("[grey]  Sin ventas completadas registradas aún.[/]");
    }

    AnsiConsole.MarkupLine("\n[bold]🏆 Top 5 productos más vendidos:[/]\n");
    var top5 = await db.LineasVenta
        .Include(l => l.Producto)
        .GroupBy(l => new { l.ProductoId, l.Producto.Nombre })
        .Select(g => new { g.Key.Nombre, TotalVendido = g.Sum(l => l.Cantidad) })
        .OrderByDescending(x => x.TotalVendido)
        .Take(5)
        .ToListAsync();

    if (top5.Any())
    {
        var rank = 1;
        foreach (var p in top5)
            AnsiConsole.MarkupLine(
                $"  [bold]{rank++}.[/] {Markup.Escape(p.Nombre)} — [yellow]{p.TotalVendido} unidades[/]");
    }
    else
    {
        AnsiConsole.MarkupLine("[grey]  Sin líneas de venta registradas aún.[/]");
    }

    AnsiConsole.MarkupLine("\n[grey]ENTER para continuar...[/]");
    Console.ReadLine();
}

// ─────────────────────────────────────────────────────────────────────────────
// Monitor del Bus
// ─────────────────────────────────────────────────────────────────────────────
static Task MostrarBusMonitorAsync()
{
    AnsiConsole.Clear();
    AnsiConsole.Write(new FigletText("Bus Monitor").Color(Color.Orange1));

    const string rabbitUrl = "amqps://gaqafkho:***@woodpecker.rmq.cloudamqp.com/gaqafkho";

    AnsiConsole.Write(new Panel(
        $"[bold]Transport:[/] [cyan]RabbitMQ (CloudAMQP)[/]\n" +
        $"[bold]Host:[/]      [yellow]woodpecker.rmq.cloudamqp.com[/]\n" +
        $"[bold]VHost:[/]     gaqafkho\n" +
        $"[bold]URL:[/]       [grey]{rabbitUrl}[/]\n" +
        $"[bold]Queues:[/]    Quorum (Alta Disponibilidad)\n\n" +
        $"[bold underline]Endpoint Core.API:[/]\n" +
        $"  [green]►[/] Suscrito a:  [yellow]AplicarTransaccionesOfflineCommand[/]\n" +
        $"  [blue]◄[/] Publica:      [yellow]InventarioActualizadoEvent[/]\n" +
        $"  [blue]◄[/] Publica:      [yellow]OrdenCambioEstadoEvent[/]\n" +
        $"  [blue]◄[/] Publica:      [yellow]VentaAplicadaEnCoreEvent[/]\n\n" +
        $"[bold underline]Endpoint IntegrationApp (P3):[/]\n" +
        $"  [green]►[/] Suscrito a:  [yellow]InventarioActualizadoEvent[/]\n" +
        $"  [green]►[/] Suscrito a:  [yellow]OrdenCambioEstadoEvent[/]\n" +
        $"  [green]►[/] Suscrito a:  [yellow]VentaAplicadaEnCoreEvent[/]\n" +
        $"  [blue]◄[/] Envía:        [yellow]AplicarTransaccionesOfflineCommand → Core.API[/]")
    .Header("[orange1]🔌 Monitor del Bus de Mensajes — RabbitMQ[/]")
    .Border(BoxBorder.Rounded));

    AnsiConsole.MarkupLine("\n[grey]💡 Consola de administración: https://woodpecker.rmq.cloudamqp.com[/]");
    AnsiConsole.MarkupLine("\n[grey]ENTER para continuar...[/]");
    Console.ReadLine();
    return Task.CompletedTask;
}

// ─────────────────────────────────────────────────────────────────────────────
// Usuarios & Sistema  (solo Administrador)
// ─────────────────────────────────────────────────────────────────────────────
static async Task MostrarUsuariosAsync(IAuthService authSvc, ConsoleSession session)
{
    AnsiConsole.Clear();
    AnsiConsole.Write(new FigletText("Usuarios").Color(Color.Magenta1));

    var opciones = new List<string> { "📋 Listar usuarios" };
    if (PermisoConsola.PuedeGestionarUsuarios(session.Rol))
    {
        opciones.Add("➕ Crear usuario");
        opciones.Add("✏️  Editar usuario");
    }
    opciones.Add("🔑 Cambiar mi contraseña");
    opciones.Add("← Volver");

    var opcion = AnsiConsole.Prompt(
        new SelectionPrompt<string>()
            .Title("[magenta1]■ Gestión de usuarios[/]")
            .AddChoices(opciones));

    if (opcion == "← Volver") return;

    if (opcion == "📋 Listar usuarios")
    {
        var usuarios = await authSvc.GetUsuariosAsync();
        var table = new Table().Border(TableBorder.Rounded)
            .AddColumn("[bold]ID[/]").AddColumn("[bold]Username[/]")
            .AddColumn("[bold]Nombre[/]").AddColumn("[bold]Rol[/]")
            .AddColumn("[bold]Sucursal[/]");

        foreach (var u in usuarios)
            table.AddRow(
                u.Id.ToString(),
                Markup.Escape(u.Username),
                Markup.Escape($"{u.Nombre} {u.Apellido}"),
                $"[{RolColor(u.Rol)} bold]{Markup.Escape(u.Rol)}[/]",
                Markup.Escape(u.NombreSucursal));

        AnsiConsole.Write(table);
    }
    else if (opcion == "➕ Crear usuario")
    {
        var username  = AnsiConsole.Ask<string>("Username:");
        var password  = AnsiConsole.Prompt(new TextPrompt<string>("Password:").Secret());
        var nombre    = AnsiConsole.Ask<string>("Nombre:");
        var apellido  = AnsiConsole.Ask<string>("Apellido:");
        var email     = AnsiConsole.Ask<string>("Email:");
        var rol       = AnsiConsole.Prompt(
            new SelectionPrompt<string>().Title("Rol:")
                .AddChoices("Administrador", "Cajero", "Vendedor", "Almacenista", "Cliente"));

        try
        {
            var req = new Core.API.DTOs.Requests.CrearUsuarioRequest(
                username, password, nombre, apellido, email, rol, 1);
            var u = await authSvc.CrearUsuarioAsync(req);
            AnsiConsole.MarkupLine(
                $"[green]✅ Usuario creado: #{u!.Id} — {Markup.Escape(u.Username)} " +
                $"[[{Markup.Escape(u.Rol)}]][/]");
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[red]❌ {Markup.Escape(ex.Message)}[/]");
        }
    }
    else if (opcion == "✏️  Editar usuario")
    {
        // Mostrar lista completa para facilitar la selección por ID
        var todos = (await authSvc.GetUsuariosAsync()).ToList();
        var tablaEditar = new Table().Border(TableBorder.Rounded)
            .AddColumn("[bold]ID[/]").AddColumn("[bold]Username[/]")
            .AddColumn("[bold]Nombre[/]").AddColumn("[bold]Rol[/]")
            .AddColumn("[bold]Sucursal[/]").AddColumn("[bold]Activo[/]");

        foreach (var u in todos)
            tablaEditar.AddRow(
                u.Id.ToString(),
                Markup.Escape(u.Username),
                Markup.Escape($"{u.Nombre} {u.Apellido}"),
                $"[{RolColor(u.Rol)} bold]{Markup.Escape(u.Rol)}[/]",
                Markup.Escape(u.NombreSucursal),
                u.EsActivo ? "[green]●[/]" : "[red]●[/]");

        AnsiConsole.Write(tablaEditar);
        AnsiConsole.WriteLine();

        var id = AnsiConsole.Ask<int>("ID del usuario a editar:");
        var destino = todos.FirstOrDefault(u => u.Id == id);

        if (destino is null)
        {
            AnsiConsole.MarkupLine($"[red]❌ No se encontró un usuario con ID {id}.[/]");
        }
        else
        {
            AnsiConsole.MarkupLine(
                $"\n[bold]Editando:[/] #{destino.Id} — [cyan]{Markup.Escape(destino.Username)}[/]  " +
                $"[grey](deja en blanco para mantener el valor actual)[/]\n");

            // Campos con valor actual visible — el admin pulsa Enter para conservarlo
            var nombre = AnsiConsole.Prompt(
                new TextPrompt<string>($"Nombre [grey]({Markup.Escape(destino.Nombre)})[/]:")
                    .AllowEmpty());
            if (string.IsNullOrWhiteSpace(nombre)) nombre = destino.Nombre;

            var apellido = AnsiConsole.Prompt(
                new TextPrompt<string>($"Apellido [grey]({Markup.Escape(destino.Apellido)})[/]:")
                    .AllowEmpty());
            if (string.IsNullOrWhiteSpace(apellido)) apellido = destino.Apellido;

            var email = AnsiConsole.Prompt(
                new TextPrompt<string>($"Email [grey]({Markup.Escape(destino.Email)})[/]:")
                    .AllowEmpty());
            if (string.IsNullOrWhiteSpace(email)) email = destino.Email;

            var rol = AnsiConsole.Prompt(
                new SelectionPrompt<string>()
                    .Title($"Rol [grey](actual: {Markup.Escape(destino.Rol)})[/]:")
                    .AddChoices("Administrador", "Cajero", "Vendedor", "Almacenista", "Cliente"));

            var esActivo = AnsiConsole.Confirm(
                $"¿Usuario activo? [grey](actual: {(destino.EsActivo ? "Sí" : "No")})[/]",
                destino.EsActivo);

            try
            {
                var req = new Core.API.DTOs.Requests.ActualizarUsuarioRequest(
                    nombre, apellido, email, rol, destino.SucursalId, esActivo);
                var actualizado = await authSvc.ActualizarUsuarioAsync(id, req);
                AnsiConsole.MarkupLine(
                    $"[green]✅ Usuario actualizado: #{actualizado!.Id} — " +
                    $"{Markup.Escape(actualizado.Username)} " +
                    $"[[{Markup.Escape(actualizado.Rol)}]] " +
                    $"{(actualizado.EsActivo ? "[green]Activo[/]" : "[red]Inactivo[/]")}[/]");
            }
            catch (Exception ex)
            {
                AnsiConsole.MarkupLine($"[red]❌ {Markup.Escape(ex.Message)}[/]");
            }
        }
    }
    else if (opcion == "🔑 Cambiar mi contraseña")
    {
        var actual = AnsiConsole.Prompt(
            new TextPrompt<string>("Contraseña actual:").Secret());
        var nueva = AnsiConsole.Prompt(
            new TextPrompt<string>("Nueva contraseña:")
                .Secret()
                .Validate(p => p.Length >= 6
                    ? ValidationResult.Success()
                    : ValidationResult.Error("[red]Mínimo 6 caracteres.[/]")));
        var confirmar = AnsiConsole.Prompt(
            new TextPrompt<string>("Confirmar nueva contraseña:").Secret());

        if (nueva != confirmar)
        {
            AnsiConsole.MarkupLine("[red]❌ Las contraseñas no coinciden.[/]");
        }
        else
        {
            var ok = await authSvc.CambiarPasswordAsync(session.UsuarioId, actual, nueva);
            AnsiConsole.MarkupLine(ok
                ? "[green]✅ Contraseña actualizada correctamente.[/]"
                : "[red]❌ Contraseña actual incorrecta.[/]");
        }
    }

    AnsiConsole.MarkupLine("\n[grey]ENTER para continuar...[/]");
    Console.ReadLine();
}

static string RolColor(string rol) => rol switch
{
    "Administrador" => "red",
    "Cajero"        => "blue",
    "Vendedor"      => "green",
    "Almacenista"   => "yellow3",
    "Cliente"       => "aqua",
    _               => "grey"
};

// ─────────────────────────────────────────────────────────────────────────────
// Stub NoOp para IMessageSession (la consola no inicia el bus)
// ─────────────────────────────────────────────────────────────────────────────
public class NoOpMessageSession : IMessageSession
{
    public Task Send(object message, SendOptions options, CancellationToken cancellationToken = default)
        => Task.CompletedTask;
    public Task Send<T>(Action<T> messageConstructor, SendOptions options, CancellationToken cancellationToken = default)
        => Task.CompletedTask;
    public Task Publish(object message, PublishOptions options, CancellationToken cancellationToken = default)
        => Task.CompletedTask;
    public Task Publish<T>(Action<T> messageConstructor, PublishOptions options, CancellationToken cancellationToken = default)
        => Task.CompletedTask;
    public Task Subscribe(Type eventType, SubscribeOptions options, CancellationToken cancellationToken = default)
        => Task.CompletedTask;
    public Task Unsubscribe(Type eventType, UnsubscribeOptions options, CancellationToken cancellationToken = default)
        => Task.CompletedTask;
}
