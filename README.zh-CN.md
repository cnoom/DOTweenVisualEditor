[English](README.md) | [中文](README.zh-CN.md)

# DOTween Visual Editor

DOTween 可视化编辑器，为 Unity 开发者提供直观、高效的 Tween 动画编辑体验。

## 功能特性

- **组件绑定设计** - 数据与 GameObject 一对一绑定，降低心智负担
- **可视化编辑器** - 基于 UI Toolkit 构建的现代化编辑器窗口（`Tools > DOTween Visual Editor`）
- **实时预览** - 编辑器内即时预览动画效果，支持暂停、重播、重置
- **播放设置面板** - 可视化编辑器中直接配置播放触发、禁用行为、循环次数、循环类型和调试模式
- **路径可视化** - DOPath 步骤在 SceneView 中实时显示路径曲线，支持拖拽编辑路径点
- **Inspector 编辑** - 自定义 PropertyDrawer，按动画类型条件显示字段，支持一键同步当前值
- **14 种动画类型** - Move、Rotate、Scale、Color、Fade、AnchorMove、SizeDelta、Jump、Punch、Shake、FillAmount、DOPath、Delay、Callback
- **灵活编排** - Append（顺序）、Join（并行）、Insert（定点插入）三种执行模式
- **路径动画** - DOPath 支持 Linear / CatmullRom / CubicBezier 路径，可视化编辑路径点
- **生命周期控制** - 可配置的播放触发时机（Manual/OnAwake/OnStart/OnEnableRestart/OnEnableResume）和禁用行为（Pause/Stop/None）
- **异步等待** - `PlayAsync()` 返回 TweenAwaitable，支持协程 yield 和 UniTask await
- **组件校验** - 自动检测目标物体是否满足动画类型的组件需求
- **复制粘贴** - Ctrl+C/V 复制粘贴步骤，Ctrl+D 快速复制
- **DOTween 兼容** - 自动适配 DOTween Free / Pro 版本及 TextMeshPro
- **日志系统** - 自定义 DOTweenLog 封装，Debug 级别发布版本自动移除

## 安装

通过 Unity Package Manager 从 Git URL 安装：

```
https://github.com/cnoom/DOTweenVisualEditor.git
```

## 前置依赖

- Unity 2021.3 LTS 或更高版本
- [DOTween](https://github.com/Demigiant/dotween.git) >= 1.2.765，在 DOTween Utility Panel 中生成 ASMDEF 文件

## 快速开始

1. 在 GameObject 上添加 `DOTween Visual Player` 组件（菜单：`DOTween Visual > DOTween Visual Player`）
2. 打开编辑器窗口：`Tools > DOTween Visual Editor`
3. 在编辑器中添加和配置动画步骤
4. 点击「预览」按钮在编辑器中即时预览，或运行游戏播放动画

### 路径可视化

选中 DOPath 类型的步骤时，SceneView 中会自动显示路径曲线：

- **绿色球体** - 起始点
- **黄色球体** - 路径航点，可直接拖拽编辑
- **白色曲线** - 完整路径轨迹（通过 DOTween 内部 Path 类计算，确保与运行时一致）
- **方向箭头** - 沿路径的运动方向
- **运行时路径** - 工具栏「运行时路径」Toggle 控制 Play Mode 下是否显示路径（默认开启）

## Runtime API

```csharp
var player = GetComponent<DOTweenVisualPlayer>();

// 播放控制
player.Play();
player.Stop();
player.Pause();
player.Resume();
player.Restart();
player.Complete();

// 异步等待（支持协程和 UniTask）
yield return player.PlayAsync();
await player.PlayAsync().ToUniTask();

// 异步回调
player.PlayAsync().OnDone(completed =>
{
    // completed: true=正常完成, false=被终止
});

// 链式回调
player.OnStart(() => Debug.Log("开始"))
      .OnComplete(() => Debug.Log("完成"))
      .OnDone(completed => Debug.Log($"结束: {completed}"))
      .Play();
```

## 项目结构

```
Runtime/
├── Components/
│   ├── DOTweenVisualPlayer.cs     # 主播放器组件
│   └── TweenAwaitable.cs          # 异步等待包装器
└── Data/
    ├── TweenStepData.cs           # 动画步骤数据
    ├── TweenStepType.cs           # 动画类型枚举
    ├── ExecutionMode.cs           # 执行模式枚举
    ├── PlayTrigger.cs             # 播放触发枚举
    ├── DisableAction.cs           # 禁用行为枚举
    ├── TransformTarget.cs         # MoveSpace/RotateSpace/PunchTarget/ShakeTarget 枚举
    ├── TweenFactory.cs            # Tween 创建工厂
    ├── TweenStepRequirement.cs    # 组件需求校验
    ├── TweenValueHelper.cs        # 值访问工具
    └── DOTweenLog.cs              # 日志系统

Editor/
├── DOTweenVisualEditorWindow.cs   # 可视化编辑器主窗口
├── DOTweenPreviewManager.cs       # 预览状态管理器
├── PathVisualizer.cs              # SceneView 路径可视化器
├── StepListController.cs          # 步骤列表控制器
├── StepDetailPanel.cs             # 步骤详情面板控制器
├── StepClipboard.cs               # 复制粘贴管理器
├── DetailFieldFactory.cs          # 详情字段工厂
├── DOTweenEditorStyle.cs          # 样式配置
├── L10n.cs                        # 多语言管理器
├── TweenStepDataDrawer.cs         # Inspector 属性绘制器
└── USS/
    └── DOTweenVisualEditor.uss    # 编辑器样式表
```

## 致谢

本项目基于 [DOTween](https://github.com/Demigiant/dotween.git)（作者 Daniele Giardini / Demigiant）构建。特别感谢 DOTween 项目为 Unity 提供了强大且易用的 Tween 动画引擎。

## 许可证

[MIT License](LICENSE.md)
