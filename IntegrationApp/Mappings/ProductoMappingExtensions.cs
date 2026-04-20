using IntegrationApp.Contracts.Responses.Productos;
using IntegrationApp.Domain.Entities;

namespace IntegrationApp.Mappings;

public static class ProductoMappingExtensions
{
    public static ProductoResumenDto ToResumenDto(this ProductoMirror source) =>
        new()
        {
            Id         = source.Id,
            Codigo     = source.Codigo,
            Nombre     = source.Nombre,
            Precio     = source.Precio,
            Stock      = source.Stock,
            Categoria  = source.Categoria ?? "Sin categoría",
            EsActivo   = source.EsActivo,
        };

    public static ProductoDetalleDto ToDetalleDto(this ProductoMirror source) =>
        new()
        {
            Id            = source.Id,
            Codigo        = source.Codigo,
            Nombre        = source.Nombre,
            Descripcion   = source.Descripcion,
            Precio        = source.Precio,
            PrecioOferta  = source.PrecioOferta,
            Stock         = source.Stock,
            StockMinimo   = source.StockMinimo,
            CategoriaId   = source.CategoriaId,
            Categoria     = source.Categoria ?? "Sin categoría",
            EsActivo      = source.EsActivo,
            UltimaSync    = new DateTimeOffset(source.UltimaSync, TimeSpan.Zero),
            FromMirror    = true,
        };
}
