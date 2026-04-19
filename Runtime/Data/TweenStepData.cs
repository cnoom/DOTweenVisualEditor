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

        [Tooltip("移动坐标空间（Move 使用）")]
        public MoveSpace MoveSpace = MoveSpace.World;

        [Tooltip("旋转坐标空间（Rotate 使用）")]
        public RotateSpace RotateSpace = RotateSpace.World;

        [Tooltip("冲击属性目标（Punch 使用）")]
        public PunchTarget PunchTarget = PunchTarget.Position;

        [Tooltip("震动属性目标（Shake 使用）")]
        public ShakeTarget ShakeTarget = ShakeTarget.Position;

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

        [Tooltip("目标浮点值（透明度/填充量等）")]
        [Range(0f, 1f)]
        public float TargetFloat = 0f;

        #endregion

        #region 特效参数值组

        [Tooltip("跳跃高度（Jump 使用）")]
        public float JumpHeight = 1f;

        [Tooltip("跳跃次数（Jump 使用）")]
        [Min(1)]
        public int JumpNum = 1;

        [Tooltip("冲击/震动强度（Punch/Shake 使用）")]
        public Vector3 Intensity = Vector3.one;

        [Tooltip("震动随机性（Shake 使用，0~90）")]
        [Range(0f, 90f)]
        public float ShakeRandomness = 90f;

        [Tooltip("弹性震荡次数（Punch/Shake 使用）")]
        [Min(1)]
        public int Vibrato = 10;

        [Tooltip("弹性回弹程度（Punch/Shake 使用，0~1）")]
        [Range(0f, 1f)]
        public float Elasticity = 1f;

        #endregion

        #region 路径动画值组

        [Tooltip("路径点列表（DOPath 使用）")]
        public Vector3[] PathWaypoints = new Vector3[] { new Vector3(1f, 0f, 0f), new Vector3(2f, 1f, 0f) };

        [Tooltip("路径类型（Linear/CatmullRom/CubicBezier）")]
        public int PathType = 0;

        [Tooltip("路径模式（3D/TopDown2D/SideScroll2D）")]
        public int PathMode = 0;

        [Tooltip("路径分辨率（CatmullRom 时有效）")]
        [Min(1)]
        public int PathResolution = 10;

        [Tooltip("路径颜色（仅编辑器调试）")]
        public Color PathGizmoColor = new Color(1f, 0.5f, 0f);

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
    }
}
