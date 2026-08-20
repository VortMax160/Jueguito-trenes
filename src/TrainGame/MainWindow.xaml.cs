using System;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using TrainGame.Core;
using TrainGame.Core.Puzzle;
using TrainGame.UI;

namespace TrainGame;

public partial class MainWindow : Window
{
    private PuzzleManager? Puzzle => CanvasGame.PuzzleManager;

    public MainWindow()
    {
        InitializeComponent();

        Loaded += MainWindow_Loaded;
        CanvasGame.OnMapStatsChanged += UpdatePuzzleUI;
        CanvasGame.Simulation.Grid.OnGridChanged += UpdatePuzzleUI;
    }

    private void MainWindow_Loaded(object sender, RoutedEventArgs e)
    {
        if (Puzzle != null)
        {
            Puzzle.OnPuzzleStateChanged += OnPuzzleStateChanged;
            Puzzle.LoadLevel(0);
            CanvasGame.ResetView();
            UpdateLevelDetails();
            RebuildLevelButtons();
        }

        Activate();
        Topmost = true;
        Topmost = false;
        Focus();
    }

    private void OnPuzzleStateChanged()
    {
        if (Puzzle == null) return;

        TxtPuzzleStatusMsg.Text = Puzzle.StatusMessage;
        UpdateLevelDetails();

        if (Puzzle.State == PuzzleState.Victory)
        {
            CardResult.Visibility = Visibility.Visible;
            CardResult.Background = new SolidColorBrush(Color.FromArgb(240, 22, 101, 52)); // Verde victoria
            TxtResultHeader.Text = "🎉 ¡NIVEL COMPLETADO!";
            TxtResultHeader.Foreground = new SolidColorBrush(Color.FromRgb(74, 222, 128));

            int stars = Puzzle.Scores[Puzzle.CurrentLevel.Id].StarsEarned;
            TxtResultStars.Text = new string('⭐', stars);
            TxtResultMsg.Text = $"¡Excelente trabajo! Has usado {Puzzle.PlayerTracksPlacedCount} vías.";
            BtnNextLevelCard.Visibility = (Puzzle.CurrentLevelIndex + 1 < Puzzle.Levels.Count) ? Visibility.Visible : Visibility.Collapsed;

            BtnRunPuzzle.Content = "🎉 ¡COMPLETADO!";
            BtnRunPuzzle.Background = new SolidColorBrush(Color.FromRgb(34, 197, 94));
        }
        else if (Puzzle.State == PuzzleState.Defeat)
        {
            CardResult.Visibility = Visibility.Visible;
            CardResult.Background = new SolidColorBrush(Color.FromArgb(240, 153, 27, 27)); // Rojo derrota
            TxtResultHeader.Text = "💥 ¡DESCARRIÓ O CHOCÓ!";
            TxtResultHeader.Foreground = new SolidColorBrush(Color.FromRgb(248, 113, 113));
            TxtResultStars.Text = "❌";
            TxtResultMsg.Text = "El tren no pudo llegar a la estación de destino. Revisa las vías.";
            BtnNextLevelCard.Visibility = Visibility.Collapsed;

            BtnRunPuzzle.Content = "🔄 REINTENTAR";
            BtnRunPuzzle.Background = new SolidColorBrush(Color.FromRgb(220, 38, 38));
        }
        else if (Puzzle.State == PuzzleState.Running)
        {
            CardResult.Visibility = Visibility.Collapsed;
            BtnRunPuzzle.Content = "⏸️ EN MARCHA...";
            BtnRunPuzzle.Background = new SolidColorBrush(Color.FromRgb(37, 99, 235));
        }
        else // Planning
        {
            CardResult.Visibility = Visibility.Collapsed;
            BtnRunPuzzle.Content = "▶️ ¡ARRANCAR TRENES!";
            BtnRunPuzzle.Background = new SolidColorBrush(Color.FromRgb(22, 163, 74));
        }

        UpdateSimulationControls();
        RebuildLevelButtons();
    }

    private void UpdateLevelDetails()
    {
        if (Puzzle == null) return;

        var lvl = Puzzle.CurrentLevel;
        TxtLevelTitle.Text = lvl.Title;
        TxtLevelDesc.Text = lvl.Description;
        TxtLevelHint.Text = $"💡 Pista: {lvl.Hint}";

        int stars = Puzzle.Scores[lvl.Id].StarsEarned;
        TxtLevelStars.Text = stars > 0 ? $" {new string('⭐', stars)}" : " (Sin completar)";

        TxtStar3Goal.Text = $"⭐⭐⭐ : {lvl.Star3Budget} vías o menos";
        TxtStar2Goal.Text = $"⭐⭐ : {lvl.Star2Budget} vías";
        TxtStar1Goal.Text = $"⭐ : {lvl.MaxTrackBudget} vías";

        // Presupuesto de vías
        int used = Puzzle.PlayerTracksPlacedCount;
        int max = lvl.MaxTrackBudget;

        TxtTracksUsed.Text = $"{used} / {max}";
        ProgressTrackBudget.Maximum = max;
        ProgressTrackBudget.Value = Math.Min(used, max);

        if (used > max)
        {
            TxtTracksUsed.Foreground = new SolidColorBrush(Color.FromRgb(239, 68, 68)); // Rojo si excede
            ProgressTrackBudget.Foreground = new SolidColorBrush(Color.FromRgb(239, 68, 68));
        }
        else
        {
            TxtTracksUsed.Foreground = new SolidColorBrush(Color.FromRgb(56, 189, 248)); // Azul cielo
            ProgressTrackBudget.Foreground = new SolidColorBrush(Color.FromRgb(37, 99, 235));
        }

        BtnPrevLevel.IsEnabled = Puzzle.CurrentLevelIndex > 0;
        BtnNextLevel.IsEnabled = Puzzle.CurrentLevelIndex + 1 < Puzzle.Levels.Count &&
                                 Puzzle.Scores[Puzzle.Levels[Puzzle.CurrentLevelIndex + 1].Id].IsUnlocked;
    }

    private void RebuildLevelButtons()
    {
        if (Puzzle == null || PanelLevelList == null) return;

        PanelLevelList.Children.Clear();

        for (int i = 0; i < Puzzle.Levels.Count; i++)
        {
            var lvl = Puzzle.Levels[i];
            var score = Puzzle.Scores[lvl.Id];

            var btn = new Button
            {
                Margin = new Thickness(0, 2, 0, 2),
                Padding = new Thickness(10, 6, 10, 6),
                HorizontalContentAlignment = HorizontalAlignment.Left,
                Cursor = System.Windows.Input.Cursors.Hand,
                IsEnabled = score.IsUnlocked,
                Tag = i
            };

            string starStr = score.StarsEarned > 0 ? new string('⭐', score.StarsEarned) : (score.IsUnlocked ? "🔓" : "🔒");
            btn.Content = $"Nivel {lvl.Id}: {starStr}";

            if (i == Puzzle.CurrentLevelIndex)
            {
                btn.Background = new SolidColorBrush(Color.FromRgb(37, 99, 235));
                btn.Foreground = Brushes.White;
                btn.FontWeight = FontWeights.Bold;
            }
            else
            {
                btn.Background = new SolidColorBrush(Color.FromRgb(30, 41, 59));
                btn.Foreground = score.IsUnlocked ? new SolidColorBrush(Color.FromRgb(226, 232, 240)) : new SolidColorBrush(Color.FromRgb(100, 116, 139));
            }

            btn.Click += (s, ev) =>
            {
                if (s is Button b && b.Tag is int idx)
                {
                    Puzzle.LoadLevel(idx);
                    CanvasGame.ResetView();
                }
            };

            PanelLevelList.Children.Add(btn);
        }
    }

    private void UpdatePuzzleUI()
    {
        if (Puzzle != null)
        {
            Puzzle.UpdateTrackCount();
            UpdateLevelDetails();
        }
    }

    #region Eventos de Botones

    private void BtnRunPuzzle_Click(object sender, RoutedEventArgs e)
    {
        if (Puzzle == null) return;

        if (Puzzle.State == PuzzleState.Running)
        {
            Puzzle.ResetToPlanning();
        }
        else
        {
            Puzzle.StartSimulation();
        }
    }

    private void BtnResetPuzzle_Click(object sender, RoutedEventArgs e)
    {
        Puzzle?.ResetToPlanning();
    }

    private void BtnPauseSimulation_Click(object sender, RoutedEventArgs e)
    {
        if (Puzzle?.State != PuzzleState.Running) return;

        CanvasGame.Simulation.IsPaused = !CanvasGame.Simulation.IsPaused;
        TxtPuzzleStatusMsg.Text = CanvasGame.Simulation.IsPaused
            ? "Simulación pausada. El trazado permanece intacto."
            : "¡Trenes en marcha!";
        UpdateSimulationControls();
    }

    private void BtnSpeed_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button button || button.Tag is not string tag ||
            !double.TryParse(tag, NumberStyles.Float, CultureInfo.InvariantCulture, out double scale))
        {
            return;
        }

        CanvasGame.Simulation.TimeScale = scale;
        TxtFooterStatus.Text = $"Velocidad de simulación: {scale.ToString("0.0", CultureInfo.InvariantCulture)}x";
    }

    private void UpdateSimulationControls()
    {
        if (Puzzle == null || BtnPauseSimulation == null) return;

        bool isRunning = Puzzle.State == PuzzleState.Running;
        BtnPauseSimulation.IsEnabled = isRunning;
        BtnPauseSimulation.Content = CanvasGame.Simulation.IsPaused ? "▶ Continuar" : "⏸ Pausar";
    }

    private void BtnPrevLevel_Click(object sender, RoutedEventArgs e)
    {
        Puzzle?.PreviousLevel();
        CanvasGame.ResetView();
    }

    private void BtnNextLevel_Click(object sender, RoutedEventArgs e)
    {
        Puzzle?.NextLevel();
        CanvasGame.ResetView();
    }

    private void BtnResetView_Click(object sender, RoutedEventArgs e)
    {
        CanvasGame.ResetView();
    }

    private void BtnClearMap_Click(object sender, RoutedEventArgs e)
    {
        if (Puzzle != null)
        {
            // Limpia las vías del jugador y restablece el nivel
            Puzzle.LoadLevel(Puzzle.CurrentLevelIndex);
        }
    }

    private void Tool_Checked(object sender, RoutedEventArgs e)
    {
        if (sender is RadioButton rb && rb.Tag is string tag && Enum.TryParse<ToolMode>(tag, out var mode))
        {
            CanvasGame.CurrentTool = mode;
            UpdateFooterHelp(mode);
        }
    }

    private void UpdateFooterHelp(ToolMode mode)
    {
        if (TxtFooterStatus == null) return;

        TxtFooterStatus.Text = mode switch
        {
            ToolMode.AutoTrack => "Modo: Auto-Vía | Arrastra sobre la grilla para conectar trenes con sus estaciones.",
            ToolMode.StraightH => "Modo: Recta Horizontal | Traza tramos horizontales.",
            ToolMode.StraightV => "Modo: Recta Vertical | Traza tramos verticales.",
            ToolMode.CurveNE => "Modo: Curva Norte-Este | Conecta Norte y Este.",
            ToolMode.CurveES => "Modo: Curva Este-Sur | Conecta Este y Sur.",
            ToolMode.CurveSW => "Modo: Curva Sur-Oeste | Conecta Sur y Oeste.",
            ToolMode.CurveWN => "Modo: Curva Oeste-Norte | Conecta Oeste y Norte.",
            ToolMode.Cross => "Modo: Cruce en Cruz | Permite cruzar dos vías perpendiculares.",
            ToolMode.DeleteTrack => "Modo: Borrar Vía | Haz clic para eliminar vías sobrantes.",
            ToolMode.Select => "Modo: Seleccionar | Haz clic para inspeccionar trenes.",
            _ => "Listo."
        };
    }

    #endregion
}
