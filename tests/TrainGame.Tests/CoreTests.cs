using TrainGame.Core;
using Xunit;

namespace TrainGame.Tests;

public class CoreTests
{
    [Fact]
    public void DirectionOpposite_ReturnsCorrectOpposites()
    {
        Assert.Equal(Direction.South, Direction.North.Opposite());
        Assert.Equal(Direction.North, Direction.South.Opposite());
        Assert.Equal(Direction.West, Direction.East.Opposite());
        Assert.Equal(Direction.East, Direction.West.Opposite());
    }

    [Fact]
    public void TrackPiece_Horizontal_AllowsEastAndWest()
    {
        var track = new TrackPiece(0, 0, TrackType.Horizontal);

        Assert.True(track.HasConnection(Direction.East));
        Assert.True(track.HasConnection(Direction.West));
        Assert.False(track.HasConnection(Direction.North));
        Assert.False(track.HasConnection(Direction.South));

        // Un tren que viaja hacia el Este entra desde el Oeste y sale hacia el Este
        Assert.Equal(Direction.East, track.GetExitDirection(Direction.East));
        Assert.Equal(Direction.West, track.GetExitDirection(Direction.West));
        Assert.Null(track.GetExitDirection(Direction.North));
    }

    [Fact]
    public void TrackPiece_CurveNorthEast_TurnsProperly()
    {
        var curve = new TrackPiece(0, 0, TrackType.CurveNorthEast);

        // Conecta Norte y Este
        Assert.True(curve.HasConnection(Direction.North));
        Assert.True(curve.HasConnection(Direction.East));
        Assert.False(curve.HasConnection(Direction.South));
        Assert.False(curve.HasConnection(Direction.West));

        // Tren viajando hacia el Sur entra por el Norte -> debe girar hacia el Este
        Assert.Equal(Direction.East, curve.GetExitDirection(Direction.South));

        // Tren viajando hacia el Oeste entra por el Este -> debe girar hacia el Norte
        Assert.Equal(Direction.North, curve.GetExitDirection(Direction.West));

        // Tren viajando hacia el Norte entra por el Sur (no conectado) -> descarrila
        Assert.Null(curve.GetExitDirection(Direction.North));
    }

    [Fact]
    public void TrackPiece_Cross_AllowsPerpendicularTravel()
    {
        var cross = new TrackPiece(0, 0, TrackType.Cross);

        Assert.True(cross.HasConnection(Direction.North));
        Assert.True(cross.HasConnection(Direction.South));
        Assert.True(cross.HasConnection(Direction.East));
        Assert.True(cross.HasConnection(Direction.West));

        Assert.Equal(Direction.East, cross.GetExitDirection(Direction.East));
        Assert.Equal(Direction.South, cross.GetExitDirection(Direction.South));
        Assert.Equal(Direction.West, cross.GetExitDirection(Direction.West));
        Assert.Equal(Direction.North, cross.GetExitDirection(Direction.North));
    }

    [Fact]
    public void RailGrid_SmartPlaceTrack_SelectsCurveWhenConnectingNeighbors()
    {
        var grid = new RailGrid(10, 10);
        // Colocar una vía al Norte (0, 0) y una al Este (1, 1)
        grid.SetTrack(0, 0, TrackType.Vertical);
        grid.SetTrack(1, 1, TrackType.Horizontal);

        // Colocar una vía en (0, 1) con auto-conexión
        var piece = grid.SmartPlaceTrack(0, 1);

        // Debería ser CurveNorthEast (conecta Norte con Este)
        Assert.Equal(TrackType.CurveNorthEast, piece.Type);
    }

    [Fact]
    public void TrainSimulation_SpawnTrainAndAdvance_MovesSuccessfully()
    {
        var sim = new TrainSimulation(10, 10);
        sim.Grid.SetTrack(0, 0, TrackType.Horizontal);
        sim.Grid.SetTrack(1, 0, TrackType.Horizontal);
        sim.Grid.SetTrack(2, 0, TrackType.Horizontal);

        var train = sim.SpawnTrain(0, 0, Direction.East, "TestTrain", carriageCount: 1);
        Assert.NotNull(train);
        Assert.Equal(0, train.CellX);
        Assert.Equal(0, train.CellY);

        // Avanzar la simulación 0.3 segundos (a 3 casillas/seg recorre ~0.9 casillas)
        sim.Update(0.3);

        Assert.False(train.IsDerailed);
        Assert.True(train.CellX > 0 || train.ProgressInCell > 0.5);
    }

    [Fact]
    public void TrainSimulation_DerailsWhenTrackEnds()
    {
        var sim = new TrainSimulation(10, 10);
        sim.Grid.SetTrack(0, 0, TrackType.Horizontal);
        // No hay vía en (1, 0)

        var train = sim.SpawnTrain(0, 0, Direction.East, "DerailTrain", carriageCount: 0);
        Assert.NotNull(train);

        // Avanzar suficiente para cruzar la celda
        sim.Update(1.0);

        Assert.True(train.IsDerailed);
        Assert.Equal(0, train.Speed);
    }

    [Fact]
    public void TrainSimulation_PresetsLoadCorrectly()
    {
        var sim = new TrainSimulation(36, 22);

        sim.LoadOvalPreset();
        Assert.NotEmpty(sim.Trains);
        Assert.NotEmpty(sim.Stations);
        Assert.NotEmpty(sim.Signals);

        sim.LoadFigureEightPreset();
        Assert.Equal(2, sim.Trains.Count);

        sim.LoadSignaledNetworkPreset();
        Assert.Equal(3, sim.Trains.Count);
        Assert.Equal(3, sim.Stations.Count);
    }

    [Fact]
    public void TrainSimulation_AntiCollision_PreventsTrainCrash()
    {
        var sim = new TrainSimulation(20, 10);
        for (int x = 0; x < 15; x++)
        {
            sim.Grid.SetTrack(x, 2, TrackType.Horizontal);
        }

        // Tren 1 parado en x=5
        var leadTrain = sim.SpawnTrain(5, 2, Direction.East, "LeadTrain", carriageCount: 1);
        leadTrain!.Speed = 0;
        leadTrain.TargetSpeed = 0;

        // Tren 2 detrás en x=2 a toda velocidad hacia el Tren 1
        var trailTrain = sim.SpawnTrain(2, 2, Direction.East, "TrailTrain", carriageCount: 1);
        trailTrain!.Speed = 4.0;
        trailTrain.TargetSpeed = 4.0;

        // Simular 2 segundos
        sim.Update(2.0);

        // El tren de atrás no debe haber descarrilado y debe haber activado el frenado anti-choque
        Assert.False(trailTrain.IsDerailed);
        Assert.True(trailTrain.IsBrakingForCollision || trailTrain.Speed < 2.0);
        // Debe mantener distancia (no sobrepasar al tren delantero)
        Assert.True(trailTrain.CellX <= leadTrain.CellX);
    }

    [Fact]
    public void TrainSimulation_Station_BoardsPassengers()
    {
        var station = new Station(0, 0, "Test Station");
        station.WaitingPassengers = 15;

        var train = new Train(1, "Passenger Express", 0, 0, Direction.East);
        train.AddCarriage(CarriageType.Passenger); // Capacidad: 16

        int boarded = train.BoardPassengersAtStation(station);

        Assert.Equal(15, boarded);
        Assert.Equal(15, train.TotalPassengersAboard);
        Assert.Equal(0, station.WaitingPassengers);
    }

    [Fact]
    public void TrainSimulation_Signals_TurnRedWhenOccupied()
    {
        var sim = new TrainSimulation(20, 10);
        for (int x = 0; x < 10; x++)
        {
            sim.Grid.SetTrack(x, 1, TrackType.Horizontal);
        }

        sim.AddSignal(2, 1, Direction.East);
        var signal = sim.Signals[0];
        Assert.Equal(SignalState.Green, signal.State);

        // Colocar un tren en el bloque protegido por el semáforo (x=4)
        sim.SpawnTrain(4, 1, Direction.East, "BlockerTrain", carriageCount: 1);

        sim.Update(0.1);

        // El semáforo debe haberse puesto en ROJO automáticamente
        Assert.Equal(SignalState.Red, signal.State);
    }
}
