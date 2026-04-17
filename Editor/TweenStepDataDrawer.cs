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
    /// 实现字段显示/隐藏和一键同步功能
    /// 
    /// 注意：PropertyDrawer 实例在列表项间复用，
    /// 缓存 SerializedProperty 会指向过期数据，
    /// 因此每次 OnGUI/GetPropertyHeight 都按需获取。
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
            var onCompleteProp = property.FindPropertyRelative("OnComplete");
            var useCustomCurveProp = property.FindPropertyRelative("UseCustomCurve");
            var executionModeProp = property.FindPropertyRelative("ExecutionMode");
            
            // 根据类型计算高度
            switch (type)
            {
                case TweenStepType.Move:
                case TweenStepType.Rotate:
                case TweenStepType.Scale:
                    height += GetTransformFieldsHeight();
                    height += GetCommonFieldsHeight(onCompleteProp, useCustomCurveProp, executionModeProp);
                    break;
                    
                case TweenStepType.Delay:
                    height += GetDelayFieldsHeight();
                    break;
                    
                case TweenStepType.Callback:
                    height += GetCallbackFieldsHeight(onCompleteProp);
                    break;
                    
                case TweenStepType.Property:
                    height += GetPropertyFieldsHeight();
                    break;
            }
            
            return height;
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);
            
            var rect = new Rect(position.x, position.y, position.width, LineHeight);
            
            // 按需获取属性
            var typeProp = property.FindPropertyRelative("Type");
            var isEnabledProp = property.FindPropertyRelative("IsEnabled");
            var onCompleteProp = property.FindPropertyRelative("OnComplete");
            var useCustomCurveProp = property.FindPropertyRelative("UseCustomCurve");
            var executionModeProp = property.FindPropertyRelative("ExecutionMode");
            
            // 启用开关
            isEnabledProp.boolValue = EditorGUI.ToggleLeft(rect, " 启用", isEnabledProp.boolValue);
            rect.y += LineHeight + Spacing;
            
            // 类型选择
            EditorGUI.PropertyField(rect, typeProp);
            rect.y += LineHeight + Spacing;
            
            var type = (TweenStepType)typeProp.enumValueIndex;
            
            // 根据类型显示不同字段
            switch (type)
            {
                case TweenStepType.Move:
                case TweenStepType.Rotate:
                case TweenStepType.Scale:
                    DrawTransformFields(ref rect, property);
                    DrawCommonFields(ref rect, property, onCompleteProp, useCustomCurveProp, executionModeProp);
                    break;
                    
                case TweenStepType.Delay:
                    DrawDelayFields(ref rect, property);
                    break;
                    
                case TweenStepType.Callback:
                    DrawCallbackFields(ref rect, onCompleteProp);
                    break;
                    
                case TweenStepType.Property:
                    DrawPropertyFields(ref rect);
                    break;
            }
            
            EditorGUI.EndProperty();
        }

        #region 高度计算

        private float GetTransformFieldsHeight()
        {
            float height = LineHeight + Spacing; // TargetTransform
            height += LineHeight + Spacing;      // TransformTarget
            height += LineHeight + Spacing;      // TargetValue
            height += LineHeight + Spacing;      // IsRelative
            height += ButtonHeight + Spacing;    // 同步按钮
            return height;
        }

        private float GetDelayFieldsHeight()
        {
            return LineHeight + Spacing; // Duration
        }

        private float GetCallbackFieldsHeight(SerializedProperty onCompleteProp)
        {
            float height = LineHeight + Spacing; // 标题
            height += EditorGUI.GetPropertyHeight(onCompleteProp) + Spacing;
            return height;
        }

        private float GetPropertyFieldsHeight()
        {
            return LineHeight + Spacing; // 占位
        }

        private float GetCommonFieldsHeight(SerializedProperty onCompleteProp, SerializedProperty useCustomCurveProp, SerializedProperty executionModeProp)
        {
            float height = LineHeight + Spacing; // Duration
            height += LineHeight + Spacing;      // Delay
            height += LineHeight + Spacing;      // Ease
            height += LineHeight + Spacing;      // UseCustomCurve
            
            // CustomCurve（条件显示）
            if (useCustomCurveProp.boolValue)
            {
                height += LineHeight + Spacing;
            }
            
            height += LineHeight + Spacing;      // ExecutionMode
            
            // InsertTime（条件显示）
            if ((ExecutionMode)executionModeProp.enumValueIndex == ExecutionMode.Insert)
            {
                height += LineHeight + Spacing;
            }
            
            height += EditorGUI.GetPropertyHeight(onCompleteProp) + Spacing; // OnComplete
            
            return height;
        }

        #endregion

        #region 绘制方法

        private void DrawTransformFields(ref Rect rect, SerializedProperty property)
        {
            var targetTransformProp = property.FindPropertyRelative("TargetTransform");
            var transformTargetProp = property.FindPropertyRelative("TransformTarget");
            var targetValueProp = property.FindPropertyRelative("TargetValue");
            var isRelativeProp = property.FindPropertyRelative("IsRelative");
            
            // 目标物体
            EditorGUI.PropertyField(rect, targetTransformProp);
            rect.y += LineHeight + Spacing;
            
            // TransformTarget
            EditorGUI.PropertyField(rect, transformTargetProp);
            rect.y += LineHeight + Spacing;
            
            // TargetValue
            EditorGUI.PropertyField(rect, targetValueProp);
            rect.y += LineHeight + Spacing;
            
            // IsRelative
            EditorGUI.PropertyField(rect, isRelativeProp);
            rect.y += LineHeight + Spacing;
            
            // 一键同步按钮
            var buttonRect = new Rect(rect.x, rect.y, rect.width, ButtonHeight);
            if (GUI.Button(buttonRect, "同步当前值"))
            {
                SyncCurrentValue(property);
            }
            rect.y += ButtonHeight + Spacing;
        }

        private void DrawDelayFields(ref Rect rect, SerializedProperty property)
        {
            var durationProp = property.FindPropertyRelative("Duration");
            EditorGUI.PropertyField(rect, durationProp, new GUIContent("延迟时间"));
            rect.y += LineHeight + Spacing;
        }

        private void DrawCallbackFields(ref Rect rect, SerializedProperty onCompleteProp)
        {
            EditorGUI.LabelField(rect, "回调事件", EditorStyles.boldLabel);
            rect.y += LineHeight + Spacing;
            
            var height = EditorGUI.GetPropertyHeight(onCompleteProp);
            var eventRect = new Rect(rect.x, rect.y, rect.width, height);
            EditorGUI.PropertyField(eventRect, onCompleteProp, GUIContent.none);
            rect.y += height + Spacing;
        }

        private void DrawPropertyFields(ref Rect rect)
        {
            EditorGUI.LabelField(rect, "Property 动画暂未实现", EditorStyles.helpBox);
            rect.y += LineHeight + Spacing;
        }

        private void DrawCommonFields(ref Rect rect, SerializedProperty property, SerializedProperty onCompleteProp, SerializedProperty useCustomCurveProp, SerializedProperty executionModeProp)
        {
            var durationProp = property.FindPropertyRelative("Duration");
            var delayProp = property.FindPropertyRelative("Delay");
            var easeProp = property.FindPropertyRelative("Ease");
            var customCurveProp = property.FindPropertyRelative("CustomCurve");
            var insertTimeProp = property.FindPropertyRelative("InsertTime");
            
            // Duration
            EditorGUI.PropertyField(rect, durationProp);
            rect.y += LineHeight + Spacing;
            
            // Delay
            EditorGUI.PropertyField(rect, delayProp);
            rect.y += LineHeight + Spacing;
            
            // Ease
            EditorGUI.PropertyField(rect, easeProp);
            rect.y += LineHeight + Spacing;
            
            // 自定义曲线
            EditorGUI.PropertyField(rect, useCustomCurveProp);
            rect.y += LineHeight + Spacing;
            
            if (useCustomCurveProp.boolValue)
            {
                EditorGUI.PropertyField(rect, customCurveProp);
                rect.y += LineHeight + Spacing;
            }
            
            // ExecutionMode
            EditorGUI.PropertyField(rect, executionModeProp);
            rect.y += LineHeight + Spacing;
            
            if ((ExecutionMode)executionModeProp.enumValueIndex == ExecutionMode.Insert)
            {
                EditorGUI.PropertyField(rect, insertTimeProp);
                rect.y += LineHeight + Spacing;
            }
            
            // OnComplete
            var height = EditorGUI.GetPropertyHeight(onCompleteProp);
            var eventRect = new Rect(rect.x, rect.y, rect.width, height);
            EditorGUI.PropertyField(eventRect, onCompleteProp);
            rect.y += height + Spacing;
        }

        #endregion

        #region 一键同步

        private void SyncCurrentValue(SerializedProperty property)
        {
            var targetTransformProp = property.FindPropertyRelative("TargetTransform");
            var typeProp = property.FindPropertyRelative("Type");
            var transformTargetProp = property.FindPropertyRelative("TransformTarget");
            var targetValueProp = property.FindPropertyRelative("TargetValue");
            
            var target = targetTransformProp.objectReferenceValue as Transform;
            
            // 如果没有指定目标，尝试获取组件所在物体
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
            
            var type = (TweenStepType)typeProp.enumValueIndex;
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
            
            targetValueProp.vector3Value = currentValue;
            targetValueProp.serializedObject.ApplyModifiedProperties();
            
            Debug.Log($"已同步 {target.name} 的 {type}.{transformTarget} = {currentValue}");
        }

        #endregion
    }
}
#endif