# Voyage dump fixtures

Drop voyage state dumps here to turn a live board into a repeatable test case.

## Capturing

1. Open **Plan Your Voyage** in game with the board you want to capture.
2. In the **Voyage Optimizer** window, click **Dump State** (or press the
   "Dump voyage state" hotkey configured in settings).
3. The dump is written to `ConfigDirectory/voyage-dumps/voyage-<timestamp>.json`,
   and a summary is printed to the ExileCore debug log.

## Using

Copy the JSON file into this folder. `VoyageDumpReplayTests` picks up every
`*.json` here automatically — no code change needed. Rename it to something
descriptive, e.g. `annul-1-0-three-starfish.json`.

## What's in a dump

Everything the solver consumes, already resolved from settings so the replay does
not need ExileCore or your settings file:

- `RawBorderMods` — border mod names in engine index order, exactly as read.
- `Tiles[]` — per tile: resolved `BorderEffect` values (tags, multiplier,
  per-connection, affects-placed-chart) plus `HasSettingsEntry` so you can tell an
  unconfigured mod from a genuinely zero-weighted one. Also the chart currently
  sitting on that tile, if any.
- `Charts[]` — every chart in the voyage inventory: room name, room path
  (connection bitmask), derived piece type, and each modifier with its raw name,
  raw `Values`, and the resolved weight/global/tags. `PieceId` is the id the
  solver uses; `IncludedInSolve` is false for charts removed by the ignore filters.
- `StrategyOptions` / `Solver` — the toggles and solver settings in effect.
- `Derived` — orb centers by priority (Divine/Annul/Ancient) and chart-category
  counts, so you can see at a glance whether a cell registered as an orb center.
- `Placement` — locks, remaining piece ids, active strategies and save counts
  produced by `VoyagePlacementRules.Apply` at capture time.
- `Solution` — best solution the solver had found, if any.
