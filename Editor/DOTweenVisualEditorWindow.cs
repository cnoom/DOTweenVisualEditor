#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.UIElements;
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
    /// 布局：顶部工具栏+状态栏 | 左侧步骤概览 | 右侧步骤详情
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
        private const float FixedItemHeight = 36f;
        private const float LeftPanelMinWidth = 220f;
        private const float RightPanelMinWidth = 260f;

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
        private Button addStepButton;

        // 右侧详情
        private VisualElement detailPanel;
        private Label detailHelpLabel;
        private PropertyField detailPropertyField;

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
            Log("OnEnable");
            EditorApplication.update += OnEditorUpdate;
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        }

        private void OnDisable()
        {
            Log("OnDisable");
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
            Log($"OnTargetChanged: {evt.newValue}");

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
            Log($"SetTarget: {player}");

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

            RefreshStepList();
            RefreshDetailPanel();
        }

        #endregion

        #region UI 构建

        private void CreateGUI()
        {
            Log("CreateGUI");

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

            stepListView = new ListView
            {
                reorderable = true,
                fixedItemHeight = FixedItemHeight,
                makeItem = MakeStepItem,
                bindItem = BindStepItem,
                selectionType = SelectionType.Single,
                showBorder = false,
                showFoldoutHeader = false,
                showAddRemoveFooter = false,
                virtualizationMethod = CollectionVirtualizationMethod.FixedHeight
            };
            stepListView.AddToClassList("step-list");
            stepListView.style.flexGrow = 1;
            stepListView.onSelectionChange += OnStepSelectionChanged;
            stepListView.itemIndexChanged += OnStepReordered;
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
            rightPanel.Add(rightHeader);

            detailPanel = new VisualElement();
            detailPanel.AddToClassList("detail-content");
            detailPanel.style.flexGrow = 1;

            detailHelpLabel = new Label("请在左侧选择一个步骤");
            detailHelpLabel.AddToClassList("detail-help-label");
            detailPanel.Add(detailHelpLabel);

            detailPropertyField = new PropertyField { name = "detail-property" };
            detailPropertyField.AddToClassList("detail-property");
            detailPropertyField.style.display = DisplayStyle.None;
            detailPanel.Add(detailPropertyField);

            rightPanel.Add(detailPanel);
            splitView.Add(rightPanel);

            rootVisualElement.Add(splitView);

            // 构建添加步骤菜单
            BuildAddStepMenu();
            UpdateButtonStates();
        }

        #endregion

        #region 步骤列表

        private void RefreshStepList()
        {
            Log($"RefreshStepList - stepListView null: {stepListView == null}, stepsProperty null: {stepsProperty == null}");

            if (stepListView == null) return;

            stepListView.Unbind();
            stepListView.itemsSource = System.Array.Empty<object>();
            stepListView.selectedIndex = -1;

            if (stepsProperty != null && serializedObject != null)
            {
                serializedObject.Update();
                stepListView.BindProperty(stepsProperty);
            }

            UpdateButtonStates();
        }

        private VisualElement MakeStepItem()
        {
            var item = new VisualElement();
            item.AddToClassList("step-item");

            var row = new VisualElement();
            row.AddToClassList("step-row");

            var enableToggle = new Toggle { name = "enable-toggle" };
            enableToggle.AddToClassList("step-enable-toggle");
            enableToggle.RegisterValueChangedCallback(evt =>
            {
                var property = item.userData as SerializedProperty;
                if (property != null)
                {
                    property.FindPropertyRelative("IsEnabled").boolValue = evt.newValue;
                    property.serializedObject.ApplyModifiedProperties();
                    // 更新禁用样式
                    item.EnableInClassList("step-disabled", !evt.newValue);
                }
            });
            row.Add(enableToggle);

            var titleLabel = new Label { name = "step-title" };
            titleLabel.AddToClassList("step-title");
            row.Add(titleLabel);

            var summaryLabel = new Label { name = "step-summary" };
            summaryLabel.AddToClassList("step-summary");
            row.Add(summaryLabel);

            var spacer = new VisualElement { style = { flexGrow = 1 } };
            row.Add(spacer);

            var deleteButton = new Button { text = "✕", name = "delete-button" };
            deleteButton.AddToClassList("step-delete-button");
            deleteButton.clickable = new Clickable(() =>
            {
                var property = item.userData as SerializedProperty;
                if (property != null && stepsProperty != null)
                {
                    int index = FindPropertyIndex(stepsProperty, property);
                    if (index >= 0)
                    {
                        stepsProperty.DeleteArrayElementAtIndex(index);
                        stepsProperty.serializedObject.ApplyModifiedProperties();
                        RefreshStepList();
                        RefreshDetailPanel();
                    }
                }
            });
            row.Add(deleteButton);

            item.Add(row);
            return item;
        }

        private void BindStepItem(VisualElement element, int index)
        {
            Log($"BindStepItem index: {index}");

            if (stepsProperty == null || index < 0 || index >= stepsProperty.arraySize) return;

            var stepProperty = stepsProperty.GetArrayElementAtIndex(index);
            element.userData = stepProperty;

            var typeProp = stepProperty.FindPropertyRelative("Type");
            var isEnabledProp = stepProperty.FindPropertyRelative("IsEnabled");
            var durationProp = stepProperty.FindPropertyRelative("Duration");
            var easeProp = stepProperty.FindPropertyRelative("Ease");
            var targetTransformProp = stepProperty.FindPropertyRelative("TargetTransform");

            var type = (TweenStepType)typeProp.enumValueIndex;

            // 标题
            var titleLabel = element.Q<Label>("step-title");
            if (titleLabel != null)
            {
                string targetName = targetTransformProp.objectReferenceValue != null
                    ? targetTransformProp.objectReferenceValue.name
                    : "未指定";
                titleLabel.text = $"{index + 1}. [{targetName}] {GetStepDisplayName(type)}";
            }

            // 摘要
            var summaryLabel = element.Q<Label>("step-summary");
            if (summaryLabel != null)
            {
                var ease = (Ease)easeProp.enumValueIndex;
                summaryLabel.text = $"{durationProp.floatValue:F1}s | {ease}";
            }

            // 启用状态
            var enableToggle = element.Q<Toggle>("enable-toggle");
            if (enableToggle != null)
            {
                enableToggle.SetValueWithoutNotify(isEnabledProp.boolValue);
            }

            // 禁用样式
            element.EnableInClassList("step-disabled", !isEnabledProp.boolValue);

            // 类型颜色标记
            element.ClearClassList();
            element.AddToClassList("step-item");
            element.AddToClassList(GetStepTypeCssClass(type));
            if (!isEnabledProp.boolValue) element.AddToClassList("step-disabled");

            // 监听属性变化
            element.TrackPropertyValue(typeProp, _ => UpdateStepTitle(element, index));
            element.TrackPropertyValue(targetTransformProp, _ => UpdateStepTitle(element, index));
            element.TrackPropertyValue(durationProp, _ => UpdateStepTitle(element, index));
            element.TrackPropertyValue(easeProp, _ => UpdateStepTitle(element, index));
            element.TrackPropertyValue(isEnabledProp, prop =>
            {
                element.EnableInClassList("step-disabled", !prop.boolValue);
            });
        }

        private void UpdateStepTitle(VisualElement element, int index)
        {
            if (stepsProperty == null || index < 0 || index >= stepsProperty.arraySize) return;

            serializedObject?.Update();

            var stepProperty = stepsProperty.GetArrayElementAtIndex(index);
            var typeProp = stepProperty.FindPropertyRelative("Type");
            var targetTransformProp = stepProperty.FindPropertyRelative("TargetTransform");

            var type = (TweenStepType)typeProp.enumValueIndex;
            var titleLabel = element.Q<Label>("step-title");

            if (titleLabel != null)
            {
                string targetName = targetTransformProp.objectReferenceValue != null
                    ? targetTransformProp.objectReferenceValue.name
                    : "未指定";
                titleLabel.text = $"{index + 1}. [{targetName}] {GetStepDisplayName(type)}";
            }

            var durationProp = stepProperty.FindPropertyRelative("Duration");
            var easeProp = stepProperty.FindPropertyRelative("Ease");
            var summaryLabel = element.Q<Label>("step-summary");
            if (summaryLabel != null)
            {
                var ease = (Ease)easeProp.enumValueIndex;
                summaryLabel.text = $"{durationProp.floatValue:F1}s | {ease}";
            }
        }

        private void OnStepSelectionChanged(IEnumerable<object> selectedItems)
        {
            RefreshDetailPanel();
        }

        private void OnStepReordered(int oldIndex, int newIndex)
        {
            if (stepsProperty == null) return;
            stepsProperty.MoveArrayElement(oldIndex, newIndex);
            stepsProperty.serializedObject.ApplyModifiedProperties();
        }

        private void RefreshDetailPanel()
        {
            if (detailPanel == null) return;

            var selectedIndex = stepListView?.selectedIndex ?? -1;

            if (selectedIndex < 0 || stepsProperty == null || selectedIndex >= stepsProperty.arraySize)
            {
                detailHelpLabel.style.display = DisplayStyle.Flex;
                detailPropertyField.style.display = DisplayStyle.None;
                detailPropertyField.Unbind();
                return;
            }

            serializedObject?.Update();
            var stepProperty = stepsProperty.GetArrayElementAtIndex(selectedIndex);

            detailHelpLabel.style.display = DisplayStyle.None;
            detailPropertyField.style.display = DisplayStyle.Flex;
            detailPropertyField.Unbind();
            detailPropertyField.BindProperty(stepProperty);
        }

        private int FindPropertyIndex(SerializedProperty arrayProperty, SerializedProperty elementProperty)
        {
            if (arrayProperty == null || elementProperty == null) return -1;

            string path = elementProperty.propertyPath;
            string arrayPath = arrayProperty.propertyPath;
            if (path.StartsWith(arrayPath + ".Array.data["))
            {
                int start = arrayPath.Length + ".Array.data[".Length;
                int end = path.IndexOf(']', start);
                if (end > start && int.TryParse(path.Substring(start, end - start), out int idx))
                {
                    return idx;
                }
            }

            for (int i = 0; i < arrayProperty.arraySize; i++)
            {
                if (arrayProperty.GetArrayElementAtIndex(i).propertyPath == elementProperty.propertyPath)
                    return i;
            }
            return -1;
        }

        private string GetStepDisplayName(TweenStepType type)
        {
            return type switch
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
        }

        private string GetStepTypeCssClass(TweenStepType type)
        {
            return type switch
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
            RefreshStepList();

            // 自动选中新添加的步骤
            if (stepListView != null)
            {
                stepListView.selectedIndex = stepsProperty.arraySize - 1;
            }
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
            {
                PausePreview();
            }
            else if (previewState == PreviewState.Paused)
            {
                ResumePreview();
            }
            else if (previewState == PreviewState.None)
            {
                StartPreview();
            }
        }

        private void OnReplayClicked()
        {
            if (targetPlayer == null) return;
            RestoreInitialStates();
            StartPreview();
        }

        private void OnStopClicked()
        {
            StopPreview();
        }

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

            Log("StartPreview");

            if (previewSequence != null)
            {
                previewSequence.Kill();
                previewSequence = null;
            }
            DOTweenEditorPreview.Stop();

            if (initialStates.Count > 0)
            {
                RestoreInitialStates();
            }
            else
            {
                SaveInitialStates();
            }

            DOTweenEditorPreview.Start();

            try
            {
                previewSequence = DOTween.Sequence();
                BuildPreviewSequence();

                Log($"Preview sequence created, duration: {previewSequence.Duration()}");

                DOTweenEditorPreview.PrepareTweenForPreview(previewSequence);

                previewSequence.OnComplete(() =>
                {
                    if (this == null) return;
                    Log("Preview completed");
                    previewState = PreviewState.Completed;
                    UpdateButtonStates();
                });

                previewSequence.Play();
                previewState = PreviewState.Playing;
                UpdateButtonStates();

                Log($"Preview started, isPlaying: {previewSequence.IsPlaying()}");
            }
            catch (Exception e)
            {
                Debug.LogError($"[DOTweenVisualEditor] 预览启动失败: {e.Message}");
                DOTweenEditorPreview.Stop();
                if (previewSequence != null)
                {
                    previewSequence.Kill();
                    previewSequence = null;
                }
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
            if (previewSequence != null)
            {
                previewSequence.Kill();
                previewSequence = null;
            }

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
                {
                    SaveTransformState(step.TargetTransform);
                }
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
            if (canvasGroup != null)
            {
                alpha = canvasGroup.alpha;
            }

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
                        {
                            renderer.material.color = state.color;
                        }

                        var canvasGroup = t.GetComponent<CanvasGroup>();
                        if (canvasGroup != null)
                        {
                            canvasGroup.alpha = state.alpha;
                        }
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
            {
                initialStates.Remove(key);
            }

            initialStates.Clear();
            Log("Initial states restored");
        }

        private void BuildPreviewSequence()
        {
            Log($"BuildPreviewSequence: step count = {targetPlayer.StepCount}");

            int addedCount = 0;
            foreach (var step in targetPlayer.Steps)
            {
                if (!step.IsEnabled) continue;
                AppendStepToPreview(step);
                addedCount++;
            }

            Log($"Added {addedCount} steps to preview sequence");
        }

        private void AppendStepToPreview(TweenStepData step)
        {
            var target = step.TargetTransform != null ? step.TargetTransform : targetPlayer.transform;
            if (target == null) return;

            Log($"AppendStep: {step.Type} | Target: {target.name} | Duration: {step.Duration}");

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

            if (tweener == null)
            {
                Log($"Warning: tweener is null for {step.Type}");
                return;
            }

            if (step.UseCustomCurve && step.CustomCurve != null)
            {
                tweener.SetEase(step.CustomCurve);
            }
            else
            {
                tweener.SetEase(step.Ease);
            }

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

            Log($"Tweener added to sequence");
        }

        #region Preview Tween 创建

        private Tweener CreatePreviewMoveTween(TweenStepData step, Transform target)
        {
            if (step.UseStartValue)
            {
                switch (step.TransformTarget)
                {
                    case TransformTarget.Position:
                        target.position = step.StartVector;
                        break;
                    case TransformTarget.LocalPosition:
                        target.localPosition = step.StartVector;
                        break;
                }
            }

            float duration = Mathf.Max(0.001f, step.Duration);

            switch (step.TransformTarget)
            {
                case TransformTarget.LocalPosition:
                    return step.IsRelative
                        ? target.DOLocalMove(step.TargetVector, duration).From(isRelative: true)
                        : target.DOLocalMove(step.TargetVector, duration);
                default:
                    return step.IsRelative
                        ? target.DOMove(step.TargetVector, duration).From(isRelative: true)
                        : target.DOMove(step.TargetVector, duration);
            }
        }

        private Tweener CreatePreviewRotateTween(TweenStepData step, Transform target)
        {
            Quaternion startQuat;
            Quaternion targetQuat;

            if (step.UseStartValue)
            {
                startQuat = Quaternion.Euler(step.StartVector);
                switch (step.TransformTarget)
                {
                    case TransformTarget.Rotation:
                        target.rotation = startQuat;
                        break;
                    case TransformTarget.LocalRotation:
                        target.localRotation = startQuat;
                        break;
                }
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
            {
                return step.IsRelative
                    ? target.DOLocalRotateQuaternion(startQuat * targetQuat, duration)
                    : target.DOLocalRotateQuaternion(targetQuat, duration);
            }
            else
            {
                return step.IsRelative
                    ? target.DORotateQuaternion(startQuat * targetQuat, duration)
                    : target.DORotateQuaternion(targetQuat, duration);
            }
        }

        private Tweener CreatePreviewScaleTween(TweenStepData step, Transform target)
        {
            if (step.UseStartValue)
            {
                target.localScale = step.StartVector;
            }

            float duration = Mathf.Max(0.001f, step.Duration);

            return step.IsRelative
                ? target.DOScale(step.TargetVector, duration).From(isRelative: true)
                : target.DOScale(step.TargetVector, duration);
        }

        private Tweener CreatePreviewColorTween(TweenStepData step, Transform target)
        {
            var renderer = target.GetComponent<Renderer>();
            if (renderer == null || renderer.material == null)
            {
                Debug.LogWarning("[DOTweenVisualEditor] 目标物体没有 Renderer，无法预览颜色动画");
                return null;
            }

            if (step.UseStartColor)
            {
                renderer.material.color = step.StartColor;
            }

            float duration = Mathf.Max(0.001f, step.Duration);
            return renderer.material.DOColor(step.TargetColor, duration);
        }

        private Tweener CreatePreviewFadeTween(TweenStepData step, Transform target)
        {
            float duration = Mathf.Max(0.001f, step.Duration);

            var canvasGroup = target.GetComponent<CanvasGroup>();
            if (canvasGroup != null)
            {
                if (step.UseStartFloat)
                {
                    canvasGroup.alpha = step.StartFloat;
                }
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

            Debug.LogWarning("[DOTweenVisualEditor] 目标物体没有 CanvasGroup 或 Renderer，无法预览透明度动画");
            return null;
        }

        #endregion

        #endregion

        #region 调试

        private void Log(string message)
        {
            if (DEBUG_MODE)
            {
                Debug.Log($"[DOTweenVisualEditor] {message}");
            }
        }

        #endregion
    }
}
#endif
