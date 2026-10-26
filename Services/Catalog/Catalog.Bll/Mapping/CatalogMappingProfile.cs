using AutoMapper;
using Catalog.Bll.Contracts;
using Catalog.Domain;

namespace Catalog.Bll.Mapping;

public sealed class CatalogMappingProfile : Profile
{
    public CatalogMappingProfile()
    {
        CreateMap<Product, ProductDto>()
            .ForMember(destination => destination.RowVersion,
                options => options.MapFrom(source => Convert.ToBase64String(source.RowVersion)))
            .ForMember(destination => destination.GameSystemName,
                options => options.MapFrom(source => source.GameSystem == null ? null : source.GameSystem.Name));
    }
}
