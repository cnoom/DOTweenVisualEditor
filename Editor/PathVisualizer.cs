#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using CNoom.DOTweenVisual.Data;
using DG.Tweening;

namespace CNoom.DOTweenVisual.Editor
{
    /// <summary>
    /// DOPath 路径可视化器
    /// 在 SceneView 中绘制路径曲线，支持拖拽编辑路径点
    /// 使用 DOTween 内部 Path 类（反射）保证可视化路径与运行时路径完全一致
    /// </summary>
    internal class PathVisualizer : IDisposable
    {
        #region 常量

        private const float WaypointHandleSize = 0.08f;
        private const float StartPointHandleSize = 0.1f;
        private const int ArrowDensity = 5;
        private const string ShowInPlayModeKey = "DOTweenVisualEditor_ShowPathInPlayMode";

        /// <summary>
        /// 是否在 Play Mode 下显示路径可视化（默认关闭）
        /// </summary>
        public static bool ShowPathInPlayMode
        {
            get => EditorPrefs.GetBool(ShowInPlayModeKey, true);
            set => EditorPrefs.SetBool(ShowInPlayModeKey, value);
        }

        #endregion

        #region DOTween Path 反射辅助

        /// <summary>
        /// 通过反射访问 DOTween 内部 Path 类，确保路径计算与运行时完全一致
        /// </summary>
        private static class DotweenPathHelper
        {
            private static readonly System.Type PathType;
            private static readonly MethodInfo FinalizePathMethod;
            private static readonly MethodInfo GetPointMethod;
            private static readonly FieldInfo AddedExtraStartWpField;
            private static readonly MethodInfo DrawMethod;
            private static readonly PropertyInfo GizmosDelegatesProperty;
            public static readonly bool IsAvailable;

            static DotweenPathHelper()
            {
                try
                {
                    PathType = typeof(DG.Tweening.PathType).Assembly.GetType(
                        "DG.Tweening.Plugins.Core.PathCore.Path");
                    if (PathType == null) return;

                    FinalizePathMethod = PathType.GetMethod("FinalizePath",
                        BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                    GetPointMethod = PathType.GetMethod("GetPoint",
                        BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                    AddedExtraStartWpField = PathType.GetField("addedExtraStartWp",
                        BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                    DrawMethod = PathType.GetMethod("Draw",
                        BindingFlags.Instance | BindingFlags.NonPublic);

                    // GizmosDelegates 是 internal static 属性，通过反射获取
                    GizmosDelegatesProperty = typeof(DG.Tweening.DOTween).GetProperty(
                        "GizmosDelegates",
                        BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);

                    IsAvailable = FinalizePathMethod != null
                                  && GetPointMethod != null
                                  && DrawMethod != null;
                }
                catch
                {
                    IsAvailable = false;
                }
            }

            public static object CreatePath(DG.Tweening.PathType pathType, Vector3[] waypoints, int resolution)
            {
                return Activator.CreateInstance(PathType, pathType, waypoints, resolution, (Color?)null);
            }

            public static void Finalize(object path, Vector3 start)
            {
                AddedExtraStartWpField?.SetValue(path, true);
                FinalizePathMethod.Invoke(path,
                    new object[] { false, DG.Tweening.AxisConstraint.None, start });
            }

            public static Vector3 GetPoint(object path, float perc)
            {
                return (Vector3)GetPointMethod.Invoke(path, new object[] { perc, false });
            }

            public static void RemoveGizmoDelegate(object path)
            {
                if (path == null || DrawMethod == null || GizmosDelegatesProperty == null) return;
                try
                {
                    var del = Delegate.CreateDelegate(typeof(Action), path, DrawMethod);
                    var delegates = GizmosDelegatesProperty.GetValue(null) as IList;
                    if (delegates != null) delegates.Remove(del);
                }
                catch
                {
                    // ignored
                }
            }
        }

        #endregion

        #region 数据

        private bool _isEnabled;
        private bool _isPreviewing;
        private Vector3 _cachedStartPos;
        private bool _hasCachedStartPos;
        private SerializedProperty _stepProperty;
        private Transform _targetTransform;
        private Func<Vector3> _getStartPosition;
        private Action _onPathDataChanged;

        #endregion

        #region 生命周期

        public void Enable()
        {
            if (_isEnabled) return;
            _isEnabled = true;
            SceneView.duringSceneGui += OnSceneGUI;
        }

        public void Disable()
        {
            if (!_isEnabled) return;
            _isEnabled = false;
            SceneView.duringSceneGui -= OnSceneGUI;
        }

        public void Dispose()
        {
            Disable();
            _stepProperty = null;
            _targetTransform = null;
            _getStartPosition = null;
            _onPathDataChanged = null;
        }

        /// <summary>
        /// 设置当前步骤数据
        /// </summary>
        public void SetData(
            SerializedProperty stepProperty,
            Transform targetTransform,
            Func<Vector3> getStartPosition,
            Action onPathDataChanged)
        {
            _stepProperty = stepProperty;
            _targetTransform = targetTransform;
            _getStartPosition = getStartPosition;
            _onPathDataChanged = onPathDataChanged;
            RefreshCachedStartPosition();
            SceneView.RepaintAll();
        }

        /// <summary>
        /// 设置预览状态，预览期间冻结起始位置避免路径跟随物体移动
        /// </summary>
        public void SetPreviewing(bool isPreviewing)
        {
            if (_isPreviewing == isPreviewing) return;
            _isPreviewing = isPreviewing;

            if (!_isPreviewing)
            {
                // 预览结束时刷新起始位置
                RefreshCachedStartPosition();
                SceneView.RepaintAll();
            }
        }

        /// <summary>
        /// 通知路径数据已更新（外部修改路径点后调用）
        /// </summary>
        public void NotifyDataChanged()
        {
            if (!_isPreviewing) RefreshCachedStartPosition();
            SceneView.RepaintAll();
        }

        private void RefreshCachedStartPosition()
        {
            if (_getStartPosition != null)
            {
                _cachedStartPos = _getStartPosition();
                _hasCachedStartPos = true;
            }
        }

        #endregion

        #region SceneView 绘制

        private void OnSceneGUI(SceneView sceneView)
        {
            if (!_isEnabled || _stepProperty == null) return;

            // Play Mode 下默认不显示路径可视化（可通过菜单勾选开启）
            if (EditorApplication.isPlaying && !ShowPathInPlayMode) return;

            var type = (TweenStepType)_stepProperty.FindPropertyRelative("Type").enumValueIndex;
            if (type != TweenStepType.DOPath) return;

            if (_targetTransform == null) return;

            var waypointsProp = _stepProperty.FindPropertyRelative("PathWaypoints");
            if (waypointsProp == null || !waypointsProp.isArray || waypointsProp.arraySize == 0) return;

            int pathType = _stepProperty.FindPropertyRelative("PathType").intValue;
            int resolution = _stepProperty.FindPropertyRelative("PathResolution").intValue;
            var gizmoColorProp = _stepProperty.FindPropertyRelative("PathGizmoColor");
            Color pathColor = gizmoColorProp != null ? gizmoColorProp.colorValue : new Color(1f, 0.5f, 0f);

            Vector3[] waypoints = new Vector3[waypointsProp.arraySize];
            for (int i = 0; i < waypoints.Length; i++)
            {
                waypoints[i] = waypointsProp.GetArrayElementAtIndex(i).vector3Value;
            }

            Vector3 startPos = _hasCachedStartPos ? _cachedStartPos : _targetTransform.position;

            // 1. 计算完整路径点
            List<Vector3> fullPath = ComputePath(startPos, waypoints, pathType, resolution);

            // 2. 绘制路径曲线
            DrawPathCurve(fullPath, pathColor);

            // 3. 绘制起始点
            DrawStartPoint(startPos);

            // 4. 绘制路径点 + 拖拽 Handles
            DrawWaypointHandles(waypoints, waypointsProp, pathColor);

            // 5. 绘制方向箭头
            DrawDirectionArrows(fullPath, pathColor);
        }

        #endregion

        #region 路径曲线绘制

        private void DrawPathCurve(List<Vector3> points, Color color)
        {
            if (points == null || points.Count < 2) return;

            Handles.color = color;
            Handles.DrawPolyLine(points.ToArray());
        }

        private void DrawStartPoint(Vector3 position)
        {
            Handles.color = Color.green;
            float size = HandleUtility.GetHandleSize(position) * StartPointHandleSize;
            Handles.SphereHandleCap(0, position, Quaternion.identity, size, EventType.Repaint);

            var labelStyle = new GUIStyle(EditorStyles.label)
            {
                normal = { textColor = Color.green },
                fontSize = 10,
                alignment = TextAnchor.LowerCenter
            };
            Handles.Label(position + Vector3.up * size * 2f, "Start", labelStyle);
        }

        private void DrawWaypointHandles(Vector3[] waypoints, SerializedProperty waypointsProp, Color color)
        {
            for (int i = 0; i < waypoints.Length; i++)
            {
                Vector3 wp = waypoints[i];
                float handleSize = HandleUtility.GetHandleSize(wp) * WaypointHandleSize;

                Handles.color = color;
                Handles.SphereHandleCap(0, wp, Quaternion.identity, handleSize, EventType.Repaint);

                EditorGUI.BeginChangeCheck();
                Vector3 newPos = Handles.FreeMoveHandle(wp, handleSize, Vector3.zero, Handles.SphereHandleCap);
                if (EditorGUI.EndChangeCheck())
                {
                    var targetObj = waypointsProp.serializedObject.targetObject;
                    Undo.RecordObject(targetObj, "Move Waypoint");
                    waypointsProp.serializedObject.Update();
                    waypointsProp.GetArrayElementAtIndex(i).vector3Value = newPos;
                    waypointsProp.serializedObject.ApplyModifiedProperties();
                    _onPathDataChanged?.Invoke();
                    SceneView.RepaintAll();
                }

                var labelStyle = new GUIStyle(EditorStyles.label)
                {
                    normal = { textColor = new Color(1f, 0.85f, 0.5f) },
                    fontSize = 10,
                    alignment = TextAnchor.LowerCenter
                };
                Handles.Label(newPos + Vector3.up * handleSize * 2f, $"WP{i + 1}", labelStyle);
            }
        }

        private void DrawDirectionArrows(List<Vector3> points, Color color)
        {
            if (points == null || points.Count < 2) return;

            Handles.color = new Color(color.r, color.g, color.b, 0.7f);

            int step = Mathf.Max(1, points.Count / ArrowDensity);
            for (int i = step; i < points.Count - 1; i += step)
            {
                Vector3 from = points[i];
                Vector3 to = points[Mathf.Min(i + 1, points.Count - 1)];
                Vector3 dir = (to - from).normalized;

                if (dir.sqrMagnitude < 0.001f) continue;

                float arrowSize = HandleUtility.GetHandleSize(from) * 0.05f;
                Vector3 right = Vector3.Cross(dir, Vector3.up).normalized;
                if (right.sqrMagnitude < 0.001f)
                    right = Vector3.Cross(dir, Vector3.right).normalized;

                Vector3 tip = from + dir * arrowSize;
                Vector3 left = from - right * arrowSize * 0.4f;
                Vector3 bottom = from + right * arrowSize * 0.4f;

                Handles.DrawLine(left, tip);
                Handles.DrawLine(bottom, tip);
            }
        }

        #endregion

        #region 路径计算

        /// <summary>
        /// 计算完整路径点序列
        /// 使用 DOTween 内部 Path 类（反射），保证与预览/运行时路径完全一致
        /// </summary>
        private List<Vector3> ComputePath(Vector3 start, Vector3[] waypoints, int pathType, int resolution)
        {
            if (!DotweenPathHelper.IsAvailable)
            {
                DrawCompatibilityWarning(start);
                return null;
            }

            try
            {
                return ComputePathViaDotween(start, waypoints, pathType, resolution);
            }
            catch (Exception e)
            {
                Debug.LogError(
                    $"[DOTweenVisualEditor] 路径可视化反射调用失败，当前 DOTween 版本可能不兼容。\n" +
                    $"请检查 DOTween 版本是否满足要求（≥1.2.0），或联系插件作者更新。\n{e}");
                return null;
            }
        }

        /// <summary>
        /// 在 SceneView 中绘制版本不兼容警告
        /// </summary>
        private void DrawCompatibilityWarning(Vector3 position)
        {
            Handles.color = Color.red;
            float size = HandleUtility.GetHandleSize(position) * 0.15f;
            Handles.SphereHandleCap(0, position, Quaternion.identity, size, EventType.Repaint);

            var warningStyle = new GUIStyle(EditorStyles.label)
            {
                normal = { textColor = Color.red },
                fontSize = 12,
                fontStyle = UnityEngine.FontStyle.Bold,
                alignment = TextAnchor.LowerCenter
            };
            Handles.Label(
                position + Vector3.up * size * 3f,
                "⚠ DOTween 版本不兼容，路径可视化不可用",
                warningStyle);

            var detailStyle = new GUIStyle(EditorStyles.label)
            {
                normal = { textColor = new Color(1f, 0.7f, 0.5f) },
                fontSize = 10,
                alignment = TextAnchor.LowerCenter
            };
            Handles.Label(
                position + Vector3.up * size * 1.5f,
                "请检查 DOTween 版本 ≥ 1.2.0",
                detailStyle);
        }

        /// <summary>
        /// 使用 DOTween 内部 Path 类计算路径（反射方式）
        /// 模拟 PlugPath.SetTween 的内部流程：前置起始点 → FinalizePath → 采样
        /// </summary>
        private List<Vector3> ComputePathViaDotween(Vector3 start, Vector3[] waypoints, int pathType, int resolution)
        {
            // DOTween 在 PlugPath.SetTween 中会前置起始位置到 waypoints 数组
            var allWps = new Vector3[waypoints.Length + 1];
            allWps[0] = start;
            Array.Copy(waypoints, 0, allWps, 1, waypoints.Length);

            var pathEnum = (DG.Tweening.PathType)pathType;
            object path = null;
            try
            {
                // 创建 DOTween Path 对象（公开构造函数）
                path = DotweenPathHelper.CreatePath(pathEnum, allWps, resolution);

                // 标记已前置起始点 + FinalizePath（初始化控制点和速度查找表）
                DotweenPathHelper.Finalize(path, start);

                // 采样路径点（密度足够保证平滑渲染）
                int sampleCount = Mathf.Max(resolution * 20, 100) * allWps.Length;
                var result = new List<Vector3>(sampleCount + 1);
                for (int i = 0; i <= sampleCount; i++)
                {
                    float t = (float)i / sampleCount;
                    result.Add(DotweenPathHelper.GetPoint(path, t));
                }

                return result;
            }
            finally
            {
                // 移除 Path 构造函数自动注册的 Gizmo 绘制委托，避免重复绘制
                DotweenPathHelper.RemoveGizmoDelegate(path);
            }
        }

        #endregion
    }
}
#endif
