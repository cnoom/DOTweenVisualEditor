#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using DG.Tweening;
using UnityEditor.Compilation;
using CNoom.DOTweenVisual.Components;
using CNoom.DOTweenVisual.Data;

namespace CNoom.DOTweenVisual.Editor
{
    /// <summary>
    /// DOTween 可视化编辑器主窗口
    /// 职责：生命周期管理、UI 骨架构建、子控制器协调
    /// 具体逻辑委托给 StepListController、StepDetailPanel、StepClipboard
    /// </summary>
    [InitializeOnLoad]
    public class DOTweenVisualEditorWindow : EditorWindow
    {
        #region 常量

        private const float LeftPanelMinWidth = 220f;

        #endregion

        #region 数据

        [SerializeField] private DOTweenVisualPlayer targetPlayer;
        private SerializedObject serializedObject;
        private SerializedProperty stepsProperty;

        #endregion

        #region UI 元素

        private ObjectField targetField;
        private Toggle showPathInPlayModeToggle;
        private Button previewButton;
        private Button stopButton;
        private Button replayButton;
        private Button resetButton;
        private ToolbarMenu addStepMenu;
        private Label stateLabel;
        private Label timeLabel;
        private Button langButton;

        #endregion

        #region 子控制器

        private StepListController _listController;
        private StepDetailPanel _detailPanelController;
        private StepClipboard _clipboard;
        private DOTweenPreviewManager _previewManager;
        private PathVisualizer _pathVisualizer;

        #endregion

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
            _previewManager.ProgressUpdated += OnPreviewProgressUpdated;
            _pathVisualizer = new PathVisualizer();
            EditorApplication.update += OnEditorUpdate;
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
            Undo.undoRedoPerformed += OnUndoRedoPerformed;
        }

        private void OnDisable()
        {
            EditorApplication.update -= OnEditorUpdate;
            EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
            Undo.undoRedoPerformed -= OnUndoRedoPerformed;

            if (_previewManager != null)
            {
                _previewManager.StateChanged -= OnPreviewStateChanged;
                _previewManager.ProgressUpdated -= OnPreviewProgressUpdated;
                _previewManager.Dispose();
                _previewManager = null;
            }
            if (_pathVisualizer != null)
            {
                _pathVisualizer.Dispose();
                _pathVisualizer = null;
            }
        }

        private void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.ExitingEditMode)
            {
                // 进入运行模式前：停止预览 + 清空目标引用
                // 场景物体在域重载后引用会失效（僵尸引用），不如直接清空
                if (_previewManager != null && _previewManager.State != DOTweenPreviewManager.PreviewState.None)
                {
                    _previewManager.Reset();
                }
                SetTarget(null);
                if (targetField != null) targetField.value = null;
            }
        }

        private void OnUndoRedoPerformed()
        {
            if (targetPlayer == null) return;

            if (serializedObject != null)
            {
                serializedObject.Update();
            }

            _listController?.RebuildStepList();
            _detailPanelController?.RefreshDetailPanel();
            RebuildPlayerSettings();
            UpdatePathVisualizer();
        }

        private void OnEditorUpdate()
        {
            UpdateTimeDisplay();
        }

        #endregion

        #region UI 构建

        private void CreateGUI()
        {
            BuildUI();

            var styleSheet = DOTweenEditorStyle.FindStyleSheet();
            if (styleSheet != null)
            {
                rootVisualElement.styleSheets.Add(styleSheet);
            }
            else
            {
                DOTweenLog.Error(L10n.Tr("Window/StyleSheetError"));
            }

            if (targetPlayer != null)
            {
                SetTarget(targetPlayer);
            }
        }

        private void BuildUI()
        {
            rootVisualElement.Clear();
            InitControllers();

            // === 顶部：工具栏 ===
            var toolbar = new VisualElement();
            toolbar.AddToClassList("top-toolbar");

            var targetLabel = new Label(L10n.Tr("Window/TargetLabel"));
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

            showPathInPlayModeToggle = new Toggle(L10n.Tr("Window/ShowPathInPlayMode"))
            {
                value = PathVisualizer.ShowPathInPlayMode,
                tooltip = L10n.Tr("Window/ShowPathTooltip")
            };
            showPathInPlayModeToggle.AddToClassList("play-mode-path-toggle");
            showPathInPlayModeToggle.RegisterValueChangedCallback(evt =>
            {
                PathVisualizer.ShowPathInPlayMode = evt.newValue;
            });
            toolbar.Add(showPathInPlayModeToggle);

            var spacer1 = new VisualElement { style = { flexGrow = 1 } };
            toolbar.Add(spacer1);

            // 语言切换下拉按钮
            langButton = new Button(OnLanguageMenuClicked)
            {
                text = L10n.Tr("Menu/Language"),
                tooltip = L10n.Tr("Menu/Language")
            };
            langButton.AddToClassList("lang-button");
            toolbar.Add(langButton);

            var helpButton = new Button(OnHelpClicked)
            {
                text = L10n.Tr("Help/Button"),
                tooltip = L10n.Tr("Help/Title")
            };
            helpButton.AddToClassList("help-button");
            toolbar.Add(helpButton);

            var separator = new VisualElement();
            separator.AddToClassList("toolbar-separator");
            toolbar.Add(separator);

            previewButton = new Button(OnPreviewClicked) { text = L10n.Tr("Window/Preview") };
            previewButton.AddToClassList("toolbar-button");
            toolbar.Add(previewButton);

            stopButton = new Button(OnStopClicked) { text = L10n.Tr("Window/Stop") };
            stopButton.AddToClassList("toolbar-button");
            toolbar.Add(stopButton);

            replayButton = new Button(OnReplayClicked) { text = L10n.Tr("Window/Replay") };
            replayButton.AddToClassList("toolbar-button");
            toolbar.Add(replayButton);

            resetButton = new Button(OnResetClicked) { text = L10n.Tr("Window/Reset") };
            resetButton.AddToClassList("toolbar-button");
            toolbar.Add(resetButton);

            rootVisualElement.Add(toolbar);

            // === 顶部：状态栏 ===
            var statusBar = new VisualElement();
            statusBar.AddToClassList("status-bar");

            stateLabel = new Label(L10n.Tr("Window/StateNone"));
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
            var leftTitle = new Label(L10n.Tr("Window/StepOverview"));
            leftTitle.AddToClassList("panel-title");
            leftHeader.Add(leftTitle);

            addStepMenu = new ToolbarMenu { text = L10n.Tr("Window/AddStep") };
            addStepMenu.AddToClassList("add-step-menu");
            leftHeader.Add(addStepMenu);
            leftPanel.Add(leftHeader);

            _listController.CreateListView(leftPanel);

            // --- 左侧底部：播放设置 ---
            var settingsFoldout = new Foldout
            {
                text = L10n.Tr("Settings/Title"),
                value = true
            };
            settingsFoldout.AddToClassList("player-settings-foldout");
            _settingsFoldout = settingsFoldout;
            BuildPlayerSettings(settingsFoldout);
            leftPanel.Add(settingsFoldout);

            splitView.Add(leftPanel);

            // --- 右侧：步骤详情 ---
            var rightPanel = new VisualElement();
            rightPanel.AddToClassList("right-panel");

            var rightHeader = new VisualElement();
            rightHeader.AddToClassList("panel-header");
            var rightTitle = new Label(L10n.Tr("Window/StepDetail"));
            rightTitle.AddToClassList("panel-title");
            rightHeader.Add(rightTitle);

            rightPanel.Add(rightHeader);

            _detailPanelController.CreateDetailPanel(rightPanel);
            splitView.Add(rightPanel);

            rootVisualElement.Add(splitView);

            BuildAddStepMenu();
            UpdateButtonStates();

            rootVisualElement.UnregisterCallback<KeyDownEvent>(OnKeyDown);
            rootVisualElement.RegisterCallback<KeyDownEvent>(OnKeyDown);

            // 语言切换后恢复数据绑定
            if (targetPlayer != null)
            {
                _listController?.RebuildStepList();
                _detailPanelController?.RefreshDetailPanel();
                RebuildPlayerSettings();
            }
        }

        private void InitControllers()
        {
            // 释放旧控制器，避免事件订阅累积
            _listController?.Dispose();
            _detailPanelController?.Dispose();

            _listController = new StepListController(
                () => serializedObject,
                () => stepsProperty,
                () => targetPlayer,
                index => selectedStepIndex = index,
                () => { _detailPanelController?.RefreshDetailPanel(); UpdatePathVisualizer(); },
                UpdateButtonStates);

            _detailPanelController = new StepDetailPanel(
                () => serializedObject,
                () => stepsProperty,
                () => targetPlayer,
                () => selectedStepIndex,
                () => { _listController?.RebuildStepList(); },
                () => { _detailPanelController?.RefreshDetailPanel(); UpdatePathVisualizer(); },
                OnPathDataChanged);

            _clipboard = new StepClipboard(
                () => serializedObject,
                () => stepsProperty,
                () => targetPlayer,
                () => selectedStepIndex,
                index => selectedStepIndex = index,
                () => _listController?.RebuildStepList(),
                () => { _detailPanelController?.RefreshDetailPanel(); UpdatePathVisualizer(); });
        }

        #endregion

        #region 目标管理

        private int selectedStepIndex
        {
            get => _listController?.SelectedStepIndex ?? -1;
            set => _listController?.SetSelectedIndex(value);
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

            _listController?.RebuildStepList();
            _detailPanelController?.RefreshDetailPanel();
            RebuildPlayerSettings();
            UpdatePathVisualizer();
        }

        #endregion

        #region PathVisualizer 管理

        /// <summary>
        /// 路径数据变更回调（由 StepDetailPanel 触发）
        /// </summary>
        private void OnPathDataChanged()
        {
            _pathVisualizer?.NotifyDataChanged();
        }

        /// <summary>
        /// 根据当前选中步骤更新 PathVisualizer
        /// </summary>
        private void UpdatePathVisualizer()
        {
            if (_pathVisualizer == null) return;

            var stepsProp = stepsProperty;
            int idx = selectedStepIndex;

            if (stepsProp == null || idx < 0 || idx >= stepsProp.arraySize)
            {
                _pathVisualizer.Disable();
                return;
            }

            var stepProp = stepsProp.GetArrayElementAtIndex(idx);
            var type = (TweenStepType)stepProp.FindPropertyRelative("Type").enumValueIndex;

            if (type != TweenStepType.DOPath)
            {
                _pathVisualizer.Disable();
                return;
            }

            // 获取目标 Transform
            var targetTransformProp = stepProp.FindPropertyRelative("TargetTransform");
            var targetTransform = targetTransformProp?.objectReferenceValue as Transform;
            if (targetTransform == null && targetPlayer != null)
                targetTransform = targetPlayer.transform;

            if (targetTransform == null)
            {
                _pathVisualizer.Disable();
                return;
            }

            // 获取起始位置
            var useStartValueProp = stepProp.FindPropertyRelative("UseStartValue");
            var startVectorProp = stepProp.FindPropertyRelative("StartVector");
            Transform capturedTransform = targetTransform;

            _pathVisualizer.SetData(
                stepProp,
                targetTransform,
                () =>
                {
                    if (useStartValueProp != null && useStartValueProp.boolValue)
                        return startVectorProp != null ? startVectorProp.vector3Value : capturedTransform.position;
                    return capturedTransform != null ? capturedTransform.position : Vector3.zero;
                },
                () => { _detailPanelController?.RefreshDetailPanel(); }
            );
            _pathVisualizer.Enable();
        }

        #endregion

        #region 时间显示

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

        internal static string FormatTime(float seconds)
        {
            int minutes = (int)(seconds / 60);
            int secs = (int)(seconds % 60);
            int ms = (int)((seconds * 10) % 10);
            return $"{minutes:D2}:{secs:D2}.{ms}";
        }

        #endregion

        #region 帮助

        private void OnHelpClicked()
        {
            var popup = EditorWindow.GetWindow<HelpPopupWindow>();
            popup.titleContent = new GUIContent(L10n.Tr("Help/Title"));
            popup.minSize = new Vector2(360, 400);
            popup.Show();
        }

        #endregion

        #region 语言切换

        private void OnLanguageMenuClicked()
        {
            var menu = new GenericMenu();
            menu.AddItem(
                new GUIContent(L10n.Tr("Menu/Chinese")),
                L10n.Current == L10n.Language.ZhCN,
                () => { L10n.Current = L10n.Language.ZhCN; BuildUI(); });
            menu.AddItem(
                new GUIContent(L10n.Tr("Menu/English")),
                L10n.Current == L10n.Language.EnUS,
                () => { L10n.Current = L10n.Language.EnUS; BuildUI(); });
            menu.DropDown(langButton.worldBound);
        }

        #endregion

        #region 快捷键

        private void OnKeyDown(KeyDownEvent evt)
        {
            bool modifier = evt.ctrlKey || evt.commandKey;
            if (!modifier) return;

            if (evt.keyCode == KeyCode.C)
            {
                _clipboard?.CopySelectedStep();
                evt.StopPropagation();
            }
            else if (evt.keyCode == KeyCode.V)
            {
                _clipboard?.PasteStep();
                evt.StopPropagation();
            }
            else if (evt.keyCode == KeyCode.D)
            {
                _clipboard?.CopySelectedStep();
                _clipboard?.PasteStep();
                evt.StopPropagation();
            }
        }

        #endregion

        #region 播放设置面板

        private Foldout _settingsFoldout;

        private void BuildPlayerSettings(Foldout container)
        {
            container.Clear();

            if (targetPlayer == null || serializedObject == null)
            {
                container.SetEnabled(false);
                return;
            }

            container.SetEnabled(true);
            serializedObject.Update();

            var playTriggerProp = serializedObject.FindProperty("_playTrigger");
            var disableActionProp = serializedObject.FindProperty("_disableAction");
            var loopsProp = serializedObject.FindProperty("_loops");
            var loopTypeProp = serializedObject.FindProperty("_loopType");
            var debugModeProp = serializedObject.FindProperty("_debugMode");

            AddSettingsField(container, L10n.Tr("Settings/PlayTrigger"),
                CreateSettingsEnumField(playTriggerProp, typeof(PlayTrigger)));
            AddSettingsField(container, L10n.Tr("Settings/DisableAction"),
                CreateSettingsEnumField(disableActionProp, typeof(DisableAction)));
            AddSettingsField(container, L10n.Tr("Settings/Loops"),
                CreateSettingsIntField(loopsProp));
            AddSettingsField(container, L10n.Tr("Settings/LoopType"),
                CreateSettingsEnumField(loopTypeProp, typeof(LoopType)));
            AddSettingsField(container, L10n.Tr("Settings/DebugMode"),
                CreateSettingsToggleField(debugModeProp));
        }

        private EnumField CreateSettingsEnumField(SerializedProperty prop, Type enumType)
        {
            var field = new EnumField((Enum)Enum.GetValues(enumType).GetValue(prop.enumValueIndex));
            field.RegisterValueChangedCallback(evt =>
            {
                if (!DetailFieldFactory.IsValidProperty(prop)) return;
                Undo.RecordObject(targetPlayer, L10n.Tr("Settings/Title"));
                serializedObject.Update();
                prop.enumValueIndex = Convert.ToInt32(evt.newValue);
                serializedObject.ApplyModifiedProperties();
            });
            return field;
        }

        private IntegerField CreateSettingsIntField(SerializedProperty prop)
        {
            var field = new IntegerField { value = prop.intValue };
            field.RegisterValueChangedCallback(evt =>
            {
                if (!DetailFieldFactory.IsValidProperty(prop)) return;
                Undo.RecordObject(targetPlayer, L10n.Tr("Settings/Title"));
                serializedObject.Update();
                prop.intValue = evt.newValue;
                serializedObject.ApplyModifiedProperties();
            });
            return field;
        }

        private Toggle CreateSettingsToggleField(SerializedProperty prop)
        {
            var field = new Toggle { value = prop.boolValue };
            field.RegisterValueChangedCallback(evt =>
            {
                if (!DetailFieldFactory.IsValidProperty(prop)) return;
                Undo.RecordObject(targetPlayer, L10n.Tr("Settings/Title"));
                serializedObject.Update();
                prop.boolValue = evt.newValue;
                serializedObject.ApplyModifiedProperties();
            });
            return field;
        }

        private static void AddSettingsField(VisualElement container, string label, VisualElement field)
        {
            var row = new VisualElement();
            row.AddToClassList("detail-field-row");

            var labelEl = new Label(label);
            labelEl.AddToClassList("detail-field-label");
            row.Add(labelEl);

            field.AddToClassList("detail-field-value");
            row.Add(field);

            container.Add(row);
        }

        private void RebuildPlayerSettings()
        {
            if (_settingsFoldout != null)
                BuildPlayerSettings(_settingsFoldout);
        }

        #endregion

        #region 工具栏回调

        private void BuildAddStepMenu()
        {
            if (addStepMenu == null) return;

            addStepMenu.menu.AppendAction(L10n.Tr("Menu/MoveWorld"), _ => AddStep(TweenStepType.Move, moveSpace: MoveSpace.World));
            addStepMenu.menu.AppendAction(L10n.Tr("Menu/MoveLocal"), _ => AddStep(TweenStepType.Move, moveSpace: MoveSpace.Local));
            addStepMenu.menu.AppendAction(L10n.Tr("Menu/RotateWorld"), _ => AddStep(TweenStepType.Rotate, rotateSpace: RotateSpace.World));
            addStepMenu.menu.AppendAction(L10n.Tr("Menu/RotateLocal"), _ => AddStep(TweenStepType.Rotate, rotateSpace: RotateSpace.Local));
            addStepMenu.menu.AppendAction(L10n.Tr("Menu/Scale"), _ => AddStep(TweenStepType.Scale));
            addStepMenu.menu.AppendSeparator();

            addStepMenu.menu.AppendAction(L10n.Tr("Menu/Color"), _ => AddStep(TweenStepType.Color));
            addStepMenu.menu.AppendAction(L10n.Tr("Menu/Fade"), _ => AddStep(TweenStepType.Fade));
            addStepMenu.menu.AppendSeparator();

            addStepMenu.menu.AppendAction(L10n.Tr("Menu/AnchorMove"), _ => AddStep(TweenStepType.AnchorMove));
            addStepMenu.menu.AppendAction(L10n.Tr("Menu/SizeDelta"), _ => AddStep(TweenStepType.SizeDelta));
            addStepMenu.menu.AppendSeparator();

            addStepMenu.menu.AppendAction(L10n.Tr("Menu/Jump"), _ => AddStep(TweenStepType.Jump));
            addStepMenu.menu.AppendAction(L10n.Tr("Menu/PunchPosition"), _ => AddStep(TweenStepType.Punch, punchTarget: PunchTarget.Position));
            addStepMenu.menu.AppendAction(L10n.Tr("Menu/PunchRotation"), _ => AddStep(TweenStepType.Punch, punchTarget: PunchTarget.Rotation));
            addStepMenu.menu.AppendAction(L10n.Tr("Menu/PunchScale"), _ => AddStep(TweenStepType.Punch, punchTarget: PunchTarget.Scale));
            addStepMenu.menu.AppendAction(L10n.Tr("Menu/ShakePosition"), _ => AddStep(TweenStepType.Shake, shakeTarget: ShakeTarget.Position));
            addStepMenu.menu.AppendAction(L10n.Tr("Menu/ShakeRotation"), _ => AddStep(TweenStepType.Shake, shakeTarget: ShakeTarget.Rotation));
            addStepMenu.menu.AppendAction(L10n.Tr("Menu/ShakeScale"), _ => AddStep(TweenStepType.Shake, shakeTarget: ShakeTarget.Scale));
            addStepMenu.menu.AppendAction(L10n.Tr("Menu/FillAmount"), _ => AddStep(TweenStepType.FillAmount));
            addStepMenu.menu.AppendAction(L10n.Tr("Menu/DOPath"), _ => AddStep(TweenStepType.DOPath));
            addStepMenu.menu.AppendSeparator();

            addStepMenu.menu.AppendAction(L10n.Tr("Menu/Delay"), _ => AddStep(TweenStepType.Delay));
            addStepMenu.menu.AppendAction(L10n.Tr("Menu/Callback"), _ => AddStep(TweenStepType.Callback));
        }

        private void AddStep(TweenStepType type, MoveSpace moveSpace = MoveSpace.World,
            RotateSpace rotateSpace = RotateSpace.World, PunchTarget punchTarget = PunchTarget.Position,
            ShakeTarget shakeTarget = ShakeTarget.Position)
        {
            if (stepsProperty == null)
            {
                DOTweenLog.Warning(L10n.Tr("Window/NoTargetWarning"));
                return;
            }

            Undo.RecordObject(targetPlayer, L10n.Tr("Undo/AddStep"));
            serializedObject.Update();
            stepsProperty.InsertArrayElementAtIndex(stepsProperty.arraySize);
            var newStep = stepsProperty.GetArrayElementAtIndex(stepsProperty.arraySize - 1);

            newStep.FindPropertyRelative("Type").enumValueIndex = (int)type;
            newStep.FindPropertyRelative("IsEnabled").boolValue = true;
            newStep.FindPropertyRelative("Duration").floatValue = 1f;
            newStep.FindPropertyRelative("Delay").floatValue = 0f;
            newStep.FindPropertyRelative("Ease").enumValueIndex = (int)Ease.OutQuad;
            newStep.FindPropertyRelative("MoveSpace").enumValueIndex = (int)moveSpace;
            newStep.FindPropertyRelative("RotateSpace").enumValueIndex = (int)rotateSpace;
            newStep.FindPropertyRelative("PunchTarget").enumValueIndex = (int)punchTarget;
            newStep.FindPropertyRelative("ShakeTarget").enumValueIndex = (int)shakeTarget;

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
                case TweenStepType.DOPath:
                    newStep.FindPropertyRelative("PathType").intValue = 0;
                    newStep.FindPropertyRelative("PathMode").intValue = 0;
                    newStep.FindPropertyRelative("PathResolution").intValue = 10;
                    break;
            }

            stepsProperty.serializedObject.ApplyModifiedProperties();
            _listController?.RebuildStepList();
            _detailPanelController?.RefreshDetailPanel();
        }

        private void OnPreviewClicked()
        {
            if (targetPlayer == null)
            {
                DOTweenLog.Warning(L10n.Tr("Window/NoTargetWarning"));
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

        private void OnPreviewStateChanged()
        {
            UpdateButtonStates();

            var state = _previewManager.State;
            bool inPreview = state != DOTweenPreviewManager.PreviewState.None;
            _pathVisualizer?.SetPreviewing(inPreview);

            if (state != DOTweenPreviewManager.PreviewState.Playing)
            {
                _listController?.ClearStepHighlight();
            }
        }

        private void OnPreviewProgressUpdated(float progress)
        {
            _listController?.HighlightCurrentStep(progress);
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
                    ? L10n.Tr("Window/Pause")
                    : (state == DOTweenPreviewManager.PreviewState.Paused ? L10n.Tr("Window/Continue") : L10n.Tr("Window/Preview"));
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
                    stateLabel.text = L10n.Tr("Window/StateNone");
                    stateLabel.style.color = new Color(0.6f, 0.6f, 0.6f);
                    break;
                case DOTweenPreviewManager.PreviewState.Playing:
                    stateLabel.text = L10n.Tr("Window/StatePlaying");
                    stateLabel.style.color = new Color(0.3f, 0.8f, 0.3f);
                    break;
                case DOTweenPreviewManager.PreviewState.Paused:
                    stateLabel.text = L10n.Tr("Window/StatePaused");
                    stateLabel.style.color = new Color(1f, 0.7f, 0f);
                    break;
                case DOTweenPreviewManager.PreviewState.Completed:
                    stateLabel.text = L10n.Tr("Window/StateCompleted");
                    stateLabel.style.color = new Color(0.3f, 0.6f, 1f);
                    break;
            }
        }

        #endregion
    }
}
#endif
