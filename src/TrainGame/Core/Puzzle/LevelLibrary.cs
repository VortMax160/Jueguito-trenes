using System.Collections.Generic;

namespace TrainGame.Core.Puzzle;

public static class LevelLibrary
{
    public static List<PuzzleLevel> GetAllLevels()
    {
        return new List<PuzzleLevel>
        {
            CreateLevel1(),
            CreateLevel2(),
            CreateLevel3(),
            CreateLevel4(),
            CreateLevel5(),
            CreateLevel6()
        };
    }

    private static PuzzleLevel CreateLevel1()
    {
        // Nivel 1: Línea recta básica (distancia: 4 vías requeridas)
        return new PuzzleLevel
        {
            Id = 1,
            Title = "Nivel 1: Primeros Raíles",
            Description = "Conecta la salida del Expreso Rojo con la Estación Central.",
            Hint = "Traza 4 vías horizontales directas hacia la estación.",
            Star3Budget = 4,
            Star2Budget = 6,
            MaxTrackBudget = 8,
            TrainSpawns = new List<TrainSpawn>
            {
                new() { X = 4, Y = 6, Direction = Direction.East, ColorHex = "#EF4444", Name = "Expreso Rojo", TargetStationId = 1, CarriageCount = 1 }
            },
            TargetStations = new List<TargetStationInfo>
            {
                new() { Id = 1, X = 9, Y = 6, Name = "Estación Central", ColorHex = "#EF4444", IsHorizontal = true }
            },
            Obstacles = new List<ObstacleInfo>
            {
                new() { X = 6, Y = 5, Type = "Tree" },
                new() { X = 7, Y = 7, Type = "Tree" }
            }
        };
    }

    private static PuzzleLevel CreateLevel2()
    {
        // Nivel 2: Girar con curva de 90° (distancia mínima: 8 vías requeridas)
        return new PuzzleLevel
        {
            Id = 2,
            Title = "Nivel 2: La Gran Curva",
            Description = "El tren sale hacia el Este y la estación está hacia el Sur.",
            Hint = "Traza vías horizontales hasta la columna 8, coloca una curva Este-Sur y baja con vías verticales.",
            Star3Budget = 8,
            Star2Budget = 10,
            MaxTrackBudget = 13,
            TrainSpawns = new List<TrainSpawn>
            {
                new() { X = 4, Y = 4, Direction = Direction.East, ColorHex = "#3B82F6", Name = "Tren Azul", TargetStationId = 1, CarriageCount = 2 }
            },
            TargetStations = new List<TargetStationInfo>
            {
                new() { Id = 1, X = 8, Y = 9, Name = "Puerto Sur", ColorHex = "#3B82F6", IsHorizontal = false }
            },
            Obstacles = new List<ObstacleInfo>
            {
                new() { X = 8, Y = 3, Type = "Rock" },
                new() { X = 4, Y = 7, Type = "Tree" }
            }
        };
    }

    private static PuzzleLevel CreateLevel3()
    {
        // Nivel 3: Esquivar obstáculos de rocas (distancia mínima: 13 vías)
        return new PuzzleLevel
        {
            Id = 3,
            Title = "Nivel 3: El Paso de las Rocas",
            Description = "Una cordillera de rocas bloquea el camino directo.",
            Hint = "Desvía la vía hacia el norte o sur alrededor de las rocas y vuelve a alinearte con la estación.",
            Star3Budget = 13,
            Star2Budget = 16,
            MaxTrackBudget = 20,
            TrainSpawns = new List<TrainSpawn>
            {
                new() { X = 3, Y = 7, Direction = Direction.East, ColorHex = "#10B981", Name = "Carguero Esmeralda", TargetStationId = 1, CarriageCount = 2 }
            },
            TargetStations = new List<TargetStationInfo>
            {
                new() { Id = 1, X = 13, Y = 7, Name = "Mina Verde", ColorHex = "#10B981", IsHorizontal = true }
            },
            Obstacles = new List<ObstacleInfo>
            {
                new() { X = 7, Y = 6, Type = "Rock" },
                new() { X = 7, Y = 7, Type = "Rock" },
                new() { X = 7, Y = 8, Type = "Rock" },
                new() { X = 8, Y = 7, Type = "Rock" }
            }
        };
    }

    private static PuzzleLevel CreateLevel4()
    {
        // Nivel 4: 2 Trenes de distinto color a sus respectivas estaciones (7 + 7 = 14 vías)
        return new PuzzleLevel
        {
            Id = 4,
            Title = "Nivel 4: Rutas Paralelas (Colores)",
            Description = "Lleva el Tren Rojo a la Estación Roja y el Tren Azul a la Azul.",
            Hint = "Traza dos líneas rectas independientes para cada tren.",
            Star3Budget = 14,
            Star2Budget = 17,
            MaxTrackBudget = 20,
            TrainSpawns = new List<TrainSpawn>
            {
                new() { X = 3, Y = 4, Direction = Direction.East, ColorHex = "#EF4444", Name = "Tren Rojo", TargetStationId = 1, CarriageCount = 1 },
                new() { X = 3, Y = 8, Direction = Direction.East, ColorHex = "#3B82F6", Name = "Tren Azul", TargetStationId = 2, CarriageCount = 1 }
            },
            TargetStations = new List<TargetStationInfo>
            {
                new() { Id = 1, X = 11, Y = 4, Name = "Terminal Roja", ColorHex = "#EF4444", IsHorizontal = true },
                new() { Id = 2, X = 11, Y = 8, Name = "Terminal Azul", ColorHex = "#3B82F6", IsHorizontal = true }
            },
            Obstacles = new List<ObstacleInfo>
            {
                new() { X = 7, Y = 5, Type = "Tree" },
                new() { X = 7, Y = 6, Type = "Tree" },
                new() { X = 7, Y = 7, Type = "Tree" }
            }
        };
    }

    private static PuzzleLevel CreateLevel5()
    {
        // Nivel 5: Cruce perpendicular usando el cruce en cruz (9 + 7 - 1 = 15 vías)
        return new PuzzleLevel
        {
            Id = 5,
            Title = "Nivel 5: El Cruce en Cruz",
            Description = "Dos trenes deben atravesar sus trayectorias perpendiculares.",
            Hint = "Utiliza una pieza de Cruce (+) en la intersección (7, 6).",
            Star3Budget = 15,
            Star2Budget = 18,
            MaxTrackBudget = 22,
            TrainSpawns = new List<TrainSpawn>
            {
                new() { X = 2, Y = 6, Direction = Direction.East, ColorHex = "#EF4444", Name = "Expreso Este", TargetStationId = 1, CarriageCount = 1 },
                new() { X = 7, Y = 2, Direction = Direction.South, ColorHex = "#8B5CF6", Name = "Expreso Sur", TargetStationId = 2, CarriageCount = 1 }
            },
            TargetStations = new List<TargetStationInfo>
            {
                new() { Id = 1, X = 12, Y = 6, Name = "Estación Oriental", ColorHex = "#EF4444", IsHorizontal = true },
                new() { Id = 2, X = 7, Y = 10, Name = "Estación Meridional", ColorHex = "#8B5CF6", IsHorizontal = false }
            },
            Obstacles = new List<ObstacleInfo>
            {
                new() { X = 5, Y = 4, Type = "Rock" },
                new() { X = 9, Y = 8, Type = "Tree" }
            }
        };
    }

    private static PuzzleLevel CreateLevel6()
    {
        // Nivel 6: Tres trenes, laberinto de montañas (37 vías mínimas)
        return new PuzzleLevel
        {
            Id = 6,
            Title = "Nivel 6: Desafío Ferroviario Total",
            Description = "3 trenes (Rojo, Azul, Verde) deben llegar a sus terminales esquivando obstáculos.",
            Hint = "Optimiza cada curva para ahorrar piezas de vía.",
            Star3Budget = 37,
            Star2Budget = 40,
            MaxTrackBudget = 43,
            TrainSpawns = new List<TrainSpawn>
            {
                new() { X = 2, Y = 3, Direction = Direction.East, ColorHex = "#EF4444", Name = "Tren Rojo", TargetStationId = 1, CarriageCount = 1 },
                new() { X = 2, Y = 7, Direction = Direction.East, ColorHex = "#3B82F6", Name = "Tren Azul", TargetStationId = 2, CarriageCount = 1 },
                new() { X = 2, Y = 11, Direction = Direction.East, ColorHex = "#10B981", Name = "Tren Verde", TargetStationId = 3, CarriageCount = 1 }
            },
            TargetStations = new List<TargetStationInfo>
            {
                new() { Id = 1, X = 14, Y = 3, Name = "Base Roja", ColorHex = "#EF4444", IsHorizontal = true },
                new() { Id = 2, X = 14, Y = 7, Name = "Base Azul", ColorHex = "#3B82F6", IsHorizontal = true },
                new() { Id = 3, X = 14, Y = 11, Name = "Base Verde", ColorHex = "#10B981", IsHorizontal = true }
            },
            Obstacles = new List<ObstacleInfo>
            {
                new() { X = 8, Y = 3, Type = "Rock" },
                new() { X = 8, Y = 5, Type = "Tree" },
                new() { X = 8, Y = 9, Type = "Rock" },
                new() { X = 8, Y = 11, Type = "Tree" }
            }
        };
    }
}
