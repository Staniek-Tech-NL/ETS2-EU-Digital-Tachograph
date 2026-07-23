using System.Security.Cryptography;
using System.Text;
using ETS2Tachograph.Core.Time;

namespace ETS2Tachograph.RuleEngine.Internal;

internal static class CompensationIdentity
{
    public const int SchemeVersion = 1;

    public static string RestBlockId(ActivityRun run)
    {
        using var stream = new MemoryStream();
        using var writer = new BinaryWriter(stream, Encoding.UTF8, leaveOpen: true);
        WriteText(writer, "ETS2TACHO-REST-BLOCK");
        writer.Write(SchemeVersion);
        WriteText(writer, run.DriverCardId);
        writer.Write(run.Start.TotalMinutes);
        writer.Write(run.EndExclusive.TotalMinutes);
        writer.Write(run.SourceRanges.Count);
        foreach (var range in run.SourceRanges.OrderBy(range => range.Start))
        {
            writer.Write(range.Start.TotalMinutes);
            writer.Write(range.EndExclusive.TotalMinutes);
            WriteText(writer, range.SourceGapId?.ToString("N") ?? string.Empty);
        }

        writer.Flush();
        return $"rest-v{SchemeVersion}-{Hash(stream.ToArray())}";
    }

    public static string ObligationId(
        string driverCardId,
        string sourceRestBlockId,
        GameWeek reductionWeek)
    {
        using var stream = new MemoryStream();
        using var writer = new BinaryWriter(stream, Encoding.UTF8, leaveOpen: true);
        WriteText(writer, "ETS2TACHO-COMPENSATION-OBLIGATION");
        writer.Write(SchemeVersion);
        WriteText(writer, driverCardId);
        WriteText(writer, sourceRestBlockId);
        writer.Write(reductionWeek.Index);
        writer.Flush();
        return $"obligation-v{SchemeVersion}-{Hash(stream.ToArray())}";
    }

    public static string RestAllocationCandidateId(
        string restBlockId,
        RestAllocationPurpose purpose,
        int hostMinimumMinutes,
        IReadOnlyList<string> obligationIds,
        int newDebtMinutes)
    {
        using var stream = new MemoryStream();
        using var writer = new BinaryWriter(stream, Encoding.UTF8, leaveOpen: true);
        WriteText(writer, "ETS2TACHO-REST-ALLOCATION-CANDIDATE");
        writer.Write(SchemeVersion);
        WriteText(writer, restBlockId);
        writer.Write((int)purpose);
        writer.Write(hostMinimumMinutes);
        writer.Write(newDebtMinutes);
        writer.Write(obligationIds.Count);
        foreach (var obligationId in obligationIds)
            WriteText(writer, obligationId);
        writer.Flush();
        return $"allocation-v{SchemeVersion}-{Hash(stream.ToArray())}";
    }

    private static void WriteText(BinaryWriter writer, string value)
    {
        var bytes = Encoding.UTF8.GetBytes(value);
        writer.Write(bytes.Length);
        writer.Write(bytes);
    }

    private static string Hash(byte[] value) =>
        Convert.ToHexString(SHA256.HashData(value)).ToLowerInvariant();
}
