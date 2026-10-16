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

Task3 C# lifecycle: in progress.

No production support, published package, passing adapter test count or measured browser payload is claimed. Existing experiment results describe the older broad Power bundle, not this proposed three-check adapter.
