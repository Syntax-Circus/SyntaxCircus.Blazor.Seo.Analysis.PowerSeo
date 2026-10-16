using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using SyntaxCircus.Blazor.Seo.Analysis.PowerSeo;
var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.Services.AddSyntaxCircusPowerSeoAnalysis();
await builder.Build().RunAsync();
