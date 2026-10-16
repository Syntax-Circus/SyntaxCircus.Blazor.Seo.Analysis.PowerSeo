// Tokens under test are explicitly controlled in the cancellation/timeout cases.
#pragma warning disable xUnit1051
using SyntaxCircus.Blazor.Seo.Analysis.PowerSeo;
using Microsoft.JSInterop;
using SyntaxCircus.Seo.Analysis;
using Xunit;
public sealed class LifecycleTests
{
    private static SeoSnapshot Snapshot(ContentPurpose purpose=ContentPurpose.Editorial,string locale="en_US")
        => new ContentSeoAnalyzer().Prepare(new("<h1>Care</h1><p>We feed puppies.</p>",Purpose:purpose,Locale:locale));
    [Fact] public async Task Lazy_initialization_and_real_response_mapping()
    {
        var js=new Runtime();await using var companion=new PowerSeoCompanion(js);
        Assert.Equal(0,js.Imports);
        var result=await companion.AnalyzeAsync(Snapshot());
        Assert.Equal(CoverageState.Complete,result.State);Assert.Equal(1,js.Imports);
        Assert.Equal(FindingSeverity.Warning,Assert.Single(result.Assessments).Severity);
        await companion.AnalyzeAsync(Snapshot());Assert.Equal(1,js.Imports);
    }
    [Fact] public async Task Prerender_is_unavailable_and_a_later_call_retries()
    {
        var js=new Runtime{Prerender=true};await using var companion=new PowerSeoCompanion(js);
        Assert.Equal(CoverageState.Unavailable,(await companion.AnalyzeAsync(Snapshot())).State);
        js.Prerender=false;Assert.Equal(CoverageState.Complete,(await companion.AnalyzeAsync(Snapshot())).State);
    }
    [Fact] public async Task Noneditorial_and_unsupported_languages_do_not_initialize_js()
    {
        var js=new Runtime();await using var companion=new PowerSeoCompanion(js);
        Assert.Equal(CoverageState.NotApplicable,(await companion.AnalyzeAsync(Snapshot(ContentPurpose.General))).State);
        Assert.Equal(CoverageState.UnsupportedLanguage,(await companion.AnalyzeAsync(Snapshot(locale:"fr_FR"))).State);
        Assert.Equal(0,js.Imports);
    }
    [Fact] public async Task Cancellation_terminates_request_and_propagates()
    {
        var js=new Runtime{Hold=true};await using var companion=new PowerSeoCompanion(js);
        using var cts=new CancellationTokenSource();
        var pending=companion.AnalyzeAsync(Snapshot(),cts.Token);
        await js.Started.Task;cts.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(()=>pending);
        Assert.Equal(1,js.Cancels);
    }
    [Fact] public async Task Provider_timeout_is_explicit()
    {
        var js=new Runtime{Hold=true};await using var companion=new PowerSeoCompanion(js,new(){Timeout=TimeSpan.FromMilliseconds(25)});
        Assert.Equal(CoverageState.TimedOut,(await companion.AnalyzeAsync(Snapshot())).State);
        Assert.Equal(1,js.Cancels);
    }
    [Fact] public async Task Capacity_is_bounded_and_disposal_cancels_active_work()
    {
        var js=new Runtime{Hold=true};var companion=new PowerSeoCompanion(js);
        var a=companion.AnalyzeAsync(Snapshot());var b=companion.AnalyzeAsync(Snapshot());
        await js.Started.Task;
        Assert.Equal(CoverageState.Unavailable,(await companion.AnalyzeAsync(Snapshot())).State);
        await companion.DisposeAsync();
        Assert.Equal(CoverageState.Unavailable,(await a).State);Assert.Equal(CoverageState.Unavailable,(await b).State);
        Assert.True(js.Disposed);Assert.Equal(2,js.Cancels);
        await Assert.ThrowsAsync<ObjectDisposedException>(()=>companion.AnalyzeAsync(Snapshot()));
    }
    [Fact] public async Task Core_findings_survive_worker_failure()
    {
        var js=new Runtime{Fail=true};await using var companion=new PowerSeoCompanion(js);
        var analyzer=new ContentSeoAnalyzer();var snapshot=analyzer.Prepare(new("<h1>Care</h1><img>",Purpose:ContentPurpose.Editorial));
        var report=await analyzer.AnalyzeAsync(snapshot,companion);
        Assert.Contains(report.Findings,x=>x.RuleId=="core.image-alt");
        Assert.Contains(report.Coverage,x=>x.State==CoverageState.Failed);
    }
    [Fact] public async Task Canceling_one_initialization_waiter_does_not_cancel_another()
    {
        var js=new Runtime{ImportDelay=new(TaskCreationOptions.RunContinuationsAsynchronously)};
        await using var companion=new PowerSeoCompanion(js);
        using var cts=new CancellationTokenSource();
        var a=companion.AnalyzeAsync(Snapshot(),cts.Token);var b=companion.AnalyzeAsync(Snapshot());
        cts.Cancel();await Assert.ThrowsAnyAsync<OperationCanceledException>(()=>a);
        js.ImportDelay.SetResult();
        Assert.Equal(CoverageState.Complete,(await b).State);Assert.Equal(1,js.Imports);
    }
    [Fact] public async Task Disposal_observes_initialization_and_releases_late_references()
    {
        var js=new Runtime{ImportDelay=new(TaskCreationOptions.RunContinuationsAsynchronously)};
        var companion=new PowerSeoCompanion(js);var pending=companion.AnalyzeAsync(Snapshot());
        var disposal=companion.DisposeAsync().AsTask();
        js.ImportDelay.SetResult();await disposal;
        Assert.Equal(CoverageState.Unavailable,(await pending).State);Assert.True(js.Disposed);
    }
    [Fact] public async Task Disposal_during_import_publication_observes_late_references()
    {
        var js=new Runtime{ImportDelay=new(TaskCreationOptions.RunContinuationsAsynchronously)};
        var companion=new PowerSeoCompanion(js);
        Task? disposal=null;
        js.OnImport=()=>disposal=companion.DisposeAsync().AsTask();
        var pending=companion.AnalyzeAsync(Snapshot());
        Assert.Equal(CoverageState.Unavailable,(await pending).State);
        try
        {
            Assert.NotNull(disposal);
            Assert.False(disposal.IsCompleted);
        }
        finally { js.ImportDelay.SetResult(); }
        await disposal!;
        Assert.True(js.Disposed);
    }
    [Fact] public void Registration_preserves_existing_companions_and_does_no_js_work()
    {
        Microsoft.Extensions.DependencyInjection.IServiceCollection services=new Microsoft.Extensions.DependencyInjection.ServiceCollection();
        var existing=Microsoft.Extensions.DependencyInjection.ServiceDescriptor.Scoped<ISeoCompanion,PowerSeoCompanion>();
        services.Add(existing);services.AddSyntaxCircusPowerSeoAnalysis();
        Assert.Same(existing,Assert.Single(services,x=>x.ServiceType==typeof(ISeoCompanion)));
        Assert.Single(services,x=>x.ServiceType==typeof(PowerSeoCompanion));
    }
    internal sealed class Runtime : IJSRuntime,IJSObjectReference
    {
        internal int Imports,Cancels;internal bool Prerender,Hold,Fail,Disposed;
        internal TaskCompletionSource Started=new(TaskCreationOptions.RunContinuationsAsynchronously);
        internal TaskCompletionSource? ImportDelay;
        internal Action? OnImport;
        public ValueTask<T> InvokeAsync<T>(string identifier,object?[]? args)=>InvokeAsync<T>(identifier,CancellationToken.None,args);
        public ValueTask<T> InvokeAsync<T>(string identifier,CancellationToken token,object?[]? args)
        {
            if(identifier=="import"){Imports++;OnImport?.Invoke();if(Prerender)throw new InvalidOperationException("prerender");return ImportDelay is null ? ValueTask.FromResult((T)(object)this) : new(WaitImport<T>(token));}
            if(identifier=="createSession")return ValueTask.FromResult((T)(object)this);
            if(identifier=="cancel"){Cancels++;return ValueTask.FromResult(default(T)!);}
            if(identifier=="dispose"){Disposed=true;return ValueTask.FromResult(default(T)!);}
            return new ValueTask<T>(Analyze<T>((AnalysisRequest)args![0]!,token));
        }
        private async Task<T> Analyze<T>(AnalysisRequest request,CancellationToken token)
        {
            Started.TrySetResult();
            if(Hold)await Task.Delay(Timeout.Infinite,token);
            if(Fail)throw new JSException("worker failed");
            return (T)(object)new AnalysisResponse(1,request.RequestId,request.SnapshotId,"1.0.19","Ready",[new("word-complexity","poor",0,"Complex.")]);
        }
        private async Task<T> WaitImport<T>(CancellationToken token){await ImportDelay!.Task.WaitAsync(token);return (T)(object)this;}
        public ValueTask DisposeAsync(){Disposed=true;return ValueTask.CompletedTask;}
    }
}
