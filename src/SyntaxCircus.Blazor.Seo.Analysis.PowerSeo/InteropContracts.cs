using System.Text.Json.Serialization;
namespace SyntaxCircus.Blazor.Seo.Analysis.PowerSeo;
internal sealed record AnalysisRequest(int ProtocolVersion, string RequestId, string SnapshotId, string Content, string Locale);
internal sealed record RawAssessment(string Id, string Status, [property: JsonRequired] double Score, string Description);
internal sealed record AnalysisResponse(int ProtocolVersion, string RequestId, string SnapshotId, string EngineVersion, string State, RawAssessment[] Assessments, string? Detail = null);
