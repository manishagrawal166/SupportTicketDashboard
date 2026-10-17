# Support Dashboard

## What this project is

A dashboard that aggregates operational data from four sources into one view:
- **ServiceNow** incident tickets
- **Azure Function Apps** execution history
- **Azure Data Factory (ADF)** pipeline runs
- **Azure Logic Apps** workflow runs

## Status

Design/mockup phase only. No application code has been written yet — everything
so far is a clickable prototype built as a Claude Artifact (the "Design" canvas
type). This folder was empty before that work started.

**Mockup (private artifact):** https://claude.ai/artifact/AFifvseLAfth34qsDbtt5x
Only the owner can open it until it's shared from the artifact's Share menu.
Treat the artifact as the source of truth for the current design — if asked to
extend the mockup, read it first rather than assuming this file is fully in
sync.

## Planned data sources & APIs (for the real implementation)

- **ServiceNow**: Table API, `GET /api/now/table/incident`, paginated via
  `sysparm_limit`/`sysparm_offset`, field-limited via `sysparm_fields`. Auth:
  Basic or OAuth2 (OAuth2 preferred for anything beyond a quick script).
- **Azure Function Apps**: execution/invocation history mainly comes from
  **Application Insights** (KQL queries against `requests`/`traces`/
  `exceptions`), not a plain ARM endpoint — depends on App Insights being
  wired up for the function apps in question.
- **ADF**: Pipeline Runs API (`queryPipelineRuns`) — gives status, timing,
  parameters, per-activity run details.
- **Logic Apps**: Workflow Runs API (`/workflows/{name}/runs`) — status,
  trigger history, per-action results. Endpoint shape differs slightly between
  Consumption and Standard logic apps.
- **Azure auth**: Entra ID service principal, OAuth2 client credentials flow,
  RBAC (Reader / Monitoring Reader as appropriate).
- **Recommended architecture**: a scheduled sync job pulls from all four
  sources into local storage; the dashboard reads from that store rather than
  querying live on every page load (avoids rate limits, keeps the UI fast).

## Design decisions already made (don't relitigate these without reason)

**Layout pattern**: a summary screen with one Bootstrap-card-style tile per
source, each clickable through to a detail screen for that source.

**Summary dashboard cards**:
- Forced to a single row of 4 (`grid-template-columns: repeat(4, 1fr)`), not
  auto-wrapping.
- Each card has a solid-color header band (icon + source name in white) above
  a white body (count, breakdown, "View details" link) — not just a thin
  accent stripe.
- Color palette (header band / darker text-safe variant for links):
  - ServiceNow Incidents: `#2563EB` / `#1D4ED8` (blue)
  - Function Apps: `#7C3AED` / `#6D28D9` (purple)
  - ADF Pipelines: `#0E9488` / `#0F766E` (teal)
  - Logic Apps: `#EA580C` / `#C2410C` (orange)
- Fonts: **Manrope** (headings/numbers), **IBM Plex Sans** (body) — via Google
  Fonts.

**ServiceNow Incidents detail screen** (the most built-out screen; treat it as
the reference pattern for bringing the other three screens to parity):
- Stat tiles: Total Opened Tickets + **P1–P5** (ServiceNow's real 5-tier
  priority scale — not a 4-tier critical/high/medium/low scheme).
- Filters: date range (From/To), a search box, and priority quick-filter chips
  (All/P1–P5).
- Search box accepts **comma-separated** incident numbers/text for multi-ticket
  lookup (union match across terms, not intersection).
- **Known gotcha already fixed once**: the priority chip filter and the search
  box both apply (AND), so a stale chip selection can hide search results that
  don't match that priority. Fix in place: typing into the search box
  auto-resets the chip filter to "All". Keep this behavior if reimplementing
  the filter logic for real.
- Table columns: Incident Number, Short Description (truncated with ellipsis +
  full text on hover via the `title` attribute), Priority (P1–P5 badge), State,
  Assignment Group (same truncate+tooltip treatment), Assigned By, Assigned To,
  Opened (date + time), Age, Comment (free-text input — not persisted in the
  mockup, intentionally not sortable).
- All columns except Comment are sortable: click a header to sort, click again
  to reverse; shows a ▲/▼ indicator on the active column.
- **Times are shown in Central Time**, computed via
  `Intl`/`toLocaleString(... { timeZone: 'America/Chicago' })` rather than a
  hardcoded offset, so it correctly reads CDT vs CST depending on the date —
  don't hardcode "CST" if reimplementing.
- Table uses a fixed `table-layout` with an explicit `<colgroup>` so all 10
  columns fit the page width with **no horizontal scrollbar** — long fields
  truncate with ellipsis instead of causing overflow.
- **Dark mode**: a moon/sun toggle switches the whole screen to a dark theme.
  Currently only implemented on this screen, not yet on the dashboard or the
  other three detail screens.
- **New-ticket notification demo**: a "+ Simulate new ticket" button adds a
  ticket to the top of the table (tagged with a "NEW" badge), plays a short
  chime synthesized live via the Web Audio API (no audio file asset — the
  click itself satisfies browser autoplay restrictions), and shows a
  color-coded, auto-dismissing toast. **Sound plays for every priority level**,
  not just critical — that was an explicit choice, don't restrict it to P1
  only unless asked. A bell icon mutes/unmutes it. Stat tiles and totals
  update live as tickets are simulated.

**Function Apps / ADF Pipelines / Logic Apps detail screens**: simpler and
static — stat tiles + a table with status badges — they have **not** yet
received the sorting/search/dark-mode/sound treatment built for Incidents. If
asked to bring them to parity, reuse the same patterns described above rather
than inventing new ones.

## Planned implementation architecture (.NET MVC)

Decided, not yet built — the mockup above is still the only thing that
exists in code. When implementation starts, build it to this shape:

**Solution structure** (layered, keep EF entities/external API clients out of
the MVC project):
```
SupportDashboard.sln
├── SupportDashboard.Web            ASP.NET Core MVC — Controllers, Views, wwwroot
├── SupportDashboard.Core           Domain models, DTOs, interfaces (IIncidentRepository, etc.)
├── SupportDashboard.Infrastructure EF Core DbContext, repositories, external API clients
├── SupportDashboard.SyncJobs       Background sync services (hosted in Web, see below)
└── SupportDashboard.Tests          xUnit
```

**Database: SQLite, not SQL Server.** Explicit decision — the user wanted to
avoid standing up/managing a SQL Server instance, not to avoid a relational
database. SQLite gives a single portable file, real indexing/transactions,
and concurrent-safe access via WAL mode, with near-zero code difference from
SQL Server when used through EF Core. Do not reintroduce SQL Server, and do
not use Excel as the live datastore (considered and rejected: concurrent
writes from sync jobs + concurrent reads from the web app risk file-lock
conflicts/corruption with typical .NET Excel libraries, which tend to
rewrite the whole file rather than one row).

Tables, same shape as the mockup's detail screens:
- `Incidents` (Number, ShortDescription, Priority, State, AssignmentGroup,
  AssignedBy, AssignedTo, OpenedUtc, Comment, LastSyncedUtc)
- `FunctionExecutions`, `PipelineRuns`, `WorkflowRuns` — fields matching each
  detail screen's columns
- `SyncState` (Source, LastSyncUtc, Cursor, LastError) — enables incremental
  sync per source and a simple sync-health view later

**Sync jobs, not live passthrough.** MVC controllers only ever query the
local SQLite database — never call ServiceNow/Azure synchronously on page
load (keeps the UI fast, insulates it from external rate limits/outages).
One `BackgroundService` per source (`ServiceNowSyncService`,
`FunctionAppSyncService`, `AdfSyncService`, `LogicAppSyncService`) polls on a
schedule and upserts into the local tables, tracking its cursor in
`SyncState` for incremental pulls. Run these in-process inside the MVC app
for v1 (simplest to build/debug/deploy as one unit); only split a job out
into a separate Azure Function (Timer trigger) later if it needs independent
scaling or its failures start affecting web app uptime.

**Auth**: ServiceNow via OAuth2 client credentials (secret in Key
Vault/User Secrets, never committed). Azure via Managed Identity where
hosted in Azure (no secret to rotate), falling back to an Entra ID app
registration + Key Vault secret otherwise. RBAC scoped to Reader/Monitoring
Reader only.

**UI serving model**: MVC renders the page shell + initial data server-side
(fast first paint, no-JS fallback); a thin JSON API
(`/api/incidents?priority=P1&search=...&sort=opened&dir=desc`) backs the
same live sort/filter/search/comma-search interactions already built in the
mockup, called via `fetch` from a small vanilla-JS layer — no need for a full
SPA framework at this scope. Dark mode becomes a cookie-backed, site-wide
preference (upgrade from the mockup's per-page-only toggle).

**New-ticket notifications → SignalR.** The mockup's simulate-button/
chime/toast demo maps onto SignalR: when a sync job inserts a new incident,
it broadcasts to connected clients, which play the chime (still sound for
every priority, not just critical) and show the toast — same client
behavior already built, just driven by a real event instead of a button
click. Simpler v1 fallback if SignalR is more than needed at first: short
-interval client polling against a `/api/incidents/latest` endpoint.

**Deployment**: Azure App Service for the web app (natural fit, already
pulling from Azure services throughout).

## Not started yet

No application code has been written. The architecture above is decided;
building it (backend sync services, EF Core + SQLite schema, MVC
controllers/views, the JSON API layer, auth wiring, SignalR) is still
future work.
