# AGENTS.md

Guía y contexto para agentes de IA que colaboren en este repositorio.

---

## 🚂 Resumen del Proyecto

- **Nombre / Tipo:** Juego de Puzles Ferroviarios 2D (estilo *Railbound*).
- **Lenguaje:** C# (.NET 9).
- **Framework UI:** **WPF** (`net9.0-windows`).
- **Plataforma:** PC de escritorio (Windows).

---

## 🛠️ Stack Tecnológico y Arquitectura

1. **Framework de Interfaz y Renderizado:**
   - **WPF** (`net9.0-windows`).
   - `GameCanvas.cs` con loop de 60 FPS mediante `CompositionTarget.Rendering` y renderizado vectorial en `DrawingContext`.
2. **Módulo de Puzles y Progresión:**
   - `src/TrainGame/Core/Puzzle/`:
     - `PuzzleLevel.cs`: Modelos de niveles, spawns, estaciones objetivo, obstáculos y puntuaciones.
     - `LevelLibrary.cs`: Biblioteca con los 6 niveles de la campaña.
     - `PuzzleManager.cs`: Administrador de estados (`Planning`, `Running`, `Victory`, `Defeat`), presupuesto de vías y cálculo de estrellas (1 a 3 ⭐).
3. **Estructura de la Solución:**
   - `TrainGame.sln`: Solución principal.
   - `src/TrainGame/`: Proyecto de aplicación WPF (`TrainGame.csproj`).
     - `Core/`: Modelos de vías, trenes, geometría y física.
     - `Rendering/`: Renderizado vectorial de vías, trenes, obstáculos y partículas de vapor.
     - `UI/`: Canvas interactivo y ventana principal XAML.
   - `tests/TrainGame.Tests/`: Proyecto de pruebas unitarias xUnit (`CoreTests.cs`, `PuzzleTests.cs`).

---

## 💻 Entorno y Requisitos

- **Sistema Operativo:** Windows con .NET 9 SDK / Runtime.
- **Herramienta CLI:** .NET SDK (`dotnet`).

---

## 🚀 Comandos de Verificación

```bash
# Compilar la solución completa
dotnet build

# Ejecutar la aplicación WPF
dotnet run --project src/TrainGame

# Ejecutar las 15 pruebas unitarias
dotnet test
```

---

## 📝 Convenciones y Buenas Prácticas

- Mantener la lógica de puzles y simulación en `Core/` desacoplada de WPF.
- Cada nuevo nivel, obstáculo o regla de victoria debe contar con pruebas unitarias en `tests/TrainGame.Tests/`.
- Usar enlaces en formato Markdown a los archivos del repositorio al describir cambios o responder.
