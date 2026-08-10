# Platform-neutral portfolio copy

This document contains ready-to-paste English copy for four portfolio entries.
It can be adapted to a personal website, freelance marketplace, professional
profile, or another portfolio platform. All four entries point to one real
repository. Projects 2–4 clearly state that they are modules of the full
application.

Repository:
<https://github.com/Staniek-Tech-NL/ETS2-EU-Digital-Tachograph>

---

## 1. ETS2 EU Digital Tachograph — Full Application

### One-line summary

A Windows desktop tachograph simulator that turns live ETS2 telemetry into
driver activity history, rule-based counters, planning tools, and exportable
reports.

### Project description

ETS2 EU Digital Tachograph is a local-first Windows application built with a
native C++ telemetry plugin and a layered .NET 9/WPF desktop stack. It reads
official SCS telemetry, records two drivers' activities in game time, evaluates
driving, break, daily-rest, weekly-rest, and compensation state, and presents
the result through a dashboard and independent in-game overlays.

The hardest engineering problem was reliable temporal history. ETS2 time can
jump forwards, move backwards, or restart at a world boundary. I designed a
versioned shared-memory protocol and a session-based truncate-and-append history
model that prevents abandoned future data from being counted twice. The same
canonical timeline feeds the rule engine, Journey Planner, manual-entry system,
SQLite persistence, and reporting pipeline.

The release workflow includes automatic database backups before migrations,
diagnostic ZIP generation, protocol mismatch detection, a single-instance guard,
localized Polish and English UI/report resources, and GitHub Actions. The
current release gate records 571 passing tests with a clean Release build.

This project demonstrates native/managed integration, time-based domain
modelling, transactional persistence, desktop UX, reporting, automated testing,
and production-minded release discipline. It is a simulator for ETS2, not a
certified real-world tachograph.

### Contribution / role

End-to-end product engineering: architecture, native telemetry integration,
domain and rule modelling, persistence, WPF interface, reporting, automated
tests, documentation, CI, and release preparation.

### Suggested services / skills

C# · .NET · WPF · C++ · SQLite · Entity Framework Core · Desktop Applications ·
Software Architecture · API / Protocol Design · Automated Testing · GitHub Actions

### Gallery order

1. `docs/images/portfolio/full-application-cover.png`
2. `docs/images/dashboard.png`
3. `docs/images/overlay-s1.png`
4. `docs/images/journey-planner-mockup.png`
5. `docs/images/manual-entry-mockup.png`
6. `docs/images/reports-dashboard.png`
7. `docs/images/report-pdf.png`

### Link

<https://github.com/Staniek-Tech-NL/ETS2-EU-Digital-Tachograph>

---

## 2. Journey Planner — Driving & Rest Planning Module

### One-line summary

A constraint-based planning module that turns current driver state into an
explainable driving, break, rest, and delivery timeline.

### Project description

Journey Planner is a planning module developed as part of the ETS2 EU Digital
Tachograph desktop application. It answers a practical question that a route
duration alone cannot solve: can the driver complete the remaining journey
inside the delivery window while respecting the supported driving and rest
constraints?

The planner evaluates an immutable snapshot of canonical activity history and
the current rule state. It schedules driving, 45-minute or split breaks, daily
rest, weekly rest, regulatory calendar waits, and post-arrival work. Every
segment includes a reason, and the result presents arrival time, completion
time, deadline margin, confidence, warnings, and relevant limit usage.

To keep live data safe, the calculation is read-only and its snapshot identity
is checked before presentation. Card changes, session changes, telemetry
movement, or newly resolved gaps mark the result as stale instead of silently
showing an outdated plan. Explicit iteration and state limits prevent unbounded
calculations.

This case study focuses on algorithmic business logic, immutable snapshots,
safe state transitions, explainable results, WPF presentation, and tests across
the RuleEngine, Application, and Desktop layers.

### Contribution / role

Domain contracts, deterministic planning algorithm, snapshot consistency,
application service, WPF view model, validation, PDF workflow, and regression
tests.

### Suggested services / skills

C# · .NET · WPF · Algorithms · Business Logic · State Machines · UX for Complex
Data · Automated Testing · Software Architecture

### Gallery order

1. `docs/images/portfolio/journey-planner-cover.png`
2. `docs/images/journey-planner-mockup.png`
3. Optional: a close-up of the segment table and warning panel

The second image is a UI design mockup. Its PDF-export control remained outside
the current release scope and should not be described as a shipped feature.

### Link

<https://github.com/Staniek-Tech-NL/ETS2-EU-Digital-Tachograph/blob/main/docs/portfolio/journey-planner.md>

---

## 3. Activity Gap Reconstruction & Manual Entry System

### One-line summary

A validated editing workflow for reconstructing missing activity intervals and
persisting them as complete, traceable history.

### Project description

Activity Gap Reconstruction is a data-editing module developed as part of the
ETS2 EU Digital Tachograph desktop application. It handles periods where a
driver card was removed, telemetry was unavailable, or game time jumped forward
and the system cannot reliably infer the driver's activity.

The editor lets the user classify the whole gap quickly or build a precise plan
from rest, other-work, and availability segments. It supports range replacement,
editing, splitting, deletion, and a live coverage summary. A resolution cannot
be confirmed until every minute is covered exactly once.

Correctness is enforced again below the UI. The service rejects invalid activity
types, overlaps, uncovered minutes, out-of-range segments, collisions with
canonical history, and edits to abandoned game-time branches. Entity Framework
Core writes the resolution and all linked activity records in one SQLite
transaction. Identical retries are idempotent, while conflicting second
submissions are reported explicitly.

Every reconstructed record retains its source gap and `ManualEntry` provenance.
After persistence, the canonical history is reloaded and the rule state is
recalculated from the reconstructed timeline. This case study demonstrates
complex editing UX, state management, validation, transactional persistence,
and temporal data integrity.

### Contribution / role

Gap domain model, segmented editor behavior, service-level validation,
canonical-history checks, transactional and idempotent persistence, rule
recalculation, and cross-layer regression tests.

### Suggested services / skills

C# · .NET · WPF · SQLite · Entity Framework Core · Data Validation · State
Management · Transactional Workflows · Desktop UX · Automated Testing

### Gallery order

1. `docs/images/portfolio/activity-gap-reconstruction-cover.png`
2. `docs/images/manual-entry-mockup.png`
3. Optional: history with an unresolved gap
4. Optional: report timeline showing reconstructed provenance

### Link

<https://github.com/Staniek-Tech-NL/ETS2-EU-Digital-Tachograph/blob/main/docs/portfolio/activity-gap-reconstruction.md>

---

## 4. Reporting, Analytics & Export Pipeline

### One-line summary

A reporting pipeline that converts detailed activity history into readable
analytics, completeness evidence, PDF reports, CSV, and structured JSON.

### Project description

Reporting & Analytics is a data-processing module developed as part of the ETS2
EU Digital Tachograph desktop application. It aggregates canonical driver
history for presets or custom game-time ranges and presents driving, work,
availability, rest, OUT activity, violations, weekly-rest compensation, and
data completeness.

The key design decision was separating stored truth from presentation. The
application keeps precise activity data for rules and diagnostics, while the PDF
builder collapses adjacent records into readable blocks. This reduced an early
minute-by-minute report that exceeded 40 pages for one game day without
discarding diagnostic detail.

Completeness is explicit: activity minutes, unresolved-gap minutes, selected
range, coverage balance, and pending rest allocation all contribute to the
evidence status. The export layer then provides a human-readable PDF, raw
activity CSV, compensation CSV, structured VTC JSON, and a checksum-protected
`.tacho` format for session data.

This case study demonstrates temporal aggregation, range projection, data
quality modelling, format-specific serialization, localization, report design,
and automated verification.

### Contribution / role

Report query model, canonical range processing, aggregation, completeness model,
PDF presentation, CSV/JSON/session exports, localization, and automated tests.

### Suggested services / skills

C# · .NET · WPF · SQLite · Entity Framework Core · Data Analytics · Reporting ·
PDF Generation · CSV · JSON · Data Validation · Automated Testing

### Gallery order

1. `docs/images/portfolio/reporting-analytics-cover.png`
2. `docs/images/reports-dashboard.png`
3. `docs/images/report-pdf.png`
4. Optional: raw CSV sample
5. Optional: VTC JSON sample

### Link

<https://github.com/Staniek-Tech-NL/ETS2-EU-Digital-Tachograph/blob/main/docs/portfolio/reporting-analytics.md>

---

## Publishing checklist

- Use the supplied 4:3 cover as the first image for each project.
- Keep the one-line summary visible before the longer description.
- Mark projects 2–4 as modules of the full application in the first paragraph.
- Use the module-specific GitHub link rather than linking all four entries to the
  repository root.
- Do not describe the project as a certified tachograph or real-world compliance
  tool.
- Do not invent a client, contract, user count, or commercial outcome.
- After the files reach the `main` branch, open every GitHub link in a private
  browser window before publishing the portfolio.
