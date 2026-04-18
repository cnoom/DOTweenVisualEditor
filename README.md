# DOTween Visual Editor

DOTween 可视化编辑器，为 Unity 开发者提供直观、高效的 Tween 动画编辑体验。

## 功能特性

- **组件绑定设计** - 数据与 GameObject 一对一绑定，降低心智负担
- **可视化编辑器** - 基于 UI Toolkit 构建的现代化编辑器窗口（`Tools > DOTween Visual Editor`）
- **实时预览** - 编辑器内即时预览动画效果，支持暂停、重播、重置
- **Inspector 编辑** - 自定义 PropertyDrawer，按动画类型条件显示字段，支持一键同步当前值
- **多类型支持** - Move、Rotate、Scale、Color、Fade、AnchorMove、SizeDelta、Jump、Punch、Shake、FillAmount、Delay、Callback
- **灵活编排** - Append（顺序）、Join（并行）、Insert（定点插入）三种执行模式
- **组件校验** - 自动检测目标物体是否满足动画类型的组件需求
- **DOTween 兼容** - 自动适配 DOTween Free / Pro 版本

## 安装

通过 Unity Package Manager 从 Git URL 安装：

```
https://github.com/cnoom/DOTweenVisualEditor.git
```

## 前置依赖

- Unity 2021.3 LTS 或更高版本
- DOTween (com.demigiant.dotween) >= 1.2.765

## 快速开始

1. 在 GameObject 上添加 `DOTween Visual Player` 组件（菜单：`DOTween Visual > DOTween Visual Player`）
2. 打开编辑器窗口：`Tools > DOTween Visual Editor`
3. 在编辑器中添加和配置动画步骤
4. 点击「预览」按钮在编辑器中即时预览，或运行游戏播放动画

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

// 链式回调
player.OnStart(() => Debug.Log("开始"))
      .OnComplete(() => Debug.Log("完成"))
      .Play();
```

## 项目结构

```
Runtime/
├── Components/
│   └── DOTweenVisualPlayer.cs     # 主播放器组件
└── Data/
    ├── TweenStepData.cs           # 动画步骤数据
    ├── TweenStepType.cs           # 动画类型枚举
    ├── TransformTarget.cs         # Transform 目标枚举
    ├── ExecutionMode.cs           # 执行模式枚举
    ├── TweenFactory.cs            # Tween 创建工厂
    ├── TweenStepRequirement.cs    # 组件需求校验
    └── TweenValueHelper.cs        # 值访问工具

Editor/
├── DOTweenVisualEditorWindow.cs   # 可视化编辑器主窗口
├── DOTweenPreviewManager.cs       # 预览状态管理器
├── DOTweenEditorStyle.cs          # 样式配置
├── TweenStepDataDrawer.cs         # Inspector 属性绘制器
└── USS/
    └── DOTweenVisualEditor.uss    # 编辑器样式表
```

## 许可证

[MIT License](LICENSE.md)
