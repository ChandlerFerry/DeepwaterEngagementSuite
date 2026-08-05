using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using DeepwaterEngagementSuite.VoyagePlannerData;
using Newtonsoft.Json;

namespace DeepwaterEngagementSuite.Tests;


public static class VoyageStateDumpFixture
{
    public static string FixtureDirectory =>
        Path.Combine(AppContext.BaseDirectory, "fixtures");

    public static IEnumerable<string> FixturePaths() =>
        Directory.Exists(FixtureDirectory)
            ? Directory.GetFiles(FixtureDirectory, "*.json").OrderBy(p => p, StringComparer.Ordinal)
            : [];

    
    public static IEnumerable<object[]> AllFixtures()
    {
        var any = false;
        foreach (var path in FixturePaths())
        {
            any = true;
            yield return [Path.GetFileName(path)];
        }

        if (!any)
            yield return [null];
    }

    public static VoyageStateDump Load(string fileName)
    {
        var path = Path.IsPathRooted(fileName) ? fileName : Path.Combine(FixtureDirectory, fileName);
        var dump = JsonConvert.DeserializeObject<VoyageStateDump>(File.ReadAllText(path))
                   ?? throw new InvalidOperationException($"Dump '{path}' deserialized to null.");
        if (dump.Version > VoyageStateDump.CurrentVersion)
            throw new InvalidOperationException(
                $"Dump '{path}' is version {dump.Version}, this build understands up to {VoyageStateDump.CurrentVersion}.");
        return dump;
    }

    
    public static (List<MapPiece> Pieces, IReadOnlyList<BorderEffect>[,] Borders, VoyageStrategyOptions Options)
        Inputs(VoyageStateDump dump) =>
        (dump.ToMapPieces(), dump.ToTileBorders(), dump.ToStrategyOptions());

    
    public static VoyagePlacementRules.Result Replay(VoyageStateDump dump)
    {
        var (pieces, borders, options) = Inputs(dump);
        return VoyagePlacementRules.Apply(pieces, borders, options);
    }

    
    public static (VoyagePlacementRules.Result Placement, VoyageSolutionResult Result) Solve(VoyageStateDump dump)
    {
        var (pieces, borders, options) = Inputs(dump);
        var session = new VoyageSolve();
        VoyageSolutionResult last = null;
        foreach (var r in session.Run(
                     pieces,
                     borders,
                     settings: dump.ToPlannerSettings(),
                     strategyOptions: options))
        {
            last = r;
        }

        return (session.Placement, last);
    }

    
    public static string Explain(VoyageStateDump dump, VoyagePlacementRules.Result placement)
    {
        var strategies = placement.ActiveStrategies ?? new List<string>();
        var lines = new List<string>
        {
            dump.Describe(),
            "--- replayed ---",
            "Locks: " + (placement.Locks.Count == 0
                ? "none"
                : string.Join(", ", placement.Locks.Select(l => $"#{l.PieceId}@({l.Row},{l.Col})"))),
            "Active strategies: " + (strategies.Count == 0 ? "none" : string.Join(", ", strategies)),
            $"Pieces remaining: {placement.Pieces.Count}",
        };
        return string.Join(Environment.NewLine, lines);
    }
}
