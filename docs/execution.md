# Power SEO browser adapter execution

The owner selected the MIT Power SEO browser companion and created this repository. Package name, Web Worker execution and selective additional writing checks are settled. Original core and optional server-side Yoast remain separate.

## Progress

- Inspected sibling badge/release conventions and the existing ISeoCompanion contract.
- Added matching Build, Tests, NuGet and license badges across all three READMEs. Yoast's client and service licenses are labeled separately.
- Inspected actual pinned Power exports: subheading-distribution, transition-words and word-complexity are standalone checks. The full engine is unnecessary for the first version.
- Architectural design written and self-reviewed for scope, API ownership, Server/WebAssembly compatibility, cancellation/disposal, bounded transport and qualification.
- Written design approved by the owner. Implementation plan: docs/superpowers/plans/2026-10-15-power-seo-browser.md. Executing inline with TDD and one final independent review.

## Implementation ledger

Pre-flight: mapper wire envelope is shared by JS session and C# lifecycle; use protocol1, engine1.0.19 and raw status, not numeric score, for semantics. JS modules are global per browser so each companion gets a distinct disposable session. No public core changes required.

Ruling: the owner's “ok, then do so” authorizes implementing the approved design end-to-end on this branch. Registration is additive and concrete, preserving other providers. Per-instance JS sessions prevent shared-module cancellation cross-talk.

Task1 mapping/package: complete.10 .NET mapping/options tests observed failing stubs then green. Status, not numeric score, controls Power advice.

Task2 scoped workers: complete. Seven JS cases observed six missing-behavior failures (invalid-input rejection also threw from the stub; positive tests prevented a throw-only implementation). Real three-check engine, independent session lifecycle and explicit bounds now pass. A follow-up unsupported-response payload test reproduced leaked extra properties RED and fixes bounded projection GREEN.

Selected worker measurement before lifecycle integration:11,387 minified bytes/4,422 gzip; bridge2,228 bytes. Shipping graph is only @power-seo/core and @power-seo/content-analysis1.0.19, both MIT. Full dev npm audit reports zero. Final measurements will be refreshed after review.

Task3 C# lifecycle: complete (2fc6f7b).20 .NET tests pass, including lazy initialization, prerender retry, caller cancellation, provider deadline, capacity, shared initialization waiters and late-reference disposal. Test compilation corrections were harness fixes, not product RED evidence. Empty Ready assessments were separately reproduced RED then rejected GREEN.

Task4 packaged consumers: implementation, independent review/fix and corpus qualification complete; PR readiness follows the external CI gate. Actual NuGet consumers pass Server/WASM under root/subpath and self-only script/worker CSP. Owned24 retain baseline24/24 in each mode, with5 eligible editorials and15 raw assessments. Browser failure, stale, timeout and unavailable states preserve baseline. Active rewrite cancellation terminates workers and publishes the latest generation. Final Windows/Linux21 .NET and8 JS tests pass; npm and NuGet audits report no known vulnerable packages.

Sample middleware routing under a path base was reproduced failing Blazor negotiation and corrected with explicit UseRouting after UsePathBase. Concurrent Playwright mouse clicks raced each other; clicks are now sequential while requests remain independent. Neither was an adapter-engine defect. Qualification evidence is in docs/qualification.md.

No published release or Puppies Plus production integration is claimed. Existing broad-engine experiment measurements are separate.

Retained34 qualification: real packaged Server/WASM at root and /demo preserve baseline34/34;19 general pages NotApplicable,15 editorials Complete and45 raw checks accounted for per mode. Initial whole-corpus fixture loading through Server JS interop exceeded SignalR's default input limit and timed out; the sample now loads fixtures through its loopback endpoint directly on Server and fetches in WASM. Actual worker results remain bounded before SignalR. Public article bodies remain ignored.

Final review: independent fresh-context reviewer inspected the full branch, reran20 .NET/8 JS tests and inspected the local NuGet. One Important finding: disposal captured initialization before active callers published it. Reproduced with Disposal_during_import_publication_observes_late_references RED (late references not disposed), then fixed by preventing new initialization after cancellation and observing initialization after callers drain. Full Windows suite21/21 GREEN.

Final: minor (deferred): cold initialization latency is not separately measured; existing performance evidence is explicitly after module warmup.

Final: Ruling: broad linguistic accuracy, other browsers/CSP policies and disconnected-circuit recovery remain outside this increment and are explicitly excluded from support claims; cost if wrong is additional host qualification before deployment.

Final: Ruling: release-account setup remains the owner's configured trusted-publishing environment and cannot be proven without a release; workflow follows sibling conventions and is linted. Cost if configuration differs is a release-job correction after merge, not a runtime/package change.

Ruling: existing push/PR authorization and the approved end-to-end plan select finishing by updating PR1 and marking ready after checks, without merging or publishing. Cost if wrong is reversible PR readiness.
