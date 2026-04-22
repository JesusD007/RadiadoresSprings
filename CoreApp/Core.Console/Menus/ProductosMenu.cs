using Core.API.DTOs.Requests;
using Core.API.Services;
using Core.ConsoleUI.Auth;
using Spectre.Console;

namespace Core.ConsoleUI.Menus;

public static class ProductosMenu
{
    public static async Task ShowAsync(
        IProductoService svc,
        ICategoriaService catSvc,
        ConsoleSession session)
    {
        var puedeEscribir = PermisoConsola.PuedeEscribirProductos(session.Rol);

        while (true)
        {
            AnsiConsole.Clear();
            AnsiConsole.Write(new FigletText("Productos").Color(Color.Cyan1));

            var opciones = new List<string>
            {
                "📋 Listar productos",
                "🔍 Buscar por código",
                "⚠️  Stock bajo"
            };

            if (puedeEscribir)
            {
                opciones.Add("➕ Crear producto");
                opciones.Add("✏️  Actualizar producto");
                opciones.Add("📦 Ajustar stock");
            }

            opciones.Add("← Volver al menú principal");

            var opcion = AnsiConsole.Prompt(
                new SelectionPrompt<string>()
                    .Title("[cyan]■ Módulo de Productos[/]")
                    .AddChoices(opciones));

            switch (opcion)
            {
                case "📋 Listar productos":
                    await ListarProductosAsync(svc);
                    break;
                case "🔍 Buscar por código":
                    await BuscarPorCodigoAsync(svc);
                    break;
                case "⚠️  Stock bajo":
                    await StockBajoAsync(svc);
                    break;
                case "➕ Crear producto":
                    await CrearProductoAsync(svc, catSvc);
                    break;
                case "✏️  Actualizar producto":
                    await ActualizarProductoAsync(svc, catSvc);
                    break;
                case "📦 Ajustar stock":
                    await AjustarStockAsync(svc);
                    break;
                case "← Volver al menú principal":
                    return;
            }
        }
    }

    private static async Task ListarProductosAsync(IProductoService svc)
    {
        var result = await svc.GetPagedAsync(1, 100, null, null);

        var table = new Table()
            .Border(TableBorder.Rounded)
            .AddColumn(new TableColumn("[bold]ID[/]").Centered())
            .AddColumn(new TableColumn("[bold]Código[/]"))
            .AddColumn(new TableColumn("[bold]Nombre[/]"))
            .AddColumn(new TableColumn("[bold]Precio[/]").RightAligned())
            .AddColumn(new TableColumn("[bold]Stock[/]").Centered())
            .AddColumn(new TableColumn("[bold]Mín.[/]").Centered())
            .AddColumn(new TableColumn("[bold]Categoría[/]"))
            .AddColumn(new TableColumn("[bold]Estado[/]").Centered());

        foreach (var p in result.Items)
        {
            var stockColor = p.StockBajo ? "[red]" : "[green]";
            table.AddRow(
                p.Id.ToString(),
                p.Codigo,
                p.Nombre.Length > 35 ? p.Nombre[..35] + "…" : p.Nombre,
                $"[yellow]${p.PrecioVigente:F2}[/]",
                $"{stockColor}{p.Stock}[/]",
                p.StockMinimo.ToString(),
                p.NombreCategoria,
                p.EsActivo ? "[green]●[/]" : "[red]●[/]"
            );
        }

        AnsiConsole.MarkupLine($"\n[bold]Productos:[/] {result.Total} registros\n");
        AnsiConsole.Write(table);
        AnsiConsole.MarkupLine("\n[grey]Presiona ENTER para continuar...[/]");
        Console.ReadLine();
    }

    private static async Task BuscarPorCodigoAsync(IProductoService svc)
    {
        var codigo = AnsiConsole.Ask<string>("🔍 [cyan]Código del producto:[/]");
        var p = await svc.GetByCodigoAsync(codigo);

        if (p is null)
        {
            AnsiConsole.MarkupLine("[red]Producto no encontrado.[/]");
        }
        else
        {
            AnsiConsole.Write(new Panel(
                $"[bold]Nombre:[/] {Markup.Escape(p.Nombre)}\n" +
                $"[bold]Descripción:[/] {Markup.Escape(p.Descripcion ?? "-")}\n" +
                $"[bold]Precio:[/] [yellow]${p.PrecioVigente:F2}[/] (Lista: ${p.Precio:F2})\n" +
                $"[bold]Stock:[/] {(p.StockBajo ? $"[red]{p.Stock}[/]" : $"[green]{p.Stock}[/]")} " +
                $"(Mínimo: {p.StockMinimo})\n" +
                $"[bold]Categoría:[/] {Markup.Escape(p.NombreCategoria)}\n" +
                $"[bold]Estado:[/] {(p.EsActivo ? "[green]Activo[/]" : "[red]Inactivo[/]")}")
            .Header($"[cyan]Producto #{p.Id} — {p.Codigo}[/]")
            .Border(BoxBorder.Rounded));
        }

        AnsiConsole.MarkupLine("\n[grey]Presiona ENTER para continuar...[/]");
        Console.ReadLine();
    }

    private static async Task StockBajoAsync(IProductoService svc)
    {
        var productos = (await svc.GetStockBajoAsync()).ToList();

        if (!productos.Any())
        {
            AnsiConsole.MarkupLine("[green]✅ Todos los productos tienen stock suficiente.[/]");
        }
        else
        {
            AnsiConsole.MarkupLine($"[red bold]⚠️  {productos.Count} productos con stock bajo:[/]\n");
            var table = new Table().Border(TableBorder.Simple)
                .AddColumn("Código").AddColumn("Nombre")
                .AddColumn(new TableColumn("Stock").Centered())
                .AddColumn(new TableColumn("Mínimo").Centered());

            foreach (var p in productos)
                table.AddRow(p.Codigo, Markup.Escape(p.Nombre),
                    $"[red]{p.Stock}[/]", p.StockMinimo.ToString());

            AnsiConsole.Write(table);
        }

        AnsiConsole.MarkupLine("\n[grey]Presiona ENTER para continuar...[/]");
        Console.ReadLine();
    }

    private static async Task CrearProductoAsync(IProductoService svc, ICategoriaService catSvc)
    {
        AnsiConsole.MarkupLine("[cyan bold]➕ Crear nuevo producto[/]\n");

        var categorias = (await catSvc.GetAllAsync()).ToList();
        var catNames = categorias.Select(c => c.Nombre).ToArray();

        var codigo    = AnsiConsole.Ask<string>("Código:");
        var nombre    = AnsiConsole.Ask<string>("Nombre:");
        var descripcion = AnsiConsole.Ask<string>("Descripción [grey](enter para omitir)[/]:");
        var precio    = AnsiConsole.Ask<decimal>("Precio:");
        var oferta    = AnsiConsole.Ask<decimal>("Precio oferta [grey](0 = sin oferta)[/]:");
        var stock     = AnsiConsole.Ask<int>("Stock inicial:");
        var stockMin  = AnsiConsole.Ask<int>("Stock mínimo:");
        var catNombre = AnsiConsole.Prompt(
            new SelectionPrompt<string>().Title("Categoría:").AddChoices(catNames));
        var categoriaId = categorias.First(c => c.Nombre == catNombre).Id;

        try
        {
            var req = new CrearProductoRequest(codigo, nombre,
                string.IsNullOrWhiteSpace(descripcion) ? null : descripcion,
                precio, oferta > 0 ? oferta : null, stock, stockMin, categoriaId);

            await AnsiConsole.Status().StartAsync("Guardando...", async _ =>
            {
                await svc.CrearAsync(req);
            });

            AnsiConsole.MarkupLine("[green]✅ Producto creado exitosamente.[/]");
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[red]❌ Error: {Markup.Escape(ex.Message)}[/]");
        }

        AnsiConsole.MarkupLine("\n[grey]Presiona ENTER para continuar...[/]");
        Console.ReadLine();
    }

    private static async Task ActualizarProductoAsync(IProductoService svc, ICategoriaService catSvc)
    {
        var id = AnsiConsole.Ask<int>("ID del producto a actualizar:");
        var producto = await svc.GetByIdAsync(id);

        if (producto is null)
        {
            AnsiConsole.MarkupLine("[red]Producto no encontrado.[/]");
            Console.ReadLine();
            return;
        }

        AnsiConsole.MarkupLine($"[cyan]Editando:[/] {Markup.Escape(producto.Nombre)}\n" +
                               "[grey](ENTER para mantener valor actual)[/]\n");

        var nombre   = AnsiConsole.Ask("Nombre:", producto.Nombre);
        var precio   = AnsiConsole.Ask("Precio:", producto.Precio);
        var oferta   = AnsiConsole.Ask("Precio oferta:", producto.PrecioOferta ?? 0m);
        var stockMin = AnsiConsole.Ask("Stock mínimo:", producto.StockMinimo);
        var categorias = (await catSvc.GetAllAsync()).ToList();
        var catNombre = AnsiConsole.Prompt(new SelectionPrompt<string>()
            .Title("Categoría:")
            .AddChoices(categorias.Select(c => c.Nombre)));
        var categoriaId = categorias.First(c => c.Nombre == catNombre).Id;

        var req = new ActualizarProductoRequest(nombre, producto.Descripcion,
            precio, oferta > 0 ? oferta : null, stockMin, categoriaId, producto.EsActivo);

        await svc.ActualizarAsync(id, req);
        AnsiConsole.MarkupLine("[green]✅ Producto actualizado.[/]");
        Console.ReadLine();
    }

    private static async Task AjustarStockAsync(IProductoService svc)
    {
        var id       = AnsiConsole.Ask<int>("ID del producto:");
        var cantidad = AnsiConsole.Ask<int>("Cantidad [grey](negativo para reducir)[/]:");
        var motivo   = AnsiConsole.Ask<string>("Motivo del ajuste:");

        try
        {
            await svc.AjustarStockAsync(id, cantidad, motivo);
            AnsiConsole.MarkupLine($"[green]✅ Stock ajustado: {cantidad:+#;-#;0} unidades.[/]");
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[red]❌ {Markup.Escape(ex.Message)}[/]");
        }

        Console.ReadLine();
    }
}
