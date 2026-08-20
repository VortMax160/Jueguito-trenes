# 🚂 TrenSim 2D — Juego de Puzles Ferroviarios (Railbound Style)

> Un juego de **puzles y lógica ferroviaria por niveles** para PC de escritorio desarrollado con **C#**, **.NET 9** y **WPF (Windows Presentation Foundation)**. Planifica el trazado de vías con presupuesto limitado, esquiva obstáculos, haz coincidir cada tren con su estación del mismo color y pulsa **¡ARRANCAR TRENES!** para comprobar tu solución.

---

## 🧩 Dinámica del Juego de Puzles

### 🎮 Cómo se Juega
1. **Fase de Planificación (Planning):** Los trenes esperan estáticos en sus puntos de salida (`Spawn Points`). Analiza el mapa, la posición de los obstáculos (rocas y árboles) y dónde están las **Estaciones de Destino**.
2. **Construcción con Presupuesto:** Tienes un límite de piezas de vía (`Max Track Budget`) para guiar cada tren a su estación objetivo (emparejados por color: 🔴 Tren Rojo a Estación Roja, 🔵 Tren Azul a Estación Azul, etc.).
3. **¡ARRANCAR TRENES! (Simulación):** Pulsa el botón grande verde para poner en marcha los trenes.
   - **🎉 ¡VICTORIA!** Si todos los trenes llegan a sus estaciones de destino sanos y salvos, ganas el nivel, desbloqueas el siguiente y obtienes hasta **3 Estrellas ⭐⭐⭐** según la eficiencia de vías usadas.
   - **💥 ¡DERROTA!** Si un tren descarrila por falta de vía o choca contra otro tren, se detiene y puedes pulsar **`🔄 Ajustar Vías`** para corregir tu trazado.

---

## 🗺️ Campaña de Niveles

| Nivel | Nombre | Desafío / Mecánica | Presupuesto ⭐⭐⭐ |
|---|---|---|---|
| **1** | *Primeros Raíles* | Tutorial: Conexión recta con árboles en el terreno | ≤ 4 vías |
| **2** | *La Gran Curva* | Giro de 90° para conectar salida Este con estación Sur | ≤ 6 vías |
| **3** | *El Paso de las Rocas* | Cordillera rocosa en el medio que obliga a trazar una curva en 'S' | ≤ 9 vías |
| **4** | *Rutas Paralelas (Colores)* | 2 trenes simultáneos (Rojo y Azul) a sus respectivas terminales | ≤ 10 vías |
| **5** | *El Cruce en Cruz* | Dos trenes en trayectorias perpendiculares usando el cruce en cruz (+) | ≤ 12 vías |
| **6** | *Desafío Ferroviario Total* | 3 trenes y 3 estaciones con laberinto de obstáculos | ≤ 16 vías |

---

## 📸 Características del Motor

- **Auto-Vía Inteligente:** Al arrastrar el mouse, resuelve empalmes conectando rectas y curvas automáticamente.
- **Renderizado Vectorial a 60 FPS:** Vías con balasto, durmientes, rieles de acero, obstáculos naturales (árboles y rocas) y rótulos de estaciones iluminados.
- **Sistema de Puntuación y Estrellas:** Registro de mejor puntuación de vías por nivel y desbloqueo progresivo.
- **Navegación Fluida:** Zoom centrado en el cursor con la rueda del mouse y paneo con el botón central o `Espacio + Arrastre`.

---

## 🏗️ Estructura de la Solución

```
TrainGame/
├── TrainGame.sln
├── README.md                         # Documentación del juego de puzles
├── AGENTS.md                         # Guía de desarrollo para agentes IA
├── src/
│   └── TrainGame/
│       ├── Core/                     # Lógica de simulación y puzles (100% testeable)
│       │   ├── Puzzle/               # Módulo de Niveles de Puzle
│       │   │   ├── PuzzleLevel.cs    # Modelos de nivel, spawns, estaciones objetivo y estrellas
│       │   │   ├── LevelLibrary.cs   # Los 6 niveles prediseñados de la campaña
│       │   │   └── PuzzleManager.cs  # Evaluación de victoria, derrota, presupuesto y estrellas
│       │   ├── Direction.cs          # Direcciones cardinales
│       │   ├── TrackPiece.cs         # Conectividad de vías y cruces
│       │   ├── TrackGeometry.cs      # Geometría y arcos circulares para giros suaves
│       │   ├── RailGrid.cs           # Grilla 2D y colocación inteligente
│       │   ├── Train.cs              # Locomotoras, vagones y seguimiento de trayectoria
│       │   └── TrainSimulation.cs    # Game loop, radares y cinemática
│       ├── Rendering/                # Motor Gráfico
│       │   ├── GameRenderer.cs       # Renderizado de vías, obstáculos, estaciones objetivo y trenes
│       │   └── ParticleSystem.cs     # Vapor y humo
│       ├── UI/                       # Vista WPF
│       │   └── GameCanvas.cs         # Canvas interactivo con soporte para modo puzle
│       ├── App.xaml / App.xaml.cs    # Entrada de la aplicación
│       └── MainWindow.xaml / .cs     # Selector de niveles, presupuesto, botón de arranque y modales
└── tests/
    └── TrainGame.Tests/              # Pruebas Unitarias Automatizadas (xUnit)
        ├── CoreTests.cs              # 12 pruebas de física y vías
        └── PuzzleTests.cs            # 3 pruebas de niveles de puzle y condiciones de victoria
```

---

## 🚀 Comandos

```bash
# Compilar la solución
dotnet build

# Ejecutar el juego de puzles
dotnet run --project src/TrainGame

# Ejecutar las 15 pruebas unitarias
dotnet test
```

---

## 🎮 Controles

| Acción | Control |
|---|---|
| **Trazar Vías** | Clic Izquierdo y arrastrar con herramienta de vía |
| **Borrar Vías** | Clic Derecho sobre cualquier vía |
| **¡ARRANCAR TRENES!** | Botón verde superior `▶️ ¡ARRANCAR TRENES!` |
| **Ajustar / Reintentar** | Botón `🔄 Ajustar Vías` |
| **Zoom In / Out** | Rueda del mouse |
| **Mover Cámara / Pan** | Botón Central o `Espacio + Arrastrar` |
