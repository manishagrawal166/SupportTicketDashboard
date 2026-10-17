---
name: support-dashboard-mock
description: Update, extend, or add screens to the Support Dashboard mockup (a Claude Artifact "Design" canvas covering ServiceNow incidents plus Azure Function Apps, ADF pipelines, and Logic Apps). Use whenever the user asks to change the "support dashboard" mockup, add a new mock screen for it, or iterate on its existing screens.
---

# Support Dashboard mockup

## Where it lives

The mockup is a Claude Artifact of the "Design" (canvas) type, not files in
this repo. Current link: https://claude.ai/artifact/AFifvseLAfth34qsDbtt5x

Before changing it:
1. Load the `artifact-design` skill (required before writing any artboard).
2. `read` the artifact's `project/canvas.json` and the specific `.dc.html`
   artboard(s) you're about to touch — someone may have edited them by hand
   in the canvas editor since the last session (this has happened before:
   a leftover placeholder artboard was repositioned by hand between turns).
3. Use the Artifact tool's `edits` field for small changes to one file when
   available; for multi-spot changes, edit the local scratch copy with
   Edit/Write first, then publish with the same `file_path`/`root`, in the
   same response.

Full project background (data sources, planned real-app architecture — ASP.NET
Core MVC, SQLite, sync `BackgroundService`s, SignalR for live notifications)
is in `CLAUDE.md` at the repo root. This skill only covers the mockup itself;
once real implementation starts, that's a separate skill/workflow, not this
one.

## Artboards in the canvas

- `Main.dc.html` — summary dashboard: 4 cards in a single row, each linking to
  a detail screen.
- `Incidents.dc.html` — ServiceNow incidents detail. Most fully built out;
  treat it as the reference pattern (see below).
- `FunctionApps.dc.html`, `ADFPipelines.dc.html`, `LogicApps.dc.html` — detail
  screens for the three Azure sources. Currently simpler/static (stat tiles +
  table with status badges) — no sorting, search, dark mode, or sound yet.

## Conventions to reuse (don't reinvent per screen)

**Fonts**: Manrope (headings/numbers, weights 500/700/800) + IBM Plex Sans
(body, weights 400/500/600), loaded via a Google Fonts `css2` link in
`<helmet>`. Declared fresh in every artboard's own `<style>` (artboards are
self-contained; nothing is shared across files).

**Color palette** (header/accent color, then a darker text-safe variant for
links/footers on white):
| Source | Accent | Text-safe accent |
|---|---|---|
| ServiceNow Incidents | `#2563EB` | `#1D4ED8` |
| Function Apps | `#7C3AED` | `#6D28D9` |
| ADF Pipelines | `#0E9488` | `#0F766E` |
| Logic Apps | `#EA580C` | `#C2410C` |

Badge colors for ServiceNow priority (P1–P5, not critical/high/medium/low):
`badge-p1` `#FEE2E2`/`#991B1B`, `badge-p2` `#FFEDD5`/`#9A3412`, `badge-p3`
`#FEF3C7`/`#92400E`, `badge-p4` `#E0E7FF`/`#3730A3`, `badge-p5` `#F3F4F6`/
`#374151`. State badges: `badge-open` `#DBEAFE`/`#1E40AF`, `badge-progress`
`#FEF3C7`/`#92400E`, `badge-resolved`/`badge-success` `#DCFCE7`/`#166534`,
`badge-failed` `#FEE2E2`/`#991B1B`, `badge-running` `#DBEAFE`/`#1E40AF`.

**Summary cards**: `grid-template-columns: repeat(4, minmax(0,1fr))` (forced
single row, not `auto-fit`), colored header band (icon in a translucent white
circle + white label text) above a white body with the stat number, breakdown,
and a "View details →" link in the text-safe accent color.

**Wide tables**: use `table-layout: fixed` with an explicit `<colgroup>` of
pixel widths (leave the last column, usually Comment, with no explicit width
so it absorbs remaining space) rather than letting the table overflow into a
horizontal scrollbar. Truncate long text columns with
`overflow:hidden;text-overflow:ellipsis;white-space:nowrap` and add a `title`
attribute with the full value for a hover tooltip.

**Sortable columns**: precompute `handlers` (an object of `() =>
this.setSort(field)` closures) and `indicators` (a `▲`/`▼`/empty string per
field) in `renderVals()`, bind `onClick="{{handlers.x}}"` on each `<th>` and
render the indicator as its own element, e.g.
`<span class="sort-ind">{{indicators.x}}</span>`. Sort by a precomputed numeric
or lowercased-string key (e.g. `priorityRank`, `openedTs`, `ageDays`) — never
sort directly on a display label.

**Timestamps**: generate them as explicit UTC (`Date.parse('...Z')`), then
format for display with
`toLocaleString('en-US', { timeZone: 'America/Chicago', ... })` (or
`toLocaleDateString`/`toLocaleTimeString` split for a stacked date/time cell).
This correctly labels CDT vs CST depending on the date — never hardcode one
or the other, and never rely on the viewer's local browser timezone.

**Filter interactions**: if a screen has both a quick-filter (chips) and a
free-text search, make typing in search reset the chip filter to "All" (or
otherwise make the combination explicit) — combining them silently with AND
logic is a real bug that already surfaced once on the Incidents screen (a
leftover priority-chip selection silently hid search results for other
priorities).

**Dark mode**: a `dark` boolean in state, toggled by a sun/moon icon button
using two `<sc-if>` blocks (never a ternary in a hole — those aren't
supported); apply as `class="page {{themeClass}}"` on the root div, with a
`.page.dark ...` override block in the same `<style>` for every surface
(stat tiles, table, inputs, chips, buttons, toasts). Badge pill colors stay
the same in both themes.

**New-item notifications**: synthesize a short chime with the Web Audio API
(`AudioContext` oscillator ramp) rather than an uploaded audio asset — a
click-triggered simulate button counts as the user gesture browsers require
before audio can play. Pair with a dismissible, auto-timeout toast
(color-coded by whatever triggered it, e.g. priority) and a mute/unmute bell
icon. Per explicit user preference, sound is not restricted to one severity
level unless asked — it fires for all of them.

## When extending the other 3 detail screens to match Incidents

Reuse the patterns above (fixed-width table, sortable headers, dark mode
toggle, Central Time formatting) rather than designing new ones — the goal is
visual and behavioral consistency across all four detail screens.
