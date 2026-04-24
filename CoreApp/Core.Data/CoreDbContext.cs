using Core.Domain.Entities;
using Core.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Core.Data;

public class CoreDbContext(DbContextOptions<CoreDbContext> options) : DbContext(options)
{
    public DbSet<Sucursal> Sucursales => Set<Sucursal>();
    public DbSet<Usuario> Usuarios => Set<Usuario>();
    public DbSet<Categoria> Categorias => Set<Categoria>();
    public DbSet<Producto> Productos => Set<Producto>();
    public DbSet<Cliente> Clientes => Set<Cliente>();
    public DbSet<Caja> Cajas => Set<Caja>();
    public DbSet<SesionCaja> SesionesCaja => Set<SesionCaja>();
    public DbSet<Venta> Ventas => Set<Venta>();
    public DbSet<LineaVenta> LineasVenta => Set<LineaVenta>();
    public DbSet<Orden> Ordenes => Set<Orden>();
    public DbSet<LineaOrden> LineasOrden => Set<LineaOrden>();
    public DbSet<CuentaCobrar> CuentasCobrar => Set<CuentaCobrar>();
    public DbSet<Pago> Pagos => Set<Pago>();

    protected override void OnModelCreating(ModelBuilder mb)
    {
        base.OnModelCreating(mb);

        // ── Sucursal ──────────────────────────────────────────────────────────
        mb.Entity<Sucursal>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Nombre).HasMaxLength(150).IsRequired();
            e.Property(x => x.Direccion).HasMaxLength(300);
            e.Property(x => x.Telefono).HasMaxLength(30);
        });

        // ── Usuario ───────────────────────────────────────────────────────────
        mb.Entity<Usuario>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasIndex(x => x.Username).IsUnique();
            e.HasIndex(x => x.Email);
            e.Property(x => x.Username).HasMaxLength(80).IsRequired();
            e.Property(x => x.PasswordHash).HasMaxLength(256).IsRequired();
            e.Property(x => x.Nombre).HasMaxLength(100).IsRequired();
            e.Property(x => x.Apellido).HasMaxLength(100).IsRequired();
            e.Property(x => x.Email).HasMaxLength(200);
            e.Property(x => x.Rol).HasConversion<string>().HasMaxLength(30);
            e.HasOne(x => x.Sucursal).WithMany(s => s.Usuarios)
             .HasForeignKey(x => x.SucursalId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.Cliente).WithMany()
             .HasForeignKey(x => x.ClienteId).IsRequired(false).OnDelete(DeleteBehavior.SetNull);
        });

        // ── Categoria ─────────────────────────────────────────────────────────
        mb.Entity<Categoria>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Nombre).HasMaxLength(100).IsRequired();
            e.Property(x => x.Descripcion).HasMaxLength(500);
        });

        // ── Producto ──────────────────────────────────────────────────────────
        mb.Entity<Producto>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasIndex(x => x.Codigo).IsUnique();
            e.Property(x => x.Codigo).HasMaxLength(50).IsRequired();
            e.Property(x => x.Nombre).HasMaxLength(200).IsRequired();
            e.Property(x => x.Descripcion).HasMaxLength(1000);
            e.Property(x => x.Precio).HasColumnType("decimal(18,2)");
            e.Property(x => x.PrecioOferta).HasColumnType("decimal(18,2)");
            // TieneStockBajo() y PrecioVigente() son métodos — EF Core los ignora automáticamente
            e.HasOne(x => x.Categoria).WithMany(c => c.Productos)
             .HasForeignKey(x => x.CategoriaId).OnDelete(DeleteBehavior.Restrict);
        });

        // ── Cliente ───────────────────────────────────────────────────────────
        mb.Entity<Cliente>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasIndex(x => x.RFC);
            e.Property(x => x.Nombre).HasMaxLength(150).IsRequired();
            e.Property(x => x.Apellido).HasMaxLength(150);
            e.Property(x => x.Email).HasMaxLength(200);
            e.Property(x => x.RFC).HasMaxLength(20);
            e.Property(x => x.LimiteCredito).HasColumnType("decimal(18,2)");
            e.Property(x => x.SaldoPendiente).HasColumnType("decimal(18,2)");
            e.Property(x => x.Tipo).HasConversion<string>().HasMaxLength(20);
            // CreditoDisponible() y PuedeCreditarse() son métodos — EF Core los ignora automáticamente
        });

        // ── Caja ──────────────────────────────────────────────────────────────
        mb.Entity<Caja>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Numero).HasMaxLength(10).IsRequired();
            e.Property(x => x.Nombre).HasMaxLength(100).IsRequired();
            e.HasOne(x => x.Sucursal).WithMany(s => s.Cajas)
             .HasForeignKey(x => x.SucursalId).OnDelete(DeleteBehavior.Restrict);
        });

        // ── SesionCaja ────────────────────────────────────────────────────────
        mb.Entity<SesionCaja>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.MontoApertura).HasColumnType("decimal(18,2)");
            e.Property(x => x.MontoCierre).HasColumnType("decimal(18,2)");
            e.Property(x => x.MontoSistema).HasColumnType("decimal(18,2)");
            e.Property(x => x.Diferencia).HasColumnType("decimal(18,2)");
            e.Property(x => x.Estado).HasConversion<string>().HasMaxLength(20);
            e.HasOne(x => x.Caja).WithMany(c => c.Sesiones)
             .HasForeignKey(x => x.CajaId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.Usuario).WithMany(u => u.SesionesCaja)
             .HasForeignKey(x => x.UsuarioId).OnDelete(DeleteBehavior.Restrict);
        });

        // ── Venta ─────────────────────────────────────────────────────────────
        mb.Entity<Venta>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasIndex(x => x.NumeroFactura).IsUnique();
            e.HasIndex(x => x.IdTransaccionLocal);
            e.HasIndex(x => x.Fecha);
            e.Property(x => x.NumeroFactura).HasMaxLength(30).IsRequired();
            e.Property(x => x.Subtotal).HasColumnType("decimal(18,2)");
            e.Property(x => x.IVA).HasColumnType("decimal(18,2)");
            e.Property(x => x.Total).HasColumnType("decimal(18,2)");
            e.Property(x => x.Descuento).HasColumnType("decimal(18,2)");
            e.Property(x => x.MetodoPago).HasConversion<string>().HasMaxLength(30);
            e.Property(x => x.Estado).HasConversion<string>().HasMaxLength(20);
            e.Property(x => x.IdTransaccionLocal).HasMaxLength(50);
            e.HasOne(x => x.Cliente).WithMany(c => c.Ventas)
             .HasForeignKey(x => x.ClienteId).IsRequired(false).OnDelete(DeleteBehavior.SetNull);
            e.HasOne(x => x.SesionCaja).WithMany(s => s.Ventas)
             .HasForeignKey(x => x.SesionCajaId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.Usuario).WithMany()
             .HasForeignKey(x => x.UsuarioId).OnDelete(DeleteBehavior.Restrict);
        });

        // ── LineaVenta ────────────────────────────────────────────────────────
        mb.Entity<LineaVenta>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.PrecioUnitario).HasColumnType("decimal(18,2)");
            e.Property(x => x.Descuento).HasColumnType("decimal(18,2)");
            e.Ignore(x => x.Subtotal);
            e.HasOne(x => x.Venta).WithMany(v => v.Lineas)
             .HasForeignKey(x => x.VentaId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.Producto).WithMany(p => p.LineasVenta)
             .HasForeignKey(x => x.ProductoId).OnDelete(DeleteBehavior.Restrict);
        });

        // ── Orden ─────────────────────────────────────────────────────────────
        mb.Entity<Orden>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasIndex(x => x.NumeroOrden).IsUnique();
            e.Property(x => x.NumeroOrden).HasMaxLength(30).IsRequired();
            e.Property(x => x.TotalOrden).HasColumnType("decimal(18,2)");
            e.Property(x => x.Estado).HasConversion<string>().HasMaxLength(20);
            e.Property(x => x.MetodoPago).HasConversion<string>().HasMaxLength(30);
            e.Property(x => x.DireccionEnvio).HasMaxLength(500);
            e.HasOne(x => x.Cliente).WithMany(c => c.Ordenes)
             .HasForeignKey(x => x.ClienteId).OnDelete(DeleteBehavior.Restrict);
        });

        // ── LineaOrden ────────────────────────────────────────────────────────
        mb.Entity<LineaOrden>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.PrecioUnitario).HasColumnType("decimal(18,2)");
            e.Ignore(x => x.Subtotal);
            e.HasOne(x => x.Orden).WithMany(o => o.Lineas)
             .HasForeignKey(x => x.OrdenId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.Producto).WithMany(p => p.LineasOrden)
             .HasForeignKey(x => x.ProductoId).OnDelete(DeleteBehavior.Restrict);
        });

        // ── CuentaCobrar ──────────────────────────────────────────────────────
        mb.Entity<CuentaCobrar>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasIndex(x => x.NumeroFactura);
            e.Property(x => x.NumeroFactura).HasMaxLength(30).IsRequired();
            e.Property(x => x.MontoOriginal).HasColumnType("decimal(18,2)");
            e.Property(x => x.MontoPagado).HasColumnType("decimal(18,2)");
            e.Property(x => x.Estado).HasConversion<string>().HasMaxLength(20);
            e.Ignore(x => x.SaldoPendiente); // propiedad calculada, no se persiste
            // EstaVencida() es método — EF Core lo ignora automáticamente
            e.HasOne(x => x.Cliente).WithMany(c => c.CuentasCobrar)
             .HasForeignKey(x => x.ClienteId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.Venta).WithOne(v => v.CuentaCobrar)
             .HasForeignKey<CuentaCobrar>(x => x.VentaId).IsRequired(false).OnDelete(DeleteBehavior.SetNull);
        });

        // ── Pago ──────────────────────────────────────────────────────────────
        mb.Entity<Pago>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasIndex(x => x.Fecha);
            e.Property(x => x.Monto).HasColumnType("decimal(18,2)");
            e.Property(x => x.MetodoPago).HasConversion<string>().HasMaxLength(30);
            e.Property(x => x.Referencia).HasMaxLength(100);
            e.HasOne(x => x.Cliente).WithMany(c => c.Pagos)
             .HasForeignKey(x => x.ClienteId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.CuentaCobrar).WithMany(cc => cc.Pagos)
             .HasForeignKey(x => x.CuentaCobrarId).IsRequired(false).OnDelete(DeleteBehavior.SetNull);
        });

    }
}
