using AutoMapper;
using IntegrationApp.Contracts.Responses.Productos;
using IntegrationApp.Domain.Entities;

namespace IntegrationApp.Mappings;

public class AutoMapperProfile : Profile
{
    public AutoMapperProfile()
    {
        // ProductoMirror → ProductoResumenDto
        CreateMap<ProductoMirror, ProductoResumenDto>()
            .ForMember(d => d.Categoria, o => o.MapFrom(s => s.Categoria ?? "Sin categoría"));

        // ProductoMirror → ProductoDetalleDto
        CreateMap<ProductoMirror, ProductoDetalleDto>()
            .ForMember(d => d.Categoria, o => o.MapFrom(s => s.Categoria ?? "Sin categoría"))
            .ForMember(d => d.UltimaSync, o => o.MapFrom(s => new DateTimeOffset(s.UltimaSync, TimeSpan.Zero)))
            .ForMember(d => d.FromMirror, o => o.MapFrom(_ => true));
    }
}
