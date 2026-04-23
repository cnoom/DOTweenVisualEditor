#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using CNoom.DOTweenVisual.Components;
using DG.Tweening;
using CNoom.DOTweenVisual.Data;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace CNoom.DOTweenVisual.Editor
{
    /// <summary>
    /// 步骤列表控制器
    /// 管理 ListView 的数据绑定、模板创建、时间轴计算和选择管理
    /// </summary>
    internal class StepListController
    {
        #region 数据

        public ListView StepListView => _stepListView;
        public int SelectedStepIndex => _selectedStepIndex;

        /// <summary>
        /// 外部设置选中索引（如粘贴、添加步骤后），同步更新 ListView 选中状态
        /// </summary>
        public void SetSelectedIndex(int index)
        {
            _selectedStepIndex = index;
            if (_stepListView != null)
            {
                if (index >= 0) _stepListView.SetSelection(index);
                else _stepListView.ClearSelection();
            }
        }
        public float TotalSequenceDuration => _totalSequenceDuration;
        public float[] StepStartTimes => _stepStartTimes;

        private ListView _stepListView;
        private int _selectedStepIndex = -1;
        private float _totalSequenceDuration;
        private float[] _stepStartTimes;

        private readonly Func<SerializedObject> _getSerializedObject;
        private readonly Func<SerializedProperty> _getStepsProperty;
        private readonly Func<DOTweenVisualPlayer> _getTargetPlayer;
        private readonly Action<int> _setSelectedIndex;
        private readonly Action _onRefreshDetail;
        private readonly Action _onUpdateButtons;

        public StepListController(
            Func<SerializedObject> getSerializedObject,
            Func<SerializedProperty> getStepsProperty,
            Func<DOTweenVisualPlayer> getTargetPlayer,
            Action<int> setSelectedIndex,
            Action onRefreshDetail,
            Action onUpdateButtons)
        {
            _getSerializedObject = getSerializedObject;
            _getStepsProperty = getStepsProperty;
            _getTargetPlayer = getTargetPlayer;
            _setSelectedIndex = setSelectedIndex;
            _onRefreshDetail = onRefreshDetail;
            _onUpdateButtons = onUpdateButtons;
        }

        #endregion

        #region 初始化

        /// <summary>
        /// 创建并配置 ListView，添加到父容器
        /// </summary>
        public ListView CreateListView(VisualElement parent)
        {
            _stepListView = new ListView
            {
                selectionType = SelectionType.Single,
                reorderable = true,
                showAddRemoveFooter = false,
                showBorder = false,
                showFoldoutHeader = false,
                virtualizationMethod = CollectionVirtualizationMethod.DynamicHeight,
            };
            _stepListView.AddToClassList("step-list");
            _stepListView.style.flexGrow = 1;
            _stepListView.makeItem = MakeStepItem;
            _stepListView.bindItem = BindStepItem;
            _stepListView.unbindItem = UnbindStepItem;
            _stepListView.destroyItem = DestroyStepItem;
            _stepListView.itemsRemoved += OnStepsRemoved;
            _stepListView.itemIndexChanged += OnStepIndexChanged;
            _stepListView.selectionChanged += OnStepSelectionChanged;
            parent.Add(_stepListView);
            return _stepListView;
        }

        /// <summary>
        /// 释放控制器资源，取消 ListView 事件订阅
        /// </summary>
        public void Dispose()
        {
            if (_stepListView != null)
            {
                _stepListView.itemsRemoved -= OnStepsRemoved;
                _stepListView.itemIndexChanged -= OnStepIndexChanged;
                _stepListView.selectionChanged -= OnStepSelectionChanged;
            }
        }

        #endregion

        #region 数据刷新

        /// <summary>
        /// 刷新列表数据源（重新绑定 SerializedProperty）
        /// </summary>
        public void RebuildStepList()
        {
            if (_stepListView == null) return;

            var stepsProperty = _getStepsProperty();
            var serializedObject = _getSerializedObject();

            if (stepsProperty == null || serializedObject == null)
            {
                _selectedStepIndex = -1;
                _stepListView.itemsSource = null;
                return;
            }

            serializedObject.Update();
            CalculateStepTimings();

            _stepListView.itemsSource = new SerializedPropertyArray(stepsProperty);
            _stepListView.Rebuild();

            if (_selectedStepIndex >= stepsProperty.arraySize) _selectedStepIndex = -1;
            if (_selectedStepIndex >= 0)
                _stepListView.SetSelection(_selectedStepIndex);
            else
                _stepListView.ClearSelection();

            _onUpdateButtons();
        }

        /// <summary>
        /// 根据执行模式计算每个步骤的开始时间
        /// </summary>
        public void CalculateStepTimings()
        {
            var stepsProperty = _getStepsProperty();
            if (stepsProperty == null || stepsProperty.arraySize == 0)
            {
                _totalSequenceDuration = 0;
                _stepStartTimes = Array.Empty<float>();
                return;
            }

            int count = stepsProperty.arraySize;
            _stepStartTimes = new float[count];
            float lastTweenStartTime = 0f;
            float sequenceEndTime = 0f;

            for (int i = 0; i < count; i++)
            {
                var step = stepsProperty.GetArrayElementAtIndex(i);
                var type = (TweenStepType)step.FindPropertyRelative("Type").enumValueIndex;
                var duration = Mathf.Max(0.001f, step.FindPropertyRelative("Duration").floatValue);
                float startTime;

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

                _stepStartTimes[i] = startTime;
            }

            _totalSequenceDuration = sequenceEndTime;
        }

        #endregion

        #region 模板

        private VisualElement MakeStepItem()
        {
            var item = new VisualElement();
            item.AddToClassList("step-item");

            var row = new VisualElement();
            row.AddToClassList("step-row");

            var typeDot = new VisualElement();
            typeDot.AddToClassList("step-type-dot");
            row.Add(typeDot);

            var enableToggle = new Toggle();
            enableToggle.AddToClassList("step-enable-toggle");
            enableToggle.RegisterValueChangedCallback(evt =>
            {
                var data = item.userData as StepItemData;
                if (data == null || data.Property == null) return;
                var prop = data.Property;
                int idx = data.OriginalIndex;
                var stepsProperty = _getStepsProperty();
                if (stepsProperty == null || idx < 0 || idx >= stepsProperty.arraySize) return;
                var targetProp = stepsProperty.GetArrayElementAtIndex(idx);
                targetProp.FindPropertyRelative("IsEnabled").boolValue = evt.newValue;
                targetProp.serializedObject.ApplyModifiedProperties();
                item.EnableInClassList("step-disabled", !evt.newValue);
            });
            enableToggle.RegisterCallback<ClickEvent>(e => e.StopPropagation());
            row.Add(enableToggle);

            var titleLabel = new Label();
            titleLabel.AddToClassList("step-title");
            row.Add(titleLabel);

            var deleteButton = new Button { text = "X" };
            deleteButton.AddToClassList("step-delete-button");
            deleteButton.RegisterCallback<ClickEvent>(e =>
            {
                var data = item.userData as StepItemData;
                if (data == null || data.Property == null) return;
                int idx = data.OriginalIndex;
                var stepsProperty = _getStepsProperty();
                var targetPlayer = _getTargetPlayer();
                var serializedObject = _getSerializedObject();
                if (stepsProperty == null || idx < 0 || idx >= stepsProperty.arraySize) return;

                Undo.RecordObject(targetPlayer, L10n.Tr("Undo/DeleteStep"));
                serializedObject.Update();
                stepsProperty.DeleteArrayElementAtIndex(idx);
                stepsProperty.serializedObject.ApplyModifiedProperties();

                if (_selectedStepIndex == idx)
                    _selectedStepIndex = -1;
                else if (_selectedStepIndex > idx)
                    _selectedStepIndex--;

                _setSelectedIndex(_selectedStepIndex);
                RebuildStepList();
                _onRefreshDetail();
                e.StopPropagation();
            });
            row.Add(deleteButton);

            item.Add(row);

            var summaryRow = new VisualElement();
            summaryRow.AddToClassList("step-summary-row");
            var summaryLabel = new Label();
            summaryLabel.AddToClassList("step-summary");
            summaryRow.Add(summaryLabel);
            item.Add(summaryRow);

            var timelineTrack = new VisualElement();
            timelineTrack.AddToClassList("step-timeline-track");
            var timelineBar = new VisualElement();
            timelineBar.AddToClassList("step-timeline-bar");
            timelineTrack.Add(timelineBar);
            item.Add(timelineTrack);

            return item;
        }

        private void BindStepItem(VisualElement element, int index)
        {
            var stepsProperty = _getStepsProperty();
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
                : L10n.Tr("StepList/NoTarget");

            element.userData = new StepItemData(stepProperty, index);

            element.ClearClassList();
            element.AddToClassList("step-item");
            if (!isEnabledProp.boolValue) element.AddToClassList("step-disabled");

            var toggle = element.Q<Toggle>(className: "step-enable-toggle");
            if (toggle != null) toggle.SetValueWithoutNotify(isEnabledProp.boolValue);

            var titleLabel = element.Q<Label>(className: "step-title");
            if (titleLabel != null) titleLabel.text = $"{index + 1}. [{targetName}] {DOTweenEditorStyle.GetStepDisplayName(type)}";

            var summaryLabel = element.Q<Label>(className: "step-summary");
            if (summaryLabel != null) summaryLabel.text = $"{durationProp.floatValue:F1}s | {ease}";

            var timelineTrack = element.Q<VisualElement>(className: "step-timeline-track");
            if (timelineTrack != null)
            {
                timelineTrack.style.height = 4f;
                timelineTrack.style.position = Position.Relative;
                timelineTrack.style.marginTop = 3f;
            }

            var timelineBar = element.Q<VisualElement>(className: "step-timeline-bar");
            if (timelineBar != null && _stepStartTimes != null && index < _stepStartTimes.Length)
            {
                float start = _stepStartTimes[index];
                float dur = durationProp.floatValue;
                float total = Mathf.Max(0.001f, _totalSequenceDuration);

                timelineBar.style.position = Position.Absolute;
                timelineBar.style.height = Length.Percent(100f);
                timelineBar.style.left = Length.Percent(Mathf.Min(start / total * 100f, 97f));
                timelineBar.style.width = Length.Percent(Mathf.Max(3f, dur / total * 100f));

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

        #endregion

        #region 事件处理

        private void OnStepSelectionChanged(IEnumerable<object> selectedItems)
        {
            using var enumerator = selectedItems?.GetEnumerator();
            if (enumerator != null && enumerator.MoveNext())
                _selectedStepIndex = _stepListView.selectedIndex;
            else
                _selectedStepIndex = -1;

            _setSelectedIndex(_selectedStepIndex);
            _onRefreshDetail();
        }

        private void OnStepIndexChanged(int from, int to)
        {
            var stepsProperty = _getStepsProperty();
            if (stepsProperty == null) return;
            var targetPlayer = _getTargetPlayer();
            var serializedObject = _getSerializedObject();
            Undo.RecordObject(targetPlayer, L10n.Tr("Undo/ReorderStep"));
            serializedObject.Update();
            stepsProperty.MoveArrayElement(from, to);
            stepsProperty.serializedObject.ApplyModifiedProperties();
            CalculateStepTimings();

            if (_selectedStepIndex == from)
                _selectedStepIndex = to;
            else if (from < _selectedStepIndex && to >= _selectedStepIndex)
                _selectedStepIndex--;
            else if (from > _selectedStepIndex && to <= _selectedStepIndex)
                _selectedStepIndex++;

            _setSelectedIndex(_selectedStepIndex);
            _onRefreshDetail();
        }

        private void OnStepsRemoved(IEnumerable<int> removedIndices)
        {
            foreach (var idx in removedIndices)
            {
                if (_selectedStepIndex == idx)
                    _selectedStepIndex = -1;
                else if (_selectedStepIndex > idx)
                    _selectedStepIndex--;
            }

            _setSelectedIndex(_selectedStepIndex);
            _onRefreshDetail();
        }

        #endregion

        #region 高亮

        /// <summary>
        /// 根据预览进度高亮当前执行的步骤
        /// </summary>
        public void HighlightCurrentStep(float progress)
        {
            if (_stepListView == null || _stepStartTimes == null) return;

            var stepsProperty = _getStepsProperty();
            if (stepsProperty == null) return;

            float currentTime = progress * _totalSequenceDuration;

            for (int i = 0; i < _stepStartTimes.Length; i++)
            {
                if (i >= stepsProperty.arraySize) break;

                var item = _stepListView.GetRootElementForIndex(i);
                if (item == null) continue;

                var stepProp = stepsProperty.GetArrayElementAtIndex(i);
                float stepStart = _stepStartTimes[i];
                float stepDur = stepProp.FindPropertyRelative("Duration").floatValue;
                bool isActive = currentTime >= stepStart && currentTime <= stepStart + stepDur;

                item.EnableInClassList("step-active", isActive);
            }
        }

        /// <summary>
        /// 清除所有步骤高亮
        /// </summary>
        public void ClearStepHighlight()
        {
            if (_stepListView == null) return;

            var stepsProperty = _getStepsProperty();
            if (stepsProperty == null) return;

            for (int i = 0; i < stepsProperty.arraySize; i++)
            {
                var item = _stepListView.GetRootElementForIndex(i);
                if (item == null) continue;
                item.EnableInClassList("step-active", false);
            }
        }

        #endregion

        #region 辅助类

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
    }
}
#endif
