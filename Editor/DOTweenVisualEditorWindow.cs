#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using DG.Tweening;
using DG.DOTweenEditor;
using UnityEditor.Compilation;
using CNoom.DOTweenVisual.Components;
using CNoom.DOTweenVisual.Data;

namespace CNoom.DOTweenVisual.Editor
{
    /// <summary>
    /// DOTween 可视化编辑器主窗口
    /// 布局：顶部工具栏+状态栏 | 左侧步骤概览(ScrollView) | 右侧步骤详情
    /// 不使用 ListView，纯 ScrollView 手动管理，避免虚拟化/绑定/排序 bug
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
                if (window.previewState != PreviewState.None)
                {
                    window.OnResetClicked();
                }
            }
        }

        #endregion

        #region 常量

        private const string USS_PATH = "Assets/Plugins/DOTweenVisualEditor/Editor/USS/DOTweenVisualEditor.uss";
        private const bool DEBUG_MODE = false;
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

        // 左侧概览 - 纯 ScrollView，不用 ListView
        private ScrollView stepScrollView;
        private int selectedStepIndex = -1;

        // 排序按钮
        private Button moveUpButton;
        private Button moveDownButton;

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

        #region 预览状态

        private enum PreviewState { None, Playing, Paused, Completed }

        private PreviewState previewState = PreviewState.None;
        private Sequence previewSequence;
        private Dictionary<Transform, TransformState> initialStates = new();

        private bool isPreviewing => previewState == PreviewState.Playing;
        private bool isPaused => previewState == PreviewState.Paused;

        private struct TransformState
        {
            public Vector3 position;
            public Quaternion rotation;
            public Vector3 localPosition;
            public Quaternion localRotation;
            public Vector3 localScale;
            public Color color;
            public float alpha;
        }

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
            EditorApplication.update += OnEditorUpdate;
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        }

        private void OnDisable()
        {
            EditorApplication.update -= OnEditorUpdate;
            EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
            StopPreview();
            rootVisualElement.Clear();
        }

        private void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.ExitingEditMode)
            {
                if (previewState != PreviewState.None)
                {
                    OnResetClicked();
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

            if (previewSequence == null)
            {
                timeLabel.text = "--:-- / --:--";
                return;
            }

            float currentTime = previewSequence.Elapsed(false);
            float totalTime = previewSequence.Duration(false);
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
            if (isPreviewing || isPaused)
            {
                StopPreview();
            }

            var player = evt.newValue as DOTweenVisualPlayer;
            SetTarget(player);
            UpdateButtonStates();
        }

        private void SetTarget(DOTweenVisualPlayer player)
        {
            targetPlayer = player;

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

            var styleSheet = AssetDatabase.LoadAssetAtPath<StyleSheet>(USS_PATH);
            if (styleSheet != null)
            {
                rootVisualElement.styleSheets.Add(styleSheet);
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

            // 纯 ScrollView 替代 ListView
            stepScrollView = new ScrollView(ScrollViewMode.Vertical);
            stepScrollView.AddToClassList("step-scroll");
            stepScrollView.style.flexGrow = 1;
            leftPanel.Add(stepScrollView);

            // 排序按钮栏
            var reorderBar = new VisualElement();
            reorderBar.AddToClassList("reorder-bar");

            moveUpButton = new Button(MoveStepUp) { text = "▲ 上移" };
            moveUpButton.AddToClassList("reorder-button");
            reorderBar.Add(moveUpButton);

            moveDownButton = new Button(MoveStepDown) { text = "▼ 下移" };
            moveDownButton.AddToClassList("reorder-button");
            reorderBar.Add(moveDownButton);

            leftPanel.Add(reorderBar);

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

        #region 步骤列表 - 纯 ScrollView 手动管理

        /// <summary>
        /// 完全重建步骤列表（从 SerializedProperty 读取）
        /// </summary>
        private void RebuildStepList()
        {
            if (stepScrollView == null) return;

            // 清空所有旧元素
            stepScrollView.Clear();

            if (stepsProperty == null || serializedObject == null)
            {
                selectedStepIndex = -1;
                UpdateReorderButtons();
                return;
            }

            serializedObject.Update();
            int count = stepsProperty.arraySize;

            for (int i = 0; i < count; i++)
            {
                var item = CreateStepItem(i);
                stepScrollView.Add(item);
            }

            // 确保 selectedStepIndex 合法
            if (selectedStepIndex >= count) selectedStepIndex = -1;
            if (selectedStepIndex >= 0) HighlightStepItem(selectedStepIndex);

            UpdateReorderButtons();
            UpdateButtonStates();
        }

        /// <summary>
        /// 创建单个步骤行
        /// </summary>
        private VisualElement CreateStepItem(int index)
        {
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

            var item = new VisualElement();
            item.AddToClassList("step-item");
            item.AddToClassList(GetStepTypeCssClass(type));
            if (!isEnabledProp.boolValue) item.AddToClassList("step-disabled");
            item.userData = index;

            // 点击选中
            item.RegisterCallback<ClickEvent>(_ =>
            {
                SelectStep((int)item.userData);
            });

            var row = new VisualElement();
            row.AddToClassList("step-row");

            // 启用开关
            var enableToggle = new Toggle { value = isEnabledProp.boolValue };
            enableToggle.AddToClassList("step-enable-toggle");
            enableToggle.RegisterValueChangedCallback(evt =>
            {
                int idx = (int)item.userData;
                if (stepsProperty == null || idx < 0 || idx >= stepsProperty.arraySize) return;
                var prop = stepsProperty.GetArrayElementAtIndex(idx);
                prop.FindPropertyRelative("IsEnabled").boolValue = evt.newValue;
                prop.serializedObject.ApplyModifiedProperties();
                item.EnableInClassList("step-disabled", !evt.newValue);
            });
            // 阻止 Toggle 的点击冒泡到 item
            enableToggle.RegisterCallback<ClickEvent>(e => e.StopPropagation());
            row.Add(enableToggle);

            // 标题
            var titleLabel = new Label { text = $"{index + 1}. [{targetName}] {GetStepDisplayName(type)}" };
            titleLabel.AddToClassList("step-title");
            row.Add(titleLabel);

            // 摘要
            var summaryLabel = new Label { text = $"{durationProp.floatValue:F1}s | {ease}" };
            summaryLabel.AddToClassList("step-summary");
            row.Add(summaryLabel);

            var spacer = new VisualElement { style = { flexGrow = 1 } };
            row.Add(spacer);

            // 删除按钮
            var deleteButton = new Button { text = "✕" };
            deleteButton.AddToClassList("step-delete-button");
            deleteButton.clickable = new Clickable(() =>
            {
                int idx = (int)item.userData;
                if (stepsProperty == null || idx < 0 || idx >= stepsProperty.arraySize) return;
                stepsProperty.DeleteArrayElementAtIndex(idx);
                stepsProperty.serializedObject.ApplyModifiedProperties();

                // 调整选中索引
                if (selectedStepIndex == idx)
                    selectedStepIndex = -1;
                else if (selectedStepIndex > idx)
                    selectedStepIndex--;

                RebuildStepList();
                RefreshDetailPanel();
            });
            deleteButton.RegisterCallback<ClickEvent>(e => e.StopPropagation());
            row.Add(deleteButton);

            item.Add(row);
            return item;
        }

        /// <summary>
        /// 选中某个步骤
        /// </summary>
        private void SelectStep(int index)
        {
            if (stepsProperty == null || index < 0 || index >= stepsProperty.arraySize)
            {
                selectedStepIndex = -1;
            }
            else
            {
                selectedStepIndex = index;
            }

            HighlightStepItem(selectedStepIndex);
            RefreshDetailPanel();
            UpdateReorderButtons();
        }

        /// <summary>
        /// 高亮选中的步骤行
        /// </summary>
        private void HighlightStepItem(int selectedIndex)
        {
            if (stepScrollView == null) return;

            foreach (var child in stepScrollView.Children())
            {
                int idx = (int)child.userData;
                child.EnableInClassList("step-selected", idx == selectedIndex);
            }
        }

        private void MoveStepUp()
        {
            if (stepsProperty == null || selectedStepIndex <= 0) return;

            stepsProperty.MoveArrayElement(selectedStepIndex, selectedStepIndex - 1);
            stepsProperty.serializedObject.ApplyModifiedProperties();
            selectedStepIndex--;
            RebuildStepList();
            RefreshDetailPanel();
        }

        private void MoveStepDown()
        {
            if (stepsProperty == null || selectedStepIndex < 0 || selectedStepIndex >= stepsProperty.arraySize - 1) return;

            stepsProperty.MoveArrayElement(selectedStepIndex, selectedStepIndex + 1);
            stepsProperty.serializedObject.ApplyModifiedProperties();
            selectedStepIndex++;
            RebuildStepList();
            RefreshDetailPanel();
        }

        private void UpdateReorderButtons()
        {
            if (moveUpButton != null)
                moveUpButton.SetEnabled(selectedStepIndex > 0);
            if (moveDownButton != null)
                moveDownButton.SetEnabled(stepsProperty != null && selectedStepIndex >= 0 && selectedStepIndex < stepsProperty.arraySize - 1);
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
            var useCustomCurveProp = stepProperty.FindPropertyRelative("UseCustomCurve");
            var customCurveProp = stepProperty.FindPropertyRelative("CustomCurve");
            var onCompleteProp = stepProperty.FindPropertyRelative("OnComplete");

            var type = (TweenStepType)typeProp.enumValueIndex;

            // --- 通用字段 ---
            AddDetailField("类型", CreateEnumField(typeProp, typeof(TweenStepType), OnTypeChanged));
            AddDetailField("启用", CreateToggle(isEnabledProp));
            AddDetailField("时长", CreateFloatField(durationProp));
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

                var startVecField = CreateVector3Field(startVectorProp);
                startVecField.SetEnabled(useStartValueProp.boolValue);
                AddDetailField("起始值", startVecField);

                string targetLabel = type == TweenStepType.Rotate ? "目标值 (欧拉角)" : "目标值";
                AddDetailField(targetLabel, CreateVector3Field(targetVectorProp));
            }
            else if (type == TweenStepType.Color)
            {
                AddDetailField("目标物体", CreateObjectField(targetTransformProp, typeof(Transform)));

                AddSeparator();

                AddDetailField("使用起始颜色", CreateToggle(useStartColorProp, OnToggleRebuild));

                var startColorField = CreateColorField(startColorProp);
                startColorField.SetEnabled(useStartColorProp.boolValue);
                AddDetailField("起始颜色", startColorField);

                AddDetailField("目标颜色", CreateColorField(targetColorProp));
            }
            else if (type == TweenStepType.Fade)
            {
                AddDetailField("目标物体", CreateObjectField(targetTransformProp, typeof(Transform)));

                AddSeparator();

                AddDetailField("使用起始透明度", CreateToggle(useStartFloatProp, OnToggleRebuild));

                var startFloatField = CreateFloatField(startFloatProp);
                startFloatField.SetEnabled(useStartFloatProp.boolValue);
                AddDetailField("起始透明度", startFloatField);

                AddDetailField("目标透明度", CreateFloatField(targetFloatProp));
            }

            AddSeparator();

            // --- 执行 & 缓动 ---
            AddDetailField("执行模式", CreateEnumField(executionModeProp, typeof(ExecutionMode), OnEnumRebuild));
            if ((ExecutionMode)executionModeProp.enumValueIndex == ExecutionMode.Insert)
            {
                AddDetailField("插入时间", CreateFloatField(insertTimeProp));
            }

            AddDetailField("缓动", CreateEnumField(easeProp, typeof(Ease)));

            AddDetailField("自定义曲线", CreateToggle(useCustomCurveProp, OnToggleRebuild));

            if (useCustomCurveProp.boolValue && customCurveProp != null)
            {
                AddDetailField("曲线", CreateCurveField(customCurveProp));
            }

            // --- 回调 ---
            if (type == TweenStepType.Callback && onCompleteProp != null)
            {
                AddSeparator();
                var eventField = new PropertyField(onCompleteProp);
                eventField.BindProperty(onCompleteProp);
                detailScrollView.Add(eventField);
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

        private FloatField CreateFloatField(SerializedProperty prop)
        {
            var field = new FloatField { value = prop.floatValue };
            field.RegisterValueChangedCallback(evt =>
            {
                prop.floatValue = evt.newValue;
                prop.serializedObject.ApplyModifiedProperties();
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

        /// <summary>类型枚举变化：重建列表+详情</summary>
        private void OnTypeChanged()
        {
            RebuildStepList();
            RefreshDetailPanel();
        }

        /// <summary>影响字段可见性的枚举变化：仅重建详情</summary>
        private void OnEnumRebuild()
        {
            RefreshDetailPanel();
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
                    var renderer = target.GetComponent<Renderer>();
                    if (renderer != null && renderer.material != null)
                        stepProperty.FindPropertyRelative("TargetColor").colorValue = renderer.material.color;
                    break;
                case TweenStepType.Fade:
                    var canvasGroup = target.GetComponent<CanvasGroup>();
                    if (canvasGroup != null)
                        stepProperty.FindPropertyRelative("TargetFloat").floatValue = canvasGroup.alpha;
                    else
                    {
                        var rend = target.GetComponent<Renderer>();
                        if (rend != null && rend.material != null)
                            stepProperty.FindPropertyRelative("TargetFloat").floatValue = rend.material.color.a;
                    }
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

            addStepMenu.menu.AppendAction("Move (Position)", _ => AddStep(TweenStepType.Move, TransformTarget.Position));
            addStepMenu.menu.AppendAction("Move (LocalPosition)", _ => AddStep(TweenStepType.Move, TransformTarget.LocalPosition));
            addStepMenu.menu.AppendAction("Rotate (Rotation)", _ => AddStep(TweenStepType.Rotate, TransformTarget.Rotation));
            addStepMenu.menu.AppendAction("Rotate (LocalRotation)", _ => AddStep(TweenStepType.Rotate, TransformTarget.LocalRotation));
            addStepMenu.menu.AppendAction("Scale", _ => AddStep(TweenStepType.Scale, TransformTarget.Scale));
            addStepMenu.menu.AppendSeparator();
            addStepMenu.menu.AppendAction("Color", _ => AddStep(TweenStepType.Color));
            addStepMenu.menu.AppendAction("Fade", _ => AddStep(TweenStepType.Fade));
            addStepMenu.menu.AppendSeparator();
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

            if (previewState == PreviewState.Playing)
                PausePreview();
            else if (previewState == PreviewState.Paused)
                ResumePreview();
            else if (previewState == PreviewState.None)
                StartPreview();
        }

        private void OnReplayClicked()
        {
            if (targetPlayer == null) return;
            RestoreInitialStates();
            StartPreview();
        }

        private void OnStopClicked() => StopPreview();

        private void OnResetClicked()
        {
            if (targetPlayer == null) return;

            if (previewSequence != null)
            {
                previewSequence.Kill();
                previewSequence = null;
            }
            DOTweenEditorPreview.Stop();
            RestoreInitialStates();
            previewState = PreviewState.None;
            initialStates.Clear();
            UpdateButtonStates();
        }

        #endregion

        #region 预览逻辑

        private void StartPreview()
        {
            if (targetPlayer == null || targetPlayer.StepCount == 0) return;

            if (previewSequence != null) { previewSequence.Kill(); previewSequence = null; }
            DOTweenEditorPreview.Stop();

            if (initialStates.Count > 0) RestoreInitialStates();
            else SaveInitialStates();

            DOTweenEditorPreview.Start();

            try
            {
                previewSequence = DOTween.Sequence();
                BuildPreviewSequence();
                DOTweenEditorPreview.PrepareTweenForPreview(previewSequence);

                previewSequence.OnComplete(() =>
                {
                    if (this == null) return;
                    previewState = PreviewState.Completed;
                    UpdateButtonStates();
                });

                previewSequence.Play();
                previewState = PreviewState.Playing;
                UpdateButtonStates();
            }
            catch (Exception e)
            {
                Debug.LogError($"[DOTweenVisualEditor] 预览启动失败: {e.Message}");
                DOTweenEditorPreview.Stop();
                if (previewSequence != null) { previewSequence.Kill(); previewSequence = null; }
                RestoreInitialStates();
                previewState = PreviewState.None;
                UpdateButtonStates();
            }
        }

        private void PausePreview()
        {
            if (previewSequence != null && previewSequence.IsPlaying())
            {
                previewSequence.Pause();
                previewState = PreviewState.Paused;
                UpdateButtonStates();
            }
        }

        private void ResumePreview()
        {
            if (previewSequence != null && !previewSequence.IsPlaying())
            {
                previewSequence.Play();
                previewState = PreviewState.Playing;
                UpdateButtonStates();
            }
        }

        private void StopPreview()
        {
            if (previewSequence != null) { previewSequence.Kill(); previewSequence = null; }
            DOTweenEditorPreview.Stop();
            previewState = PreviewState.Completed;
            UpdateButtonStates();
        }

        private void UpdateButtonStates()
        {
            bool hasTarget = targetPlayer != null;
            bool hasSteps = hasTarget && targetPlayer.StepCount > 0;
            bool inPreview = isPreviewing || isPaused;
            bool isCompleted = previewState == PreviewState.Completed;

            if (previewButton != null)
            {
                previewButton.SetEnabled(hasSteps && !isCompleted);
                previewButton.text = isPreviewing ? "暂停" : (isPaused ? "继续" : "预览");
            }

            if (stopButton != null) stopButton.SetEnabled(inPreview);
            if (replayButton != null) replayButton.SetEnabled(hasSteps && isCompleted);
            if (resetButton != null) resetButton.SetEnabled(isCompleted);
            if (addStepMenu != null) addStepMenu.SetEnabled(hasTarget && !inPreview);

            UpdateStatusBar();
            UpdateReorderButtons();
        }

        private void UpdateStatusBar()
        {
            if (stateLabel == null) return;

            switch (previewState)
            {
                case PreviewState.None:
                    stateLabel.text = "● 未播放";
                    stateLabel.style.color = new Color(0.6f, 0.6f, 0.6f);
                    break;
                case PreviewState.Playing:
                    stateLabel.text = "● 播放中";
                    stateLabel.style.color = new Color(0.3f, 0.8f, 0.3f);
                    break;
                case PreviewState.Paused:
                    stateLabel.text = "● 已暂停";
                    stateLabel.style.color = new Color(1f, 0.7f, 0f);
                    break;
                case PreviewState.Completed:
                    stateLabel.text = "● 播放完成";
                    stateLabel.style.color = new Color(0.3f, 0.6f, 1f);
                    break;
            }
        }

        private void SaveInitialStates()
        {
            initialStates.Clear();
            SaveTransformState(targetPlayer.transform);
            foreach (var step in targetPlayer.Steps)
            {
                if (step.TargetTransform != null)
                    SaveTransformState(step.TargetTransform);
            }
        }

        private void SaveTransformState(Transform t)
        {
            if (t == null || initialStates.ContainsKey(t)) return;

            Color color = Color.white;
            float alpha = 1f;

            var renderer = t.GetComponent<Renderer>();
            if (renderer != null && renderer.material != null)
            {
                color = renderer.material.color;
                alpha = renderer.material.color.a;
            }

            var canvasGroup = t.GetComponent<CanvasGroup>();
            if (canvasGroup != null) alpha = canvasGroup.alpha;

            initialStates[t] = new TransformState
            {
                position = t.position,
                rotation = t.rotation,
                localPosition = t.localPosition,
                localRotation = t.localRotation,
                localScale = t.localScale,
                color = color,
                alpha = alpha
            };
        }

        private void RestoreInitialStates()
        {
            var keysToRemove = new List<Transform>();

            foreach (var kvp in initialStates)
            {
                var t = kvp.Key;
                var state = kvp.Value;

                try
                {
                    if (t != null && t.gameObject != null)
                    {
                        Undo.RecordObject(t, "Reset Preview State");
                        t.position = state.position;
                        t.rotation = state.rotation;
                        t.localPosition = state.localPosition;
                        t.localRotation = state.localRotation;
                        t.localScale = state.localScale;

                        var renderer = t.GetComponent<Renderer>();
                        if (renderer != null && renderer.material != null)
                            renderer.material.color = state.color;

                        var canvasGroup = t.GetComponent<CanvasGroup>();
                        if (canvasGroup != null)
                            canvasGroup.alpha = state.alpha;
                    }
                    else
                    {
                        keysToRemove.Add(t);
                    }
                }
                catch (MissingReferenceException)
                {
                    keysToRemove.Add(t);
                }
            }

            foreach (var key in keysToRemove)
                initialStates.Remove(key);

            initialStates.Clear();
        }

        private void BuildPreviewSequence()
        {
            foreach (var step in targetPlayer.Steps)
            {
                if (!step.IsEnabled) continue;
                AppendStepToPreview(step);
            }
        }

        private void AppendStepToPreview(TweenStepData step)
        {
            var target = step.TargetTransform != null ? step.TargetTransform : targetPlayer.transform;
            if (target == null) return;

            Tweener tweener = null;

            switch (step.Type)
            {
                case TweenStepType.Move:
                    tweener = CreatePreviewMoveTween(step, target);
                    break;
                case TweenStepType.Rotate:
                    tweener = CreatePreviewRotateTween(step, target);
                    break;
                case TweenStepType.Scale:
                    tweener = CreatePreviewScaleTween(step, target);
                    break;
                case TweenStepType.Color:
                    tweener = CreatePreviewColorTween(step, target);
                    break;
                case TweenStepType.Fade:
                    tweener = CreatePreviewFadeTween(step, target);
                    break;
                case TweenStepType.Delay:
                    previewSequence.AppendInterval(Mathf.Max(0.001f, step.Duration));
                    return;
                case TweenStepType.Callback:
                    var onComplete = step.OnComplete;
                    previewSequence.AppendCallback(() => onComplete?.Invoke());
                    return;
            }

            if (tweener == null) return;

            if (step.UseCustomCurve && step.CustomCurve != null)
                tweener.SetEase(step.CustomCurve);
            else
                tweener.SetEase(step.Ease);

            switch (step.ExecutionMode)
            {
                case ExecutionMode.Append:
                    previewSequence.Append(tweener);
                    break;
                case ExecutionMode.Join:
                    previewSequence.Join(tweener);
                    break;
                case ExecutionMode.Insert:
                    previewSequence.Insert(Mathf.Max(0f, step.InsertTime), tweener);
                    break;
            }
        }

        #region Preview Tween 创建

        private Tweener CreatePreviewMoveTween(TweenStepData step, Transform target)
        {
            if (step.UseStartValue)
            {
                if (step.TransformTarget == TransformTarget.LocalPosition)
                    target.localPosition = step.StartVector;
                else
                    target.position = step.StartVector;
            }

            float duration = Mathf.Max(0.001f, step.Duration);

            if (step.TransformTarget == TransformTarget.LocalPosition)
                return step.IsRelative
                    ? target.DOLocalMove(step.TargetVector, duration).From(isRelative: true)
                    : target.DOLocalMove(step.TargetVector, duration);
            else
                return step.IsRelative
                    ? target.DOMove(step.TargetVector, duration).From(isRelative: true)
                    : target.DOMove(step.TargetVector, duration);
        }

        private Tweener CreatePreviewRotateTween(TweenStepData step, Transform target)
        {
            Quaternion startQuat;
            Quaternion targetQuat;

            if (step.UseStartValue)
            {
                startQuat = Quaternion.Euler(step.StartVector);
                if (step.TransformTarget == TransformTarget.LocalRotation)
                    target.localRotation = startQuat;
                else
                    target.rotation = startQuat;
            }
            else
            {
                startQuat = step.TransformTarget == TransformTarget.LocalRotation
                    ? target.localRotation
                    : target.rotation;
            }

            targetQuat = Quaternion.Euler(step.TargetVector);
            float duration = Mathf.Max(0.001f, step.Duration);

            if (step.TransformTarget == TransformTarget.LocalRotation)
                return step.IsRelative
                    ? target.DOLocalRotateQuaternion(startQuat * targetQuat, duration)
                    : target.DOLocalRotateQuaternion(targetQuat, duration);
            else
                return step.IsRelative
                    ? target.DORotateQuaternion(startQuat * targetQuat, duration)
                    : target.DORotateQuaternion(targetQuat, duration);
        }

        private Tweener CreatePreviewScaleTween(TweenStepData step, Transform target)
        {
            if (step.UseStartValue) target.localScale = step.StartVector;
            float duration = Mathf.Max(0.001f, step.Duration);
            return step.IsRelative
                ? target.DOScale(step.TargetVector, duration).From(isRelative: true)
                : target.DOScale(step.TargetVector, duration);
        }

        private Tweener CreatePreviewColorTween(TweenStepData step, Transform target)
        {
            var renderer = target.GetComponent<Renderer>();
            if (renderer == null || renderer.material == null) return null;
            if (step.UseStartColor) renderer.material.color = step.StartColor;
            float duration = Mathf.Max(0.001f, step.Duration);
            return renderer.material.DOColor(step.TargetColor, duration);
        }

        private Tweener CreatePreviewFadeTween(TweenStepData step, Transform target)
        {
            float duration = Mathf.Max(0.001f, step.Duration);

            var canvasGroup = target.GetComponent<CanvasGroup>();
            if (canvasGroup != null)
            {
                if (step.UseStartFloat) canvasGroup.alpha = step.StartFloat;
                return canvasGroup.DOFade(step.TargetFloat, duration);
            }

            var renderer = target.GetComponent<Renderer>();
            if (renderer != null && renderer.material != null)
            {
                if (step.UseStartFloat)
                {
                    Color c = renderer.material.color;
                    c.a = step.StartFloat;
                    renderer.material.color = c;
                }
                return renderer.material.DOFade(step.TargetFloat, duration);
            }

            return null;
        }

        #endregion

        #endregion

        #region 工具方法

        private string GetStepDisplayName(TweenStepType type) => type switch
        {
            TweenStepType.Move => "Move",
            TweenStepType.Rotate => "Rotate",
            TweenStepType.Scale => "Scale",
            TweenStepType.Color => "Color",
            TweenStepType.Fade => "Fade",
            TweenStepType.Delay => "Delay",
            TweenStepType.Callback => "Callback",
            _ => type.ToString()
        };

        private string GetStepTypeCssClass(TweenStepType type) => type switch
        {
            TweenStepType.Move => "step-move",
            TweenStepType.Rotate => "step-rotate",
            TweenStepType.Scale => "step-scale",
            TweenStepType.Color => "step-color",
            TweenStepType.Fade => "step-fade",
            TweenStepType.Delay => "step-delay",
            TweenStepType.Callback => "step-callback",
            _ => ""
        };

        private void Log(string message)
        {
            if (DEBUG_MODE) Debug.Log($"[DOTweenVisualEditor] {message}");
        }

        #endregion
    }
}
#endif
