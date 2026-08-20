using System;

namespace TrainGame.Core;

public static class TrackGeometry
{
    /// <summary>
    /// Calcula la posición continua en el mundo (en unidades de celda) y el ángulo de rotación (en grados)
    /// de un objeto que transita por una celda con vía dada su dirección de entrada y progreso [0..1].
    /// </summary>
    public static (double x, double y, double angleDegrees) GetPositionAndAngle(
        int cellX,
        int cellY,
        TrackType type,
        Direction travelDirection,
        double progress)
    {
        progress = Math.Clamp(progress, 0.0, 1.0);
        Direction entryFrom = travelDirection.Opposite();

        switch (type)
        {
            case TrackType.Horizontal or TrackType.StationHorizontal:
            {
                if (travelDirection == Direction.East)
                {
                    return (cellX + progress, cellY + 0.5, 0);
                }
                else
                {
                    return (cellX + (1.0 - progress), cellY + 0.5, 180);
                }
            }

            case TrackType.Vertical or TrackType.StationVertical:
            {
                if (travelDirection == Direction.South)
                {
                    return (cellX + 0.5, cellY + progress, 90);
                }
                else
                {
                    return (cellX + 0.5, cellY + (1.0 - progress), 270);
                }
            }

            case TrackType.Cross:
            {
                if (travelDirection == Direction.East) return (cellX + progress, cellY + 0.5, 0);
                if (travelDirection == Direction.West) return (cellX + (1.0 - progress), cellY + 0.5, 180);
                if (travelDirection == Direction.South) return (cellX + 0.5, cellY + progress, 90);
                if (travelDirection == Direction.North) return (cellX + 0.5, cellY + (1.0 - progress), 270);
                return (cellX + 0.5, cellY + 0.5, 0);
            }

            case TrackType.CurveNorthEast:
            {
                // Arco con centro en esquina superior derecha: (cellX + 1, cellY + 0)
                // Radio = 0.5
                double cx = cellX + 1.0;
                double cy = cellY + 0.0;
                double r = 0.5;

                if (entryFrom == Direction.North)
                {
                    // Viajando hacia el Sur, gira hacia el Este
                    // Ángulo en círculo de PI (izq) a PI/2 (abajo) -> en mundo es en sentido horario
                    double t = progress * (Math.PI / 2.0);
                    double theta = Math.PI - t; // de PI a PI/2
                    double x = cx + r * Math.Cos(theta);
                    double y = cy + r * Math.Sin(theta);
                    double angle = 90.0 - (progress * 90.0); // 90° -> 0°
                    return (x, y, angle);
                }
                else
                {
                    // entryFrom == Direction.East -> Viajando hacia el Oeste, gira hacia el Norte
                    double t = progress * (Math.PI / 2.0);
                    double theta = (Math.PI / 2.0) + t; // de PI/2 a PI
                    double x = cx + r * Math.Cos(theta);
                    double y = cy + r * Math.Sin(theta);
                    double angle = 180.0 + (progress * 90.0); // 180° -> 270°
                    return (x, y, angle);
                }
            }

            case TrackType.CurveEastSouth:
            {
                // Arco con centro en esquina inferior derecha: (cellX + 1, cellY + 1)
                double cx = cellX + 1.0;
                double cy = cellY + 1.0;
                double r = 0.5;

                if (entryFrom == Direction.East)
                {
                    // Viajando hacia el Oeste, gira hacia el Sur
                    double t = progress * (Math.PI / 2.0);
                    double theta = (3.0 * Math.PI / 2.0) - t; // 270° a 180°
                    double x = cx + r * Math.Cos(theta);
                    double y = cy + r * Math.Sin(theta);
                    double angle = 180.0 - (progress * 90.0); // 180° -> 90°
                    return (x, y, angle);
                }
                else
                {
                    // entryFrom == Direction.South -> Viajando hacia el Norte, gira hacia el Este
                    double t = progress * (Math.PI / 2.0);
                    double theta = Math.PI + t; // 180° a 270°
                    double x = cx + r * Math.Cos(theta);
                    double y = cy + r * Math.Sin(theta);
                    double angle = 270.0 + (progress * 90.0); // 270° -> 360°/0°
                    return (x, y, angle >= 360 ? angle - 360 : angle);
                }
            }

            case TrackType.CurveSouthWest:
            {
                // Arco con centro en esquina inferior izquierda: (cellX + 0, cellY + 1)
                double cx = cellX + 0.0;
                double cy = cellY + 1.0;
                double r = 0.5;

                if (entryFrom == Direction.South)
                {
                    // Viajando hacia el Norte, gira hacia el Oeste
                    double t = progress * (Math.PI / 2.0);
                    double theta = 0.0 - t; // 0° a -90° (o 360° a 270°)
                    double x = cx + r * Math.Cos(theta);
                    double y = cy + r * Math.Sin(theta);
                    double angle = 270.0 - (progress * 90.0); // 270° -> 180°
                    return (x, y, angle);
                }
                else
                {
                    // entryFrom == Direction.West -> Viajando hacia el Este, gira hacia el Sur
                    double t = progress * (Math.PI / 2.0);
                    double theta = (3.0 * Math.PI / 2.0) + t; // 270° a 360°
                    double x = cx + r * Math.Cos(theta);
                    double y = cy + r * Math.Sin(theta);
                    double angle = 0.0 + (progress * 90.0); // 0° -> 90°
                    return (x, y, angle);
                }
            }

            case TrackType.CurveWestNorth:
            {
                // Arco con centro en esquina superior izquierda: (cellX + 0, cellY + 0)
                double cx = cellX + 0.0;
                double cy = cellY + 0.0;
                double r = 0.5;

                if (entryFrom == Direction.West)
                {
                    // Viajando hacia el Este, gira hacia el Norte
                    double t = progress * (Math.PI / 2.0);
                    double theta = (Math.PI / 2.0) - t; // 90° a 0°
                    double x = cx + r * Math.Cos(theta);
                    double y = cy + r * Math.Sin(theta);
                    double angle = 360.0 - (progress * 90.0); // 360° (0°) -> 270°
                    return (x, y, angle >= 360 ? angle - 360 : angle);
                }
                else
                {
                    // entryFrom == Direction.North -> Viajando hacia el Sur, gira hacia el Oeste
                    double t = progress * (Math.PI / 2.0);
                    double theta = 0.0 + t; // 0° a 90°
                    double x = cx + r * Math.Cos(theta);
                    double y = cy + r * Math.Sin(theta);
                    double angle = 90.0 + (progress * 90.0); // 90° -> 180°
                    return (x, y, angle);
                }
            }

            default:
                return (cellX + 0.5, cellY + 0.5, travelDirection.ToAngleDegrees());
        }
    }
}
