using System;
using System.Collections.Generic;
using System.Windows.Media;

namespace TrainGame.Rendering;

public class SmokeParticle
{
    public double X { get; set; }
    public double Y { get; set; }
    public double Size { get; set; }
    public double Opacity { get; set; }
    public double Life { get; set; }
    public double MaxLife { get; set; }
    public double VelocityX { get; set; }
    public double VelocityY { get; set; }
}

public class ParticleSystem
{
    private readonly List<SmokeParticle> _particles = new();
    private readonly Random _random = new();

    public void EmitSmoke(double worldX, double worldY, double trainAngle)
    {
        double angleRad = (trainAngle + 180 + _random.NextDouble() * 30 - 15) * (Math.PI / 180.0);
        double speed = 0.3 + _random.NextDouble() * 0.4;

        _particles.Add(new SmokeParticle
        {
            X = worldX,
            Y = worldY,
            Size = 0.2 + _random.NextDouble() * 0.15,
            Opacity = 0.8,
            Life = 0.8 + _random.NextDouble() * 0.5,
            MaxLife = 1.2,
            VelocityX = Math.Cos(angleRad) * speed * 0.3 + (_random.NextDouble() - 0.5) * 0.1,
            VelocityY = Math.Sin(angleRad) * speed * 0.3 - 0.2 // Flota un poco hacia arriba
        });
    }

    public void Update(double dt)
    {
        for (int i = _particles.Count - 1; i >= 0; i--)
        {
            var p = _particles[i];
            p.Life -= dt;
            if (p.Life <= 0)
            {
                _particles.RemoveAt(i);
                continue;
            }

            p.X += p.VelocityX * dt;
            p.Y += p.VelocityY * dt;
            p.Size += 0.25 * dt; // Se expande con el tiempo
            p.Opacity = Math.Clamp(p.Life / p.MaxLife * 0.7, 0, 0.8);
        }
    }

    public void Render(DrawingContext dc, double cellSize)
    {
        foreach (var p in _particles)
        {
            var brush = new SolidColorBrush(Color.FromArgb((byte)(p.Opacity * 255), 230, 235, 240));
            brush.Freeze();
            dc.DrawEllipse(brush, null, new System.Windows.Point(p.X * cellSize, p.Y * cellSize), p.Size * cellSize * 0.5, p.Size * cellSize * 0.5);
        }
    }
}
