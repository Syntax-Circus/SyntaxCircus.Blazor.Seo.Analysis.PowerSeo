# Power SEO browser adapter execution

The owner selected the MIT Power SEO browser companion and created this repository. Package name, Web Worker execution and selective additional writing checks are settled. Original core and optional server-side Yoast remain separate.

## Progress

- Inspected sibling badge/release conventions and the existing ISeoCompanion contract.
- Added matching Build, Tests, NuGet and license badges across all three READMEs. Yoast's client and service licenses are labeled separately.
- Inspected actual pinned Power exports: subheading-distribution, transition-words and word-complexity are standalone checks. The full engine is unnecessary for the first version.
- Architectural design written and self-reviewed for scope, API ownership, Server/WebAssembly compatibility, cancellation/disposal, bounded transport and qualification.
- Runtime implementation and qualification have not started. Await written-spec review under the brainstorming skill.

No production support, published package, passing adapter test count or measured browser payload is claimed. Existing experiment results describe the older broad Power bundle, not this proposed three-check adapter.
