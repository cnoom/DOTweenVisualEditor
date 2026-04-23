#if UNITY_EDITOR
using System;
using CNoom.DOTweenVisual.Components;
using DG.Tweening;
using CNoom.DOTweenVisual.Data;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor.UIElements;

namespace CNoom.DOTweenVisual.Editor
{
    /// <summary>
    /// 步骤详情面板控制器
    /// 管理详情面板的渲染、字段构建、路径点编辑和同步功能
    /// </summary>
    internal class StepDetailPanel
    {
        private VisualElement _detailPanel;
        private Label _detailHelpLabel;
        private ScrollView _detailScrollView;

        private readonly Func<SerializedObject> _getSerializedObject;
        private readonly Func<SerializedProperty> _getStepsProperty;
        private readonly Func<DOTweenVisualPlayer> _getTargetPlayer;
        private readonly Func<int> _getSelectedIndex;
        private readonly Action _onRebuildList;
        private readonly Action _onRefreshDetail;
        private readonly Action _onPathDataChanged;

        public StepDetailPanel(
            Func<SerializedObject> getSerializedObject,
            Func<SerializedProperty> getStepsProperty,
            Func<DOTweenVisualPlayer> getTargetPlayer,
            Func<int> getSelectedIndex,
            Action onRebuildList,
            Action onRefreshDetail,
            Action onPathDataChanged = null)
        {
            _getSerializedObject = getSerializedObject;
            _getStepsProperty = getStepsProperty;
            _getTargetPlayer = getTargetPlayer;
            _getSelectedIndex = getSelectedIndex;
            _onRebuildList = onRebuildList;
            _onRefreshDetail = onRefreshDetail;
            _onPathDataChanged = onPathDataChanged;
        }

        #region UI 创建

        /// <summary>
        /// 创建详情面板 UI 元素，添加到父容器
        /// </summary>
        public VisualElement CreateDetailPanel(VisualElement parent)
        {
            _detailPanel = new VisualElement();
            _detailPanel.AddToClassList("detail-content");
            _detailPanel.style.flexGrow = 1;

            _detailHelpLabel = new Label(L10n.Tr("Detail/SelectStep"));
            _detailHelpLabel.AddToClassList("detail-help-label");
            _detailPanel.Add(_detailHelpLabel);

            _detailScrollView = new ScrollView(ScrollViewMode.Vertical);
            _detailScrollView.AddToClassList("detail-scroll");
            _detailScrollView.style.display = DisplayStyle.None;
            _detailPanel.Add(_detailScrollView);

            parent.Add(_detailPanel);
            return _detailPanel;
        }

        #endregion

        #region 刷新

        /// <summary>
        /// 刷新详情面板
        /// </summary>
        public void RefreshDetailPanel()
        {
            if (_detailPanel == null) return;

            var stepsProperty = _getStepsProperty();
            int selectedIndex = _getSelectedIndex();

            if (selectedIndex < 0 || stepsProperty == null || selectedIndex >= stepsProperty.arraySize)
            {
                _detailHelpLabel.style.display = DisplayStyle.Flex;
                _detailScrollView.style.display = DisplayStyle.None;
                _detailScrollView.Clear();
                return;
            }

            var serializedObject = _getSerializedObject();
            serializedObject?.Update();
            var stepProperty = stepsProperty.GetArrayElementAtIndex(selectedIndex);

            _detailHelpLabel.style.display = DisplayStyle.None;
            _detailScrollView.style.display = DisplayStyle.Flex;
            BuildDetailFields(stepProperty);
        }

        #endregion

        #region 字段构建

        private void BuildDetailFields(SerializedProperty stepProperty)
        {
            _detailScrollView.Clear();

            var typeProp = stepProperty.FindPropertyRelative("Type");
            var isEnabledProp = stepProperty.FindPropertyRelative("IsEnabled");
            var durationProp = stepProperty.FindPropertyRelative("Duration");
            var delayProp = stepProperty.FindPropertyRelative("Delay");
            var easeProp = stepProperty.FindPropertyRelative("Ease");
            var executionModeProp = stepProperty.FindPropertyRelative("ExecutionMode");
            var insertTimeProp = stepProperty.FindPropertyRelative("InsertTime");
            var moveSpaceProp = stepProperty.FindPropertyRelative("MoveSpace");
            var rotateSpaceProp = stepProperty.FindPropertyRelative("RotateSpace");
            var rotateDirectionProp = stepProperty.FindPropertyRelative("RotateDirection");
            var punchTargetProp = stepProperty.FindPropertyRelative("PunchTarget");
            var shakeTargetProp = stepProperty.FindPropertyRelative("ShakeTarget");
            var targetTransformProp = stepProperty.FindPropertyRelative("TargetTransform");
            var isRelativeProp = stepProperty.FindPropertyRelative("IsRelative");
            var useStartValueProp = stepProperty.FindPropertyRelative("UseStartValue");
            var startVectorProp = stepProperty.FindPropertyRelative("StartVector");
            var targetVectorProp = stepProperty.FindPropertyRelative("TargetVector");
            var useStartColorProp = stepProperty.FindPropertyRelative("UseStartColor");
            var startColorProp = stepProperty.FindPropertyRelative("StartColor");
            var targetColorProp = stepProperty.FindPropertyRelative("TargetColor");
            var useStartFloatProp = stepProperty.FindPropertyRelative("UseStartFloat");
            var startFloatProp = stepProperty.FindPropertyRelative("StartFloat");
            var targetFloatProp = stepProperty.FindPropertyRelative("TargetFloat");
            var jumpHeightProp = stepProperty.FindPropertyRelative("JumpHeight");
            var jumpNumProp = stepProperty.FindPropertyRelative("JumpNum");
            var intensityProp = stepProperty.FindPropertyRelative("Intensity");
            var vibratoProp = stepProperty.FindPropertyRelative("Vibrato");
            var elasticityProp = stepProperty.FindPropertyRelative("Elasticity");
            var shakeRandomnessProp = stepProperty.FindPropertyRelative("ShakeRandomness");
            var useCustomCurveProp = stepProperty.FindPropertyRelative("UseCustomCurve");
            var customCurveProp = stepProperty.FindPropertyRelative("CustomCurve");
            var onCompleteProp = stepProperty.FindPropertyRelative("OnComplete");

            var type = (TweenStepType)typeProp.enumValueIndex;

            // 预计算同步值读取函数
            var targetValueReader = GetTargetValueReader(stepProperty);
            var startValueReader = GetStartValueReader(stepProperty);

            // 回调委托
            Action onTypeChanged = () => { _onRebuildList(); _onRefreshDetail(); };
            Action onEnumRebuild = () => { _onRebuildList(); _onRefreshDetail(); };
            Action onTimingChanged = () => { _onRebuildList(); };
            Action onToggleRebuild = () => _onRefreshDetail();
            Action onObjectFieldChanged = () => { _onRebuildList(); _onRefreshDetail(); };

            // --- 通用字段 ---
            AddDetailField(L10n.Tr("Detail/Type"), DetailFieldFactory.CreateEnumField(typeProp, typeof(TweenStepType), onTypeChanged));
            AddDetailField(L10n.Tr("Detail/Enabled"), DetailFieldFactory.CreateToggle(isEnabledProp));
            AddDetailField(L10n.Tr("Detail/Duration"), DetailFieldFactory.CreateFloatField(durationProp, onTimingChanged));
            AddDetailField(L10n.Tr("Detail/Delay"), DetailFieldFactory.CreateFloatField(delayProp));

            // --- 按类型显示 ---
            bool isTransformType = type == TweenStepType.Move || type == TweenStepType.Rotate || type == TweenStepType.Scale;

            if (isTransformType)
            {
                if (type == TweenStepType.Move)
                    AddDetailField(L10n.Tr("Detail/MoveSpace"), DetailFieldFactory.CreateEnumField(moveSpaceProp, typeof(MoveSpace)));
                else if (type == TweenStepType.Rotate)
                {
                    AddDetailField(L10n.Tr("Detail/RotateSpace"), DetailFieldFactory.CreateEnumField(rotateSpaceProp, typeof(RotateSpace)));
                    AddDetailField(L10n.Tr("Detail/RotateDirection"), DetailFieldFactory.CreateEnumField(rotateDirectionProp, typeof(RotateDirection)));
                }

                AddDetailField(L10n.Tr("Detail/TargetObject"), DetailFieldFactory.CreateObjectField(targetTransformProp, typeof(Transform), onObjectFieldChanged));
                AddDetailField(L10n.Tr("Detail/RelativeMode"), DetailFieldFactory.CreateToggle(isRelativeProp));

                AddSeparator();

                AddDetailField(L10n.Tr("Detail/UseStartValue"), DetailFieldFactory.CreateToggle(useStartValueProp, onToggleRebuild));

                if (useStartValueProp.boolValue)
                {
                    string startLabel = type == TweenStepType.Rotate ? L10n.Tr("Detail/StartRotation") : L10n.Tr("Detail/StartValue");
                    AddSyncableField(startLabel, DetailFieldFactory.CreateVector3Field(startVectorProp), startVectorProp, startValueReader);
                }

                string targetLabel = type == TweenStepType.Rotate ? L10n.Tr("Detail/TargetValueEuler") : L10n.Tr("Detail/TargetValue");
                AddSyncableField(targetLabel, DetailFieldFactory.CreateVector3Field(targetVectorProp), targetVectorProp, targetValueReader);
            }
            else if (type == TweenStepType.AnchorMove || type == TweenStepType.SizeDelta)
            {
                AddDetailField(L10n.Tr("Detail/TargetObject"), DetailFieldFactory.CreateObjectField(targetTransformProp, typeof(Transform), onObjectFieldChanged));
                AddValidationWarning(targetTransformProp, type);
                AddDetailField(L10n.Tr("Detail/RelativeMode"), DetailFieldFactory.CreateToggle(isRelativeProp));

                AddSeparator();

                AddDetailField(L10n.Tr("Detail/UseStartValue"), DetailFieldFactory.CreateToggle(useStartValueProp, onToggleRebuild));

                if (useStartValueProp.boolValue)
                {
                    string startLabel = type == TweenStepType.AnchorMove ? L10n.Tr("Detail/StartAnchorPos") : L10n.Tr("Detail/StartSize");
                    AddSyncableField(startLabel, DetailFieldFactory.CreateVector3Field(startVectorProp), startVectorProp, startValueReader);
                }

                string targetLabel = type == TweenStepType.AnchorMove ? L10n.Tr("Detail/TargetAnchorPos") : L10n.Tr("Detail/TargetSize");
                AddSyncableField(targetLabel, DetailFieldFactory.CreateVector3Field(targetVectorProp), targetVectorProp, targetValueReader);
            }
            else if (type == TweenStepType.Color)
            {
                AddDetailField(L10n.Tr("Detail/TargetObject"), DetailFieldFactory.CreateObjectField(targetTransformProp, typeof(Transform), onObjectFieldChanged));
                AddValidationWarning(targetTransformProp, TweenStepType.Color);

                AddSeparator();

                AddDetailField(L10n.Tr("Detail/UseStartColor"), DetailFieldFactory.CreateToggle(useStartColorProp, onToggleRebuild));

                if (useStartColorProp.boolValue)
                {
                    AddSyncableField(L10n.Tr("Detail/StartColor"), DetailFieldFactory.CreateColorField(startColorProp), startColorProp, startValueReader);
                }

                AddSyncableField(L10n.Tr("Detail/TargetColor"), DetailFieldFactory.CreateColorField(targetColorProp), targetColorProp, targetValueReader);
            }
            else if (type == TweenStepType.Fade)
            {
                AddDetailField(L10n.Tr("Detail/TargetObject"), DetailFieldFactory.CreateObjectField(targetTransformProp, typeof(Transform), onObjectFieldChanged));
                AddValidationWarning(targetTransformProp, TweenStepType.Fade);

                AddSeparator();

                AddDetailField(L10n.Tr("Detail/UseStartAlpha"), DetailFieldFactory.CreateToggle(useStartFloatProp, onToggleRebuild));

                if (useStartFloatProp.boolValue)
                {
                    AddSyncableField(L10n.Tr("Detail/StartAlpha"), DetailFieldFactory.CreateFloatField(startFloatProp), startFloatProp, startValueReader);
                }

                AddSyncableField(L10n.Tr("Detail/TargetAlpha"), DetailFieldFactory.CreateFloatField(targetFloatProp), targetFloatProp, targetValueReader);
            }
            else if (type == TweenStepType.Jump)
            {
                AddDetailField(L10n.Tr("Detail/TargetObject"), DetailFieldFactory.CreateObjectField(targetTransformProp, typeof(Transform), onObjectFieldChanged));

                AddSeparator();

                AddDetailField(L10n.Tr("Detail/UseStartPosition"), DetailFieldFactory.CreateToggle(useStartValueProp, onToggleRebuild));

                if (useStartValueProp.boolValue)
                {
                    AddSyncableField(L10n.Tr("Detail/StartPosition"), DetailFieldFactory.CreateVector3Field(startVectorProp), startVectorProp, startValueReader);
                }

                AddSyncableField(L10n.Tr("Detail/TargetPosition"), DetailFieldFactory.CreateVector3Field(targetVectorProp), targetVectorProp, targetValueReader);

                AddSeparator();

                AddDetailField(L10n.Tr("Detail/JumpHeight"), DetailFieldFactory.CreateFloatField(jumpHeightProp));
                AddDetailField(L10n.Tr("Detail/JumpCount"), DetailFieldFactory.CreateIntegerField(jumpNumProp));
            }
            else if (type == TweenStepType.Punch)
            {
                AddDetailField(L10n.Tr("Detail/TargetObject"), DetailFieldFactory.CreateObjectField(targetTransformProp, typeof(Transform), onObjectFieldChanged));

                AddSeparator();

                AddDetailField(L10n.Tr("Detail/PunchTarget"), DetailFieldFactory.CreateEnumField(punchTargetProp, typeof(PunchTarget)));
                AddDetailField(L10n.Tr("Detail/Intensity"), DetailFieldFactory.CreateVector3Field(intensityProp));
                AddDetailField(L10n.Tr("Detail/Vibrato"), DetailFieldFactory.CreateIntegerField(vibratoProp));
                AddDetailField(L10n.Tr("Detail/Elasticity"), DetailFieldFactory.CreateFloatField(elasticityProp));
            }
            else if (type == TweenStepType.Shake)
            {
                AddDetailField(L10n.Tr("Detail/TargetObject"), DetailFieldFactory.CreateObjectField(targetTransformProp, typeof(Transform), onObjectFieldChanged));

                AddSeparator();

                AddDetailField(L10n.Tr("Detail/ShakeTarget"), DetailFieldFactory.CreateEnumField(shakeTargetProp, typeof(ShakeTarget)));
                AddDetailField(L10n.Tr("Detail/Intensity"), DetailFieldFactory.CreateVector3Field(intensityProp));
                AddDetailField(L10n.Tr("Detail/Vibrato"), DetailFieldFactory.CreateIntegerField(vibratoProp));
                AddDetailField(L10n.Tr("Detail/Elasticity"), DetailFieldFactory.CreateFloatField(elasticityProp));
                AddDetailField(L10n.Tr("Detail/ShakeRandomness"), DetailFieldFactory.CreateFloatField(shakeRandomnessProp));
            }
            else if (type == TweenStepType.FillAmount)
            {
                AddDetailField(L10n.Tr("Detail/TargetObject"), DetailFieldFactory.CreateObjectField(targetTransformProp, typeof(Transform), onObjectFieldChanged));
                AddValidationWarning(targetTransformProp, TweenStepType.FillAmount);

                AddSeparator();

                AddDetailField(L10n.Tr("Detail/UseStartValue"), DetailFieldFactory.CreateToggle(useStartFloatProp, onToggleRebuild));

                if (useStartFloatProp.boolValue)
                {
                    AddSyncableField(L10n.Tr("Detail/StartValue"), DetailFieldFactory.CreateFloatField(startFloatProp), startFloatProp, startValueReader);
                }

                AddSyncableField(L10n.Tr("Detail/TargetValue"), DetailFieldFactory.CreateFloatField(targetFloatProp), targetFloatProp, targetValueReader);
            }
            else if (type == TweenStepType.DOPath)
            {
                AddDetailField(L10n.Tr("Detail/TargetObject"), DetailFieldFactory.CreateObjectField(targetTransformProp, typeof(Transform), onObjectFieldChanged));

                AddSeparator();

                AddDetailField(L10n.Tr("Detail/UseStartPosition"), DetailFieldFactory.CreateToggle(useStartValueProp, onToggleRebuild));

                if (useStartValueProp.boolValue)
                {
                    AddSyncableField(L10n.Tr("Detail/StartPosition"), DetailFieldFactory.CreateVector3Field(startVectorProp), startVectorProp, startValueReader);
                }

                var waypointsProp = stepProperty.FindPropertyRelative("PathWaypoints");
                var pathTypeProp = stepProperty.FindPropertyRelative("PathType");

                // 确保路径点数量满足当前 PathType 的约束
                EnsureWaypointCount(waypointsProp, pathTypeProp.intValue);
                AddPathWaypointsEditor(waypointsProp, pathTypeProp);

                AddSeparator();

                AddDetailField(L10n.Tr("Detail/PathType"), DetailFieldFactory.CreatePathTypeEnumField(pathTypeProp, () =>
                {
                    // PathType 切换后自动调整路径点数量
                    EnsureWaypointCount(waypointsProp, pathTypeProp.intValue);
                    _onRefreshDetail();
                    _onPathDataChanged?.Invoke();
                }));
                AddDetailField(L10n.Tr("Detail/PathMode"), DetailFieldFactory.CreatePathModeEnumField(stepProperty.FindPropertyRelative("PathMode")));
                AddDetailField(L10n.Tr("Detail/PathResolution"), DetailFieldFactory.CreateIntegerField(stepProperty.FindPropertyRelative("PathResolution")));
            }

            AddSeparator();

            // --- 执行 & 缓动 & 回调 ---
            if (type == TweenStepType.Callback)
            {
                if (onCompleteProp != null)
                {
                    var eventField = new PropertyField(onCompleteProp);
                    eventField.BindProperty(onCompleteProp);
                    _detailScrollView.Add(eventField);
                }
            }
            else if (type == TweenStepType.Delay)
            {
                AddDetailField(L10n.Tr("Detail/ExecutionMode"), DetailFieldFactory.CreateEnumField(executionModeProp, typeof(ExecutionMode), onEnumRebuild));
                if ((ExecutionMode)executionModeProp.enumValueIndex == ExecutionMode.Insert)
                {
                    AddDetailField(L10n.Tr("Detail/InsertTime"), DetailFieldFactory.CreateFloatField(insertTimeProp, onTimingChanged));
                }
            }
            else if (type == TweenStepType.Punch || type == TweenStepType.Shake)
            {
                AddDetailField(L10n.Tr("Detail/ExecutionMode"), DetailFieldFactory.CreateEnumField(executionModeProp, typeof(ExecutionMode), onEnumRebuild));
                if ((ExecutionMode)executionModeProp.enumValueIndex == ExecutionMode.Insert)
                {
                    AddDetailField(L10n.Tr("Detail/InsertTime"), DetailFieldFactory.CreateFloatField(insertTimeProp, onTimingChanged));
                }

                if (onCompleteProp != null)
                {
                    AddSeparator();
                    var eventField = new PropertyField(onCompleteProp);
                    eventField.BindProperty(onCompleteProp);
                    _detailScrollView.Add(eventField);
                }
            }
            else
            {
                AddDetailField(L10n.Tr("Detail/ExecutionMode"), DetailFieldFactory.CreateEnumField(executionModeProp, typeof(ExecutionMode), onEnumRebuild));
                if ((ExecutionMode)executionModeProp.enumValueIndex == ExecutionMode.Insert)
                {
                    AddDetailField(L10n.Tr("Detail/InsertTime"), DetailFieldFactory.CreateFloatField(insertTimeProp, onTimingChanged));
                }

                AddDetailField(L10n.Tr("Detail/Ease"), DetailFieldFactory.CreateEnumField(easeProp, typeof(Ease)));

                AddDetailField(L10n.Tr("Detail/CustomCurve"), DetailFieldFactory.CreateToggle(useCustomCurveProp, onToggleRebuild));

                if (useCustomCurveProp.boolValue && customCurveProp != null)
                {
                    AddDetailField(L10n.Tr("Detail/CustomCurve"), DetailFieldFactory.CreateCurveField(customCurveProp));
                }

                if (onCompleteProp != null)
                {
                    AddSeparator();
                    var eventField = new PropertyField(onCompleteProp);
                    eventField.BindProperty(onCompleteProp);
                    _detailScrollView.Add(eventField);
                }
            }
        }

        #endregion

        #region 辅助 UI 方法

        private void AddDetailField(string label, VisualElement field)
        {
            var row = new VisualElement();
            row.AddToClassList("detail-field-row");

            var labelEl = new Label(label);
            labelEl.AddToClassList("detail-field-label");
            row.Add(labelEl);

            field.AddToClassList("detail-field-value");
            row.Add(field);

            _detailScrollView.Add(row);
        }

        private void AddSeparator()
        {
            var sep = new VisualElement();
            sep.AddToClassList("detail-separator");
            _detailScrollView.Add(sep);
        }

        /// <summary>
        /// 添加组件需求校验警告
        /// </summary>
        private void AddValidationWarning(SerializedProperty targetTransformProp, TweenStepType type)
        {
            var requirement = TweenStepRequirement.GetRequirementDescription(type);
            if (requirement == null) return;

            var target = targetTransformProp.objectReferenceValue as Transform;
            if (target == null) return;

            if (TweenStepRequirement.Validate(target, type, out string errorMessage))
                return;

            var warning = new VisualElement();
            warning.AddToClassList("validation-warning");
            warning.style.backgroundColor = new Color(0.6f, 0.15f, 0.15f, 0.5f);
            warning.style.paddingTop = 4f;
            warning.style.paddingBottom = 4f;
            warning.style.paddingLeft = 8f;
            warning.style.paddingRight = 8f;
            warning.style.marginTop = 2f;
            warning.style.marginBottom = 2f;
            warning.style.borderTopLeftRadius = 3f;
            warning.style.borderTopRightRadius = 3f;
            warning.style.borderBottomLeftRadius = 3f;
            warning.style.borderBottomRightRadius = 3f;

            var label = new Label(errorMessage);
            label.style.color = Color.yellow;
            label.style.fontSize = 11f;
            label.style.unityTextAlign = TextAnchor.MiddleLeft;
            warning.Add(label);

            _detailScrollView.Add(warning);
        }

        #endregion

        #region 路径点编辑器

        /// <summary>
        /// PathType 枚举值: 0=Linear, 1=CatmullRom, 2=CubicBezier
        /// </summary>
        private const int PathType_CubicBezier = 2;

        /// <summary>
        /// 获取指定 PathType 所需的最小路径点数量
        /// </summary>
        private static int GetMinWaypointCount(int pathType)
        {
            return pathType == PathType_CubicBezier ? 3 : 2;
        }

        /// <summary>
        /// 获取指定 PathType 每次添加/删除的步长
        /// </summary>
        private static int GetWaypointStep(int pathType)
        {
            return pathType == PathType_CubicBezier ? 3 : 1;
        }

        /// <summary>
        /// 确保路径点数量满足 PathType 约束，不满足时自动补齐或裁剪
        /// </summary>
        private void EnsureWaypointCount(SerializedProperty waypointsProp, int pathType)
        {
            if (waypointsProp == null || !waypointsProp.isArray) return;

            int minCount = GetMinWaypointCount(pathType);
            int step = GetWaypointStep(pathType);
            int current = waypointsProp.arraySize;

            if (pathType == PathType_CubicBezier)
            {
                // CubicBezier 需要 3n 个点，不足则补齐
                int target = Mathf.Max(minCount, Mathf.CeilToInt((float)current / step) * step);
                if (target < minCount) target = minCount;

                if (current < target)
                {
                    _getSerializedObject()?.Update();
                    while (waypointsProp.arraySize < target)
                    {
                        Vector3 lastPos = waypointsProp.arraySize > 0
                            ? waypointsProp.GetArrayElementAtIndex(waypointsProp.arraySize - 1).vector3Value
                            : Vector3.zero;
                        waypointsProp.InsertArrayElementAtIndex(waypointsProp.arraySize);
                        waypointsProp.GetArrayElementAtIndex(waypointsProp.arraySize - 1).vector3Value =
                            lastPos + new Vector3(1f, 0f, 0f);
                    }
                    waypointsProp.serializedObject.ApplyModifiedProperties();
                }
            }
            else
            {
                // Linear/CatmullRom 至少 2 个点
                if (current < minCount)
                {
                    _getSerializedObject()?.Update();
                    while (waypointsProp.arraySize < minCount)
                    {
                        Vector3 lastPos = waypointsProp.arraySize > 0
                            ? waypointsProp.GetArrayElementAtIndex(waypointsProp.arraySize - 1).vector3Value
                            : Vector3.zero;
                        waypointsProp.InsertArrayElementAtIndex(waypointsProp.arraySize);
                        waypointsProp.GetArrayElementAtIndex(waypointsProp.arraySize - 1).vector3Value =
                            lastPos + new Vector3(1f, 0f, 0f);
                    }
                    waypointsProp.serializedObject.ApplyModifiedProperties();
                }
            }
        }

        /// <summary>
        /// 添加路径点列表编辑器（支持增删改路径点）
        /// </summary>
        private void AddPathWaypointsEditor(SerializedProperty waypointsProp, SerializedProperty pathTypeProp)
        {
            if (waypointsProp == null || !waypointsProp.isArray) return;

            var container = new VisualElement();
            container.AddToClassList("path-waypoints-container");

            var headerRow = new VisualElement();
            headerRow.style.flexDirection = FlexDirection.Row;
            headerRow.style.alignItems = Align.Center;
            headerRow.style.marginBottom = 4f;

            var countLabel = new Label($"{L10n.Tr("Detail/Waypoints")} ({waypointsProp.arraySize})");
            countLabel.style.fontSize = 11f;
            countLabel.style.color = new Color(0.85f, 0.75f, 0.5f);
            countLabel.style.flexGrow = 1;
            headerRow.Add(countLabel);

            int pathType = pathTypeProp.intValue;
            int step = GetWaypointStep(pathType);
            string addLabel = step > 1
                ? $"{L10n.Tr("Detail/AddWaypoint")} +{step}"
                : L10n.Tr("Detail/AddWaypoint");
            var addButton = new Button(() => AddPathWaypoint(waypointsProp, pathTypeProp.intValue)) { text = addLabel };
            addButton.style.fontSize = 10f;
            addButton.style.paddingLeft = 6f;
            addButton.style.paddingRight = 6f;
            headerRow.Add(addButton);

            container.Add(headerRow);

            for (int i = 0; i < waypointsProp.arraySize; i++)
            {
                int idx = i;
                var wp = waypointsProp.GetArrayElementAtIndex(idx);

                var pointRow = new VisualElement();
                pointRow.style.flexDirection = FlexDirection.Row;
                pointRow.style.alignItems = Align.Center;
                pointRow.style.marginBottom = 2f;
                pointRow.style.paddingLeft = 4f;
                pointRow.style.paddingRight = 2f;
                pointRow.style.backgroundColor = new Color(0.25f, 0.25f, 0.25f);

                var idxLabel = new Label($"{idx + 1}.");
                idxLabel.style.fontSize = 9f;
                idxLabel.style.color = new Color(0.7f, 0.7f, 0.7f);
                idxLabel.style.width = 18f;
                idxLabel.style.flexShrink = 0;
                pointRow.Add(idxLabel);

                Vector3 currentVal = wp.vector3Value;

                var xField = DetailFieldFactory.CreatePathCoordFloatField("X", currentVal.x,
                    val =>
                    {
                        if (!DetailFieldFactory.IsValidProperty(wp)) return;
                        Undo.RecordObject(_getTargetPlayer(), L10n.Tr("Undo/ModifyWaypoint"));
                        _getSerializedObject()?.Update();
                        var v = waypointsProp.GetArrayElementAtIndex(idx).vector3Value;
                        v.x = val;
                        waypointsProp.GetArrayElementAtIndex(idx).vector3Value = v;
                        waypointsProp.serializedObject.ApplyModifiedProperties();
                        _onPathDataChanged?.Invoke();
                    });
                xField.style.flexGrow = 1f;
                xField.style.flexBasis = 0f;
                xField.style.marginRight = 1f;
                pointRow.Add(xField);

                var yField = DetailFieldFactory.CreatePathCoordFloatField("Y", currentVal.y,
                    val =>
                    {
                        if (!DetailFieldFactory.IsValidProperty(wp)) return;
                        Undo.RecordObject(_getTargetPlayer(), L10n.Tr("Undo/ModifyWaypoint"));
                        _getSerializedObject()?.Update();
                        var v = waypointsProp.GetArrayElementAtIndex(idx).vector3Value;
                        v.y = val;
                        waypointsProp.GetArrayElementAtIndex(idx).vector3Value = v;
                        waypointsProp.serializedObject.ApplyModifiedProperties();
                        _onPathDataChanged?.Invoke();
                    });
                yField.style.flexGrow = 1f;
                yField.style.flexBasis = 0f;
                yField.style.marginRight = 1f;
                pointRow.Add(yField);

                var zField = DetailFieldFactory.CreatePathCoordFloatField("Z", currentVal.z,
                    val =>
                    {
                        if (!DetailFieldFactory.IsValidProperty(wp)) return;
                        Undo.RecordObject(_getTargetPlayer(), L10n.Tr("Undo/ModifyWaypoint"));
                        _getSerializedObject()?.Update();
                        var v = waypointsProp.GetArrayElementAtIndex(idx).vector3Value;
                        v.z = val;
                        waypointsProp.GetArrayElementAtIndex(idx).vector3Value = v;
                        waypointsProp.serializedObject.ApplyModifiedProperties();
                        _onPathDataChanged?.Invoke();
                    });
                zField.style.flexGrow = 1f;
                zField.style.flexBasis = 0f;
                pointRow.Add(zField);

                // 同步按钮：将该路径点设为物体当前位置
                var wpProp = waypointsProp.GetArrayElementAtIndex(idx);
                var wpSyncBtn = CreateInlineSyncButton(wpProp, t => (object)t.position);
                wpSyncBtn.EnableInClassList("inline-sync-button", true);
                wpSyncBtn.EnableInClassList("inline-sync-button--compact", true);
                pointRow.Add(wpSyncBtn);

                var delBtn = new Button(() =>
                {
                    RemovePathWaypoint(waypointsProp, idx, pathTypeProp.intValue);
                }) { text = "✕" };
                delBtn.style.fontSize = 9f;
                delBtn.style.color = new Color(0.9f, 0.4f, 0.4f);
                delBtn.style.width = 18f;
                delBtn.style.height = 18f;
                delBtn.style.flexShrink = 0;
                delBtn.style.marginLeft = 2f;
                // CubicBezier 模式下，无法单独删除（需保持 3n），禁用单点删除
                if (pathType == PathType_CubicBezier)
                    delBtn.SetEnabled(false);
                pointRow.Add(delBtn);

                container.Add(pointRow);
            }

            _detailScrollView.Add(container);
        }

        private void AddPathWaypoint(SerializedProperty waypointsProp, int pathType)
        {
            if (waypointsProp == null) return;
            int step = GetWaypointStep(pathType);
            int minCount = GetMinWaypointCount(pathType);

            Undo.RecordObject(_getTargetPlayer(), L10n.Tr("Undo/AddWaypoint"));
            _getSerializedObject()?.Update();

            for (int i = 0; i < step; i++)
            {
                Vector3 lastPos = Vector3.zero;
                if (waypointsProp.arraySize > 0)
                {
                    lastPos = waypointsProp.GetArrayElementAtIndex(waypointsProp.arraySize - 1).vector3Value;
                }

                waypointsProp.InsertArrayElementAtIndex(waypointsProp.arraySize);
                var newWp = waypointsProp.GetArrayElementAtIndex(waypointsProp.arraySize - 1);
                newWp.vector3Value = lastPos + new Vector3(1f, 0f, 0f);
            }
            waypointsProp.serializedObject.ApplyModifiedProperties();

            _onRefreshDetail();
            _onPathDataChanged?.Invoke();
        }

        private void RemovePathWaypoint(SerializedProperty waypointsProp, int index, int pathType)
        {
            if (waypointsProp == null || index < 0 || index >= waypointsProp.arraySize) return;
            int minCount = GetMinWaypointCount(pathType);

            if (waypointsProp.arraySize <= minCount)
            {
                DOTweenLog.Warning(L10n.Tr("Detail/MinWaypointsWarning"));
                return;
            }

            Undo.RecordObject(_getTargetPlayer(), L10n.Tr("Undo/DeleteWaypoint"));
            _getSerializedObject()?.Update();
            waypointsProp.DeleteArrayElementAtIndex(index);
            waypointsProp.serializedObject.ApplyModifiedProperties();

            _onRefreshDetail();
            _onPathDataChanged?.Invoke();
        }

        #endregion

        #region 同步

        /// <summary>
        /// 同步枚举：标识要同步的是目标值还是起始值
        /// </summary>
        private enum SyncTarget
        {
            TargetValue,
            StartValue
        }

        /// <summary>
        /// 获取当前步骤中目标 Transform
        /// </summary>
        private Transform GetStepTargetTransform()
        {
            int selectedIndex = _getSelectedIndex();
            var stepsProperty = _getStepsProperty();
            if (selectedIndex < 0 || stepsProperty == null || selectedIndex >= stepsProperty.arraySize) return null;

            var stepProperty = stepsProperty.GetArrayElementAtIndex(selectedIndex);
            var targetTransformProp = stepProperty.FindPropertyRelative("TargetTransform");
            var target = targetTransformProp.objectReferenceValue as Transform;
            if (target == null)
            {
                var player = _getTargetPlayer();
                if (player != null) target = player.transform;
            }
            return target;
        }

        /// <summary>
        /// 创建内联同步按钮，点击后将物体当前值写入指定 SerializedProperty
        /// </summary>
        private Button CreateInlineSyncButton(SerializedProperty prop, System.Func<Transform, object> readCurrentValue)
        {
            var btn = new Button(() =>
            {
                var target = GetStepTargetTransform();
                if (target == null) return;

                var value = readCurrentValue(target);
                if (value == null) return;

                Undo.RecordObject(_getTargetPlayer(), L10n.Tr("Undo/SyncValue"));
                _getSerializedObject()?.Update();

                if (value is Vector3 v3)
                    prop.vector3Value = v3;
                else if (value is Color col)
                    prop.colorValue = col;
                else if (value is float f)
                    prop.floatValue = f;

                prop.serializedObject.ApplyModifiedProperties();
                _onRefreshDetail();
            })
            {
                text = "⤓",
                tooltip = L10n.Tr("Detail/SyncTooltip")
            };
            btn.AddToClassList("inline-sync-button");
            return btn;
        }

        /// <summary>
        /// 根据动画类型获取当前目标值的读取函数
        /// </summary>
        private System.Func<Transform, object> GetTargetValueReader(SerializedProperty stepProperty)
        {
            var type = (TweenStepType)stepProperty.FindPropertyRelative("Type").enumValueIndex;
            switch (type)
            {
                case TweenStepType.Move:
                    var moveSpace = (MoveSpace)stepProperty.FindPropertyRelative("MoveSpace").enumValueIndex;
                    return t => moveSpace == MoveSpace.Local ? (object)t.localPosition : t.position;
                case TweenStepType.Rotate:
                    var rotateSpace = (RotateSpace)stepProperty.FindPropertyRelative("RotateSpace").enumValueIndex;
                    return t => rotateSpace == RotateSpace.Local ? (object)t.localRotation.eulerAngles : t.rotation.eulerAngles;
                case TweenStepType.Scale:
                    return t => t.localScale;
                case TweenStepType.Color:
                    return t => TweenValueHelper.TryGetColor(t, out var c) ? (object)c : null;
                case TweenStepType.Fade:
                    return t => TweenValueHelper.TryGetAlpha(t, out var a) ? (object)a : null;
                case TweenStepType.AnchorMove:
                    return t => TweenValueHelper.TryGetRectTransform(t, out var rt) ? (object)(Vector3)rt.anchoredPosition : null;
                case TweenStepType.SizeDelta:
                    return t => TweenValueHelper.TryGetRectTransform(t, out var rt) ? (object)(Vector3)rt.sizeDelta : null;
                case TweenStepType.Jump:
                    return t => t.position;
                case TweenStepType.FillAmount:
                    return t =>
                    {
                        var img = t.GetComponent<UnityEngine.UI.Image>();
                        return img != null ? (object)img.fillAmount : null;
                    };
                case TweenStepType.DOPath:
                    return t => t.position;
                default:
                    return null;
            }
        }

        /// <summary>
        /// 根据动画类型获取起始值的读取函数（起始值=目标值，概念上起始值就是动画开始时的"当前值"）
        /// </summary>
        private System.Func<Transform, object> GetStartValueReader(SerializedProperty stepProperty)
        {
            return GetTargetValueReader(stepProperty);
        }

        /// <summary>
        /// 添加带内联同步按钮的字段行
        /// </summary>
        private void AddSyncableField(string label, VisualElement field, SerializedProperty prop, System.Func<Transform, object> valueReader)
        {
            var row = new VisualElement();
            row.AddToClassList("detail-field-row");

            var labelEl = new Label(label);
            labelEl.AddToClassList("detail-field-label");
            row.Add(labelEl);

            field.AddToClassList("detail-field-value");
            row.Add(field);

            if (valueReader != null)
            {
                var syncBtn = CreateInlineSyncButton(prop, valueReader);
                row.Add(syncBtn);
            }

            _detailScrollView.Add(row);
        }

        #endregion
    }
}
#endif
