using System.Diagnostics;
using System.Globalization;
using System.Text;
using ETS2Tachograph.Core.Entities;
using ETS2Tachograph.Core.Settings;
using ETS2Tachograph.Core.Telemetry;
using ETS2Tachograph.Core.Time;
using ETS2Tachograph.Telemetry.Scs;
using ETS2Tachograph.Engine;
using ETS2Tachograph.RuleEngine;

Console.OutputEncoding = Encoding.UTF8;
Console.Title = "ETS2 Tachograph - monitor telemetrii SCS";

using var singleInstanceMutex = new Mutex(
    initiallyOwned: true,
    @"Local\ETS2Tachograph.Telemetry.Monitor.SingleInstance",
    out var ownsSingleInstanceMutex);
if (!ownsSingleInstanceMutex)
{
    Console.Error.WriteLine("Monitor telemetrii ETS2 jest już uruchomiony.");
    Environment.ExitCode = 2;
    return;
}

using var cancellation = new CancellationTokenSource();
Console.CancelKeyPress += (_, eventArgs) =>
{
    eventArgs.Cancel = true;
    cancellation.Cancel();
};

using var reader = new ScsMemoryMappedTelemetryReader();
var settings = new TachographSettings();
var tachographEngine = new TachographEngine("MONITOR-DRIVER", settings);
var stopwatch = Stopwatch.StartNew();
var lastChange = TimeSpan.Zero;
uint? lastSequence = null;
ScsTelemetrySnapshot? lastSnapshot = null;
ActivityRecord? lastClosedRecord = null;
var sessionIndex = 0;
var historyEvent = "--";
RegulationEvaluation? regulation = null;
string? lastError = null;

TrySetCursorVisible(false);
try
{
    while (!cancellation.IsCancellationRequested)
    {
        HandleManualInput(tachographEngine);

        try
        {
            if (reader.TryRead(out var snapshot))
            {
                if (snapshot.Sequence != lastSequence)
                {
                    lastSequence = snapshot.Sequence;
                    lastChange = stopwatch.Elapsed;
                    if (snapshot.Running)
                    {
                        var frame = new TelemetryFrame(
                            new GameTime(snapshot.GameTimeMinutes),
                            DateTimeOffset.UtcNow,
                            Math.Abs(snapshot.SpeedMetersPerSecond) * 3.6,
                            GamePaused: false,
                            WorldGeneration: snapshot.WorldGeneration);
                        var engineSnapshot = tachographEngine.ProcessFrame(frame);
                        lastClosedRecord = engineSnapshot.LastClosedRecord;
                        sessionIndex = engineSnapshot.SessionIndex;
                        historyEvent = engineSnapshot.WorldGenerationChanged
                        ? "WCZYTANIE ŚWIATA - NOWA SESJA"
                        : engineSnapshot.ClockMovedBackward
                        ? "COFNIĘCIE CZASU - NOWA SESJA"
                        : engineSnapshot.GameTimeJumpDetected
                            ? "SKOK CZASU - MINUTY ODTWORZONE"
                            : "--";
                        regulation = engineSnapshot.Regulation;
                    }
                }

                lastSnapshot = snapshot;
                lastError = null;
            }
        }
        catch (Exception exception) when (
            exception is IOException or UnauthorizedAccessException or InvalidOperationException)
        {
            lastError = exception.Message;
        }

        Render(
            lastSnapshot,
            lastChange,
            stopwatch.Elapsed,
            tachographEngine.Current.ProvisionalActivity,
            tachographEngine.Current.ManualActivity,
            tachographEngine.Current.OutModeEnabled,
            tachographEngine.Current.FerryModeEnabled,
            tachographEngine.Current.MultiManningEnabled,
            lastClosedRecord,
            sessionIndex,
            historyEvent,
            regulation,
            lastError);

        try
        {
            await Task.Delay(100, cancellation.Token);
        }
        catch (OperationCanceledException)
        {
            break;
        }
    }
}
finally
{
    TrySetCursorVisible(true);
    Console.WriteLine();
    Console.WriteLine("Monitor zatrzymany.");
}

static void Render(
    ScsTelemetrySnapshot? snapshot,
    TimeSpan lastChange,
    TimeSpan now,
    ETS2Tachograph.Core.Enums.DriverActivity? provisionalActivity,
    ETS2Tachograph.Core.Enums.DriverActivity manualActivity,
    bool outModeEnabled,
    bool ferryModeEnabled,
    bool multiManningEnabled,
    ActivityRecord? lastClosedRecord,
    int sessionIndex,
    string historyEvent,
    RegulationEvaluation? regulation,
    string? error)
{
    var lines = new List<string>
    {
        "ETS2 EU DIGITAL TACHOGRAPH - MONITOR TELEMETRII SCS",
        new('=', 58),
        string.Empty
    };

    if (!string.IsNullOrWhiteSpace(error))
    {
        lines.Add("Połączenie : BŁĄD ODCZYTU");
        lines.Add($"Szczegóły  : {error}");
        lines.Add(string.Empty);
        lines.Add("Sprawdź, czy ETS2 działa i plugin znajduje się w bin\\win_x64\\plugins.");
    }
    else if (snapshot is null)
    {
        lines.Add("Połączenie : OCZEKIWANIE NA PLUGIN");
        lines.Add("Czas gry   : --");
        lines.Add("Prędkość   : --");
        lines.Add("Stan gry   : --");
        lines.Add("Aktywność  : --");
        lines.Add("Ramka      : --");
        lines.Add($"Tryb ręczny: {FormatActivity(manualActivity)}");
        lines.Add($"OUT / PROM : {(outModeEnabled ? "OUT" : "--")} / {(ferryModeEnabled ? "PROM" : "--")}");
        lines.Add($"Obsada     : {(multiManningEnabled ? "PODWÓJNA (30 h)" : "POJEDYNCZA (24 h)")}");
        lines.Add(string.Empty);
        lines.Add("Uruchom ETS2 i wejdź do profilu kierowcy.");
    }
    else
    {
        var value = snapshot.Value;
        var age = now - lastChange;
        var signalFresh = age <= TimeSpan.FromSeconds(2);
        var speedKph = Math.Abs(value.SpeedMetersPerSecond) * 3.6;
        var connection = signalFresh
            ? "AKTYWNE"
            : value.Running
                ? "BRAK NOWYCH DANYCH"
                : "PAUZA / MENU";

        lines.Add($"Połączenie : {connection}");
        lines.Add($"Czas gry   : {FormatGameTime(value.GameTimeMinutes)}");
        lines.Add($"Prędkość   : {speedKph.ToString("0.0", CultureInfo.GetCultureInfo("pl-PL"))} km/h");
        lines.Add($"Stan gry   : {(value.Running ? "URUCHOMIONA" : "PAUZA")}");
        lines.Add($"Aktywność  : {FormatActivity(provisionalActivity)}");
        lines.Add($"Tryb ręczny: {FormatActivity(manualActivity)}");
        lines.Add($"OUT / PROM : {(outModeEnabled ? "OUT" : "--")} / {(ferryModeEnabled ? "PROM" : "--")}");
        lines.Add($"Obsada     : {(multiManningEnabled ? "PODWÓJNA (30 h)" : "POJEDYNCZA (24 h)")}");
        lines.Add($"Minuta tach.: {FormatClosedMinute(lastClosedRecord)}");
        lines.Add($"Sesja      : {sessionIndex + 1}");
        lines.Add($"Zdarzenie  : {historyEvent}");
        lines.Add($"Ramka      : {value.Sequence}");
        lines.Add($"Wiek danych: {age.TotalSeconds.ToString("0.0", CultureInfo.GetCultureInfo("pl-PL"))} s");

        if (regulation is not null)
        {
            lines.Add(string.Empty);
            lines.Add("LICZNIKI 561/2006");
            lines.Add($"JAZDA CIĄGŁA  : {FormatMinutes(regulation.State.ContinuousDrivingMinutes)} / 04:30");
            lines.Add($"DO PRZERWY    : {FormatRemaining(regulation.State.MinutesUntilBreak)}");
            lines.Add($"Jazda dzienna: {FormatMinutes(regulation.State.DailyDrivingMinutes)} / 09:00");
            lines.Add($"Jazda tydzień: {FormatMinutes(regulation.State.WeeklyDrivingMinutes)} / 56:00");
            lines.Add($"Dwa tygodnie : {FormatMinutes(regulation.State.FortnightlyDrivingMinutes)} / 90:00");
            lines.Add($"Do odp. dz.  : {FormatRemaining(regulation.State.MinutesUntilDailyRestDeadline)}");
            lines.Add($"Do odp. tyg. : {FormatRemaining(regulation.State.MinutesUntilWeeklyRestDeadline)}");
            lines.AddRange(FormatViolations(regulation.Violations));
        }
    }

    lines.Add(string.Empty);
    lines.Add("[1] PRACA [2] GOTOWOŚĆ [3] ODPOCZYNEK [4] OUT [5] PROM [6] OBSADA");
    lines.Add("Jazda włącza DRIVING automatycznie | Ctrl+C - zakończ");
    WriteScreen(lines);
}

static string FormatGameTime(uint totalMinutes)
{
    var day = (totalMinutes / GameWeek.MinutesPerDay) + 1;
    var minuteOfDay = totalMinutes % GameWeek.MinutesPerDay;
    var hour = minuteOfDay / 60;
    var minute = minuteOfDay % 60;
    return $"dzień {day}, {hour:00}:{minute:00} ({totalMinutes} min)";
}

static string FormatActivity(ETS2Tachograph.Core.Enums.DriverActivity? activity) => activity switch
{
    ETS2Tachograph.Core.Enums.DriverActivity.Driving => "DRIVING / PROWADZENIE",
    ETS2Tachograph.Core.Enums.DriverActivity.OtherWork => "OTHER WORK / INNA PRACA",
    ETS2Tachograph.Core.Enums.DriverActivity.Availability => "AVAILABILITY / GOTOWOŚĆ",
    ETS2Tachograph.Core.Enums.DriverActivity.BreakOrRest => "BREAK OR REST / ODPOCZYNEK",
    null => "BRAK - GRA WSTRZYMANA",
    _ => activity.ToString() ?? "--"
};

static string FormatClosedMinute(ActivityRecord? record) => record is null
    ? "oczekiwanie na domknięcie"
    : $"{record.Start.TotalMinutes} = {FormatActivity(record.Activity)} [{record.Source}]";

static string FormatMinutes(long minutes) => $"{minutes / 60:00}:{Math.Abs(minutes % 60):00}";

static string FormatRemaining(long minutes) => minutes >= 0
    ? FormatMinutes(minutes)
    : $"PRZEKROCZONO O {FormatMinutes(-minutes)}";

static IReadOnlyList<string> FormatViolations(IReadOnlyList<RuleViolation> violations)
{
    if (violations.Count == 0)
    {
        return ["Naruszenia   : BRAK"];
    }

    var lines = new List<string> { $"NARUSZENIA ({violations.Count}):" };
    lines.AddRange(violations.Select(violation =>
        $"! {FormatViolationType(violation.Type)} | {violation.Article}" +
        (violation.ExcessMinutes > 0
            ? $" | przekroczenie {FormatMinutes(violation.ExcessMinutes)}"
            : string.Empty)));
    return lines;
}

static string FormatViolationType(ViolationType type) => type switch
{
    ViolationType.ContinuousDrivingExceeded => "przekroczona jazda ciągła",
    ViolationType.MissingRequiredBreak => "brak wymaganej przerwy",
    ViolationType.DailyDrivingExceeded => "przekroczona jazda dzienna",
    ViolationType.WeeklyDrivingExceeded => "przekroczona jazda tygodniowa",
    ViolationType.FortnightlyDrivingExceeded => "przekroczona jazda dwutygodniowa",
    ViolationType.TooManyDailyExtensions => "za dużo wydłużeń dziennych",
    ViolationType.DailyRestMissing => "brak odpoczynku dziennego",
    ViolationType.TooManyReducedDailyRests => "za dużo skróconych odpoczynków",
    ViolationType.WeeklyRestMissing => "brak odpoczynku tygodniowego",
    ViolationType.WeeklyRestPatternInvalid => "nieprawidłowy układ odpoczynków",
    ViolationType.WeeklyRestCompensationOverdue => "zaległa rekompensata odpoczynku",
    _ => type.ToString()
};

static void HandleManualInput(TachographEngine engine)
{
    try
    {
        if (Console.IsInputRedirected || !Console.KeyAvailable)
        {
            return;
        }

        var key = Console.ReadKey(intercept: true).Key;
        var activity = key switch
        {
            ConsoleKey.D1 or ConsoleKey.NumPad1 or ConsoleKey.F1 =>
                ETS2Tachograph.Core.Enums.DriverActivity.OtherWork,
            ConsoleKey.D2 or ConsoleKey.NumPad2 or ConsoleKey.F2 =>
                ETS2Tachograph.Core.Enums.DriverActivity.Availability,
            ConsoleKey.D3 or ConsoleKey.NumPad3 or ConsoleKey.F3 =>
                ETS2Tachograph.Core.Enums.DriverActivity.BreakOrRest,
            _ => engine.Current.ManualActivity
        };

        if (key is ConsoleKey.D1 or ConsoleKey.NumPad1 or ConsoleKey.F1 or
            ConsoleKey.D2 or ConsoleKey.NumPad2 or ConsoleKey.F2 or
            ConsoleKey.D3 or ConsoleKey.NumPad3 or ConsoleKey.F3)
        {
            engine.SetManualActivity(activity);
        }
        else if (key is ConsoleKey.D4 or ConsoleKey.NumPad4 or ConsoleKey.F4)
        {
            engine.SetOutMode(!engine.Current.OutModeEnabled);
        }
        else if (key is ConsoleKey.D5 or ConsoleKey.NumPad5 or ConsoleKey.F5)
        {
            engine.SetFerryMode(!engine.Current.FerryModeEnabled);
        }
        else if (key is ConsoleKey.D6 or ConsoleKey.NumPad6 or ConsoleKey.F6)
        {
            engine.SetMultiManning(!engine.Current.MultiManningEnabled);
        }
    }
    catch (InvalidOperationException)
    {
    }
    catch (IOException)
    {
    }
}

static void WriteScreen(IReadOnlyList<string> lines)
{
    try
    {
        Console.SetCursorPosition(0, 0);
        var width = Math.Max(20, Console.WindowWidth - 1);
        var height = Math.Max(lines.Count, Console.WindowHeight - 1);
        for (var index = 0; index < height; index++)
        {
            var line = index < lines.Count ? lines[index] : string.Empty;
            if (line.Length > width)
            {
                line = line[..width];
            }

            Console.Write(line.PadRight(width));
            if (index < height - 1)
            {
                Console.WriteLine();
            }
        }
    }
    catch (IOException)
    {
        foreach (var line in lines)
        {
            Console.WriteLine(line);
        }
    }
}

static void TrySetCursorVisible(bool visible)
{
    try
    {
        Console.CursorVisible = visible;
    }
    catch (IOException)
    {
    }
}
