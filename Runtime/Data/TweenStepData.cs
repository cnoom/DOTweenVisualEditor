using System;
using UnityEngine;
using UnityEngine.Events;
using DG.Tweening;

namespace CNoom.DOTweenVisual.Data
{
    /// <summary>
    /// 动画步骤数据
    /// 使用多值组方案：不同类型步骤使用对应值组，PropertyDrawer 按类型过滤显示
    /// </summary>
    [Serializable]
    public class TweenStepData
    {
        #region 基本信息

        [Tooltip("是否启用此步骤")]
        public bool IsEnabled = true;

        #endregion

        #region 动画类型

        [Tooltip("动画类型")]
        public TweenStepType Type = TweenStepType.Move;

        #endregion

        #region 时长控制

        [Tooltip("动画时长（秒）")]
        [Min(0.001f)]
        public float Duration = 1f;

        [Tooltip("延迟时间（秒）")]
        [Min(0f)]
        public float Delay = 0f;

        #endregion

        #region 缓动曲线

        [Tooltip("缓动类型")]
        public Ease Ease = Ease.OutQuad;

        [Tooltip("使用自定义曲线")]
        public bool UseCustomCurve = false;

        [Tooltip("自定义动画曲线")]
        public AnimationCurve CustomCurve;

        #endregion

        #region Transform 值组

        [Tooltip("目标物体（为null时使用组件所在物体）")]
        public Transform TargetTransform = null;

        [Tooltip("Transform 目标类型")]
        public TransformTarget TransformTarget = TransformTarget.Position;

        [Tooltip("是否使用起始值（为false时使用物体当前值）")]
        public bool UseStartValue = false;

        [Tooltip("起始值（Move/Rotate/Scale 使用）")]
        public Vector3 StartVector = Vector3.zero;

        [Tooltip("目标值（Move/Rotate/Scale 使用，旋转以欧拉角编辑内部转四元数）")]
        public Vector3 TargetVector = Vector3.zero;

        [Tooltip("是否为相对值")]
        public bool IsRelative = false;

        #endregion

        #region Color 值组

        [Tooltip("是否使用起始颜色（为false时使用物体当前颜色）")]
        public bool UseStartColor = false;

        [Tooltip("起始颜色")]
        public Color StartColor = Color.white;

        [Tooltip("目标颜色")]
        public Color TargetColor = Color.white;

        #endregion

        #region Float 值组

        [Tooltip("是否使用起始浮点值（为false时使用物体当前值）")]
        public bool UseStartFloat = false;

        [Tooltip("起始浮点值（透明度等）")]
        [Range(0f, 1f)]
        public float StartFloat = 1f;

        [Tooltip("目标浮点值（透明度等）")]
        [Range(0f, 1f)]
        public float TargetFloat = 0f;

        #endregion

        #region 执行模式

        [Tooltip("执行模式")]
        public ExecutionMode ExecutionMode = ExecutionMode.Append;

        [Tooltip("插入时间点（仅 Insert 模式有效）")]
        [Min(0f)]
        public float InsertTime = 0f;

        #endregion

        #region 回调

        [Tooltip("步骤完成回调")]
        public UnityEvent OnComplete = new UnityEvent();

        #endregion

        #region 兼容性属性

        /// <summary>
        /// 向后兼容：获取目标值
        /// Move/Rotate/Scale → TargetVector
        /// Fade → TargetFloat 的 Vector3 包装
        /// </summary>
        [Obsolete("请使用对应值组字段（TargetVector/TargetColor/TargetFloat）")]
        public Vector3 TargetValue
        {
            get => TargetVector;
            set => TargetVector = value;
        }

        #endregion
    }
}
