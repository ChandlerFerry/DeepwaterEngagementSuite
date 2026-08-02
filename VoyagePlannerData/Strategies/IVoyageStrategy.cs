namespace DeepwaterEngagementSuite.VoyagePlannerData.Strategies;

public interface IVoyageStrategy
{
    string Id { get; }
    int Order { get; }
    bool IsEnabled(VoyageStrategyOptions options);
    void Apply(PlacementContext ctx);
}
