#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace CNoom.DOTweenVisual.Editor
{
    /// <summary>
    /// 多语言管理器 - 支持 zh-CN / en-US
    /// 通过 EditorPrefs 持久化语言选择
    /// </summary>
    internal static class L10n
    {
        public enum Language
        {
            ZhCN,
            EnUS
        }

        private const string LanguageKey = "DOTweenVisualEditor_Language";
        private static Language _current;
        private static Dictionary<string, string> _dict;

        public static Language Current
        {
            get => _current;
            set
            {
                if (_current == value) return;
                _current = value;
                EditorPrefs.SetInt(LanguageKey, (int)value);
                _dict = null; // 强制重建字典
            }
        }

        static L10n()
        {
            _current = (Language)EditorPrefs.GetInt(LanguageKey, (int)Language.ZhCN);
        }

        /// <summary>
        /// 获取本地化文本，key 格式为 "Group/Key"
        /// </summary>
        public static string Tr(string key)
        {
            _dict ??= BuildDictionary();
            return _dict.TryGetValue(key, out var value) ? value : key;
        }

        private static Dictionary<string, string> BuildDictionary()
        {
            return _current == Language.ZhCN ? BuildZhCN() : BuildEnUS();
        }

        #region 中文

        private static Dictionary<string, string> BuildZhCN()
        {
            return new Dictionary<string, string>
            {
                // === Window ===
                {"Window/Title", "DOTween Visual Editor"},
                {"Window/TargetLabel", "目标物体:"},
                {"Window/ShowPathInPlayMode", "运行时路径"},
                {"Window/ShowPathTooltip", "Play Mode 下是否显示路径可视化"},
                {"Window/Preview", "预览"},
                {"Window/Stop", "停止"},
                {"Window/Replay", "重播"},
                {"Window/Reset", "重置"},
                {"Window/Pause", "暂停"},
                {"Window/Continue", "继续"},
                {"Window/StateNone", "● 未播放"},
                {"Window/StatePlaying", "● 播放中"},
                {"Window/StatePaused", "● 已暂停"},
                {"Window/StateCompleted", "● 播放完成"},
                {"Window/StepOverview", "步骤概览"},
                {"Window/AddStep", "＋ 添加"},
                {"Window/StepDetail", "步骤详情"},
                {"Window/SyncValue", "同步当前值"},
                {"Window/StyleSheetError", "样式表加载失败！请检查 Editor/USS/DOTweenVisualEditor.uss 是否存在且已被 Unity 导入"},
                {"Window/NoTargetWarning", "请先选择一个 DOTweenVisualPlayer 组件"},

                // === Add Step Menu ===
                {"Menu/MoveWorld", "Move (World)"},
                {"Menu/MoveLocal", "Move (Local)"},
                {"Menu/RotateWorld", "Rotate (World)"},
                {"Menu/RotateLocal", "Rotate (Local)"},
                {"Menu/Scale", "Scale"},
                {"Menu/Color", "Color"},
                {"Menu/Fade", "Fade"},
                {"Menu/AnchorMove", "Anchor Move"},
                {"Menu/SizeDelta", "Size Delta"},
                {"Menu/Jump", "Jump"},
                {"Menu/PunchPosition", "Punch (Position)"},
                {"Menu/PunchRotation", "Punch (Rotation)"},
                {"Menu/PunchScale", "Punch (Scale)"},
                {"Menu/ShakePosition", "Shake (Position)"},
                {"Menu/ShakeRotation", "Shake (Rotation)"},
                {"Menu/ShakeScale", "Shake (Scale)"},
                {"Menu/FillAmount", "Fill Amount"},
                {"Menu/DOPath", "DOPath (路径移动)"},
                {"Menu/Delay", "Delay"},
                {"Menu/Callback", "Callback"},

                // === Undo ===
                {"Undo/AddStep", "添加动画步骤"},
                {"Undo/DeleteStep", "删除动画步骤"},
                {"Undo/ReorderStep", "调整步骤顺序"},
                {"Undo/ModifyWaypoint", "修改路径点"},
                {"Undo/AddWaypoint", "添加路径点"},
                {"Undo/DeleteWaypoint", "删除路径点"},
                {"Undo/SyncValue", "同步当前值"},
                {"Undo/PasteStep", "粘贴动画步骤"},
                {"Undo/MoveWaypoint", "移动路径点"},
                {"Undo/ResetPreviewState", "重置预览状态"},

                // === Player Settings ===
                {"Settings/Title", "播放设置"},
                {"Settings/PlayTrigger", "播放触发"},
                {"Settings/DisableAction", "禁用行为"},
                {"Settings/Loops", "循环次数"},
                {"Settings/LoopType", "循环类型"},
                {"Settings/DebugMode", "调试模式"},

                // === Detail Panel ===
                {"Detail/SelectStep", "请在左侧选择一个步骤"},
                {"Detail/Type", "类型"},
                {"Detail/Enabled", "启用"},
                {"Detail/Duration", "时长"},
                {"Detail/Delay", "延迟"},
                {"Detail/Ease", "缓动"},
                {"Detail/Loops", "循环次数"},
                {"Detail/LoopType", "循环类型"},
                {"Detail/ExecutionMode", "执行模式"},
                {"Detail/MoveSpace", "坐标空间"},
                {"Detail/RotateSpace", "坐标空间"},
                {"Detail/RotateDirection", "旋转方向"},
                {"Detail/TargetObject", "目标物体"},
                {"Detail/RelativeMode", "相对模式"},
                {"Detail/UseStartValue", "使用起始值"},
                {"Detail/StartRotation", "起始旋转"},
                {"Detail/StartRotationEuler", "起始旋转 (欧拉角)"},
                {"Detail/TargetRotationEuler", "目标旋转 (欧拉角)"},
                {"Detail/StartAnchorPos", "起始锚点位置"},
                {"Detail/TargetAnchorPos", "目标锚点位置"},
                {"Detail/StartSize", "起始尺寸"},
                {"Detail/TargetSize", "目标尺寸"},
                {"Detail/StartValue", "起始值"},
                {"Detail/TargetValue", "目标值"},
                {"Detail/TargetValueEuler", "目标值 (欧拉角)"},
                {"Detail/UseStartColor", "使用起始颜色"},
                {"Detail/StartColor", "起始颜色"},
                {"Detail/TargetColor", "目标颜色"},
                {"Detail/UseStartAlpha", "使用起始透明度"},
                {"Detail/StartAlpha", "起始透明度"},
                {"Detail/TargetAlpha", "目标透明度"},
                {"Detail/UseStartFillAmount", "使用起始填充量"},
                {"Detail/StartFillAmount", "起始填充量"},
                {"Detail/TargetFillAmount", "目标填充量"},
                {"Detail/UseStartPosition", "使用起始位置"},
                {"Detail/StartPosition", "起始位置"},
                {"Detail/TargetPosition", "目标位置"},
                {"Detail/JumpHeight", "跳跃高度"},
                {"Detail/JumpCount", "跳跃次数"},
                {"Detail/PunchTarget", "冲击目标"},
                {"Detail/ShakeTarget", "震动目标"},
                {"Detail/Intensity", "强度"},
                {"Detail/Vibrato", "震荡次数"},
                {"Detail/Elasticity", "弹性"},
                {"Detail/ShakeRandomness", "随机性"},
                {"Detail/PathType", "路径类型"},
                {"Detail/PathMode", "路径模式"},
                {"Detail/PathResolution", "路径分辨率"},
                {"Detail/Waypoints", "路径点"},
                {"Detail/Point", "点"},
                {"Detail/WaypointCount", "个"},
                {"Detail/AddWaypoint", "+ 添加路径点"},
                {"Detail/RemoveLastWaypoint", "- 删除最后一个路径点"},
                {"Detail/MinWaypointsWarning", "至少需要保留一个路径点"},
                {"Detail/CallbackEvent", "回调事件"},
                {"Detail/InsertTime", "插入时间"},
                {"Detail/UseCustomCurve", "使用自定义曲线"},
                {"Detail/CustomCurve", "自定义曲线"},
                {"Detail/OnStart", "OnStart"},
                {"Detail/OnComplete", "OnComplete"},
                {"Detail/SyncTooltip", "从物体当前状态同步该值"},
                {"Detail/SyncWaypointTooltip", "同步到物体当前位置"},

                // === Path Options ===
                {"PathOption/Linear", "Linear (直线)"},
                {"PathOption/CatmullRom", "CatmullRom (曲线)"},
                {"PathOption/CubicBezier", "CubicBezier (贝塞尔)"},
                {"PathOption/3D", "3D (三维)"},
                {"PathOption/TopDown2D", "TopDown2D (俯视)"},
                {"PathOption/SideScroll2D", "SideScroll2D (横版)"},

                // === Step List ===
                {"StepList/NoTarget", "未指定"},
                {"StepList/StepFormat", "步骤 {0}"},

                // === Inspector Drawer ===
                {"Drawer/Enabled", " 启用"},
                {"Drawer/Enable", " 启用"},
                {"Drawer/UseStartValue", " 使用起始值"},
                {"Drawer/SyncCurrent", "同步当前值"},
                {"Drawer/SyncCurrentPos", "同步当前位置"},
                {"Drawer/UseStartColor", " 使用起始颜色"},
                {"Drawer/UseStartAlpha", " 使用起始透明度"},
                {"Drawer/UseStartFillAmount", " 使用起始填充量"},
                {"Drawer/DelayTime", "延迟时间"},
                {"Drawer/UseStartPosition", " 使用起始位置"},
                {"Drawer/StartPosition", "起始位置"},
                {"Drawer/TargetPosition", "目标位置"},
                {"Drawer/JumpHeight", "跳跃高度"},
                {"Drawer/JumpCount", "跳跃次数"},
                {"Drawer/PunchTarget", "冲击目标"},
                {"Drawer/ShakeTarget", "震动目标"},
                {"Drawer/Intensity", "强度"},
                {"Drawer/Vibrato", "震荡次数"},
                {"Drawer/Elasticity", "弹性"},
                {"Drawer/ShakeRandomness", "随机性"},
                {"Drawer/WaypointCount", "路径点 ({0} 个)"},
                {"Drawer/Waypoint", "点"},
                {"Drawer/UseStartFill", " 使用起始填充量"},
                {"Drawer/StartFill", "起始填充量"},
                {"Drawer/TargetFill", "目标填充量"},
                {"Drawer/Point", "点 {0}"},
                {"Drawer/CallbackEvent", "回调事件"},
                {"Drawer/CannotGetTarget", "无法获取目标物体"},
                {"Drawer/Synced", "已同步 {0} 的 {1} = {2}"},

                // === Path Visualizer ===
                {"Path/ReflectionError", "[DOTweenVisualEditor] 路径可视化反射调用失败，当前 DOTween 版本可能不兼容。\n请检查 DOTween 版本是否满足要求（≥1.2.0），或联系插件作者更新。\n{0}"},
                {"Path/VersionWarning", "⚠ DOTween 版本不兼容，路径可视化不可用"},
                {"Path/VersionDetail", "请检查 DOTween 版本 ≥ 1.2.0"},

                // === Preview ===
                {"Preview/StartFailed", "预览启动失败: {0}\n{1}"},

                // === Clipboard ===
                {"Clipboard/Copied", "已复制步骤 {0}"},
                {"Clipboard/Empty", "剪贴板为空，请先复制一个步骤"},
                {"Clipboard/Pasted", "已粘贴步骤"},
                {"Clipboard/PasteFailed", "粘贴步骤失败：{0}"},

                // === Lifecycle ===
                {"Lifecycle/PlayTrigger", "播放触发"},
                {"Lifecycle/Manual", "手动"},
                {"Lifecycle/OnAwake", "Awake 时播放"},
                {"Lifecycle/OnStart", "Start 时播放"},
                {"Lifecycle/OnEnableRestart", "Enable 时重新播放"},
                {"Lifecycle/OnEnableResume", "Enable 时继续播放"},
                {"Lifecycle/DisableAction", "禁用时行为"},
                {"Lifecycle/Pause", "暂停"},
                {"Lifecycle/Stop", "停止"},
                {"Lifecycle/None", "无操作"},

                // === Language Menu ===
                {"Menu/Language", "语言/Language"},
                {"Menu/Chinese", "中文 (简体)"},
                {"Menu/English", "English"},

                // === Help ===
                {"Help/Button", "?"},
                {"Help/Title", "使用帮助"},
                {"Help/Section_Colors", "按钮颜色约定"},
                {"Help/Color_Sync", "[S] 青色 — 同步按钮：从物体当前状态读取值并填入对应字段"},
                {"Help/Color_Delete", "[X] 红色 — 删除按钮：删除当前项（路径点、步骤等）"},
                {"Help/Color_Add", "[+] 绿色 — 添加按钮：添加新项（路径点等）"},
                {"Help/Section_Shortcuts", "快捷键"},
                {"Help/Shortcut_Copy", "Ctrl+C — 复制当前步骤"},
                {"Help/Shortcut_Paste", "Ctrl+V — 粘贴步骤"},
                {"Help/Shortcut_Delete", "Delete — 删除当前步骤"},
                {"Help/Section_Path", "路径点规则"},
                {"Help/Path_Linear", "Linear / CatmullRom：最少 2 个路径点，每次增减 1 个"},
                {"Help/Path_Bezier", "CubicBezier：最少 3 个路径点，数量须为 3 的倍数，每次增减 3 个"},
            };
        }

        #endregion

        #region English

        private static Dictionary<string, string> BuildEnUS()
        {
            return new Dictionary<string, string>
            {
                // === Window ===
                {"Window/Title", "DOTween Visual Editor"},
                {"Window/TargetLabel", "Target:"},
                {"Window/ShowPathInPlayMode", "Runtime Path"},
                {"Window/ShowPathTooltip", "Show path visualization in Play Mode"},
                {"Window/Preview", "Preview"},
                {"Window/Stop", "Stop"},
                {"Window/Replay", "Replay"},
                {"Window/Reset", "Reset"},
                {"Window/Pause", "Pause"},
                {"Window/Continue", "Resume"},
                {"Window/StateNone", "● Idle"},
                {"Window/StatePlaying", "● Playing"},
                {"Window/StatePaused", "● Paused"},
                {"Window/StateCompleted", "● Completed"},
                {"Window/StepOverview", "Step Overview"},
                {"Window/AddStep", "＋ Add"},
                {"Window/StepDetail", "Step Detail"},
                {"Window/SyncValue", "Sync Current"},
                {"Window/StyleSheetError", "Style sheet loading failed! Check Editor/USS/DOTweenVisualEditor.uss"},
                {"Window/NoTargetWarning", "Please select a DOTweenVisualPlayer component first"},

                // === Add Step Menu ===
                {"Menu/MoveWorld", "Move (World)"},
                {"Menu/MoveLocal", "Move (Local)"},
                {"Menu/RotateWorld", "Rotate (World)"},
                {"Menu/RotateLocal", "Rotate (Local)"},
                {"Menu/Scale", "Scale"},
                {"Menu/Color", "Color"},
                {"Menu/Fade", "Fade"},
                {"Menu/AnchorMove", "Anchor Move"},
                {"Menu/SizeDelta", "Size Delta"},
                {"Menu/Jump", "Jump"},
                {"Menu/PunchPosition", "Punch (Position)"},
                {"Menu/PunchRotation", "Punch (Rotation)"},
                {"Menu/PunchScale", "Punch (Scale)"},
                {"Menu/ShakePosition", "Shake (Position)"},
                {"Menu/ShakeRotation", "Shake (Rotation)"},
                {"Menu/ShakeScale", "Shake (Scale)"},
                {"Menu/FillAmount", "Fill Amount"},
                {"Menu/DOPath", "DOPath (Path Move)"},
                {"Menu/Delay", "Delay"},
                {"Menu/Callback", "Callback"},

                // === Undo ===
                {"Undo/AddStep", "Add Tween Step"},
                {"Undo/DeleteStep", "Delete Tween Step"},
                {"Undo/ReorderStep", "Reorder Steps"},
                {"Undo/ModifyWaypoint", "Modify Waypoint"},
                {"Undo/AddWaypoint", "Add Waypoint"},
                {"Undo/DeleteWaypoint", "Delete Waypoint"},
                {"Undo/SyncValue", "Sync Current Value"},
                {"Undo/PasteStep", "Paste Tween Step"},
                {"Undo/MoveWaypoint", "Move Waypoint"},
                {"Undo/ResetPreviewState", "Reset Preview State"},

                // === Player Settings ===
                {"Settings/Title", "Player Settings"},
                {"Settings/PlayTrigger", "Play Trigger"},
                {"Settings/DisableAction", "On Disable"},
                {"Settings/Loops", "Loops"},
                {"Settings/LoopType", "Loop Type"},
                {"Settings/DebugMode", "Debug Mode"},

                // === Detail Panel ===
                {"Detail/SelectStep", "Select a step on the left"},
                {"Detail/Type", "Type"},
                {"Detail/Enabled", "Enabled"},
                {"Detail/Duration", "Duration"},
                {"Detail/Delay", "Delay"},
                {"Detail/Ease", "Ease"},
                {"Detail/Loops", "Loops"},
                {"Detail/LoopType", "Loop Type"},
                {"Detail/ExecutionMode", "Execution Mode"},
                {"Detail/MoveSpace", "Space"},
                {"Detail/RotateSpace", "Space"},
                {"Detail/RotateDirection", "Rotate Direction"},
                {"Detail/TargetObject", "Target Object"},
                {"Detail/RelativeMode", "Relative"},
                {"Detail/UseStartValue", "Use Start Value"},
                {"Detail/StartRotation", "Start Rotation"},
                {"Detail/StartRotationEuler", "Start Rotation (Euler)"},
                {"Detail/TargetRotationEuler", "Target Rotation (Euler)"},
                {"Detail/StartAnchorPos", "Start Anchor Pos"},
                {"Detail/TargetAnchorPos", "Target Anchor Pos"},
                {"Detail/StartSize", "Start Size"},
                {"Detail/TargetSize", "Target Size"},
                {"Detail/StartValue", "Start Value"},
                {"Detail/TargetValue", "Target Value"},
                {"Detail/TargetValueEuler", "Target Value (Euler)"},
                {"Detail/UseStartColor", "Use Start Color"},
                {"Detail/StartColor", "Start Color"},
                {"Detail/TargetColor", "Target Color"},
                {"Detail/UseStartAlpha", "Use Start Alpha"},
                {"Detail/StartAlpha", "Start Alpha"},
                {"Detail/TargetAlpha", "Target Alpha"},
                {"Detail/UseStartFillAmount", "Use Start Fill"},
                {"Detail/StartFillAmount", "Start Fill"},
                {"Detail/TargetFillAmount", "Target Fill"},
                {"Detail/UseStartPosition", "Use Start Position"},
                {"Detail/StartPosition", "Start Position"},
                {"Detail/TargetPosition", "Target Position"},
                {"Detail/JumpHeight", "Jump Height"},
                {"Detail/JumpCount", "Jump Count"},
                {"Detail/PunchTarget", "Punch Target"},
                {"Detail/ShakeTarget", "Shake Target"},
                {"Detail/Intensity", "Intensity"},
                {"Detail/Vibrato", "Vibrato"},
                {"Detail/Elasticity", "Elasticity"},
                {"Detail/ShakeRandomness", "Randomness"},
                {"Detail/PathType", "Path Type"},
                {"Detail/PathMode", "Path Mode"},
                {"Detail/PathResolution", "Path Resolution"},
                {"Detail/Waypoints", "Waypoints"},
                {"Detail/Point", "Point"},
                {"Detail/WaypointCount", ""},
                {"Detail/AddWaypoint", "+ Add Waypoint"},
                {"Detail/RemoveLastWaypoint", "- Remove Last Waypoint"},
                {"Detail/MinWaypointsWarning", "At least one waypoint is required"},
                {"Detail/CallbackEvent", "Callback Event"},
                {"Detail/InsertTime", "Insert Time"},
                {"Detail/UseCustomCurve", "Use Custom Curve"},
                {"Detail/CustomCurve", "Custom Curve"},
                {"Detail/OnStart", "OnStart"},
                {"Detail/OnComplete", "OnComplete"},
                {"Detail/SyncTooltip", "Sync this value from object's current state"},
                {"Detail/SyncWaypointTooltip", "Sync to object's current position"},

                // === Path Options ===
                {"PathOption/Linear", "Linear"},
                {"PathOption/CatmullRom", "CatmullRom"},
                {"PathOption/CubicBezier", "CubicBezier"},
                {"PathOption/3D", "3D"},
                {"PathOption/TopDown2D", "TopDown2D"},
                {"PathOption/SideScroll2D", "SideScroll2D"},

                // === Step List ===
                {"StepList/NoTarget", "Not Set"},
                {"StepList/StepFormat", "Step {0}"},

                // === Inspector Drawer ===
                {"Drawer/Enable", " Enable"},
                {"Drawer/Enabled", " Enable"},
                {"Drawer/UseStartValue", " Use Start Value"},
                {"Drawer/SyncCurrent", "Sync Current"},
                {"Drawer/SyncCurrentPos", "Sync Current Position"},
                {"Drawer/UseStartColor", " Use Start Color"},
                {"Drawer/UseStartAlpha", " Use Start Alpha"},
                {"Drawer/UseStartFillAmount", " Use Start Fill"},
                {"Drawer/DelayTime", "Delay Time"},
                {"Drawer/UseStartPosition", " Use Start Position"},
                {"Drawer/StartPosition", "Start Position"},
                {"Drawer/TargetPosition", "Target Position"},
                {"Drawer/JumpHeight", "Jump Height"},
                {"Drawer/JumpCount", "Jump Count"},
                {"Drawer/PunchTarget", "Punch Target"},
                {"Drawer/ShakeTarget", "Shake Target"},
                {"Drawer/Intensity", "Intensity"},
                {"Drawer/Vibrato", "Vibrato"},
                {"Drawer/Elasticity", "Elasticity"},
                {"Drawer/ShakeRandomness", "Randomness"},
                {"Drawer/WaypointCount", "Waypoints ({0})"},
                {"Drawer/Waypoint", "Point"},
                {"Drawer/UseStartFill", " Use Start Fill"},
                {"Drawer/StartFill", "Start Fill"},
                {"Drawer/TargetFill", "Target Fill"},
                {"Drawer/Point", "Point {0}"},
                {"Drawer/CallbackEvent", "Callback Event"},
                {"Drawer/CannotGetTarget", "Cannot get target object"},
                {"Drawer/Synced", "Synced {0}'s {1} = {2}"},

                // === Path Visualizer ===
                {"Path/ReflectionError", "[DOTweenVisualEditor] Path visualization reflection failed. Current DOTween version may be incompatible.\nPlease check DOTween version ≥ 1.2.0 or contact the plugin author.\n{0}"},
                {"Path/VersionWarning", "⚠ DOTween version incompatible, path visualization unavailable"},
                {"Path/VersionDetail", "Please check DOTween version ≥ 1.2.0"},

                // === Preview ===
                {"Preview/StartFailed", "Preview failed: {0}\n{1}"},

                // === Clipboard ===
                {"Clipboard/Copied", "Copied step {0}"},
                {"Clipboard/Empty", "Clipboard is empty, copy a step first"},
                {"Clipboard/Pasted", "Step pasted"},
                {"Clipboard/PasteFailed", "Paste step failed: {0}"},

                // === Lifecycle ===
                {"Lifecycle/PlayTrigger", "Play Trigger"},
                {"Lifecycle/Manual", "Manual"},
                {"Lifecycle/OnAwake", "On Awake"},
                {"Lifecycle/OnStart", "On Start"},
                {"Lifecycle/OnEnableRestart", "On Enable (Restart)"},
                {"Lifecycle/OnEnableResume", "On Enable (Resume)"},
                {"Lifecycle/DisableAction", "On Disable"},
                {"Lifecycle/Pause", "Pause"},
                {"Lifecycle/Stop", "Stop"},
                {"Lifecycle/None", "None"},

                // === Language Menu ===
                {"Menu/Language", "语言/Language"},
                {"Menu/Chinese", "中文 (简体)"},
                {"Menu/English", "English"},

                // === Help ===
                {"Help/Button", "?"},
                {"Help/Title", "Help"},
                {"Help/Section_Colors", "Button Color Convention"},
                {"Help/Color_Sync", "[S] Cyan — Sync: read current value from object and fill the field"},
                {"Help/Color_Delete", "[X] Red — Delete: remove the item (waypoint, step, etc.)"},
                {"Help/Color_Add", "[+] Green — Add: add a new item (waypoint, etc.)"},
                {"Help/Section_Shortcuts", "Shortcuts"},
                {"Help/Shortcut_Copy", "Ctrl+C — Copy current step"},
                {"Help/Shortcut_Paste", "Ctrl+V — Paste step"},
                {"Help/Shortcut_Delete", "Delete — Delete current step"},
                {"Help/Section_Path", "Waypoint Rules"},
                {"Help/Path_Linear", "Linear / CatmullRom: minimum 2 waypoints, add/remove 1 at a time"},
                {"Help/Path_Bezier", "CubicBezier: minimum 3 waypoints, count must be multiple of 3, add/remove 3 at a time"},
            };
        }

        #endregion
    }
}
#endif
