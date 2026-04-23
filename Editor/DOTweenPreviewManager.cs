#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using DG.DOTweenEditor;
using CNoom.DOTweenVisual.Components;
using CNoom.DOTweenVisual.Data;

namespace CNoom.DOTweenVisual.Editor
{
    /// <summary>
    /// 预览状态管理器
    /// 负责编辑器预览的生命周期管理、状态保存与恢复
    /// 与 EditorWindow 解耦，通过 StateChanged 事件通知 UI 更新
    /// </summary>
    internal class DOTweenPreviewManager : IDisposable
    {
        #region 类型定义

        /// <summary>
        /// 预览状态
        /// </summary>
        public enum PreviewState
        {
            None,
            Playing,
            Paused,
            Completed
        }

        /// <summary>
        /// Transform 快照状态，用于预览前保存和预览后恢复
        /// </summary>
        private struct TransformSnapshot
        {
            public Vector3 Position;
            public Quaternion Rotation;
            public Vector3 LocalPosition;
            public Quaternion LocalRotation;
            public Vector3 LocalScale;
            public Color Color;
            public float Alpha;
            public Vector2 AnchoredPosition;
            public Vector2 SizeDelta;
            public float FillAmount;
        }

        #endregion

        #region 公共属性

        /// <summary>当前预览状态</summary>
        public PreviewState State { get; private set; } = PreviewState.None;

        /// <summary>预览 Sequence（用于时间显示等）</summary>
        public Sequence PreviewSequence => _previewSequence;

        /// <summary>是否正在播放</summary>
        public bool IsPlaying => State == PreviewState.Playing;

        /// <summary>是否暂停</summary>
        public bool IsPaused => State == PreviewState.Paused;

        #endregion

        #region 事件

        /// <summary>
        /// 预览状态变更事件，EditorWindow 订阅此事件更新 UI
        /// </summary>
        public event Action StateChanged;

        /// <summary>
        /// 预览进度更新事件，参数为归一化进度 (0~1)
        /// </summary>
        public event Action<float> ProgressUpdated;

        #endregion

        #region 私有字段

        private DOTweenVisualPlayer _targetPlayer;
        private Sequence _previewSequence;
        private readonly Dictionary<Transform, TransformSnapshot> _snapshots = new();

        #endregion

        #region 公共方法

        /// <summary>
        /// 设置目标播放器
        /// </summary>
        public void SetTarget(DOTweenVisualPlayer player)
        {
            if (State != PreviewState.None)
            {
                Reset();
            }
            _targetPlayer = player;
        }

        /// <summary>
        /// 开始预览
        /// </summary>
        public void StartPreview()
        {
            if (_targetPlayer == null || _targetPlayer.StepCount == 0) return;

            // 确保清理旧的预览状态
            CleanupSequence();
            // 仅在之前有预览状态时才调用 Stop，避免清除 DOTween 初始化后的内部回调
            // Stop() 文档注明会 "clears any callback"，域重载后 DOTween 刚初始化，
            // 此时调用 Stop() 可能破坏 DOTween 内部状态，导致后续预览卡顿
            if (State != PreviewState.None)
            {
                DOTweenEditorPreview.Stop();
            }

            if (_snapshots.Count > 0)
            {
                RestoreSnapshots();
            }
            else
            {
                SaveSnapshots();
            }

            DOTweenEditorPreview.Start();

            try
            {
                _previewSequence = DOTween.Sequence();
                BuildPreviewSequence();
                DOTweenEditorPreview.PrepareTweenForPreview(_previewSequence);

                _previewSequence.OnComplete(() =>
                {
                    State = PreviewState.Completed;
                    StateChanged?.Invoke();
                });

                _previewSequence.OnUpdate(() =>
                {
                    if (_previewSequence != null)
                    {
                        float elapsed = _previewSequence.Elapsed(false);
                        float total = Mathf.Max(0.001f, _previewSequence.Duration(false));
                        ProgressUpdated?.Invoke(Mathf.Clamp01(elapsed / total));
                    }
                });

                _previewSequence.Play();
                State = PreviewState.Playing;
                StateChanged?.Invoke();
            }
            catch (Exception e)
            {
                DOTweenLog.Error(string.Format(L10n.Tr("Preview/StartFailed"), e.Message, e.StackTrace));
                DOTweenEditorPreview.Stop();
                CleanupSequence();
                RestoreSnapshots();
                State = PreviewState.None;
                StateChanged?.Invoke();
            }
        }

        /// <summary>
        /// 暂停预览
        /// </summary>
        public void PausePreview()
        {
            if (_previewSequence != null && _previewSequence.IsPlaying())
            {
                _previewSequence.Pause();
                State = PreviewState.Paused;
                StateChanged?.Invoke();
            }
        }

        /// <summary>
        /// 恢复预览
        /// </summary>
        public void ResumePreview()
        {
            if (_previewSequence != null && !_previewSequence.IsPlaying())
            {
                _previewSequence.Play();
                State = PreviewState.Playing;
                StateChanged?.Invoke();
            }
        }

        /// <summary>
        /// 停止预览：停止动画 + 恢复初始状态
        /// </summary>
        public void StopPreview()
        {
            CleanupSequence();
            DOTweenEditorPreview.Stop();
            RestoreSnapshots();
            State = PreviewState.None;
            StateChanged?.Invoke();
        }

        /// <summary>
        /// 重置预览：停止动画 + 恢复初始状态
        /// </summary>
        public void Reset()
        {
            CleanupSequence();
            DOTweenEditorPreview.Stop();
            RestoreSnapshots();
            State = PreviewState.None;
            _snapshots.Clear();
            StateChanged?.Invoke();
        }

        /// <summary>
        /// 重播：恢复状态后重新开始
        /// </summary>
        public void Replay()
        {
            RestoreSnapshots();
            StartPreview();
        }

        /// <summary>
        /// 释放资源
        /// </summary>
        public void Dispose()
        {
            Reset();
            _targetPlayer = null;
        }

        #endregion

        #region 私有方法 - 序列构建

        private void BuildPreviewSequence()
        {
            foreach (var step in _targetPlayer.Steps)
            {
                if (!step.IsEnabled) continue;
                TweenFactory.AppendToSequence(_previewSequence, step, _targetPlayer.transform);
            }
        }

        #endregion

        #region 私有方法 - 状态快照

        private void SaveSnapshots()
        {
            _snapshots.Clear();
            SaveTransformSnapshot(_targetPlayer.transform);

            foreach (var step in _targetPlayer.Steps)
            {
                if (step.TargetTransform != null)
                {
                    SaveTransformSnapshot(step.TargetTransform);
                }
            }
        }

        private void SaveTransformSnapshot(Transform target)
        {
            if (target == null || _snapshots.ContainsKey(target)) return;

            TweenValueHelper.TryGetColor(target, out var color);
            TweenValueHelper.TryGetAlpha(target, out var alpha);

            Vector2 anchoredPos = Vector2.zero;
            Vector2 sizeDelta = Vector2.zero;
            float fillAmount = 0f;

            if (TweenValueHelper.TryGetRectTransform(target, out var rectTransform))
            {
                anchoredPos = rectTransform.anchoredPosition;
                sizeDelta = rectTransform.sizeDelta;
            }

            var image = target.GetComponent<Image>();
            if (image != null)
            {
                fillAmount = image.fillAmount;
            }

            _snapshots[target] = new TransformSnapshot
            {
                Position = target.position,
                Rotation = target.rotation,
                LocalPosition = target.localPosition,
                LocalRotation = target.localRotation,
                LocalScale = target.localScale,
                Color = color,
                Alpha = alpha,
                AnchoredPosition = anchoredPos,
                SizeDelta = sizeDelta,
                FillAmount = fillAmount
            };
        }

        private void RestoreSnapshots()
        {
            foreach (var kvp in _snapshots)
            {
                var target = kvp.Key;
                var snapshot = kvp.Value;

                try
                {
                    if (target == null || target.gameObject == null) continue;

                    Undo.RecordObject(target, L10n.Tr("Undo/ResetPreviewState"));
                    target.position = snapshot.Position;
                    target.rotation = snapshot.Rotation;
                    target.localPosition = snapshot.LocalPosition;
                    target.localRotation = snapshot.LocalRotation;
                    target.localScale = snapshot.LocalScale;

                    TweenValueHelper.TrySetColor(target, snapshot.Color);
                    TweenValueHelper.TrySetAlpha(target, snapshot.Alpha);

                    if (TweenValueHelper.TryGetRectTransform(target, out var rectTransform))
                    {
                        rectTransform.anchoredPosition = snapshot.AnchoredPosition;
                        rectTransform.sizeDelta = snapshot.SizeDelta;
                    }

                    var image = target.GetComponent<Image>();
                    if (image != null)
                    {
                        image.fillAmount = snapshot.FillAmount;
                    }
                }
                catch (MissingReferenceException) { }
            }

            _snapshots.Clear();
        }

        #endregion

        #region 私有方法 - 工具

        private void CleanupSequence()
        {
            if (_previewSequence != null)
            {
                // 先 Rewind 回滚属性到动画开始前的状态，避免 Kill 时残留中间值
                if (_previewSequence.IsActive())
                {
                    _previewSequence.Rewind();
                }
                _previewSequence.Kill();
                _previewSequence = null;
            }
        }

        #endregion
    }
}
#endif
