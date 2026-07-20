namespace ETS2Tachograph.Core.Ferry;

/// <summary>Conservative ETS2 v1.0 implementation of the ferry-only Article 9 derogation.</summary>
public static class FerryRestDerogation
{
    public static FerryRestAssessment Evaluate(FerryRestRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (request.Interruptions.Count > 2)
        {
            return FerryRestAssessment.Invalid("A rest may be interrupted at most twice.");
        }

        if (request.Interruptions.Any(duration => duration <= TimeSpan.Zero))
        {
            return FerryRestAssessment.Invalid("Every interruption must have a positive duration.");
        }

        if (request.Interruptions.Sum(duration => duration.Ticks) > TimeSpan.FromHours(1).Ticks)
        {
            return FerryRestAssessment.Invalid("Interruptions may not exceed 60 minutes in total.");
        }

        if (!request.HasSleepingFacility)
        {
            return FerryRestAssessment.Invalid("A sleeping facility must be available.");
        }

        var requiredRest = request.RestType switch
        {
            FerryRestType.RegularDaily => TimeSpan.FromHours(11),
            FerryRestType.ReducedWeekly => TimeSpan.FromHours(24),
            FerryRestType.RegularWeekly => TimeSpan.FromHours(45),
            _ => TimeSpan.Zero
        };

        if (requiredRest == TimeSpan.Zero)
        {
            return FerryRestAssessment.Invalid("This rest type is not covered by the project derogation.");
        }

        if (request.RestType == FerryRestType.RegularWeekly &&
            request.ScheduledCrossingDuration < TimeSpan.FromHours(8))
        {
            return FerryRestAssessment.Invalid("A regular weekly rest requires a scheduled crossing of at least 8 hours.");
        }

        if (request.RestExcludingInterruptions < requiredRest)
        {
            return FerryRestAssessment.Invalid("Rest excluding interruptions is shorter than the required duration.");
        }

        return FerryRestAssessment.Valid();
    }
}
