using System;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using TrainGame.Core;
using TrainGame.Core.Puzzle;
using TrainGame.Rendering;

namespace TrainGame.UI;

public enum ToolMode
{
    AutoTrack,
    StraightH,
    StraightV,
    CurveNE,
    CurveES,
    CurveSW,
    CurveWN,
    Cross,
    StationH,
    StationV,
    Signal,
    SpawnTrain,
    DeleteTrack,
    Select
}

public class GameCanvas : FrameworkElement
{
    public TrainSimulation Simulation { get; }
    public PuzzleManager? PuzzleManager { get; set; }

    private readonly GameRenderer _renderer = new();
    private readonly ParticleSystem _particles = new();
    private readonly Stopwatch _stopwatch = new();
    private double _lastFrameTime;
    private double _smokeTimer;

    // Vista y navegación
    public double CellSize { get; set; } = 48.0;
    public Point PanOffset { get; set; } = new(0, 0);

    // Herramientas y selección
    public ToolMode CurrentTool { get; set; } = ToolMode.AutoTrack;
    public Train? SelectedTrain { get; private set; }

    public event Action<Train?>? OnSelectedTrainChanged;
    public event Action? OnMapStatsChanged;

    // Estado del mouse
    private bool _isDrawing;
    private bool _isDeleting;
    private bool _isPanning;
    private Point _lastMousePos;
    private Point? _hoverCell;
    private (int x, int y)? _lastEditedCell;

    public GameCanvas()
    {
        Simulation = new TrainSimulation(36, 22);
        PuzzleManager = new PuzzleManager(Simulation);
        ClipToBounds = true;
        Focusable = true;

        Loaded += OnLoaded;
        Unloaded += OnUnloaded;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        _stopwatch.Start();
        _lastFrameTime = _stopwatch.Elapsed.TotalSeconds;
        CompositionTarget.Rendering += OnRenderFrame;
    }

    private void OnUnloaded(object sender, RoutedEventArgs e)
    {
        CompositionTarget.Rendering -= OnRenderFrame;
        _stopwatch.Stop();
    }

    private void OnRenderFrame(object? sender, EventArgs e)
    {
        double currentTime = _stopwatch.Elapsed.TotalSeconds;
        double dt = currentTime - _lastFrameTime;
        _lastFrameTime = currentTime;

        dt = Math.Min(dt, 0.05);

        Simulation.Update(dt);
        PuzzleManager?.Update();

        _smokeTimer += dt;
        if (_smokeTimer >= 0.12)
        {
            _smokeTimer = 0;
            foreach (var train in Simulation.Trains)
            {
                if (!train.IsDerailed && train.Speed > 0.3)
                {
                    _particles.EmitSmoke(train.WorldX, train.WorldY, train.AngleDegrees);
                }
            }
        }
        _particles.Update(dt);

        InvalidateVisual();
    }

    protected override void OnRender(DrawingContext dc)
    {
        base.OnRender(dc);

        dc.PushTransform(new TranslateTransform(PanOffset.X, PanOffset.Y));

        TrackType? previewTrack = GetTrackTypeForTool(CurrentTool);
        _renderer.Render(dc, Simulation, CellSize, _hoverCell, previewTrack, SelectedTrain, _particles, PuzzleManager);

        dc.Pop();
    }

    #region Interacción con Mouse

    protected override void OnMouseDown(MouseButtonEventArgs e)
    {
        base.OnMouseDown(e);
        Focus();

        var pos = e.GetPosition(this);
        var worldPos = ScreenToWorld(pos);
        int cx = (int)Math.Floor(worldPos.X);
        int cy = (int)Math.Floor(worldPos.Y);

        if (e.MiddleButton == MouseButtonState.Pressed || Keyboard.IsKeyDown(Key.Space))
        {
            _isPanning = true;
            _lastMousePos = pos;
            CaptureMouse();
            return;
        }

        if (e.LeftButton == MouseButtonState.Pressed)
        {
            // 1. Clic en un tren
            var clickedTrain = FindTrainAt(worldPos);
            if (clickedTrain != null)
            {
                SelectTrain(clickedTrain);
                return;
            }

            var clickedTrack = Simulation.Grid.GetTrack(cx, cy);

            // 2. Clic en semáforo para conmutarlo
            if (clickedTrack?.Signal != null && CurrentTool == ToolMode.Select)
            {
                clickedTrack.Signal.ToggleManual();
                InvalidateVisual();
                return;
            }

            // 3. Clic en desvío interactivo
            if (clickedTrack?.IsSwitch == true && CurrentTool == ToolMode.Select)
            {
                clickedTrack.ToggleSwitch();
                InvalidateVisual();
                return;
            }

            if (CurrentTool == ToolMode.SpawnTrain)
            {
                if (Simulation.Grid.HasTrack(cx, cy))
                {
                    var train = Simulation.SpawnTrain(cx, cy, Direction.East);
                    SelectTrain(train);
                    OnMapStatsChanged?.Invoke();
                }
            }
            else if (CurrentTool == ToolMode.Signal)
            {
                if (clickedTrack != null)
                {
                    if (clickedTrack.Signal == null)
                    {
                        var (d1, _) = clickedTrack.GetConnectedPair();
                        Simulation.AddSignal(cx, cy, d1);
                    }
                    else
                    {
                        clickedTrack.Signal.ToggleManual();
                    }
                    OnMapStatsChanged?.Invoke();
                    InvalidateVisual();
                }
            }
            else if (CurrentTool == ToolMode.Select)
            {
                SelectTrain(null);
            }
            else if (CurrentTool == ToolMode.DeleteTrack)
            {
                _isDeleting = true;
                ApplyDelete(cx, cy);
                CaptureMouse();
            }
            else
            {
                _isDrawing = true;
                _lastEditedCell = (cx, cy);
                ApplyTool(cx, cy);
                CaptureMouse();
            }
        }
        else if (e.RightButton == MouseButtonState.Pressed)
        {
            _isDeleting = true;
            ApplyDelete(cx, cy);
            CaptureMouse();
        }
    }

    protected override void OnMouseMove(MouseEventArgs e)
    {
        base.OnMouseMove(e);
        var pos = e.GetPosition(this);
        var worldPos = ScreenToWorld(pos);
        int cx = (int)Math.Floor(worldPos.X);
        int cy = (int)Math.Floor(worldPos.Y);

        _hoverCell = new Point(cx, cy);

        if (_isPanning)
        {
            var delta = pos - _lastMousePos;
            PanOffset = new Point(PanOffset.X + delta.X, PanOffset.Y + delta.Y);
            _lastMousePos = pos;
            InvalidateVisual();
            return;
        }

        if (_isDrawing && (cx, cy) != _lastEditedCell)
        {
            _lastEditedCell = (cx, cy);
            ApplyTool(cx, cy);
        }
        else if (_isDeleting)
        {
            ApplyDelete(cx, cy);
        }
    }

    protected override void OnMouseUp(MouseButtonEventArgs e)
    {
        base.OnMouseUp(e);
        _isDrawing = false;
        _isDeleting = false;
        _isPanning = false;
        _lastEditedCell = null;
        ReleaseMouseCapture();
    }

    protected override void OnMouseWheel(MouseWheelEventArgs e)
    {
        base.OnMouseWheel(e);

        var pos = e.GetPosition(this);
        double zoomFactor = e.Delta > 0 ? 1.15 : 0.87;
        double newCellSize = Math.Clamp(CellSize * zoomFactor, 24.0, 120.0);

        if (Math.Abs(newCellSize - CellSize) > 0.01)
        {
            double scale = newCellSize / CellSize;
            PanOffset = new Point(
                pos.X - (pos.X - PanOffset.X) * scale,
                pos.Y - (pos.Y - PanOffset.Y) * scale
            );
            CellSize = newCellSize;
            InvalidateVisual();
        }
    }

    #endregion

    #region Herramientas de Edición

    public void SelectTrain(Train? train)
    {
        SelectedTrain = train;
        OnSelectedTrainChanged?.Invoke(train);
        InvalidateVisual();
    }

    private void ApplyTool(int x, int y)
    {
        if (!Simulation.Grid.IsInBounds(x, y)) return;

        // Si hay un obstáculo en este punto, no se puede construir
        if (PuzzleManager?.CurrentLevel.Obstacles.Any(o => o.X == x && o.Y == y) == true)
        {
            return;
        }

        // Si es una estación objetivo fija del nivel, no sobrescribirla
        if (PuzzleManager?.CurrentLevel.TargetStations.Any(st => st.X == x && st.Y == y) == true)
        {
            return;
        }

        switch (CurrentTool)
        {
            case ToolMode.AutoTrack:
                Simulation.Grid.SmartPlaceTrack(x, y);
                break;
            case ToolMode.StraightH:
                Simulation.Grid.SetTrack(x, y, TrackType.Horizontal);
                break;
            case ToolMode.StraightV:
                Simulation.Grid.SetTrack(x, y, TrackType.Vertical);
                break;
            case ToolMode.CurveNE:
                Simulation.Grid.SetTrack(x, y, TrackType.CurveNorthEast);
                break;
            case ToolMode.CurveES:
                Simulation.Grid.SetTrack(x, y, TrackType.CurveEastSouth);
                break;
            case ToolMode.CurveSW:
                Simulation.Grid.SetTrack(x, y, TrackType.CurveSouthWest);
                break;
            case ToolMode.CurveWN:
                Simulation.Grid.SetTrack(x, y, TrackType.CurveWestNorth);
                break;
            case ToolMode.Cross:
                Simulation.Grid.SetTrack(x, y, TrackType.Cross);
                break;
            case ToolMode.StationH:
                Simulation.AddStation(x, y, $"Estación #{Simulation.Stations.Count + 1}", true);
                break;
            case ToolMode.StationV:
                Simulation.AddStation(x, y, $"Terminal #{Simulation.Stations.Count + 1}", false);
                break;
            case ToolMode.Signal:
                Simulation.AddSignal(x, y, Direction.East);
                break;
        }

        PuzzleManager?.UpdateTrackCount();
        OnMapStatsChanged?.Invoke();
        InvalidateVisual();
    }

    private void ApplyDelete(int x, int y)
    {
        // No permitir borrar estaciones fijas del puzzle
        if (PuzzleManager?.CurrentLevel.TargetStations.Any(st => st.X == x && st.Y == y) == true)
        {
            return;
        }

        if (Simulation.Grid.RemoveTrack(x, y))
        {
            PuzzleManager?.UpdateTrackCount();
            OnMapStatsChanged?.Invoke();
            InvalidateVisual();
        }
    }

    private Train? FindTrainAt(Point worldPos)
    {
        foreach (var train in Simulation.Trains)
        {
            double distSq = Math.Pow(train.WorldX - worldPos.X, 2) + Math.Pow(train.WorldY - worldPos.Y, 2);
            if (distSq < 0.36)
            {
                return train;
            }

            foreach (var wagon in train.Carriages)
            {
                double wDistSq = Math.Pow(wagon.WorldX - worldPos.X, 2) + Math.Pow(wagon.WorldY - worldPos.Y, 2);
                if (wDistSq < 0.36)
                {
                    return train;
                }
            }
        }
        return null;
    }

    private Point ScreenToWorld(Point screenPos)
    {
        return new Point(
            (screenPos.X - PanOffset.X) / CellSize,
            (screenPos.Y - PanOffset.Y) / CellSize
        );
    }

    private static TrackType? GetTrackTypeForTool(ToolMode tool) => tool switch
    {
        ToolMode.StraightH => TrackType.Horizontal,
        ToolMode.StraightV => TrackType.Vertical,
        ToolMode.CurveNE => TrackType.CurveNorthEast,
        ToolMode.CurveES => TrackType.CurveEastSouth,
        ToolMode.CurveSW => TrackType.CurveSouthWest,
        ToolMode.CurveWN => TrackType.CurveWestNorth,
        ToolMode.Cross => TrackType.Cross,
        ToolMode.StationH => TrackType.StationHorizontal,
        ToolMode.StationV => TrackType.StationVertical,
        _ => null
    };

    public void ResetView()
    {
        PanOffset = new Point(40, 40);
        CellSize = 52.0;
        InvalidateVisual();
    }

    #endregion
}
