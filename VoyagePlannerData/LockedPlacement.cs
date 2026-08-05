namespace DeepwaterEngagementSuite.VoyagePlannerData;


public record LockedPlacement(
    int Row,
    int Col,
    int PieceId,
    int? Rotation = null,
    string Strategy = null,
    int Priority = 0);


public static class LockPriorities
{
    public const int DivinePelagic = 1000;
    public const int DivineSupport = 900;
    public const int AnnulPelagic = 800;
    public const int AncientPelagic = 700;
    public const int AnnulSupport = 600;
    public const int AncientSupport = 500;
    public const int Amulet = 400;
    public const int CenterSpecialty = 300;
    public const int NoConsume = 200;
    
    public const int DivineFill = 100;
    public const int Default = 0;
}
