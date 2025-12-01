# Rabbit vs Mole v2 🐰🕳️

A Unity-based 3D farming game featuring asymmetric gameplay between a Rabbit and a Mole, showcasing advanced game development techniques and optimized systems.

## 🎮 Game Overview

**Rabbit vs Mole v2** is a portfolio project demonstrating professional Unity game development practices. Players can control either a Rabbit (farming above ground) or a Mole (underground operations), each with unique mechanics and objectives.

## ✨ Key Features

### Core Gameplay
- **Dual Character System**: Play as Rabbit or Mole with distinct abilities and interactions
- **Farming Mechanics**: Plant seeds, water fields, harvest crops with a complete growth cycle
- **Underground System**: Mole-specific mechanics for tunneling and underground navigation
- **Day System**: Dynamic day-of-week progression affecting gameplay
- **Golden Carrot Collection**: Special collectibles tied to the day system

### Advanced Systems

#### 🎯 State Pattern Architecture
- **Farm Field State Machine**: Sophisticated state pattern implementation with multiple states:
  - `UntouchedField` → `PlantedField` → `GrownField` → `MoundedField` → `RootedField`
  - Clean separation of concerns with interface-based design
  - State transitions with validation and coroutine-based interactions

#### 🔊 Professional Audio System
- **Singleton Audio Manager** with thread-safe operations
- **Object Pooling** for 3D sound effects (20+ pooled AudioSources)
- **Addressables Integration** with async/await for dynamic audio loading
- **ConcurrentDictionary** for lock-free thread-safe audio clip caching
- **Music Crossfading**: Dual AudioSource system for seamless track transitions
- **Smart Playlist System**: Random music playback with immediate repeat avoidance
- **Audio Mixer Integration**: Separate volume controls for Music, SFX, Dialogue, and Ambient channels
- **Preloading System**: Pre-cache frequently used clips to prevent loading delays

#### 🚶 Advanced Walking Immersion System
- **Dynamic Terrain Surface Detection**: Real-time texture analysis using `GetAlphamaps()`
- **Performance-Optimized Footstep System**:
  - Surface detection result caching (2m distance threshold)
  - Particle system pooling (16 pre-instantiated systems)
  - Color change throttling (avoids expensive ParticleSystem.main.startColor updates)
  - Multiple cooldown systems to prevent excessive calls
  - `sqrMagnitude` optimization instead of `Distance` calculations
- **Terrain-Aware Effects**: Footstep particles and sounds adapt to terrain texture (grass, dirt, stone, etc.)
- **Animation Event Integration**: Footstep events triggered from animation system

#### 💬 Custom Dialogue System
- **Graph-Based Editor**: Visual node editor for creating dialogue sequences
- **Render Texture System**: Dynamic actor rendering to textures for dialogue display
- **Typewriter Effect**: Character-by-character text display with skip functionality
- **Fade Transitions**: Smooth actor fade-in/fade-out effects
- **Pose System**: Actor animation poses integrated with dialogue nodes
- **GUID-Based Node System**: Robust node linking with unique identifiers

#### 🤖 AI Behavior Systems

**Wasp AI Controller:**
- **State Machine**: 5-state behavior system (FindTarget → GoToTarget → CircleFlower → LandOnFlower → Rest)
- **Spiral Pathfinding**: Smooth Catmull-Rom-based spiral descent to flowers
- **Terrain Height Adaptation**: Dynamic height adjustment based on terrain raycasting
- **Weighted Target Selection**: Randomness factor prevents repetitive flower visits
- **Procedural Wing Animation**: Sin wave-based wing flapping synchronized with flight state

**Ant Path Follower:**
- **Catmull-Rom Spline Interpolation**: Smooth path following using cubic splines
- **Particle System Manipulation**: Direct particle position updates for ant visualization
- **Pre-calculated Path System**: Pre-computed 3D path points for performance
- **Terrain-Conforming Paths**: Raycast-based height adjustment along paths

#### 🎮 Advanced Player Controller
- **Physics-Based Movement**: Rigidbody movement with proper collision handling
- **Collision Sliding**: X/Z axis sliding when hitting obstacles
- **Acceleration/Deceleration System**: Custom `SpeedController` class for smooth speed transitions
- **Animation Hash Caching**: Pre-computed animation hashes for performance
- **Input System Integration**: Unity's new Input System with action maps
- **Interaction System**: Raycast-based interaction with `IInteractable` interface

#### 🗺️ Terrain Integration
- **Multi-Layer Terrain Support**: Texture-based surface identification
- **Terrain Layer Data System**: ScriptableObject-based configuration for surface properties
- **Dynamic Surface Detection**: Real-time terrain texture analysis with caching

## 🛠️ Technical Highlights

### Design Patterns
- ✅ **Singleton Pattern**: GameManager, AudioManager
- ✅ **State Pattern**: FarmField state machine
- ✅ **Object Pooling**: Audio sources, particle systems
- ✅ **Interface-Based Design**: IInteractable, IFarmFieldState
- ✅ **Observer Pattern**: Event-driven systems

### Performance Optimizations
- **Memory Management**: Object pooling for frequently instantiated objects
- **Caching Strategies**: Multiple caching layers (audio clips, terrain surfaces, animation hashes)
- **Thread Safety**: ConcurrentDictionary, lock statements for async operations
- **Allocation Reduction**: Avoided LINQ in hot paths, reused collections
- **Cooldown Systems**: Prevented excessive expensive operations (GetAlphamaps, particle updates)

### Advanced C# Features
- **Async/Await**: Addressables loading with Task-based async operations
- **Generics**: Type-safe extension methods and collections
- **Extension Methods**: Custom extensions for Transform, String, List, Enum
- **Coroutines**: Complex state machines and timing systems
- **LINQ**: Strategic use where performance allows

### Unity Technologies
- **Addressables**: Dynamic asset loading and management
- **Input System**: Modern input handling with action maps
- **Universal Render Pipeline (URP)**: Modern rendering pipeline
- **Cinemachine**: Camera system integration
- **Timeline**: Cutscene and animation sequencing
- **Post-Processing**: Visual effects stack
- **Terrain System**: Advanced terrain manipulation

## 📁 Project Structure

```
Assets/
├── Game Systems/          # Modular game systems
│   ├── AudioManager/      # Professional audio system
│   ├── DialogueSystem/     # Custom dialogue graph editor
│   ├── WalkingImmersion/  # Terrain-aware footstep system
│   ├── Wasp/              # Wasp AI behavior
│   ├── Ants/              # Ant path following system
│   └── GarbageScanner/    # Debug tools
├── Scripts/               # Core game scripts
│   ├── GameObjects/       # Game object implementations
│   │   └── FarmField/     # State pattern implementation
│   ├── Extensions/        # C# extension methods
│   └── Enums/             # Type definitions
└── Settings/              # Project configuration
```

## 🎯 Programming Skills Demonstrated

### Software Architecture
- **Modular System Design**: Self-contained, reusable game systems
- **Separation of Concerns**: Clear boundaries between systems
- **SOLID Principles**: Single responsibility, interface segregation
- **Clean Code**: Readable, maintainable codebase with comprehensive comments

### Advanced Algorithms
- **Spline Interpolation**: Catmull-Rom spline calculations
- **State Machine Design**: Complex behavior trees
- **Pathfinding**: Weighted target selection algorithms
- **Surface Detection**: Terrain texture analysis

### Performance Engineering
- **Profiling Awareness**: Optimized hot paths identified and improved
- **Memory Profiling**: Reduced allocations in critical systems
- **Frame Rate Optimization**: Cooldowns and caching prevent frame drops
- **Async Operations**: Non-blocking asset loading

### Game Development Expertise
- **Unity Best Practices**: Proper use of Unity systems and APIs
- **Physics Integration**: Rigidbody movement and collision handling
- **Animation Systems**: Mecanim integration with custom controllers
- **Asset Management**: Addressables for scalable content delivery

## 🚀 Getting Started

### Requirements
- Unity 2022.3 LTS or later
- Universal Render Pipeline (URP)
- Input System package

### Setup
1. Clone the repository
2. Open the project in Unity
3. Ensure all packages are imported (check Package Manager)
4. Open a scene from `Assets/Scenes/`
5. Press Play!

## 📝 Notes

This project was developed as a portfolio piece to showcase:
- Advanced Unity development skills
- Performance optimization techniques
- Clean code architecture
- Complex system integration
- Professional game development practices

## 📄 License

This project is for portfolio purposes. All assets and code are property of the developer.

---

**Built with Unity** | **C#** | **Professional Game Development**

