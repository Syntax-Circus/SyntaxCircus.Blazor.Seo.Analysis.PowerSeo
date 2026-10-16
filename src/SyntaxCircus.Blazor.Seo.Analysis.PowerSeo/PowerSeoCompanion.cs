using Microsoft.JSInterop;
using SyntaxCircus.Seo.Analysis;
using System.Text.Json;

namespace SyntaxCircus.Blazor.Seo.Analysis.PowerSeo;

/// <summary>A scoped browser companion; use only after interactive rendering.</summary>
public sealed class PowerSeoCompanion : ISeoCompanion, IAsyncDisposable
{
    internal const string ModulePath = "./_content/SyntaxCircus.Blazor.Seo.Analysis.PowerSeo/power-seo.js";
    private const int MaximumConcurrency = 2;
    private static readonly TimeSpan CleanupTimeout = TimeSpan.FromSeconds(1);
    private readonly IJSRuntime _runtime;
    private readonly PowerSeoOptions _options;
    private readonly object _gate = new();
    private readonly CancellationTokenSource _lifetime = new();
    private Task? _initialization;
    private IJSObjectReference? _module;
    private IJSObjectReference? _session;
    private int _active;
    private bool _disposed;
    private TaskCompletionSource _idle = CompletedSource();
    private Task? _disposal;

    public PowerSeoCompanion(IJSRuntime runtime, PowerSeoOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(runtime);
        _runtime = runtime;
        _options = options ?? new();
        _options.Validate();
    }

    public async Task<CompanionResult> AnalyzeAsync(SeoSnapshot snapshot, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        cancellationToken.ThrowIfCancellationRequested();
        lock (_gate) ObjectDisposedException.ThrowIf(_disposed, this);
        if (!(snapshot.Locale.Equals("en", StringComparison.OrdinalIgnoreCase) ||
            snapshot.Locale.StartsWith("en_", StringComparison.OrdinalIgnoreCase) ||
            snapshot.Locale.StartsWith("en-", StringComparison.OrdinalIgnoreCase)))
            return Empty(snapshot.Id, CoverageState.UnsupportedLanguage);
        if (snapshot.Purpose != ContentPurpose.Editorial) return Empty(snapshot.Id, CoverageState.NotApplicable);
        lock (_gate)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            if (_active >= MaximumConcurrency) return Empty(snapshot.Id, CoverageState.Unavailable, "Browser analysis capacity reached.");
            if (_active++ == 0) _idle = new(TaskCreationOptions.RunContinuationsAsynchronously);
        }
        var id = Guid.NewGuid().ToString("N");
        IJSObjectReference? session = null;
        using var deadline = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, _lifetime.Token);
        deadline.CancelAfter(_options.Timeout);
        try
        {
            Task initialization;
            lock (_gate)
            {
                _lifetime.Token.ThrowIfCancellationRequested();
                if (_initialization is { IsFaulted: true } or { IsCanceled: true }) _initialization = null;
                initialization = _initialization ??= InitializeAsync();
            }
            // Cancel this waiter, not shared initialization: another editor may still need it.
            await initialization.WaitAsync(deadline.Token).ConfigureAwait(false);
            session = _session!;
            deadline.Token.ThrowIfCancellationRequested();
            var request = new AnalysisRequest(PowerSeoResultMapper.ProtocolVersion, id, snapshot.Id, snapshot.ReadabilityHtml, snapshot.Locale);
            var response = await session.InvokeAsync<AnalysisResponse>("analyze", deadline.Token, request, _options.Timeout.TotalMilliseconds)
                .AsTask().WaitAsync(deadline.Token).ConfigureAwait(false);
            cancellationToken.ThrowIfCancellationRequested();
            return PowerSeoResultMapper.Map(response, id, snapshot.Id);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { throw; }
        catch (OperationCanceledException)
        {
            return Empty(snapshot.Id, _lifetime.IsCancellationRequested ? CoverageState.Unavailable : CoverageState.TimedOut);
        }
        catch (JSDisconnectedException) { return Empty(snapshot.Id, CoverageState.Unavailable, "Interactive browser disconnected."); }
        catch (InvalidOperationException) { return Empty(snapshot.Id, CoverageState.Unavailable, "Browser interop is not available; retry after interactive rendering."); }
        catch (JSException) { return Empty(snapshot.Id, CoverageState.Failed, "Browser analysis failed."); }
        catch (JsonException) { return Empty(snapshot.Id, CoverageState.Failed, "Malformed browser response."); }
        finally
        {
            if (session is not null) await CleanupAsync(session, "cancel", id).ConfigureAwait(false);
            lock (_gate) if (--_active == 0) _idle.TrySetResult();
        }
    }

    private async Task InitializeAsync()
    {
        // Initialization owns its own finite deadline and is observed during disposal.
        using var deadline = new CancellationTokenSource(_options.Timeout);
        _module ??= await _runtime.InvokeAsync<IJSObjectReference>("import", deadline.Token, ModulePath).ConfigureAwait(false);
        _session ??= await _module.InvokeAsync<IJSObjectReference>("createSession", deadline.Token).ConfigureAwait(false);
    }

    public ValueTask DisposeAsync()
    {
        lock (_gate) return new(_disposal ??= DisposeCoreAsync());
    }

    private async Task DisposeCoreAsync()
    {
        Task idle;
        Task? initialization;
        lock (_gate) { _disposed = true; idle = _idle.Task; }
        _lifetime.Cancel();
        await idle.ConfigureAwait(false);
        // Active callers have now published any initialization they started, including
        // reentrant disposal during import before the task assignment completed.
        lock (_gate) initialization = _initialization;
        if (initialization is not null)
        {
            try { await initialization.ConfigureAwait(false); }
            catch (Exception exception) when (exception is JSException or InvalidOperationException or OperationCanceledException) { }
        }
        if (_session is not null) { await CleanupAsync(_session, "dispose").ConfigureAwait(false); await ReleaseAsync(_session).ConfigureAwait(false); }
        if (_module is not null) await ReleaseAsync(_module).ConfigureAwait(false);
        _lifetime.Dispose();
    }

    private static async Task CleanupAsync(IJSObjectReference reference, string method, params object?[] args)
    {
        using var deadline = new CancellationTokenSource(CleanupTimeout);
        try { await reference.InvokeVoidAsync(method, deadline.Token, args).AsTask().WaitAsync(deadline.Token).ConfigureAwait(false); }
        catch (Exception exception) when (exception is JSException or OperationCanceledException or ObjectDisposedException or InvalidOperationException) { }
    }
    private static async Task ReleaseAsync(IJSObjectReference reference)
    {
        try { await reference.DisposeAsync().AsTask().WaitAsync(CleanupTimeout).ConfigureAwait(false); }
        catch (Exception exception) when (exception is JSException or TimeoutException or ObjectDisposedException or InvalidOperationException) { }
    }
    private static CompanionResult Empty(string snapshotId, CoverageState state, string? detail = null)
        => new(snapshotId, PowerSeoResultMapper.Provider, PowerSeoResultMapper.EngineVersion, state, [], detail);
    private static TaskCompletionSource CompletedSource()
    {
        var result = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        result.SetResult(); return result;
    }
}
