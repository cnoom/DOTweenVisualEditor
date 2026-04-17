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

        #endregion

        #region UI 元素

        private Toolbar toolbar;
        private ObjectField targetField;
        private Button previewButton;
        private Button stopButton;
        private Button replayButton;
        private Button resetButton;
        private ToolbarMenu addStepMenu;
        private ListView stepListView;
        private Label helpLabel;

        // 状态栏元素
        private VisualElement statusBar;
        private Label stateLabel;
        private Label timeLabel;

        #endregion

        #region 数据

        private DOTweenVisualPlayer targetPlayer;
        private SerializedObject serializedObject;
        private SerializedProperty stepsProperty;

        #endregion

        #region 预览状态

        private enum PreviewState
        {
            None,
            Playing,
            Paused,
            Completed
        }

        private PreviewState previewState = PreviewState.None;
        private Sequence previewSequence;
        private Dictionary<Transform, TransformState> initialStates = new();

        private bool isPreviewing => previewState == PreviewState.Playing;
        private bool isPaused => previewState == PreviewState.Paused;
        private bool hasPreviewed => previewState != PreviewState.None;

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
            window.minSize = new Vector2(400, 300);
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
            if (previewSequence == null)
            {
                timeLabel.text = "--:-- / --:--";
                return;
            }

            float currentTime = previewSequence.Elapsed(false);
            float totalTime = previewSequence.Duration(false);

            string currentStr = FormatTime(currentTime);
            string totalStr = FormatTime(totalTime);

            timeLabel.text = $"{currentStr} / {totalStr}";
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
                Log($"stepsProperty: {stepsProperty}, arraySize: {stepsProperty?.arraySize}");
            }
            else
            {
                serializedObject = null;
                stepsProperty = null;
            }

            RefreshStepList();
        }

        #endregion

        #region UI 构建

        private void CreateGUI()
        {
            Log("CreateGUI");

            BuildUIManually();

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

        private void BuildUIManually()
        {
            rootVisualElement.Clear();

            // 工具栏
            toolbar = new Toolbar();
            toolbar.AddToClassList("main-toolbar");

            var targetLabel = new Label("目标物体:");
            targetLabel.style.marginRight = 4;
            toolbar.Add(targetLabel);

            targetField = new ObjectField
            {
                objectType = typeof(DOTweenVisualPlayer),
                allowSceneObjects = true,
                value = targetPlayer
            };
            targetField.style.width = 200;
            targetField.RegisterValueChangedCallback(OnTargetChanged);
            toolbar.Add(targetField);

            var spacer1 = new VisualElement();
            spacer1.style.flexGrow = 1;
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

            var spacer2 = new VisualElement();
            spacer2.style.flexGrow = 1;
            toolbar.Add(spacer2);

            addStepMenu = new ToolbarMenu { text = "添加步骤" };
            addStepMenu.AddToClassList("toolbar-menu");
            toolbar.Add(addStepMenu);

            rootVisualElement.Add(toolbar);

            // 状态栏
            statusBar = new VisualElement();
            statusBar.AddToClassList("status-bar");
            statusBar.style.flexDirection = FlexDirection.Row;
            statusBar.style.paddingLeft = 8;
            statusBar.style.paddingRight = 8;
            statusBar.style.paddingTop = 4;
            statusBar.style.paddingBottom = 4;
            statusBar.style.backgroundColor = new Color(0.2f, 0.2f, 0.2f, 1f);

            stateLabel = new Label("● 未播放");
            stateLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            stateLabel.style.color = new Color(0.6f, 0.6f, 0.6f);
            stateLabel.style.flexGrow = 1;
            statusBar.Add(stateLabel);

            timeLabel = new Label("--:-- / --:--");
            timeLabel.style.color = new Color(0.8f, 0.8f, 0.8f);
            timeLabel.style.unityTextAlign = TextAnchor.MiddleRight;
            timeLabel.style.width = 140;
            statusBar.Add(timeLabel);

            rootVisualElement.Add(statusBar);

            // 主内容区域
            var contentArea = new VisualElement();
            contentArea.style.flexGrow = 1;
            contentArea.style.position = Position.Relative;

            stepListView = new ListView
            {
                reorderable = false,
                makeItem = MakeStepItem,
                bindItem = BindStepItem,
                selectionType = SelectionType.Single,
                showBorder = true,
                showFoldoutHeader = false,
                showAddRemoveFooter = false,
                virtualizationMethod = CollectionVirtualizationMethod.DynamicHeight
            };
            stepListView.AddToClassList("step-list");
            stepListView.style.flexGrow = 1;
            stepListView.style.minHeight = 100;

            contentArea.Add(stepListView);

            helpLabel = new Label("请在上方指定目标物体（包含 DOTweenVisualPlayer 组件）");
            helpLabel.AddToClassList("help-label");
            helpLabel.style.position = Position.Absolute;
            helpLabel.style.left = 0;
            helpLabel.style.right = 0;
            helpLabel.style.top = 0;
            helpLabel.style.bottom = 0;
            contentArea.Add(helpLabel);

            rootVisualElement.Add(contentArea);

            BuildAddStepMenu();
            UpdateButtonStates();
        }

        #endregion

        #region 步骤列表

        private void RefreshStepList()
        {
            Log($"RefreshStepList - stepListView null: {stepListView == null}, stepsProperty null: {stepsProperty == null}");

            if (stepListView == null) return;

            // 先清空数据源和选中状态，避免虚拟化控制器索引越界
            stepListView.Unbind();
            stepListView.itemsSource = System.Array.Empty<object>();
            stepListView.selectedIndex = -1;

            if (helpLabel != null)
            {
                helpLabel.style.display = stepsProperty != null ? DisplayStyle.None : DisplayStyle.Flex;
            }

            if (stepsProperty != null && serializedObject != null)
            {
                serializedObject.Update();
                stepListView.BindProperty(stepsProperty);
            }

            UpdateButtonStates();
        }

        private VisualElement MakeStepItem()
        {
            Log("MakeStepItem");

            var item = new VisualElement();
            item.AddToClassList("step-item");

            var headerRow = new VisualElement();
            headerRow.AddToClassList("step-header-row");

            var foldout = new Foldout { value = false, name = "step-foldout" };
            foldout.AddToClassList("step-foldout");

            var titleLabel = new Label { name = "step-title" };
            titleLabel.AddToClassList("step-title");

            var summaryLabel = new Label { name = "step-summary" };
            summaryLabel.AddToClassList("step-summary");

            var buttonRow = new VisualElement();
            buttonRow.AddToClassList("step-button-row");

            var upButton = new Button { text = "↑", name = "up-button" };
            upButton.AddToClassList("move-button");
            upButton.clickable = new Clickable(() =>
            {
                var property = item.userData as SerializedProperty;
                if (property != null && stepsProperty != null)
                {
                    int index = FindPropertyIndex(stepsProperty, property);
                    if (index > 0)
                    {
                        int newIndex = index - 1;
                        stepsProperty.MoveArrayElement(index, newIndex);
                        stepsProperty.serializedObject.ApplyModifiedProperties();
                        RefreshStepList();

                        if (stepListView != null)
                        {
                            stepListView.selectedIndex = newIndex;
                        }
                    }
                }
            });

            var downButton = new Button { text = "↓", name = "down-button" };
            downButton.AddToClassList("move-button");
            downButton.clickable = new Clickable(() =>
            {
                var property = item.userData as SerializedProperty;
                if (property != null && stepsProperty != null)
                {
                    int index = FindPropertyIndex(stepsProperty, property);
                    if (index >= 0 && index < stepsProperty.arraySize - 1)
                    {
                        int newIndex = index + 1;
                        stepsProperty.MoveArrayElement(index, newIndex);
                        stepsProperty.serializedObject.ApplyModifiedProperties();
                        RefreshStepList();

                        if (stepListView != null)
                        {
                            stepListView.selectedIndex = newIndex;
                        }
                    }
                }
            });

            var enableToggle = new Toggle { name = "enable-toggle", value = true };
            enableToggle.AddToClassList("enable-toggle");
            enableToggle.RegisterValueChangedCallback(evt =>
            {
                var property = item.userData as SerializedProperty;
                if (property != null)
                {
                    property.FindPropertyRelative("IsEnabled").boolValue = evt.newValue;
                    property.serializedObject.ApplyModifiedProperties();
                }
            });

            var deleteButton = new Button { text = "删除", name = "delete-button" };
            deleteButton.AddToClassList("delete-button");
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
                    }
                }
            });

            buttonRow.Add(upButton);
            buttonRow.Add(downButton);
            buttonRow.Add(enableToggle);
            buttonRow.Add(deleteButton);

            headerRow.Add(foldout);
            headerRow.Add(titleLabel);
            headerRow.Add(summaryLabel);
            headerRow.Add(buttonRow);

            item.Add(headerRow);

            var detailsContainer = new PropertyField { name = "step-details" };
            detailsContainer.AddToClassList("step-details");
            item.Add(detailsContainer);

            detailsContainer.style.display = DisplayStyle.None;

            foldout.RegisterValueChangedCallback(evt =>
            {
                detailsContainer.style.display = evt.newValue ? DisplayStyle.Flex : DisplayStyle.None;
            });

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

            // 更新标题
            var titleLabel = element.Q<Label>("step-title");
            if (titleLabel != null)
            {
                string targetName = "未指定";
                if (targetTransformProp.objectReferenceValue != null)
                {
                    targetName = targetTransformProp.objectReferenceValue.name;
                }
                titleLabel.text = $"{index + 1}. [{targetName}] {GetStepDisplayName(type)}";
            }

            // 更新摘要
            var summaryLabel = element.Q<Label>("step-summary");
            if (summaryLabel != null)
            {
                var ease = (Ease)easeProp.enumValueIndex;
                summaryLabel.text = $"{durationProp.floatValue:F1}s | {ease}";
            }

            // 更新启用状态
            var enableToggle = element.Q<Toggle>("enable-toggle");
            if (enableToggle != null)
            {
                enableToggle.SetValueWithoutNotify(isEnabledProp.boolValue);
            }

            // 绑定详情
            var detailsField = element.Q<PropertyField>("step-details");
            if (detailsField != null)
            {
                detailsField.BindProperty(stepProperty);
            }

            // 监听属性变化
            element.TrackPropertyValue(typeProp, _ => UpdateStepTitle(element, index));
            element.TrackPropertyValue(targetTransformProp, _ => UpdateStepTitle(element, index));
            element.TrackPropertyValue(durationProp, _ => UpdateStepTitle(element, index));
            element.TrackPropertyValue(easeProp, _ => UpdateStepTitle(element, index));
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
                string targetName = "未指定";
                if (targetTransformProp.objectReferenceValue != null)
                {
                    targetName = targetTransformProp.objectReferenceValue.name;
                }
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

        private int FindPropertyIndex(SerializedProperty arrayProperty, SerializedProperty elementProperty)
        {
            if (arrayProperty == null || elementProperty == null) return -1;

            // 优化：从 propertyPath 提取索引 O(1)
            // 格式: _steps.Array.data[N]
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

            // 回退：遍历查找 O(n)
            for (int i = 0; i < arrayProperty.arraySize; i++)
            {
                var item = arrayProperty.GetArrayElementAtIndex(i);
                if (item.propertyPath == elementProperty.propertyPath)
                {
                    return i;
                }
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

        #endregion

        #region 工具栏回调

        private void BuildAddStepMenu()
        {
            if (addStepMenu == null) return;

            addStepMenu.menu.AppendAction("Transform/Move (Position)", _ => AddStep(TweenStepType.Move, TransformTarget.Position));
            addStepMenu.menu.AppendAction("Transform/Move (LocalPosition)", _ => AddStep(TweenStepType.Move, TransformTarget.LocalPosition));
            addStepMenu.menu.AppendAction("Transform/Rotate (Rotation)", _ => AddStep(TweenStepType.Rotate, TransformTarget.Rotation));
            addStepMenu.menu.AppendAction("Transform/Rotate (LocalRotation)", _ => AddStep(TweenStepType.Rotate, TransformTarget.LocalRotation));
            addStepMenu.menu.AppendAction("Transform/Scale", _ => AddStep(TweenStepType.Scale, TransformTarget.Scale));
            addStepMenu.menu.AppendSeparator();
            addStepMenu.menu.AppendAction("Visual/Color", _ => AddStep(TweenStepType.Color));
            addStepMenu.menu.AppendAction("Visual/Fade", _ => AddStep(TweenStepType.Fade));
            addStepMenu.menu.AppendSeparator();
            addStepMenu.menu.AppendAction("Other/Delay", _ => AddStep(TweenStepType.Delay));
            addStepMenu.menu.AppendAction("Other/Callback", _ => AddStep(TweenStepType.Callback));
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

            // 根据类型设置默认值
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
            Log("PausePreview");

            if (previewSequence != null && previewSequence.IsPlaying())
            {
                previewSequence.Pause();
                previewState = PreviewState.Paused;
                UpdateButtonStates();
            }
        }

        private void ResumePreview()
        {
            Log("ResumePreview");

            if (previewSequence != null && !previewSequence.IsPlaying())
            {
                previewSequence.Play();
                previewState = PreviewState.Playing;
                UpdateButtonStates();
            }
        }

        private void StopPreview()
        {
            Log("StopPreview");

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

            previewButton.SetEnabled(hasSteps && !isCompleted);
            previewButton.text = isPreviewing ? "暂停" : (isPaused ? "继续" : "预览");

            stopButton.SetEnabled(inPreview);

            replayButton.SetEnabled(hasSteps && isCompleted);

            resetButton.SetEnabled(isCompleted);

            addStepMenu.SetEnabled(hasTarget && !inPreview);

            UpdateStatusBar();
        }

        private void UpdateStatusBar()
        {
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
            if (t == null) return;
            if (!initialStates.ContainsKey(t))
            {
                // 尝试保存颜色和透明度
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

                        // 恢复颜色
                        var renderer = t.GetComponent<Renderer>();
                        if (renderer != null && renderer.material != null)
                        {
                            renderer.material.color = state.color;
                        }

                        // 恢复透明度
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
                if (!step.IsEnabled)
                {
                    Log($"Step skipped (disabled)");
                    continue;
                }

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

            // 设置缓动
            if (step.UseCustomCurve && step.CustomCurve != null)
            {
                tweener.SetEase(step.CustomCurve);
            }
            else
            {
                tweener.SetEase(step.Ease);
            }

            // 添加到序列
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
            // 应用起始值
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
            // 旋转使用四元数插值
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
