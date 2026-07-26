using System.IO;
using System.Text.Json;
using ETS2Tachograph.Core.Time;

namespace ETS2Tachograph.Desktop;

public sealed record JourneyPlannerInputState(
    bool IsMarketOffer,
    int SelectedSlot,
    string DriveToPickup,
    string OfferExpiresIn,
    string PickupWork,
    string LoadedDrive,
    GameWeekday WindowStartDay,
    int WindowStartHour,
    int WindowStartMinute,
    GameWeekday WindowEndDay,
    int WindowEndHour,
    int WindowEndMinute,
    string UnloadingWork,
    string PostDeliveryWork,
    string TightMargin,
    string Origin = "user");

public interface IJourneyPlannerInputStateStore
{
    JourneyPlannerInputState? Load();
    void Save(JourneyPlannerInputState state);
}

public sealed class JsonJourneyPlannerInputStateStore(string path) : IJourneyPlannerInputStateStore
{
    public static JsonJourneyPlannerInputStateStore CreateDefault() => new(Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "ETS2Tachograph",
        "journey-planner-inputs.json"));

    public JourneyPlannerInputState? Load()
    {
        if (!File.Exists(path))
            return null;

        return JsonSerializer.Deserialize<JourneyPlannerInputState>(File.ReadAllText(path));
    }

    public void Save(JourneyPlannerInputState state)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, JsonSerializer.Serialize(state));
    }
}
