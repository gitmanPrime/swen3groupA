using DMS.BLL.Interfaces.Services;
using DMS.BLL.Mapping;
using Microsoft.Extensions.DependencyInjection;

namespace DMS.BLL;

// registers the BLL services
public static class DependencyInjection
{
    public static IServiceCollection AddBusinessLogic(this IServiceCollection services)
    {
        services.AddScoped<IDocumentService, Services.DocumentService>();
        services.AddScoped<ICollectionService, Services.CollectionService>();
        services.AddScoped<ITagService, Services.TagService>();
        services.AddAutoMapper(cfg => cfg.AddProfile<MappingProfile>());
        return services;
    }
}