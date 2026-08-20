namespace TrainGame.Core;

public enum Direction
{
    North,
    East,
    South,
    West
}

public static class DirectionExtensions
{
    public static Direction Opposite(this Direction direction) => direction switch
    {
        Direction.North => Direction.South,
        Direction.South => Direction.North,
        Direction.East => Direction.West,
        Direction.West => Direction.East,
        _ => Direction.North
    };

    public static (int dx, int dy) ToOffset(this Direction direction) => direction switch
    {
        Direction.North => (0, -1),
        Direction.South => (0, 1),
        Direction.East => (1, 0),
        Direction.West => (-1, 0),
        _ => (0, 0)
    };

    public static double ToAngleDegrees(this Direction direction) => direction switch
    {
        Direction.East => 0,
        Direction.South => 90,
        Direction.West => 180,
        Direction.North => 270,
        _ => 0
    };
}
