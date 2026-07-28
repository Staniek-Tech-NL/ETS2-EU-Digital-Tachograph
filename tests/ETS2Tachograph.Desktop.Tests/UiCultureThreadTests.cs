using System.Globalization;
using ETS2Tachograph.Application.Dtos;
using ETS2Tachograph.Core.Enums;
using ETS2Tachograph.Core.Time;
using ETS2Tachograph.Desktop.Localization;

namespace ETS2Tachograph.Desktop.Tests;

[CollectionDefinition(Name, DisableParallelization = true)]
public sealed class UiCultureSerialCollection
{
    public const string Name = "UI culture serial";
}

[Collection(UiCultureSerialCollection.Name)]
public sealed class UiCultureThreadTests
{
    [Fact]
    public void Ui_resources_use_selected_process_culture_on_a_thread_with_different_culture()
    {
        var originalCulture = UiCulture.Current.Name;
        try
        {
            UiCulture.Apply(UiCulture.EnglishUnitedKingdom, applyWpfLanguage: false);
            string? menuTitle = null;
            var thread = new Thread(() =>
            {
                CultureInfo.CurrentUICulture =
                    CultureInfo.GetCultureInfo(UiCulture.Polish);
                menuTitle = UiStrings.Get("DeviceMenu_MainTitle");
            });

            thread.Start();
            thread.Join();

            Assert.Equal("MAIN MENU", menuTitle);
        }
        finally
        {
            UiCulture.Apply(originalCulture, applyWpfLanguage: false);
        }
    }

    [Fact]
    public void Visible_game_clock_values_use_selected_ui_culture()
    {
        var originalCulture = UiCulture.Current.Name;
        try
        {
            UiCulture.Apply(UiCulture.EnglishUnitedKingdom, applyWpfLanguage: false);
            var history = new HistoryActivityRow(
                Guid.NewGuid(),
                "CARD",
                DriverActivity.Driving,
                new GameTime(1_440),
                new GameTime(1_500),
                ActivitySource.Telemetry,
                SpecialCondition.None);
            var compensation = CompensationDetailRow.From(
                "CARD",
                new WeeklyRestCompensationDto(
                    IdentitySchemeVersion: 1,
                    ObligationId: "obligation",
                    DriverCardId: "CARD",
                    SourceRestBlockId: "rest",
                    SourceRestEndGameMinuteExclusive: 1_440,
                    OriginalOwedMinutes: 300,
                    RemainingMinutes: 300,
                    ReductionWeek: 0,
                    DueAtGameMinuteExclusive: 2_880,
                    PaymentRestBlockId: null,
                    PaymentRange: null,
                    SettledAtGameMinute: null,
                    Status: WeeklyRestCompensationStatusDto.OpenOnTime),
                new GameCalendarResolver(new GameCalendarContext(0)));

            Assert.Equal("Day 2, 00:00", history.StartGameTimeText);
            Assert.Equal("Day 2, 00:00", compensation.SourceRestEndText);
        }
        finally
        {
            UiCulture.Apply(originalCulture, applyWpfLanguage: false);
        }
    }
}
