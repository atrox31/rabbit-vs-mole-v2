# Rabbit vs Mole v2 🐰🕳️

> **Portfolio Project** | A comprehensive Unity 6 LTS game development showcase demonstrating advanced programming techniques, performance optimization, and professional system architecture.

![Unity](https://img.shields.io/badge/Unity-6000.2.12f1-black?style=flat-square&logo=unity)
![C#](https://img.shields.io/badge/C%23-11.0-239120?style=flat-square&logo=c-sharp)
![License](https://img.shields.io/badge/License-Portfolio-blue?style=flat-square)

---

## 📋 About This Project

**Rabbit vs Mole v2** is a professional Unity game development portfolio project that demonstrates mastery of advanced game programming concepts, performance optimization techniques, and clean software architecture. This 3D farming game features asymmetric gameplay mechanics, sophisticated AI systems, and a modular, production-ready codebase.

### 🎯 Project Goals

This project was developed to showcase:
- **Advanced Unity Development**: Deep understanding of Unity's systems and best practices
- **Performance Engineering**: Optimized systems with profiling-driven improvements
- **Software Architecture**: Clean, modular, maintainable code following SOLID principles
- **Complex System Integration**: Multiple interconnected systems working seamlessly
- **Professional Game Development**: Production-quality code and systems

---

## 🎮 Game Overview

**Rabbit vs Mole v2** is a 3D farming game featuring asymmetric gameplay between two playable characters:
- **Rabbit**: Farm above ground, plant seeds, water fields, and harvest crops
- **Mole**: Navigate underground with unique tunneling mechanics

The game includes a complete farming cycle, dynamic day-of-week progression, golden carrot collectibles, and immersive terrain-based systems.

---

## 🏗️ Systemy (Systems)

### 🎮 Player Management System
- **Modular Player Controller Architecture**: Separated base system from game-specific implementations
- **Input Device Manager**: Centralized management of gamepads and keyboards
  - Tracks gamepad usage to prevent device conflicts
  - Automatic device refresh on connection/disconnection
  - Support for split-screen multiplayer with device assignment
- **Agent Controller System**: Supports Human, Bot, and Online player types
- **Player Avatar System**: Customizable avatar stats and speed controllers
- **Spawn Point System**: Game-specific spawn point management with player type support

### 🔊 Professional Audio System
- **Singleton Audio Manager** with thread-safe operations
- **Object Pooling**: 20+ pooled AudioSources for 3D sound effects
- **Addressables Integration**: Async/await for dynamic audio loading
- **Thread-Safe Caching**: 
  - `ConcurrentDictionary` for lock-free non-Addressable clip management
  - Lock-based caching for Addressable clips with duplicate request prevention
- **Music Crossfading**: Dual AudioSource system (A/B) for seamless track transitions
- **Smart Playlist System**: 
  - Random music playback with immediate repeat avoidance
  - Filtered playlist caching to avoid LINQ allocations
- **Audio Mixer Integration**: Separate volume controls (Music, SFX, Dialogue, Ambient)
- **Preloading System**: Pre-cache frequently used clips to prevent loading delays
- **Ambient Sound System**: Configurable ambient sound emitters with mixer group support
- **Dynamic Audio Listener Management**: Automatic listener registration for split-screen

### 🚶 Advanced Walking Immersion System
- **Dynamic Terrain Surface Detection**: Real-time texture analysis using `GetAlphamaps()`
- **Performance-Optimized Footstep System**:
  - Surface detection result caching (2m distance threshold)
  - Particle system pooling (16 pre-instantiated systems)
  - Color change throttling (avoids expensive ParticleSystem updates)
  - Multiple cooldown systems to prevent excessive calls
  - `sqrMagnitude` optimization instead of `Distance` calculations
- **Terrain-Aware Effects**: Footstep particles and sounds adapt to terrain texture
- **Animation Event Integration**: Footstep events triggered from animation system
- **ScriptableObject Configuration**: `TerrainLayerData` for surface properties
- **Addressables Sound Loading**: Label-based loading of terrain sound configurations

### 💬 Custom Dialogue System
- **Graph-Based Editor**: Visual node editor built with Unity GraphView API
  - Custom node types: Dialogue, Trigger, Logic, Start nodes
  - GUID-based node linking for robust connections
  - Serialized node data in ScriptableObject
- **Render Texture System**: Dynamic actor rendering to textures for dialogue display
- **Typewriter Effect**: Character-by-character text display with skip functionality
- **Fade Transitions**: Smooth actor fade-in/fade-out effects
- **Pose System**: Actor animation poses integrated with dialogue nodes
- **Trigger Data System**: Extensible system for game-specific dialogue triggers
- **Input System Integration**: Custom Input Actions for dialogue control
- **Addressables Integration**: Async loading of dialogue canvas prefabs

### 🤖 AI Behavior Systems

**Wasp AI Controller:**
- **5-State Behavior System**: FindTarget → GoToTarget → CircleFlower → LandOnFlower → Rest
- **Spiral Pathfinding**: Smooth Catmull-Rom-based spiral descent to flowers
- **Terrain Height Adaptation**: Dynamic height adjustment based on terrain raycasting
- **Weighted Target Selection**: Randomness factor prevents repetitive flower visits
- **Procedural Wing Animation**: Sin wave-based wing flapping synchronized with flight state
- **Coroutine-Based State Machine**: Clean state transitions with yield-based timing

**Ant Path Follower:**
- **Catmull-Rom Spline Interpolation**: Smooth path following using cubic splines
- **Particle System Manipulation**: Direct particle position updates for ant visualization
- **Pre-calculated Path System**: Pre-computed 3D path points for performance
- **Terrain-Conforming Paths**: Raycast-based height adjustment along paths

### 🎮 Advanced Player Controller
- **Physics-Based Movement**: Rigidbody movement with proper collision handling
- **Collision Sliding**: X/Z axis sliding when hitting obstacles
- **Acceleration/Deceleration System**: Custom `SpeedController` class for smooth speed transitions
- **Animation Hash Caching**: Pre-computed animation hashes for performance
- **Input System Integration**: Unity's new Input System with action maps
- **Interaction System**: Raycast-based interaction with `IInteractable` interface
- **Avatar Stats System**: Configurable player statistics

### 🌾 Farming System
- **State Pattern Implementation**: Farm field state machine
  - State flow: `UntouchedField` → `PlantedField` → `GrownField` → `MoundedField` → `RootedField`
  - Interface-based design (`IFarmFieldState`) with clean separation of concerns
  - State transitions with validation and coroutine-based interactions
  - Action mapping system for player-specific interactions
- **Watering System**: Coroutine-based watering with visual feedback
- **Seed Growth System**: Time-based growth with visual state changes
- **Root System**: Random root generation affecting gameplay
- **Mound System**: Mole-specific mound creation and destruction
- **Field Generators**: Procedural field generation systems

### 🗺️ Scene Management System
- **Addressables-Based Loading**: Async scene loading with progress tracking
- **Material Preloading**: Automatic material preloading from Addressables
- **Loading Screen Integration**: Custom loading screen with progress display
- **Transition System**: Smooth scene transitions with EasyTransitions integration
- **Scene Lifecycle Management**: Proper scene activation and cleanup
- **Progress Callbacks**: Multi-stage progress reporting (deserialization, loading, materials)

### 🎨 UI System
- **Main Menu Manager**: Comprehensive menu system with game mode selection
- **Day Selector**: Visual day-of-week selection system
- **Game Mode System**: ScriptableObject-based game mode configuration
- **Settings Management**: Volume controls, key binding, and preferences
- **Input Action Integration**: UI-specific input handling with cancel support
- **Localization Support**: Prepared for multi-language support

### 💾 Save System
- **Encrypted PlayerPrefs**: Secure data storage with encryption
- **Game Progress Manager**: Status tracking for golden carrots and story progress
- **Data Persistence**: Dictionary-based status saving/loading

### 🛠️ Utility Systems
- **Extension Methods Library**: Comprehensive extension methods for:
  - Transform, GameObject, String, List, Enum, Dictionary, Int, Float
  - ParticleSystem, RaycastHit
- **Debug Helper**: Centralized logging system
- **Garbage Scanner**: Editor tool for finding memory allocations
- **Bootstrap System**: Core initialization system with `CoreBootstrapComponent`
- **Serialized Collections**: Enhanced dictionary serialization support

---

## 🎨 Wzorce Projektowe (Design Patterns)

### ✅ Singleton Pattern
- **Generic Singleton Implementation**: `SingletonMonoBehaviour<T>` with bootstrap integration
- **Thread-Safe Access**: Proper instance management with null checks
- **DontDestroyOnLoad**: Persistent managers across scenes
- **Used in**: `GameManager`, `AudioManager`, `SceneLoader`

### ✅ State Pattern
- **Farm Field State Machine**: Complete state pattern implementation
- **Interface-Based States**: `IFarmFieldState` interface for all states
- **State Transitions**: Clean state switching with validation
- **Coroutine-Based Actions**: Non-blocking state interactions

### ✅ Object Pooling Pattern
- **Audio Source Pooling**: 20+ pooled AudioSources for 3D sounds
- **Particle System Pooling**: 16 pre-instantiated footstep particle systems
- **Queue-Based Management**: Efficient pool allocation and deallocation
- **Automatic Return**: Coroutine-based automatic pool return

### ✅ Interface-Based Design
- **IInteractable**: Interaction system interface
- **IFarmFieldState**: State pattern interface
- **Separation of Concerns**: Clear contract definitions

### ✅ Observer Pattern
- **Event-Driven Systems**: Callback-based communication
- **Unity Events**: Integration with Unity's event system
- **Action Delegates**: C# delegate-based notifications

### ✅ Factory Pattern
- **Agent Controller Factory**: `CreateAgentController()` method
- **System Initialization**: Factory methods for system setup

### ✅ Strategy Pattern
- **Action Mapping**: Player-specific action strategies
- **Speed Controller**: Configurable movement strategies

### ✅ Command Pattern
- **Action System**: Encapsulated player actions
- **Cancellation Support**: Action cancellation with CancellationToken

### ✅ Template Method Pattern
- **Bootstrap System**: `CoreBootstrapComponent` with template methods
- **State Base Class**: `FarmFieldStateBase` with template methods

### ✅ ScriptableObject Pattern
- **Data Configuration**: GameModeData, TerrainLayerData, MusicPlaylistSO
- **Asset-Based Design**: Designer-friendly configuration system

---

## 🔧 Techniki Programistyczne (Programming Techniques)

### 🚀 Zaawansowane Funkcje C#
- **Async/Await**: 
  - Addressables loading with Task-based async operations
  - Non-blocking asset loading
  - Proper exception handling in async methods
- **Generics**: 
  - Type-safe singleton implementation
  - Generic extension methods
  - Type-safe collections
- **Extension Methods**: 
  - Custom extensions for Transform, String, List, Enum, Dictionary, Int, Float
  - ParticleSystem, RaycastHit, GameObject extensions
  - Improved code readability and reusability
- **Coroutines**: 
  - Complex state machines and timing systems
  - Non-blocking operations
  - Yield-based control flow
- **LINQ**: 
  - Strategic use where performance allows
  - Avoided in hot paths to prevent allocations
- **Concurrent Collections**: 
  - `ConcurrentDictionary` for thread-safe operations
  - Lock-free read operations
- **Delegates & Actions**: 
  - Callback systems
  - Event-driven architecture
- **Nullable Reference Types**: Proper null checking throughout

### ⚡ Optymalizacje Wydajności (Performance Optimizations)
- **Memory Management**: 
  - Object pooling for frequently instantiated objects
  - Reduced GC allocations
  - Collection reuse
- **Caching Strategies**: 
  - Multiple caching layers (audio clips, terrain surfaces, animation hashes)
  - Distance-based cache invalidation
  - Thread-safe cache access
- **Thread Safety**: 
  - `ConcurrentDictionary` for lock-free operations
  - Lock statements for async operations
  - Proper synchronization primitives
- **Allocation Reduction**: 
  - Avoided LINQ in hot paths
  - Reused collections
  - Pre-allocated buffers
- **Cooldown Systems**: 
  - Prevented excessive expensive operations
  - Throttled updates
- **Profiling-Driven Optimization**: 
  - Hot paths identified using Unity Profiler
  - Frame rate optimization
  - Memory profiling
- **Mathematical Optimizations**:
  - `sqrMagnitude` instead of `Distance` calculations
  - Pre-computed values (animation hashes)
  - Efficient algorithms (spline interpolation)

### 🎯 Algorytmy (Algorithms)
- **Spline Interpolation**: 
  - Catmull-Rom spline calculations for smooth paths
  - Cubic spline mathematics
- **State Machine Design**: 
  - Complex behavior trees
  - State transition validation
- **Pathfinding**: 
  - Weighted target selection algorithms
  - Spiral pathfinding for wasps
- **Surface Detection**: 
  - Terrain texture analysis
  - Alphamap-based texture identification
  - Caching strategies
- **Procedural Animation**: 
  - Mathematical animation systems (sin waves, splines)
  - Time-based calculations
- **Weighted Random Selection**: 
  - Target selection with randomness factors
  - Avoids repetitive behavior

### 🏛️ Architektura Oprogramowania (Software Architecture)
- **Modular System Design**: 
  - Self-contained, reusable game systems
  - Clear system boundaries
- **Separation of Concerns**: 
  - Clear boundaries between systems
  - Single responsibility principle
- **SOLID Principles**: 
  - Single Responsibility: Each class has one job
  - Open/Closed: Extensible through interfaces
  - Liskov Substitution: Proper inheritance
  - Interface Segregation: Focused interfaces
  - Dependency Inversion: Interface-based dependencies
- **Clean Code**: 
  - Readable, maintainable codebase
  - Comprehensive comments
  - Consistent naming conventions
- **Dependency Management**: 
  - Minimal coupling
  - Interface-based dependencies
  - Singleton pattern for managers

---

## 🛠️ Technical Stack

### Unity Technologies
- **Unity 6.0.2 LTS** (6000.2.12f1)
- **Universal Render Pipeline (URP)**: Modern rendering pipeline
- **Addressables**: Dynamic asset loading and management
- **Input System**: Modern input handling with action maps
- **Cinemachine**: Professional camera system integration
- **Timeline**: Cutscene and animation sequencing
- **Post-Processing**: Visual effects stack
- **Terrain System**: Advanced terrain manipulation
- **GraphView API**: Custom editor tools
- **Serialized Collections**: Enhanced serialization

### Third-Party Packages
- **EasyTransitions**: Scene transition effects
- **SerializedCollections**: Enhanced dictionary serialization
- **TextMesh Pro**: Advanced text rendering

---

## 📁 Project Structure

```
Assets/
├── Game Systems/                    # Modular, reusable game systems
│   ├── AudioManager/                # Professional audio system with pooling
│   ├── DialogueSystem/              # Custom dialogue graph editor
│   ├── WalkingImmersion/            # Terrain-aware footstep system
│   ├── LoadScene/                   # Scene loading with Addressables
│   ├── Player Management System/    # Input device and player management
│   ├── CoreMonoBehaviourSingleton/ # Bootstrap and singleton base
│   ├── Universal/                  # Extension methods and utilities
│   ├── GarbageScanner/             # Debug and profiling tools
│   └── AddressablesStaticDictionary/ # Addressables management
├── Game/                            # Game-specific implementations
│   ├── Managers/                   # GameManager, GameInspector
│   ├── MainMenu/                   # Menu systems and game modes
│   └── RvM Player Management System/ # Game-specific player controllers
├── Scripts/                        # Core game scripts
│   ├── GameObjects/                # Game object implementations
│   │   ├── FarmField/             # State pattern implementation
│   │   ├── Base/                  # Base classes
│   │   └── Misc/                 # Miscellaneous game objects
│   ├── Wasp/                      # Wasp AI system
│   ├── Ants/                      # Ant path following
│   ├── System/                    # Save system, progress management
│   └── Enums/                     # Type definitions
├── Interface/                      # UI systems
├── Graphics/                       # Visual assets
├── Audio/                         # Sound assets
└── Settings/                      # Project configuration
```

---

## 💻 Programming Skills Demonstrated

### Software Architecture
- **Modular System Design**: Self-contained, reusable game systems
- **Separation of Concerns**: Clear boundaries between systems
- **SOLID Principles**: Single responsibility, interface segregation, dependency inversion
- **Clean Code**: Readable, maintainable codebase with comprehensive comments
- **Design Patterns**: Proper application of industry-standard patterns

### Advanced Algorithms
- **Spline Interpolation**: Catmull-Rom spline calculations for smooth paths
- **State Machine Design**: Complex behavior trees and state transitions
- **Pathfinding**: Weighted target selection algorithms
- **Surface Detection**: Terrain texture analysis and caching
- **Procedural Animation**: Mathematical animation systems (sin waves, splines)

### Performance Engineering
- **Profiling Awareness**: Optimized hot paths identified using Unity Profiler
- **Memory Profiling**: Reduced allocations in critical systems
- **Frame Rate Optimization**: Cooldowns and caching prevent frame drops
- **Async Operations**: Non-blocking asset loading
- **Object Pooling**: Eliminated garbage collection spikes
- **Thread Safety**: Proper async/await and concurrent collections usage

### Game Development Expertise
- **Unity Best Practices**: Proper use of Unity systems and APIs
- **Physics Integration**: Rigidbody movement and collision handling
- **Animation Systems**: Mecanim integration with custom controllers
- **Asset Management**: Addressables for scalable content delivery
- **Editor Tools**: Custom editor scripts for workflow improvement
- **Input Management**: Modern Input System with action maps

---

## 🚀 Getting Started

### Requirements
- **Unity 6.0.2 LTS** (6000.2.12f1) or compatible version
- **Universal Render Pipeline (URP)**
- **Input System** package
- **Addressables** package

### Setup Instructions
1. Clone the repository
2. Open the project in Unity 6.0.2 LTS
3. Ensure all packages are imported (check Package Manager)
4. Open a scene from `Assets/Scenes/`
5. Press Play!

### Git LFS Setup
This project uses Git LFS for large binary files. After cloning, run:
```bash
git lfs install
```

---

## 📊 Code Quality Metrics

- **Modular Architecture**: Systems are self-contained and reusable
- **Performance Optimized**: Profiling-driven optimizations throughout
- **Thread-Safe**: Proper async/await and concurrent collections usage
- **Well-Documented**: Comprehensive code comments and structure
- **Production-Ready**: Follows Unity best practices and industry standards
- **SOLID Principles**: Clean architecture with proper separation of concerns
- **Design Patterns**: Industry-standard patterns properly implemented

---

## 🎓 Learning Outcomes

This project demonstrates proficiency in:
- Advanced Unity game development
- Performance optimization and profiling
- Software architecture and design patterns
- C# advanced features (async/await, generics, extensions)
- System integration and modular design
- Professional game development workflows
- Thread-safe programming
- Memory management and optimization
- Custom editor tool development

---

## 📝 Notes

This project serves as a **portfolio piece** to showcase:
- Advanced Unity development skills
- Performance optimization techniques
- Clean code architecture
- Complex system integration
- Professional game development practices
- Design pattern implementation
- Advanced C# programming techniques

All code and systems were developed from scratch to demonstrate technical capabilities and understanding of game development principles.

---

## 📄 License

This project is for **portfolio purposes only**. All assets and code are property of the developer.

---

**Built with Unity 6 LTS** | **C#** | **Professional Game Development**

*This repository serves as a technical portfolio demonstrating advanced Unity game development skills and software engineering practices.*
