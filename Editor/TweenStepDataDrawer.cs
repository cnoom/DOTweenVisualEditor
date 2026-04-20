#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using DG.Tweening;
using CNoom.DOTweenVisual.Data;

namespace CNoom.DOTweenVisual.Editor
{
    /// <summary>
    /// TweenStepData 的自定义属性绘制器
    /// 根据 TweenStepType 条件显示对应值组
    /// </summary>
    [CustomPropertyDrawer(typeof(TweenStepData))]
    public class TweenStepDataDrawer : PropertyDrawer
    {
        #region 常量

        private const float LineHeight = 18f;
        private const float Spacing = 2f;
        private const float ButtonHeight = 24f;

        #endregion

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            float height = LineHeight + Spacing; // IsEnabled + Type

            var type = (TweenStepType)property.FindPropertyRelative("Type").enumValueIndex;

            switch (type)
            {
                case TweenStepType.Move:
                case TweenStepType.Rotate:
                case TweenStepType.Scale:
                case TweenStepType.AnchorMove:
                case TweenStepType.SizeDelta:
                    height += GetTransformFieldsHeight(property, type);
                    height += GetCommonFieldsHeight(property);
                    break;

                case TweenStepType.Color:
                    height += GetColorFieldsHeight(property);
                    height += GetCommonFieldsHeight(property);
                    break;

                case TweenStepType.Fade:
                case TweenStepType.FillAmount:
                    height += GetFadeFieldsHeight(property);
                    height += GetCommonFieldsHeight(property);
                    break;

                case TweenStepType.Jump:
                    height += GetJumpFieldsHeight(property);
                    height += GetCommonFieldsHeight(property);
                    break;

                case TweenStepType.Punch:
                case TweenStepType.Shake:
                    height += GetPunchShakeFieldsHeight(property, type);
                    height += GetCommonFieldsHeight(property, skipEase: true);
                    break;

                case TweenStepType.DOPath:
                    height += GetDOPathFieldsHeight(property);
                    height += GetCommonFieldsHeight(property);
                    break;

                case TweenStepType.Delay:
                    height += GetDelayFieldsHeight();
                    break;

                case TweenStepType.Callback:
                    height += GetCallbackFieldsHeight(property);
                    break;
            }

            return height;
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);

            var rect = new Rect(position.x, position.y, position.width, LineHeight);

            // 启用开关
            var isEnabledProp = property.FindPropertyRelative("IsEnabled");
            isEnabledProp.boolValue = EditorGUI.ToggleLeft(rect, L10n.Tr("Drawer/Enabled"), isEnabledProp.boolValue);
            rect.y += LineHeight + Spacing;

            // 类型选择
            var typeProp = property.FindPropertyRelative("Type");
            EditorGUI.PropertyField(rect, typeProp);
            rect.y += LineHeight + Spacing;

            var type = (TweenStepType)typeProp.enumValueIndex;

            switch (type)
            {
                case TweenStepType.Move:
                case TweenStepType.Rotate:
                case TweenStepType.Scale:
                case TweenStepType.AnchorMove:
                case TweenStepType.SizeDelta:
                    DrawTransformFields(ref rect, property, type);
                    DrawCommonFields(ref rect, property);
                    break;

                case TweenStepType.Color:
                    DrawColorFields(ref rect, property);
                    DrawCommonFields(ref rect, property);
                    break;

                case TweenStepType.Fade:
                case TweenStepType.FillAmount:
                    DrawFadeFields(ref rect, property, type);
                    DrawCommonFields(ref rect, property);
                    break;

                case TweenStepType.Jump:
                    DrawJumpFields(ref rect, property);
                    DrawCommonFields(ref rect, property);
                    break;

                case TweenStepType.Punch:
                case TweenStepType.Shake:
                    DrawPunchShakeFields(ref rect, property, type);
                    DrawCommonFields(ref rect, property, skipEase: true);
                    break;

                case TweenStepType.DOPath:
                    DrawDOPathFields(ref rect, property);
                    DrawCommonFields(ref rect, property);
                    break;

                case TweenStepType.Delay:
                    DrawDelayFields(ref rect, property);
                    break;

                case TweenStepType.Callback:
                    DrawCallbackFields(ref rect, property);
                    break;
            }

            EditorGUI.EndProperty();
        }

        #region 高度计算

        private float GetTransformFieldsHeight(SerializedProperty property, TweenStepType type)
        {
            float height = LineHeight + Spacing; // TargetTransform
            height += LineHeight + Spacing;      // TransformTarget

            // RotateDirection（仅 Rotate 类型）
            if (type == TweenStepType.Rotate)
                height += LineHeight + Spacing;

            // 起始值开关
            height += LineHeight + Spacing;
            var useStartValueProp = property.FindPropertyRelative("UseStartValue");
            if (useStartValueProp.boolValue)
            {
                height += LineHeight + Spacing; // StartVector
            }

            height += LineHeight + Spacing; // TargetVector
            height += LineHeight + Spacing; // IsRelative
            height += ButtonHeight + Spacing; // 同步按钮
            return height;
        }

        private float GetColorFieldsHeight(SerializedProperty property)
        {
            float height = LineHeight + Spacing; // TargetTransform
            height += GetValidationWarningHeight(property, TweenStepType.Color);

            // 起始颜色开关
            height += LineHeight + Spacing;
            var useStartColorProp = property.FindPropertyRelative("UseStartColor");
            if (useStartColorProp.boolValue)
            {
                height += LineHeight + Spacing; // StartColor
            }

            height += LineHeight + Spacing; // TargetColor
            return height;
        }

        private float GetFadeFieldsHeight(SerializedProperty property)
        {
            float height = LineHeight + Spacing; // TargetTransform
            height += GetValidationWarningHeight(property, TweenStepType.Fade);

            // 起始值开关
            height += LineHeight + Spacing;
            var useStartFloatProp = property.FindPropertyRelative("UseStartFloat");
            if (useStartFloatProp.boolValue)
            {
                height += LineHeight + Spacing; // StartFloat
            }

            height += LineHeight + Spacing; // TargetFloat
            return height;
        }

        private float GetJumpFieldsHeight(SerializedProperty property)
        {
            float height = LineHeight + Spacing; // TargetTransform
            height += LineHeight + Spacing;      // UseStartValue

            var useStartValueProp = property.FindPropertyRelative("UseStartValue");
            if (useStartValueProp.boolValue)
            {
                height += LineHeight + Spacing;  // StartVector
            }

            height += LineHeight + Spacing;      // TargetVector
            height += LineHeight + Spacing;      // JumpHeight
            height += LineHeight + Spacing;      // JumpNum
            height += ButtonHeight + Spacing;    // 同步按钮
            return height;
        }

        private float GetPunchShakeFieldsHeight(SerializedProperty property, TweenStepType type)
        {
            float height = LineHeight + Spacing; // TargetTransform
            height += LineHeight + Spacing;      // TransformTarget (Punch/Shake 子类型)
            height += LineHeight + Spacing;      // Intensity
            height += LineHeight + Spacing;      // Vibrato
            height += LineHeight + Spacing;      // Elasticity

            if (type == TweenStepType.Shake)
            {
                height += LineHeight + Spacing;  // ShakeRandomness
            }

            return height;
        }

        private float GetDOPathFieldsHeight(SerializedProperty property)
        {
            float height = LineHeight + Spacing; // TargetTransform
            height += LineHeight + Spacing;      // UseStartValue

            var useStartValueProp = property.FindPropertyRelative("UseStartValue");
            if (useStartValueProp.boolValue)
            {
                height += LineHeight + Spacing;
            }

            height += LineHeight + Spacing;      // PathType
            height += LineHeight + Spacing;      // PathMode
            height += LineHeight + Spacing;      // PathResolution

            // 路径点数量 * 每行高度
            var waypointsProp = property.FindPropertyRelative("PathWaypoints");
            if (waypointsProp != null && waypointsProp.isArray)
            {
                height += LineHeight + Spacing; // 标题行
                for (int i = 0; i < waypointsProp.arraySize; i++)
                {
                    height += LineHeight + Spacing;
                }
            }

            height += ButtonHeight + Spacing;   // 同步按钮
            return height;
        }

        private float GetDelayFieldsHeight()
        {
            return LineHeight + Spacing; // Duration
        }

        private float GetCallbackFieldsHeight(SerializedProperty property)
        {
            float height = LineHeight + Spacing; // 标题
            height += EditorGUI.GetPropertyHeight(property.FindPropertyRelative("OnComplete")) + Spacing;
            return height;
        }

        private float GetCommonFieldsHeight(SerializedProperty property, bool skipEase = false)
        {
            float height = LineHeight + Spacing; // Duration
            height += LineHeight + Spacing;      // Delay

            if (!skipEase)
            {
                height += LineHeight + Spacing; // Ease
                height += LineHeight + Spacing; // UseCustomCurve

                var useCustomCurveProp = property.FindPropertyRelative("UseCustomCurve");
                if (useCustomCurveProp.boolValue)
                {
                    height += LineHeight + Spacing;
                }
            }

            height += LineHeight + Spacing; // ExecutionMode

            var executionModeProp = property.FindPropertyRelative("ExecutionMode");
            if ((ExecutionMode)executionModeProp.enumValueIndex == ExecutionMode.Insert)
            {
                height += LineHeight + Spacing;
            }

            height += EditorGUI.GetPropertyHeight(property.FindPropertyRelative("OnComplete")) + Spacing;

            return height;
        }

        #endregion

        #region 绘制方法

        private void DrawTransformFields(ref Rect rect, SerializedProperty property, TweenStepType type)
        {
            // 目标物体
            EditorGUI.PropertyField(rect, property.FindPropertyRelative("TargetTransform"));
            rect.y += LineHeight + Spacing;

            // TransformTarget（仅 Move/Rotate 类型显示子类型选择）
            if (type == TweenStepType.Move)
            {
                EditorGUI.PropertyField(rect, property.FindPropertyRelative("MoveSpace"));
                rect.y += LineHeight + Spacing;
            }
            else if (type == TweenStepType.Rotate)
            {
                EditorGUI.PropertyField(rect, property.FindPropertyRelative("RotateSpace"));
                rect.y += LineHeight + Spacing;

                EditorGUI.PropertyField(rect, property.FindPropertyRelative("RotateDirection"));
                rect.y += LineHeight + Spacing;
            }

            // 起始值开关
            var useStartValueProp = property.FindPropertyRelative("UseStartValue");
            useStartValueProp.boolValue = EditorGUI.ToggleLeft(rect, L10n.Tr("Drawer/UseStartValue"), useStartValueProp.boolValue);
            rect.y += LineHeight + Spacing;

            if (useStartValueProp.boolValue)
            {
                // 起始值标签根据类型调整
                string startLabel = type switch
                {
                    TweenStepType.Rotate => L10n.Tr("Detail/StartRotationEuler"),
                    TweenStepType.AnchorMove => L10n.Tr("Detail/StartAnchorPos"),
                    TweenStepType.SizeDelta => L10n.Tr("Detail/StartSize"),
                    _ => L10n.Tr("Detail/StartValue")
                };
                EditorGUI.PropertyField(rect, property.FindPropertyRelative("StartVector"), new GUIContent(startLabel));
                rect.y += LineHeight + Spacing;
            }

            // 目标值标签根据类型调整
            string targetLabel = type switch
            {
                TweenStepType.Rotate => L10n.Tr("Detail/TargetRotationEuler"),
                TweenStepType.AnchorMove => L10n.Tr("Detail/TargetAnchorPos"),
                TweenStepType.SizeDelta => L10n.Tr("Detail/TargetSize"),
                _ => L10n.Tr("Detail/TargetValue")
            };
            EditorGUI.PropertyField(rect, property.FindPropertyRelative("TargetVector"), new GUIContent(targetLabel));
            rect.y += LineHeight + Spacing;

            // 相对值（AnchorMove/SizeDelta 也支持）
            EditorGUI.PropertyField(rect, property.FindPropertyRelative("IsRelative"));
            rect.y += LineHeight + Spacing;

            // 一键同步按钮
            var buttonRect = new Rect(rect.x, rect.y, rect.width, ButtonHeight);
            if (GUI.Button(buttonRect, L10n.Tr("Drawer/SyncCurrent")))
            {
                SyncCurrentValue(property, type);
            }
            rect.y += ButtonHeight + Spacing;
        }

        private void DrawColorFields(ref Rect rect, SerializedProperty property)
        {
            // 目标物体
            EditorGUI.PropertyField(rect, property.FindPropertyRelative("TargetTransform"));
            rect.y += LineHeight + Spacing;

            // 校验警告
            DrawValidationWarning(ref rect, property, TweenStepType.Color);

            // 起始颜色开关
            var useStartColorProp = property.FindPropertyRelative("UseStartColor");
            useStartColorProp.boolValue = EditorGUI.ToggleLeft(rect, L10n.Tr("Detail/UseStartColor"), useStartColorProp.boolValue);
            rect.y += LineHeight + Spacing;

            if (useStartColorProp.boolValue)
            {
                EditorGUI.PropertyField(rect, property.FindPropertyRelative("StartColor"));
                rect.y += LineHeight + Spacing;
            }

            // 目标颜色
            EditorGUI.PropertyField(rect, property.FindPropertyRelative("TargetColor"));
            rect.y += LineHeight + Spacing;
        }

        private void DrawFadeFields(ref Rect rect, SerializedProperty property, TweenStepType type = TweenStepType.Fade)
        {
            // 目标物体
            EditorGUI.PropertyField(rect, property.FindPropertyRelative("TargetTransform"));
            rect.y += LineHeight + Spacing;

            // 校验警告（仅 Fade 类型，FillAmount 另行处理）
            if (type == TweenStepType.Fade)
                DrawValidationWarning(ref rect, property, TweenStepType.Fade);

            bool isFillAmount = type == TweenStepType.FillAmount;
            string startLabel = isFillAmount ? L10n.Tr("Drawer/UseStartFill") : L10n.Tr("Detail/UseStartAlpha");
            string startValueLabel = isFillAmount ? L10n.Tr("Drawer/StartFill") : L10n.Tr("Detail/StartAlpha");
            string targetValueLabel = isFillAmount ? L10n.Tr("Drawer/TargetFill") : L10n.Tr("Detail/TargetAlpha");

            // 起始值开关
            var useStartFloatProp = property.FindPropertyRelative("UseStartFloat");
            useStartFloatProp.boolValue = EditorGUI.ToggleLeft(rect, startLabel, useStartFloatProp.boolValue);
            rect.y += LineHeight + Spacing;

            if (useStartFloatProp.boolValue)
            {
                EditorGUI.PropertyField(rect, property.FindPropertyRelative("StartFloat"), new GUIContent(startValueLabel));
                rect.y += LineHeight + Spacing;
            }

            // 目标值
            EditorGUI.PropertyField(rect, property.FindPropertyRelative("TargetFloat"), new GUIContent(targetValueLabel));
            rect.y += LineHeight + Spacing;
        }

        private void DrawDelayFields(ref Rect rect, SerializedProperty property)
        {
            EditorGUI.PropertyField(rect, property.FindPropertyRelative("Duration"), new GUIContent(L10n.Tr("Drawer/DelayTime")));
            rect.y += LineHeight + Spacing;
        }

        private void DrawJumpFields(ref Rect rect, SerializedProperty property)
        {
            // 目标物体
            EditorGUI.PropertyField(rect, property.FindPropertyRelative("TargetTransform"));
            rect.y += LineHeight + Spacing;

            // 起始值开关
            var useStartValueProp = property.FindPropertyRelative("UseStartValue");
            useStartValueProp.boolValue = EditorGUI.ToggleLeft(rect, L10n.Tr("Detail/UseStartPosition"), useStartValueProp.boolValue);
            rect.y += LineHeight + Spacing;

            if (useStartValueProp.boolValue)
            {
                EditorGUI.PropertyField(rect, property.FindPropertyRelative("StartVector"), new GUIContent(L10n.Tr("Detail/StartPosition")));
                rect.y += LineHeight + Spacing;
            }

            // 目标位置
            EditorGUI.PropertyField(rect, property.FindPropertyRelative("TargetVector"), new GUIContent(L10n.Tr("Detail/TargetPosition")));
            rect.y += LineHeight + Spacing;

            // 跳跃参数
            EditorGUI.PropertyField(rect, property.FindPropertyRelative("JumpHeight"), new GUIContent(L10n.Tr("Detail/JumpHeight")));
            rect.y += LineHeight + Spacing;
            EditorGUI.PropertyField(rect, property.FindPropertyRelative("JumpNum"), new GUIContent(L10n.Tr("Detail/JumpCount")));
            rect.y += LineHeight + Spacing;

            // 同步按钮
            var buttonRect = new Rect(rect.x, rect.y, rect.width, ButtonHeight);
            if (GUI.Button(buttonRect, L10n.Tr("Drawer/SyncCurrentPos")))
            {
                SyncCurrentValue(property, TweenStepType.Jump);
            }
            rect.y += ButtonHeight + Spacing;
        }

        private void DrawPunchShakeFields(ref Rect rect, SerializedProperty property, TweenStepType type)
        {
            bool isShake = type == TweenStepType.Shake;

            // 目标物体
            EditorGUI.PropertyField(rect, property.FindPropertyRelative("TargetTransform"));
            rect.y += LineHeight + Spacing;

            // Punch/Shake 子类型
            string targetPropName = isShake ? "ShakeTarget" : "PunchTarget";
            var targetProp = property.FindPropertyRelative(targetPropName);
            EditorGUI.PropertyField(rect, targetProp, new GUIContent(isShake ? L10n.Tr("Detail/ShakeTarget") : L10n.Tr("Detail/PunchTarget")));
            rect.y += LineHeight + Spacing;

            // 强度
            EditorGUI.PropertyField(rect, property.FindPropertyRelative("Intensity"), new GUIContent(L10n.Tr("Detail/Intensity")));
            rect.y += LineHeight + Spacing;

            // 震荡次数
            EditorGUI.PropertyField(rect, property.FindPropertyRelative("Vibrato"), new GUIContent(L10n.Tr("Detail/Vibrato")));
            rect.y += LineHeight + Spacing;

            // 弹性
            EditorGUI.PropertyField(rect, property.FindPropertyRelative("Elasticity"), new GUIContent(L10n.Tr("Detail/Elasticity")));
            rect.y += LineHeight + Spacing;

            // 随机性（仅 Shake）
            if (isShake)
            {
                EditorGUI.PropertyField(rect, property.FindPropertyRelative("ShakeRandomness"), new GUIContent(L10n.Tr("Detail/ShakeRandomness")));
                rect.y += LineHeight + Spacing;
            }
        }

        private void DrawDOPathFields(ref Rect rect, SerializedProperty property)
        {
            // 目标物体
            EditorGUI.PropertyField(rect, property.FindPropertyRelative("TargetTransform"));
            rect.y += LineHeight + Spacing;

            // 起始值
            var useStartValueProp = property.FindPropertyRelative("UseStartValue");
            useStartValueProp.boolValue = EditorGUI.ToggleLeft(rect, L10n.Tr("Detail/UseStartPosition"), useStartValueProp.boolValue);
            rect.y += LineHeight + Spacing;

            if (useStartValueProp.boolValue)
            {
                EditorGUI.PropertyField(rect, property.FindPropertyRelative("StartVector"), new GUIContent(L10n.Tr("Detail/StartPosition")));
                rect.y += LineHeight + Spacing;
            }

            // 路径参数
            EditorGUI.PropertyField(rect, property.FindPropertyRelative("PathType"));
            rect.y += LineHeight + Spacing;
            EditorGUI.PropertyField(rect, property.FindPropertyRelative("PathMode"));
            rect.y += LineHeight + Spacing;
            EditorGUI.PropertyField(rect, property.FindPropertyRelative("PathResolution"));
            rect.y += LineHeight + Spacing;

            // 路径点列表
            var waypointsProp = property.FindPropertyRelative("PathWaypoints");
            if (waypointsProp != null && waypointsProp.isArray)
            {
                var wpLabel = L10n.Tr("Detail/Waypoints");
                EditorGUI.LabelField(rect, $"{wpLabel} ({waypointsProp.arraySize})", EditorStyles.boldLabel);
                rect.y += LineHeight + Spacing;

                for (int i = 0; i < waypointsProp.arraySize; i++)
                {
                    var wp = waypointsProp.GetArrayElementAtIndex(i);
                    EditorGUI.PropertyField(rect, wp, new GUIContent($"{L10n.Tr("Drawer/Waypoint")} {i + 1}"));
                    rect.y += LineHeight + Spacing;
                }
            }

            // 同步按钮
            var buttonRect = new Rect(rect.x, rect.y, rect.width, ButtonHeight);
            if (GUI.Button(buttonRect, L10n.Tr("Drawer/SyncCurrentPos")))
            {
                SyncCurrentValue(property, TweenStepType.DOPath);
            }
            rect.y += ButtonHeight + Spacing;
        }

        private void DrawCallbackFields(ref Rect rect, SerializedProperty property)
        {
            EditorGUI.LabelField(rect, L10n.Tr("Detail/CallbackEvent"), EditorStyles.boldLabel);
            rect.y += LineHeight + Spacing;

            var onCompleteProp = property.FindPropertyRelative("OnComplete");
            var height = EditorGUI.GetPropertyHeight(onCompleteProp);
            var eventRect = new Rect(rect.x, rect.y, rect.width, height);
            EditorGUI.PropertyField(eventRect, onCompleteProp, GUIContent.none);
            rect.y += height + Spacing;
        }

        private void DrawCommonFields(ref Rect rect, SerializedProperty property, bool skipEase = false)
        {
            // Duration
            EditorGUI.PropertyField(rect, property.FindPropertyRelative("Duration"));
            rect.y += LineHeight + Spacing;

            // Delay
            EditorGUI.PropertyField(rect, property.FindPropertyRelative("Delay"));
            rect.y += LineHeight + Spacing;

            // 缓动设置（Punch/Shake 有内置振荡缓动，跳过）
            if (!skipEase)
            {
                // Ease
                EditorGUI.PropertyField(rect, property.FindPropertyRelative("Ease"));
                rect.y += LineHeight + Spacing;

                // 自定义曲线
                var useCustomCurveProp = property.FindPropertyRelative("UseCustomCurve");
                EditorGUI.PropertyField(rect, useCustomCurveProp);
                rect.y += LineHeight + Spacing;

                if (useCustomCurveProp.boolValue)
                {
                    EditorGUI.PropertyField(rect, property.FindPropertyRelative("CustomCurve"));
                    rect.y += LineHeight + Spacing;
                }
            }

            // ExecutionMode
            EditorGUI.PropertyField(rect, property.FindPropertyRelative("ExecutionMode"));
            rect.y += LineHeight + Spacing;

            var executionModeProp = property.FindPropertyRelative("ExecutionMode");
            if ((ExecutionMode)executionModeProp.enumValueIndex == ExecutionMode.Insert)
            {
                EditorGUI.PropertyField(rect, property.FindPropertyRelative("InsertTime"));
                rect.y += LineHeight + Spacing;
            }

            // OnComplete
            var onCompleteProp = property.FindPropertyRelative("OnComplete");
            var height = EditorGUI.GetPropertyHeight(onCompleteProp);
            var eventRect = new Rect(rect.x, rect.y, rect.width, height);
            EditorGUI.PropertyField(eventRect, onCompleteProp);
            rect.y += height + Spacing;
        }

        #endregion

        #region 校验警告

        private const float WarningHeight = 18f;

        /// <summary>
        /// 获取校验警告区域高度（仅在目标不满足需求时占用空间）
        /// </summary>
        private float GetValidationWarningHeight(SerializedProperty property, TweenStepType type)
        {
            var target = property.FindPropertyRelative("TargetTransform")?.objectReferenceValue as Transform;
            if (target == null) return 0f;
            if (TweenStepRequirement.Validate(target, type, out _)) return 0f;
            return WarningHeight + Spacing;
        }

        /// <summary>
        /// 绘制校验警告（当目标物体不满足动画类型的组件需求时显示红色提示）
        /// </summary>
        private void DrawValidationWarning(ref Rect rect, SerializedProperty property, TweenStepType type)
        {
            var target = property.FindPropertyRelative("TargetTransform")?.objectReferenceValue as Transform;
            if (target == null) return;
            if (TweenStepRequirement.Validate(target, type, out string errorMessage)) return;

            var oldColor = GUI.color;
            GUI.color = new Color(1f, 0.7f, 0.2f);
            EditorGUI.LabelField(rect, $"⚠ {errorMessage}", EditorStyles.miniLabel);
            GUI.color = oldColor;
            rect.y += WarningHeight + Spacing;
        }

        #endregion

        #region 一键同步

        private void SyncCurrentValue(SerializedProperty property, TweenStepType type)
        {
            var targetTransformProp = property.FindPropertyRelative("TargetTransform");
            var target = targetTransformProp.objectReferenceValue as Transform;

            if (target == null)
            {
                var component = targetTransformProp.serializedObject.targetObject as MonoBehaviour;
                if (component != null)
                {
                    target = component.transform;
                }
            }

            if (target == null)
            {
                DOTweenLog.Warning(L10n.Tr("Drawer/CannotGetTarget"));
                return;
            }

            var transformTargetProp = property.FindPropertyRelative("MoveSpace");
            var moveSpace = (MoveSpace)transformTargetProp.enumValueIndex;

            Vector3 currentValue = Vector3.zero;

            switch (type)
            {
                case TweenStepType.Move:
                    switch (moveSpace)
                    {
                        case MoveSpace.Local:
                            currentValue = target.localPosition;
                            break;
                        default:
                            currentValue = target.position;
                            break;
                    }
                    break;

                case TweenStepType.Rotate:
                    var rotateSpace = (RotateSpace)property.FindPropertyRelative("RotateSpace").enumValueIndex;
                    switch (rotateSpace)
                    {
                        case RotateSpace.Local:
                            currentValue = target.localRotation.eulerAngles;
                            break;
                        default:
                            currentValue = target.rotation.eulerAngles;
                            break;
                    }
                    break;

                case TweenStepType.Scale:
                    currentValue = target.localScale;
                    break;

                case TweenStepType.AnchorMove:
                    var rectTransform = target as RectTransform;
                    if (rectTransform == null) rectTransform = target.GetComponent<RectTransform>();
                    if (rectTransform != null)
                    {
                        currentValue = rectTransform.anchoredPosition;
                    }
                    break;

                case TweenStepType.SizeDelta:
                    var rt2 = target as RectTransform;
                    if (rt2 == null) rt2 = target.GetComponent<RectTransform>();
                    if (rt2 != null)
                    {
                        currentValue = rt2.sizeDelta;
                    }
                    break;

                case TweenStepType.Jump:
                    currentValue = target.position;
                    break;
            }

            property.FindPropertyRelative("TargetVector").vector3Value = currentValue;
            property.serializedObject.ApplyModifiedProperties();

            DOTweenLog.Info($"{L10n.Tr("Drawer/Synced")} {target.name} {type} = {currentValue}");
        }

        #endregion
    }
}
#endif
