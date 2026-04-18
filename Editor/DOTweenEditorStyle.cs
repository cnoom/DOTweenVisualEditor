#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using CNoom.DOTweenVisual.Data;

namespace CNoom.DOTweenVisual.Editor
{
    /// <summary>
    /// 编辑器样式配置
    /// 集中管理颜色常量、CSS 类名、显示名称等样式相关配置
    /// </summary>
    internal static class DOTweenEditorStyle
    {
        #region 常量

        private const string USS_FILE_NAME = "DOTweenVisualEditor.uss";

        #endregion

        #region 显示名称

        /// <summary>
        /// 获取步骤类型的显示名称
        /// </summary>
        public static string GetStepDisplayName(TweenStepType type) => type switch
        {
            TweenStepType.Move => "Move",
            TweenStepType.Rotate => "Rotate",
            TweenStepType.Scale => "Scale",
            TweenStepType.Color => "Color",
            TweenStepType.Fade => "Fade",
            TweenStepType.AnchorMove => "AnchorMove",
            TweenStepType.SizeDelta => "SizeDelta",
            TweenStepType.Jump => "Jump",
            TweenStepType.Punch => "Punch",
            TweenStepType.Shake => "Shake",
            TweenStepType.FillAmount => "FillAmount",
            TweenStepType.Delay => "Delay",
            TweenStepType.Callback => "Callback",
            _ => type.ToString()
        };

        #endregion

        #region 执行模式样式

        /// <summary>
        /// 获取执行模式对应的 CSS 类名
        /// </summary>
        public static string GetExecutionModeCssClass(ExecutionMode mode) => mode switch
        {
            ExecutionMode.Append => "mode-append",
            ExecutionMode.Join => "mode-join",
            ExecutionMode.Insert => "mode-insert",
            _ => "mode-append"
        };

        /// <summary>
        /// 获取执行模式对应的颜色
        /// </summary>
        public static Color GetExecutionModeColor(ExecutionMode mode) => mode switch
        {
            ExecutionMode.Append => new Color(0.29f, 0.56f, 0.85f),  // #4A90D9
            ExecutionMode.Join => new Color(0.29f, 0.85f, 0.29f),    // #4AD94A
            ExecutionMode.Insert => new Color(0.85f, 0.60f, 0.29f),  // #D99A4A
            _ => new Color(0.44f, 0.44f, 0.44f)
        };

        #endregion

#region 资源查找

        /// <summary>
        /// 查找并加载 USS 样式表文件
        /// 搜索所有 StyleSheet 资源，按文件名匹配目标 USS
        /// </summary>
        public static StyleSheet FindStyleSheet()
        {
            var guids = AssetDatabase.FindAssets($"t:StyleSheet");
            foreach (var guid in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                if (path.EndsWith(USS_FILE_NAME))
                {
                    return AssetDatabase.LoadAssetAtPath<StyleSheet>(path);
                }
            }
            return null;
        }

        #endregion
    }
}
#endif
