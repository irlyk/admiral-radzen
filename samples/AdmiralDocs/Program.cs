using System.Globalization;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Radzen;
using Radzen.Admiral.Components;
using AdmiralConsumerSmoke;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");
builder.Services.AddLocalization(options => options.ResourcesPath = "Resources");
builder.Services.AddRadzenComponents();
builder.Services.AddScoped<AdmiralToastService>();

// The boot script exposes only validated preferences. Set culture before Razor
// components and localizers are constructed, including after a deep-link refresh.
var host = builder.Build();
var preferences = await host.Services.GetRequiredService<Microsoft.JSInterop.IJSRuntime>()
    .InvokeAsync<string>("admiralDocs.readCulture", Array.Empty<object>());
var culture = CultureInfo.GetCultureInfo(preferences == "en" ? "en" : "ru");
CultureInfo.DefaultThreadCurrentCulture = culture;
CultureInfo.DefaultThreadCurrentUICulture = culture;
CultureInfo.CurrentCulture = culture;
CultureInfo.CurrentUICulture = culture;

await host.RunAsync();
