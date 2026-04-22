using Core.API.DTOs.Requests;
using Core.API.Services;
using Core.ConsoleUI.Auth;
using Spectre.Console;

namespace Core.ConsoleUI.Menus;

public static class VentasMenu
{
    public static async Task ShowAsync(
        IVentaService ventaSvc,
        IProductoService productoSvc,
        IClienteService clienteSvc,
        ConsoleSession session)
    {
        var puedeCancelar = PermisoConsola.PuedeCancelarVentas(session.Rol);

        while (true)
        {
            AnsiConsole.Clear();
            AnsiConsole.Write(new FigletText("Ventas").Color(Color.Yellow));

            var opciones = new List<string>
            {
                "📋 Listar ventas recientes",
                "🔍 Buscar por número de factura",
                "🧾 Registrar venta"
            };

            if (puedeCancelar)
                opciones.Add("❌ Cancelar venta");

            opciones.Add("← Volver");

            var opcion = AnsiConsole.Prompt(
                new SelectionPrompt<string>()
                    .Title("[yellow]■ Módulo de Ventas[/]")
                    .AddChoices(opciones));

            switch (opcion)
            {
                case "📋 Listar ventas recientes":
                    await ListarVentasAsync(ventaSvc);
                    break;
                case "🔍 Buscar por número de factura":
                    await BuscarFacturaAsync(ventaSvc);
                    break;
                case "🧾 Registrar venta":
                    await RegistrarVentaAsync(ventaSvc, productoSvc, clienteSvc, session.UsuarioId);
                    break;
                case "❌ Cancelar venta":
                    await CancelarVentaAsync(ventaSvc);
                    break;
                case "← Volver":
                    return;
            }
        }
    }

    private static async Task ListarVentasAsync(IVentaService svc)
    {
        var desde  = DateTime.UtcNow.AddDays(-7);
        var result = await svc.GetPagedAsync(1, 50, desde, null);

        var table = new Table().Border(TableBorder.Rounded)
            .AddColumn("[bold]ID[/]").AddColumn("[bold]Factura[/]")
            .AddColumn("[bold]Fecha[/]").AddColumn("[bold]Cliente[/]")
            .AddColumn(new TableColumn("[bold]Total[/]").RightAligned())
            .AddColumn("[bold]Método[/]").AddColumn("[bold]Estado[/]")
            .AddColumn("[bold]Offline[/]");

        foreach (var v in result.Items)
        {
            var estadoColor = v.Estado == "Completada" ? "[green]" : "[red]";
            table.AddRow(
                v.Id.ToString(), v.NumeroFactura,
                v.Fecha.ToLocalTime().ToString("dd/MM HH:mm"),
                v.NombreCliente ?? "[grey]Anónimo[/]",
                $"[yellow]${v.Total:F2}[/]",
                v.MetodoPago,
                $"{estadoColor}{v.Estado}[/]",
                v.EsOffline ? "[orange1]SÍ[/]" : "[grey]No[/]"
            );
        }

        AnsiConsole.MarkupLine($"\n[bold]Ventas últimos 7 días:[/] {result.Total} registros\n");
        AnsiConsole.Write(table);
        AnsiConsole.MarkupLine("\n[grey]ENTER para continuar...[/]");
        Console.ReadLine();
    }

    private static async Task BuscarFacturaAsync(IVentaService svc)
    {
        var numero = AnsiConsole.Ask<string>("🔍 [yellow]Número de factura:[/]");
        var v = await svc.GetByFacturaAsync(numero);

        if (v is null)
        {
            AnsiConsole.MarkupLine("[red]Factura no encontrada.[/]");
        }
        else
        {
            var lineas = string.Join("\n", v.Lineas.Select(l =>
                $"  • {Markup.Escape(l.NombreProducto)} × {l.Cantidad} @ ${l.PrecioUnitario:F2} = ${l.Subtotal:F2}"));

            AnsiConsole.Write(new Panel(
                $"[bold]Fecha:[/] {v.Fecha.ToLocalTime():dd/MM/yyyy HH:mm}\n" +
                $"[bold]Cliente:[/] {Markup.Escape(v.NombreCliente ?? "Anónimo")}\n" +
                $"[bold]Cajero:[/] {Markup.Escape(v.NombreUsuario)}\n" +
                $"[bold]Subtotal:[/] ${v.Subtotal:F2}  [bold]IVA:[/] ${v.IVA:F2}  " +
                $"[bold]Total:[/] [yellow bold]${v.Total:F2}[/]\n" +
                $"[bold]Método pago:[/] {v.MetodoPago}  [bold]Estado:[/] {v.Estado}\n\n" +
                lineas)
            .Header($"[yellow]Factura {v.NumeroFactura}[/]")
            .Border(BoxBorder.Rounded));
        }

        Console.ReadLine();
    }

    private static async Task RegistrarVentaAsync(
        IVentaService ventaSvc,
        IProductoService productoSvc,
        IClienteService clienteSvc,
        int usuarioId)
    {
        AnsiConsole.MarkupLine("[yellow bold]🧾 Registrar nueva venta[/]\n");

        var lineas = new List<LineaVentaRequest>();
        decimal totalEstimado = 0;

        while (true)
        {
            var codigo = AnsiConsole.Ask<string>("Código de producto [grey](ENTER para terminar)[/]:");
            if (string.IsNullOrWhiteSpace(codigo)) break;

            var producto = await productoSvc.GetByCodigoAsync(codigo);
            if (producto is null)
            {
                AnsiConsole.MarkupLine("[red]Producto no encontrado.[/]");
                continue;
            }

            AnsiConsole.MarkupLine(
                $"[cyan]{Markup.Escape(producto.Nombre)}[/] — " +
                $"${producto.PrecioVigente:F2} | Stock: {producto.Stock}");

            var cantidad = AnsiConsole.Ask<int>("Cantidad:");

            if (cantidad > producto.Stock)
            {
                AnsiConsole.MarkupLine($"[red]⚠️  Stock insuficiente ({producto.Stock} disponibles).[/]");
                continue;
            }

            lineas.Add(new LineaVentaRequest(producto.Id, cantidad));
            totalEstimado += producto.PrecioVigente * cantidad;
            AnsiConsole.MarkupLine($"[green]✅ {cantidad} × {Markup.Escape(producto.Nombre)} agregado(s)[/]");
        }

        if (!lineas.Any())
        {
            AnsiConsole.MarkupLine("[red]No se agregaron productos. Venta cancelada.[/]");
            Console.ReadLine();
            return;
        }

        AnsiConsole.MarkupLine($"\n[bold]Subtotal estimado:[/] [yellow]${totalEstimado:F2}[/] + IVA 16%");

        var metodo = AnsiConsole.Prompt(
            new SelectionPrompt<string>().Title("Método de pago:")
                .AddChoices("Efectivo", "TarjetaCredito", "TarjetaDebito",
                            "Transferencia", "PayPal", "Credito"));

        int? clienteId = null;
        if (AnsiConsole.Confirm("¿Asociar a un cliente?"))
        {
            var cId = AnsiConsole.Ask<int>("ID del cliente:");
            var cliente = await clienteSvc.GetByIdAsync(cId);
            if (cliente is not null)
            {
                clienteId = cId;
                AnsiConsole.MarkupLine($"[green]Cliente: {Markup.Escape(cliente.NombreCompleto)}[/]");
            }
        }

        var descuento = AnsiConsole.Ask<decimal>("Descuento global [$]:", 0m);

        if (!AnsiConsole.Confirm("¿Confirmar venta?")) return;

        try
        {
            var req = new CrearVentaRequest(
                SucursalId: 1, CajaId: 1, SesionCajaId: 1,
                ClienteId: clienteId, MetodoPago: metodo,
                Lineas: lineas, Descuento: descuento);

            Core.API.DTOs.Responses.VentaResponse? venta = null;
            await AnsiConsole.Status().StartAsync("Procesando venta...", async _ =>
            {
                venta = await ventaSvc.CrearAsync(req, usuarioId);
            });

            AnsiConsole.MarkupLine("[green bold]✅ Venta registrada exitosamente![/]");
            AnsiConsole.MarkupLine($"   Factura: [yellow]{venta!.NumeroFactura}[/]");
            AnsiConsole.MarkupLine($"   Total:   [yellow]${venta.Total:F2}[/]");
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[red]❌ Error: {Markup.Escape(ex.Message)}[/]");
        }

        Console.ReadLine();
    }

    private static async Task CancelarVentaAsync(IVentaService svc)
    {
        var id     = AnsiConsole.Ask<int>("ID de la venta a cancelar:");
        var motivo = AnsiConsole.Ask<string>("Motivo de cancelación:");

        if (!AnsiConsole.Confirm($"¿Confirmar cancelación de venta #{id}?")) return;

        var ok = await svc.CancelarAsync(id, motivo);
        AnsiConsole.MarkupLine(ok
            ? "[green]✅ Venta cancelada. Stock reintegrado.[/]"
            : "[red]❌ Venta no encontrada o ya cancelada.[/]");

        Console.ReadLine();
    }
}
