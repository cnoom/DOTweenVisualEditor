#if UNITY_EDITOR
using System;
using System.Globalization;
using System.Text;
using CNoom.DOTweenVisual.Components;
using CNoom.DOTweenVisual.Data;
using UnityEditor;
using UnityEngine;

namespace CNoom.DOTweenVisual.Editor
{
    /// <summary>
    /// 步骤复制/粘贴管理器
    /// 使用管道分隔的文本格式存储剪贴板数据
    /// </summary>
    internal class StepClipboard
    {
        private static string _clipboardJson;

        private readonly Func<SerializedObject> _getSerializedObject;
        private readonly Func<SerializedProperty> _getStepsProperty;
        private readonly Func<DOTweenVisualPlayer> _getTargetPlayer;
        private readonly Func<int> _getSelectedIndex;
        private readonly Action<int> _setSelectedIndex;
        private readonly Action _onRebuildList;
        private readonly Action _onRefreshDetail;

        public StepClipboard(
            Func<SerializedObject> getSerializedObject,
            Func<SerializedProperty> getStepsProperty,
            Func<DOTweenVisualPlayer> getTargetPlayer,
            Func<int> getSelectedIndex,
            Action<int> setSelectedIndex,
            Action onRebuildList,
            Action onRefreshDetail)
        {
            _getSerializedObject = getSerializedObject;
            _getStepsProperty = getStepsProperty;
            _getTargetPlayer = getTargetPlayer;
            _getSelectedIndex = getSelectedIndex;
            _setSelectedIndex = setSelectedIndex;
            _onRebuildList = onRebuildList;
            _onRefreshDetail = onRefreshDetail;
        }

        /// <summary>
        /// 复制选中的步骤到剪贴板
        /// </summary>
        public void CopySelectedStep()
        {
            int selectedStepIndex = _getSelectedIndex();
            var stepsProperty = _getStepsProperty();
            if (selectedStepIndex < 0 || stepsProperty == null || selectedStepIndex >= stepsProperty.arraySize) return;

            var serializedObject = _getSerializedObject();
            serializedObject?.Update();
            var stepProp = stepsProperty.GetArrayElementAtIndex(selectedStepIndex);
            var sb = new StringBuilder();
            sb.Append(stepProp.FindPropertyRelative("Type").enumValueIndex); sb.Append('|');
            sb.Append(stepProp.FindPropertyRelative("IsEnabled").boolValue ? 1 : 0); sb.Append('|');
            sb.Append(stepProp.FindPropertyRelative("Duration").floatValue); sb.Append('|');
            sb.Append(stepProp.FindPropertyRelative("Delay").floatValue); sb.Append('|');
            sb.Append(stepProp.FindPropertyRelative("Ease").enumValueIndex); sb.Append('|');
            sb.Append(stepProp.FindPropertyRelative("MoveSpace").enumValueIndex); sb.Append('|');
            sb.Append(stepProp.FindPropertyRelative("RotateSpace").enumValueIndex); sb.Append('|');
            sb.Append(stepProp.FindPropertyRelative("RotateDirection").enumValueIndex); sb.Append('|');
            sb.Append(stepProp.FindPropertyRelative("PunchTarget").enumValueIndex); sb.Append('|');
            sb.Append(stepProp.FindPropertyRelative("ShakeTarget").enumValueIndex); sb.Append('|');
            sb.Append(stepProp.FindPropertyRelative("UseStartValue").boolValue ? 1 : 0); sb.Append('|');
            AppendVector3(sb, stepProp.FindPropertyRelative("StartVector").vector3Value); sb.Append('|');
            AppendVector3(sb, stepProp.FindPropertyRelative("TargetVector").vector3Value); sb.Append('|');
            sb.Append(stepProp.FindPropertyRelative("IsRelative").boolValue ? 1 : 0); sb.Append('|');
            sb.Append(stepProp.FindPropertyRelative("UseStartColor").boolValue ? 1 : 0); sb.Append('|');
            AppendColor(sb, stepProp.FindPropertyRelative("StartColor").colorValue); sb.Append('|');
            AppendColor(sb, stepProp.FindPropertyRelative("TargetColor").colorValue); sb.Append('|');
            sb.Append(stepProp.FindPropertyRelative("UseStartFloat").boolValue ? 1 : 0); sb.Append('|');
            sb.Append(stepProp.FindPropertyRelative("StartFloat").floatValue); sb.Append('|');
            sb.Append(stepProp.FindPropertyRelative("TargetFloat").floatValue); sb.Append('|');
            sb.Append(stepProp.FindPropertyRelative("JumpHeight").floatValue); sb.Append('|');
            sb.Append(stepProp.FindPropertyRelative("JumpNum").intValue); sb.Append('|');
            AppendVector3(sb, stepProp.FindPropertyRelative("Intensity").vector3Value); sb.Append('|');
            sb.Append(stepProp.FindPropertyRelative("Vibrato").intValue); sb.Append('|');
            sb.Append(stepProp.FindPropertyRelative("Elasticity").floatValue); sb.Append('|');
            sb.Append(stepProp.FindPropertyRelative("ShakeRandomness").floatValue); sb.Append('|');
            sb.Append(stepProp.FindPropertyRelative("ExecutionMode").enumValueIndex); sb.Append('|');
            sb.Append(stepProp.FindPropertyRelative("InsertTime").floatValue); sb.Append('|');
            sb.Append(stepProp.FindPropertyRelative("UseCustomCurve").boolValue ? 1 : 0); sb.Append('|');
            sb.Append(stepProp.FindPropertyRelative("PathType").intValue); sb.Append('|');
            sb.Append(stepProp.FindPropertyRelative("PathMode").intValue); sb.Append('|');
            sb.Append(stepProp.FindPropertyRelative("PathResolution").intValue);

            // 路径点数据（DOPath 使用）
            var waypointsProp = stepProp.FindPropertyRelative("PathWaypoints");
            sb.Append('|');
            if (waypointsProp != null && waypointsProp.isArray)
            {
                sb.Append(waypointsProp.arraySize);
                for (int w = 0; w < waypointsProp.arraySize; w++)
                {
                    sb.Append(';');
                    AppendVector3(sb, waypointsProp.GetArrayElementAtIndex(w).vector3Value);
                }
            }
            else
            {
                sb.Append('0');
            }

            _clipboardJson = sb.ToString();
            DOTweenLog.Info($"已复制步骤 {selectedStepIndex + 1}");
        }

        /// <summary>
        /// 粘贴剪贴板中的步骤
        /// </summary>
        public void PasteStep()
        {
            if (string.IsNullOrEmpty(_clipboardJson))
            {
                DOTweenLog.Warning("剪贴板为空，请先复制一个步骤");
                return;
            }

            var stepsProperty = _getStepsProperty();
            var targetPlayer = _getTargetPlayer();
            if (stepsProperty == null) return;

            var serializedObject = _getSerializedObject();
            Undo.RecordObject(targetPlayer, "粘贴动画步骤");
            serializedObject?.Update();

            stepsProperty.InsertArrayElementAtIndex(stepsProperty.arraySize);
            var newStep = stepsProperty.GetArrayElementAtIndex(stepsProperty.arraySize - 1);

            var parts = _clipboardJson.Split('|');
            int i = 0;
            newStep.FindPropertyRelative("Type").enumValueIndex = int.Parse(parts[i++], CultureInfo.InvariantCulture);
            newStep.FindPropertyRelative("IsEnabled").boolValue = parts[i++] == "1";
            newStep.FindPropertyRelative("Duration").floatValue = float.Parse(parts[i++], CultureInfo.InvariantCulture);
            newStep.FindPropertyRelative("Delay").floatValue = float.Parse(parts[i++], CultureInfo.InvariantCulture);
            newStep.FindPropertyRelative("Ease").enumValueIndex = int.Parse(parts[i++], CultureInfo.InvariantCulture);
            newStep.FindPropertyRelative("MoveSpace").enumValueIndex = int.Parse(parts[i++], CultureInfo.InvariantCulture);
            newStep.FindPropertyRelative("RotateSpace").enumValueIndex = int.Parse(parts[i++], CultureInfo.InvariantCulture);
            // RotateDirection（兼容旧剪贴板格式）
            if (parts.Length > 30)
                newStep.FindPropertyRelative("RotateDirection").enumValueIndex = int.Parse(parts[i++], CultureInfo.InvariantCulture);
            else
                i++; // skip
            newStep.FindPropertyRelative("PunchTarget").enumValueIndex = int.Parse(parts[i++], CultureInfo.InvariantCulture);
            newStep.FindPropertyRelative("ShakeTarget").enumValueIndex = int.Parse(parts[i++], CultureInfo.InvariantCulture);
            newStep.FindPropertyRelative("UseStartValue").boolValue = parts[i++] == "1";
            newStep.FindPropertyRelative("StartVector").vector3Value = ParseVector3(parts[i++]);
            newStep.FindPropertyRelative("TargetVector").vector3Value = ParseVector3(parts[i++]);
            newStep.FindPropertyRelative("IsRelative").boolValue = parts[i++] == "1";
            newStep.FindPropertyRelative("UseStartColor").boolValue = parts[i++] == "1";
            newStep.FindPropertyRelative("StartColor").colorValue = ParseColor(parts[i++]);
            newStep.FindPropertyRelative("TargetColor").colorValue = ParseColor(parts[i++]);
            newStep.FindPropertyRelative("UseStartFloat").boolValue = parts[i++] == "1";
            newStep.FindPropertyRelative("StartFloat").floatValue = float.Parse(parts[i++], CultureInfo.InvariantCulture);
            newStep.FindPropertyRelative("TargetFloat").floatValue = float.Parse(parts[i++], CultureInfo.InvariantCulture);
            newStep.FindPropertyRelative("JumpHeight").floatValue = float.Parse(parts[i++], CultureInfo.InvariantCulture);
            newStep.FindPropertyRelative("JumpNum").intValue = int.Parse(parts[i++], CultureInfo.InvariantCulture);
            newStep.FindPropertyRelative("Intensity").vector3Value = ParseVector3(parts[i++]);
            newStep.FindPropertyRelative("Vibrato").intValue = int.Parse(parts[i++], CultureInfo.InvariantCulture);
            newStep.FindPropertyRelative("Elasticity").floatValue = float.Parse(parts[i++], CultureInfo.InvariantCulture);
            newStep.FindPropertyRelative("ShakeRandomness").floatValue = float.Parse(parts[i++], CultureInfo.InvariantCulture);
            newStep.FindPropertyRelative("ExecutionMode").enumValueIndex = int.Parse(parts[i++], CultureInfo.InvariantCulture);
            newStep.FindPropertyRelative("InsertTime").floatValue = float.Parse(parts[i++], CultureInfo.InvariantCulture);
            newStep.FindPropertyRelative("UseCustomCurve").boolValue = parts[i++] == "1";
            newStep.FindPropertyRelative("PathType").intValue = int.Parse(parts[i++], CultureInfo.InvariantCulture);
            newStep.FindPropertyRelative("PathMode").intValue = int.Parse(parts[i++], CultureInfo.InvariantCulture);
            newStep.FindPropertyRelative("PathResolution").intValue = int.Parse(parts[i++], CultureInfo.InvariantCulture);

            // 路径点数据（兼容旧剪贴板格式：无路径点数据时跳过）
            if (i < parts.Length)
            {
                // 格式：wpCount;wp1x,wp1y,wp1z;wp2x,wp2y,wp2z;...
                var wpParts = parts[i].Split(';');
                int wpCount = int.Parse(wpParts[0], CultureInfo.InvariantCulture);
                if (wpCount > 0 && wpParts.Length > wpCount)
                {
                    var waypointsProp = newStep.FindPropertyRelative("PathWaypoints");
                    waypointsProp.arraySize = wpCount;
                    for (int w = 0; w < wpCount; w++)
                    {
                        waypointsProp.GetArrayElementAtIndex(w).vector3Value = ParseVector3(wpParts[w + 1]);
                    }
                }
            }

            stepsProperty.serializedObject.ApplyModifiedProperties();
            _setSelectedIndex(stepsProperty.arraySize - 1);
            _onRebuildList();
            _onRefreshDetail();
            DOTweenLog.Info("已粘贴步骤");
        }

        #region 序列化工具

        public static void AppendVector3(StringBuilder sb, Vector3 v)
        {
            sb.Append(v.x.ToString("R", CultureInfo.InvariantCulture));
            sb.Append(',');
            sb.Append(v.y.ToString("R", CultureInfo.InvariantCulture));
            sb.Append(',');
            sb.Append(v.z.ToString("R", CultureInfo.InvariantCulture));
        }

        public static void AppendColor(StringBuilder sb, Color c)
        {
            sb.Append(c.r.ToString("R", CultureInfo.InvariantCulture));
            sb.Append(',');
            sb.Append(c.g.ToString("R", CultureInfo.InvariantCulture));
            sb.Append(',');
            sb.Append(c.b.ToString("R", CultureInfo.InvariantCulture));
            sb.Append(',');
            sb.Append(c.a.ToString("R", CultureInfo.InvariantCulture));
        }

        public static Vector3 ParseVector3(string s)
        {
            var p = s.Split(',');
            return new Vector3(
                float.Parse(p[0], CultureInfo.InvariantCulture),
                float.Parse(p[1], CultureInfo.InvariantCulture),
                float.Parse(p[2], CultureInfo.InvariantCulture));
        }

        public static Color ParseColor(string s)
        {
            var p = s.Split(',');
            return new Color(
                float.Parse(p[0], CultureInfo.InvariantCulture),
                float.Parse(p[1], CultureInfo.InvariantCulture),
                float.Parse(p[2], CultureInfo.InvariantCulture),
                float.Parse(p[3], CultureInfo.InvariantCulture));
        }

        #endregion
    }
}
#endif
