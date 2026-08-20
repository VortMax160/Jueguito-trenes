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
}
