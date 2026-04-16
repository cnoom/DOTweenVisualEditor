using System;
using System.Collections.Generic;
using UnityEngine;
using CNoom.DOTweenVisual.Data;
using CNoom.DOTweenVisual.Adapter;
using CNoom.DOTweenVisual.Pool;

namespace CNoom.DOTweenVisual.Player
{
    /// <summary>
    /// 动画播放器组件
    /// 挂载在 GameObject 上，引用 AnimationSequenceAsset 进行播放
    /// </summary>
    public class AnimationPlayer : MonoBehaviour
    {
        [Header("配置")]
        [Tooltip("动画序列资源")]
        [SerializeField] private AnimationSequenceAsset _sequenceAsset;

        [Tooltip("播放设置")]
        [SerializeField] private PlaySettings _playSettings = new();

        [Header("目标绑定")]
        [Tooltip("目标对象映射（轨道名称 -> 目标 Transform）")]
        [SerializeField] private List<TargetBinding> _targetBindings = new();

        private SequencePlayer _sequencePlayer;
        private PlaybackState _state = PlaybackState.Stopped;

        #region 公开属性

        public AnimationSequenceAsset SequenceAsset => _sequenceAsset;
        public PlaybackState State => _state;
        public float CurrentTime => _sequencePlayer?.CurrentTime ?? 0f;
        public float TotalDuration => _sequenceAsset?.CalculateTotalDuration() ?? 0f;

        #endregion

        #region 事件

        public event Action<PlaybackEventArgs> OnStateChanged;
        public event Action<ClipData> OnClipStart;
        public event Action<ClipData> OnClipComplete;

        #endregion

        #region Unity 生命周期

        private void Awake()
        {
            InitializePlayer();
        }

        private void OnDestroy()
        {
            DisposePlayer();
        }

        #endregion

        #region 公开方法

        /// <summary>
        /// 播放动画
        /// </summary>
        public void Play()
        {
            _sequencePlayer?.Play();
        }

        /// <summary>
        /// 暂停动画
        /// </summary>
        public void Pause()
        {
            _sequencePlayer?.Pause();
        }

        /// <summary>
        /// 停止动画
        /// </summary>
        public void Stop()
        {
            _sequencePlayer?.Stop();
        }

        /// <summary>
        /// 设置序列资源
        /// </summary>
        public void SetSequence(AnimationSequenceAsset sequenceAsset)
        {
            _sequenceAsset = sequenceAsset;
            InitializePlayer();
        }

        /// <summary>
        /// 添加目标绑定
        /// </summary>
        public void AddTargetBinding(string trackName, Transform target)
        {
            _targetBindings.Add(new TargetBinding
            {
                TrackName = trackName,
                Target = target
            });

            _sequencePlayer?.RegisterTarget(trackName, target);
        }

        #endregion

        #region 私有方法

        private void InitializePlayer()
        {
            DisposePlayer();

            if (_sequenceAsset == null) return;

            _sequencePlayer = new SequencePlayer(_sequenceAsset);

            // 订阅事件
            _sequencePlayer.OnStateChanged += HandleStateChanged;
            _sequencePlayer.OnClipStart += HandleClipStart;
            _sequencePlayer.OnClipComplete += HandleClipComplete;

            // 注册目标绑定
            foreach (var binding in _targetBindings)
            {
                _sequencePlayer.RegisterTarget(binding.TrackName, binding.Target);
            }

            // 自动播放
            if (_playSettings.PlayOnStart)
            {
                Play();
            }
        }

        private void DisposePlayer()
        {
            if (_sequencePlayer != null)
            {
                _sequencePlayer.OnStateChanged -= HandleStateChanged;
                _sequencePlayer.OnClipStart -= HandleClipStart;
                _sequencePlayer.OnClipComplete -= HandleClipComplete;
                _sequencePlayer.Dispose();
                _sequencePlayer = null;
            }
        }

        private void HandleStateChanged(PlaybackEventArgs args)
        {
            _state = args.State;
            OnStateChanged?.Invoke(args);
        }

        private void HandleClipStart(ClipData clip)
        {
            OnClipStart?.Invoke(clip);
        }

        private void HandleClipComplete(ClipData clip)
        {
            OnClipComplete?.Invoke(clip);
        }

        #endregion
    }

    #region 辅助类型

    /// <summary>
    /// 播放设置
    /// </summary>
    [Serializable]
    public class PlaySettings
    {
        [Tooltip("开始时自动播放")]
        public bool PlayOnStart;
    }

    /// <summary>
    /// 目标绑定
    /// </summary>
    [Serializable]
    public class TargetBinding
    {
        [Tooltip("轨道名称")]
        public string TrackName;

        [Tooltip("目标 Transform")]
        public Transform Target;
    }

    #endregion
}
