using IntegrationApp.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace IntegrationApp.Data;

public class IntegrationDbContext : DbContext
{
    public IntegrationDbContext(DbContextOptions<IntegrationDbContext> options) : base(options) { }

    public DbSet<ProductoMirror> ProductosMirror => Set<ProductoMirror>();
    public DbSet<IntegrationLogEntry> IntegrationLogs => Set<IntegrationLogEntry>();
    public DbSet<IdempotencyLog> IdempotencyLogs => Set<IdempotencyLog>();
    public DbSet<VentaOfflinePendiente> VentasOfflinePendientes => Set<VentaOfflinePendiente>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // ProductoMirror
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

        // IntegrationLogEntry
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
            e.Property(x => x.RequestJson).HasColumnType("nvarchar(max)");
            e.Property(x => x.ResponseJson).HasColumnType("nvarchar(max)");
            e.HasIndex(x => x.CorrelationId);
            e.HasIndex(x => x.Fecha);
        });

        // IdempotencyLog
        modelBuilder.Entity<IdempotencyLog>(e =>
        {
            e.ToTable("IdempotencyLog");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).ValueGeneratedOnAdd();
            e.Property(x => x.Estado).HasMaxLength(20).IsRequired();
            e.Property(x => x.MotivoRechazo).HasMaxLength(500);
            e.HasIndex(x => x.IdTransaccionLocal).IsUnique();
        });

        // VentaOfflinePendiente
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
            e.Property(x => x.LineasJson).HasColumnType("nvarchar(max)").IsRequired();
            e.Property(x => x.Estado).HasMaxLength(20).HasDefaultValue("Pendiente");
            e.HasIndex(x => x.IdTransaccionLocal).IsUnique();
            e.HasIndex(x => x.Estado);
        });
    }
}
