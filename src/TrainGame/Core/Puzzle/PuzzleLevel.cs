using System;
using System.Collections.Generic;

namespace TrainGame.Core.Puzzle;

public class TrainSpawn
{
    public int X { get; set; }
    public int Y { get; set; }
    public Direction Direction { get; set; }
    public string ColorHex { get; set; } = "#EF4444";
    public string Name { get; set; } = "Tren";
    public int TargetStationId { get; set; } = 1;
    public int CarriageCount { get; set; } = 1;
}

public class TargetStationInfo
{
    public int Id { get; set; }
    public int X { get; set; }
    public int Y { get; set; }
    public string Name { get; set; } = "Estación";
    public string ColorHex { get; set; } = "#EF4444";
    public bool IsHorizontal { get; set; } = true;
    public bool HasReached { get; set; }
}

public class FixedTrackInfo
{
    public int X { get; set; }
    public int Y { get; set; }
    public TrackType Type { get; set; }
}

public class ObstacleInfo
{
    public int X { get; set; }
    public int Y { get; set; }
    public string Type { get; set; } = "Rock"; // "Rock", "Tree", "Water"
}

public class PuzzleLevel
{
    public int Id { get; set; }
    public string Title { get; set; } = "";
    public string Description { get; set; } = "";
    public string Hint { get; set; } = "";
    public int MaxTrackBudget { get; set; }
    public int Star3Budget { get; set; }
    public int Star2Budget { get; set; }

    public List<TrainSpawn> TrainSpawns { get; set; } = new();
    public List<TargetStationInfo> TargetStations { get; set; } = new();
    public List<FixedTrackInfo> FixedTracks { get; set; } = new();
    public List<ObstacleInfo> Obstacles { get; set; } = new();
}

public class LevelScore
{
    public int LevelId { get; set; }
    public bool IsUnlocked { get; set; }
    public bool IsCompleted { get; set; }
    public int StarsEarned { get; set; }
    public int BestTracksUsed { get; set; }
}
