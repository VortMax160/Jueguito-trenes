using System;
using System.Globalization;
using System.Windows;
using System.Windows.Media;
using TrainGame.Core;
using TrainGame.Core.Puzzle;

namespace TrainGame.Rendering;

public class GameRenderer
{
    private readonly LinearGradientBrush _worldBrush;
    private readonly LinearGradientBrush _groundBrush;
    private readonly LinearGradientBrush _boardTopBrush;
    private readonly LinearGradientBrush _boardSideBrush;
    private readonly SolidColorBrush _farHillBrush = new(Color.FromArgb(150, 36, 56, 74));
    private readonly SolidColorBrush _nearHillBrush = new(Color.FromArgb(190, 28, 52, 59));
    private readonly SolidColorBrush _terrainDetailBrush = new(Color.FromArgb(45, 125, 170, 135));
    private readonly SolidColorBrush _sunGlowBrush = new(Color.FromArgb(40, 251, 191, 36));
    private readonly SolidColorBrush _sunBrush = new(Color.FromArgb(180, 253, 224, 71));
    private readonly SolidColorBrush _depthShadowBrush = new(Color.FromArgb(85, 2, 6, 23));
    private readonly SolidColorBrush _gridBrush = new(Color.FromArgb(28, 255, 255, 255));
    private readonly SolidColorBrush _ballastBrush = new(Color.FromRgb(51, 65, 85));    // Slate 700
    private readonly SolidColorBrush _sleeperBrush = new(Color.FromRgb(120, 85, 50));   // Wooden brown
    private readonly SolidColorBrush _railBrush = new(Color.FromRgb(226, 232, 240));     // Steel shiny
    private readonly SolidColorBrush _stationPlatBrush = new(Color.FromRgb(217, 119, 6)); // Amber wood/concrete
    private readonly SolidColorBrush _stationRoofBrush = new(Color.FromRgb(180, 83, 9));  // Dark Amber
    private readonly SolidColorBrush _headlightGlow = new(Color.FromArgb(70, 253, 224, 71)); // Yellow light cone

    private readonly SolidColorBrush _signalRedBrush = new(Color.FromRgb(239, 68, 68));
    private readonly SolidColorBrush _signalGreenBrush = new(Color.FromRgb(34, 197, 94));
    private readonly SolidColorBrush _signalHousingBrush = new(Color.FromRgb(15, 23, 42));

    private readonly SolidColorBrush _rockBrush = new(Color.FromRgb(100, 116, 139));
    private readonly SolidColorBrush _rockHighlight = new(Color.FromRgb(148, 163, 184));
    private readonly SolidColorBrush _treeTrunkBrush = new(Color.FromRgb(120, 75, 45));
    private readonly SolidColorBrush _treeFoliageBrush = new(Color.FromRgb(22, 101, 52));
    private readonly SolidColorBrush _treeFoliageLight = new(Color.FromRgb(34, 197, 94));

    private readonly Pen _gridPen;
    private readonly Pen _boardGridShadowPen;
    private readonly Pen _boardEdgePen;
    private readonly Pen _ballastPen;
    private readonly Pen _railPen;
    private readonly Pen _sleeperPen;
    private readonly Pen _selectionPen;

    public GameRenderer()
    {
        _worldBrush = new LinearGradientBrush
        {
            StartPoint = new Point(0, 0),
            EndPoint = new Point(0, 1)
        };
        _worldBrush.GradientStops.Add(new GradientStop(Color.FromRgb(15, 23, 42), 0));
        _worldBrush.GradientStops.Add(new GradientStop(Color.FromRgb(30, 58, 75), 0.52));
        _worldBrush.GradientStops.Add(new GradientStop(Color.FromRgb(21, 42, 48), 1));

        _groundBrush = new LinearGradientBrush
        {
            StartPoint = new Point(0, 0),
            EndPoint = new Point(0, 1)
        };
        _groundBrush.GradientStops.Add(new GradientStop(Color.FromArgb(190, 38, 73, 69), 0));
        _groundBrush.GradientStops.Add(new GradientStop(Color.FromArgb(230, 22, 48, 51), 1));

        _boardTopBrush = new LinearGradientBrush
        {
            StartPoint = new Point(0, 0),
            EndPoint = new Point(0.8, 1)
        };
        _boardTopBrush.GradientStops.Add(new GradientStop(Color.FromRgb(27, 57, 61), 0));
        _boardTopBrush.GradientStops.Add(new GradientStop(Color.FromRgb(20, 43, 48), 1));

        _boardSideBrush = new LinearGradientBrush
        {
            StartPoint = new Point(0, 0),
            EndPoint = new Point(1, 1)
        };
        _boardSideBrush.GradientStops.Add(new GradientStop(Color.FromRgb(13, 29, 35), 0));
        _boardSideBrush.GradientStops.Add(new GradientStop(Color.FromRgb(7, 16, 25), 1));

        _worldBrush.Freeze();
        _groundBrush.Freeze();
        _boardTopBrush.Freeze();
        _boardSideBrush.Freeze();
        _farHillBrush.Freeze();
        _nearHillBrush.Freeze();
        _terrainDetailBrush.Freeze();
        _sunGlowBrush.Freeze();
        _sunBrush.Freeze();
        _depthShadowBrush.Freeze();
        _gridBrush.Freeze();
        _ballastBrush.Freeze();
        _sleeperBrush.Freeze();
        _railBrush.Freeze();
        _stationPlatBrush.Freeze();
        _stationRoofBrush.Freeze();
        _headlightGlow.Freeze();
        _signalRedBrush.Freeze();
        _signalGreenBrush.Freeze();
        _signalHousingBrush.Freeze();

        _rockBrush.Freeze();
        _rockHighlight.Freeze();
        _treeTrunkBrush.Freeze();
        _treeFoliageBrush.Freeze();
        _treeFoliageLight.Freeze();

        _gridPen = new Pen(_gridBrush, 1);
        _gridPen.Freeze();

        _boardGridShadowPen = new Pen(new SolidColorBrush(Color.FromArgb(130, 1, 8, 15)), 2.5);
        _boardGridShadowPen.Freeze();
        _boardEdgePen = new Pen(new SolidColorBrush(Color.FromArgb(160, 111, 190, 184)), 1.5);
        _boardEdgePen.Freeze();

        _ballastPen = new Pen(_ballastBrush, 14);
        _ballastPen.StartLineCap = PenLineCap.Round;
        _ballastPen.EndLineCap = PenLineCap.Round;
        _ballastPen.Freeze();

        _railPen = new Pen(_railBrush, 2.5);
        _railPen.Freeze();

        _sleeperPen = new Pen(_sleeperBrush, 3.5);
        _sleeperPen.Freeze();

        var selBrush = new SolidColorBrush(Color.FromArgb(220, 245, 158, 11));
        selBrush.Freeze();
        _selectionPen = new Pen(selBrush, 2.5);
        _selectionPen.DashStyle = DashStyles.Dash;
        _selectionPen.Freeze();
    }

    public void Render(DrawingContext dc, TrainSimulation sim, double cellSize, Point? hoverCell, TrackType? activeToolTrack, Train? selectedTrain, ParticleSystem particles, PuzzleManager? puzzle = null)
    {
        double width = sim.Grid.Columns * cellSize;
        double height = sim.Grid.Rows * cellSize;

        // 1. Fondo y profundidad ambiental
        dc.DrawRectangle(_worldBrush, null, new Rect(0, 0, width, height));
        RenderEnvironment(dc, width, height, cellSize);
        RenderBoardSurface(dc, width, height, cellSize);

        // 2. Grilla
        double gridDepth = cellSize * 0.08;
        for (int x = 0; x <= sim.Grid.Columns; x++)
        {
            dc.DrawLine(_boardGridShadowPen,
                new Point(x * cellSize + gridDepth, gridDepth),
                new Point(x * cellSize + gridDepth, height + gridDepth));
            dc.DrawLine(_gridPen, new Point(x * cellSize, 0), new Point(x * cellSize, height));
        }
        for (int y = 0; y <= sim.Grid.Rows; y++)
        {
            dc.DrawLine(_boardGridShadowPen,
                new Point(gridDepth, y * cellSize + gridDepth),
                new Point(width + gridDepth, y * cellSize + gridDepth));
            dc.DrawLine(_gridPen, new Point(0, y * cellSize), new Point(width, y * cellSize));
        }

        // 3. Obstáculos del Puzzle (si estamos en modo puzzle)
        if (puzzle != null)
        {
            foreach (var obs in puzzle.CurrentLevel.Obstacles)
            {
                RenderObstacle(dc, obs, cellSize);
            }

            // Marcadores de Salida (Depots)
            foreach (var sp in puzzle.CurrentLevel.TrainSpawns)
            {
                RenderSpawnMarker(dc, sp, cellSize);
            }
        }

        // 4. Vías instaladas
        foreach (var track in sim.Grid.GetAllTracks())
        {
            RenderTrack(dc, track.X, track.Y, track.EffectiveType, cellSize, 1.0);

            if (track.Station != null)
            {
                // Comprobar si coincide con una estación de puzzle para resaltarla con su color
                TargetStationInfo? puzzleStation = puzzle?.CurrentLevel.TargetStations.Find(st => st.X == track.X && st.Y == track.Y);
                RenderStationOverlay(dc, track.Station, cellSize, puzzleStation);
            }

            if (track.Signal != null)
            {
                RenderSignal(dc, track.Signal, cellSize);
            }
        }

        // 5. Previsualización Ghost
        if (hoverCell.HasValue && activeToolTrack.HasValue)
        {
            int hx = (int)hoverCell.Value.X;
            int hy = (int)hoverCell.Value.Y;
            if (sim.Grid.IsInBounds(hx, hy) && !sim.Grid.HasTrack(hx, hy))
            {
                // Evitar previsualizar sobre obstáculos
                bool isObstacle = puzzle?.CurrentLevel.Obstacles.Exists(o => o.X == hx && o.Y == hy) ?? false;
                if (!isObstacle)
                {
                    RenderTrack(dc, hx, hy, activeToolTrack.Value, cellSize, 0.45);
                }
            }
        }

        // 6. Partículas de humo/vapor
        particles.Render(dc, cellSize);

        // 7. Trenes y Vagones
        foreach (var train in sim.Trains)
        {
            RenderTrain(dc, train, cellSize, train == selectedTrain);
        }
    }

    private void RenderObstacle(DrawingContext dc, ObstacleInfo obs, double s)
    {
        double px = (obs.X + 0.5) * s;
        double py = (obs.Y + 0.5) * s;

        dc.DrawEllipse(_depthShadowBrush, null, new Point(px + s * 0.08, py + s * 0.24), s * 0.38, s * 0.13);

        if (obs.Type == "Tree")
        {
            // Tronco
            dc.DrawRectangle(_treeTrunkBrush, null, new Rect(px - s * 0.08, py + s * 0.1, s * 0.16, s * 0.3));
            // Copa de árbol (dos capas circulares)
            dc.DrawEllipse(_treeFoliageBrush, null, new Point(px, py - s * 0.05), s * 0.36, s * 0.36);
            dc.DrawEllipse(_treeFoliageLight, null, new Point(px, py - s * 0.15), s * 0.24, s * 0.24);
        }
        else
        {
            // Roca montañosa
            dc.DrawEllipse(_rockBrush, new Pen(_rockHighlight, 1.5), new Point(px, py), s * 0.38, s * 0.34);
            dc.DrawEllipse(_rockHighlight, null, new Point(px - s * 0.1, py - s * 0.1), s * 0.14, s * 0.12);
        }
    }

    private void RenderSpawnMarker(DrawingContext dc, TrainSpawn sp, double s)
    {
        double x = sp.X * s;
        double y = sp.Y * s;

        Color col = ParseColor(sp.ColorHex);
        var brush = new SolidColorBrush(Color.FromArgb(60, col.R, col.G, col.B));
        var pen = new Pen(new SolidColorBrush(col), 2);
        pen.DashStyle = DashStyles.Dash;

        dc.DrawRoundedRectangle(brush, pen, new Rect(x + 2, y + 2, s - 4, s - 4), 6, 6);

        // Flecha de dirección de salida
        var (dx, dy) = sp.Direction.ToOffset();
        double cx = x + s * 0.5;
        double cy = y + s * 0.5;
        dc.DrawLine(new Pen(new SolidColorBrush(col), 3), new Point(cx, cy), new Point(cx + dx * s * 0.35, cy + dy * s * 0.35));
    }

    private void RenderTrack(DrawingContext dc, int cx, int cy, TrackType type, double s, double opacity)
    {
        double x = cx * s;
        double y = cy * s;
        double midX = x + s * 0.5;
        double midY = y + s * 0.5;
        double gauge = s * 0.22;

        Brush ballast = opacity < 1.0 ? new SolidColorBrush(Color.FromArgb((byte)(opacity * 150), 70, 85, 105)) : _ballastBrush;
        Pen railPen = opacity < 1.0 ? new Pen(new SolidColorBrush(Color.FromArgb((byte)(opacity * 200), 200, 220, 240)), 2) : _railPen;
        Pen sleeperPen = opacity < 1.0 ? new Pen(new SolidColorBrush(Color.FromArgb((byte)(opacity * 150), 140, 100, 60)), 3) : _sleeperPen;

        switch (type)
        {
            case TrackType.Horizontal:
            case TrackType.StationHorizontal:
                dc.DrawRoundedRectangle(_depthShadowBrush, null, new Rect(x + s * 0.04, y + s * 0.36, s, s * 0.32), s * 0.12, s * 0.12);
                RenderStraight(dc, x, y, s, isHorizontal: true, isStation: type == TrackType.StationHorizontal, ballast, railPen, sleeperPen, gauge);
                break;

            case TrackType.Vertical:
            case TrackType.StationVertical:
                dc.DrawRoundedRectangle(_depthShadowBrush, null, new Rect(x + s * 0.36, y + s * 0.04, s * 0.32, s), s * 0.12, s * 0.12);
                RenderStraight(dc, x, y, s, isHorizontal: false, isStation: type == TrackType.StationVertical, ballast, railPen, sleeperPen, gauge);
                break;

            case TrackType.Cross:
                RenderStraight(dc, x, y, s, isHorizontal: true, false, ballast, railPen, sleeperPen, gauge);
                RenderStraight(dc, x, y, s, isHorizontal: false, false, ballast, railPen, sleeperPen, gauge);
                dc.DrawRectangle(new SolidColorBrush(Color.FromRgb(71, 85, 105)), null, new Rect(midX - gauge * 0.7, midY - gauge * 0.7, gauge * 1.4, gauge * 1.4));
                break;

            case TrackType.CurveNorthEast:
                RenderArc(dc, x + s, y, s * 0.5, 90, 180, ballast, railPen, sleeperPen, gauge);
                break;

            case TrackType.CurveEastSouth:
                RenderArc(dc, x + s, y + s, s * 0.5, 180, 270, ballast, railPen, sleeperPen, gauge);
                break;

            case TrackType.CurveSouthWest:
                RenderArc(dc, x, y + s, s * 0.5, 270, 360, ballast, railPen, sleeperPen, gauge);
                break;

            case TrackType.CurveWestNorth:
                RenderArc(dc, x, y, s * 0.5, 0, 90, ballast, railPen, sleeperPen, gauge);
                break;
        }
    }

    private void RenderStraight(DrawingContext dc, double x, double y, double s, bool isHorizontal, bool isStation, Brush ballast, Pen railPen, Pen sleeperPen, double gauge)
    {
        double midX = x + s * 0.5;
        double midY = y + s * 0.5;

        if (isHorizontal)
        {
            dc.DrawRectangle(ballast, null, new Rect(x, midY - s * 0.28, s, s * 0.56));
            int count = 6;
            for (int i = 0; i < count; i++)
            {
                double sx = x + (i + 0.5) * (s / count);
                dc.DrawLine(sleeperPen, new Point(sx, midY - s * 0.24), new Point(sx, midY + s * 0.24));
            }
            dc.DrawLine(railPen, new Point(x, midY - gauge), new Point(x + s, midY - gauge));
            dc.DrawLine(railPen, new Point(x, midY + gauge), new Point(x + s, midY + gauge));
        }
        else
        {
            dc.DrawRectangle(ballast, null, new Rect(midX - s * 0.28, y, s * 0.56, s));
            int count = 6;
            for (int i = 0; i < count; i++)
            {
                double sy = y + (i + 0.5) * (s / count);
                dc.DrawLine(sleeperPen, new Point(midX - s * 0.24, sy), new Point(midX + s * 0.24, sy));
            }
            dc.DrawLine(railPen, new Point(midX - gauge, y), new Point(midX - gauge, y + s));
            dc.DrawLine(railPen, new Point(midX + gauge, y), new Point(midX + gauge, y + s));
        }

        if (isStation)
        {
            if (isHorizontal)
            {
                dc.DrawRoundedRectangle(_stationPlatBrush, null, new Rect(x + 2, y + 2, s - 4, s * 0.18), 3, 3);
                dc.DrawRoundedRectangle(_stationRoofBrush, null, new Rect(x + 6, y + 4, s - 12, s * 0.12), 2, 2);
            }
            else
            {
                dc.DrawRoundedRectangle(_stationPlatBrush, null, new Rect(x + 2, y + 2, s * 0.18, s - 4), 3, 3);
                dc.DrawRoundedRectangle(_stationRoofBrush, null, new Rect(x + 4, y + 6, s * 0.12, s - 12), 2, 2);
            }
        }
    }

    private void RenderArc(DrawingContext dc, double cx, double cy, double r, double startAngle, double endAngle, Brush ballast, Pen railPen, Pen sleeperPen, double gauge)
    {
        int segments = 10;
        double angleStep = (endAngle - startAngle) / segments;
        double ballastWidth = gauge * 1.4;
        dc.DrawGeometry(_depthShadowBrush, null,
            CreateArcBand(cx + gauge * 0.45, cy + gauge * 0.7, r, startAngle, endAngle, ballastWidth, segments));
        dc.DrawGeometry(ballast, null,
            CreateArcBand(cx, cy, r, startAngle, endAngle, ballastWidth, segments));

        for (int i = 0; i <= segments; i++)
        {
            double a = (startAngle + i * angleStep) * (Math.PI / 180.0);
            double rInner = r - gauge * 1.4;
            double rOuter = r + gauge * 1.4;
            Point p1 = new(cx + rInner * Math.Cos(a), cy + rInner * Math.Sin(a));
            Point p2 = new(cx + rOuter * Math.Cos(a), cy + rOuter * Math.Sin(a));
            dc.DrawLine(sleeperPen, p1, p2);
        }

        double r1 = r - gauge;
        double r2 = r + gauge;

        for (int i = 0; i < segments; i++)
        {
            double a1 = (startAngle + i * angleStep) * (Math.PI / 180.0);
            double a2 = (startAngle + (i + 1) * angleStep) * (Math.PI / 180.0);

            Point inP1 = new(cx + r1 * Math.Cos(a1), cy + r1 * Math.Sin(a1));
            Point inP2 = new(cx + r1 * Math.Cos(a2), cy + r1 * Math.Sin(a2));
            dc.DrawLine(railPen, inP1, inP2);

            Point outP1 = new(cx + r2 * Math.Cos(a1), cy + r2 * Math.Sin(a1));
            Point outP2 = new(cx + r2 * Math.Cos(a2), cy + r2 * Math.Sin(a2));
            dc.DrawLine(railPen, outP1, outP2);
        }
    }

    private static StreamGeometry CreateArcBand(double cx, double cy, double radius, double startAngle, double endAngle, double halfWidth, int segments)
    {
        var geometry = new StreamGeometry();
        double step = (endAngle - startAngle) / segments;

        using (var context = geometry.Open())
        {
            Point first = PointOnCircle(cx, cy, radius - halfWidth, startAngle);
            context.BeginFigure(first, true, true);

            for (int i = 1; i <= segments; i++)
            {
                context.LineTo(PointOnCircle(cx, cy, radius - halfWidth, startAngle + i * step), true, false);
            }

            for (int i = segments; i >= 0; i--)
            {
                context.LineTo(PointOnCircle(cx, cy, radius + halfWidth, startAngle + i * step), true, false);
            }
        }

        geometry.Freeze();
        return geometry;
    }

    private static Point PointOnCircle(double cx, double cy, double radius, double angleDegrees)
    {
        double angle = angleDegrees * (Math.PI / 180.0);
        return new Point(cx + radius * Math.Cos(angle), cy + radius * Math.Sin(angle));
    }

    private void RenderStationOverlay(DrawingContext dc, Station st, double s, TargetStationInfo? targetInfo = null)
    {
        double x = st.X * s;
        double y = st.Y * s;

        Color targetCol = targetInfo != null ? ParseColor(targetInfo.ColorHex) : Color.FromRgb(245, 158, 11);
        string statusSymbol = targetInfo != null ? (targetInfo.HasReached ? "✅" : "🎯") : "👥";
        string label = targetInfo != null ? $"{statusSymbol} {targetInfo.Name}" : $"🚉 {st.Name} (👥 {st.WaitingPassengers})";

        var text = new FormattedText(label, CultureInfo.InvariantCulture, FlowDirection.LeftToRight,
            new Typeface("Segoe UI Semibold"), Math.Max(10, s * 0.22), Brushes.White, 1.25);

        double badgeW = text.Width + 12;
        double badgeH = text.Height + 4;
        double bx = x + s * 0.5 - badgeW * 0.5;
        double by = y - badgeH - 2;

        dc.DrawRoundedRectangle(new SolidColorBrush(Color.FromArgb(220, 15, 23, 42)), new Pen(new SolidColorBrush(targetCol), 2),
            new Rect(bx, by, badgeW, badgeH), 4, 4);

        dc.DrawText(text, new Point(bx + 6, by + 2));
    }

    private void RenderSignal(DrawingContext dc, RailwaySignal sig, double s)
    {
        double px = (sig.X + 0.82) * s;
        double py = (sig.Y + 0.18) * s;
        double radius = s * 0.16;

        dc.DrawLine(new Pen(new SolidColorBrush(Color.FromRgb(71, 85, 105)), 2), new Point(px, py), new Point(px, py + s * 0.35));

        dc.DrawRoundedRectangle(_signalHousingBrush, new Pen(new SolidColorBrush(Color.FromRgb(100, 116, 139)), 1),
            new Rect(px - radius * 1.2, py - radius * 1.2, radius * 2.4, radius * 2.4), 3, 3);

        Brush lightBrush = sig.State == SignalState.Red ? _signalRedBrush : _signalGreenBrush;
        dc.DrawEllipse(lightBrush, null, new Point(px, py), radius * 0.75, radius * 0.75);

        Color glowColor = sig.State == SignalState.Red ? Color.FromArgb(70, 239, 68, 68) : Color.FromArgb(70, 34, 197, 94);
        dc.DrawEllipse(new SolidColorBrush(glowColor), null, new Point(px, py), radius * 1.6, radius * 1.6);
    }

    private void RenderTrain(DrawingContext dc, Train train, double s, bool isSelected)
    {
        for (int i = train.Carriages.Count - 1; i >= 0; i--)
        {
            var carriage = train.Carriages[i];
            RenderCarriage(dc, carriage, s);
        }

        double px = train.WorldX * s;
        double py = train.WorldY * s;

        dc.DrawEllipse(_depthShadowBrush, null, new Point(px + s * 0.1, py + s * 0.18), s * 0.42, s * 0.14);

        dc.PushTransform(new RotateTransform(train.AngleDegrees, px, py));

        if (!train.IsDerailed && train.Speed > 0.1)
        {
            var lightGeom = new StreamGeometry();
            using (var ctx = lightGeom.Open())
            {
                ctx.BeginFigure(new Point(px + s * 0.4, py), true, true);
                ctx.LineTo(new Point(px + s * 1.8, py - s * 0.6), true, false);
                ctx.LineTo(new Point(px + s * 1.8, py + s * 0.6), true, false);
            }
            lightGeom.Freeze();
            dc.DrawGeometry(_headlightGlow, null, lightGeom);
        }

        double trainLen = s * 0.72;
        double trainW = s * 0.44;
        Rect bodyRect = new(px - trainLen * 0.5, py - trainW * 0.5, trainLen, trainW);

        Color bodyColor = ParseColor(train.ColorHex);
        var bodyBrush = new SolidColorBrush(bodyColor);
        bodyBrush.Freeze();
        var sideBrush = new SolidColorBrush(Color.FromRgb(
            (byte)(bodyColor.R * 0.62),
            (byte)(bodyColor.G * 0.62),
            (byte)(bodyColor.B * 0.62)));
        sideBrush.Freeze();

        dc.DrawRoundedRectangle(new SolidColorBrush(Color.FromArgb(80, 0, 0, 0)), null, new Rect(bodyRect.X + 2, bodyRect.Y + 2, bodyRect.Width, bodyRect.Height), 4, 4);
        dc.DrawRoundedRectangle(sideBrush, null, new Rect(bodyRect.X, bodyRect.Y + trainW * 0.16, bodyRect.Width, bodyRect.Height), 4, 4);
        dc.DrawRoundedRectangle(bodyBrush, new Pen(new SolidColorBrush(Color.FromRgb(30, 41, 59)), 1.5), bodyRect, 4, 4);
        dc.DrawLine(new Pen(new SolidColorBrush(Color.FromArgb(135, 255, 255, 255)), 1),
            new Point(bodyRect.X + 4, bodyRect.Y + 3), new Point(bodyRect.Right - 4, bodyRect.Y + 3));

        dc.DrawRoundedRectangle(new SolidColorBrush(Color.FromRgb(15, 23, 42)), null, new Rect(px - trainLen * 0.45, py - trainW * 0.4, trainLen * 0.35, trainW * 0.8), 2, 2);
        dc.DrawRectangle(new SolidColorBrush(Color.FromRgb(254, 240, 138)), null, new Rect(px - trainLen * 0.4, py - trainW * 0.3, trainLen * 0.25, trainW * 0.6));

        dc.DrawEllipse(new SolidColorBrush(Color.FromRgb(30, 41, 59)), null, new Point(px + trainLen * 0.25, py), trainW * 0.22, trainW * 0.22);
        dc.DrawEllipse(new SolidColorBrush(Color.FromRgb(71, 85, 105)), null, new Point(px + trainLen * 0.25, py), trainW * 0.12, trainW * 0.12);

        dc.DrawEllipse(new SolidColorBrush(Color.FromRgb(254, 240, 138)), null, new Point(px + trainLen * 0.46, py), trainW * 0.14, trainW * 0.14);

        dc.Pop();

        if (isSelected)
        {
            dc.DrawEllipse(null, _selectionPen, new Point(px, py), s * 0.65, s * 0.65);
        }

        if (train.IsDerailed)
        {
            var ft = new FormattedText("💥 DESCARRILADO", CultureInfo.InvariantCulture, FlowDirection.LeftToRight,
                new Typeface("Segoe UI Black"), 12, Brushes.Red, 1.25);
            dc.DrawText(ft, new Point(px - ft.Width * 0.5, py - s * 0.7));
        }
        else if (train.IsWaitingForSignal)
        {
            var ft = new FormattedText("🔴 Semáforo Rojo", CultureInfo.InvariantCulture, FlowDirection.LeftToRight,
                new Typeface("Segoe UI Bold"), 11, Brushes.OrangeRed, 1.25);
            dc.DrawText(ft, new Point(px - ft.Width * 0.5, py - s * 0.7));
        }
        else if (train.IsBrakingForCollision)
        {
            var ft = new FormattedText("🛑 Anti-Choque", CultureInfo.InvariantCulture, FlowDirection.LeftToRight,
                new Typeface("Segoe UI Bold"), 11, Brushes.Yellow, 1.25);
            dc.DrawText(ft, new Point(px - ft.Width * 0.5, py - s * 0.7));
        }
    }

    private void RenderCarriage(DrawingContext dc, TrainCarriage c, double s)
    {
        double px = c.WorldX * s;
        double py = c.WorldY * s;

        dc.PushTransform(new RotateTransform(c.AngleDegrees, px, py));

        double wagonLen = s * 0.64;
        double wagonW = s * 0.40;
        Rect wRect = new(px - wagonLen * 0.5, py - wagonW * 0.5, wagonLen, wagonW);

        Color cColor = ParseColor(c.ColorHex);
        var cBrush = new SolidColorBrush(cColor);
        cBrush.Freeze();

        dc.DrawRoundedRectangle(cBrush, new Pen(new SolidColorBrush(Color.FromRgb(30, 41, 59)), 1.5), wRect, 3, 3);

        if (c.Type == CarriageType.Passenger)
        {
            int windows = 3;
            for (int w = 0; w < windows; w++)
            {
                double wx = wRect.X + (w + 1) * (wagonLen / (windows + 1)) - wagonLen * 0.08;
                Brush winBrush = c.PassengersOnBoard > 0 ? new SolidColorBrush(Color.FromRgb(254, 240, 138)) : new SolidColorBrush(Color.FromRgb(148, 163, 184));
                dc.DrawRectangle(winBrush, null, new Rect(wx, py - wagonW * 0.28, wagonLen * 0.16, wagonW * 0.56));
            }
        }
        else if (c.Type == CarriageType.Freight)
        {
            dc.DrawLine(new Pen(new SolidColorBrush(Color.FromArgb(100, 0, 0, 0)), 2), new Point(wRect.X + 4, py), new Point(wRect.Right - 4, py));
        }
        else if (c.Type == CarriageType.Caboose)
        {
            dc.DrawRoundedRectangle(new SolidColorBrush(Color.FromRgb(185, 28, 28)), null, new Rect(px - wagonLen * 0.2, py - wagonW * 0.3, wagonLen * 0.4, wagonW * 0.6), 2, 2);
            dc.DrawEllipse(new SolidColorBrush(Color.FromRgb(239, 68, 68)), null, new Point(wRect.Left + 2, py), 3, 3);
        }

        dc.Pop();
    }

    private static Color ParseColor(string hex)
    {
        try
        {
            return (Color)ColorConverter.ConvertFromString(hex);
        }
        catch
        {
            return Colors.Crimson;
        }
    }

    private void RenderEnvironment(DrawingContext dc, double width, double height, double cellSize)
    {
        double horizon = Math.Max(height * 0.34, cellSize * 3.0);
        dc.DrawRectangle(_groundBrush, null, new Rect(0, horizon, width, height - horizon));

        double sunX = width * 0.82;
        double sunY = horizon * 0.38;
        dc.DrawEllipse(_sunGlowBrush, null, new Point(sunX, sunY), cellSize * 1.35, cellSize * 1.35);
        dc.DrawEllipse(_sunBrush, null, new Point(sunX, sunY), cellSize * 0.48, cellSize * 0.48);

        double hillSpacing = Math.Max(cellSize * 4.5, width / 6.0);
        for (int i = -1; i < 8; i++)
        {
            double x = i * hillSpacing + hillSpacing * 0.5;
            dc.DrawEllipse(_farHillBrush, null, new Point(x, horizon + cellSize * 0.03), hillSpacing * 0.72, cellSize * 1.35);
            dc.DrawEllipse(_nearHillBrush, null, new Point(x + hillSpacing * 0.35, horizon + cellSize * 0.2), hillSpacing * 0.58, cellSize * 0.92);
        }

        for (int i = 0; i < 28; i++)
        {
            double x = ((i * 83) % 97) / 97.0 * width;
            double y = horizon + cellSize * (0.8 + ((i * 37) % 19) / 10.0);
            double size = cellSize * (0.025 + ((i * 13) % 5) * 0.008);
            dc.DrawEllipse(_terrainDetailBrush, null, new Point(x, y), size * 1.8, size);
        }
    }

    private void RenderBoardSurface(DrawingContext dc, double width, double height, double cellSize)
    {
        double depth = cellSize * 0.22;
        var board = new Rect(0, 0, width, height);
        var side = new Rect(depth, depth, width, height);
        var shadow = new Rect(depth * 1.7, depth * 1.9, width, height);
        double radius = cellSize * 0.16;

        dc.DrawRoundedRectangle(_depthShadowBrush, null, shadow, radius, radius);
        dc.DrawRoundedRectangle(_boardSideBrush, null, side, radius, radius);
        dc.DrawRoundedRectangle(_boardTopBrush, _boardEdgePen, board, radius, radius);

        // Resalta el canto superior para separar la superficie jugable del fondo.
        dc.DrawLine(_boardEdgePen, new Point(radius, 0.75), new Point(width - radius, 0.75));
        dc.DrawLine(_boardEdgePen, new Point(0.75, radius), new Point(0.75, height - radius));
    }
}
