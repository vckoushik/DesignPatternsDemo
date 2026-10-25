using AutoMapper;
using DesignPatternsDemo.Models.Client.Requests;
using DesignPatternsDemo.Models.Client.Responses;
using DesignPatternsDemo.Models.Data;

namespace DesignPatternsDemo.Mapping;

/// <summary>
/// AutoMapper profile defining all DTO &lt;-&gt; Entity mappings for the application.
/// </summary>
public sealed class MappingProfile : Profile
{
    public MappingProfile()
    {
        // Entity -> Response (bidirectional)
        CreateMap<ProductEntity, ProductResponse>().ReverseMap();

        // CreateRequest -> Entity (Id, timestamps are server-assigned)
        CreateMap<CreateProductRequest, ProductEntity>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedAtUtc, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedAtUtc, opt => opt.Ignore());

        // UpdateRequest -> Entity (Id, CreatedAtUtc are immutable)
        CreateMap<UpdateProductRequest, ProductEntity>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedAtUtc, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedAtUtc, opt => opt.Ignore());
    }
}
