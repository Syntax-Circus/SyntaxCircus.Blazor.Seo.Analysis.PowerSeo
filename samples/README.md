# Packaged browser qualification

This anonymous local demonstration consumes the actual adapter NuGet and public core. It is not a production admin interface. Both projects reference the package; neither references the adapter source project.

From the repository root, use a unique local version:

```sh
dotnet pack src/SyntaxCircus.Blazor.Seo.Analysis.PowerSeo -c Release -p:DisableGitVersionTask=true -p:Version=0.0.0-qualification -o artifacts/feed
dotnet build samples/BlazorHost -c Release -p:PowerSeoPackageVersion=0.0.0-qualification -p:RestoreAdditionalProjectSources=ABSOLUTE_PATH_TO_REPO/artifacts/feed
npx playwright install --with-deps chromium
node scripts/run-browser.mjs
```

The runner starts its own loopback host on port5092, exercises root and /demo hosting and closes the host. Server and WebAssembly paths each have two editors with a 750ms debounce, cancellation, generation rejection and DI-owned provider disposal. No analysis starts during prerendering.

The owned24 fixtures are the default corpus. Optionally run `node scripts/import-experiment.mjs PATH_TO_EXPERIMENT_DOCUMENTS_JSON`, then set CORPUS_INPUT to the resulting artifacts/experiment-inputs.json before running the browser script. Imported public article bodies and detailed output stay ignored. Qualification output is written to artifacts/browser*.json.

To view the sample manually, run `dotnet run --project samples/BlazorHost -c Release --no-build -- --urls http://127.0.0.1:5092 --environment Development`. Visit /server or /wasm. The fixture probe and checked-draft diagnostics exist for qualification; they are not package UI.
