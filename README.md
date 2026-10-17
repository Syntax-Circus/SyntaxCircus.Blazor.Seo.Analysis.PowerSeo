# SyntaxCircus.Blazor.Seo.Analysis.PowerSeo

[![Build](https://github.com/Syntax-Circus/SyntaxCircus.Blazor.Seo.Analysis.PowerSeo/actions/workflows/build.yml/badge.svg)](https://github.com/Syntax-Circus/SyntaxCircus.Blazor.Seo.Analysis.PowerSeo/actions/workflows/build.yml)
[![Tests](https://img.shields.io/github/actions/workflow/status/Syntax-Circus/SyntaxCircus.Blazor.Seo.Analysis.PowerSeo/build.yml?label=tests)](https://github.com/Syntax-Circus/SyntaxCircus.Blazor.Seo.Analysis.PowerSeo/actions/workflows/build.yml)
[![NuGet](https://img.shields.io/nuget/v/SyntaxCircus.Blazor.Seo.Analysis.PowerSeo.svg)](https://www.nuget.org/packages/SyntaxCircus.Blazor.Seo.Analysis.PowerSeo)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](LICENSE)

MIT browser companion for `SyntaxCircus.Seo.Analysis`, using three selected Power SEO checks in a self-hosted Web Worker. Supports interactive Blazor Server and WebAssembly.

The package is published on nuget.org (v0.1.0):

```sh
dotnet add package SyntaxCircus.Blazor.Seo.Analysis.PowerSeo
```

The Tests badge tracks the workflow's test gates. See [qualification evidence](docs/qualification.md) and [execution status](docs/execution.md).

Checks: subheading distribution, transition words and word complexity. Only English editorial snapshots qualify. General pages return NotApplicable; other languages return UnsupportedLanguage. The core retains baseline ownership and applicability. Raw statuses and numeric values remain diagnostics; this package provides no overall authoring score or search ranking prediction.

## Use

Install `SyntaxCircus.Blazor.Seo.Analysis.PowerSeo` in the interactive client and, for Server rendering, the host. The package references the published core.

```csharp
using SyntaxCircus.Blazor.Seo.Analysis.PowerSeo;
using SyntaxCircus.Seo.Analysis;

builder.Services.AddSyntaxCircusPowerSeoAnalysis(
    new PowerSeoOptions { Timeout = TimeSpan.FromSeconds(5) });
```

Inject the scoped `PowerSeoCompanion`. After interactive rendering:

```csharp
var analyzer = new ContentSeoAnalyzer();
var snapshot = analyzer.Prepare(
    new SeoDocumentInput(articleHtml, Purpose: ContentPurpose.Editorial), cancellationToken);
var report = await analyzer.AnalyzeAsync(snapshot, companion, cancellationToken);
```

Registration adds the concrete provider and preserves existing `ISeoCompanion` registrations. Choose the companion explicitly when invoking the core. The host owns debounce, cancellation and latest-generation checks. The [packaged sample](samples/README.md) demonstrates these with a 750ms debounce.

Construction performs no interop. Prerender calls return Unavailable and may be retried once interactive. Each companion owns an isolated JS session with at most two active workers and no queue. Caller cancellation terminates its worker and propagates cancellation. Provider deadlines return TimedOut; broken workers return explicit incomplete coverage while core findings remain available. Let DI dispose the scoped provider.

Timeout must be positive and at most one minute. Prepared HTML is limited to three million characters; replies are limited to three assessments with bounded identifiers and messages. Unknown rule IDs are withheld by the core. Power's heuristics invite editorial review rather than prescribing writing quotas.

## Hosting and building

The NuGet carries its bridge, module worker and MIT notices under `_content/SyntaxCircus.Blazor.Seo.Analysis.PowerSeo/`. Consumers need .NET 10 and normal Blazor static assets; no Node service, CDN, React or Yoast dependency is required. Worker URLs resolve relative to the module for subpath hosting.

CSP must permit self-hosted module scripts and workers. The sample also permits `wasm-unsafe-eval` for the .NET WebAssembly runtime. Application CSP policies beyond the tested sample need their own verification. Browser analysis sends no drafts to an external analysis service; Blazor Server's normal circuit still transports application data between its server and browser.

Repository builds require Node 24 and .NET 10:

```sh
npm ci --ignore-scripts
npm test
dotnet test -c Release
dotnet pack src/SyntaxCircus.Blazor.Seo.Analysis.PowerSeo -c Release
```

The build bundles only the three pinned Power checks and regenerates shipping notices. Build tooling is not shipped to consuming applications. See [THIRD-PARTY-NOTICES.txt](THIRD-PARTY-NOTICES.txt) for the bundled MIT dependency notices.
