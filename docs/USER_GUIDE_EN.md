# Basic user guide - ETS2 Digital Tachograph

[Polski](USER_GUIDE_PL.md) | [English](USER_GUIDE_EN.md)

## Before driving

1. Start ETS2 and load a profile.
2. Run `ETS2Tachograph.Desktop.exe`.
3. Wait for an active telemetry connection.
4. On the Dashboard, select a card slot and insert the driver's card. Enter the
   start country if prompted.

Do not drive without the correct card in the active slot. The application
detects driving automatically from vehicle speed.

## Dashboard and virtual tachograph

The Dashboard shows both slots, current activity, time to break, driving limits,
and compensation obligations. Its quick actions can refresh the report, create
a PDF, export the obligations CSV, and save a diagnostic report.

On the virtual tachograph:

- use the arrow buttons to move through the menu;
- use `OK` to confirm;
- select other work, availability, or rest when the activity is not driving;
- enable OUT and ferry modes only when they match the actual session;
- stop the vehicle and select the end country before ejecting a card.

## Breaks, rests, and the co-driver

Each slot lets you select a rest target and start a break. A moving break is
available for the co-driver when the current state allows it. RuleEngine remains
the source of qualification; selecting a target alone does not guarantee that
a break will qualify.

## History and manual entry

The **History** view shows activities and gaps for the selected card. If an
unresolved gap appears after the card is inserted again:

1. open the manual-entry editor;
2. assign the whole missing interval to rest, other work, or availability;
3. verify that the segments cover the gap without overlaps or empty periods;
4. confirm the entry.

This view also imports `.tacho` files and exports a driver's session. Do not
edit `.tacho` files manually.

## Compensations

The **Compensations** view shows open, overdue, and completed obligations.
Review each deadline and the assigned rest segments. Status is derived from the
recorded history; it is not set manually in the interface.

## Reports and exports

In **Reports**:

1. select a driver and time range;
2. refresh the analysis;
3. review the summary, activities, infringements, gaps, and compensations;
4. export a PDF, raw CSV, compensation CSV, or VTC JSON.

The PDF uses the active application language. The technical CSV, JSON, and
`.tacho` contracts do not change when the interface language changes.

## Journey planner

The Journey Planner builds journey variants from the current card state,
limits, breaks, rests, and compensations. Treat the result as a simulation:

- resolve reported gaps or a missing card before planning;
- review plan readiness and all warnings;
- generate the plan again after changing history or settings;
- do not treat the result as legal advice.

## Overlays

- `Alt+1` - show or hide the slot 1 overlay;
- `Alt+2` - show or hide the slot 2 overlay;
- `Alt+Q` - additional shortcut for slot 1.

Drag an overlay by its top bar. The `S1` and `S2` positions are stored
separately.

## Language and settings

**Settings** contains the driving-detection threshold, regulatory week offset,
and interface language. A saved language change takes effect after the
application restarts.

## Reporting a problem

1. Save a diagnostic report from the Dashboard.
2. Record the application version, ETS2 version, and steps that caused the
   problem.
3. Attach the diagnostic ZIP and a screenshot, but do not publish private
   driver data.

This program is a simulator and is not a certified tachograph.
