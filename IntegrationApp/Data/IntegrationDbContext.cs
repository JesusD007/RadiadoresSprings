using IntegrationApp.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace IntegrationApp.Data;

public class IntegrationDbContext : DbContext
{
    public IntegrationDbContext(DbContextOptions<IntegrationDbContext> options) : base(options) { }

    // ── Mirrors (réplicas de Core — fuente de verdad cuando Core está activo) ─
    public DbSet<ProductoMirror> ProductosMirror => Set<ProductoMirror>();
    public DbSet<UsuarioMirror> UsuariosMirror => Set<UsuarioMirror>();
    public DbSet<ClienteMirror> ClientesMirror => Set<ClienteMirror>();
    public DbSet<SucursalMirror> SucursalesMirror => Set<SucursalMirror>();
    public DbSet<CategoriaMirror> CategoriasMirror => Set<CategoriaMirror>();
    public DbSet<CajaMirror> CajasMirror => Set<CajaMirror>();
    public DbSet<CuentaCobrarMirror> CuentasCobrarMirror => Set<CuentaCobrarMirror>();

    // ── Operaciones offline ───────────────────────────────────────────────────
    public DbSet<VentaOfflinePendiente> VentasOfflinePendientes => Set<VentaOfflinePendiente>();
    public DbSet<SesionCajaMirror> SesionesCajaMirror => Set<SesionCajaMirror>();
    public DbSet<OperacionPendiente> OperacionesPendientes => Set<OperacionPendiente>();

    // ── Auditoría ─────────────────────────────────────────────────────────────
    public DbSet<IntegrationLogEntry> IntegrationLogs => Set<IntegrationLogEntry>();
    public DbSet<IdempotencyLog> IdempotencyLogs => Set<IdempotencyLog>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // ── ProductoMirror ────────────────────────────────────────────────────
        modelBuilder.Entity<ProductoMirror>(e =>
        {
            e.ToTable("ProductoMirror");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).ValueGeneratedNever(); // PK viene del Core
            e.Property(x => x.Codigo).HasMaxLength(50).IsRequired();
            e.Property(x => x.Nombre).HasMaxLength(200).IsRequired();
            e.Property(x => x.Precio).HasPrecision(18, 2);
            e.Property(x => x.PrecioOferta).HasPrecision(18, 2);
            e.Property(x => x.Categoria).HasMaxLength(100);
            e.Property(x => x.Descripcion).HasMaxLength(500);
        });

        // ── UsuarioMirror ─────────────────────────────────────────────────────
        modelBuilder.Entity<UsuarioMirror>(e =>
        {
            e.ToTable("UsuarioMirror");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).ValueGeneratedNever(); // PK viene del Core
            e.Property(x => x.Username).HasMaxLength(100).IsRequired();
            e.Property(x => x.PasswordHash).HasMaxLength(500).IsRequired();
            e.Property(x => x.Rol).HasMaxLength(50).IsRequired();
            e.Property(x => x.Nombre).HasMaxLength(100).IsRequired();
            e.Property(x => x.Apellido).HasMaxLength(100);
            e.Property(x => x.Email).HasMaxLength(200);
            e.HasIndex(x => x.Username).IsUnique();
        });

        // ── ClienteMirror ─────────────────────────────────────────────────────
        modelBuilder.Entity<ClienteMirror>(e =>
        {
            e.ToTable("ClienteMirror");
            e.HasKey(x => x.LocalId);           // PK local (Guid)
            e.Property(x => x.LocalId).ValueGeneratedNever();
            e.Property(x => x.CoreId);          // nullable, se llena al sincronizar
            e.Property(x => x.Nombre).HasMaxLength(150).IsRequired();
            e.Property(x => x.Apellido).HasMaxLength(150);
            e.Property(x => x.Email).HasMaxLength(200);
            e.Property(x => x.Telefono).HasMaxLength(30);
            e.Property(x => x.RFC).HasMaxLength(20);
            e.Property(x => x.Tipo).HasMaxLength(30).HasDefaultValue("Regular");
            e.Property(x => x.LimiteCredito).HasPrecision(18, 2);
            e.Property(x => x.SaldoPendiente).HasPrecision(18, 2);
            e.HasIndex(x => x.CoreId);
        });

        // ── SucursalMirror ────────────────────────────────────────────────────
        modelBuilder.Entity<SucursalMirror>(e =>
        {
            e.ToTable("SucursalMirror");
            e.HasKey(x => x.CoreId);
            e.Property(x => x.CoreId).ValueGeneratedNever();
            e.Property(x => x.Nombre).HasMaxLength(150).IsRequired();
            e.Property(x => x.Direccion).HasMaxLength(300);
            e.Property(x => x.Telefono).HasMaxLength(30);
        });

        // ── CategoriaMirror ───────────────────────────────────────────────────
        modelBuilder.Entity<CategoriaMirror>(e =>
        {
            e.ToTable("CategoriaMirror");
            e.HasKey(x => x.CoreId);
            e.Property(x => x.CoreId).ValueGeneratedNever();
            e.Property(x => x.Nombre).HasMaxLength(100).IsRequired();
            e.Property(x => x.Descripcion).HasMaxLength(300);
        });

        // ── CajaMirror ────────────────────────────────────────────────────────
        modelBuilder.Entity<CajaMirror>(e =>
        {
            e.ToTable("CajaMirror");
            e.HasKey(x => x.CoreId);
            e.Property(x => x.CoreId).ValueGeneratedNever();
            e.Property(x => x.Nombre).HasMaxLength(100).IsRequired();
            e.HasIndex(x => x.SucursalId);
        });

        // ── CuentaCobrarMirror ────────────────────────────────────────────────
        modelBuilder.Entity<CuentaCobrarMirror>(e =>
        {
            e.ToTable("CuentaCobrarMirror");
            e.HasKey(x => x.CoreId);
            e.Property(x => x.CoreId).ValueGeneratedNever();
            e.Property(x => x.MontoTotal).HasPrecision(18, 2);
            e.Property(x => x.SaldoPendiente).HasPrecision(18, 2);
            e.Property(x => x.Estado).HasMaxLength(30).IsRequired();
            e.HasIndex(x => x.ClienteId);
        });

        // ── SesionCajaMirror ──────────────────────────────────────────────────
        modelBuilder.Entity<SesionCajaMirror>(e =>
        {
            e.ToTable("SesionCajaMirror");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).ValueGeneratedOnAdd();
            e.Property(x => x.NombreCaja).HasMaxLength(100);
            e.Property(x => x.NombreUsuario).HasMaxLength(200);
            e.Property(x => x.MontoApertura).HasPrecision(18, 2);
            e.Property(x => x.MontoCierre).HasPrecision(18, 2);
            e.Property(x => x.Estado).HasMaxLength(20).HasDefaultValue("Abierta");
            e.Property(x => x.EstadoSync).HasMaxLength(20).HasDefaultValue("Pendiente");
            e.Property(x => x.Observaciones).HasMaxLength(500);
            e.HasIndex(x => x.IdLocal).IsUnique();
            e.HasIndex(x => x.EstadoSync);
        });

        // ── OperacionPendiente ────────────────────────────────────────────────
        modelBuilder.Entity<OperacionPendiente>(e =>
        {
            e.ToTable("OperacionPendiente");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).ValueGeneratedOnAdd();
            e.Property(x => x.TipoEntidad).HasMaxLength(50).IsRequired();
            e.Property(x => x.TipoOperacion).HasMaxLength(50).IsRequired();
            e.Property(x => x.EndpointCore).HasMaxLength(300).IsRequired();
            e.Property(x => x.MetodoHttp).HasMaxLength(10).HasDefaultValue("POST");
            e.Property(x => x.PayloadJson).HasColumnType("text").IsRequired();
            e.Property(x => x.IdLocalTemporal).HasMaxLength(100);
            e.Property(x => x.UsuarioId).HasMaxLength(100);
            e.Property(x => x.Estado).HasMaxLength(20).HasDefaultValue("Pendiente");
            e.Property(x => x.MotivoRechazo).HasMaxLength(1000);
            e.Property(x => x.RespuestaCore).HasColumnType("text");
            e.HasIndex(x => x.Estado);
            e.HasIndex(x => x.FechaCreacion);
            e.HasIndex(x => x.IdempotencyKey).IsUnique();
        });

        // ── IntegrationLogEntry ───────────────────────────────────────────────
        modelBuilder.Entity<IntegrationLogEntry>(e =>
        {
            e.ToTable("IntegrationLog");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).ValueGeneratedOnAdd();
            e.Property(x => x.Endpoint).HasMaxLength(500).IsRequired();
            e.Property(x => x.Direccion).HasMaxLength(3).IsRequired();
            e.Property(x => x.CorrelationId).HasMaxLength(50).IsRequired();
            e.Property(x => x.UserId).HasMaxLength(100);
            e.Property(x => x.Layer).HasMaxLength(20).HasDefaultValue("Integracion");
            e.Property(x => x.RequestJson).HasColumnType("text");
            e.Property(x => x.ResponseJson).HasColumnType("text");
            e.HasIndex(x => x.CorrelationId);
            e.HasIndex(x => x.Fecha);
        });

        // ── IdempotencyLog ────────────────────────────────────────────────────
        modelBuilder.Entity<IdempotencyLog>(e =>
        {
            e.ToTable("IdempotencyLog");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).ValueGeneratedOnAdd();
            e.Property(x => x.Estado).HasMaxLength(20).IsRequired();
            e.Property(x => x.MotivoRechazo).HasMaxLength(500);
            e.HasIndex(x => x.IdTransaccionLocal).IsUnique();
        });

        // ── VentaOfflinePendiente ─────────────────────────────────────────────
        modelBuilder.Entity<VentaOfflinePendiente>(e =>
        {
            e.ToTable("VentaOfflinePendiente");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).ValueGeneratedOnAdd();
            e.Property(x => x.IdTransaccionLocal).IsRequired();
            e.Property(x => x.CajeroId).HasMaxLength(100).IsRequired();
            e.Property(x => x.SucursalId).HasMaxLength(50).IsRequired();
            e.Property(x => x.MetodoPago).HasMaxLength(30).IsRequired();
            e.Property(x => x.MontoTotal).HasPrecision(18, 2);
            e.Property(x => x.MontoRecibido).HasPrecision(18, 2);
            e.Property(x => x.LineasJson).HasColumnType("text").IsRequired();
            e.Property(x => x.Estado).HasMaxLength(20).HasDefaultValue("Pendiente");
            e.HasIndex(x => x.IdTransaccionLocal).IsUnique();
            e.HasIndex(x => x.Estado);
        });
    }
}
