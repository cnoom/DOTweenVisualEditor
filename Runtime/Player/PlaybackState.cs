namespace CNoom.DOTweenVisual.Player
{
    /// <summary>
    /// 播放状态
    /// </summary>
    public enum PlaybackState
    {
        /// <summary>停止</summary>
        Stopped,
        /// <summary>播放中</summary>
        Playing,
        /// <summary>暂停</summary>
        Paused
    }

    /// <summary>
    /// 播放事件参数
    /// </summary>
    public struct PlaybackEventArgs
    {
        public PlaybackState State;
        public float CurrentTime;
        public float TotalDuration;

        public PlaybackEventArgs(PlaybackState state, float currentTime, float totalDuration)
        {
            State = state;
            CurrentTime = currentTime;
            TotalDuration = totalDuration;
        }
    }
}
