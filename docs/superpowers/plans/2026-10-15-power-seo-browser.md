# Power SEO Browser Companion Implementation Plan

> For agentic workers: use superpowers:executing-plans to implement task-by-task with test-first behavior and one final independent review.

Goal: implement the approved browser companion as a packable net10 RCL and qualify actual packaged Server/WebAssembly consumers.
Architecture: published core snapshots → C# companion → scoped JS session → per-call module worker → validated raw results → core arbitration.
Tech stack: .NET10, Microsoft.JSInterop, DI abstractions, Power SEO1.0.19, esbuild0.28.2, xUnit3, Node tests, Playwright.
Spec: ../specs/2026-10-15-power-seo-browser-design.md

## Global constraints
- Existing checkout/branch only; no worktrees. Preserve README badges.
- MIT shipping dependency closure; no Yoast, React, retext, or ASP.NET framework reference in the library.
- Core is a NuGet dependency, version0.1.0. No sibling source reference.
- Three rules only; raw status drives mapping, including poor score0.
- Timeout default5s, maximum1min; concurrency2; snapshot/request/engine correlation and explicit degraded coverage.
- User's instruction “ok, then do so” authorizes end-to-end implementation of the approved design inline. This plan makes that work reviewable; routine implementation choices do not require another confirmation.

## Review focus
- Multiple scoped instances share a JS module but must never cancel each other's workers.
- Cancellation/disposal during module initialization must not strand workers or JS references.
- Worker errors, malformed payloads and missing assets must preserve core availability.
- Subpath hosting and a restrictive self-hosted CSP must work in actual WASM and Server consumers.
- Static assets must ship in the actual NuGet package and need no Node runtime in consuming apps.

## Task1: package and rule mapping
Files: solution/build metadata; src/.../PowerSeoOptions.cs, PowerSeoResultMapper.cs; tests/.../MappingTests.cs.
Interfaces: options Timeout; wire envelope protocolVersion, requestId, snapshotId, engineVersion, state, assessments(id,status,score,description).
- [ ] Write tests for poor score0→Warning, good/na→Information, unknown rules→partial diagnostics, malformed/version/stale results, invalid options, registration preserving existing ISeoCompanion.
- [ ] Run dotnet test -c Release: expect stub behavior failures, then implement mapping/options/registration and rerun green.
- [ ] Commit coherent package foundation.

## Task2: scoped worker sessions
Files: js/analysis.mjs, js/bridge.mjs, js/worker.mjs; scripts/build.mjs; JS tests.
Interfaces: module.createSession() returns JS object with analyze(request,timeout), cancel(requestId), dispose().
- [ ] Write Node tests for the three real checks, bounds/status validation, independent requests/sessions, capacity2, timeout, cancel, disposal and worker errors.
- [ ] Observe stub failures, implement exact selected imports and bridge lifecycle, rerun npm test.
- [ ] Build ESM assets into library wwwroot; measure gzip size and retain notices.
- [ ] Commit.

## Task3: C# lifecycle
Files: PowerSeoCompanion.cs and InteropContracts.cs; tests/.../LifecycleTests.cs.
Interfaces: PowerSeoCompanion implements ISeoCompanion,IAsyncDisposable; lazily imports bridge and creates a per-instance session.
- [ ] Test no eager JS, prerender/retry, cancellation, timeout, independent concurrent calls, disposal, failure/stale fallback and unsupported locale/purpose.
- [ ] Observe failing behavior, implement lifetime synchronization/correlation and rerun full .NET suite.
- [ ] Commit.

## Task4: packaged consumers and evidence
Files: samples/BlazorHost, samples/BlazorClient, scripts/browser.mjs, owned fixtures/importer, CI, README, docs/execution.md.
- [ ] Pack a unique local qualification version; build samples using that actual package and public core.
- [ ] Run real Server/WASM analysis under750ms debounce. Check rewrites, contact applicability, stale results, two instances, cleanup, failures, subpath/CSP, no third-party draft requests and package assets.
- [ ] Run owned24 and optional retained34 corpus through real C#→JS→worker→C#; preserve baseline and account for all raw assessments.
- [ ] Run Windows/Linux .NET+JS suites, actual Chromium browser checks, audits, pack inspection and measured size/latency evidence.
- [ ] Add sibling GitVersion/trusted-publishing CI, include bundled notices, update consumer README without removing badges.
- [ ] Independent whole-change review; reproduce/fix Important findings, verify, commit, push/update existing PR and mark ready after checks.
