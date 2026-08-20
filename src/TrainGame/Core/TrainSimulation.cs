using System;
using System.Collections.Generic;

namespace TrainGame.Core;

public class TrainSimulation
{
    public RailGrid Grid { get; }
    public List<Train> Trains { get; } = new();
    public List<Station> Stations { get; } = new();
    public List<RailwaySignal> Signals { get; } = new();

    public bool IsPaused { get; set; }
    public double TimeScale { get; set; } = 1.0;

    private int _nextTrainId = 1;

    public event Action? OnSimulationStep;

    public TrainSimulation(int cols = 36, int rows = 22)
    {
        Grid = new RailGrid(cols, rows);
    }

    public Train? SpawnTrain(int x, int y, Direction dir, string? name = null, int carriageCount = 2, string? colorHex = null)
    {
        var track = Grid.GetTrack(x, y);
        if (track == null) return null;

        if (!track.HasConnection(dir) && !track.HasConnection(dir.Opposite()))
        {
            var (d1, d2) = track.GetConnectedPair();
            dir = d1;
        }

        string trainName = name ?? $"Tren #{_nextTrainId}";
        string color = colorHex ?? GetDefaultTrainColor(_nextTrainId);
        var train = new Train(_nextTrainId++, trainName, x, y, dir, color);

        for (int i = 0; i < carriageCount; i++)
        {
            var type = (i == carriageCount - 1) ? CarriageType.Caboose :
                       (i % 2 == 0) ? CarriageType.Passenger : CarriageType.Freight;
            train.AddCarriage(type);
        }

        var (wx, wy, angle) = TrackGeometry.GetPositionAndAngle(x, y, track.EffectiveType, dir, train.ProgressInCell);
        train.SetWorldTransform(wx, wy, angle);

        for (int h = 0; h < (carriageCount + 2) * 10; h++)
        {
            train.RecordHistory(wx, wy, angle, 0.1);
        }
        train.UpdateCarriagePositions();

        Trains.Add(train);
        return train;
    }

    public void ResetTransientState()
    {
        foreach (var station in Stations)
        {
            station.Reset();
        }

        foreach (var signal in Signals)
        {
            signal.Reset();
        }
    }

    public void RemoveTrain(Train train)
    {
        Trains.Remove(train);
    }

    public void AddSignal(int x, int y, Direction dir = Direction.East)
    {
        var track = Grid.GetTrack(x, y);
        if (track != null)
        {
            var signal = new RailwaySignal(x, y, dir);
            track.Signal = signal;
            Signals.Add(signal);
        }
    }

    public void AddStation(int x, int y, string name, bool isHorizontal = true)
    {
        Grid.SetTrack(x, y, isHorizontal ? TrackType.StationHorizontal : TrackType.StationVertical);
        var track = Grid.GetTrack(x, y);
        if (track != null)
        {
            track.Station = new Station(x, y, name);
            Stations.Add(track.Station);
        }
    }

    public void ClearAll()
    {
        Trains.Clear();
        Stations.Clear();
        Signals.Clear();
        Grid.Clear();
    }

    public void Update(double deltaTimeSeconds)
    {
        if (IsPaused || deltaTimeSeconds <= 0) return;

        double remaining = Math.Min(deltaTimeSeconds * TimeScale, 2.0);
        const double maxStep = 0.033; // ~30 FPS sub-step para precisión

        while (remaining > 0)
        {
            double subDt = Math.Min(remaining, maxStep);

            // 1. Actualizar estaciones (generación de pasajeros)
            foreach (var st in Stations)
            {
                st.Update(subDt);
            }

            // 2. Actualizar semáforos automáticos
            UpdateSignals();

            // 3. Actualizar cada tren
            foreach (var train in Trains)
            {
                UpdateTrain(train, subDt);
            }

            remaining -= subDt;
        }

        OnSimulationStep?.Invoke();
    }

    private void UpdateSignals()
    {
        foreach (var signal in Signals)
        {
            if (signal.IsManual) continue;

            // Detección de ocupación en los 4 bloques siguientes
            bool occupied = false;
            int cx = signal.X;
            int cy = signal.Y;
            Direction dir = signal.FacingDirection;

            for (int i = 0; i < 4; i++)
            {
                var (dx, dy) = dir.ToOffset();
                cx += dx;
                cy += dy;

                var track = Grid.GetTrack(cx, cy);
                if (track == null) break;

                // Comprobar si hay algún tren en esta celda
                foreach (var t in Trains)
                {
                    if (t.CellX == cx && t.CellY == cy)
                    {
                        occupied = true;
                        break;
                    }
                    foreach (var w in t.Carriages)
                    {
                        if ((int)Math.Floor(w.WorldX) == cx && (int)Math.Floor(w.WorldY) == cy)
                        {
                            occupied = true;
                            break;
                        }
                    }
                }
                if (occupied) break;

                var nextDir = track.GetExitDirection(dir);
                if (!nextDir.HasValue) break;
                dir = nextDir.Value;
            }

            signal.State = occupied ? SignalState.Red : SignalState.Green;
        }
    }

    private void UpdateTrain(Train train, double dt)
    {
        if (train.IsDerailed)
        {
            train.StatusMessage = "💥 Descarrilado";
            return;
        }

        var currentTrack = Grid.GetTrack(train.CellX, train.CellY);
        if (currentTrack == null)
        {
            train.IsDerailed = true;
            train.Speed = 0;
            train.StatusMessage = "💥 Descarrilado";
            return;
        }

        // ================= 1. DETECCIÓN ANTI-COLISIÓN Y SEMÁFOROS =================
        bool mustStopForSignal = false;
        bool mustStopForTrainAhead = false;
        double? aheadTrainDistance = CheckTrainAhead(train, maxLookahead: 3.5);

        if (aheadTrainDistance.HasValue)
        {
            if (aheadTrainDistance.Value < 1.4)
            {
                // Frenado de emergencia inmediato
                mustStopForTrainAhead = true;
            }
            else if (aheadTrainDistance.Value < 3.0)
            {
                // Reducir velocidad suavemente para no chocar
                double safeSpeed = Math.Max(0.5, train.TargetSpeed * (aheadTrainDistance.Value / 3.0));
                train.Speed = Math.Max(0.5, train.Speed - train.Acceleration * 1.5 * dt);
            }
        }

        // Comprobar semáforo en celda actual o inmediata siguiente
        if (currentTrack.Signal != null && currentTrack.Signal.State == SignalState.Red && train.ProgressInCell < 0.6)
        {
            mustStopForSignal = true;
        }
        else
        {
            var (dx, dy) = train.TravelDirection.ToOffset();
            var nextTrack = Grid.GetTrack(train.CellX + dx, train.CellY + dy);
            if (nextTrack?.Signal != null && nextTrack.Signal.State == SignalState.Red && train.ProgressInCell > 0.4)
            {
                mustStopForSignal = true;
            }
        }

        train.IsWaitingForSignal = mustStopForSignal;
        train.IsBrakingForCollision = mustStopForTrainAhead;

        // ================= 2. GESTIÓN DE PARADA EN ESTACIÓN =================
        bool isAtStation = currentTrack.Station != null || currentTrack.Type is TrackType.StationHorizontal or TrackType.StationVertical;
        if (isAtStation && Math.Abs(train.ProgressInCell - 0.5) < 0.12 && train.StationWaitTimer <= 0 && train.Speed > 0.5)
        {
            train.StationWaitTimer = 2.5; // Espera de 2.5 segundos
            if (currentTrack.Station != null)
            {
                int boarded = train.BoardPassengersAtStation(currentTrack.Station);
                train.StatusMessage = $"🚉 {currentTrack.Station.Name} (+{boarded} pas)";
            }
            else
            {
                train.StatusMessage = "🚉 En Estación";
            }
        }

        // ================= 3. ACELERACIÓN / FRENADO =================
        if (mustStopForTrainAhead)
        {
            train.Speed = Math.Max(0, train.Speed - train.Acceleration * 3.5 * dt);
            train.StatusMessage = "🛑 Frenado de Seguridad (Tren adelante)";
        }
        else if (mustStopForSignal)
        {
            train.Speed = Math.Max(0, train.Speed - train.Acceleration * 2.5 * dt);
            train.StatusMessage = "🔴 Esperando Semáforo";
        }
        else if (train.StationWaitTimer > 0)
        {
            train.StationWaitTimer -= dt;
            train.Speed = Math.Max(0, train.Speed - train.Acceleration * 2.0 * dt);
            if (train.StationWaitTimer <= 0)
            {
                train.StationWaitTimer = -1.0;
            }
        }
        else
        {
            // Aceleración normal hacia velocidad deseada
            if (train.Speed < train.TargetSpeed)
            {
                train.Speed = Math.Min(train.TargetSpeed, train.Speed + train.Acceleration * dt);
            }
            else if (train.Speed > train.TargetSpeed)
            {
                train.Speed = Math.Max(train.TargetSpeed, train.Speed - train.Acceleration * dt);
            }

            if (!mustStopForSignal && !mustStopForTrainAhead && train.StationWaitTimer <= 0)
            {
                train.StatusMessage = "🟢 En marcha";
            }
        }

        if (!isAtStation && train.StationWaitTimer < 0)
        {
            train.StationWaitTimer = 0;
        }

        if (train.Speed <= 0.001)
        {
            return;
        }

        // ================= 4. AVANCE Y TRANSICIÓN DE CELDAS =================
        double moveDistance = train.Speed * dt;
        train.ProgressInCell += moveDistance;

        while (train.ProgressInCell >= 1.0)
        {
            var (dx, dy) = train.TravelDirection.ToOffset();
            int nextX = train.CellX + dx;
            int nextY = train.CellY + dy;

            var nextTrack = Grid.GetTrack(nextX, nextY);
            if (nextTrack == null)
            {
                train.IsDerailed = true;
                train.Speed = 0;
                train.StatusMessage = "💥 Descarrilado (Fin de vía)";
                break;
            }

            var nextExitDir = nextTrack.GetExitDirection(train.TravelDirection);
            if (!nextExitDir.HasValue)
            {
                train.IsDerailed = true;
                train.Speed = 0;
                train.StatusMessage = "💥 Descarrilado (Vía incompatible)";
                break;
            }

            train.CellX = nextX;
            train.CellY = nextY;
            train.TravelDirection = nextExitDir.Value;
            train.ProgressInCell -= 1.0;
            currentTrack = nextTrack;
        }

        if (!train.IsDerailed)
        {
            var (wx, wy, angle) = TrackGeometry.GetPositionAndAngle(
                train.CellX,
                train.CellY,
                currentTrack.EffectiveType,
                train.TravelDirection,
                train.ProgressInCell);

            train.SetWorldTransform(wx, wy, angle);
            train.RecordHistory(wx, wy, angle, moveDistance);
            train.UpdateCarriagePositions();
        }
    }

    /// <summary>
    /// Escanea la ruta que tiene el tren adelante y retorna la distancia en casillas
    /// al tren o vagón más próximo, o null si la vía está despejada.
    /// </summary>
    private double? CheckTrainAhead(Train train, double maxLookahead = 3.5)
    {
        int checkX = train.CellX;
        int checkY = train.CellY;
        Direction checkDir = train.TravelDirection;
        double accumulatedDistance = 1.0 - train.ProgressInCell;

        while (accumulatedDistance < maxLookahead)
        {
            var (dx, dy) = checkDir.ToOffset();
            checkX += dx;
            checkY += dy;

            var track = Grid.GetTrack(checkX, checkY);
            if (track == null) break;

            // Verificar si hay algún tren en esta celda
            foreach (var other in Trains)
            {
                if (other == train) continue;

                // Comprobar cabeza del otro tren
                if (other.CellX == checkX && other.CellY == checkY)
                {
                    double dist = accumulatedDistance + other.ProgressInCell;
                    return dist;
                }

                // Comprobar vagones del otro tren
                foreach (var w in other.Carriages)
                {
                    if ((int)Math.Floor(w.WorldX) == checkX && (int)Math.Floor(w.WorldY) == checkY)
                    {
                        return accumulatedDistance + 0.5;
                    }
                }
            }

            var nextDir = track.GetExitDirection(checkDir);
            if (!nextDir.HasValue) break;
            checkDir = nextDir.Value;
            accumulatedDistance += 1.0;
        }

        return null;
    }

    private static string GetDefaultTrainColor(int id)
    {
        string[] colors = { "#EF4444", "#3B82F6", "#10B981", "#F59E0B", "#8B5CF6", "#EC4899", "#14B8A6" };
        return colors[(id - 1) % colors.Length];
    }

    #region Presets de Circuitos

    public void LoadOvalPreset()
    {
        ClearAll();

        int startX = 4, startY = 3, width = 18, height = 9;

        Grid.SetTrack(startX, startY, TrackType.CurveEastSouth);
        Grid.SetTrack(startX + width - 1, startY, TrackType.CurveSouthWest);
        Grid.SetTrack(startX + width - 1, startY + height - 1, TrackType.CurveWestNorth);
        Grid.SetTrack(startX, startY + height - 1, TrackType.CurveNorthEast);

        for (int x = startX + 1; x < startX + width - 1; x++)
        {
            if (x == startX + width / 2)
            {
                AddStation(x, startY, "Estación Norte", isHorizontal: true);
                AddStation(x, startY + height - 1, "Estación Sur", isHorizontal: true);
            }
            else
            {
                Grid.SetTrack(x, startY, TrackType.Horizontal);
                Grid.SetTrack(x, startY + height - 1, TrackType.Horizontal);
            }
        }

        for (int y = startY + 1; y < startY + height - 1; y++)
        {
            Grid.SetTrack(startX, y, TrackType.Vertical);
            Grid.SetTrack(startX + width - 1, y, TrackType.Vertical);
        }

        // Semáforos para ordenar el paso
        AddSignal(startX + 3, startY, Direction.East);
        AddSignal(startX + width - 4, startY + height - 1, Direction.West);

        // Tren con pasajeros
        SpawnTrain(startX + 2, startY, Direction.East, "Expreso del Norte", carriageCount: 3, colorHex: "#EF4444");
    }

    public void LoadFigureEightPreset()
    {
        ClearAll();

        int cx = 16, cy = 9, radius = 6;

        Grid.SetTrack(cx, cy, TrackType.Cross);

        // Bucle Superior
        Grid.SetTrack(cx, cy - radius, TrackType.Horizontal);
        Grid.SetTrack(cx - radius, cy - radius, TrackType.CurveEastSouth);
        Grid.SetTrack(cx + radius, cy - radius, TrackType.CurveSouthWest);
        Grid.SetTrack(cx - radius, cy, TrackType.CurveNorthEast);
        Grid.SetTrack(cx + radius, cy, TrackType.CurveWestNorth);

        for (int x = cx - radius + 1; x < cx; x++)
        {
            Grid.SetTrack(x, cy - radius, TrackType.Horizontal);
            Grid.SetTrack(x, cy, TrackType.Horizontal);
        }
        for (int x = cx + 1; x < cx + radius; x++)
        {
            Grid.SetTrack(x, cy - radius, TrackType.Horizontal);
            Grid.SetTrack(x, cy, TrackType.Horizontal);
        }
        for (int y = cy - radius + 1; y < cy; y++)
        {
            Grid.SetTrack(cx - radius, y, TrackType.Vertical);
            Grid.SetTrack(cx + radius, y, TrackType.Vertical);
        }

        // Bucle Inferior
        AddStation(cx, cy + radius, "Estación del Valle", isHorizontal: true);
        Grid.SetTrack(cx - radius, cy + radius, TrackType.CurveNorthEast);
        Grid.SetTrack(cx + radius, cy + radius, TrackType.CurveWestNorth);

        for (int x = cx - radius + 1; x < cx + radius; x++)
        {
            if (x != cx) Grid.SetTrack(x, cy + radius, TrackType.Horizontal);
        }
        for (int y = cy + 1; y < cy + radius; y++)
        {
            Grid.SetTrack(cx, y, TrackType.Vertical);
            Grid.SetTrack(cx - radius, y, TrackType.Vertical);
            Grid.SetTrack(cx + radius, y, TrackType.Vertical);
        }

        // Semáforos en el cruce central para evitar choques
        AddSignal(cx - 1, cy, Direction.East);
        AddSignal(cx, cy - 1, Direction.South);

        SpawnTrain(cx - 3, cy - radius, Direction.East, "Tren Azul", carriageCount: 2, colorHex: "#3B82F6");
        SpawnTrain(cx, cy + 2, Direction.South, "Carguero Esmeralda", carriageCount: 3, colorHex: "#10B981");
    }

    public void LoadSignaledNetworkPreset()
    {
        ClearAll();

        int ox = 2, oy = 2, ow = 30, oh = 16;

        // Vía Principal (Anillo Grande)
        Grid.SetTrack(ox, oy, TrackType.CurveEastSouth);
        Grid.SetTrack(ox + ow - 1, oy, TrackType.CurveSouthWest);
        Grid.SetTrack(ox + ow - 1, oy + oh - 1, TrackType.CurveWestNorth);
        Grid.SetTrack(ox, oy + oh - 1, TrackType.CurveNorthEast);

        for (int x = ox + 1; x < ox + ow - 1; x++)
        {
            if (x == ox + 6) AddStation(x, oy, "Terminal Oeste", true);
            else if (x == ox + 22) AddStation(x, oy, "Estación Central", true);
            else if (x == ox + 14) AddStation(x, oy + oh - 1, "Puerto Sur", true);
            else
            {
                Grid.SetTrack(x, oy, TrackType.Horizontal);
                Grid.SetTrack(x, oy + oh - 1, TrackType.Horizontal);
            }
        }
        for (int y = oy + 1; y < oy + oh - 1; y++)
        {
            Grid.SetTrack(ox, y, TrackType.Vertical);
            Grid.SetTrack(ox + ow - 1, y, TrackType.Vertical);
        }

        // Semáforos por cantones para que múltiples trenes compartan la misma vía sin colisionar
        AddSignal(ox + 4, oy, Direction.East);
        AddSignal(ox + 16, oy, Direction.East);
        AddSignal(ox + ow - 1, oy + 4, Direction.South);
        AddSignal(ox + 20, oy + oh - 1, Direction.West);
        AddSignal(ox + 8, oy + oh - 1, Direction.West);
        AddSignal(ox, oy + oh - 6, Direction.North);

        // 3 Trenes de pasajeros simultáneos en el mismo circuito
        SpawnTrain(ox + 2, oy, Direction.East, "Intercity Alfa", carriageCount: 3, colorHex: "#EF4444");
        SpawnTrain(ox + 18, oy, Direction.East, "Cercanías Beta", carriageCount: 2, colorHex: "#3B82F6");
        SpawnTrain(ox + ow - 1, oy + 6, Direction.South, "Expreso Gamma", carriageCount: 3, colorHex: "#8B5CF6");
    }

    public void LoadDualLoopPreset()
    {
        LoadSignaledNetworkPreset();
    }

    #endregion
}
