using System;
using System.Collections.Generic;

namespace TrainGame.Core;

public class RailGrid
{
    public int Columns { get; }
    public int Rows { get; }

    private readonly TrackPiece?[,] _grid;

    public event Action? OnGridChanged;

    public RailGrid(int columns = 32, int rows = 20)
    {
        Columns = columns;
        Rows = rows;
        _grid = new TrackPiece?[columns, rows];
    }

    public bool IsInBounds(int x, int y) => x >= 0 && x < Columns && y >= 0 && y < Rows;

    public TrackPiece? GetTrack(int x, int y)
    {
        if (!IsInBounds(x, y)) return null;
        return _grid[x, y];
    }

    public bool HasTrack(int x, int y)
    {
        return IsInBounds(x, y) && _grid[x, y] != null;
    }

    public void SetTrack(int x, int y, TrackType type)
    {
        if (!IsInBounds(x, y)) return;
        _grid[x, y] = new TrackPiece(x, y, type);
        OnGridChanged?.Invoke();
    }

    public bool RemoveTrack(int x, int y)
    {
        if (!IsInBounds(x, y) || _grid[x, y] == null) return false;
        _grid[x, y] = null;
        OnGridChanged?.Invoke();
        return true;
    }

    public void Clear()
    {
        for (int x = 0; x < Columns; x++)
        {
            for (int y = 0; y < Rows; y++)
            {
                _grid[x, y] = null;
            }
        }
        OnGridChanged?.Invoke();
    }

    public IEnumerable<TrackPiece> GetAllTracks()
    {
        for (int x = 0; x < Columns; x++)
        {
            for (int y = 0; y < Rows; y++)
            {
                if (_grid[x, y] != null)
                {
                    yield return _grid[x, y]!;
                }
            }
        }
    }

    /// <summary>
    /// Coloca una vía de forma inteligente resolviendo automáticamente si debe ser
    /// recta, curva o cruce basándose en las vías vecinas.
    /// </summary>
    public TrackPiece SmartPlaceTrack(int x, int y)
    {
        if (!IsInBounds(x, y)) return null!;

        bool hasNorth = HasConnectingNeighbor(x, y, Direction.North);
        bool hasSouth = HasConnectingNeighbor(x, y, Direction.South);
        bool hasEast  = HasConnectingNeighbor(x, y, Direction.East);
        bool hasWest  = HasConnectingNeighbor(x, y, Direction.West);

        int connectionCount = (hasNorth ? 1 : 0) + (hasSouth ? 1 : 0) + (hasEast ? 1 : 0) + (hasWest ? 1 : 0);

        TrackType type;

        if (connectionCount == 4)
        {
            type = TrackType.Cross;
        }
        else if (hasNorth && hasSouth && (hasEast || hasWest))
        {
            type = TrackType.Cross;
        }
        else if (hasEast && hasWest && (hasNorth || hasSouth))
        {
            type = TrackType.Cross;
        }
        else if (hasNorth && hasEast)
        {
            type = TrackType.CurveNorthEast;
        }
        else if (hasEast && hasSouth)
        {
            type = TrackType.CurveEastSouth;
        }
        else if (hasSouth && hasWest)
        {
            type = TrackType.CurveSouthWest;
        }
        else if (hasWest && hasNorth)
        {
            type = TrackType.CurveWestNorth;
        }
        else if (hasNorth || hasSouth)
        {
            type = TrackType.Vertical;
        }
        else
        {
            // Por defecto horizontal
            type = TrackType.Horizontal;
        }

        var piece = new TrackPiece(x, y, type);
        _grid[x, y] = piece;
        OnGridChanged?.Invoke();
        return piece;
    }

    public bool HasConnectingNeighbor(int x, int y, Direction dir)
    {
        var (dx, dy) = dir.ToOffset();
        int nx = x + dx;
        int ny = y + dy;

        var neighbor = GetTrack(nx, ny);
        if (neighbor == null) return false;

        // El vecino debe tener conexión hacia nuestra celda (dirección opuesta)
        return neighbor.HasConnection(dir.Opposite());
    }
}
