[English](README.md) | [中文](README.zh-CN.md)

# DOTween Visual Editor

A visual editor for DOTween, providing Unity developers with an intuitive and efficient Tween animation editing experience.

## Features

- **Component-Bound Design** - Data is bound 1:1 to GameObjects, reducing cognitive load
- **Visual Editor** - Modern editor window built with UI Toolkit (`Tools > DOTween Visual Editor`)
- **Real-time Preview** - Instant animation preview in the editor, with pause, replay, and reset support
- **Path Visualization** - DOPath steps display path curves in SceneView in real-time, with draggable waypoint editing
- **Inspector Editing** - Custom PropertyDrawer that conditionally displays fields by animation type, with one-click sync of current values
- **14 Animation Types** - Move, Rotate, Scale, Color, Fade, AnchorMove, SizeDelta, Jump, Punch, Shake, FillAmount, DOPath, Delay, Callback
- **Flexible Orchestration** - Append (sequential), Join (parallel), Insert (point-in-time) execution modes
- **Path Animation** - DOPath supports Linear / CatmullRom / CubicBezier paths with visual waypoint editing
- **Async Await** - `PlayAsync()` returns TweenAwaitable, supporting coroutine yield and UniTask await
- **Component Validation** - Automatically checks if target objects meet the component requirements for each animation type
- **Copy & Paste** - Ctrl+C/V to copy/paste steps, Ctrl+D for quick duplication
- **DOTween Compatible** - Auto-adapts to DOTween Free / Pro versions and TextMeshPro
- **Logging System** - Custom DOTweenLog wrapper; Debug-level logs are automatically stripped in release builds

## Installation

Install via Unity Package Manager from Git URL:

```
https://github.com/cnoom/DOTweenVisualEditor.git
```

## Requirements

- Unity 2021.3 LTS or later
- [DOTween](https://github.com/Demigiant/dotween.git) >= 1.2.765, with ASMDEF files generated in the DOTween Utility Panel

## Quick Start

1. Add the `DOTween Visual Player` component to a GameObject (menu: `DOTween Visual > DOTween Visual Player`)
2. Open the editor window: `Tools > DOTween Visual Editor`
3. Add and configure animation steps in the editor
4. Click "Preview" for instant preview in the editor, or run the game to play the animation

### Path Visualization

When a DOPath step is selected, the path curve is automatically displayed in SceneView:

- **Green sphere** - Start point
- **Yellow spheres** - Path waypoints, draggable for editing
- **White curve** - Full path trajectory (calculated via DOTween's internal Path class, ensuring runtime consistency)
- **Direction arrows** - Movement direction along the path
- **Runtime path** - The "Runtime Path" toggle in the toolbar controls whether paths are displayed during Play Mode (enabled by default)

## Runtime API

```csharp
var player = GetComponent<DOTweenVisualPlayer>();

// Playback control
player.Play();
player.Stop();
player.Pause();
player.Resume();
player.Restart();
player.Complete();

// Async await (supports coroutines and UniTask)
yield return player.PlayAsync();
await player.PlayAsync().ToUniTask();

// Async callback
player.PlayAsync().OnDone(completed =>
{
    // completed: true=finished normally, false=aborted
});

// Chained callbacks
player.OnStart(() => Debug.Log("Started"))
      .OnComplete(() => Debug.Log("Completed"))
      .OnDone(completed => Debug.Log($"Done: {completed}"))
      .Play();
```

## Project Structure

```
Runtime/
├── Components/
│   ├── DOTweenVisualPlayer.cs     # Main player component
│   └── TweenAwaitable.cs          # Async await wrapper
└── Data/
    ├── TweenStepData.cs           # Animation step data
    ├── TweenStepType.cs           # Animation type enum
    ├── ExecutionMode.cs           # Execution mode enum
    ├── TransformTarget.cs         # MoveSpace/RotateSpace/PunchTarget/ShakeTarget enums
    ├── TweenFactory.cs            # Tween creation factory
    ├── TweenStepRequirement.cs    # Component requirement validation
    ├── TweenValueHelper.cs        # Value access utilities
    └── DOTweenLog.cs              # Logging system

Editor/
├── DOTweenVisualEditorWindow.cs   # Visual editor main window
├── DOTweenPreviewManager.cs       # Preview state manager
├── PathVisualizer.cs              # SceneView path visualizer
├── StepListController.cs          # Step list controller
├── StepDetailPanel.cs             # Step detail panel controller
├── StepClipboard.cs               # Copy/paste manager
├── DetailFieldFactory.cs          # Detail field factory
├── DOTweenEditorStyle.cs          # Style configuration
├── L10n.cs                        # Localization manager
├── TweenStepDataDrawer.cs         # Inspector property drawer
└── USS/
    └── DOTweenVisualEditor.uss    # Editor stylesheet
```

## Acknowledgements

This project is built on top of [DOTween](https://github.com/Demigiant/dotween.git) by Daniele Giardini (Demigiant). Many thanks to the DOTween project for providing a powerful and easy-to-use tween animation engine for Unity.

## License

[MIT License](LICENSE.md)
