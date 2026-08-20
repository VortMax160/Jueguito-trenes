using TrainGame.Core;
using TrainGame.Core.Puzzle;
using Xunit;

namespace TrainGame.Tests;

public class PuzzleTests
{
    [Fact]
    public void PuzzleManager_LoadLevel_InitializesCorrectly()
    {
        var sim = new TrainSimulation(36, 22);
        var puzzle = new PuzzleManager(sim);

        puzzle.LoadLevel(0); // Nivel 1

        Assert.Equal(PuzzleState.Planning, puzzle.State);
        Assert.Equal("Nivel 1: Primeros Raíles", puzzle.CurrentLevel.Title);
        Assert.NotEmpty(puzzle.CurrentLevel.TrainSpawns);
        Assert.NotEmpty(puzzle.CurrentLevel.TargetStations);
        Assert.NotEmpty(puzzle.CurrentLevel.Obstacles);
        Assert.True(puzzle.Scores[1].IsUnlocked);
    }

    [Fact]
    public void PuzzleManager_CompleteLevel1_TriggersVictoryAndUnlocksNext()
    {
        var sim = new TrainSimulation(36, 22);
        var puzzle = new PuzzleManager(sim);
        puzzle.LoadLevel(0);

        // En el Nivel 1: TrainSpawn está en (4, 6) hacia el Este y Estación en (9, 6)
        // Conectamos con vías horizontales en x=5, 6, 7, 8
        sim.Grid.SetTrack(5, 6, TrackType.Horizontal);
        sim.Grid.SetTrack(6, 6, TrackType.Horizontal);
        sim.Grid.SetTrack(7, 6, TrackType.Horizontal);
        sim.Grid.SetTrack(8, 6, TrackType.Horizontal);

        puzzle.StartSimulation();
        Assert.Equal(PuzzleState.Running, puzzle.State);

        // Avanzar simulación suficiente para que el tren llegue a la estación (distancia ~5 casillas a 2.8 c/s -> ~2s)
        for (int step = 0; step < 40; step++)
        {
            sim.Update(0.1);
            puzzle.Update();
            if (puzzle.State == PuzzleState.Victory) break;
        }

        Assert.Equal(PuzzleState.Victory, puzzle.State);
        Assert.True(puzzle.Scores[1].IsCompleted);
        Assert.True(puzzle.Scores[1].StarsEarned >= 1);
        // Nivel 2 debe haberse desbloqueado
        Assert.True(puzzle.Scores[2].IsUnlocked);
    }

    [Fact]
    public void PuzzleManager_Derailment_TriggersDefeat()
    {
        var sim = new TrainSimulation(36, 22);
        var puzzle = new PuzzleManager(sim);
        puzzle.LoadLevel(0);

        // No colocamos vías suficientes (solo en x=5)
        sim.Grid.SetTrack(5, 6, TrackType.Horizontal);

        puzzle.StartSimulation();

        for (int step = 0; step < 20; step++)
        {
            sim.Update(0.1);
            puzzle.Update();
            if (puzzle.State == PuzzleState.Defeat) break;
        }

        Assert.Equal(PuzzleState.Defeat, puzzle.State);
    }

    [Fact]
    public void PuzzleManager_TrackEditing_EnforcesBudgetAndProtectedCells()
    {
        var sim = new TrainSimulation(36, 22);
        var puzzle = new PuzzleManager(sim);
        puzzle.LoadLevel(0);

        for (int x = 0; x < puzzle.CurrentLevel.MaxTrackBudget; x++)
        {
            Assert.True(puzzle.TryPlaceTrack(x, 0, TrackType.Horizontal));
        }

        Assert.Equal(puzzle.CurrentLevel.MaxTrackBudget, puzzle.PlayerTracksPlacedCount);
        Assert.False(puzzle.TryPlaceTrack(0, 1, TrackType.Horizontal));

        // Reemplazar una pieza existente no consume una vía adicional.
        Assert.True(puzzle.TryPlaceTrack(0, 0, TrackType.Vertical));
        Assert.Equal(puzzle.CurrentLevel.MaxTrackBudget, puzzle.PlayerTracksPlacedCount);

        Assert.False(puzzle.TryPlaceTrack(4, 6, TrackType.Horizontal)); // Spawn
        Assert.False(puzzle.TryPlaceTrack(9, 6, TrackType.Horizontal)); // Estación objetivo
        Assert.False(puzzle.TryPlaceTrack(6, 5, TrackType.Horizontal)); // Obstáculo

        puzzle.CurrentLevel.FixedTracks.Add(new FixedTrackInfo { X = 12, Y = 12, Type = TrackType.Horizontal });
        Assert.False(puzzle.TryPlaceTrack(12, 12, TrackType.Horizontal));

        Assert.True(puzzle.TryRemoveTrack(0, 0));
        Assert.Equal(puzzle.CurrentLevel.MaxTrackBudget - 1, puzzle.PlayerTracksPlacedCount);
        Assert.False(puzzle.TryRemoveTrack(4, 6)); // Spawn protegido
    }

    [Fact]
    public void PuzzleManager_ResetToPlanning_RestoresSimulationState()
    {
        var sim = new TrainSimulation(36, 22);
        var puzzle = new PuzzleManager(sim);
        puzzle.LoadLevel(0);
        Assert.True(puzzle.TryAddSignal(4, 6, Direction.East));

        var station = sim.Stations[0];
        station.WaitingPassengers = 27;
        station.TotalPassengersDisembarked = 4;
        station.PassengerSpawnTimer = 3.5;
        sim.Signals[0].ToggleManual();

        puzzle.ResetToPlanning();

        Assert.Equal(PuzzleState.Planning, puzzle.State);
        Assert.Equal(10, station.WaitingPassengers);
        Assert.Equal(0, station.TotalPassengersDisembarked);
        Assert.Equal(0, station.PassengerSpawnTimer);
        Assert.Equal(SignalState.Green, sim.Signals[0].State);
        Assert.False(sim.Signals[0].IsManual);
        Assert.Single(sim.Trains);
        Assert.Equal(4, sim.Trains[0].CellX);
        Assert.Equal(6, sim.Trains[0].CellY);
        Assert.Equal(0.5, sim.Trains[0].ProgressInCell);
        Assert.Equal(0, sim.Trains[0].Speed);
    }

    [Fact]
    public void PuzzleManager_StartSimulationAfterDefeat_RecreatesPlanningTrains()
    {
        var sim = new TrainSimulation(36, 22);
        var puzzle = new PuzzleManager(sim);
        puzzle.LoadLevel(0);

        puzzle.StartSimulation();
        var derailedTrain = sim.Trains[0];
        derailedTrain.IsDerailed = true;
        puzzle.Update();
        Assert.Equal(PuzzleState.Defeat, puzzle.State);

        puzzle.StartSimulation();

        Assert.Equal(PuzzleState.Running, puzzle.State);
        Assert.NotSame(derailedTrain, sim.Trains[0]);
        Assert.False(sim.Trains[0].IsDerailed);
        Assert.Equal(4, sim.Trains[0].CellX);
        Assert.Equal(6, sim.Trains[0].CellY);
    }

    [Fact]
    public void PuzzleManager_TrainTargets_UseStableStationIds()
    {
        var sim = new TrainSimulation(36, 22);
        var puzzle = new PuzzleManager(sim);
        puzzle.Scores[4].IsUnlocked = true;
        puzzle.LoadLevel(3);

        // Los nombres y colores no son identidades únicas.
        puzzle.CurrentLevel.TrainSpawns[0].Name = "Mismo tren";
        puzzle.CurrentLevel.TrainSpawns[1].Name = "Mismo tren";
        puzzle.CurrentLevel.TrainSpawns[0].ColorHex = "#FFFFFF";
        puzzle.CurrentLevel.TrainSpawns[1].ColorHex = "#FFFFFF";

        for (int x = 4; x <= 10; x++)
        {
            sim.Grid.SetTrack(x, 4, TrackType.Horizontal);
            sim.Grid.SetTrack(x, 8, TrackType.Horizontal);
        }

        puzzle.StartSimulation();
        for (int step = 0; step < 40 && puzzle.State == PuzzleState.Running; step++)
        {
            sim.Update(0.1);
            puzzle.Update();
        }

        Assert.Equal(PuzzleState.Victory, puzzle.State);
        Assert.All(puzzle.CurrentLevel.TargetStations, station => Assert.True(station.HasReached));
    }

    [Fact]
    public void PuzzleManager_Level3_Achieves3StarsWithOptimalLayout()
    {
        var sim = new TrainSimulation(36, 22);
        var puzzle = new PuzzleManager(sim);
        puzzle.Scores[3].IsUnlocked = true;
        puzzle.LoadLevel(2); // Level 3

        // Trazado óptimo de 13 vías alrededor de las rocas por el Norte (y=5)
        sim.Grid.SetTrack(4, 7, TrackType.Horizontal);
        sim.Grid.SetTrack(5, 7, TrackType.Horizontal);
        sim.Grid.SetTrack(6, 7, TrackType.CurveWestNorth);
        sim.Grid.SetTrack(6, 6, TrackType.Vertical);
        sim.Grid.SetTrack(6, 5, TrackType.CurveEastSouth);
        sim.Grid.SetTrack(7, 5, TrackType.Horizontal);
        sim.Grid.SetTrack(8, 5, TrackType.Horizontal);
        sim.Grid.SetTrack(9, 5, TrackType.CurveSouthWest);
        sim.Grid.SetTrack(9, 6, TrackType.Vertical);
        sim.Grid.SetTrack(9, 7, TrackType.CurveNorthEast);
        sim.Grid.SetTrack(10, 7, TrackType.Horizontal);
        sim.Grid.SetTrack(11, 7, TrackType.Horizontal);
        sim.Grid.SetTrack(12, 7, TrackType.Horizontal);

        Assert.Equal(13, puzzle.CountPlayerTracks());

        puzzle.StartSimulation();
        for (int step = 0; step < 80 && puzzle.State == PuzzleState.Running; step++)
        {
            sim.Update(0.1);
            puzzle.Update();
        }

        Assert.Equal(PuzzleState.Victory, puzzle.State);
        Assert.Equal(3, puzzle.Scores[3].StarsEarned); // ¡3 estrellas conseguidas!
    }

    [Fact]
    public void TrainSimulation_CurveTransition_NeverJumpsBackwards()
    {
        var sim = new TrainSimulation(10, 10);
        sim.Grid.SetTrack(0, 0, TrackType.Horizontal);
        sim.Grid.SetTrack(1, 0, TrackType.CurveSouthWest); // Entra Este (x=0), sale Sur (y=1)
        sim.Grid.SetTrack(1, 1, TrackType.Vertical);

        var train = sim.SpawnTrain(0, 0, Direction.East, "CurveTrain", carriageCount: 0);
        Assert.NotNull(train);

        double lastX = train.WorldX;
        double lastY = train.WorldY;

        for (int step = 0; step < 20; step++)
        {
            sim.Update(0.05);
            // X debe ser monotónicamente creciente hacia 1.5, luego Y debe ser monotónicamente creciente hacia el Sur
            Assert.True(train.WorldX >= lastX - 0.01, $"X disminuyó inesperadamente: {train.WorldX} vs {lastX}");
            Assert.True(train.WorldY >= lastY - 0.01, $"Y disminuyó inesperadamente: {train.WorldY} vs {lastY}");
            lastX = train.WorldX;
            lastY = train.WorldY;
        }
    }

    [Fact]
    public void PuzzleManager_Level6_OptimalLayoutUses37Tracks()
    {
        var sim = new TrainSimulation(36, 22);
        var puzzle = new PuzzleManager(sim);
        puzzle.Scores[6].IsUnlocked = true;
        puzzle.LoadLevel(5); // Level 6

        // Ruta roja: rodea la roca de (8, 3) por el norte.
        for (int x = 3; x <= 6; x++) sim.Grid.SetTrack(x, 3, TrackType.Horizontal);
        sim.Grid.SetTrack(7, 3, TrackType.CurveWestNorth);
        sim.Grid.SetTrack(7, 2, TrackType.CurveEastSouth);
        sim.Grid.SetTrack(8, 2, TrackType.Horizontal);
        sim.Grid.SetTrack(9, 2, TrackType.CurveSouthWest);
        sim.Grid.SetTrack(9, 3, TrackType.CurveNorthEast);
        for (int x = 10; x <= 13; x++) sim.Grid.SetTrack(x, 3, TrackType.Horizontal);

        // Ruta azul: directa por la fila central.
        for (int x = 3; x <= 13; x++) sim.Grid.SetTrack(x, 7, TrackType.Horizontal);

        // Ruta verde: rodea la roca de (8, 11) por el sur.
        for (int x = 3; x <= 6; x++) sim.Grid.SetTrack(x, 11, TrackType.Horizontal);
        sim.Grid.SetTrack(7, 11, TrackType.CurveSouthWest);
        sim.Grid.SetTrack(7, 12, TrackType.CurveNorthEast);
        sim.Grid.SetTrack(8, 12, TrackType.Horizontal);
        sim.Grid.SetTrack(9, 12, TrackType.CurveWestNorth);
        sim.Grid.SetTrack(9, 11, TrackType.CurveEastSouth);
        for (int x = 10; x <= 13; x++) sim.Grid.SetTrack(x, 11, TrackType.Horizontal);

        Assert.Equal(37, puzzle.CountPlayerTracks());

        puzzle.StartSimulation();
        for (int step = 0; step < 100 && puzzle.State == PuzzleState.Running; step++)
        {
            sim.Update(0.1);
            puzzle.Update();
        }

        Assert.Equal(PuzzleState.Victory, puzzle.State);
        Assert.Equal(3, puzzle.Scores[6].StarsEarned);
    }
}
