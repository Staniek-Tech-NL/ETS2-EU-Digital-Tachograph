# English portfolio descriptions

Each entry has three reusable lengths. Character counts can vary slightly when
a platform normalizes whitespace.

## 1. ETS2 EU Digital Tachograph — Full Application

### Short (~300 characters)

```text
A Windows desktop tachograph simulator that converts live Euro Truck Simulator 2 telemetry into auditable driver history, driving-time counters, planning tools, and PDF/CSV/JSON reports. Built with C++, C#/.NET 9, WPF, SQLite, EF Core, automated tests, and GitHub Actions.
```

### Medium (~600 characters)

```text
ETS2 EU Digital Tachograph is a local-first Windows application that turns live game telemetry into auditable activity history for two virtual drivers. A native C++ plugin publishes a versioned shared-memory protocol consumed by a layered .NET 9/WPF application. The system handles game-time rollbacks, driving and rest counters, manual history reconstruction, SQLite persistence, overlays, planning, and PDF/CSV/JSON exports. The release workflow includes automated backups, diagnostics, protocol checks, GitHub Actions, and a 571-test release gate. It is an ETS2 simulator, not a certified real-world tachograph.
```

### Extended (~1200 characters)

```text
ETS2 EU Digital Tachograph is a Windows simulator that converts live Euro Truck Simulator 2 telemetry into auditable driver history, driving-time analysis, planning tools, and reports. It combines a native C++ plugin using SCS SDK with a layered C#/.NET 9 and WPF application.

The main challenge was temporal reliability. ETS2 time can jump forwards, move backwards, or restart at a world boundary. I designed a versioned shared-memory protocol and a session-based truncate-and-append model that prevents abandoned future data from being counted twice. The canonical timeline feeds two driver-card engines, rule evaluation, manual gap reconstruction, Journey Planner, SQLite persistence, and reporting.

The product also includes independent overlays, Polish and English localization, migration backups, diagnostics, protocol mismatch detection, PDF/CSV/JSON and checksum-protected session exports, and GitHub Actions. A 571-test release gate covers domain logic, telemetry, services, persistence, reports, and UI behavior. The project demonstrates native/managed integration, temporal modelling, desktop UX, transactions, testing, and release discipline. It is a simulator, not a certified tachograph.
```

## 2. Journey Planner — Driving & Rest Planning Module

### Short (~300 characters)

```text
A constraint-based module within ETS2 EU Digital Tachograph that converts the current driver state into an explainable timeline of driving, breaks, rests, calendar waits, and delivery margin. Built with deterministic C# domain logic, immutable snapshots, bounded calculations, WPF, and regression tests.
```

### Medium (~600 characters)

```text
Journey Planner is a module within ETS2 EU Digital Tachograph that evaluates whether a virtual driver can complete a route inside a delivery window while respecting the implemented driving and rest constraints. It captures an immutable snapshot of canonical history and rule state, then produces an explainable sequence of driving, breaks, daily or weekly rests, calendar waits, and post-arrival work. Snapshot identity checks reject stale results when telemetry, cards, sessions, or gaps change. Explicit calculation limits prevent unbounded searches. The module demonstrates algorithmic business logic, safe state transitions, WPF presentation, and cross-layer tests.
```

### Extended (~1200 characters)

```text
Journey Planner is a constraint-based module within ETS2 EU Digital Tachograph. It answers a question that route duration alone cannot solve: can the virtual driver complete the journey inside the delivery window while respecting driving, break, daily-rest, weekly-rest, and calendar constraints?

The application captures an immutable snapshot containing canonical history, rule evaluation, unresolved gaps, game time, world generation, and session identity. A deterministic engine advances through Drive, Break, Daily Rest, Weekly Rest, Calendar Wait, and Other Work segments. Each segment records its reason; the result exposes arrival, completion, deadline margin, warnings, confidence, and limit usage.

Planning is read-only and bounded. Before presenting a result, the application checks whether the snapshot is current; card swaps, session changes, telemetry movement, or resolved gaps produce a stale status. Segment, elapsed-time, and visited-state limits prevent unbounded calculations. The module demonstrates algorithmic modelling, immutable state, explainable output, separation from WPF presentation, and regression testing. The planner image is a UI mockup; its PDF-export control is not claimed as shipped.
```

## 3. Activity Gap Reconstruction & Manual Entry System

### Short (~300 characters)

```text
A validated editing workflow within ETS2 EU Digital Tachograph for reconstructing missing activity intervals. Users build complete rest, work, or availability segments, while service-level validation and an atomic SQLite transaction protect canonical history, provenance, retries, and rule recalculation.
```

### Medium (~600 characters)

```text
Activity Gap Reconstruction is a module within ETS2 EU Digital Tachograph for periods where a removed card, unavailable telemetry, or a time jump leaves missing activity. The WPF editor supports quick whole-gap choices and precise multi-segment plans for rest, other work, and availability. It prevents uncovered minutes, overlaps, invalid ranges, and unsupported activities. The service validates the plan again against canonical history, then persists the resolution and linked source records in one SQLite transaction. Identical retries are idempotent; conflicting submissions fail explicitly. The rule state is recalculated from the persisted timeline.
```

### Extended (~1200 characters)

```text
Activity Gap Reconstruction is a data-editing and validation module developed as part of ETS2 EU Digital Tachograph. It handles intervals where a removed driver card, unavailable telemetry, or a forward game-time jump means the application cannot reliably infer activity.

The WPF workflow lets the user classify the whole gap or construct a plan from rest, other-work, and availability segments. It supports range replacement, editing, splitting, deletion, and live coverage. Confirmation remains unavailable until every minute is covered exactly once.

Correctness does not depend on the UI. The service validates activity types, positive durations, coverage, ordering, overlaps, bounds, canonical-branch membership, and history collisions. Entity Framework Core stores the gap and linked records in one SQLite transaction. Identical retries are idempotent; a different second submission returns a conflict. Records retain ManualEntry provenance and source-gap identity. After persistence, canonical history is reloaded and rules are recalculated. The module demonstrates complex editing UX, defense-in-depth validation, temporal integrity, transactions, and robust retry behavior.
```

## 4. Reporting, Analytics & Export Pipeline

### Short (~300 characters)

```text
A reporting module within ETS2 EU Digital Tachograph that turns canonical activity history into readable analytics, completeness evidence, violations, and PDF/CSV/JSON exports. It keeps precise diagnostic data while aggregating adjacent records into compact human-readable report blocks.
```

### Medium (~600 characters)

```text
Reporting & Analytics is a module within ETS2 EU Digital Tachograph that processes canonical driver history for preset or custom game-time ranges. It calculates driving, work, availability, rest, OUT activity, violations, compensation, and explicit completeness evidence. The database keeps detailed records for rules and diagnostics, while the PDF layer collapses adjacent activity into readable blocks. Raw CSV preserves diagnostic precision, VTC JSON exposes structured data, and the checksum-protected session format retains sessions, gaps, and provenance. The module demonstrates temporal aggregation, data-quality modelling, localization, and multi-format serialization.
```

### Extended (~1200 characters)

```text
Reporting & Analytics is a data-processing and export module developed as part of ETS2 EU Digital Tachograph. It transforms canonical driver history into readable summaries, completeness evidence, violations, compensation information, and human- or machine-readable outputs.

An early minute-by-minute PDF exceeded 40 pages for one game day. I separated stored truth from presentation: precise activity remains available for rules and diagnostic CSV, while the report layer collapses adjacent records into blocks and inserts unresolved gaps into the timeline. The PDF stays compact without discarding source detail.

Completeness is explicit: the model exposes activity and gap minutes, coverage, range length, balance, pending rest allocation, and evidence status. Queries use the same canonical branch as the rule engine, so abandoned rollback data does not return. Outputs include localized PDF, raw activity CSV, compensation CSV, VTC JSON, and a SHA-256-protected session envelope. The module demonstrates temporal aggregation, range projection, data-quality modelling, format-specific serialization, localization, and automated verification.
```
