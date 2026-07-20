namespace ETS2Tachograph.Core.Ferry;

public sealed record FerryRestAssessment(bool IsValid, string Reason)
{
    public static FerryRestAssessment Valid() => new(true, "The ferry rest derogation is satisfied.");
    public static FerryRestAssessment Invalid(string reason) => new(false, reason);
}
