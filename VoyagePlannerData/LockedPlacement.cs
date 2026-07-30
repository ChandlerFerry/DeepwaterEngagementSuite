namespace DeepwaterEngagementSuite.VoyagePlannerData;

/// <param name="Rotation">
/// Fixed rotation, or null to let the solver pick any rotation that satisfies connectivity.
/// </param>
public record LockedPlacement(
    int Row,
    int Col,
    int PieceId,
    int? Rotation = null);
