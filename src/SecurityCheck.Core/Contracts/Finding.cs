namespace SecurityCheck.Core.Contracts;
public sealed record Finding(
    string Title,
    Severity Severity,
    string Explanation,
    string Recommendation
);