# Rabbit vs Mole v2 🐰🕳️

> **Commercial Release & Portfolio Project** | A comprehensive Unity 6 LTS game development showcase demonstrating advanced programming techniques, performance optimization, and professional system architecture.
>
> **Note to Recruiters:** This project is effectively released on Steam as a commercial product. This repository serves as a code portfolio demonstrating the clean, modular, and professional architecture behind the game.

![Unity](https://img.shields.io/badge/Unity-6000.2.12f1-black?style=flat-square&logo=unity)
![C#](https://img.shields.io/badge/C%23-11.0-239120?style=flat-square&logo=c-sharp)
![Steam](https://img.shields.io/badge/Platform-Steam-1b2838?style=flat-square&logo=steam)
![License](https://img.shields.io/badge/License-Portfolio-blue?style=flat-square)

---

## 📋 About This Project

**Rabbit vs Mole v2** is a professional Unity game development portfolio project that demonstrates mastery of advanced game programming concepts, performance optimization techniques, and clean software architecture. This 3D farming game features asymmetric gameplay mechanics, sophisticated AI systems, online multiplayer, and a modular, production-ready codebase.

### 🎯 Project Goals

This project was developed to showcase:
- **Advanced Unity Development**: Deep understanding of Unity's systems and best practices
- **Networking & Multiplayer**: Custom P2P networking implementation using Steamworks
- **Performance Engineering**: Optimized systems with profiling-driven improvements
- **Software Architecture**: Clean, modular, maintainable code following SOLID principles
- **Commercial Readiness**: Integration of Steamworks, Achievements, and production pipelines

---

## 🎮 Game Overview

**Rabbit vs Mole v2** is a 3D farming game featuring asymmetric gameplay between two playable characters:
- **Rabbit**: Farm above ground, plant seeds, water fields, and harvest crops
- **Mole**: Navigate underground with unique tunneling mechanics

The game includes a complete farming cycle, dynamic day-of-week progression, golden carrot collectibles, immersive terrain-based systems, and fully networked co-op gameplay.

---

## 🏗️ Systemy (Systems)

### 🌐 Multiplayer & Networking System
- **Host-Authoritative Architecture**: Custom P2P networking solution built on **Steamworks.NET**.
  - **Host**: Manages game state, validates actions, and broadcasts world updates.
  - **Client**: Sends input/interaction requests and renders state based on Host authority.
- **Optimized Data Transport**: Custom efficiently packed binary protocol for low-bandwidth usage.
- **State Synchronization**:
  - **Position Sync**: Interpolated avatar movement with prediction for smooth visual experience.
  - **World Consistency**: Reliable syncing of farming field states (`Untouched` → `Planted` → `Grown`).
  - **Inventory Management**: Secure server-side inventory tracking synced to clients.
- **Steam Integration**: 
  - **Lobby System**: Custom Lobby Browser and Room Binder (`SteamLobbySession`).
  - **Connection Handling**: Robust handling of P2P connections and disconnections.

### 🏆 Achievements System
- **Event-Driven Architecture**: `AchievementsWatcher` Singleton listens to game events via the Event Bus.
- **Steam Stats Integration**: Direct synchronization with Steam User Stats API.
- **Progress Tracking**: 
  - Persistent tracking of cumulative stats (e.g., "100 Carrots Collected").
  - Interface-based stat progress trackers (`IStatProgressInfo`).
- **Scalable Design**: easy addition of new achievements via `IAchievement` interface implementation.

### 🎮 Player Management System
- **Modular Player Controller Architecture**: Separated base system from game-specific implementations
- **Input Device Manager**: Centralized management of gamepads and keyboards
  - Tracks gamepad usage to prevent device conflicts
  - Automatic device refresh on connection/disconnection
  - Support for split-screen multiplayer with device assignment
- **Agent Controller System**: Supports Human, Bot, and Online player types (`OnlineAgentController`)
- **Player Avatar System**: Customizable avatar stats and speed controllers

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

### 🚶 Advanced Walking Immersion System
- **Dynamic Terrain Surface Detection**: Real-time texture analysis using `GetAlphamaps()`
- **Performance-Optimized Footstep System**:
  - Surface detection result caching (2m distance threshold)
  - Particle system pooling (16 pre-instantiated systems)
  - Color change throttling (avoids expensive ParticleSystem updates)
  - `sqrMagnitude` optimization instead of `Distance` calculations
- **Terrain-Aware Effects**: Footstep particles and sounds adapt to terrain texture

### 💬 Custom Dialogue System
- **Graph-Based Editor**: Visual node editor built with Unity GraphView API
  - Custom node types: Dialogue, Trigger, Logic, Start nodes
  - GUID-based node linking for robust connections
- **Render Texture System**: Dynamic actor rendering to textures for dialogue display
- **Typewriter Effect**: Character-by-character text display with skip functionality

### 🤖 AI Behavior Systems
**Bot AI System:**
- **Action-Based AI**: Modular action system for bot behavior
- **Intelligent Field Selection**: Advanced scoring algorithm for Rabbit AI to select optimal fields based on distance and state.
- **Blackboard System**: Shared data structure for AI decision making
- **Behavior Sequences**: Complex behavior sequences for advanced AI

**Wasp AI Controller:**
- **5-State Behavior System**: FindTarget → GoToTarget → CircleFlower → LandOnFlower → Rest
- **Spiral Pathfinding**: Smooth Catmull-Rom-based spiral descent to flowers

### 🌾 Farming System
- **State Pattern Implementation**: Farm field state machine
  - State flow: `UntouchedField` → `PlantedField` → `GrownField` → `MoundedField` → `RootedField`
  - Interface-based design (`IFarmFieldState`) with clean separation of concerns
- **Watering & Growth**: Coroutine-based interaction validation and timeframe management.

### 💾 Save System
- **Encrypted PlayerPrefs**: Secure data storage with encryption
- **Game Progress Manager**: Status tracking for golden carrots and story progress

### 📡 Event Bus System
- **Type-Safe Event System**: Generic event bus with type-based subscriptions
- **Decoupled Communication**: Allows systems (like Achievements or Audio) to react to game events without direct dependencies.

---

## 🎨 Wzorce Projektowe (Design Patterns)

### ✅ Singleton Pattern
- **Generic Singleton Implementation**: `SingletonMonoBehaviour<T>` with bootstrap integration
- **Used in**: `GameManager`, `AudioManager`, `AchievementsWatcher`

### ✅ State Pattern
- **Farm Field State Machine**: Complete state pattern implementation
- **AI Behavior**: State-based logic for Wasp and Bot AI.

### ✅ Observer Pattern
- **Event Bus**: Type-safe publish-subscribe event system for decoupling systems.

### ✅ Command Pattern
- **Online Interactions**: Encapsulated interaction requests sent over the network.
- **Action System**: Player actions encapsulated for execution and cancellation.

### ✅ Object Pooling Pattern
- **Audio & Particles**: High-performance reuse of frequent objects to eliminate GC spikes.

---

## 🔧 Techniki Programistyczne (Programming Techniques)

### 🚀 Networking & Parallelism
- **Custom Serialization**: Efficient binary writing/reading for network packets.
- **Server Reconciliation**: Client-side prediction with host authority correction.
- **Async/Await**: Extensive use of Task-based async operations for loading (Addressables).

### ⚡ Optymalizacje Wydajności (Performance Optimizations)
- **Memory Management**: Aggressive object pooling and `ConcurrentDictionary` usage.
- **Math Optimizations**: Use of `sqrMagnitude`, pre-computed hashes, and cached values.
- **Profiling-Driven**: Hot paths optimized based on Unity Profiler data.

### 🏛️ Architektura Oprogramowania (Software Architecture)
- **Modular Design**: Systems reside in `GameSystems` namespace, decoupled from gameplay logic.
- **SOLID Principles**: strict adherence to Single Responsibility and Interface Segregation.
- **Separation of Concerns**: Clear distinction between Data (ScriptableObjects), Logic (Controllers), and View (Visuals).

---

## 🛠️ Technical Stack

### Unity Technologies
- **Unity 6.0.2 LTS** (6000.2.12f1)
- **Universal Render Pipeline (URP)**
- **Addressables**
- **Input System** (New)
- **Cinemachine**

### External Libraries
- **Steamworks.NET**: Steam API integration.
- **EasyTransitions**: Scene transition effects.
- **SerializedCollections**: Enhanced dictionary serialization.

---

## 📁 Project Structure

```
Assets/
├── GameSystems/                    # Modular, reusable game systems
│   ├── Steam/                     # Steamworks integration & Networking
│   ├── AudioManager/              # Professional audio system
│   ├── DialogueSystem/            # Custom dialogue graph editor
│   ├── PlayerManagementSystem/    # Input & Player handling
│   └── EventBus/                  # Decoupled event system
├── RabbitVsMole/                  # Game-specific implementations
│   ├── Managers/                  # GameManager, GameInspector
│   ├── PlayerManagementSystem/    # OnlineAgentController, HumanAgentController
│   │   └── AIBehavior/           # Bot AI logic
│   ├── InteractableGameObject/    # Field & Item interactions
│   └── TV/                       # Ending/Story sequences
├── Scripts/                       # Core game logic
└── Scenes/                        # Game scenes
```

---

## 📝 Notes

This project serves as a **portfolio piece** to showcase advanced Unity development skills. While the code is available here for review, the game is a **shipped commercial product on Steam**.

**Built with Unity 6 LTS** | **C#** | **Steamworks**
