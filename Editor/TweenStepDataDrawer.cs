#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityEngine.Events;
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
                    height += GetTransformFieldsHeight(property, type);
                    height += GetCommonFieldsHeight(property);
                    break;

                case TweenStepType.Color:
                    height += GetColorFieldsHeight(property);
                    height += GetCommonFieldsHeight(property);
                    break;

                case TweenStepType.Fade:
                    height += GetFadeFieldsHeight(property);
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
            isEnabledProp.boolValue = EditorGUI.ToggleLeft(rect, " 启用", isEnabledProp.boolValue);
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
                    DrawTransformFields(ref rect, property, type);
                    DrawCommonFields(ref rect, property);
                    break;

                case TweenStepType.Color:
                    DrawColorFields(ref rect, property);
                    DrawCommonFields(ref rect, property);
                    break;

                case TweenStepType.Fade:
                    DrawFadeFields(ref rect, property);
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

        private float GetCommonFieldsHeight(SerializedProperty property)
        {
            float height = LineHeight + Spacing; // Duration
            height += LineHeight + Spacing;      // Delay
            height += LineHeight + Spacing;      // Ease
            height += LineHeight + Spacing;      // UseCustomCurve

            var useCustomCurveProp = property.FindPropertyRelative("UseCustomCurve");
            if (useCustomCurveProp.boolValue)
            {
                height += LineHeight + Spacing;
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

            // TransformTarget
            EditorGUI.PropertyField(rect, property.FindPropertyRelative("TransformTarget"));
            rect.y += LineHeight + Spacing;

            // 起始值开关
            var useStartValueProp = property.FindPropertyRelative("UseStartValue");
            useStartValueProp.boolValue = EditorGUI.ToggleLeft(rect, " 使用起始值", useStartValueProp.boolValue);
            rect.y += LineHeight + Spacing;

            if (useStartValueProp.boolValue)
            {
                // 起始值标签根据类型调整
                string startLabel = type == TweenStepType.Rotate ? "起始旋转 (欧拉角)" : "起始值";
                EditorGUI.PropertyField(rect, property.FindPropertyRelative("StartVector"), new GUIContent(startLabel));
                rect.y += LineHeight + Spacing;
            }

            // 目标值标签根据类型调整
            string targetLabel = type == TweenStepType.Rotate ? "目标旋转 (欧拉角)" : "目标值";
            EditorGUI.PropertyField(rect, property.FindPropertyRelative("TargetVector"), new GUIContent(targetLabel));
            rect.y += LineHeight + Spacing;

            // 相对值
            EditorGUI.PropertyField(rect, property.FindPropertyRelative("IsRelative"));
            rect.y += LineHeight + Spacing;

            // 一键同步按钮
            var buttonRect = new Rect(rect.x, rect.y, rect.width, ButtonHeight);
            if (GUI.Button(buttonRect, "同步当前值"))
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

            // 起始颜色开关
            var useStartColorProp = property.FindPropertyRelative("UseStartColor");
            useStartColorProp.boolValue = EditorGUI.ToggleLeft(rect, " 使用起始颜色", useStartColorProp.boolValue);
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

        private void DrawFadeFields(ref Rect rect, SerializedProperty property)
        {
            // 目标物体
            EditorGUI.PropertyField(rect, property.FindPropertyRelative("TargetTransform"));
            rect.y += LineHeight + Spacing;

            // 起始值开关
            var useStartFloatProp = property.FindPropertyRelative("UseStartFloat");
            useStartFloatProp.boolValue = EditorGUI.ToggleLeft(rect, " 使用起始透明度", useStartFloatProp.boolValue);
            rect.y += LineHeight + Spacing;

            if (useStartFloatProp.boolValue)
            {
                EditorGUI.PropertyField(rect, property.FindPropertyRelative("StartFloat"), new GUIContent("起始透明度"));
                rect.y += LineHeight + Spacing;
            }

            // 目标透明度
            EditorGUI.PropertyField(rect, property.FindPropertyRelative("TargetFloat"), new GUIContent("目标透明度"));
            rect.y += LineHeight + Spacing;
        }

        private void DrawDelayFields(ref Rect rect, SerializedProperty property)
        {
            EditorGUI.PropertyField(rect, property.FindPropertyRelative("Duration"), new GUIContent("延迟时间"));
            rect.y += LineHeight + Spacing;
        }

        private void DrawCallbackFields(ref Rect rect, SerializedProperty property)
        {
            EditorGUI.LabelField(rect, "回调事件", EditorStyles.boldLabel);
            rect.y += LineHeight + Spacing;

            var onCompleteProp = property.FindPropertyRelative("OnComplete");
            var height = EditorGUI.GetPropertyHeight(onCompleteProp);
            var eventRect = new Rect(rect.x, rect.y, rect.width, height);
            EditorGUI.PropertyField(eventRect, onCompleteProp, GUIContent.none);
            rect.y += height + Spacing;
        }

        private void DrawCommonFields(ref Rect rect, SerializedProperty property)
        {
            // Duration
            EditorGUI.PropertyField(rect, property.FindPropertyRelative("Duration"));
            rect.y += LineHeight + Spacing;

            // Delay
            EditorGUI.PropertyField(rect, property.FindPropertyRelative("Delay"));
            rect.y += LineHeight + Spacing;

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
                Debug.LogWarning("无法获取目标物体");
                return;
            }

            var transformTargetProp = property.FindPropertyRelative("TransformTarget");
            var transformTarget = (TransformTarget)transformTargetProp.enumValueIndex;

            Vector3 currentValue = Vector3.zero;

            switch (type)
            {
                case TweenStepType.Move:
                    switch (transformTarget)
                    {
                        case TransformTarget.Position:
                            currentValue = target.position;
                            break;
                        case TransformTarget.LocalPosition:
                            currentValue = target.localPosition;
                            break;
                    }
                    break;

                case TweenStepType.Rotate:
                    switch (transformTarget)
                    {
                        case TransformTarget.Rotation:
                            currentValue = target.rotation.eulerAngles;
                            break;
                        case TransformTarget.LocalRotation:
                            currentValue = target.localRotation.eulerAngles;
                            break;
                    }
                    break;

                case TweenStepType.Scale:
                    currentValue = target.localScale;
                    break;
            }

            property.FindPropertyRelative("TargetVector").vector3Value = currentValue;
            property.serializedObject.ApplyModifiedProperties();

            Debug.Log($"已同步 {target.name} 的 {type}.{transformTarget} = {currentValue}");
        }

        #endregion
    }
}
#endif
