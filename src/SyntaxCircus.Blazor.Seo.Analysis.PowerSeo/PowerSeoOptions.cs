namespace SyntaxCircus.Blazor.Seo.Analysis.PowerSeo;
public sealed record PowerSeoOptions
{
    public TimeSpan Timeout { get; init; } = TimeSpan.FromSeconds(5);
    internal void Validate()
    {
        if (Timeout <= TimeSpan.Zero || Timeout > TimeSpan.FromMinutes(1))
            throw new ArgumentOutOfRangeException(nameof(Timeout), "Timeout must be positive and at most one minute.");
    }
}
