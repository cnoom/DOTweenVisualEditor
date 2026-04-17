using System;
using UnityEngine;
using DG.Tweening;
using CNoom.DOTweenVisual.Data;

namespace CNoom.DOTweenVisual.Adapter
{
    /// <summary>
    /// DOTween 适配器实现
    /// 封装 DOTween API，支持 Free/Pro 版本
    /// </summary>
    public class DOTweenAdapter : IDOTweenAdapter
    {
        private static DOTweenAdapter _instance;
        private static readonly object _lock = new();
        public static DOTweenAdapter Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (_lock)
                    {
                        _instance ??= new DOTweenAdapter();
                    }
                }
                return _instance;
            }
        }

        private bool _initialized;

        public bool IsInitialized => _initialized;
        public bool IsProVersion => DOTweenCompatibility.IsProVersion;

        private DOTweenAdapter() { }

        public void Initialize()
        {
            if (_initialized) return;

            if (!DOTweenCompatibility.IsDOTweenAvailable)
            {
                Debug.LogError("[DOTweenVisual] DOTween 未安装！请先安装 DOTween。");
                return;
            }

            // 初始化 DOTween
            DOTween.Init(true, true, LogBehaviour.Verbose);
            _initialized = true;

            Debug.Log($"[DOTweenVisual] DOTween 初始化完成 - {DOTweenCompatibility.GetVersionInfo()}");
        }

        public TweenerAdapter CreateMoveTween(Transform target, Vector3 endValue, float duration)
        {
            if (target == null)
            {
                Debug.LogError("[DOTweenVisual] CreateMoveTween: target 为 null");
                return null;
            }
            var tweener = target.DOMove(endValue, duration).SetRecyclable(true);
            return new TweenerWrapper(tweener);
        }

        public TweenerAdapter CreateMoveTweenRelative(Transform target, Vector3 delta, float duration)
        {
            if (target == null)
            {
                Debug.LogError("[DOTweenVisual] CreateMoveTweenRelative: target 为 null");
                return null;
            }
            var tweener = target.DOMove(delta, duration).From(isRelative: true).SetRecyclable(true);
            return new TweenerWrapper(tweener);
        }

        public TweenerAdapter CreateRotateTween(Transform target, Vector3 endValue, float duration)
        {
            if (target == null)
            {
                Debug.LogError("[DOTweenVisual] CreateRotateTween: target 为 null");
                return null;
            }
            var tweener = target.DORotate(endValue, duration).SetRecyclable(true);
            return new TweenerWrapper(tweener);
        }

        public TweenerAdapter CreateScaleTween(Transform target, Vector3 endValue, float duration)
        {
            if (target == null)
            {
                Debug.LogError("[DOTweenVisual] CreateScaleTween: target 为 null");
                return null;
            }
            var tweener = target.DOScale(endValue, duration).SetRecyclable(true);
            return new TweenerWrapper(tweener);
        }

        public SequenceAdapter CreateSequence()
        {
            var sequence = DOTween.Sequence().SetRecyclable(true);
            return new SequenceWrapper(sequence);
        }

        public void ClearAll()
        {
            DOTween.Clear(true);
        }
    }

    #region Wrappers

    /// <summary>
    /// Tweener 包装器
    /// </summary>
    public class TweenerWrapper : TweenerAdapter
    {
        internal Tweener _tweener;

        public TweenerWrapper(Tweener tweener)
        {
            _tweener = tweener;
        }

        public void SetEase(Ease ease) => _tweener?.SetEase(ease);
        public void SetEase(AnimationCurve curve) => _tweener?.SetEase(curve);
        public void SetLoops(int loops, LoopType loopType) => _tweener?.SetLoops(loops, loopType);
        public void SetDelay(float delay) => _tweener?.SetDelay(delay);
        public void SetRecyclable(bool recyclable) => _tweener?.SetRecyclable(recyclable);
        public void OnComplete(Action callback) => _tweener?.OnComplete(() => callback?.Invoke());
        public void Play() => _tweener?.Play();
        public void Pause() => _tweener?.Pause();
        public void Kill(bool complete = false) => _tweener?.Kill(complete);
        public bool IsPlaying() => _tweener?.IsPlaying() ?? false;
        public bool IsComplete() => _tweener?.IsComplete() ?? true;
    }

    /// <summary>
    /// Sequence 包装器
    /// </summary>
    public class SequenceWrapper : SequenceAdapter
    {
        private Sequence _sequence;

        public SequenceWrapper(Sequence sequence)
        {
            _sequence = sequence;
        }

        public void Append(TweenerAdapter tweener)
        {
            if (tweener is TweenerWrapper wrapper)
            {
                _sequence?.Append(wrapper._tweener);
            }
        }

        public void AppendInterval(float interval) => _sequence?.AppendInterval(interval);
        public void Insert(float time, TweenerAdapter tweener) => _sequence?.Insert(time, (tweener as TweenerWrapper)?._tweener);
        public void Join(TweenerAdapter tweener) => _sequence?.Join((tweener as TweenerWrapper)?._tweener);
        public void SetLoops(int loops, LoopType loopType) => _sequence?.SetLoops(loops, loopType);
        public void SetRecyclable(bool recyclable) => _sequence?.SetRecyclable(recyclable);
        public void OnComplete(Action callback) => _sequence?.OnComplete(() => callback?.Invoke());
        public void Play() => _sequence?.Play();
        public void Pause() => _sequence?.Pause();
        public void Kill(bool complete = false) => _sequence?.Kill(complete);
        public bool IsPlaying() => _sequence?.IsPlaying() ?? false;
        public bool IsComplete() => _sequence?.IsComplete() ?? true;
    }

    #endregion
}
