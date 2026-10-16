using BlazorHost.Components;
using SyntaxCircus.Blazor.Seo.Analysis.PowerSeo;
var builder = WebApplication.CreateBuilder(args);
builder.Services.AddRazorComponents().AddInteractiveServerComponents().AddInteractiveWebAssemblyComponents();
builder.Services.AddSyntaxCircusPowerSeoAnalysis();
var app = builder.Build();
var pathBase = builder.Configuration["SamplePathBase"];
if (!string.IsNullOrEmpty(pathBase)) app.UsePathBase(pathBase);
app.UseRouting();
app.Use(async (context, next) =>
{
    context.Response.Headers.ContentSecurityPolicy = "default-src 'self'; script-src 'self' 'wasm-unsafe-eval'; worker-src 'self'; connect-src 'self' ws:; style-src 'self'; img-src 'self' data:; object-src 'none'; base-uri 'self'";
    await next();
});
app.UseAntiforgery();
app.MapStaticAssets();
app.MapRazorComponents<App>().AddInteractiveServerRenderMode().AddInteractiveWebAssemblyRenderMode()
    .AddAdditionalAssemblies(typeof(BlazorClient.Editor).Assembly);
app.Run();
