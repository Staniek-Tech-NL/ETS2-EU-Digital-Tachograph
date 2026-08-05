# ETS2 EU Digital Tachograph 0.1.0-beta.12 — M8 GO release

[Polski](RELEASE_NOTES.md) | [English](RELEASE_NOTES_EN.md)

The final beta before the first broad public release. The immutable ZIP
received M7 GO and was published as a GitHub pre-release with an M8 GO decision
on 5 August 2026.

## Highlights

- complete `pl-PL` and `en-GB` UI, language selection applied after restart,
  and localised PDF reports;
- Journey Planner supporting two drivers, events, game-time deadlines,
  warnings, and persisted form state;
- expanded Reports and statistics, PDF/CSV/JSON exports, and readable
  compensation obligations;
- manual-entry variant B with a complete gap plan, segment editing, and
  protection against incomplete submission;
- catalogue of 249 ISO 3166-1 countries with PL/EN names and tachograph codes;
- finalised Dashboard, virtual device, both overlays, and error states under
  the UI freeze;
- corrected 44/45-minute break counter and weekly-rest presentation;
- faster startup on an existing database and a hot/warm projection fix after
  loading an older game save;
- localised visible game-clock values (`Dzień` / `Day`) in History,
  Compensation, and manual entry.

## Candidate verification

- 570/570 automated tests;
- Release build: 0 errors, 0 warnings;
- `FileVersion 0.1.12.0`;
- M5.2-P correctness and performance checkpoint: GO;
- clean and existing database startup/restart: PASS;
- complete PL/EN validation: GO;
- M6 ZIP SHA-256:
  `A2B8F949E100F8683225B7A0D5A76E5C7E3434AD95AEC9596006C4A5E41F5E78`;
- final M7 smoke: GO — all items passed, no P0/P1.

Country names use Unicode CLDR data. Required licensing information is in
`docs/THIRD_PARTY_NOTICES.md`.

---

# ETS2 EU Digital Tachograph 0.1.0-beta.11.1

Corrective release replacing the withdrawn beta.11 candidate.

- a completed 24-hour-or-longer block receives legal accounting candidates;
  the user selects `CandidateId` without manually splitting minutes;
- the daily variant with compensation uses a 9-hour base, while assigning the
  entire block as weekly preserves reduced-weekly-rest consequences;
- the same minutes cannot simultaneously form the rest base and repay
  compensation; repayment remains complete and en bloc;
- decisions are persistent, versioned, and audited in SQLite; changing a
  decision preserves `Superseded`, and changing canonical `RestBlockId`
  invalidates the previous selection;
- a shared multi-manning time jump is classified once per vehicle. Stable
  `BreakOrRest`, `OtherWork`, and `Availability` are reconstructed symmetrically
  without inventing Driving;
- two Day 141 reference gaps were corrected on a database copy without manual
  SQL. The original trace remains and correction records use
  `AutomaticCrewReconstruction`;
- the UI displays RuleEngine variants, while PDF, CSV, and JSON include the
  complete allocation trace. An unresolved choice marks the report incomplete.

## Verification

- 282/282 automated tests;
- RuleEngine 62/62, Engine 69/69, Application 50/50, Reports 9/9,
  Infrastructure 51/51;
- Release build: 0 errors, 0 warnings;
- migration and two restarts checked on a copy of the real database;
- after restart: exactly two audited reconstruction records and no unresolved
  reference gaps;
- final live-telemetry field smoke completed on 23 July 2026;
- every smoke-checklist item passed;
- release decision: **GO** for `0.1.0-beta.11.1`.

---

# ETS2 EU Digital Tachograph 0.1.0-beta.11 — WITHDRAWN CANDIDATE

This package did not proceed to the final smoke test because of incorrect
allocation of an ambiguous 24-hour-or-longer rest and false gaps during a
shared crew time jump.

Complete reduced-weekly-rest compensation model:

- debt is created only after a canonical reduced weekly rest closes and equals
  `45 h - rest duration`;
- the obligation is assigned to the reduction week, has a strict
  `DueAtExclusive` deadline, and uses `OpenOnTime`, `Overdue`, `PaidOnTime`, or
  `PaidLate` status;
- repayment occurs only en bloc through one qualifying rest of at least
  9 hours; fragments from several rests are never combined;
- multiple obligations are handled deterministically by deadline FIFO and
  stable tie-breakers;
- the complete trace includes `ObligationId`, source rest, original and
  remaining debt, repayment block/range, and `SettledAt`;
- Dashboard and overlays display layered summaries, while the Compensation tab
  displays complete obligations for both cards;
- PDF includes an obligation table, CSV writes one record per obligation, and
  JSON exposes complete `CompensationObligations`; the former summary remains a
  derived compatibility projection;
- contract stability is verified after closing and reopening a file-based
  SQLite database.

## Reference data

- Staniek: `1253 min` (`20:53`) open debt;
- Doboś: `1192 min` (`19:52`) open debt;
- previous `18 min` and `353 min` values resulted from illegal aggregation of
  excess fragments and are no longer used.

## Compatibility

- SCS plugin protocol unchanged at version 3;
- no new EF Core migration and no requirement to clear user data;
- regulatory state remains a projection of canonical history, never a second
  source of truth;
- raw minute CSV remains available for diagnostics and retention.

## Verification

- 262/262 automated tests;
- RuleEngine 55/55, Application 45/45, Reports 9/9, Infrastructure 48/48;
- reference tests for both drivers and matrices for threshold, FIFO, deadline,
  and restart;
- Release build without errors or warnings;
- final live-telemetry smoke remained pending for this withdrawn candidate.

---

# ETS2 EU Digital Tachograph 0.1.0-beta.10.1

Hotfix for beta.10.

- fixed application startup failure caused by overlapping canonical activity
  history records;
- a new session adds only previously uncovered fragments to older history;
- manual entries backfilling earlier gaps are preserved in full;
- overlaps are detected early with clear diagnostics instead of a database
  failure;
- corrected warm-block archiving for cards containing duplicated minutes.

## Compatibility

- SCS plugin protocol unchanged at version 3;
- no change requires clearing or migrating user data;
- retain the existing database; the application still backs it up before
  migration.

## Verification

- 239 automated tests;
- 14 canonical-projection regression tests, including cases reproduced from
  field data for both cards;
- database-copy control: no overlaps or duplicate starts, idempotent
  `ArchiveWarmAsync`, and intact manual-entry backfill;
- Release build without warnings.

---

# ETS2 EU Digital Tachograph 0.1.0-beta.10

Rest-continuity correction after card removal and reinsertion.

- removing a card still opens an explicit gap and stops automatic recording;
- after accounting for missing time as Break/Rest, adjacent rest sections are
  treated as one continuous block;
- continuity works in both directions: rest before removal and after
  reinsertion;
- manual entries preserve `SourceGapId`, so the audit trail remains intact;
- Other work, Availability, an unresolved gap, or non-contiguous minutes still
  break the rest.

## Verification

- 225 automated tests;
- regressions for `2 h + 7 h = 9 h`, `7 h + 2 h = 9 h`, and rest on both sides
  of a resolved gap;
- complete flow: remove card → insert card → manual entry → daily reset;
- WPF built without errors or warnings.

---

# ETS2 EU Digital Tachograph 0.1.0-beta.9

Fix for losing the selected activity during loading and unloading.

- the beta.8 plugin correctly passed the cargo-operation marker, but the engine
  cleared the remembered activity after receiving `GamePaused`;
- pre-pause activity is now stored separately and used only to classify
  confirmed loading or unloading time;
- operation time preserves the tachograph selection: Other work, Availability,
  or Break/Rest;
- an ordinary `g_set_time` jump without an operation marker still creates a
  gap;
- pause/menu frames still add no real time to history.

## Verification

- 221 automated tests;
- regression reproducing the real log sequence: activity → pause with cargo
  marker → resume after a 20-minute jump;
- all three manually selected activities verified.

---

# ETS2 EU Digital Tachograph 0.1.0-beta.8

Event-order correction for real loading and unloading.

- ETS2 may send the first advanced-time frame before `cargo.loaded` changes;
  the plugin holds the first frame after resuming;
- if cargo confirmation arrives one frame later, the engine retracts the fresh
  gap and replaces it with the selected activity;
- gap removal and reconstructed-minute persistence are one write set;
- the optional wizard closes if the gap was automatically retracted;
- the diagnostic log records cargo-operation marker changes.

## Verification

- 218 automated tests;
- exact ETS2 ordering regression: time jump followed by
  `cargo.loaded=true` on the next frame;
- x64 plugin and WPF built without errors or warnings.

---

# ETS2 EU Digital Tachograph 0.1.0-beta.7

Urgent plugin startup fix for protocol v3.

- the plugin declares SCS Telemetry API 1.01 required by `gameplay` events;
- removes the `event introduced in 1.1` initialisation error from
  `game.log.txt`;
- retains the beta.6 loading/unloading fix.

---

# ETS2 EU Digital Tachograph 0.1.0-beta.6

Sixth beta-test package. Fixes false gaps during ETS2 loading and unloading
screens.

## Loading and unloading

- the plugin uses official job configuration events and `job.delivered`;
- a controlled loading/unloading time jump no longer creates a gap;
- missing minutes preserve the activity selected before the operation on each
  card: Other work, Availability, or Break/Rest;
- an ordinary large jump, including `g_set_time`, remains a gap;
- telemetry protocol was upgraded to v3, so the beta.6 DLL must replace the v2
  plugin.

## Verification

- 217 automated tests;
- separate regressions for all three activities and both slots;
- x64 plugin built without errors or warnings.

---

# ETS2 EU Digital Tachograph 0.1.0-beta.5

Fifth beta-test package. Adds a working activity-gap list and explicit report
completeness.

## History and gap resolution

- History displays canonical gaps for both cards, newest first;
- only unresolved gaps are shown by default; **Show resolved** exposes the
  complete audit trail;
- an open gap has an in-progress status and updated duration but cannot yet be
  resolved;
- a closed gap can be resolved with the existing optional wizard;
- the list and counter refresh after saving without restart;
- gaps from abandoned time branches do not appear in the working projection.

## Report completeness

- every report is recalculated immediately before export;
- a range containing unresolved gaps displays a warning and **Show gaps**;
- the warning does not block PDF, JSON, or raw CSV export;
- PDF explicitly shows no gaps or the number and duration of gaps plus range
  balance;
- VTC JSON includes `completeness`, gap count/minutes, balance, and
  `evidenceComplete`;
- resolved gaps do not reduce report completeness.

## Further UI improvements

- the main LCD displays game time instead of the Windows clock;
- daily work, extended daily driving, reduced-rest, and compensation counters
  are exposed with their correct reset horizons.

## Verification

- 212 automated tests;
- complete solution build without errors or warnings;
- rendered PDF page checked for gap and balance header.

---

# ETS2 EU Digital Tachograph 0.1.0-beta.4

Fix for a blocking manual-entry wizard error after game-time rollback.

## Fix

- a trimmed gap from the current time branch can be resolved normally;
- the source gap from the abandoned branch remains untouched as an audit trail;
- the resolved fragment is stored in the current session and restored after
  restart;
- repeated submission of an identical entry remains idempotent;
- `.tacho` export preserves the fragment's source-gap link (schema 3).

## Verification

- 193 automated tests;
- SQLite regressions for a trimmed gap and engine restart;
- WPF application builds without warnings.

---

# ETS2 EU Digital Tachograph 0.1.0-beta.3

Third local beta-test package. Introduces explicit activity gaps, manual
entries, and the required wizard after card reinsertion.

## Manual entries

- `CardRemoved` requires an entry and locks tachograph controls until resolved;
- `ForwardTimeJump` remains optional and does not block driving;
- one click records the whole gap as rest, while additional Other-work blocks
  create a mixed entry with no holes;
- the result distinguishes the selected intent from actual rest qualification;
- continuous rest of at least 9 hours resets the daily period retroactively at
  the end of the block; Other work and Availability break continuity.

## Verification

- 191 automated tests in Release configuration;
- self-contained `win-x64` application;
- SCS `Release|x64` plugin, protocol v2.

---

# ETS2 EU Digital Tachograph 0.1.0-beta.2

Second local beta-test package. Application and plugin must be updated together
because shared-memory protocol v2 is not binary-compatible with v1.

## Blocking fixes

- closed minutes are saved under the session they actually belong to;
- the rollback boundary is stored atomically for both cards;
- identical minute re-persistence is recognised by session and minute,
  independently of a random GUID;
- conflicting content for the same minute does not stop telemetry and is logged
  as `ACTIVITY_RECORD_CONFLICT`.

## Protocol v2

- 28-byte structure and `Local\ETS2Tachograph.Telemetry.v2` map;
- `world_generation` incremented by `frame_start.timer_restart`;
- the first generation value is only a reference point;
- a later change creates a shared session boundary for both cards even with
  identical or later game time;
- the application detects a remaining v1 map and requests DLL replacement.

## Verification

- 122 automated tests;
- native `Release|x64` plugin, WPF application, and monitor build without
  warnings.

---

# ETS2 EU Digital Tachograph 0.1.0-beta.1

First versioned package for controlled beta tests.

## Key features

- official SCS SDK 1.14 telemetry and versioned shared-memory protocol v1;
- WPF dashboard, realistic tachograph, two driver cards, and `S1`/`S2`
  overlays;
- driving/rest counters, multi-manning, OUT, ferry, and start/end countries;
- persistent SQLite history, import/export, PDF, raw CSV, VTC JSON, and
  diagnostic ZIP;
- layered hot/warm retention based on `highWaterMark`;
- readable activity blocks and milestones in PDF reports.

## Safeguards

- SQLite backup before every migration;
- protection against a second application or monitor instance;
- explicit incompatible-plugin warning;
- `running == 0` frames ignored by persistence and the high-water mark;
- regressions for `03:53 + 01:34 = 05:27`, block sum equal to clock span, and
  single-card daily-driving reset after 9 hours of rest.

## Before testing

Remove the old plugin DLL from `bin\win_x64\plugins`, copy the DLL from this
package, and restart ETS2 completely. Installation details and known
limitations are available in `README.md` and `KNOWN_ISSUES_EN.md`.
