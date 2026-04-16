namespace CNoom.DOTweenVisual.Data
{
    /// <summary>
    /// 动画步骤类型
    /// </summary>
    public enum TweenStepType
    {
        /// <summary>移动动画</summary>
        Move,
        /// <summary>旋转动画</summary>
        Rotate,
        /// <summary>缩放动画</summary>
        Scale,
        /// <summary>属性动画（预留）</summary>
        Property,
        /// <summary>延迟等待</summary>
        Delay,
        /// <summary>回调调用</summary>
        Callback
    }
}
