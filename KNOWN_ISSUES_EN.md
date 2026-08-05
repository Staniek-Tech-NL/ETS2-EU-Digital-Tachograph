# Known limitations and issues

[Polski](KNOWN_ISSUES.md) | [English](KNOWN_ISSUES_EN.md)

> Protocol v3 requires the application and DLL to be updated together. Restart
> ETS2 completely after replacing the plugin; reloading a save does not reload
> the native library.

The current release is `0.1.0-beta.12`, published as a pre-release on
5 August 2026 with an M8 GO decision. Every final M7 smoke-test item passed.
The gate is 570/570 tests and the Release build has 0 errors and 0 warnings.
The beta.11.1 artifact passed its final live-telemetry smoke test on 23 July
2026 and remains available as a historical release.

New defects should be recorded with either the affected released version or
the `local` status and a diagnostic report.

## Fixed in beta.12

- The hot/warm projection fix prevents failure after loading a save more than
  14 game days older. A branch below the threshold atomically invalidates warm
  derived blocks and restores raw records for rebuilding. Overlap diagnostics
  safely fall back to raw projection for databases that were already
  inconsistent. `BackwardBranchProjectionTests` verifies `RegulationState`
  equivalence, no data loss, rebuild, and idempotency.
- The weekly-rest display no longer counts periods down or exposes raw remaining
  time. The shared S1/S2 formatter shows the current `1/6–6/6+` period and a
  fixed rest-start deadline in `game_time`, for example `4/6 (D141 22:55)`.
  Missing reliable anchoring produces `—/6 (—)`.
- The break-target counter no longer uses elapsed time since a UI click.
  `RegulationState.CurrentContinuousBreakMinutes` reports the current continuous
  `BreakOrRest` block after the one-minute rule.
- Dashboard, device, and overlays for both slots show the same state as
  RuleEngine. `41 min reconstructed + 3 min telemetry` produces `00:44`,
  `00:01` remaining, and an in-progress status; minute 45 qualifies the break.
- The dedicated slot-2 break while moving still uses separate
  `CrewTachographEngine` logic and was not changed.
- Detailed fix report: `docs/BUGFIX_REPORT_QUALIFIED_BREAK_COUNTER_2026-07-24.md`.

## Fixed in beta.11.1

- Automatic classification of a 24-hour-or-longer block hosting compensation
  was corrected. RuleEngine produces legal candidates and the user selects the
  block's role.
- A false `ForwardTimeJump` for the second card during a shared crew time jump
  was fixed. Reconstruction preserves only the card's stable activity.
- Two Day 141 reference gaps are corrected auditably as
  `AutomaticCrewReconstruction`; they are not deleted using manual SQL.
- Allocation decisions and their full audit trail survive SQLite restart and
  are available in the UI, PDF, CSV, and JSON.

## Fixed in beta.11

- The simplified weekly-rest compensation model that combined small excesses
  from several later rests was removed. Repayment is now atomic and en bloc,
  assigned to one qualifying rest of at least 9 hours, with a full deadline and
  trace.
- Corrected reference data: Staniek `1253 min / 20:53`, Doboś
  `1192 min / 19:52`. Values `18 min` and `353 min` came from the old algorithm.
- The complete obligation contract is available in DTOs, UI details, PDF, CSV,
  and JSON, and is restored identically after reopening a file-based SQLite
  database.

> Beta.11 was withdrawn before its smoke test. Beta.11.1 is its historical
> successor.

## Telemetry and game time

- A forward time jump, including `g_set_time`, provides no telemetry for the
  skipped period. Jumps up to 2 minutes are reconstructed, a longer confirmed
  rest while parked may be reconstructed, and other cases create an explicit
  data gap.
- Rolling `game_time` back creates a new session and replaces only the
  overlapping future. This is intended behaviour, not deletion of all earlier
  history.
- Frames with `running == 0` are ignored by history and the high-water mark.
  Counters should not advance in a menu or while paused.
- The plugin and application support Windows x64 only. ETS2 must run as
  `win_x64`.

## Tachograph rules

- This is a simulator, not a certified Annex 1C legal implementation. PDF, CSV,
  JSON, and `.tacho` exports are not official real-tachograph files.
- Ferry mode is enabled manually. ETS2 telemetry does not provide a reliable
  event for automatically recognising an entire ferry-rest sequence.
- Ferry mode marks a crossing in data and reports but does not implement the
  Article 9 derogation. Rest interrupted by driving onto or off a ferry remains
  split into separate blocks and is not merged automatically. Article 9
  conditions exist in `FerryRestDerogation` but are not connected to the UI or
  counter engine. Enabling ferry mode neither starts rest nor changes activity:
  Driving, Other work, or Availability remains non-rest time. Full Article 9
  support is planned for post-beta.12 work.
- Trains are not modelled because ETS2 does not expose a useful railway scenario
  for this project.
- Start and end countries are selected manually from a searchable ISO 3166-1
  alpha-2 catalogue. They are not inferred from the truck position. The LCD may
  display the corresponding tachograph code.

## History, retention, and reports

- The cold-retention layer—daily summaries for data older than 365 game
  days—has only an architectural hook and is not implemented.
- There is no explicit command for deleting history older than a selected
  number of game days. The application archives automatically but does not
  delete data.
- Raw diagnostic CSV is intentionally minute-based and may be large. The
  compensation-obligation CSV uses a separate contract with one record per
  obligation. PDF uses collapsed activity blocks and a separate compensation
  table.
- Reconstructed segments are marked `Reconstructed`; blocks containing mixed
  sources are marked `Mixed`.

## Application and distribution

- The required-manual-entry lock controls tachograph state. Official SCS
  telemetry is read-only, so the application cannot physically stop the ETS2
  truck; attempted movement is still recorded as driving.
- `Alt+1`, `Alt+2`, or `Alt+Q` may conflict with other overlays. Disable the
  conflicting shortcut in the other application if necessary.
- Updates are manual: replace both the application and the correct plugin DLL.
- Use **Diagnostic report** when reporting a problem. Local logs are retained
  for 14 days.
