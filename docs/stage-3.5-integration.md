# Stage 3.5 - integration engine

Stage 3.5 introduces one central application layer between SCS telemetry and the
tachograph rules.

## Data flow

`ScsMemoryMappedTelemetryReader` reads the versioned shared-memory block published by
the native SCS plugin. `ScsTelemetrySource` exposes stable frames through the generic
`ITelemetrySource` contract. `TelemetryProcessor` pumps those frames into
`TachographEngine`.

`TachographEngine` owns the complete live state:

- `ActivityHistoryProcessor` classifies frames and appends closed game minutes to an
  immutable `ActivityTimeline`;
- `RegulationEngine` derives counters and violations from that timeline;
- `TachographSnapshot` exposes the current activity, modes, time discontinuities and
  regulation result to the user interface.

The driver may manually select other work, availability, or rest while stopped.
Movement still selects driving automatically. OUT overrides automatic driving and is
excluded from driving counters. Ferry mode attaches the ferry-crossing condition to
created records. OUT and ferry modes are mutually exclusive. Multi-manning can be
enabled explicitly and changes the daily-rest completion window from 24 to 30 hours.

Paused frames do not advance activity. A forward game-time jump (including
`g_set_time`) is filled with reconstructed minutes using the last known activity. A
backward clock change closes the old append-only session and starts a new one.
Protocol v2 also carries a `world_generation` advanced by the SCS
`frame_start.timer_restart` flag. Its first observed value is only a baseline. A later
change opens one coordinated branch for both card slots, even when `game.time` stays
the same or moves forward. Changes first seen while paused are applied on the first
active frame so the boundary is persisted atomically.

## Verification

Unit tests cover classification, modes, pause and clock discontinuities. The E2E test
creates an actual Windows named memory mapping, writes an SCS protocol frame, then
verifies the whole path:

`shared memory -> SCS reader -> telemetry source -> processor -> engine -> timeline/rules`

This validates the real serialization boundary without requiring ETS2 to run during
the automated test.

## Current limits

- reconstructed time reflects the last known activity because telemetry contains no
  evidence of what happened during the skipped interval;
- ferry is a recorded special condition, not yet a complete ferry-rest derogation
  decision workflow;
- persistence, restart recovery and layered hot/warm retention are implemented;
  the optional cold daily-summary layer remains a future extension.
