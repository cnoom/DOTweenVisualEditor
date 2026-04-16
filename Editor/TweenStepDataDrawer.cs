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
    /// </summary>
    [CustomPropertyDrawer(typeof(TweenStepData))]
    public class TweenStepDataDrawer : PropertyDrawer
    {
        #region 常量

        private const float LineHeight = 18f;
        private const float Spacing = 2f;
        private const float ButtonHeight = 24f;

        #endregion

        #region 缓存

        private SerializedProperty _typeProp;
        private SerializedProperty _targetTransformProp;
        private SerializedProperty _transformTargetProp;
        private SerializedProperty _targetValueProp;
        private SerializedProperty _isRelativeProp;
        private SerializedProperty _durationProp;
        private SerializedProperty _delayProp;
        private SerializedProperty _easeProp;
        private SerializedProperty _useCustomCurveProp;
        private SerializedProperty _customCurveProp;
        private SerializedProperty _executionModeProp;
        private SerializedProperty _insertTimeProp;
        private SerializedProperty _onCompleteProp;
        private SerializedProperty _isEnabledProp;

        #endregion

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            CacheProperties(property);
            
            float height = LineHeight + Spacing; // IsEnabled + Type
            
            var type = (TweenStepType)_typeProp.enumValueIndex;
            
            // 根据类型计算高度
            switch (type)
            {
                case TweenStepType.Move:
                case TweenStepType.Rotate:
                case TweenStepType.Scale:
                    height += GetTransformFieldsHeight();
                    height += GetCommonFieldsHeight(type);
                    break;
                    
                case TweenStepType.Delay:
                    height += GetDelayFieldsHeight();
                    break;
                    
                case TweenStepType.Callback:
                    height += GetCallbackFieldsHeight();
                    break;
                    
                case TweenStepType.Property:
                    height += GetPropertyFieldsHeight();
                    break;
            }
            
            return height;
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            CacheProperties(property);
            
            EditorGUI.BeginProperty(position, label, property);
            
            var rect = new Rect(position.x, position.y, position.width, LineHeight);
            
            // 启用开关
            _isEnabledProp.boolValue = EditorGUI.ToggleLeft(rect, " 启用", _isEnabledProp.boolValue);
            rect.y += LineHeight + Spacing;
            
            // 类型选择
            EditorGUI.PropertyField(rect, _typeProp);
            rect.y += LineHeight + Spacing;
            
            var type = (TweenStepType)_typeProp.enumValueIndex;
            
            // 根据类型显示不同字段
            switch (type)
            {
                case TweenStepType.Move:
                case TweenStepType.Rotate:
                case TweenStepType.Scale:
                    DrawTransformFields(ref rect);
                    DrawCommonFields(ref rect, type);
                    break;
                    
                case TweenStepType.Delay:
                    DrawDelayFields(ref rect);
                    break;
                    
                case TweenStepType.Callback:
                    DrawCallbackFields(ref rect);
                    break;
                    
                case TweenStepType.Property:
                    DrawPropertyFields(ref rect);
                    break;
            }
            
            EditorGUI.EndProperty();
        }

        #region 缓存方法

        private void CacheProperties(SerializedProperty property)
        {
            _typeProp = property.FindPropertyRelative("Type");
            _targetTransformProp = property.FindPropertyRelative("TargetTransform");
            _transformTargetProp = property.FindPropertyRelative("TransformTarget");
            _targetValueProp = property.FindPropertyRelative("TargetValue");
            _isRelativeProp = property.FindPropertyRelative("IsRelative");
            _durationProp = property.FindPropertyRelative("Duration");
            _delayProp = property.FindPropertyRelative("Delay");
            _easeProp = property.FindPropertyRelative("Ease");
            _useCustomCurveProp = property.FindPropertyRelative("UseCustomCurve");
            _customCurveProp = property.FindPropertyRelative("CustomCurve");
            _executionModeProp = property.FindPropertyRelative("ExecutionMode");
            _insertTimeProp = property.FindPropertyRelative("InsertTime");
            _onCompleteProp = property.FindPropertyRelative("OnComplete");
            _isEnabledProp = property.FindPropertyRelative("IsEnabled");
        }

        #endregion

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

        private float GetCallbackFieldsHeight()
        {
            float height = LineHeight + Spacing; // 标题
            height += EditorGUI.GetPropertyHeight(_onCompleteProp) + Spacing;
            return height;
        }

        private float GetPropertyFieldsHeight()
        {
            return LineHeight + Spacing; // 占位
        }

        private float GetCommonFieldsHeight(TweenStepType type)
        {
            float height = LineHeight + Spacing; // Duration
            height += LineHeight + Spacing;      // Delay
            height += LineHeight + Spacing;      // Ease
            height += LineHeight + Spacing;      // UseCustomCurve
            
            // CustomCurve（条件显示）
            if (_useCustomCurveProp.boolValue)
            {
                height += LineHeight + Spacing;
            }
            
            height += LineHeight + Spacing;      // ExecutionMode
            
            // InsertTime（条件显示）
            if ((ExecutionMode)_executionModeProp.enumValueIndex == ExecutionMode.Insert)
            {
                height += LineHeight + Spacing;
            }
            
            height += EditorGUI.GetPropertyHeight(_onCompleteProp) + Spacing; // OnComplete
            
            return height;
        }

        #endregion

        #region 绘制方法

        private void DrawTransformFields(ref Rect rect)
        {
            // 目标物体
            EditorGUI.PropertyField(rect, _targetTransformProp);
            rect.y += LineHeight + Spacing;
            
            // TransformTarget
            EditorGUI.PropertyField(rect, _transformTargetProp);
            rect.y += LineHeight + Spacing;
            
            // TargetValue
            EditorGUI.PropertyField(rect, _targetValueProp);
            rect.y += LineHeight + Spacing;
            
            // IsRelative
            EditorGUI.PropertyField(rect, _isRelativeProp);
            rect.y += LineHeight + Spacing;
            
            // 一键同步按钮
            var buttonRect = new Rect(rect.x, rect.y, rect.width, ButtonHeight);
            if (GUI.Button(buttonRect, "同步当前值"))
            {
                SyncCurrentValue();
            }
            rect.y += ButtonHeight + Spacing;
        }

        private void DrawDelayFields(ref Rect rect)
        {
            // Delay 类型只需要 Duration
            EditorGUI.PropertyField(rect, _durationProp, new GUIContent("延迟时间"));
            rect.y += LineHeight + Spacing;
        }

        private void DrawCallbackFields(ref Rect rect)
        {
            // Callback 类型只需要 OnComplete
            EditorGUI.LabelField(rect, "回调事件", EditorStyles.boldLabel);
            rect.y += LineHeight + Spacing;
            
            var height = EditorGUI.GetPropertyHeight(_onCompleteProp);
            var eventRect = new Rect(rect.x, rect.y, rect.width, height);
            EditorGUI.PropertyField(eventRect, _onCompleteProp, GUIContent.none);
            rect.y += height + Spacing;
        }

        private void DrawPropertyFields(ref Rect rect)
        {
            EditorGUI.LabelField(rect, "Property 动画暂未实现", EditorStyles.helpBox);
            rect.y += LineHeight + Spacing;
        }

        private void DrawCommonFields(ref Rect rect, TweenStepType type)
        {
            // Duration
            EditorGUI.PropertyField(rect, _durationProp);
            rect.y += LineHeight + Spacing;
            
            // Delay
            EditorGUI.PropertyField(rect, _delayProp);
            rect.y += LineHeight + Spacing;
            
            // Ease
            EditorGUI.PropertyField(rect, _easeProp);
            rect.y += LineHeight + Spacing;
            
            // 自定义曲线
            EditorGUI.PropertyField(rect, _useCustomCurveProp);
            rect.y += LineHeight + Spacing;
            
            if (_useCustomCurveProp.boolValue)
            {
                EditorGUI.PropertyField(rect, _customCurveProp);
                rect.y += LineHeight + Spacing;
            }
            
            // ExecutionMode
            EditorGUI.PropertyField(rect, _executionModeProp);
            rect.y += LineHeight + Spacing;
            
            if ((ExecutionMode)_executionModeProp.enumValueIndex == ExecutionMode.Insert)
            {
                EditorGUI.PropertyField(rect, _insertTimeProp);
                rect.y += LineHeight + Spacing;
            }
            
            // OnComplete
            var height = EditorGUI.GetPropertyHeight(_onCompleteProp);
            var eventRect = new Rect(rect.x, rect.y, rect.width, height);
            EditorGUI.PropertyField(eventRect, _onCompleteProp);
            rect.y += height + Spacing;
        }

        #endregion

        #region 一键同步

        private void SyncCurrentValue()
        {
            var target = _targetTransformProp.objectReferenceValue as Transform;
            
            // 如果没有指定目标，尝试获取组件所在物体
            if (target == null)
            {
                // 通过 serializedObject 获取组件
                var component = _targetTransformProp.serializedObject.targetObject as MonoBehaviour;
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
            
            var type = (TweenStepType)_typeProp.enumValueIndex;
            var transformTarget = (TransformTarget)_transformTargetProp.enumValueIndex;
            
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
            
            _targetValueProp.vector3Value = currentValue;
            
            // 标记为已修改
            _targetValueProp.serializedObject.ApplyModifiedProperties();
            
            Debug.Log($"已同步 {target.name} 的 {type}.{transformTarget} = {currentValue}");
        }

        #endregion
    }
}
#endif
