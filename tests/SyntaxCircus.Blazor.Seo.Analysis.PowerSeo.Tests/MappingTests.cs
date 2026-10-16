using SyntaxCircus.Blazor.Seo.Analysis.PowerSeo;
using SyntaxCircus.Seo.Analysis;
using Xunit;
public sealed class MappingTests
{
    private static AnalysisResponse Response(params RawAssessment[] items) => new(1,"request","snapshot","1.0.19","Ready",items);
    [Theory] [InlineData("poor",0,FindingSeverity.Warning)] [InlineData("ok",3,FindingSeverity.Suggestion)]
    [InlineData("good",5,FindingSeverity.Information)] [InlineData("na",0,FindingSeverity.Information)]
    public void Status_not_numeric_score_controls_advice(string status,double score,FindingSeverity expected)
    {
        var result=PowerSeoResultMapper.Map(Response(new RawAssessment("word-complexity",status,score,"Writing.")),"request","snapshot");
        Assert.Equal(CoverageState.Complete,result.State);
        var item=Assert.Single(result.Assessments);
        Assert.Equal(expected,item.Severity);Assert.Equal(score,item.Score);
        Assert.Equal("writing.word-complexity",item.SemanticKey);
    }
    [Fact] public void Unknown_rules_remain_unmapped_diagnostics()
    {
        var result=PowerSeoResultMapper.Map(Response(new RawAssessment("future","poor",0,"New.")),"request","snapshot");
        Assert.Null(Assert.Single(result.Assessments).SemanticKey);
    }
    [Fact] public void Correlation_and_version_mismatches_withhold_advice()
    {
        Assert.Equal(CoverageState.Stale,PowerSeoResultMapper.Map(Response() with {RequestId="other"},"request","snapshot").State);
        Assert.Equal(CoverageState.Stale,PowerSeoResultMapper.Map(Response() with {SnapshotId="other"},"request","snapshot").State);
        Assert.Equal(CoverageState.Partial,PowerSeoResultMapper.Map(Response() with {EngineVersion="2"},"request","snapshot").State);
    }
    [Fact] public void Malformed_or_excessive_results_fail_closed()
    {
        foreach(var response in new[]{Response(),Response(new RawAssessment("word-complexity","unknown",0,"Bad.")),Response() with {Assessments=null!},Response(new RawAssessment("word-complexity","poor",double.NaN,"Bad.")),Response(new RawAssessment("word-complexity","poor",0,new string('x',2001)))})
            Assert.Equal(CoverageState.Failed,PowerSeoResultMapper.Map(response,"request","snapshot").State);
    }
    [Theory] [InlineData(0)] [InlineData(-1)] [InlineData(61)]
    public void Invalid_timeout_is_rejected(int seconds)
        => Assert.Throws<ArgumentOutOfRangeException>(()=>new PowerSeoOptions{Timeout=TimeSpan.FromSeconds(seconds)}.Validate());
}
