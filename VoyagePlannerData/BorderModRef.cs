namespace DeepwaterEngagementSuite.VoyagePlannerData;


public sealed class BorderModRef
{
    public string Id { get; init; }
    public string DisplayText { get; init; }
    public string Source { get; init; }

    public string Label =>
        !string.IsNullOrEmpty(DisplayText) ? DisplayText :
        !string.IsNullOrEmpty(Id) ? Id :
        "";
}
