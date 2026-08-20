namespace TrainGame.Core;

public enum TrackType
{
    Horizontal,        // Conecta Oeste <-> Este
    Vertical,          // Conecta Norte <-> Sur
    CurveNorthEast,    // Conecta Norte <-> Este
    CurveEastSouth,    // Conecta Este <-> Sur
    CurveSouthWest,    // Conecta Sur <-> Oeste
    CurveWestNorth,    // Conecta Oeste <-> Norte
    Cross,             // Cruce en cruz (Norte-Sur y Este-Oeste)
    StationHorizontal, // Estación en vía horizontal
    StationVertical,   // Estación en vía vertical
    Switch             // Desvío interactivo (Horizontal con desvío a Curva o viceversa)
}

public class TrackPiece
{
    public int X { get; }
    public int Y { get; }
    public TrackType Type { get; set; }

    // Propiedades de Desvío (Switch)
    public bool IsSwitch { get; set; }
    public TrackType MainType { get; set; } = TrackType.Horizontal;
    public TrackType BranchType { get; set; } = TrackType.CurveEastSouth;
    public bool SwitchState { get; set; } // false = vía principal, true = desvío activo

    // Estación y Señalización asociadas a esta vía
    public Station? Station { get; set; }
    public RailwaySignal? Signal { get; set; }

    public TrackType EffectiveType => IsSwitch ? (SwitchState ? BranchType : MainType) : Type;

    public TrackPiece(int x, int y, TrackType type)
    {
        X = x;
        Y = y;
        Type = type;
        if (type == TrackType.StationHorizontal)
        {
            Station = new Station(x, y, "Estación Central");
        }
        else if (type == TrackType.StationVertical)
        {
            Station = new Station(x, y, "Terminal Norte");
        }
    }

    public void ToggleSwitch()
    {
        if (IsSwitch)
        {
            SwitchState = !SwitchState;
        }
    }

    /// <summary>
    /// Retorna si esta vía tiene una conexión abierta hacia la dirección especificada.
    /// </summary>
    public bool HasConnection(Direction direction)
    {
        var activeType = EffectiveType;
        return activeType switch
        {
            TrackType.Horizontal or TrackType.StationHorizontal => direction is Direction.West or Direction.East,
            TrackType.Vertical or TrackType.StationVertical => direction is Direction.North or Direction.South,
            TrackType.CurveNorthEast => direction is Direction.North or Direction.East,
            TrackType.CurveEastSouth => direction is Direction.East or Direction.South,
            TrackType.CurveSouthWest => direction is Direction.South or Direction.West,
            TrackType.CurveWestNorth => direction is Direction.West or Direction.North,
            TrackType.Cross => true,
            _ => false
        };
    }

    /// <summary>
    /// Dado que el tren entra a esta casilla viajando en cierta dirección,
    /// calcula en qué dirección debe continuar su movimiento al salir de esta casilla.
    /// </summary>
    public Direction? GetExitDirection(Direction travelDirection)
    {
        Direction entryFrom = travelDirection.Opposite();

        if (!HasConnection(entryFrom))
        {
            return null; // Descarrilamiento
        }

        var activeType = EffectiveType;
        return activeType switch
        {
            TrackType.Horizontal or TrackType.StationHorizontal => travelDirection,
            TrackType.Vertical or TrackType.StationVertical => travelDirection,
            TrackType.Cross => travelDirection,
            TrackType.CurveNorthEast => entryFrom == Direction.North ? Direction.East : Direction.North,
            TrackType.CurveEastSouth => entryFrom == Direction.East ? Direction.South : Direction.East,
            TrackType.CurveSouthWest => entryFrom == Direction.South ? Direction.West : Direction.South,
            TrackType.CurveWestNorth => entryFrom == Direction.West ? Direction.North : Direction.West,
            _ => null
        };
    }

    public (Direction d1, Direction d2) GetConnectedPair()
    {
        var activeType = EffectiveType;
        return activeType switch
        {
            TrackType.Horizontal or TrackType.StationHorizontal => (Direction.West, Direction.East),
            TrackType.Vertical or TrackType.StationVertical => (Direction.North, Direction.South),
            TrackType.CurveNorthEast => (Direction.North, Direction.East),
            TrackType.CurveEastSouth => (Direction.East, Direction.South),
            TrackType.CurveSouthWest => (Direction.South, Direction.West),
            TrackType.CurveWestNorth => (Direction.West, Direction.North),
            _ => (Direction.West, Direction.East)
        };
    }
}

public static class TrackDirectionHelper
{
    public static Direction Sur(this Direction _) => Direction.South;
}
