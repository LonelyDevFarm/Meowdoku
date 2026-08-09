# R1 EditMode Test Report

- Date: 2026-08-08
- Unity: 6000.3.19f1
- Runtime used: Unity MonoBleedingEdge
- Result: **67 passed, 0 failed**

## Scope

- `CellStateTests`
- `GameScoreModelTests`
- `LevelDataTests`
- `QueendokuCoreTests`
- `BoardGestureRecognizerTests`
- `SwipeAxisGuardTests`
- `SwipeVelocityGateTests`
- `MiniJsonTests`
- `SaveStoreTests`
- `GameStateRepositoryTests`

## Verification method

The current Core, Gameplay and EditMode test sources were compiled with Unity's bundled Roslyn compiler against Unity 6000.3 reference assemblies. Tests were executed with Unity's bundled Mono runtime and the project's NUnit framework.

This validates the pure EditMode suite without launching a second Unity Editor against the project while the user's Editor is already open. PlayMode/UI tests still require Unity Test Runner.

## Defect caught during the run

The first run produced 66 passed and 1 failed at the exact swipe velocity threshold `1.2`. The initial C# port used 32-bit `float`, while Godot's GDScript `float` is 64-bit. `SwipeVelocityGate` was corrected to use `double`, after which the full 67-case suite passed.

