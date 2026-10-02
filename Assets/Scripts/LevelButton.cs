using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>One level's button in level select, filled in from its LevelDefinition.</summary>
public class LevelButton : MonoBehaviour
{
    [SerializeField] Button button;
    [Tooltip("The button's own image, tinted to mark the newest unlocked level.")]
    [SerializeField] Image background;
    [SerializeField] Image thumbnail;
    [SerializeField] TMP_Text label;
    [Tooltip("Covers the button while the level is locked.")]
    [SerializeField] GameObject lockPanel;
    [Tooltip("Shown once the level is unlocked.")]
    [SerializeField] GameObject bar;

    public int Level { get; private set; }

    public void Bind(LevelDefinition definition, Action<int> onSelected)
    {
        Level = definition.level;
        name = $"Button ({Level})";

        if (thumbnail != null && definition.thumbnail != null)
            thumbnail.sprite = definition.thumbnail;
        if (label != null)
            label.text = $"Level {Level}";

        // A fresh event also drops the template's persistent click, which points at the template's level.
        button.onClick = new Button.ButtonClickedEvent();
        int level = Level;
        button.onClick.AddListener(() => onSelected(level));
    }

    public void SetLocked(bool locked)
    {
        if (lockPanel != null)
            lockPanel.SetActive(locked);
        if (bar != null)
            bar.SetActive(!locked);
    }

    public void Highlight(Color color)
    {
        if (background != null)
            background.color = color;
    }

#if UNITY_EDITOR
    /// <summary>Fills the references from the standard button layout: render, BarImage/Text (TMP), LockPanel.</summary>
    public void AutoWire()
    {
        button = GetComponent<Button>();
        background = GetComponent<Image>();
        Transform render = transform.Find("render");
        thumbnail = render != null ? render.GetComponent<Image>() : null;
        Transform barTransform = transform.Find("BarImage");
        bar = barTransform != null ? barTransform.gameObject : null;
        label = barTransform != null ? barTransform.GetComponentInChildren<TMP_Text>(true) : null;
        Transform lockTransform = transform.Find("LockPanel");
        lockPanel = lockTransform != null ? lockTransform.gameObject : null;
    }
#endif
}
