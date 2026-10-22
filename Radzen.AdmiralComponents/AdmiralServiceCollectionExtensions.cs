using Microsoft.Extensions.DependencyInjection;
using Radzen;

namespace Radzen.Admiral.Components;

public static class AdmiralServiceCollectionExtensions
{
    public static IServiceCollection AddAdmiralComponents(
        this IServiceCollection services,
        Action<AdmiralComponentsOptions>? configure = null)
    {
        var options = new AdmiralComponentsOptions();
        configure?.Invoke(options);

        services.AddRadzenComponents();
        services.AddScoped<AdmiralToastService>();
        services.AddRadzenCookieThemeService(cookieOptions =>
        {
            cookieOptions.Name = options.ThemeCookieName;
            cookieOptions.Duration = options.ThemeCookieDuration;
        });

        return services;
    }
}
