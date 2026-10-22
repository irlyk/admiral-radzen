namespace Radzen.Admiral.Components;

public sealed class AdmiralComponentsOptions
{
    public string ThemeCookieName { get; set; } = "AdmiralTheme";
    public TimeSpan ThemeCookieDuration { get; set; } = TimeSpan.FromDays(365);
}
