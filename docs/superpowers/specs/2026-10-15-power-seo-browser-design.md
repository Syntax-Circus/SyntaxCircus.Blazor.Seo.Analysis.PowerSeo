# Power SEO browser companion design

Date: 2026-10-15. Status: ready for owner review. Engine, package name, browser worker approach and restrained initial checks were approved in conversation.

## Goal and boundaries

Provide browser-local writing assistance alongside SyntaxCircus.Seo.Analysis, without GPL dependencies or a separate analysis service. Support interactive Blazor Server and WebAssembly. Success means useful additional advice, unchanged baseline findings, explicit incomplete coverage, responsive editing and no external transmission of draft content by the analyzer.

Ship one MIT .NET 10 Razor class library: SyntaxCircus.Blazor.Seo.Analysis.PowerSeo. Reference the published core through NuGet. Use Microsoft.JSInterop and DI abstractions without an ASP.NET shared-framework reference that would prevent WebAssembly consumption. No dependencies on other SyntaxCircus libraries, Yoast, retext, React, or @power-seo/readability.

Do not integrate Puppies Plus, introduce a shared editor UI, add authoring scores, or implement all of Power SEO's checks in this increment. Existing Yoast remains a separate optional provider.

## Public API and host lifecycle

- PowerSeoCompanion implements ISeoCompanion and IAsyncDisposable. It receives IJSRuntime and validated PowerSeoOptions.
- AddSyntaxCircusPowerSeoAnalysis registers a scoped concrete PowerSeoCompanion. It does not replace another ISeoCompanion registration; hosts explicitly choose the companion passed to the core analyzer.
- PowerSeoOptions exposes Timeout, default5 seconds, valid greater than zero and at most1 minute. Validate options before the first JS call.
- Construction and service registration perform no JS work. Initialize the JS module lazily after interactivity. Calls during prerendering or without a usable browser return Unavailable coverage; callers can retry after rendering.
- Calls are independent and correlated with unique request IDs plus the immutable snapshot ID. Support two active analyses per companion instance; reject excess work as Unavailable without an unbounded queue.
- Caller cancellation terminates that request's worker and propagates OperationCanceledException. A provider deadline returns TimedOut. Disposal cancels active work, terminates workers and releases JS references; later calls cannot start work.
- Hosts own debouncing, document switching and generation checks. The sample uses750ms debounce, cancels obsolete requests, and refuses to display results from an older editor generation. No automatic analysis during prerender.

## JavaScript and rule mapping

Pin @power-seo/content-analysis1.0.19 and esbuild0.28.2, matching the inspected experiment. Commit exact lockfiles. Import only checkSubheadingDistribution, checkTransitionWords and checkWordComplexity; do not call the full scoring engine.

Use the C# snapshot's ReadabilityHtml for all three checks so chrome, code and generated content stay excluded. The browser does not independently re-extract a DOM or invent another snapshot identity.

| Power rule | Core semantic key |
| --- | --- |
| subheading-distribution | writing.subheading-distribution |
| transition-words | writing.transition-words |
| word-complexity | writing.word-complexity |

Map good and na to informational diagnostics, ok to suggestions and poor to warnings. Preserve the raw numeric score and upstream message; scores are not combined or presented as ranking predictions. Return all three assessments, including inapplicable results. Unknown IDs remain withheld diagnostics and cause partial coverage through the existing core policy. Keep the core's ownership/applicability rules intact.

Only English editorial snapshots qualify. General/contact/gallery content stays quiet. Power's own insufficient-content results remain na; the adapter adds no word, keyword, image, or link quotas. Reading-grade and transition checks are heuristics; UI copy should invite review rather than command authors to obey a score.

The RCL ships a small JS bridge and an ESM Web Worker bundle under its own _content path. Resolve the worker relative to import.meta.url so subpath hosting works. Use self-hosted files with no CDN, eval, blob workers or Node runtime in consuming applications.

The bridge owns per-request worker maps, finite timers, error/exit handling and explicit cancellation. A fresh worker handles each request and is terminated on every outcome. Limit prepared HTML to3 million characters to accommodate core HTML serialization while bounding transport. Limit responses to the three assessments, IDs64 characters and messages2000 characters each; reject malformed or oversize results explicitly before SignalR interop. Protocol1 echoes request ID, snapshot ID and engine version. Version/protocol mismatches withhold output with explicit partial coverage.

## Samples, packaging and qualification

Provide a minimal packaged-consumer host with Server and WebAssembly paths. Reference actual NuGet artifacts, not a sibling core ProjectReference. Include real C#→JS→worker→C# analysis, rewrite progression, cancellation, simultaneous editors, stale switching, disposal, failure/timeout and unsupported worker behavior. Components with state or async behavior use code-behind files.

Use owned synthetic fixtures by default. Re-run the existing34-document corpus through an optional importer; public article bodies remain ignored. Verify core finding retention, new-rule contributions, contact-page quietness and degraded behavior. Explain any changed findings rather than treating experiment output as a frozen accuracy specification.

Run .NET and JS tests on Windows/Linux and actual Chromium Server/WebAssembly checks. Check package assets from a fresh consumer, subpath loading, no third-party draft requests, no React/Yoast dependencies and no ASP.NET runtime-pack leakage into WebAssembly. Verify output with CSP allowing self-hosted scripts/workers; do not claim compatibility with every application CSP.

Measure the selected bundle's minified/gzip bytes, first/subsequent worker latency, payload sizes and observed limits. Inspect exact shipping dependency licenses and audit results. Record evidence and limitations without ranking or general linguistic-accuracy claims.

Follow sibling GitVersion, build.yml, NuGet trusted publishing, release environment and release tagging conventions. Build/test the JS assets before pack and include upstream notices. README begins with Build, Tests, NuGet and MIT badges. Tests badge reflects the workflow containing test gates, not a hardcoded count.

Implementation follows written-spec review and a task plan. Work stays on a branch in this checkout, without worktrees.
