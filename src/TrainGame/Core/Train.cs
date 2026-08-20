using System;
using System.Collections.Generic;

namespace TrainGame.Core;

public enum CarriageType
{
    Passenger,
    Freight,
    Tanker,
    Caboose
}

public class TrainCarriage
{
    public CarriageType Type { get; set; }
    public string ColorHex { get; set; } = "#3B82F6";
    public double WorldX { get; set; }
    public double WorldY { get; set; }
    public double AngleDegrees { get; set; }
    public int PassengersOnBoard { get; set; } = 0;
    public int MaxCapacity => Type == CarriageType.Passenger ? 16 : 0;

    public TrainCarriage(CarriageType type, string colorHex = "#3B82F6")
    {
        Type = type;
        ColorHex = colorHex;
    }
}

public class PositionRecord
{
    public double WorldX { get; }
    public double WorldY { get; }
    public double AngleDegrees { get; }
    public double DistanceFromStart { get; }

    public PositionRecord(double x, double y, double angle, double dist)
    {
        WorldX = x;
        WorldY = y;
        AngleDegrees = angle;
        DistanceFromStart = dist;
    }
}

public class Train
{
    public int Id { get; }
    public string Name { get; set; }
    public string ColorHex { get; set; }
    public int? TargetStationId { get; set; }

    // Posición en la grilla y movimiento
    public int CellX { get; set; }
    public int CellY { get; set; }
    public Direction TravelDirection { get; set; }
    public Direction EntryTravelDirection { get; set; }
    public double ProgressInCell { get; set; } // [0..1]

    // Coordenadas calculadas y renderizado
    public double WorldX { get; private set; }
    public double WorldY { get; private set; }
    public double AngleDegrees { get; private set; }

    // Física
    public double Speed { get; set; } = 3.0;
    public double TargetSpeed { get; set; } = 3.0;
    public double MaxSpeed { get; set; } = 6.0;
    public double Acceleration { get; set; } = 2.5;

    // Estado
    public bool IsDerailed { get; set; }
    public bool IsPaused { get; set; }
    public double StationWaitTimer { get; set; } = 0.0;
    public bool IsWaitingForSignal { get; set; }
    public bool IsBrakingForCollision { get; set; }
    public string StatusMessage { get; set; } = "En marcha";

    // Pasajeros y Economía
    public int TotalPassengersDelivered { get; set; } = 0;
    public int TotalPassengersAboard
    {
        get
        {
            int total = 0;
            foreach (var c in Carriages) total += c.PassengersOnBoard;
            return total;
        }
    }
    public int TotalPassengerCapacity
    {
        get
        {
            int cap = 0;
            foreach (var c in Carriages) cap += c.MaxCapacity;
            return cap;
        }
    }

    // Vagones y trayectoria
    public List<TrainCarriage> Carriages { get; } = new();
    public List<PositionRecord> PositionHistory { get; } = new();
    public double TotalDistanceTraveled { get; private set; }

    // Distancia entre vagones en unidades de celda
    public const double CarriageSpacing = 0.85;

    public Train(int id, string name, int startX, int startY, Direction startDir, string colorHex = "#EF4444")
    {
        Id = id;
        Name = name;
        CellX = startX;
        CellY = startY;
        TravelDirection = startDir;
        EntryTravelDirection = startDir;
        ColorHex = colorHex;
        ProgressInCell = 0.5;
    }

    public void AddCarriage(CarriageType type, string? colorHex = null)
    {
        string color = colorHex ?? (Carriages.Count % 2 == 0 ? "#10B981" : "#F59E0B");
        Carriages.Add(new TrainCarriage(type, color));
    }

    public void SetWorldTransform(double wx, double wy, double angle)
    {
        WorldX = wx;
        WorldY = wy;
        AngleDegrees = angle;
    }

    public void RecordHistory(double wx, double wy, double angle, double distDelta)
    {
        TotalDistanceTraveled += distDelta;
        PositionHistory.Insert(0, new PositionRecord(wx, wy, angle, TotalDistanceTraveled));

        double maxDistanceNeeded = (Carriages.Count + 1) * CarriageSpacing + 1.0;
        while (PositionHistory.Count > 1 &&
               (TotalDistanceTraveled - PositionHistory[^1].DistanceFromStart) > maxDistanceNeeded)
        {
            PositionHistory.RemoveAt(PositionHistory.Count - 1);
        }
    }

    public void UpdateCarriagePositions()
    {
        for (int i = 0; i < Carriages.Count; i++)
        {
            double targetDistance = TotalDistanceTraveled - ((i + 1) * CarriageSpacing);
            var carriage = Carriages[i];

            if (PositionHistory.Count == 0)
            {
                carriage.WorldX = WorldX;
                carriage.WorldY = WorldY;
                carriage.AngleDegrees = AngleDegrees;
                continue;
            }

            bool found = false;
            for (int p = 0; p < PositionHistory.Count - 1; p++)
            {
                var p1 = PositionHistory[p];
                var p2 = PositionHistory[p + 1];

                if (p1.DistanceFromStart >= targetDistance && p2.DistanceFromStart <= targetDistance)
                {
                    double range = p1.DistanceFromStart - p2.DistanceFromStart;
                    double t = range > 0.0001 ? (targetDistance - p2.DistanceFromStart) / range : 0.0;

                    carriage.WorldX = p2.WorldX + (p1.WorldX - p2.WorldX) * t;
                    carriage.WorldY = p2.WorldY + (p1.WorldY - p2.WorldY) * t;

                    double diff = (p1.AngleDegrees - p2.AngleDegrees + 180 + 360) % 360 - 180;
                    carriage.AngleDegrees = (p2.AngleDegrees + diff * t + 360) % 360;
                    found = true;
                    break;
                }
            }

            if (!found)
            {
                var oldest = PositionHistory[^1];
                carriage.WorldX = oldest.WorldX;
                carriage.WorldY = oldest.WorldY;
                carriage.AngleDegrees = oldest.AngleDegrees;
            }
        }
    }

    /// <summary>
    /// Desembarca y embarca pasajeros en una estación dada.
    /// </summary>
    public int BoardPassengersAtStation(Station station)
    {
        // 1. Desembarcar pasajeros
        int disembarked = 0;
        foreach (var c in Carriages)
        {
            if (c.Type == CarriageType.Passenger && c.PassengersOnBoard > 0)
            {
                // Desembarcan la mitad de pasajeros en cada parada
                int leaving = (int)Math.Ceiling(c.PassengersOnBoard * 0.5);
                c.PassengersOnBoard -= leaving;
                disembarked += leaving;
            }
        }
        TotalPassengersDelivered += disembarked;
        station.TotalPassengersDisembarked += disembarked;

        // 2. Embarcar nuevos pasajeros esperando en la estación
        int boarded = 0;
        foreach (var c in Carriages)
        {
            if (c.Type == CarriageType.Passenger && station.WaitingPassengers > 0)
            {
                int space = c.MaxCapacity - c.PassengersOnBoard;
                int toBoard = Math.Min(space, station.WaitingPassengers);
                c.PassengersOnBoard += toBoard;
                station.WaitingPassengers -= toBoard;
                boarded += toBoard;
            }
        }

        return boarded;
    }
}
