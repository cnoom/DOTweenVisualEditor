#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using CNoom.DOTweenVisual.Data;

namespace CNoom.DOTweenVisual.Editor
{
    /// <summary>
    /// DOPath 路径可视化器
    /// 在 SceneView 中绘制路径曲线，支持拖拽编辑路径点
    /// </summary>
    internal class PathVisualizer : IDisposable
    {
        #region 常量

        private const float WaypointHandleSize = 0.08f;
        private const float StartPointHandleSize = 0.1f;
        private const float ArrowSpacing = 0.1f;
        private const int ArrowDensity = 5;

        #endregion

        #region 数据

        private bool _isEnabled;
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
        /// <param name="stepProperty">当前步骤的 SerializedProperty</param>
        /// <param name="targetTransform">目标 Transform（用于获取起始位置）</param>
        /// <param name="getStartPosition">获取起始位置的委托</param>
        /// <param name="onPathDataChanged">路径数据变更后的回调（用于刷新 UI）</param>
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
            SceneView.RepaintAll();
        }

        /// <summary>
        /// 通知路径数据已更新（外部修改路径点后调用）
        /// </summary>
        public void NotifyDataChanged()
        {
            SceneView.RepaintAll();
        }

        #endregion

        #region SceneView 绘制

        private void OnSceneGUI(SceneView sceneView)
        {
            if (!_isEnabled || _stepProperty == null) return;

            var type = (TweenStepType)_stepProperty.FindPropertyRelative("Type").enumValueIndex;
            if (type != TweenStepType.DOPath) return;

            if (_targetTransform == null) return;

            // 获取路径数据
            var waypointsProp = _stepProperty.FindPropertyRelative("PathWaypoints");
            if (waypointsProp == null || !waypointsProp.isArray || waypointsProp.arraySize == 0) return;

            int pathType = _stepProperty.FindPropertyRelative("PathType").intValue;
            int resolution = _stepProperty.FindPropertyRelative("PathResolution").intValue;
            var gizmoColorProp = _stepProperty.FindPropertyRelative("PathGizmoColor");
            Color pathColor = gizmoColorProp != null ? gizmoColorProp.colorValue : new Color(1f, 0.5f, 0f);

            // 收集路径点
            Vector3[] waypoints = new Vector3[waypointsProp.arraySize];
            for (int i = 0; i < waypoints.Length; i++)
            {
                waypoints[i] = waypointsProp.GetArrayElementAtIndex(i).vector3Value;
            }

            Vector3 startPos = _getStartPosition != null ? _getStartPosition() : _targetTransform.position;

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

            // 标签
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

                // 路径点球体
                Handles.color = color;
                Handles.SphereHandleCap(0, wp, Quaternion.identity, handleSize, EventType.Repaint);

                // FreeMoveHandle 支持拖拽
                EditorGUI.BeginChangeCheck();
                Vector3 newPos = Handles.FreeMoveHandle(wp, handleSize, Vector3.zero, Handles.SphereHandleCap);
                if (EditorGUI.EndChangeCheck())
                {
                    // 通过 SerializedProperty 回写（支持 Undo）
                    var targetObj = waypointsProp.serializedObject.targetObject;
                    Undo.RecordObject(targetObj, "Move Waypoint");
                    waypointsProp.serializedObject.Update();
                    waypointsProp.GetArrayElementAtIndex(i).vector3Value = newPos;
                    waypointsProp.serializedObject.ApplyModifiedProperties();
                    _onPathDataChanged?.Invoke();
                    SceneView.RepaintAll();
                }

                // 序号标签
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

        private List<Vector3> ComputePath(Vector3 start, Vector3[] waypoints, int pathType, int resolution)
        {
            switch (pathType)
            {
                case 1: return ComputeCatmullRomPath(start, waypoints, resolution);
                case 2: return ComputeCubicBezierPath(start, waypoints, resolution);
                default: return ComputeLinearPath(start, waypoints);
            }
        }

        private List<Vector3> ComputeLinearPath(Vector3 start, Vector3[] waypoints)
        {
            var points = new List<Vector3> { start };
            points.AddRange(waypoints);
            return points;
        }

        private List<Vector3> ComputeCatmullRomPath(Vector3 start, Vector3[] waypoints, int resolution)
        {
            var allPoints = new List<Vector3> { start };
            allPoints.AddRange(waypoints);

            if (allPoints.Count < 2) return allPoints;

            resolution = Mathf.Max(1, resolution);
            var result = new List<Vector3>();

            for (int i = 0; i < allPoints.Count - 1; i++)
            {
                Vector3 p0 = i > 0 ? allPoints[i - 1] : allPoints[i];
                Vector3 p1 = allPoints[i];
                Vector3 p2 = allPoints[i + 1];
                Vector3 p3 = i < allPoints.Count - 2 ? allPoints[i + 2] : allPoints[i + 1];

                for (int t = 0; t < resolution; t++)
                {
                    float f = (float)t / resolution;
                    result.Add(CatmullRomInterpolate(p0, p1, p2, p3, f));
                }
            }

            result.Add(allPoints[allPoints.Count - 1]);
            return result;
        }

        private static Vector3 CatmullRomInterpolate(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, float t)
        {
            float t2 = t * t;
            float t3 = t2 * t;
            return 0.5f * (
                (2f * p1) +
                (-p0 + p2) * t +
                (2f * p0 - 5f * p1 + 4f * p2 - p3) * t2 +
                (-p0 + 3f * p1 - 3f * p2 + p3) * t3
            );
        }

        private List<Vector3> ComputeCubicBezierPath(Vector3 start, Vector3[] waypoints, int resolution)
        {
            var allPoints = new List<Vector3> { start };
            allPoints.AddRange(waypoints);

            if (allPoints.Count < 2) return allPoints;

            resolution = Mathf.Max(1, resolution);
            var result = new List<Vector3>();

            // DOTween CubicBezier: 每 3 个点定义一段（起始点 + 2控制点 + 终点）
            // 实际上 DOTween 的 CubicBezier 模式下，路径点被当作控制点序列
            // 每 4 个点（含起始点）定义一段三次贝塞尔
            int segments = (allPoints.Count - 1) / 3;

            if (segments == 0)
            {
                // 不足 4 个点时退化为直线
                return ComputeLinearPath(start, waypoints);
            }

            for (int s = 0; s < segments; s++)
            {
                int idx = s * 3;
                Vector3 p0 = allPoints[idx];
                Vector3 p1 = allPoints[idx + 1];
                Vector3 p2 = allPoints[idx + 2];
                Vector3 p3 = allPoints[idx + 3];

                for (int t = 0; t <= resolution; t++)
                {
                    float f = (float)t / resolution;
                    result.Add(BezierCubic(p0, p1, p2, p3, f));
                }
            }

            // 如果有剩余的点（不足一段贝塞尔），用直线连接
            int remainingStart = segments * 3 + 1;
            for (int i = remainingStart; i < allPoints.Count; i++)
            {
                result.Add(allPoints[i]);
            }

            return result;
        }

        private static Vector3 BezierCubic(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, float t)
        {
            float omt = 1f - t;
            return omt * omt * omt * p0
                 + 3f * omt * omt * t * p1
                 + 3f * omt * t * t * p2
                 + t * t * t * p3;
        }

        #endregion
    }
}
#endif
