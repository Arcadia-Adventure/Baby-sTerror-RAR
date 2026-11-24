using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using TMPro;
using DG.Tweening;
using Ommy.Singleton;
using Ommy.Prefs;
using Ommy.Audio;

public class ObjectiveUIController : Singleton<ObjectiveUIController>
{
    [SerializeField] private TextMeshProUGUI levelnoTxt;
    [SerializeField] private TextMeshProUGUI missionNameTxt;
    [SerializeField] private TextMeshProUGUI[] taskTxt;
    [SerializeField] private Color completeTaskColor;
    [SerializeField] private float typewriterCharsPerSecond = 28f;
    [SerializeField] private AudioClip typewriterClip;

    public int TotalTasks { get; private set; }
    public int CompletedTasks => TotalTasks - PendingTasks;
    public int PendingTasks => _taskInfos.Count(t => !t.isCompleted);

    /// <summary>
    /// The first task still to do, which is what hints point at. Tasks can complete out of order
    /// (a key picked up early, say), so the completed count isn't always the current task.
    /// </summary>
    public int CurrentTaskIndex
    {
        get
        {
            int index = _taskInfos.FindIndex(t => !t.isCompleted);
            return index >= 0 ? index : TotalTasks;
        }
    }

    public static event Action<TaskType> OnTaskReceived;

    List<TaskInfo> _taskInfos = new();
    readonly Dictionary<TaskType, string> _progressSuffix = new();
    LevelDefinition _levelData;
    int _displayableCount;
    float _lastObjectiveTime;
    bool _stallReported;
    const float StallThresholdSeconds = 120f;

    // Tasks are built in Awake: level modules run in GamePlayManager.Start and may report progress
    // straight away, and the order between two Start methods isn't guaranteed.
    protected override void Awake()
    {
        base.Awake();

        _levelData = LevelConfigLoader.GetLevelData(GamePreference.selectedLevel);
        if (_levelData == null || _levelData.tasks == null || _levelData.tasks.Length == 0)
        {
            Debug.LogError($"[ObjectiveUIController] No task data for level {GamePreference.selectedLevel}");
            _levelData = null;
            return;
        }

        BuildTasks(_levelData);
    }

    private void Start()
    {
        if (_levelData == null)
            return;

        InitializeUI(_levelData);

        _lastObjectiveTime = Time.realtimeSinceStartup;
        _stallReported = false;
        StartCoroutine(StallDetectionLoop());
    }

    void BuildTasks(LevelDefinition levelData)
    {
        _taskInfos.Clear();
        foreach (var td in levelData.tasks)
        {
            _taskInfos.Add(new TaskInfo
            {
                taskType = td.taskType,
                description = td.description,
                completePreviousTasks = td.completePreviousTasks,
                isCompleted = false
            });
        }
        TotalTasks = _taskInfos.Count;
    }

    void InitializeUI(LevelDefinition levelData)
    {
        levelnoTxt.text = "Night " + levelData.level;
        missionNameTxt.text = levelData.missionName;
        _displayableCount = Mathf.Min(TotalTasks, taskTxt.Length);

        for (int i = 0; i < taskTxt.Length; i++)
        {
            if (i < TotalTasks)
            {
                if (i == 0)
                    RevealTask(i);
                else
                    HideTask(i);
            }
            else
            {
                taskTxt[i].gameObject.SetActive(false);
            }
        }
    }

    #region Task Completion

    public static void OnTaskEventReceived(TaskType taskType)
    {
        if (taskType == TaskType.None || Instance == null) return;
        Instance.CompleteTask(taskType);
    }

    void CompleteTask(TaskType taskType)
    {
        int taskIndex = _taskInfos.FindIndex(t => t.taskType == taskType && !t.isCompleted);
        if (taskIndex < 0) return;

        var taskInfo = _taskInfos[taskIndex];

        if (taskInfo.completePreviousTasks)
        {
            for (int i = 0; i < taskIndex; i++)
            {
                var prior = _taskInfos[i];
                if (!prior.isCompleted)
                {
                    prior.isCompleted = true;
                    OnTaskReceived?.Invoke(prior.taskType);
                    UpdateTaskUI(i);
                }
            }
        }

        taskInfo.isCompleted = true;
        OnTaskReceived?.Invoke(taskType);
        UpdateTaskUI(taskIndex);

        _lastObjectiveTime = Time.realtimeSinceStartup;
        _stallReported = false;
        AnalyticsTracker.OnObjectiveCompleted(taskIndex);
        AA_AnalyticsManager.Agent.TrackObjectiveCompleted(
            GamePreference.selectedLevel, taskIndex, taskType.ToString());

        if (_taskInfos.All(t => t.isCompleted))
            GamePlayManager.Instance.LevelComplete();
    }

    void UpdateTaskUI(int taskIndex)
    {
        if (taskIndex < 0 || taskIndex >= _displayableCount) return;

        ShowTaskComplete(taskIndex);

        for (int i = 0; i < _displayableCount; i++)
        {
            if (!_taskInfos[i].isCompleted && !taskTxt[i].gameObject.activeSelf)
            {
                RevealTask(i);
                break;
            }
        }
    }

    void RevealTask(int index)
    {
        var txt = taskTxt[index];
        txt.gameObject.SetActive(true);
        _taskInfos[index].isRevealing = true;
        var tween = TweenUtilities.Typewriter(txt, DisplayText(index), typewriterCharsPerSecond,
            onComplete: () => FinishReveal(index));
        if (tween == null)
        {
            FinishReveal(index);
            return;
        }

        PlayTypewriterSound();
        tween.OnKill(StopTypewriterSound);
    }

    // Progress can change while the line types out, so the finished line picks up the latest text.
    void FinishReveal(int index)
    {
        _taskInfos[index].isRevealing = false;
        if (_taskInfos[index].isCompleted)
            return;

        var txt = taskTxt[index];
        txt.text = DisplayText(index);
        txt.maxVisibleCharacters = int.MaxValue;
    }

    void HideTask(int index)
    {
        var txt = taskTxt[index];
        TweenUtilities.Kill(txt);
        txt.text = DisplayText(index);
        txt.maxVisibleCharacters = 0;
        txt.gameObject.SetActive(false);
    }

    void ShowTaskComplete(int index)
    {
        var txt = taskTxt[index];
        TweenUtilities.Kill(txt);
        _taskInfos[index].isRevealing = false;
        txt.maxVisibleCharacters = int.MaxValue;
        txt.text = $"{_taskInfos[index].description} (complete)";
        txt.color = completeTaskColor;
        txt.gameObject.SetActive(true);
    }

    string DisplayText(int index)
    {
        TaskInfo info = _taskInfos[index];
        return _progressSuffix.TryGetValue(info.taskType, out string suffix)
            ? info.description + suffix
            : info.description;
    }

    /// <summary>
    /// Shows live progress after a task's description, e.g. " (2/3)" or " (1:24)". An empty
    /// suffix clears it. Every task of that type shows it.
    /// </summary>
    public void SetTaskProgress(TaskType taskType, string suffix)
    {
        if (string.IsNullOrEmpty(suffix))
            _progressSuffix.Remove(taskType);
        else
            _progressSuffix[taskType] = suffix;

        for (int i = 0; i < _displayableCount; i++)
        {
            TaskInfo info = _taskInfos[i];
            if (info.taskType != taskType || info.isCompleted || info.isRevealing)
                continue;

            var txt = taskTxt[i];
            if (!txt.gameObject.activeSelf)
                continue;

            txt.text = DisplayText(i);
            txt.maxVisibleCharacters = int.MaxValue;
        }
    }

    #endregion

    void PlayTypewriterSound()
    {
        if (typewriterClip == null || AudioManager.Instance == null) return;
        AudioManager.Instance.PlayLoopingSFX(typewriterClip);
    }

    void StopTypewriterSound()
    {
        if (AudioManager.Instance == null) return;
        AudioManager.Instance.StopLoopingSFX(typewriterClip);
    }

    void OnDisable() => StopTypewriterSound();

    #region Queries

    public bool TryGetTaskInfo(TaskType taskType, out TaskInfo taskInfo)
    {
        taskInfo = _taskInfos.Find(t => t.taskType == taskType);
        return taskInfo != null;
    }

    public bool IsTaskCompleted(TaskType taskType) =>
        _taskInfos.Count > 0 && _taskInfos.TrueForAll(t => t.taskType != taskType || t.isCompleted);

    #endregion

    IEnumerator StallDetectionLoop()
    {
        var wait = new WaitForSecondsRealtime(30f);
        while (true)
        {
            yield return wait;
            if (_stallReported) continue;
            float elapsed = Time.realtimeSinceStartup - _lastObjectiveTime;
            if (elapsed >= StallThresholdSeconds)
            {
                _stallReported = true;
                AA_AnalyticsManager.Agent.TrackObjectiveStalled(
                    GamePreference.selectedLevel, CurrentTaskIndex, elapsed);
            }
        }
    }
}

[Serializable]
public class TaskInfo
{
    public bool isCompleted;
    [NonSerialized] public bool isRevealing;
    public TaskType taskType;
    public bool completePreviousTasks;
    public string description;
}
