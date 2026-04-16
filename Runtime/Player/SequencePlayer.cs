using System;
using System.Collections.Generic;
using UnityEngine;
using CNoom.DOTweenVisual.Data;
using CNoom.DOTweenVisual.Adapter;

namespace CNoom.DOTweenVisual.Player
{
    /// <summary>
    /// 序列播放器
    /// 管理整个动画序列的播放
    /// </summary>
    public class SequencePlayer : IDisposable
    {
        private readonly AnimationSequenceAsset _sequenceAsset;
        private readonly IDOTweenAdapter _adapter;
        private readonly Dictionary<Transform, List<ClipPlayer>> _clipPlayers = new();
        private SequenceAdapter _mainSequence;

        private PlaybackState _state = PlaybackState.Stopped;
        private float _currentTime;

        public event Action<PlaybackEventArgs> OnStateChanged;
        public event Action<ClipData> OnClipStart;
        public event Action<ClipData> OnClipComplete;

        public AnimationSequenceAsset SequenceAsset => _sequenceAsset;
        public PlaybackState State => _state;
        public float CurrentTime => _currentTime;
        public float TotalDuration => _sequenceAsset?.CalculateTotalDuration() ?? 0f;

        public SequencePlayer(AnimationSequenceAsset sequenceAsset, IDOTweenAdapter adapter = null)
        {
            _sequenceAsset = sequenceAsset;
            _adapter = adapter ?? DOTweenAdapter.Instance;
        }

        /// <summary>
        /// 播放序列
        /// </summary>
        public void Play()
        {
            if (_state == PlaybackState.Playing) return;

            if (!_adapter.IsInitialized)
            {
                _adapter.Initialize();
            }

            KillAllClips();
            BuildSequence();

            _mainSequence?.Play();
            _state = PlaybackState.Playing;
            OnStateChanged?.Invoke(new PlaybackEventArgs(_state, _currentTime, TotalDuration));
        }

        /// <summary>
        /// 暂停序列
        /// </summary>
        public void Pause()
        {
            if (_state != PlaybackState.Playing) return;

            _mainSequence?.Pause();
            _state = PlaybackState.Paused;
            OnStateChanged?.Invoke(new PlaybackEventArgs(_state, _currentTime, TotalDuration));
        }

        /// <summary>
        /// 停止序列
        /// </summary>
        public void Stop()
        {
            if (_state == PlaybackState.Stopped) return;

            KillAllClips();
            _currentTime = 0f;
            _state = PlaybackState.Stopped;
            OnStateChanged?.Invoke(new PlaybackEventArgs(_state, _currentTime, TotalDuration));
        }

        /// <summary>
        /// 注册目标对象
        /// </summary>
        public void RegisterTarget(string trackName, Transform target)
        {
            if (target == null || string.IsNullOrEmpty(trackName)) return;

            // 找到对应轨道
            var track = _sequenceAsset?.Tracks.Find(t => t.TrackName == trackName);
            if (track == null) return;

            if (!_clipPlayers.ContainsKey(target))
            {
                _clipPlayers[target] = new List<ClipPlayer>();
            }

            // 为每个片段创建 ClipPlayer
            foreach (var clip in track.Clips)
            {
                var player = new ClipPlayer(clip, target, _adapter);
                player.OnClipStart += OnClipStart;
                player.OnClipComplete += OnClipComplete;
                _clipPlayers[target].Add(player);
            }
        }

        /// <summary>
        /// 注销目标对象
        /// </summary>
        public void UnregisterTarget(Transform target)
        {
            if (!_clipPlayers.TryGetValue(target, out var players)) return;

            foreach (var player in players)
            {
                player.Dispose();
            }
            players.Clear();
            _clipPlayers.Remove(target);
        }

        private void BuildSequence()
        {
            _mainSequence = _adapter.CreateSequence();

            // 按 Track 组织片段
            foreach (var kvp in _clipPlayers)
            {
                foreach (var clipPlayer in kvp.Value)
                {
                    // 使用 Insert 在正确时间点插入
                    // 注意：这里需要实际创建 Tween，简化实现
                }
            }

            // 设置循环
            if (_sequenceAsset != null && _sequenceAsset.Loops != 1)
            {
                _mainSequence?.SetLoops(_sequenceAsset.Loops, _sequenceAsset.LoopType);
            }

            _mainSequence?.OnComplete(OnSequenceComplete);
        }

        private void OnSequenceComplete()
        {
            _state = PlaybackState.Stopped;
            _currentTime = TotalDuration;
            OnStateChanged?.Invoke(new PlaybackEventArgs(_state, _currentTime, TotalDuration));
        }

        private void KillAllClips()
        {
            _mainSequence?.Kill();

            foreach (var players in _clipPlayers.Values)
            {
                foreach (var player in players)
                {
                    player.Stop();
                }
            }
        }

        public void Dispose()
        {
            Stop();

            foreach (var players in _clipPlayers.Values)
            {
                foreach (var player in players)
                {
                    player.Dispose();
                }
            }
            _clipPlayers.Clear();
        }
    }
}
