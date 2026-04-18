#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using DG.Tweening;
using UnityEditor.Compilation;
using CNoom.DOTweenVisual.Components;
using CNoom.DOTweenVisual.Data;
using UnityEditor.UIElements;
// 注意：UnityEngine.UI.Image 通过完全限定名使用，避免与 UIElements 冲突

namespace CNoom.DOTweenVisual.Editor
{
    /// <summary>
    /// DOTween 可视化编辑器主窗口
    /// 布局：顶部工具栏+状态栏 | 左侧步骤概览(ListView) | 右侧步骤详情
    /// 使用 ListView 实现步骤列表，内置拖拽排序和选择管理
    /// 预览逻辑委托给 DOTweenPreviewManager，样式配置委托给 DOTweenEditorStyle
    /// </summary>
    [InitializeOnLoad]
    public class DOTweenVisualEditorWindow : EditorWindow
    {
        #region 静态初始化

        static DOTweenVisualEditorWindow()
        {
            CompilationPipeline.compilationStarted -= OnCompilationStarted;
            CompilationPipeline.compilationStarted += OnCompilationStarted;
        }

        private static void OnCompilationStarted(object obj)
        {
            var windows = Resources.FindObjectsOfTypeAll<DOTweenVisualEditorWindow>();
            foreach (var window in windows)
            {
                if (window._previewManager != null && window._previewManager.State != DOTweenPreviewManager.PreviewState.None)
                {
                    window._previewManager.Reset();
                }
            }
        }

        #endregion

        #region 常量

        private const float LeftPanelMinWidth = 220f;

        #endregion

        #region UI 元素

        // 顶部
        private ObjectField targetField;
        private Button previewButton;
        private Button stopButton;
        private Button replayButton;
        private Button resetButton;
        private ToolbarMenu addStepMenu;

        // 状态栏
        private Label stateLabel;
        private Label timeLabel;

        // 左侧概览
        private ListView stepListView;
        private int selectedStepIndex = -1;
        private float totalSequenceDuration;
        private float[] stepStartTimes;

        // 右侧详情
        private VisualElement detailPanel;
        private Label detailHelpLabel;
        private ScrollView detailScrollView;

        #endregion

        #region 数据

        private DOTweenVisualPlayer targetPlayer;
        private SerializedObject serializedObject;
        private SerializedProperty stepsProperty;

        #endregion

        #region 预览管理

        private DOTweenPreviewManager _previewManager;

        #endregion

        #region 生命周期

        [MenuItem("Tools/DOTween Visual Editor")]
        public static void ShowWindow()
        {
            var window = GetWindow<DOTweenVisualEditorWindow>("DOTween Visual Editor");
            window.minSize = new Vector2(600, 400);
            window.Show();
        }

        private void OnEnable()
        {
            _previewManager = new DOTweenPreviewManager();
            _previewManager.StateChanged += OnPreviewStateChanged;
            EditorApplication.update += OnEditorUpdate;
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        }

        private void OnDisable()
        {
            EditorApplication.update -= OnEditorUpdate;
            EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
            _previewManager.StateChanged -= OnPreviewStateChanged;
            _previewManager.Dispose();
            _previewManager = null;
            rootVisualElement.Clear();
        }

        private void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.ExitingEditMode)
            {
                if (_previewManager != null && _previewManager.State != DOTweenPreviewManager.PreviewState.None)
                {
                    _previewManager.Reset();
                }
            }
        }

        private void OnEditorUpdate()
        {
            UpdateTimeDisplay();
        }

        private void UpdateTimeDisplay()
        {
            if (timeLabel == null) return;

            var sequence = _previewManager?.PreviewSequence;
            if (sequence == null)
            {
                timeLabel.text = "--:-- / --:--";
                return;
            }

            float currentTime = sequence.Elapsed(false);
            float totalTime = sequence.Duration(false);
            timeLabel.text = $"{FormatTime(currentTime)} / {FormatTime(totalTime)}";
        }

        private string FormatTime(float seconds)
        {
            int minutes = (int)(seconds / 60);
            int secs = (int)(seconds % 60);
            int ms = (int)((seconds * 10) % 10);
            return $"{minutes:D2}:{secs:D2}.{ms}";
        }

        private void OnTargetChanged(ChangeEvent<UnityEngine.Object> evt)
        {
            if (_previewManager.IsPlaying || _previewManager.IsPaused)
            {
                _previewManager.StopPreview();
            }

            var player = evt.newValue as DOTweenVisualPlayer;
            SetTarget(player);
            UpdateButtonStates();
        }

        private void SetTarget(DOTweenVisualPlayer player)
        {
            targetPlayer = player;
            _previewManager?.SetTarget(player);

            if (player != null)
            {
                serializedObject = new SerializedObject(player);
                stepsProperty = serializedObject.FindProperty("_steps");
            }
            else
            {
                serializedObject = null;
                stepsProperty = null;
            }

            selectedStepIndex = -1;
            RebuildStepList();
            RefreshDetailPanel();
        }

        #endregion

        #region UI 构建

        private void CreateGUI()
        {
            BuildUI();

            // 加载 USS 样式表（必须确保加载成功）
            var styleSheet = DOTweenEditorStyle.FindStyleSheet();
            if (styleSheet != null)
            {
                rootVisualElement.styleSheets.Add(styleSheet);
            }
            else
            {
                Debug.LogError("[DOTweenVisual] 样式表加载失败！请检查 Editor/USS/DOTweenVisualEditor.uss 是否存在且已被 Unity 导入");
            }

            if (targetPlayer != null)
            {
                SetTarget(targetPlayer);
            }
        }

        private void BuildUI()
        {
            rootVisualElement.Clear();

            // === 顶部：工具栏 ===
            var toolbar = new VisualElement();
            toolbar.AddToClassList("top-toolbar");

            var targetLabel = new Label("目标物体:");
            targetLabel.AddToClassList("toolbar-label");
            toolbar.Add(targetLabel);

            targetField = new ObjectField
            {
                objectType = typeof(DOTweenVisualPlayer),
                allowSceneObjects = true,
                value = targetPlayer
            };
            targetField.AddToClassList("target-field");
            targetField.RegisterValueChangedCallback(OnTargetChanged);
            toolbar.Add(targetField);

            var spacer1 = new VisualElement { style = { flexGrow = 1 } };
            toolbar.Add(spacer1);

            previewButton = new Button(OnPreviewClicked) { text = "预览" };
            previewButton.AddToClassList("toolbar-button");
            toolbar.Add(previewButton);

            stopButton = new Button(OnStopClicked) { text = "停止" };
            stopButton.AddToClassList("toolbar-button");
            toolbar.Add(stopButton);

            replayButton = new Button(OnReplayClicked) { text = "重播" };
            replayButton.AddToClassList("toolbar-button");
            toolbar.Add(replayButton);

            resetButton = new Button(OnResetClicked) { text = "重置" };
            resetButton.AddToClassList("toolbar-button");
            toolbar.Add(resetButton);

            rootVisualElement.Add(toolbar);

            // === 顶部：状态栏 ===
            var statusBar = new VisualElement();
            statusBar.AddToClassList("status-bar");

            stateLabel = new Label("● 未播放");
            stateLabel.AddToClassList("state-label");
            statusBar.Add(stateLabel);

            timeLabel = new Label("--:-- / --:--");
            timeLabel.AddToClassList("time-label");
            statusBar.Add(timeLabel);

            rootVisualElement.Add(statusBar);

            // === 下方：左右分栏 ===
            var splitView = new TwoPaneSplitView(0, LeftPanelMinWidth + 80, TwoPaneSplitViewOrientation.Horizontal);

            // --- 左侧：步骤概览 ---
            var leftPanel = new VisualElement();
            leftPanel.AddToClassList("left-panel");

            var leftHeader = new VisualElement();
            leftHeader.AddToClassList("panel-header");
            var leftTitle = new Label("步骤概览");
            leftTitle.AddToClassList("panel-title");
            leftHeader.Add(leftTitle);

            addStepMenu = new ToolbarMenu { text = "＋ 添加" };
            addStepMenu.AddToClassList("add-step-menu");
            leftHeader.Add(addStepMenu);
            leftPanel.Add(leftHeader);

            // ListView 步骤列表
            stepListView = new ListView
            {
                selectionType = SelectionType.Single,
                reorderable = true,
                showAddRemoveFooter = false,
                showBorder = false,
                showFoldoutHeader = false,
                virtualizationMethod = CollectionVirtualizationMethod.DynamicHeight,
            };
            stepListView.AddToClassList("step-list");
            stepListView.style.flexGrow = 1;
            stepListView.makeItem = MakeStepItem;
            stepListView.bindItem = BindStepItem;
            stepListView.unbindItem = UnbindStepItem;
            stepListView.destroyItem = DestroyStepItem;
            stepListView.itemsRemoved += OnStepsRemoved;
            stepListView.itemIndexChanged += OnStepIndexChanged;
            stepListView.selectionChanged += OnStepSelectionChanged;
            leftPanel.Add(stepListView);

            splitView.Add(leftPanel);

            // --- 右侧：步骤详情 ---
            var rightPanel = new VisualElement();
            rightPanel.AddToClassList("right-panel");

            var rightHeader = new VisualElement();
            rightHeader.AddToClassList("panel-header");
            var rightTitle = new Label("步骤详情");
            rightTitle.AddToClassList("panel-title");
            rightHeader.Add(rightTitle);

            var syncButton = new Button(OnSyncClicked) { text = "同步当前值" };
            syncButton.AddToClassList("sync-button");
            rightHeader.Add(syncButton);

            rightPanel.Add(rightHeader);

            detailPanel = new VisualElement();
            detailPanel.AddToClassList("detail-content");
            detailPanel.style.flexGrow = 1;

            detailHelpLabel = new Label("请在左侧选择一个步骤");
            detailHelpLabel.AddToClassList("detail-help-label");
            detailPanel.Add(detailHelpLabel);

            detailScrollView = new ScrollView(ScrollViewMode.Vertical);
            detailScrollView.AddToClassList("detail-scroll");
            detailScrollView.style.display = DisplayStyle.None;
            detailPanel.Add(detailScrollView);

            rightPanel.Add(detailPanel);
            splitView.Add(rightPanel);

            rootVisualElement.Add(splitView);

            BuildAddStepMenu();
            UpdateButtonStates();
        }

        #endregion

        #region 步骤列表 - ListView

        /// <summary>
        /// 刷新列表数据源（重新绑定 SerializedProperty）
        /// </summary>
        private void RebuildStepList()
        {
            if (stepListView == null) return;

            if (stepsProperty == null || serializedObject == null)
            {
                selectedStepIndex = -1;
                stepListView.itemsSource = null;
                return;
            }

            serializedObject.Update();
            CalculateStepTimings();

            // 使用 SerializedProperty 作为数据源
            stepListView.itemsSource = new SerializedPropertyArray(stepsProperty);
            stepListView.Rebuild();

            // 恢复选中状态
            if (selectedStepIndex >= stepsProperty.arraySize) selectedStepIndex = -1;
            if (selectedStepIndex >= 0)
                stepListView.SetSelection(selectedStepIndex);
            else
                stepListView.ClearSelection();

            UpdateButtonStates();
        }

        /// <summary>
        /// 根据执行模式计算每个步骤的开始时间
        /// </summary>
        private void CalculateStepTimings()
        {
            if (stepsProperty == null || stepsProperty.arraySize == 0)
            {
                totalSequenceDuration = 0;
                stepStartTimes = Array.Empty<float>();
                return;
            }

            int count = stepsProperty.arraySize;
            stepStartTimes = new float[count];
            float lastTweenStartTime = 0f;
            float sequenceEndTime = 0f;

            for (int i = 0; i < count; i++)
            {
                var step = stepsProperty.GetArrayElementAtIndex(i);
                var type = (TweenStepType)step.FindPropertyRelative("Type").enumValueIndex;
                var duration = Mathf.Max(0.001f, step.FindPropertyRelative("Duration").floatValue);
                float startTime;

                // Delay 和 Callback 始终视为 Append
                if (type == TweenStepType.Callback)
                {
                    startTime = sequenceEndTime;
                    lastTweenStartTime = startTime;
                }
                else if (type == TweenStepType.Delay)
                {
                    startTime = sequenceEndTime;
                    lastTweenStartTime = startTime;
                    sequenceEndTime = startTime + duration;
                }
                else
                {
                    var mode = (ExecutionMode)step.FindPropertyRelative("ExecutionMode").enumValueIndex;
                    switch (mode)
                    {
                        case ExecutionMode.Append:
                            startTime = sequenceEndTime;
                            lastTweenStartTime = startTime;
                            sequenceEndTime = startTime + duration;
                            break;
                        case ExecutionMode.Join:
                            startTime = lastTweenStartTime;
                            if (startTime + duration > sequenceEndTime)
                                sequenceEndTime = startTime + duration;
                            break;
                        case ExecutionMode.Insert:
                            startTime = step.FindPropertyRelative("InsertTime").floatValue;
                            lastTweenStartTime = startTime;
                            if (startTime + duration > sequenceEndTime)
                                sequenceEndTime = startTime + duration;
                            break;
                        default:
                            startTime = sequenceEndTime;
                            lastTweenStartTime = startTime;
                            sequenceEndTime = startTime + duration;
                            break;
                    }
                }

                stepStartTimes[i] = startTime;
            }

            totalSequenceDuration = sequenceEndTime;
        }

        /// <summary>
        /// 创建步骤行模板（可复用）
        /// </summary>
        private VisualElement MakeStepItem()
        {
            var item = new VisualElement();
            item.AddToClassList("step-item");

            var row = new VisualElement();
            row.AddToClassList("step-row");

            // 步骤类型色块
            var typeDot = new VisualElement();
            typeDot.AddToClassList("step-type-dot");
            row.Add(typeDot);

            // 启用开关
            var enableToggle = new Toggle();
            enableToggle.AddToClassList("step-enable-toggle");
            enableToggle.RegisterValueChangedCallback(evt =>
            {
                var data = item.userData as StepItemData;
                if (data == null || data.Property == null) return;
                var prop = data.Property;
                int idx = data.OriginalIndex;
                if (stepsProperty == null || idx < 0 || idx >= stepsProperty.arraySize) return;
                var targetProp = stepsProperty.GetArrayElementAtIndex(idx);
                targetProp.FindPropertyRelative("IsEnabled").boolValue = evt.newValue;
                targetProp.serializedObject.ApplyModifiedProperties();
                item.EnableInClassList("step-disabled", !evt.newValue);
            });
            enableToggle.RegisterCallback<ClickEvent>(e => e.StopPropagation());
            row.Add(enableToggle);

            // 标题
            var titleLabel = new Label();
            titleLabel.AddToClassList("step-title");
            row.Add(titleLabel);

            // 删除按钮
            var deleteButton = new Button { text = "✕" };
            deleteButton.AddToClassList("step-delete-button");
            deleteButton.RegisterCallback<ClickEvent>(e =>
            {
                var data = item.userData as StepItemData;
                if (data == null || data.Property == null) return;
                int idx = data.OriginalIndex;
                if (stepsProperty == null || idx < 0 || idx >= stepsProperty.arraySize) return;

                stepsProperty.DeleteArrayElementAtIndex(idx);
                stepsProperty.serializedObject.ApplyModifiedProperties();

                if (selectedStepIndex == idx)
                    selectedStepIndex = -1;
                else if (selectedStepIndex > idx)
                    selectedStepIndex--;

                RebuildStepList();
                RefreshDetailPanel();
                e.StopPropagation();
            });
            row.Add(deleteButton);

            item.Add(row);

            // 摘要行
            var summaryRow = new VisualElement();
            summaryRow.AddToClassList("step-summary-row");
            var summaryLabel = new Label();
            summaryLabel.AddToClassList("step-summary");
            summaryRow.Add(summaryLabel);
            item.Add(summaryRow);

            // 时间轴条
            var timelineTrack = new VisualElement();
            timelineTrack.AddToClassList("step-timeline-track");
            var timelineBar = new VisualElement();
            timelineBar.AddToClassList("step-timeline-bar");
            timelineTrack.Add(timelineBar);
            item.Add(timelineTrack);

            return item;
        }

        /// <summary>
        /// 绑定数据到步骤行
        /// </summary>
        private void BindStepItem(VisualElement element, int index)
        {
            if (stepsProperty == null || index < 0 || index >= stepsProperty.arraySize) return;

            var stepProperty = stepsProperty.GetArrayElementAtIndex(index);
            var typeProp = stepProperty.FindPropertyRelative("Type");
            var isEnabledProp = stepProperty.FindPropertyRelative("IsEnabled");
            var durationProp = stepProperty.FindPropertyRelative("Duration");
            var easeProp = stepProperty.FindPropertyRelative("Ease");
            var targetTransformProp = stepProperty.FindPropertyRelative("TargetTransform");

            var type = (TweenStepType)typeProp.enumValueIndex;
            var ease = (Ease)easeProp.enumValueIndex;
            string targetName = targetTransformProp.objectReferenceValue != null
                ? targetTransformProp.objectReferenceValue.name
                : "未指定";

            // 存储 userData 用于回调
            element.userData = new StepItemData(stepProperty, index);

            // 更新样式（注意：ClearClassList 后需重新添加基础类）
            element.ClearClassList();
            element.AddToClassList("step-item");

            if (!isEnabledProp.boolValue) element.AddToClassList("step-disabled");

            // 绑定 Toggle
            var toggle = element.Q<Toggle>(className: "step-enable-toggle");
            if (toggle != null) toggle.SetValueWithoutNotify(isEnabledProp.boolValue);

            // 绑定标题
            var titleLabel = element.Q<Label>(className: "step-title");
            if (titleLabel != null) titleLabel.text = $"{index + 1}. [{targetName}] {DOTweenEditorStyle.GetStepDisplayName(type)}";

            // 绑定摘要
            var summaryLabel = element.Q<Label>(className: "step-summary");
            if (summaryLabel != null) summaryLabel.text = $"{durationProp.floatValue:F1}s | {ease}";

            // 时间轴位置（通过内联样式确保可见性，不依赖 USS 加载状态）
            var timelineTrack = element.Q<VisualElement>(className: "step-timeline-track");
            if (timelineTrack != null)
            {
                timelineTrack.style.height = 4f;
                timelineTrack.style.position = Position.Relative;
                timelineTrack.style.marginTop = 3f;
            }

            var timelineBar = element.Q<VisualElement>(className: "step-timeline-bar");
            if (timelineBar != null && stepStartTimes != null && index < stepStartTimes.Length)
            {
                float start = stepStartTimes[index];
                float dur = durationProp.floatValue;
                float total = Mathf.Max(0.001f, totalSequenceDuration);

                timelineBar.style.position = Position.Absolute;
                timelineBar.style.height = Length.Percent(100f);
                timelineBar.style.left = Length.Percent(Mathf.Min(start / total * 100f, 97f));
                timelineBar.style.width = Length.Percent(Mathf.Max(3f, dur / total * 100f));

                // 根据执行模式设置时间轴颜色
                Color barColor;
                if (type == TweenStepType.Delay || type == TweenStepType.Callback)
                    barColor = new Color(0.44f, 0.44f, 0.44f);
                else
                {
                    var execMode = (ExecutionMode)stepProperty.FindPropertyRelative("ExecutionMode").enumValueIndex;
                    barColor = DOTweenEditorStyle.GetExecutionModeColor(execMode);
                }
                timelineBar.style.backgroundColor = barColor;
            }
        }

        private void UnbindStepItem(VisualElement element, int index)
        {
            element.userData = null;
        }

        private void DestroyStepItem(VisualElement element)
        {
            element.userData = null;
        }

        /// <summary>
        /// ListView 选择变化回调
        /// </summary>
        private void OnStepSelectionChanged(IEnumerable<object> selectedItems)
        {
            using var enumerator = selectedItems?.GetEnumerator();
            if (enumerator != null && enumerator.MoveNext())
            {
                selectedStepIndex = stepListView.selectedIndex;
            }
            else
            {
                selectedStepIndex = -1;
            }

            RefreshDetailPanel();
        }

        /// <summary>
        /// ListView 拖拽排序后同步到 SerializedProperty
        /// </summary>
        private void OnStepIndexChanged(int from, int to)
        {
            if (stepsProperty == null) return;
            stepsProperty.MoveArrayElement(from, to);
            stepsProperty.serializedObject.ApplyModifiedProperties();
            CalculateStepTimings();

            // 同步选中索引
            if (selectedStepIndex == from)
                selectedStepIndex = to;
            else if (from < selectedStepIndex && to >= selectedStepIndex)
                selectedStepIndex--;
            else if (from > selectedStepIndex && to <= selectedStepIndex)
                selectedStepIndex++;

            RefreshDetailPanel();
        }

        /// <summary>
        /// ListView 删除元素回调
        /// </summary>
        private void OnStepsRemoved(IEnumerable<int> removedIndices)
        {
            foreach (var idx in removedIndices)
            {
                if (selectedStepIndex == idx)
                    selectedStepIndex = -1;
                else if (selectedStepIndex > idx)
                    selectedStepIndex--;
            }

            RefreshDetailPanel();
        }

        /// <summary>
        /// 步骤行辅助数据
        /// </summary>
        private class StepItemData
        {
            public SerializedProperty Property { get; }
            public int OriginalIndex { get; }

            public StepItemData(SerializedProperty property, int index)
            {
                Property = property;
                OriginalIndex = index;
            }
        }

        /// <summary>
        /// 简易 SerializedProperty 数组包装，用作 ListView itemsSource
        /// </summary>
        private class SerializedPropertyArray : IList
        {
            private readonly SerializedProperty _property;

            public SerializedPropertyArray(SerializedProperty property)
            {
                _property = property;
            }

            public int Count => _property.isArray ? _property.arraySize : 0;
            public bool IsFixedSize => false;
            public bool IsReadOnly => true;
            public bool IsSynchronized => false;
            public object SyncRoot => this;

            public object this[int index]
            {
                get => _property.GetArrayElementAtIndex(index);
                set { }
            }

            public IEnumerator GetEnumerator()
            {
                for (int i = 0; i < Count; i++)
                    yield return _property.GetArrayElementAtIndex(i);
            }

            public int Add(object value) => -1;
            public void Clear() { }
            public bool Contains(object value) => false;
            public int IndexOf(object value) => -1;
            public void Insert(int index, object value) { }
            public void Remove(object value) { }
            public void RemoveAt(int index) { }

            public void CopyTo(Array array, int index)
            {
                for (int i = 0; i < Count; i++)
                    array.SetValue(_property.GetArrayElementAtIndex(i), index + i);
            }
        }

        #endregion

        #region 步骤详情

        private void RefreshDetailPanel()
        {
            if (detailPanel == null) return;

            if (selectedStepIndex < 0 || stepsProperty == null || selectedStepIndex >= stepsProperty.arraySize)
            {
                detailHelpLabel.style.display = DisplayStyle.Flex;
                detailScrollView.style.display = DisplayStyle.None;
                detailScrollView.Clear();
                return;
            }

            serializedObject?.Update();
            var stepProperty = stepsProperty.GetArrayElementAtIndex(selectedStepIndex);

            detailHelpLabel.style.display = DisplayStyle.None;
            detailScrollView.style.display = DisplayStyle.Flex;
            BuildDetailFields(stepProperty);
        }

        private void BuildDetailFields(SerializedProperty stepProperty)
        {
            detailScrollView.Clear();

            var typeProp = stepProperty.FindPropertyRelative("Type");
            var isEnabledProp = stepProperty.FindPropertyRelative("IsEnabled");
            var durationProp = stepProperty.FindPropertyRelative("Duration");
            var delayProp = stepProperty.FindPropertyRelative("Delay");
            var easeProp = stepProperty.FindPropertyRelative("Ease");
            var executionModeProp = stepProperty.FindPropertyRelative("ExecutionMode");
            var insertTimeProp = stepProperty.FindPropertyRelative("InsertTime");
            var transformTargetProp = stepProperty.FindPropertyRelative("TransformTarget");
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

            // --- 通用字段 ---
            AddDetailField("类型", CreateEnumField(typeProp, typeof(TweenStepType), OnTypeChanged));
            AddDetailField("启用", CreateToggle(isEnabledProp));
            AddDetailField("时长", CreateFloatField(durationProp, OnTimingChanged));
            AddDetailField("延迟", CreateFloatField(delayProp));

            // --- 按类型显示 ---
            bool isTransformType = type == TweenStepType.Move || type == TweenStepType.Rotate || type == TweenStepType.Scale;

            if (isTransformType)
            {
                AddDetailField("目标类型", CreateEnumField(transformTargetProp, typeof(TransformTarget)));
                AddDetailField("目标物体", CreateObjectField(targetTransformProp, typeof(Transform)));
                AddDetailField("相对模式", CreateToggle(isRelativeProp));

                AddSeparator();

                AddDetailField("使用起始值", CreateToggle(useStartValueProp, OnToggleRebuild));

                if (useStartValueProp.boolValue)
                {
                    string startLabel = type == TweenStepType.Rotate ? "起始旋转" : "起始值";
                    AddDetailField(startLabel, CreateVector3Field(startVectorProp));
                }

                string targetLabel = type == TweenStepType.Rotate ? "目标值 (欧拉角)" : "目标值";
                AddDetailField(targetLabel, CreateVector3Field(targetVectorProp));
            }
            else if (type == TweenStepType.AnchorMove || type == TweenStepType.SizeDelta)
            {
                AddDetailField("目标物体", CreateObjectField(targetTransformProp, typeof(Transform)));
                AddValidationWarning(targetTransformProp, type);
                AddDetailField("相对模式", CreateToggle(isRelativeProp));

                AddSeparator();

                AddDetailField("使用起始值", CreateToggle(useStartValueProp, OnToggleRebuild));

                if (useStartValueProp.boolValue)
                {
                    string startLabel = type == TweenStepType.AnchorMove ? "起始锚点位置" : "起始尺寸";
                    AddDetailField(startLabel, CreateVector3Field(startVectorProp));
                }

                string targetLabel = type == TweenStepType.AnchorMove ? "目标锚点位置" : "目标尺寸";
                AddDetailField(targetLabel, CreateVector3Field(targetVectorProp));
            }
            else if (type == TweenStepType.Color)
            {
                AddDetailField("目标物体", CreateObjectField(targetTransformProp, typeof(Transform)));
                AddValidationWarning(targetTransformProp, TweenStepType.Color);

                AddSeparator();

                AddDetailField("使用起始颜色", CreateToggle(useStartColorProp, OnToggleRebuild));

                if (useStartColorProp.boolValue)
                {
                    AddDetailField("起始颜色", CreateColorField(startColorProp));
                }

                AddDetailField("目标颜色", CreateColorField(targetColorProp));
            }
            else if (type == TweenStepType.Fade)
            {
                AddDetailField("目标物体", CreateObjectField(targetTransformProp, typeof(Transform)));
                AddValidationWarning(targetTransformProp, TweenStepType.Fade);

                AddSeparator();

                AddDetailField("使用起始透明度", CreateToggle(useStartFloatProp, OnToggleRebuild));

                if (useStartFloatProp.boolValue)
                {
                    AddDetailField("起始透明度", CreateFloatField(startFloatProp));
                }

                AddDetailField("目标透明度", CreateFloatField(targetFloatProp));
            }
            else if (type == TweenStepType.Jump)
            {
                AddDetailField("目标物体", CreateObjectField(targetTransformProp, typeof(Transform)));

                AddSeparator();

                AddDetailField("使用起始位置", CreateToggle(useStartValueProp, OnToggleRebuild));

                if (useStartValueProp.boolValue)
                {
                    AddDetailField("起始位置", CreateVector3Field(startVectorProp));
                }

                AddDetailField("目标位置", CreateVector3Field(targetVectorProp));

                AddSeparator();

                AddDetailField("跳跃高度", CreateFloatField(jumpHeightProp));
                AddDetailField("跳跃次数", CreateIntegerField(jumpNumProp));
            }
            else if (type == TweenStepType.Punch)
            {
                AddDetailField("目标物体", CreateObjectField(targetTransformProp, typeof(Transform)));

                AddSeparator();

                AddDetailField("冲击目标", CreateEnumField(transformTargetProp, typeof(TransformTarget)));
                AddDetailField("强度", CreateVector3Field(intensityProp));
                AddDetailField("震荡次数", CreateIntegerField(vibratoProp));
                AddDetailField("弹性", CreateFloatField(elasticityProp));
            }
            else if (type == TweenStepType.Shake)
            {
                AddDetailField("目标物体", CreateObjectField(targetTransformProp, typeof(Transform)));

                AddSeparator();

                AddDetailField("震动目标", CreateEnumField(transformTargetProp, typeof(TransformTarget)));
                AddDetailField("强度", CreateVector3Field(intensityProp));
                AddDetailField("震荡次数", CreateIntegerField(vibratoProp));
                AddDetailField("弹性", CreateFloatField(elasticityProp));
                AddDetailField("随机性", CreateFloatField(shakeRandomnessProp));
            }
            else if (type == TweenStepType.FillAmount)
            {
                AddDetailField("目标物体", CreateObjectField(targetTransformProp, typeof(Transform)));
                AddValidationWarning(targetTransformProp, TweenStepType.FillAmount);

                AddSeparator();

                AddDetailField("使用起始填充量", CreateToggle(useStartFloatProp, OnToggleRebuild));

                if (useStartFloatProp.boolValue)
                {
                    AddDetailField("起始填充量", CreateFloatField(startFloatProp));
                }

                AddDetailField("目标填充量", CreateFloatField(targetFloatProp));
            }

            AddSeparator();

            // --- 按类型显示执行 & 缓动 & 回调 ---
            if (type == TweenStepType.Callback)
            {
                // Callback：仅显示回调事件
                if (onCompleteProp != null)
                {
                    var eventField = new PropertyField(onCompleteProp);
                    eventField.BindProperty(onCompleteProp);
                    detailScrollView.Add(eventField);
                }
            }
            else if (type == TweenStepType.Delay)
            {
                // Delay：仅执行模式（无需缓动）
                AddDetailField("执行模式", CreateEnumField(executionModeProp, typeof(ExecutionMode), OnEnumRebuild));
                if ((ExecutionMode)executionModeProp.enumValueIndex == ExecutionMode.Insert)
                {
                    AddDetailField("插入时间", CreateFloatField(insertTimeProp, OnTimingChanged));
                }
            }
            else
            {
                // 动画类型：完整执行模式 + 缓动
                AddDetailField("执行模式", CreateEnumField(executionModeProp, typeof(ExecutionMode), OnEnumRebuild));
                if ((ExecutionMode)executionModeProp.enumValueIndex == ExecutionMode.Insert)
                {
                    AddDetailField("插入时间", CreateFloatField(insertTimeProp, OnTimingChanged));
                }

                AddDetailField("缓动", CreateEnumField(easeProp, typeof(Ease)));

                AddDetailField("自定义曲线", CreateToggle(useCustomCurveProp, OnToggleRebuild));

                if (useCustomCurveProp.boolValue && customCurveProp != null)
                {
                    AddDetailField("曲线", CreateCurveField(customCurveProp));
                }

                // 所有动画类型也支持完成回调
                if (onCompleteProp != null)
                {
                    AddSeparator();
                    var eventField = new PropertyField(onCompleteProp);
                    eventField.BindProperty(onCompleteProp);
                    detailScrollView.Add(eventField);
                }
            }
        }

        #region 详情字段工厂

        private Toggle CreateToggle(SerializedProperty prop, Action onChanged = null)
        {
            var toggle = new Toggle { value = prop.boolValue };
            toggle.RegisterValueChangedCallback(evt =>
            {
                prop.boolValue = evt.newValue;
                prop.serializedObject.ApplyModifiedProperties();
                onChanged?.Invoke();
            });
            return toggle;
        }

        private FloatField CreateFloatField(SerializedProperty prop, Action onChanged = null)
        {
            var field = new FloatField { value = prop.floatValue };
            field.RegisterValueChangedCallback(evt =>
            {
                prop.floatValue = evt.newValue;
                prop.serializedObject.ApplyModifiedProperties();
                onChanged?.Invoke();
            });
            return field;
        }

        private Vector3Field CreateVector3Field(SerializedProperty prop)
        {
            var field = new Vector3Field { value = prop.vector3Value };
            field.RegisterValueChangedCallback(evt =>
            {
                prop.vector3Value = evt.newValue;
                prop.serializedObject.ApplyModifiedProperties();
            });
            return field;
        }

        private ColorField CreateColorField(SerializedProperty prop)
        {
            var field = new ColorField { value = prop.colorValue };
            field.RegisterValueChangedCallback(evt =>
            {
                prop.colorValue = evt.newValue;
                prop.serializedObject.ApplyModifiedProperties();
            });
            return field;
        }

        private EnumField CreateEnumField(SerializedProperty prop, Type enumType, Action onChanged = null)
        {
            var field = new EnumField((Enum)Enum.GetValues(enumType).GetValue(prop.enumValueIndex));
            field.RegisterValueChangedCallback(evt =>
            {
                prop.enumValueIndex = Convert.ToInt32(evt.newValue);
                prop.serializedObject.ApplyModifiedProperties();
                onChanged?.Invoke();
            });
            return field;
        }

        private ObjectField CreateObjectField(SerializedProperty prop, Type objType)
        {
            var field = new ObjectField { objectType = objType, value = prop.objectReferenceValue };
            field.RegisterValueChangedCallback(evt =>
            {
                prop.objectReferenceValue = evt.newValue;
                prop.serializedObject.ApplyModifiedProperties();
                RebuildStepList();
                RefreshDetailPanel();
            });
            return field;
        }

        private CurveField CreateCurveField(SerializedProperty prop)
        {
            var field = new CurveField { value = prop.animationCurveValue };
            field.RegisterValueChangedCallback(evt =>
            {
                prop.animationCurveValue = evt.newValue;
                prop.serializedObject.ApplyModifiedProperties();
            });
            return field;
        }

        private IntegerField CreateIntegerField(SerializedProperty prop)
        {
            var field = new IntegerField { value = prop.intValue };
            field.RegisterValueChangedCallback(evt =>
            {
                prop.intValue = evt.newValue;
                prop.serializedObject.ApplyModifiedProperties();
            });
            return field;
        }

        /// <summary>类型枚举变化：重建列表+详情</summary>
        private void OnTypeChanged()
        {
            RebuildStepList();
            RefreshDetailPanel();
        }

        /// <summary>影响字段可见性的枚举变化：重建列表+详情</summary>
        private void OnEnumRebuild()
        {
            RebuildStepList();
            RefreshDetailPanel();
        }

        /// <summary>时间相关字段变化：重算时间轴并刷新列表</summary>
        private void OnTimingChanged()
        {
            CalculateStepTimings();
            stepListView.Rebuild();
        }

        /// <summary>影响字段可见性的 Toggle 变化：仅重建详情</summary>
        private void OnToggleRebuild()
        {
            RefreshDetailPanel();
        }

        #endregion

        private void AddDetailField(string label, VisualElement field)
        {
            var row = new VisualElement();
            row.AddToClassList("detail-field-row");

            var labelEl = new Label(label);
            labelEl.AddToClassList("detail-field-label");
            row.Add(labelEl);

            field.AddToClassList("detail-field-value");
            row.Add(field);

            detailScrollView.Add(row);
        }

        private void AddSeparator()
        {
            var sep = new VisualElement();
            sep.AddToClassList("detail-separator");
            detailScrollView.Add(sep);
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

            detailScrollView.Add(warning);
        }

        private void OnSyncClicked()
        {
            if (selectedStepIndex < 0 || stepsProperty == null || selectedStepIndex >= stepsProperty.arraySize) return;

            serializedObject?.Update();
            var stepProperty = stepsProperty.GetArrayElementAtIndex(selectedStepIndex);
            var type = (TweenStepType)stepProperty.FindPropertyRelative("Type").enumValueIndex;
            var targetTransformProp = stepProperty.FindPropertyRelative("TargetTransform");
            var target = targetTransformProp.objectReferenceValue as Transform;
            if (target == null && targetPlayer != null) target = targetPlayer.transform;
            if (target == null) return;

            switch (type)
            {
                case TweenStepType.Move:
                    var moveTarget = (TransformTarget)stepProperty.FindPropertyRelative("TransformTarget").enumValueIndex;
                    stepProperty.FindPropertyRelative("TargetVector").vector3Value =
                        moveTarget == TransformTarget.LocalPosition ? target.localPosition : target.position;
                    break;
                case TweenStepType.Rotate:
                    var rotTarget = (TransformTarget)stepProperty.FindPropertyRelative("TransformTarget").enumValueIndex;
                    stepProperty.FindPropertyRelative("TargetVector").vector3Value =
                        rotTarget == TransformTarget.LocalRotation ? target.localRotation.eulerAngles : target.rotation.eulerAngles;
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
            }

            stepsProperty.serializedObject.ApplyModifiedProperties();
            RefreshDetailPanel();
            RebuildStepList();
        }

        #endregion

        #region 工具栏回调

        private void BuildAddStepMenu()
        {
            if (addStepMenu == null) return;

            // Transform
            addStepMenu.menu.AppendAction("Move (Position)", _ => AddStep(TweenStepType.Move, TransformTarget.Position));
            addStepMenu.menu.AppendAction("Move (LocalPosition)", _ => AddStep(TweenStepType.Move, TransformTarget.LocalPosition));
            addStepMenu.menu.AppendAction("Rotate (Rotation)", _ => AddStep(TweenStepType.Rotate, TransformTarget.Rotation));
            addStepMenu.menu.AppendAction("Rotate (LocalRotation)", _ => AddStep(TweenStepType.Rotate, TransformTarget.LocalRotation));
            addStepMenu.menu.AppendAction("Scale", _ => AddStep(TweenStepType.Scale, TransformTarget.Scale));
            addStepMenu.menu.AppendSeparator();

            // 视觉
            addStepMenu.menu.AppendAction("Color", _ => AddStep(TweenStepType.Color));
            addStepMenu.menu.AppendAction("Fade", _ => AddStep(TweenStepType.Fade));
            addStepMenu.menu.AppendSeparator();

            // UI
            addStepMenu.menu.AppendAction("Anchor Move", _ => AddStep(TweenStepType.AnchorMove));
            addStepMenu.menu.AppendAction("Size Delta", _ => AddStep(TweenStepType.SizeDelta));
            addStepMenu.menu.AppendSeparator();

            // 特效
            addStepMenu.menu.AppendAction("Jump", _ => AddStep(TweenStepType.Jump));
            addStepMenu.menu.AppendAction("Punch (Position)", _ => AddStep(TweenStepType.Punch, TransformTarget.PunchPosition));
            addStepMenu.menu.AppendAction("Punch (Rotation)", _ => AddStep(TweenStepType.Punch, TransformTarget.PunchRotation));
            addStepMenu.menu.AppendAction("Punch (Scale)", _ => AddStep(TweenStepType.Punch, TransformTarget.PunchScale));
            addStepMenu.menu.AppendAction("Shake (Position)", _ => AddStep(TweenStepType.Shake, TransformTarget.ShakePosition));
            addStepMenu.menu.AppendAction("Shake (Rotation)", _ => AddStep(TweenStepType.Shake, TransformTarget.ShakeRotation));
            addStepMenu.menu.AppendAction("Shake (Scale)", _ => AddStep(TweenStepType.Shake, TransformTarget.ShakeScale));
            addStepMenu.menu.AppendAction("Fill Amount", _ => AddStep(TweenStepType.FillAmount));
            addStepMenu.menu.AppendSeparator();

            // 流程控制
            addStepMenu.menu.AppendAction("Delay", _ => AddStep(TweenStepType.Delay));
            addStepMenu.menu.AppendAction("Callback", _ => AddStep(TweenStepType.Callback));
        }

        private void AddStep(TweenStepType type, TransformTarget transformTarget = TransformTarget.Position)
        {
            if (stepsProperty == null)
            {
                Debug.LogWarning("请先选择一个 DOTweenVisualPlayer 组件");
                return;
            }

            stepsProperty.InsertArrayElementAtIndex(stepsProperty.arraySize);
            var newStep = stepsProperty.GetArrayElementAtIndex(stepsProperty.arraySize - 1);

            newStep.FindPropertyRelative("Type").enumValueIndex = (int)type;
            newStep.FindPropertyRelative("IsEnabled").boolValue = true;
            newStep.FindPropertyRelative("Duration").floatValue = 1f;
            newStep.FindPropertyRelative("Delay").floatValue = 0f;
            newStep.FindPropertyRelative("Ease").enumValueIndex = (int)Ease.OutQuad;
            newStep.FindPropertyRelative("TransformTarget").enumValueIndex = (int)transformTarget;

            switch (type)
            {
                case TweenStepType.Color:
                    newStep.FindPropertyRelative("TargetColor").colorValue = Color.white;
                    break;
                case TweenStepType.Fade:
                    newStep.FindPropertyRelative("TargetFloat").floatValue = 0f;
                    newStep.FindPropertyRelative("StartFloat").floatValue = 1f;
                    break;
                case TweenStepType.FillAmount:
                    newStep.FindPropertyRelative("TargetFloat").floatValue = 1f;
                    newStep.FindPropertyRelative("StartFloat").floatValue = 0f;
                    break;
                case TweenStepType.Jump:
                    newStep.FindPropertyRelative("JumpHeight").floatValue = 1f;
                    newStep.FindPropertyRelative("JumpNum").intValue = 1;
                    break;
                case TweenStepType.Punch:
                    newStep.FindPropertyRelative("Intensity").vector3Value = new Vector3(1f, 1f, 1f);
                    newStep.FindPropertyRelative("Vibrato").intValue = 10;
                    newStep.FindPropertyRelative("Elasticity").floatValue = 1f;
                    break;
                case TweenStepType.Shake:
                    newStep.FindPropertyRelative("Intensity").vector3Value = new Vector3(1f, 1f, 1f);
                    newStep.FindPropertyRelative("Vibrato").intValue = 10;
                    newStep.FindPropertyRelative("Elasticity").floatValue = 0.5f;
                    newStep.FindPropertyRelative("ShakeRandomness").floatValue = 90f;
                    break;
            }

            stepsProperty.serializedObject.ApplyModifiedProperties();
            selectedStepIndex = stepsProperty.arraySize - 1;
            RebuildStepList();
            RefreshDetailPanel();
        }

        private void OnPreviewClicked()
        {
            if (targetPlayer == null)
            {
                Debug.LogWarning("请先选择一个 DOTweenVisualPlayer 组件");
                return;
            }

            if (_previewManager.IsPlaying)
                _previewManager.PausePreview();
            else if (_previewManager.IsPaused)
                _previewManager.ResumePreview();
            else
                _previewManager.StartPreview();
        }

        private void OnReplayClicked()
        {
            if (targetPlayer == null) return;
            _previewManager.Replay();
        }

        private void OnStopClicked() => _previewManager.StopPreview();

        private void OnResetClicked()
        {
            if (targetPlayer == null) return;
            _previewManager.Reset();
        }

        #endregion

        #region 状态更新

        /// <summary>
        /// 预览状态变更回调，更新 UI 按钮和状态栏
        /// </summary>
        private void OnPreviewStateChanged()
        {
            UpdateButtonStates();
        }

        private void UpdateButtonStates()
        {
            var state = _previewManager?.State ?? DOTweenPreviewManager.PreviewState.None;
            bool hasTarget = targetPlayer != null;
            bool hasSteps = hasTarget && targetPlayer.StepCount > 0;
            bool inPreview = state == DOTweenPreviewManager.PreviewState.Playing
                          || state == DOTweenPreviewManager.PreviewState.Paused;
            bool isCompleted = state == DOTweenPreviewManager.PreviewState.Completed;

            if (previewButton != null)
            {
                previewButton.SetEnabled(hasSteps && !isCompleted);
                previewButton.text = state == DOTweenPreviewManager.PreviewState.Playing
                    ? "暂停"
                    : (state == DOTweenPreviewManager.PreviewState.Paused ? "继续" : "预览");
            }

            if (stopButton != null) stopButton.SetEnabled(inPreview);
            if (replayButton != null) replayButton.SetEnabled(hasSteps && isCompleted);
            if (resetButton != null) resetButton.SetEnabled(isCompleted);
            if (addStepMenu != null) addStepMenu.SetEnabled(hasTarget && !inPreview);

            UpdateStatusBar();
        }

        private void UpdateStatusBar()
        {
            if (stateLabel == null) return;

            var state = _previewManager?.State ?? DOTweenPreviewManager.PreviewState.None;

            switch (state)
            {
                case DOTweenPreviewManager.PreviewState.None:
                    stateLabel.text = "● 未播放";
                    stateLabel.style.color = new Color(0.6f, 0.6f, 0.6f);
                    break;
                case DOTweenPreviewManager.PreviewState.Playing:
                    stateLabel.text = "● 播放中";
                    stateLabel.style.color = new Color(0.3f, 0.8f, 0.3f);
                    break;
                case DOTweenPreviewManager.PreviewState.Paused:
                    stateLabel.text = "● 已暂停";
                    stateLabel.style.color = new Color(1f, 0.7f, 0f);
                    break;
                case DOTweenPreviewManager.PreviewState.Completed:
                    stateLabel.text = "● 播放完成";
                    stateLabel.style.color = new Color(0.3f, 0.6f, 1f);
                    break;
            }
        }

        #endregion
    }
}
#endif
