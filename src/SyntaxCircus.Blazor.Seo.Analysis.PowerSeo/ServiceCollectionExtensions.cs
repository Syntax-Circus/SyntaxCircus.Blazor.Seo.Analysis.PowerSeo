using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
namespace SyntaxCircus.Blazor.Seo.Analysis.PowerSeo;
public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddSyntaxCircusPowerSeoAnalysis(this IServiceCollection services, PowerSeoOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(services);
        options ??= new(); options.Validate();
        services.TryAddSingleton(options);
        services.TryAddScoped<PowerSeoCompanion>();
        return services;
    }
}
