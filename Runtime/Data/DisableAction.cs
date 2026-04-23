namespace CNoom.DOTweenVisual.Data
{
    /// <summary>
    /// 禁用时的动画行为
    /// </summary>
    public enum DisableAction
    {
        /// <summary>暂停动画（重新启用后根据 PlayTrigger 决定行为）</summary>
        Pause,
        /// <summary>停止并回滚动画到初始状态</summary>
        Stop,
        /// <summary>不做处理（动画继续运行）</summary>
        None,
    }
}
