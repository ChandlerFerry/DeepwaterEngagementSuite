# Voyage Solver — Modifier Scoring Handoff

## Status: tag-based scoring implemented (Jul 2026)

The flat-scalar border bug described in earlier versions of this document is fixed. Scoring
is now tag-based and lives in `VoyagePlannerData/VoyageScorer.cs`; `VoyagePlanner` only does
search/connectivity and delegates all scoring (including the pruning upper bound) to the scorer.

## Scoring model

- Every chart modifier (`Modifier`) carries a `ModifierTag` flags value (see
  `VoyagePlannerData/ModifierTag.cs`: Monsters, MagicMonsters, RareMonsters, Essences,
  Strongboxes, Uniques, Currency, Scarabs, Gold, Equipment, Experience, Resources, Lanterns,
  Rarity, plus `None`/`All`).
- Local ("Adjacent...") chart mods deliver their weight to each orthogonally adjacent tile;
  global mods deliver their weight to every tile.
- Every border (`BorderEffect`) has tags, a multiplier, and two behavior flags:
  - **Tile-effect borders** (default) multiply rewards materializing on their tile, but only
    rewards sharing at least one tag with the border. `All` matches everything (including
    untagged mods); `None` makes the border inert for scoring (correct for purely flat payouts
    like ducat chests — since all 9 cells are always filled, a flat add is the same constant in
    every solution and cannot steer placement).
  - **`AffectsPlacedChart`** borders (ChartEffect, ChanceToNotConsumeChart) multiply all mods
    *of the chart placed on their tile*, wherever that value lands — this is geometrically
    different from tile-effect borders and was previously mismodeled.
  - **`PerConnection`** borders scale with the connection count of the piece placed on the
    affected tile: effective multiplier = `1 + (multiplier - 1) × connections`. Configured
    multiplier means "per single connection" (e.g. 1.4 → ×2.6 on a Cross). Evaluated live per
    placement; previously these were treated as flat multipliers.
- Multiple matching borders compound multiplicatively (deliberate).
- Untagged *chart mods* pass through at raw weight (only `All` borders touch them). Untagged
  *borders* default to `All` — this keeps old user profiles (which have no `Tags` fields)
  behaving like the legacy flat-scalar model.

## Solver integration

- All multiplier combinations are precomputed per (tile, tag-mask, connection count), so the
  search hot path is table lookups only.
- `VoyageScorer.UpperBound` is admissible w.r.t. `VoyageScorer.Score`: empty tiles use the
  max multiplier over connection counts, unknown neighbor pieces use a precomputed max over
  all pieces, and unplaced globals are bounded by the best (9 − filled) unused pieces.
- Piece grouping for symmetry breaking now keys on the full modifier signature
  (tags + weights + global flag), not just weight sums.
- Pieces are scanned heaviest-first (value ordering) so good solutions are found early and
  pruning bites sooner.
- The optimizer window shows a per-tile score column (`VoyageScorer.CellScores`; local rewards
  attributed to the receiving tile, globals to their carrier).

## Debugging

The optimizer window includes a per-tile **Score** column and a collapsible **Score details**
tree that explains each contribution: source piece, weight, chart/tile multipliers, and which
borders applied. Use this to sanity-check tag assignments and placement decisions.

## Config

`profiles/default.json` has a full first-pass tagging of all border and chart entries, with
per-connection borders (`RareMonstersPerConnection*`, `QuantityPerConnection*`) and chart-side
borders (`ChartEffect*`, `ChanceToNotConsumeChart*`) flagged. Multipliers of formerly-flat
"adds stuff" borders (AdditionalCrabs, GiantOctopus, TreasureAnchors, ...) were reduced or set
to `None` because their flat value cannot steer placement (see above).
