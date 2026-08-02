using System.Collections.Generic;

namespace DeepwaterEngagementSuite.VoyagePlannerData;

public record VoyagePuzzle(
    List<MapPiece> AvailablePieces,
    IReadOnlyList<BorderEffect>[,] TileBorders,
    List<LockedPlacement> LockedPlacements,
    bool AllowSacrificeCornerBorderDeadEnds = false,
    /// <summary>
    /// Soft strategy: score-boost Clam-infested Shelf on orthogonal neighbors of center Unique Amulet2.
    /// </summary>
    bool PreferClamsAdjacentToAmulet = false);
