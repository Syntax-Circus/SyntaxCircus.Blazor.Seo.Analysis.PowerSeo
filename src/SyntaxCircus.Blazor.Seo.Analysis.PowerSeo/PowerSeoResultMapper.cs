using SyntaxCircus.Seo.Analysis;
using System.Collections.Immutable;
namespace SyntaxCircus.Blazor.Seo.Analysis.PowerSeo;
internal static class PowerSeoResultMapper
{
    internal const string Provider = "power-seo";
    internal const string EngineVersion = "1.0.19";
    internal const int ProtocolVersion = 1;
    internal const int MaximumMessageCharacters = 2000;
    internal const int MaximumAssessments = 3;
    internal const int MaximumIdCharacters = 64;
    internal static CompanionResult Map(AnalysisResponse? response, string requestId, string snapshotId)
    {
        CompanionResult Empty(CoverageState state, string detail) => new(snapshotId, Provider, EngineVersion, state, [], detail);
        if (response is null) return Empty(CoverageState.Failed, "Missing worker result.");
        if (response.RequestId != requestId || response.SnapshotId != snapshotId)
            return Empty(CoverageState.Stale, "Worker correlation mismatch.");
        if (response.ProtocolVersion != ProtocolVersion || response.EngineVersion != EngineVersion)
            return Empty(CoverageState.Partial, "Unsupported worker protocol or engine; assessments withheld.");
        var state = response.State switch {
            "Ready" => CoverageState.Complete, "TimedOut" => CoverageState.TimedOut,
            "Unavailable" => CoverageState.Unavailable, _ => CoverageState.Failed
        };
        if (state != CoverageState.Complete) return Empty(state, "Browser companion did not complete.");
        if (response.Assessments is null || response.Assessments.Length > MaximumAssessments ||
            response.Assessments.Any(x => x is null || string.IsNullOrWhiteSpace(x.Id) || x.Id.Length > MaximumIdCharacters ||
                x.Description is null || x.Description.Length > MaximumMessageCharacters || !double.IsFinite(x.Score) ||
                x.Status is not ("good" or "na" or "ok" or "poor")))
            return Empty(CoverageState.Failed, "Malformed or excessive worker result.");
        return new(snapshotId, Provider, EngineVersion, state, response.Assessments.Select(x =>
            new CompanionAssessment(x.Id, x.Id switch {
                "subheading-distribution" => "writing.subheading-distribution",
                "transition-words" => "writing.transition-words",
                "word-complexity" => "writing.word-complexity",
                _ => null
            }, FindingCategory.Readability, x.Status switch {
                "poor" => FindingSeverity.Warning, "ok" => FindingSeverity.Suggestion, _ => FindingSeverity.Information
            }, x.Description, "Review this suggestion in context; keep natural wording.", Score: x.Score)).ToImmutableArray());
    }
}
