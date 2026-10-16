using Microsoft.AspNetCore.Components;
using SyntaxCircus.Blazor.Seo.Analysis.PowerSeo;
using SyntaxCircus.Seo.Analysis;
namespace BlazorClient;
public partial class Editor
{
    [Parameter] public string Name { get; set; } = "Editor";
    [Inject] private PowerSeoCompanion Companion { get; set; } = null!;
    private readonly ContentSeoAnalyzer _analyzer = new();
    private string _text = "<p>We welcome our neighbors. We share practical advice. We help people write clear articles.</p>";
    private bool _interactive;
    private bool _disposed;
    private long _generation;
    private CancellationTokenSource? _pending;
    private Task _work = Task.CompletedTask;
    private SeoAnalysisReport? _report;
    private string _checkedText = "";
    private string _status = "Waiting for interactive rendering.";
    protected override void OnAfterRender(bool firstRender)
    {
        if (firstRender) { _interactive = true; _status = "Ready"; StateHasChanged(); }
    }
    private Task Changed(ChangeEventArgs change)
    {
        _text = change.Value?.ToString() ?? "";
        return Schedule(TimeSpan.FromMilliseconds(750));
    }
    private Task Analyze() => Schedule(TimeSpan.Zero);
    private Task Schedule(TimeSpan delay)
    {
        _pending?.Cancel();
        _pending?.Dispose();
        _pending = new();
        var generation = ++_generation;
        _work = RunAsync(_text, generation, delay, _pending.Token);
        return delay == TimeSpan.Zero ? _work : Task.CompletedTask;
    }
    private async Task RunAsync(string text, long generation, TimeSpan delay, CancellationToken token)
    {
        try
        {
            await Task.Delay(delay, token);
            if (!_interactive || _disposed) return;
            _status = "Checking…";
            await InvokeAsync(StateHasChanged);
            var snapshot = _analyzer.Prepare(new SeoDocumentInput(text, Purpose: ContentPurpose.Editorial), token);
            var report = await _analyzer.AnalyzeAsync(snapshot, Companion, token);
            if (_disposed || generation != _generation) return;
            _report = report;
            _checkedText = text;
            _status = "Checked";
            await InvokeAsync(StateHasChanged);
        }
        catch (OperationCanceledException) when (token.IsCancellationRequested) { }
    }
    public async ValueTask DisposeAsync()
    {
        _disposed = true; ++_generation;
        _pending?.Cancel();
        await _work;
        _pending?.Dispose();
    }
}
