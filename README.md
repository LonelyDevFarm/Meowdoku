# Meowdoku

> A mobile logic puzzle game built with Unity, combining color-region Queens rules with progression, daily challenges, streaks, and polished mobile interactions.

## About

**Meowdoku** is a 2D mobile logic puzzle game inspired by the Queens puzzle format.

Players place cats on a colored grid while following four rules:

- Each row contains exactly one cat.
- Each column contains exactly one cat.
- Each colored region contains exactly one cat.
- Cats cannot touch diagonally in adjacent cells.

Beyond the core puzzle, Meowdoku includes progression, daily challenges, streaks, logical hints, profile customization, localization, persistent game state, and an offline rank activity.

## Key Features

- **Queens-style puzzle gameplay** with row, column, region, and diagonal constraints.
- **Mobile-first controls** with tap, double-tap, swipe, and multi-cell stroke interaction.
- **Logical hint system** with contextual board highlighting.
- **Score & combo system** with lives, penalties, multipliers, and animated feedback.
- **Progression & puzzle banks** with multiple sizes, tiers, and level selection.
- **Daily Challenge & Streak** with calendar progression and reward flows.
- **Interactive onboarding** with tutorial and animated How to Play pages.
- **Profile & Rank activity** with avatar/frame customization and simulated competitors.
- **Localization** with runtime language switching and a CSV-driven translation system.
- **Persistent sessions** supporting progression, settings, statistics, and gameplay resume.

## Technical Highlights

### Puzzle Architecture
Core puzzle rules and session state are separated from the Unity presentation layer.  
`QueendokuCore` handles board rules and conflicts, while `GameSession` manages player actions, score, lives, history, and session state.

### Mobile Input
A custom board-level input pipeline handles tap, double-tap, and swipe gestures with direction/velocity guards and stroke interpolation for responsive mobile interaction.

### Hint Engine
The hint system analyzes the current puzzle state and generates structured logical hints with contextual cell, row, column, and region highlighting instead of simply revealing an answer.

### Persistence & UI
Game progress, settings, daily/streak state, profile data, rank activity, and resumable sessions are stored through authenticated encrypted local persistence.

The game uses a registry-driven UI system for page/popup lifecycle, navigation, caching, input guards, and application startup flow.

## Tech Stack

- **Unity 6000.3.19f1**
- **C#**
- **Universal Render Pipeline / Unity 2D**
- **Unity uGUI**
- **Unity Input System**
- **DOTween**
- **Unity Test Framework**
- **ScriptableObject**
- **Custom CSV Localization**
- **Android JNI**

## Project Structure

```text
Assets/_Project/
├── Scripts/
│   ├── Core/          # Puzzle logic, state, persistence and app systems
│   ├── Gameplay/      # Gameplay flow, views, presenters and input
│   └── Services/      # Shared runtime services
├── Prefabs/UI/        # Pages, popups and reusable UI
├── Resources/Levels/  # Puzzle-bank data
├── Scenes/            # Application scenes
├── Settings/          # Runtime catalogs and configuration
├── Localization/      # Translation data
└── Tests/             # EditMode and PlayMode tests

## Platform

**Android**

- ARM64
- Minimum Android API Level 25
- Portrait-oriented mobile UI
- Safe-area-aware layout
- Native Android vibration support

## Status

**Playable Android portfolio project — final validation and presentation in progress.**

Current focus:

- Android device validation
- Repository documentation
- Gameplay showcase preparation