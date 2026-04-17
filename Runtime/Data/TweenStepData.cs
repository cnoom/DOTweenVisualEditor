using System;
using UnityEngine;
using UnityEngine.Events;
using DG.Tweening;

namespace CNoom.DOTweenVisual.Data
{
    /// <summary>
    /// 动画步骤数据
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
        [Min(0f)]
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

        #region Transform 相关参数

        [Tooltip("目标物体（为null时使用组件所在物体）")]
        public Transform TargetTransform = null;

        [Tooltip("Transform 目标类型")]
        public TransformTarget TransformTarget = TransformTarget.Position;

        [Tooltip("目标值")]
        public Vector3 TargetValue = Vector3.zero;

        [Tooltip("是否为相对值")]
        public bool IsRelative = false;

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

        #region 预留字段（Property 动画用）

        [Tooltip("属性名称")]
        public string PropertyName = "";

        [Tooltip("属性起始值")]
        public float PropertyValueFrom = 0f;

        [Tooltip("属性目标值")]
        public float PropertyValueTo = 1f;

        #endregion
    }
}
