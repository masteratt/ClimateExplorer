using Blazorise;
using Blazorise.Bootstrap5;
using Blazorise.Icons.FontAwesome;
using DPBlazorMapLibrary;
using ClimateExplorer.Visualiser.Services;
using BlazorCurrentDevice;
using Blazored.LocalStorage;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
//builder.RootComponents.Add<App>("#app");
//builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services
    .AddBlazorise()
    .AddBootstrap5Providers()
    .AddFontAwesomeIcons()
    .AddMapService()
    .AddScoped(sp => new HttpClient { BaseAddress = new Uri("http://localhost:54836/") })
    .AddSingleton<IDataServiceCache, DataServiceCache>()
    .AddTransient<IExporter, Exporter>()
    .AddBlazorCurrentDevice()
    .AddBlazoredLocalStorage()
    .AddHttpClient<IDataService, DataService>(client =>
    {
        client.BaseAddress = new Uri("http://localhost:54836/");// builder.Configuration["dataServiceBaseUri"]!);
    });

await builder.Build().RunAsync();
