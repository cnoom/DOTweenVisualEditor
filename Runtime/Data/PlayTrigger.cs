namespace CNoom.DOTweenVisual.Data
{
    /// <summary>
    /// 动画播放触发时机
    /// </summary>
    public enum PlayTrigger
    {
        /// <summary>手动播放，需调用 Play()</summary>
        Manual,
        /// <summary>在 Awake() 中自动播放</summary>
        OnAwake,
        /// <summary>在 Start() 中自动播放</summary>
        OnStart,
        /// <summary>每次 OnEnable() 时从头播放</summary>
        OnEnableRestart,
        /// <summary>每次 OnEnable() 时继续播放</summary>
        OnEnableResume,
    }
}
