# SCS telemetry plugin

This x64 DLL is loaded directly by Euro Truck Simulator 2. It uses the official SCS
Telemetry API 1.01 and SDK 1.14 headers.

Published channels:

- `game.time` (`u32`) - absolute in-game minute;
- `truck.speed` (`float`) - metres per second;
- `started` / `paused` events;
- `frame_start.timer_restart` used to advance the monotonic world generation;
- `job` configuration and `job.delivered` gameplay events used to mark loading
  and unloading game-time jumps;
- the first frame after telemetry resumes is held back so late cargo events are
  included before the game-time jump is published;
- `frame_end` event used to atomically publish a completed snapshot.

Shared-memory mapping: `Local\ETS2Tachograph.Telemetry.v3` (protocol v3, 32 bytes).

Build requirements: Visual Studio 2022 with MSVC v143 and Windows 10/11 SDK. Select
`Release|x64`. Install the resulting DLL in the game's `bin/win_x64/plugins` folder.
