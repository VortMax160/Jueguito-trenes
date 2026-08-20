using System;
using System.Collections.Generic;
using System.Linq;

namespace TrainGame.Core.Puzzle;

public enum PuzzleState
{
    Planning, // Colocando vías, trenes listos en salida
    Running,  // Simulación activa, trenes en movimiento hacia estaciones
    Victory,  // Todos los trenes llegaron con éxito a sus estaciones de destino
    Defeat    // Un tren descarriló o colisionó
}

public class PuzzleManager
{
    public TrainSimulation Simulation { get; }
    public List<PuzzleLevel> Levels { get; }
    public int CurrentLevelIndex { get; private set; } = 0;
    public PuzzleLevel CurrentLevel => Levels[CurrentLevelIndex];

    public PuzzleState State { get; private set; } = PuzzleState.Planning;
    public Dictionary<int, LevelScore> Scores { get; } = new();

    public int PlayerTracksPlacedCount { get; private set; } = 0;
    public string StatusMessage { get; private set; } = "Planifica el trazado de vías y pulsa ¡ARRANCAR TRENES!";

    public event Action? OnPuzzleStateChanged;

    public PuzzleManager(TrainSimulation simulation)
    {
        Simulation = simulation;
        Levels = LevelLibrary.GetAllLevels();

        for (int i = 0; i < Levels.Count; i++)
        {
            Scores[Levels[i].Id] = new LevelScore
            {
                LevelId = Levels[i].Id,
                IsUnlocked = i == 0, // El primer nivel empieza desbloqueado
                IsCompleted = false,
                StarsEarned = 0,
                BestTracksUsed = 999
            };
        }
    }

    public void LoadLevel(int index)
    {
        if (index < 0 || index >= Levels.Count) return;
        if (!Scores[Levels[index].Id].IsUnlocked) return;

        CurrentLevelIndex = index;
        State = PuzzleState.Planning;
        StatusMessage = "Planifica el trazado de vías y pulsa ¡ARRANCAR TRENES!";

        // Limpiar simulación
        Simulation.ClearAll();
        Simulation.IsPaused = true;

        var lvl = CurrentLevel;

        // 1. Colocar Vías Fijas
        foreach (var ft in lvl.FixedTracks)
        {
            Simulation.Grid.SetTrack(ft.X, ft.Y, ft.Type);
        }

        // 2. Colocar Estaciones Objetivo
        foreach (var st in lvl.TargetStations)
        {
            st.HasReached = false;
            Simulation.AddStation(st.X, st.Y, st.Name, st.IsHorizontal);
            var track = Simulation.Grid.GetTrack(st.X, st.Y);
            if (track?.Station != null)
            {
                // Ajustar color si corresponde
                track.Station.Name = st.Name;
            }
        }

        // 3. Colocar Vías de Salida en los Spawns para que el tren tenga soporte inicial
        foreach (var sp in lvl.TrainSpawns)
        {
            TrackType spawnTrackType = sp.Direction is Direction.East or Direction.West ? TrackType.Horizontal : TrackType.Vertical;
            Simulation.Grid.SetTrack(sp.X, sp.Y, spawnTrackType);
        }

        // 4. Previsualizar trenes en Planning (estáticos)
        SpawnPlanningTrains();

        UpdateTrackCount();
        OnPuzzleStateChanged?.Invoke();
    }

    private void SpawnPlanningTrains()
    {
        Simulation.Trains.Clear();
        foreach (var sp in CurrentLevel.TrainSpawns)
        {
            var train = Simulation.SpawnTrain(sp.X, sp.Y, sp.Direction, sp.Name, sp.CarriageCount, sp.ColorHex);
            if (train != null)
            {
                train.TargetStationId = sp.TargetStationId;
                train.Speed = 0;
                train.TargetSpeed = 0;
                train.ProgressInCell = 0.5;
            }
        }
    }

    public void StartSimulation()
    {
        if (State != PuzzleState.Planning && State != PuzzleState.Defeat) return;

        if (State == PuzzleState.Defeat)
        {
            ResetToPlanning();
        }

        UpdateTrackCount();
        if (PlayerTracksPlacedCount > CurrentLevel.MaxTrackBudget)
        {
            StatusMessage = $"Presupuesto excedido: máximo {CurrentLevel.MaxTrackBudget} vías.";
            Simulation.IsPaused = true;
            OnPuzzleStateChanged?.Invoke();
            return;
        }

        State = PuzzleState.Running;
        StatusMessage = "¡Trenes en marcha! Esperando que alcancen sus estaciones...";

        // Arrancar todos los trenes
        foreach (var t in Simulation.Trains)
        {
            t.TargetSpeed = 2.8;
            t.Speed = 2.8;
            t.IsDerailed = false;
        }

        Simulation.IsPaused = false;
        OnPuzzleStateChanged?.Invoke();
    }

    public void ResetToPlanning()
    {
        State = PuzzleState.Planning;
        Simulation.IsPaused = true;
        StatusMessage = "Modo Planificación: Ajusta tus vías.";

        // Reiniciar estado de estaciones
        foreach (var st in CurrentLevel.TargetStations)
        {
            st.HasReached = false;
        }

        Simulation.ResetTransientState();

        // Reposicionar trenes en sus salidas
        SpawnPlanningTrains();
        UpdateTrackCount();
        OnPuzzleStateChanged?.Invoke();
    }

    public void Update()
    {
        if (State != PuzzleState.Running) return;

        var lvl = CurrentLevel;
        bool allReached = true;

        // Comprobar cada tren
        foreach (var train in Simulation.Trains)
        {
            if (train.IsDerailed)
            {
                State = PuzzleState.Defeat;
                Simulation.IsPaused = true;
                StatusMessage = "💥 ¡Un tren ha descarrilado! Revisa el trazado e inténtalo de nuevo.";
                OnPuzzleStateChanged?.Invoke();
                return;
            }

            // Comprobar si llegó a su estación objetivo
            var targetSt = train.TargetStationId.HasValue
                ? lvl.TargetStations.FirstOrDefault(st => st.Id == train.TargetStationId.Value)
                : null;
            if (targetSt != null)
            {
                if (train.CellX == targetSt.X && train.CellY == targetSt.Y && Math.Abs(train.ProgressInCell - 0.5) < 0.25)
                {
                    targetSt.HasReached = true;
                    train.Speed = 0;
                    train.TargetSpeed = 0;
                }
            }
        }

        // Comprobar si todas las estaciones objetivo fueron alcanzadas
        foreach (var st in lvl.TargetStations)
        {
            if (!st.HasReached)
            {
                allReached = false;
                break;
            }
        }

        if (allReached)
        {
            State = PuzzleState.Victory;
            Simulation.IsPaused = true;

            int tracksUsed = CountPlayerTracks();
            int stars = 1;
            if (tracksUsed <= lvl.Star3Budget) stars = 3;
            else if (tracksUsed <= lvl.Star2Budget) stars = 2;

            var score = Scores[lvl.Id];
            score.IsCompleted = true;
            score.StarsEarned = Math.Max(score.StarsEarned, stars);
            score.BestTracksUsed = Math.Min(score.BestTracksUsed, tracksUsed);

            // Desbloquear siguiente nivel
            if (CurrentLevelIndex + 1 < Levels.Count)
            {
                Scores[Levels[CurrentLevelIndex + 1].Id].IsUnlocked = true;
            }

            StatusMessage = $"🎉 ¡NIVEL COMPLETADO! Has obtenido {new string('⭐', stars)} ({tracksUsed} vías usadas)";
            OnPuzzleStateChanged?.Invoke();
        }
    }

    public int CountPlayerTracks()
    {
        int count = 0;
        foreach (var track in Simulation.Grid.GetAllTracks())
        {
            bool isTargetStation = CurrentLevel.TargetStations.Any(st => st.X == track.X && st.Y == track.Y);
            bool isTrainSpawn = CurrentLevel.TrainSpawns.Any(sp => sp.X == track.X && sp.Y == track.Y);
            bool isFixedTrack = CurrentLevel.FixedTracks.Any(ft => ft.X == track.X && ft.Y == track.Y);

            if (!isTargetStation && !isTrainSpawn && !isFixedTrack)
            {
                count++;
            }
        }
        return count;
    }

    public void UpdateTrackCount()
    {
        PlayerTracksPlacedCount = CountPlayerTracks();
    }

    public bool TryPlaceTrack(int x, int y, TrackType type)
    {
        if (!CanEditCell(x, y)) return false;

        var existing = Simulation.Grid.GetTrack(x, y);
        if (existing?.Station != null || existing?.Signal != null) return false;
        if (existing == null && CountPlayerTracks() >= CurrentLevel.MaxTrackBudget) return false;

        Simulation.Grid.SetTrack(x, y, type);
        UpdateTrackCount();
        return true;
    }

    public bool TrySmartPlaceTrack(int x, int y)
    {
        if (!CanEditCell(x, y)) return false;

        var existing = Simulation.Grid.GetTrack(x, y);
        if (existing?.Station != null || existing?.Signal != null) return false;
        if (existing == null && CountPlayerTracks() >= CurrentLevel.MaxTrackBudget) return false;

        Simulation.Grid.SmartPlaceTrack(x, y);
        UpdateTrackCount();
        return true;
    }

    public bool TryAddStation(int x, int y, string name, bool isHorizontal)
    {
        if (!CanEditCell(x, y) || Simulation.Grid.HasTrack(x, y)) return false;
        if (CountPlayerTracks() >= CurrentLevel.MaxTrackBudget) return false;

        Simulation.AddStation(x, y, name, isHorizontal);
        UpdateTrackCount();
        return true;
    }

    public bool TryRemoveTrack(int x, int y)
    {
        if (!CanEditCell(x, y)) return false;

        var track = Simulation.Grid.GetTrack(x, y);
        if (track == null || track.Station != null) return false;

        if (track.Signal != null)
        {
            Simulation.Signals.Remove(track.Signal);
        }

        bool removed = Simulation.Grid.RemoveTrack(x, y);
        if (removed) UpdateTrackCount();
        return removed;
    }

    public bool TryAddSignal(int x, int y, Direction direction)
    {
        var track = Simulation.Grid.GetTrack(x, y);
        if (track == null || track.Signal != null) return false;

        Simulation.AddSignal(x, y, direction);
        return true;
    }

    private bool CanEditCell(int x, int y)
    {
        if (!Simulation.Grid.IsInBounds(x, y)) return false;

        var level = CurrentLevel;
        return !level.Obstacles.Any(o => o.X == x && o.Y == y)
            && !level.TargetStations.Any(st => st.X == x && st.Y == y)
            && !level.FixedTracks.Any(ft => ft.X == x && ft.Y == y)
            && !level.TrainSpawns.Any(sp => sp.X == x && sp.Y == y);
    }

    public void NextLevel()
    {
        if (CurrentLevelIndex + 1 < Levels.Count)
        {
            LoadLevel(CurrentLevelIndex + 1);
        }
    }

    public void PreviousLevel()
    {
        if (CurrentLevelIndex > 0)
        {
            LoadLevel(CurrentLevelIndex - 1);
        }
    }
}
