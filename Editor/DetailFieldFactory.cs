#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace CNoom.DOTweenVisual.Editor
{
    /// <summary>
    /// 详情面板 UI 字段工厂
    /// 提供 SerializedProperty 绑定的 UI 字段创建方法
    /// </summary>
    internal static class DetailFieldFactory
    {
        /// <summary>
        /// 检查 SerializedProperty 是否仍然有效
        /// 兼容 Unity 2021.3+（isValid 在 2022.1 才引入）
        /// </summary>
        public static bool IsValidProperty(SerializedProperty prop)
        {
            if (prop == null) return false;
            try
            {
                _ = prop.type;
                return true;
            }
            catch
            {
                return false;
            }
        }

        public static Toggle CreateToggle(SerializedProperty prop, Action onChanged = null)
        {
            var toggle = new Toggle { value = prop.boolValue };
            toggle.RegisterValueChangedCallback(evt =>
            {
                if (!IsValidProperty(prop)) return;
                prop.boolValue = evt.newValue;
                prop.serializedObject.ApplyModifiedProperties();
                onChanged?.Invoke();
            });
            return toggle;
        }

        public static FloatField CreateFloatField(SerializedProperty prop, Action onChanged = null)
        {
            var field = new FloatField { value = prop.floatValue };
            field.RegisterValueChangedCallback(evt =>
            {
                if (!IsValidProperty(prop)) return;
                prop.floatValue = evt.newValue;
                prop.serializedObject.ApplyModifiedProperties();
                onChanged?.Invoke();
            });
            return field;
        }

        public static Vector3Field CreateVector3Field(SerializedProperty prop)
        {
            var field = new Vector3Field { value = prop.vector3Value };
            field.RegisterValueChangedCallback(evt =>
            {
                if (!IsValidProperty(prop)) return;
                prop.vector3Value = evt.newValue;
                prop.serializedObject.ApplyModifiedProperties();
            });
            return field;
        }

        public static ColorField CreateColorField(SerializedProperty prop)
        {
            var field = new ColorField { value = prop.colorValue };
            field.RegisterValueChangedCallback(evt =>
            {
                if (!IsValidProperty(prop)) return;
                prop.colorValue = evt.newValue;
                prop.serializedObject.ApplyModifiedProperties();
            });
            return field;
        }

        public static EnumField CreateEnumField(SerializedProperty prop, Type enumType, Action onChanged = null)
        {
            var field = new EnumField((Enum)Enum.GetValues(enumType).GetValue(prop.enumValueIndex));
            field.RegisterValueChangedCallback(evt =>
            {
                if (!IsValidProperty(prop)) return;
                prop.enumValueIndex = Convert.ToInt32(evt.newValue);
                prop.serializedObject.ApplyModifiedProperties();
                onChanged?.Invoke();
            });
            return field;
        }

        public static ObjectField CreateObjectField(SerializedProperty prop, Type objType, Action onChanged = null)
        {
            var field = new ObjectField { objectType = objType, value = prop.objectReferenceValue };
            field.RegisterValueChangedCallback(evt =>
            {
                if (!IsValidProperty(prop)) return;
                prop.objectReferenceValue = evt.newValue;
                prop.serializedObject.ApplyModifiedProperties();
                onChanged?.Invoke();
            });
            return field;
        }

        public static CurveField CreateCurveField(SerializedProperty prop)
        {
            var field = new CurveField { value = prop.animationCurveValue };
            field.RegisterValueChangedCallback(evt =>
            {
                if (!IsValidProperty(prop)) return;
                prop.animationCurveValue = evt.newValue;
                prop.serializedObject.ApplyModifiedProperties();
            });
            return field;
        }

        public static IntegerField CreateIntegerField(SerializedProperty prop)
        {
            var field = new IntegerField { value = prop.intValue };
            field.RegisterValueChangedCallback(evt =>
            {
                if (!IsValidProperty(prop)) return;
                prop.intValue = evt.newValue;
                prop.serializedObject.ApplyModifiedProperties();
            });
            return field;
        }

        /// <summary>
        /// 创建路径类型下拉选择器（0=Linear, 1=CatmullRom, 2=CubicBezier）
        /// </summary>
        public static VisualElement CreatePathTypeEnumField(SerializedProperty prop)
        {
            var options = new System.Collections.Generic.List<string> { L10n.Tr("PathOption/Linear"), L10n.Tr("PathOption/CatmullRom"), L10n.Tr("PathOption/CubicBezier") };
            int idx = Mathf.Clamp(prop.intValue, 0, options.Count - 1);
            var field = new PopupField<string>(options, options[idx]);
            field.style.flexGrow = 1;
            field.RegisterValueChangedCallback(evt =>
            {
                if (!IsValidProperty(prop)) return;
                prop.intValue = options.IndexOf(evt.newValue);
                if (prop.intValue < 0) prop.intValue = 0;
                prop.serializedObject.ApplyModifiedProperties();
            });
            return field;
        }

        /// <summary>
        /// 创建路径模式下拉选择器（0=3D, 1=TopDown2D, 2=SideScroll2D）
        /// </summary>
        public static VisualElement CreatePathModeEnumField(SerializedProperty prop)
        {
            var options = new System.Collections.Generic.List<string> { L10n.Tr("PathOption/3D"), L10n.Tr("PathOption/TopDown2D"), L10n.Tr("PathOption/SideScroll2D") };
            int idx = Mathf.Clamp(prop.intValue, 0, options.Count - 1);
            var field = new PopupField<string>(options, options[idx]);
            field.style.flexGrow = 1;
            field.RegisterValueChangedCallback(evt =>
            {
                if (!IsValidProperty(prop)) return;
                prop.intValue = options.IndexOf(evt.newValue);
                if (prop.intValue < 0) prop.intValue = 0;
                prop.serializedObject.ApplyModifiedProperties();
            });
            return field;
        }

        /// <summary>
        /// 创建路径点坐标的紧凑 FloatField（使用 FloatField 内置 label）
        /// </summary>
        public static FloatField CreatePathCoordFloatField(string label, float value, Action<float> onValueChanged)
        {
            var field = new FloatField(label) { value = value };
            field.labelElement.style.fontSize = 9f;
            field.labelElement.style.color = new Color(0.6f, 0.8f, 1f);
            field.labelElement.style.minWidth = 14f;
            field.labelElement.style.width = 14f;
            field.labelElement.style.marginRight = 1f;
            field.RegisterValueChangedCallback(evt => onValueChanged(evt.newValue));
            return field;
        }
    }
}
#endif
