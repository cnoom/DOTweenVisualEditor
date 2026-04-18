# 更新日志

本文件记录项目的所有重要变更。

## [1.0.0] - 2026-04-18

### 新增

- **DOTweenVisualPlayer** - 主播放器组件，支持 Play/Stop/Pause/Resume/Restart/Complete API
- **TweenStepData** - 动画步骤数据结构，支持多值组方案
- **TweenStepType** - 13 种动画类型（Move、Rotate、Scale、Color、Fade、AnchorMove、SizeDelta、Jump、Punch、Shake、FillAmount、Delay、Callback）
- **ExecutionMode** - Append / Join / Insert 三种执行模式
- **TransformTarget** - Position / LocalPosition / Rotation / LocalRotation / Scale 及 Punch/Shake 子类型
- **TweenFactory** - 统一的 Tween 创建工厂，消除运行时与编辑器预览的代码重复
- **TweenStepRequirement** - 组件需求校验系统，编辑器中自动提示目标物体是否满足动画要求
- **TweenValueHelper** - 颜色/透明度/RectTransform 等属性的安全读写和动画创建工具
- **DOTweenVisualEditorWindow** - UI Toolkit 可视化编辑器窗口
  - 左侧步骤概览（ListView），支持拖拽排序、启用/禁用、删除
  - 右侧步骤详情面板，按类型条件显示字段
  - 内联时间轴条，可视化每个步骤的时间位置
  - 预览/停止/重播/重置工具栏
  - 状态栏显示播放状态和时间
- **DOTweenPreviewManager** - 编辑器预览状态管理器，支持快照保存与恢复
- **DOTweenEditorStyle** - 集中管理编辑器样式配置
- **TweenStepDataDrawer** - Inspector 自定义属性绘制器，按类型条件显示字段，支持一键同步当前值
- **DOTweenVisualEditor.uss** - 编辑器暗色主题样式表
- 链式事件回调 API（OnStart / OnComplete / OnUpdate）
- 右键菜单快捷操作（播放/停止/暂停/重播/完成）
- DOTween Free / Pro 版本自动适配（条件编译）
