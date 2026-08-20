using System;

namespace TrainGame.Core;

public enum SignalState
{
    Green,  // Vía libre
    Red,    // Ocupada / Alto
    Yellow  // Precaución / Reducir velocidad
}

public class RailwaySignal
{
    public int X { get; }
    public int Y { get; }
    public SignalState State { get; set; } = SignalState.Green;
    public Direction FacingDirection { get; set; } = Direction.East;
    public bool IsManual { get; set; } // Si el usuario la fijó manualmente o es automática

    public RailwaySignal(int x, int y, Direction facingDir = Direction.East)
    {
        X = x;
        Y = y;
        FacingDirection = facingDir;
    }

    public void ToggleManual()
    {
        IsManual = true;
        State = State == SignalState.Green ? SignalState.Red : SignalState.Green;
    }

    public void Reset()
    {
        State = SignalState.Green;
        IsManual = false;
    }
}

public class Station
{
    public int X { get; }
    public int Y { get; }
    public string Name { get; set; }
    public int WaitingPassengers { get; set; } = 10;
    public int TotalPassengersDisembarked { get; set; } = 0;
    public double PassengerSpawnTimer { get; set; } = 0;

    public Station(int x, int y, string name)
    {
        X = x;
        Y = y;
        Name = name;
    }

    public void Update(double dt)
    {
        // Generar nuevos pasajeros periódicamente hasta un máximo de 30
        PassengerSpawnTimer += dt;
        if (PassengerSpawnTimer >= 5.0)
        {
            PassengerSpawnTimer = 0;
            if (WaitingPassengers < 30)
            {
                WaitingPassengers += new Random().Next(1, 4);
            }
        }
    }

    public void Reset()
    {
        WaitingPassengers = 10;
        TotalPassengersDisembarked = 0;
        PassengerSpawnTimer = 0;
    }
}
