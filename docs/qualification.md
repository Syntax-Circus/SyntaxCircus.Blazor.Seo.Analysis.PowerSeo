# Power SEO adapter qualification

Measured 2026-10-15 with .NET SDK10.0.401, Node24, Playwright1.64.1 and pinned Power SEO1.0.19. This qualifies the three-check adapter, not the earlier broad-engine experiment or general linguistic accuracy.

## Package and dependency evidence

An actual local NuGet was consumed by separate Server and WebAssembly projects, using the public SyntaxCircus.Seo.Analysis0.1.0 dependency. Its nuspec contains only core, JSInterop and DI abstractions; no ASP.NET framework reference or Node dependency. Inspection confirmed both static JS assets, buildTransitive static-asset metadata, README, MIT license and third-party notices.

The selected worker is11,387 minified bytes /4,422 gzip bytes; bridge2,336 bytes. The bundled graph contains @power-seo/core1.0.19 and @power-seo/content-analysis1.0.19, both MIT. npm audit reports zero vulnerabilities including development tooling. No React, Yoast, retext or readability package ships.

## Automated evidence

- Windows and Linux qualification covers the .NET and Node suites; final suite totals are21 .NET tests and8 Node tests.
- Chromium runs packaged Server and WebAssembly at root and /demo. The tested CSP allows self-hosted scripts/workers plus wasm-unsafe-eval for the .NET runtime, with no script unsafe-inline or blob workers.
- Owned24 corpus: baseline retained24/24 per mode;19 general inputs correctly NotApplicable,5 editorial inputs Complete,15 raw assessments accounted for per mode. General/contact content contributes no writing advice.
- Retained34 corpus: Windows packaged Server/WASM at root and /demo retain baseline34/34 per mode;19 NotApplicable,15 Complete and45 raw checks accounted for. Largest prepared HTML50,282 characters; largest serialized result1,624 UTF-8 bytes. One root run measured6–30ms Server and8–116ms WebAssembly after module warmup. Public article bodies remain ignored and are not redistributed.
- Actual interop checks: two editors, debounced changes, active-request cancellation and latest-generation publication, missing Worker API, worker error, malformed response, stale correlation and five-second timeout. Each degraded state preserves core findings.
- Healthy runs report zero active workers after completion, peak at most two, zero CSP violations and zero third-party browser requests. Independent-session cancellation and disposal are also covered by controlled .NET/JS unit tests.
- One Windows root run measured editorial end-to-end calls at5.6–8.4ms Server and9.4–11ms WebAssembly after module warmup. Largest owned prepared HTML was2,117 characters; largest serialized CompanionResult1,381 UTF-8 bytes. These are small local fixture observations, not performance guarantees.

Detailed per-fixture measurements are ignored artifacts/browser*.json; CI retains them together with sample logs and bundle measurements. scripts/run-browser.mjs reproduces root/subpath checks; Dockerfile.qualification reproduces Linux tests and actual packaged browsers.

## Limits

Only English editorial text and three additional writing checks are supported. Advice uses upstream heuristics and may not fit every author's style. Numeric vendor values are retained as diagnostics, never combined into a ranking or author score.

The owned corpus is small, synthetic and mostly general pages. The optional retained34-document corpus is a limited spot check, not a linguistic accuracy benchmark. Boundary/malformed inputs, unsupported languages, capacity and cancellation races use deterministic tests rather than claiming broad corpus coverage. Cold module/import latency has not been measured separately; that minor qualification follow-up remains deferred.

Fresh workers incur startup overhead per request. The provider allows two active requests without queueing. Default deadline is five seconds; maximum one minute. The prepared HTML bound is three million characters; messages are bounded before Server interop. No mobile, Firefox, Safari, disconnected-circuit recovery, every possible CSP, or production Puppies Plus integration is claimed.
