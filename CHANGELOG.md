# 更新日志

本文件记录项目的所有重要变更。

## [1.0.0] - 2026-04-19

### 新增

- **DOTweenVisualPlayer** - 主播放器组件，支持 Play/Stop/Pause/Resume/Restart/Complete API
  - PlayOnStart 自动播放、Loops 循环次数、LoopType 循环类型设置
  - 链式事件回调 API（OnStart / OnComplete / OnUpdate / OnDone）
  - PlayAsync() 异步播放，返回 TweenAwaitable 只读等待包装器
- **TweenAwaitable** - 基于 CustomYieldInstruction 的异步等待包装器
  - 支持协程 yield return 和 UniTask await
  - OnDone(Action<bool>) 完成回调，区分正常完成和被终止
  - IsDone / IsCompleted / IsPlaying / IsActive 状态查询
- **TweenStepData** - 动画步骤数据结构，支持多值组方案
- **TweenStepType** - 14 种动画类型（Move、Rotate、Scale、Color、Fade、AnchorMove、SizeDelta、Jump、Punch、Shake、FillAmount、DOPath、Delay、Callback）
- **ExecutionMode** - Append / Join / Insert 三种执行模式
- **MoveSpace / RotateSpace** - 移动和旋转的坐标空间选择（World / Local）
- **PunchTarget / ShakeTarget** - 冲击和震动的属性目标选择（Position / Rotation / Scale）
- **DOPath 路径动画** - 支持 Linear / CatmullRom / CubicBezier 路径类型，可视化路径点编辑
- **TweenFactory** - 统一的 Tween 创建工厂，消除运行时与编辑器预览的代码重复
- **TweenStepRequirement** - 组件需求校验系统，编辑器中自动提示目标物体是否满足动画要求
- **TweenValueHelper** - 颜色/透明度/RectTransform 等属性的安全读写和动画创建工具
- **DOTweenLog** - 自定义日志系统，支持 Debug/Info/Warning/Error 四级日志，Debug 级别发布版本自动移除
- **DOTweenVisualEditorWindow** - UI Toolkit 可视化编辑器窗口
  - 左侧步骤概览（ListView），支持拖拽排序、启用/禁用、删除
  - 右侧步骤详情面板，按类型条件显示字段
  - 内联时间轴条，可视化每个步骤的时间位置
  - 预览/停止/重播/重置工具栏
  - 状态栏显示播放状态和时间
  - Ctrl+C/V 复制粘贴步骤，Ctrl+D 快速复制
  - 预览进度高亮当前执行步骤
  - Undo/Redo 支持
- **DOTweenPreviewManager** - 编辑器预览状态管理器，支持快照保存与恢复、进度事件通知
- **DOTweenEditorStyle** - 集中管理编辑器样式配置
- **TweenStepDataDrawer** - Inspector 自定义属性绘制器，按类型条件显示字段，支持一键同步当前值
- **DOTweenVisualEditor.uss** - 编辑器暗色主题样式表
- 右键菜单快捷操作（播放/停止/暂停/重播/完成）
- DOTween Free / Pro 版本自动适配（条件编译）
- TextMeshPro 颜色/透明度动画自动适配（条件编译）
