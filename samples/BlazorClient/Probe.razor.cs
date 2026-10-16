using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using SyntaxCircus.Seo.Analysis;
using SyntaxCircus.Blazor.Seo.Analysis.PowerSeo;
using System.Text.Json;
namespace BlazorClient;
public partial class Probe
{
    [Inject] private PowerSeoCompanion Companion { get; set; } = null!;
    [Inject] private IJSRuntime JS { get; set; } = null!;
private bool _interactive;
    private string _result = "Waiting";
    protected override void OnAfterRender(bool firstRender)
    {
        if (firstRender) { _interactive = true; _result = "Ready"; StateHasChanged(); }
    }
    private async Task Run()
    {
        _result = "Running";
        var json = await JS.InvokeAsync<string>("sampleFixtures");
        var inputs = JsonSerializer.Deserialize<SeoDocumentInput[]>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true })!;
        var analyzer = new ContentSeoAnalyzer();
        var results = new List<object>();
        foreach (var input in inputs)
        {
            var snapshot = analyzer.Prepare(input);
            var baseline = await analyzer.AnalyzeAsync(snapshot);
            var watch = System.Diagnostics.Stopwatch.StartNew();
            var raw = await Companion.AnalyzeAsync(snapshot);
            var report = await analyzer.AnalyzeAsync(snapshot, new FixedCompanion(raw));
            results.Add(new { snapshot.Id, State = raw.State.ToString(), Raw = raw.Assessments.Length,
                Preserved = baseline.Findings.All(f => report.Findings.Contains(f)),
                Decisions = report.CompanionDecisions.Length, Milliseconds = watch.Elapsed.TotalMilliseconds,
                InputCharacters = snapshot.ReadabilityHtml.Length,
                ResultBytes = System.Text.Encoding.UTF8.GetByteCount(JsonSerializer.Serialize(raw)),
                Assessments = raw.Assessments });
        }
        _result = JsonSerializer.Serialize(results);
    }
    private sealed class FixedCompanion(CompanionResult result) : ISeoCompanion
    {
        public Task<CompanionResult> AnalyzeAsync(SeoSnapshot snapshot, CancellationToken cancellationToken = default)
            => Task.FromResult(result);
    }

}

