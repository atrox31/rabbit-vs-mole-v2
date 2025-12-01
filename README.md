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

## ✨ Key Features & Systems

### 🎯 Core Gameplay
- **Dual Character System**: Play as Rabbit or Mole with distinct abilities and interactions
- **Farming Mechanics**: Complete growth cycle from planting to harvesting
- **Underground System**: Mole-specific mechanics for tunneling and underground navigation
- **Day System**: Dynamic day-of-week progression affecting gameplay
- **Golden Carrot Collection**: Special collectibles tied to the day system

### 🏗️ Advanced Systems

#### State Pattern Architecture
- **Farm Field State Machine**: Sophisticated state pattern implementation
  - State flow: `UntouchedField` → `PlantedField` → `GrownField` → `MoundedField` → `RootedField`
  - Interface-based design with clean separation of concerns
  - State transitions with validation and coroutine-based interactions
  - Demonstrates deep understanding of design patterns

#### 🔊 Professional Audio System
- **Singleton Audio Manager** with thread-safe operations
- **Object Pooling**: 20+ pooled AudioSources for 3D sound effects
- **Addressables Integration**: Async/await for dynamic audio loading
- **Thread-Safe Caching**: ConcurrentDictionary for lock-free audio clip management
- **Music Crossfading**: Dual AudioSource system for seamless track transitions
- **Smart Playlist System**: Random music playback with immediate repeat avoidance
- **Audio Mixer Integration**: Separate volume controls (Music, SFX, Dialogue, Ambient)
- **Preloading System**: Pre-cache frequently used clips to prevent loading delays

#### 🚶 Advanced Walking Immersion System
- **Dynamic Terrain Surface Detection**: Real-time texture analysis using `GetAlphamaps()`
- **Performance-Optimized Footstep System**:
  - Surface detection result caching (2m distance threshold)
  - Particle system pooling (16 pre-instantiated systems)
  - Color change throttling (avoids expensive ParticleSystem updates)
  - Multiple cooldown systems to prevent excessive calls
  - `sqrMagnitude` optimization instead of `Distance` calculations
- **Terrain-Aware Effects**: Footstep particles and sounds adapt to terrain texture
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
- **5-State Behavior System**: FindTarget → GoToTarget → CircleFlower → LandOnFlower → Rest
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

### Design Patterns Implemented
- ✅ **Singleton Pattern**: GameManager, AudioManager
- ✅ **State Pattern**: FarmField state machine
- ✅ **Object Pooling**: Audio sources, particle systems
- ✅ **Interface-Based Design**: IInteractable, IFarmFieldState
- ✅ **Observer Pattern**: Event-driven systems
- ✅ **Factory Pattern**: System initialization

### Advanced C# Features
- **Async/Await**: Addressables loading with Task-based async operations
- **Generics**: Type-safe extension methods and collections
- **Extension Methods**: Custom extensions for Transform, String, List, Enum
- **Coroutines**: Complex state machines and timing systems
- **LINQ**: Strategic use where performance allows
- **Concurrent Collections**: Thread-safe data structures

### Performance Optimizations
- **Memory Management**: Object pooling for frequently instantiated objects
- **Caching Strategies**: Multiple caching layers (audio clips, terrain surfaces, animation hashes)
- **Thread Safety**: ConcurrentDictionary, lock statements for async operations
- **Allocation Reduction**: Avoided LINQ in hot paths, reused collections
- **Cooldown Systems**: Prevented excessive expensive operations
- **Profiling-Driven Optimization**: Hot paths identified and improved using Unity Profiler

---

## 📁 Project Structure

```
Assets/
├── Game Systems/          # Modular, reusable game systems
│   ├── AudioManager/      # Professional audio system with pooling
│   ├── DialogueSystem/    # Custom dialogue graph editor
│   ├── WalkingImmersion/ # Terrain-aware footstep system
│   ├── Wasp/             # Wasp AI behavior system
│   ├── Ants/             # Ant path following system
│   └── GarbageScanner/   # Debug and profiling tools
├── Scripts/              # Core game scripts
│   ├── GameObjects/      # Game object implementations
│   │   └── FarmField/    # State pattern implementation
│   ├── Extensions/       # C# extension methods
│   └── Enums/           # Type definitions
├── Interface/            # UI systems
├── Graphics/             # Visual assets
├── Audio/               # Sound assets
└── Settings/            # Project configuration
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

### Game Development Expertise
- **Unity Best Practices**: Proper use of Unity systems and APIs
- **Physics Integration**: Rigidbody movement and collision handling
- **Animation Systems**: Mecanim integration with custom controllers
- **Asset Management**: Addressables for scalable content delivery
- **Editor Tools**: Custom editor scripts for workflow improvement

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

---

## 🎓 Learning Outcomes

This project demonstrates proficiency in:
- Advanced Unity game development
- Performance optimization and profiling
- Software architecture and design patterns
- C# advanced features (async/await, generics, extensions)
- System integration and modular design
- Professional game development workflows

---

## 📝 Notes

This project serves as a **portfolio piece** to showcase:
- Advanced Unity development skills
- Performance optimization techniques
- Clean code architecture
- Complex system integration
- Professional game development practices

All code and systems were developed from scratch to demonstrate technical capabilities and understanding of game development principles.

---

## 📄 License

This project is for **portfolio purposes only**. All assets and code are property of the developer.

---

**Built with Unity 6 LTS** | **C#** | **Professional Game Development**

*This repository serves as a technical portfolio demonstrating advanced Unity game development skills and software engineering practices.*
