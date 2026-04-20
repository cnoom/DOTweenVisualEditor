#if UNITY_EDITOR
using System;
using CNoom.DOTweenVisual.Components;
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

        public StepDetailPanel(
            Func<SerializedObject> getSerializedObject,
            Func<SerializedProperty> getStepsProperty,
            Func<DOTweenVisualPlayer> getTargetPlayer,
            Func<int> getSelectedIndex,
            Action onRebuildList,
            Action onRefreshDetail)
        {
            _getSerializedObject = getSerializedObject;
            _getStepsProperty = getStepsProperty;
            _getTargetPlayer = getTargetPlayer;
            _getSelectedIndex = getSelectedIndex;
            _onRebuildList = onRebuildList;
            _onRefreshDetail = onRefreshDetail;
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

            _detailHelpLabel = new Label("请在左侧选择一个步骤");
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

            // 回调委托
            Action onTypeChanged = () => { _onRebuildList(); _onRefreshDetail(); };
            Action onEnumRebuild = () => { _onRebuildList(); _onRefreshDetail(); };
            Action onTimingChanged = () => { _onRebuildList(); };
            Action onToggleRebuild = () => _onRefreshDetail();
            Action onObjectFieldChanged = () => { _onRebuildList(); _onRefreshDetail(); };

            // --- 通用字段 ---
            AddDetailField("类型", DetailFieldFactory.CreateEnumField(typeProp, typeof(TweenStepType), onTypeChanged));
            AddDetailField("启用", DetailFieldFactory.CreateToggle(isEnabledProp));
            AddDetailField("时长", DetailFieldFactory.CreateFloatField(durationProp, onTimingChanged));
            AddDetailField("延迟", DetailFieldFactory.CreateFloatField(delayProp));

            // --- 按类型显示 ---
            bool isTransformType = type == TweenStepType.Move || type == TweenStepType.Rotate || type == TweenStepType.Scale;

            if (isTransformType)
            {
                if (type == TweenStepType.Move)
                    AddDetailField("坐标空间", DetailFieldFactory.CreateEnumField(moveSpaceProp, typeof(MoveSpace)));
                else if (type == TweenStepType.Rotate)
                {
                    AddDetailField("坐标空间", DetailFieldFactory.CreateEnumField(rotateSpaceProp, typeof(RotateSpace)));
                    AddDetailField("旋转方向", DetailFieldFactory.CreateEnumField(rotateDirectionProp, typeof(RotateDirection)));
                }

                AddDetailField("目标物体", DetailFieldFactory.CreateObjectField(targetTransformProp, typeof(Transform), onObjectFieldChanged));
                AddDetailField("相对模式", DetailFieldFactory.CreateToggle(isRelativeProp));

                AddSeparator();

                AddDetailField("使用起始值", DetailFieldFactory.CreateToggle(useStartValueProp, onToggleRebuild));

                if (useStartValueProp.boolValue)
                {
                    string startLabel = type == TweenStepType.Rotate ? "起始旋转" : "起始值";
                    AddDetailField(startLabel, DetailFieldFactory.CreateVector3Field(startVectorProp));
                }

                string targetLabel = type == TweenStepType.Rotate ? "目标值 (欧拉角)" : "目标值";
                AddDetailField(targetLabel, DetailFieldFactory.CreateVector3Field(targetVectorProp));
            }
            else if (type == TweenStepType.AnchorMove || type == TweenStepType.SizeDelta)
            {
                AddDetailField("目标物体", DetailFieldFactory.CreateObjectField(targetTransformProp, typeof(Transform), onObjectFieldChanged));
                AddValidationWarning(targetTransformProp, type);
                AddDetailField("相对模式", DetailFieldFactory.CreateToggle(isRelativeProp));

                AddSeparator();

                AddDetailField("使用起始值", DetailFieldFactory.CreateToggle(useStartValueProp, onToggleRebuild));

                if (useStartValueProp.boolValue)
                {
                    string startLabel = type == TweenStepType.AnchorMove ? "起始锚点位置" : "起始尺寸";
                    AddDetailField(startLabel, DetailFieldFactory.CreateVector3Field(startVectorProp));
                }

                string targetLabel = type == TweenStepType.AnchorMove ? "目标锚点位置" : "目标尺寸";
                AddDetailField(targetLabel, DetailFieldFactory.CreateVector3Field(targetVectorProp));
            }
            else if (type == TweenStepType.Color)
            {
                AddDetailField("目标物体", DetailFieldFactory.CreateObjectField(targetTransformProp, typeof(Transform), onObjectFieldChanged));
                AddValidationWarning(targetTransformProp, TweenStepType.Color);

                AddSeparator();

                AddDetailField("使用起始颜色", DetailFieldFactory.CreateToggle(useStartColorProp, onToggleRebuild));

                if (useStartColorProp.boolValue)
                {
                    AddDetailField("起始颜色", DetailFieldFactory.CreateColorField(startColorProp));
                }

                AddDetailField("目标颜色", DetailFieldFactory.CreateColorField(targetColorProp));
            }
            else if (type == TweenStepType.Fade)
            {
                AddDetailField("目标物体", DetailFieldFactory.CreateObjectField(targetTransformProp, typeof(Transform), onObjectFieldChanged));
                AddValidationWarning(targetTransformProp, TweenStepType.Fade);

                AddSeparator();

                AddDetailField("使用起始透明度", DetailFieldFactory.CreateToggle(useStartFloatProp, onToggleRebuild));

                if (useStartFloatProp.boolValue)
                {
                    AddDetailField("起始透明度", DetailFieldFactory.CreateFloatField(startFloatProp));
                }

                AddDetailField("目标透明度", DetailFieldFactory.CreateFloatField(targetFloatProp));
            }
            else if (type == TweenStepType.Jump)
            {
                AddDetailField("目标物体", DetailFieldFactory.CreateObjectField(targetTransformProp, typeof(Transform), onObjectFieldChanged));

                AddSeparator();

                AddDetailField("使用起始位置", DetailFieldFactory.CreateToggle(useStartValueProp, onToggleRebuild));

                if (useStartValueProp.boolValue)
                {
                    AddDetailField("起始位置", DetailFieldFactory.CreateVector3Field(startVectorProp));
                }

                AddDetailField("目标位置", DetailFieldFactory.CreateVector3Field(targetVectorProp));

                AddSeparator();

                AddDetailField("跳跃高度", DetailFieldFactory.CreateFloatField(jumpHeightProp));
                AddDetailField("跳跃次数", DetailFieldFactory.CreateIntegerField(jumpNumProp));
            }
            else if (type == TweenStepType.Punch)
            {
                AddDetailField("目标物体", DetailFieldFactory.CreateObjectField(targetTransformProp, typeof(Transform), onObjectFieldChanged));

                AddSeparator();

                AddDetailField("冲击目标", DetailFieldFactory.CreateEnumField(punchTargetProp, typeof(PunchTarget)));
                AddDetailField("强度", DetailFieldFactory.CreateVector3Field(intensityProp));
                AddDetailField("震荡次数", DetailFieldFactory.CreateIntegerField(vibratoProp));
                AddDetailField("弹性", DetailFieldFactory.CreateFloatField(elasticityProp));
            }
            else if (type == TweenStepType.Shake)
            {
                AddDetailField("目标物体", DetailFieldFactory.CreateObjectField(targetTransformProp, typeof(Transform), onObjectFieldChanged));

                AddSeparator();

                AddDetailField("震动目标", DetailFieldFactory.CreateEnumField(shakeTargetProp, typeof(ShakeTarget)));
                AddDetailField("强度", DetailFieldFactory.CreateVector3Field(intensityProp));
                AddDetailField("震荡次数", DetailFieldFactory.CreateIntegerField(vibratoProp));
                AddDetailField("弹性", DetailFieldFactory.CreateFloatField(elasticityProp));
                AddDetailField("随机性", DetailFieldFactory.CreateFloatField(shakeRandomnessProp));
            }
            else if (type == TweenStepType.FillAmount)
            {
                AddDetailField("目标物体", DetailFieldFactory.CreateObjectField(targetTransformProp, typeof(Transform), onObjectFieldChanged));
                AddValidationWarning(targetTransformProp, TweenStepType.FillAmount);

                AddSeparator();

                AddDetailField("使用起始填充量", DetailFieldFactory.CreateToggle(useStartFloatProp, onToggleRebuild));

                if (useStartFloatProp.boolValue)
                {
                    AddDetailField("起始填充量", DetailFieldFactory.CreateFloatField(startFloatProp));
                }

                AddDetailField("目标填充量", DetailFieldFactory.CreateFloatField(targetFloatProp));
            }
            else if (type == TweenStepType.DOPath)
            {
                AddDetailField("目标物体", DetailFieldFactory.CreateObjectField(targetTransformProp, typeof(Transform), onObjectFieldChanged));

                AddSeparator();

                AddDetailField("使用起始位置", DetailFieldFactory.CreateToggle(useStartValueProp, onToggleRebuild));

                if (useStartValueProp.boolValue)
                {
                    AddDetailField("起始位置", DetailFieldFactory.CreateVector3Field(startVectorProp));
                }

                var waypointsProp = stepProperty.FindPropertyRelative("PathWaypoints");
                AddPathWaypointsEditor(waypointsProp);

                AddSeparator();

                AddDetailField("路径类型", DetailFieldFactory.CreatePathTypeEnumField(stepProperty.FindPropertyRelative("PathType")));
                AddDetailField("路径模式", DetailFieldFactory.CreatePathModeEnumField(stepProperty.FindPropertyRelative("PathMode")));
                AddDetailField("路径分辨率", DetailFieldFactory.CreateIntegerField(stepProperty.FindPropertyRelative("PathResolution")));
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
                AddDetailField("执行模式", DetailFieldFactory.CreateEnumField(executionModeProp, typeof(ExecutionMode), onEnumRebuild));
                if ((ExecutionMode)executionModeProp.enumValueIndex == ExecutionMode.Insert)
                {
                    AddDetailField("插入时间", DetailFieldFactory.CreateFloatField(insertTimeProp, onTimingChanged));
                }
            }
            else if (type == TweenStepType.Punch || type == TweenStepType.Shake)
            {
                AddDetailField("执行模式", DetailFieldFactory.CreateEnumField(executionModeProp, typeof(ExecutionMode), onEnumRebuild));
                if ((ExecutionMode)executionModeProp.enumValueIndex == ExecutionMode.Insert)
                {
                    AddDetailField("插入时间", DetailFieldFactory.CreateFloatField(insertTimeProp, onTimingChanged));
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
                AddDetailField("执行模式", DetailFieldFactory.CreateEnumField(executionModeProp, typeof(ExecutionMode), onEnumRebuild));
                if ((ExecutionMode)executionModeProp.enumValueIndex == ExecutionMode.Insert)
                {
                    AddDetailField("插入时间", DetailFieldFactory.CreateFloatField(insertTimeProp, onTimingChanged));
                }

                AddDetailField("缓动", DetailFieldFactory.CreateEnumField(easeProp, typeof(Ease)));

                AddDetailField("自定义曲线", DetailFieldFactory.CreateToggle(useCustomCurveProp, onToggleRebuild));

                if (useCustomCurveProp.boolValue && customCurveProp != null)
                {
                    AddDetailField("曲线", DetailFieldFactory.CreateCurveField(customCurveProp));
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
        /// 添加路径点列表编辑器（支持增删改路径点）
        /// </summary>
        private void AddPathWaypointsEditor(SerializedProperty waypointsProp)
        {
            if (waypointsProp == null || !waypointsProp.isArray) return;

            var container = new VisualElement();
            container.AddToClassList("path-waypoints-container");

            var headerRow = new VisualElement();
            headerRow.style.flexDirection = FlexDirection.Row;
            headerRow.style.alignItems = Align.Center;
            headerRow.style.marginBottom = 4f;

            var countLabel = new Label($"路径点 ({waypointsProp.arraySize} 个)");
            countLabel.style.fontSize = 11f;
            countLabel.style.color = new Color(0.85f, 0.75f, 0.5f);
            countLabel.style.flexGrow = 1;
            headerRow.Add(countLabel);

            var addButton = new Button(() => AddPathWaypoint(waypointsProp, countLabel)) { text = "＋ 添加" };
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
                        Undo.RecordObject(_getTargetPlayer(), "修改路径点");
                        _getSerializedObject()?.Update();
                        var v = waypointsProp.GetArrayElementAtIndex(idx).vector3Value;
                        v.x = val;
                        waypointsProp.GetArrayElementAtIndex(idx).vector3Value = v;
                        waypointsProp.serializedObject.ApplyModifiedProperties();
                    });
                xField.style.width = Length.Percent(32f);
                xField.style.marginRight = 1f;
                pointRow.Add(xField);

                var yField = DetailFieldFactory.CreatePathCoordFloatField("Y", currentVal.y,
                    val =>
                    {
                        if (!DetailFieldFactory.IsValidProperty(wp)) return;
                        Undo.RecordObject(_getTargetPlayer(), "修改路径点");
                        _getSerializedObject()?.Update();
                        var v = waypointsProp.GetArrayElementAtIndex(idx).vector3Value;
                        v.y = val;
                        waypointsProp.GetArrayElementAtIndex(idx).vector3Value = v;
                        waypointsProp.serializedObject.ApplyModifiedProperties();
                    });
                yField.style.width = Length.Percent(32f);
                yField.style.marginRight = 1f;
                pointRow.Add(yField);

                var zField = DetailFieldFactory.CreatePathCoordFloatField("Z", currentVal.z,
                    val =>
                    {
                        if (!DetailFieldFactory.IsValidProperty(wp)) return;
                        Undo.RecordObject(_getTargetPlayer(), "修改路径点");
                        _getSerializedObject()?.Update();
                        var v = waypointsProp.GetArrayElementAtIndex(idx).vector3Value;
                        v.z = val;
                        waypointsProp.GetArrayElementAtIndex(idx).vector3Value = v;
                        waypointsProp.serializedObject.ApplyModifiedProperties();
                    });
                zField.style.width = Length.Percent(32f);
                pointRow.Add(zField);

                var delBtn = new Button(() =>
                {
                    RemovePathWaypoint(waypointsProp, idx);
                }) { text = "✕" };
                delBtn.style.fontSize = 9f;
                delBtn.style.color = new Color(0.9f, 0.4f, 0.4f);
                delBtn.style.width = 18f;
                delBtn.style.height = 18f;
                delBtn.style.flexShrink = 0;
                delBtn.style.marginLeft = 2f;
                pointRow.Add(delBtn);

                container.Add(pointRow);
            }

            _detailScrollView.Add(container);
        }

        private void AddPathWaypoint(SerializedProperty waypointsProp, Label countLabel)
        {
            if (waypointsProp == null) return;
            Undo.RecordObject(_getTargetPlayer(), "添加路径点");
            _getSerializedObject()?.Update();

            Vector3 lastPos = Vector3.zero;
            if (waypointsProp.arraySize > 0)
            {
                lastPos = waypointsProp.GetArrayElementAtIndex(waypointsProp.arraySize - 1).vector3Value;
            }

            waypointsProp.InsertArrayElementAtIndex(waypointsProp.arraySize);
            var newWp = waypointsProp.GetArrayElementAtIndex(waypointsProp.arraySize - 1);
            newWp.vector3Value = lastPos + new Vector3(1f, 0f, 0f);
            waypointsProp.serializedObject.ApplyModifiedProperties();

            _onRefreshDetail();
        }

        private void RemovePathWaypoint(SerializedProperty waypointsProp, int index)
        {
            if (waypointsProp == null || index < 0 || index >= waypointsProp.arraySize) return;
            if (waypointsProp.arraySize <= 1)
            {
                DOTweenLog.Warning("至少需要保留一个路径点");
                return;
            }

            Undo.RecordObject(_getTargetPlayer(), "删除路径点");
            _getSerializedObject()?.Update();
            waypointsProp.DeleteArrayElementAtIndex(index);
            waypointsProp.serializedObject.ApplyModifiedProperties();

            _onRefreshDetail();
        }

        #endregion

        #region 同步

        /// <summary>
        /// 同步当前选中步骤的目标值为物体当前值
        /// </summary>
        public void OnSyncClicked()
        {
            int selectedIndex = _getSelectedIndex();
            var stepsProperty = _getStepsProperty();
            if (selectedIndex < 0 || stepsProperty == null || selectedIndex >= stepsProperty.arraySize) return;

            var targetPlayer = _getTargetPlayer();
            Undo.RecordObject(targetPlayer, "同步当前值");
            _getSerializedObject()?.Update();
            var stepProperty = stepsProperty.GetArrayElementAtIndex(selectedIndex);
            var type = (TweenStepType)stepProperty.FindPropertyRelative("Type").enumValueIndex;
            var targetTransformProp = stepProperty.FindPropertyRelative("TargetTransform");
            var target = targetTransformProp.objectReferenceValue as Transform;
            if (target == null && targetPlayer != null) target = targetPlayer.transform;
            if (target == null) return;

            switch (type)
            {
                case TweenStepType.Move:
                    var moveSpace = (MoveSpace)stepProperty.FindPropertyRelative("MoveSpace").enumValueIndex;
                    stepProperty.FindPropertyRelative("TargetVector").vector3Value =
                        moveSpace == MoveSpace.Local ? target.localPosition : target.position;
                    break;
                case TweenStepType.Rotate:
                    var rotateSpace = (RotateSpace)stepProperty.FindPropertyRelative("RotateSpace").enumValueIndex;
                    stepProperty.FindPropertyRelative("TargetVector").vector3Value =
                        rotateSpace == RotateSpace.Local ? target.localRotation.eulerAngles : target.rotation.eulerAngles;
                    break;
                case TweenStepType.Scale:
                    stepProperty.FindPropertyRelative("TargetVector").vector3Value = target.localScale;
                    break;
                case TweenStepType.Color:
                    if (TweenValueHelper.TryGetColor(target, out Color currentColor))
                        stepProperty.FindPropertyRelative("TargetColor").colorValue = currentColor;
                    break;
                case TweenStepType.Fade:
                    if (TweenValueHelper.TryGetAlpha(target, out float currentAlpha))
                        stepProperty.FindPropertyRelative("TargetFloat").floatValue = currentAlpha;
                    break;
                case TweenStepType.AnchorMove:
                    if (TweenValueHelper.TryGetRectTransform(target, out var rt1))
                        stepProperty.FindPropertyRelative("TargetVector").vector3Value = rt1.anchoredPosition;
                    break;
                case TweenStepType.SizeDelta:
                    if (TweenValueHelper.TryGetRectTransform(target, out var rt2))
                        stepProperty.FindPropertyRelative("TargetVector").vector3Value = rt2.sizeDelta;
                    break;
                case TweenStepType.Jump:
                    stepProperty.FindPropertyRelative("TargetVector").vector3Value = target.position;
                    break;
                case TweenStepType.FillAmount:
                    var image = target.GetComponent<UnityEngine.UI.Image>();
                    if (image != null)
                        stepProperty.FindPropertyRelative("TargetFloat").floatValue = image.fillAmount;
                    break;
                case TweenStepType.DOPath:
                    stepProperty.FindPropertyRelative("TargetVector").vector3Value = target.position;
                    break;
            }

            stepsProperty.serializedObject.ApplyModifiedProperties();
            _onRefreshDetail();
            _onRebuildList();
        }

        #endregion
    }
}
#endif
