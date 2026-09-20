using System.Linq;
using DG.Tweening;
using System;
using System.Collections.Generic;
using PeopleFun.Utilities;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace PeopleFun.UI
{
	/// <summary>
	/// Manages the Developer Settings dialog, handling the dynamic creation and interaction of various UI elements
	/// such as buttons, toggles, dropdowns, and text fields. Provides functionality for user input and visibility control.
	/// </summary>
    public class DevSettingsDialog : MonoBehaviour
    {
	    [SerializeField] private Button ExitButton;

        [SerializeField] private RectTransform ControlParent;
        [SerializeField] private GameObject ButtonPrefab;
        [SerializeField] private GameObject TogglePrefab;
        [SerializeField] private GameObject TextPrefab;
        [SerializeField] private GameObject DropdownPrefab;
        [SerializeField] private GameObject SpacerPrefab;
        [SerializeField] private GameObject HeaderPrefab;
        [SerializeField] private UICollapsibleGroup SectionPrefab;
        [SerializeField] private Color[] backgroundColors;
        [SerializeField] private ScrollRect scrollRect;
        public GameObject PlayTestParticles;
		private List<Action> polledTextActions = new List<Action>();
		private List<UICollapsibleGroup> collapsibleGroups = new();

		private void Awake()
		{
			var canvasGroup = GetComponent<CanvasGroup>();
			if (canvasGroup != null)
			{
				canvasGroup.interactable = true;
				canvasGroup.blocksRaycasts = true;
			}

			if (GetComponent<Canvas>() != null && GetComponent<GraphicRaycaster>() == null)
				gameObject.AddComponent<GraphicRaycaster>();

			if (ExitButton != null)
				ExitButton.onClick.AddListener(Close);
			Init();
			if (scrollRect != null)
				scrollRect.verticalNormalizedPosition = 1.0f;
		}

        private void OnDestroy()
        {
	        foreach (var group in collapsibleGroups)
	        {
		        group.OnGroupInitialized -= HandleGroupInitialized;
	        }
        }

        public void Init()
        {
	        ControlParent.RemoveAllChildren();
			polledTextActions.Clear();
			collapsibleGroups.Clear();

			AddGroup(out UICollapsibleGroup group,
				DevSettingsMenu.Entries.OrderBy(kvp => kvp.Key).ToDictionary(kvp => kvp.Key, kvp => kvp.Value),
				ControlParent);
        }

        public void Close()
        {
	        Destroy(transform.root.gameObject);
        }

        private void AddGroup(out UICollapsibleGroup group, Dictionary<string, List<DevSettingsEntry>> groupData, Transform parent)
        {
	        group = null;
	        int index = 0;
	        foreach (var kvp in groupData.OrderBy(kvp => kvp.Key))
	        {
			    group = AddSection(kvp.Key, backgroundColors[index % backgroundColors.Length], parent, true);
			    group.SetEntries(kvp.Value, group);
			    group.OnGroupInitialized += HandleGroupInitialized;
			    collapsibleGroups.Add(group);
		        index++;
	        }
        }

        private GameObject InstantiateElement(DevSettingsEntry entry, UICollapsibleGroup group, int index)
        {
	        GameObject go = null;
	        switch (entry)
	        {
		        case DevSettingsToggle toggle:
			        go = AddToggle(toggle.Text, toggle.OnToggle, toggle.DefaultValue());
			        break;
		        case DevSettingsDropDown dropdown:
			        go = AddDropdown(dropdown.Text, dropdown.Options ?? dropdown.OptionsGetter?.Invoke(),
				        dropdown.OnSelectionChanged, dropdown.DefaultOption?.Invoke() ?? 0);
			        break;
		        case DevSettingsStepper stepper:
			        go = AddStepper(stepper);
			        break;
		        case DevSettingsPlayTestButton playTestButton:
			        go = AddButton(playTestButton.Text, playTestButton.OnClick, false);
			        break;
		        case DevSettingsButton button:
			        go = AddButton(button.Text, button.OnClick);
			        break;
		        case DevSettingsTitledEntry titledText:
			        go = titledText.Polled ? AddPolledText(titledText, titledText.title) : AddText(titledText.Text, titledText.title);
			        break;
		        case DevSettingsGroup devSettingsGroup:
			        var section = AddSection(entry.Text, backgroundColors[index % backgroundColors.Length], group.Content, false);
			        section.SetEntries(devSettingsGroup.Entries, group);
			        section.OnGroupInitialized += HandleGroupInitialized;
				    section.RegisterParentGroupResize(section.ParentGroup.OnChildResize);
			        collapsibleGroups.Add(section);
			        go = section.gameObject;
			        break;
		        default:
			        go = entry.Polled ? AddPolledText(entry) : AddText(entry.Text);
			        break;
	        }
	        return go;
        }

        private void Update()
		{
			foreach (var entry in polledTextActions)
			{
				try
				{
					entry();
				}
				catch (Exception e)
				{
					UnityEngine.Debug.LogError(e);
				}
			}
		}

		private void HandleGroupInitialized(UICollapsibleGroup clickedGroup)
		{
			CreateContentForGroup(clickedGroup);
		}

		private void CreateContentForGroup(UICollapsibleGroup clickedGroup)
		{
			var index = 0;
			foreach (var go in clickedGroup.StoredEntries.Select(entry => InstantiateElement(entry, clickedGroup, index)))
			{
				clickedGroup.AddItem(go.RectTransform());
				index++;
			}
		}

		private UICollapsibleGroup AddSection(string text, Color bgColor, Transform parent, bool animate = true)
        {
	        var group = Instantiate(SectionPrefab, parent);
	        group.GetComponentInChildren<TextMeshProUGUI>().text = text;
	        group.GetComponentInChildren<UIImage>().color = bgColor;
	        group.animate = animate;
	        return group;
        }

        private GameObject AddButton(string text, Action onClick, bool playTest = false)
        {
            var btn = Instantiate(ButtonPrefab, ControlParent);
            btn.GetComponentInChildren<TextMeshProUGUI>().text = text;
            btn.GetComponentInChildren<Button>().onClick.AddListener(() =>
            {
	            if (playTest)
	            {
		            Instantiate(PlayTestParticles);
	            }
	            onClick();
            });
            return btn;
        }

        private GameObject AddHeader(string text)
        {
	        var btn = Instantiate(HeaderPrefab, ControlParent);
	        var tmp = btn.GetComponentInChildren<TextMeshProUGUI>();
	        tmp.text = text;
	        return btn;
        }

		private GameObject AddText(string text, string title = null)
		{
			var btn = Instantiate(TextPrefab, ControlParent);
			var textField = btn.transform.Find("TextDisplay").GetComponent<TextMeshProUGUI>();
			var titleField = btn.transform.Find("TitleDisplay").GetComponent<TextMeshProUGUI>();

			textField.text = text;

			bool titleExists = title.IsNullOrEmpty() == false;
			titleField.margin = new Vector4(0, 0, titleExists ? 40 : 0, 0);
			titleField.text = titleExists
								  ? $"{title}: "
								  : "";


			// Allows any text to be copied to system clipboard on hold
			var holdTrigger = btn.GetComponentInChildren<UIHoldEventTrigger>();
			holdTrigger.OnHold.AddListener(() =>
			{
				textField.text = "Copied!";
				AnimationUtilities.Pulse(textField.gameObject, 0.5f, 0.25f).OnComplete(() =>
				{
					textField.text = text;
				});
				GUIUtility.systemCopyBuffer = text;
			});

			return btn;
		}

		private GameObject AddPolledText(DevSettingsEntry entry, string title = null)
		{
			var btn = Instantiate(TextPrefab, ControlParent);
			var textField = btn.transform.Find("TextDisplay").GetComponent<TextMeshProUGUI>();
			var titleField = btn.transform.Find("TitleDisplay").GetComponent<TextMeshProUGUI>();

			Action polledAction = () =>
			{
				textField.text = entry.Text;
			};
			polledAction.Invoke();
			polledTextActions.Add(polledAction);

			bool titleExists = title.IsNullOrEmpty() == false;
			titleField.margin = new Vector4(0, 0, titleExists ? 40 : 0, 0);
			titleField.text = titleExists
								  ? $"{title}: "
								  : "";


			// Allows any text to be copied to system clipboard on hold
			var holdTrigger = btn.GetComponentInChildren<UIHoldEventTrigger>();
			holdTrigger.OnHold.AddListener(() =>
			{
				textField.text = "Copied!";
				AnimationUtilities.Pulse(textField.gameObject, 0.5f, 0.25f).OnComplete(() =>
				{
					polledAction.Invoke();
				});
				GUIUtility.systemCopyBuffer = entry.Text;
			});

			return btn;
		}

		private GameObject AddToggle(string text, Action<bool> onToggle, bool defaultValue)
        {
            var btn = Instantiate(TogglePrefab, ControlParent);
            btn.GetComponentInChildren<TextMeshProUGUI>().text = text;
            btn.GetComponentInChildren<UIToggle>().OnToggledAction += onToggle;
            btn.GetComponentInChildren<UIToggle>().On = defaultValue;
            return btn;
        }

        private GameObject AddDropdown(string text, List<string> options, Action<int> onSelectionChanged,
	        int defaultOption = 0)
        {
	        var btn = Instantiate(DropdownPrefab, ControlParent);
	        btn.GetComponentInChildren<TextMeshProUGUI>().text = text;
	        var dropDown = btn.GetComponentInChildren<TMP_Dropdown>();
	        dropDown.onValueChanged.AddListener(v => onSelectionChanged(v));
	        dropDown.ClearOptions();
	        dropDown.AddOptions(options);
	        dropDown.SetValueWithoutNotify(defaultOption);
	        return btn;
        }

        // Numeric row composed from the dialog's themed prefabs: a Text prefab
        // showing "Label: value" flanked by two Button prefabs for - / +.
        private GameObject AddStepper(DevSettingsStepper entry)
        {
	        float current = Mathf.Clamp(entry.WholeNumbers ? Mathf.Round(entry.GetValue()) : entry.GetValue(),
		        entry.Min, entry.Max);

	        var row = new GameObject(entry.Text, typeof(RectTransform));
	        row.RectTransform().SetParent(ControlParent, false);
	        row.AddComponent<LayoutElement>().minHeight = 90;

	        var hlg = row.AddComponent<HorizontalLayoutGroup>();
	        hlg.childControlWidth = true;
	        hlg.childControlHeight = true;
	        hlg.childForceExpandWidth = false;
	        hlg.childForceExpandHeight = true;
	        hlg.spacing = 12;
	        hlg.childAlignment = TextAnchor.MiddleLeft;

	        var textRow = Instantiate(TextPrefab, row.transform);
	        var valueField = textRow.transform.Find("TextDisplay").GetComponent<TextMeshProUGUI>();
	        var titleField = textRow.transform.Find("TitleDisplay").GetComponent<TextMeshProUGUI>();
	        titleField.margin = new Vector4(0, 0, 40, 0);
	        titleField.text = $"{entry.Text}: ";
	        textRow.transform.GetOrCreateComponent<LayoutElement>().flexibleWidth = 1f;

	        var minus = Instantiate(ButtonPrefab, row.transform);
	        minus.GetComponentInChildren<TextMeshProUGUI>().text = "-";
	        StepButtonWidth(minus);

	        var plus = Instantiate(ButtonPrefab, row.transform);
	        plus.GetComponentInChildren<TextMeshProUGUI>().text = "+";
	        StepButtonWidth(plus);

	        void Apply(float v)
	        {
		        current = Mathf.Clamp(entry.WholeNumbers ? Mathf.Round(v) : v, entry.Min, entry.Max);
		        valueField.text = FormatStepperValue(entry, current);
		        entry.OnValueChanged(current);
	        }

	        minus.GetComponentInChildren<Button>().onClick.AddListener(() => Apply(current - entry.Step));
	        plus.GetComponentInChildren<Button>().onClick.AddListener(() => Apply(current + entry.Step));

	        valueField.text = FormatStepperValue(entry, current);
	        return row;
        }

        private void StepButtonWidth(GameObject button)
        {
	        var le = button.transform.GetOrCreateComponent<LayoutElement>();
	        le.preferredWidth = 110f;
	        le.flexibleWidth = 0f;
        }

        private static string FormatStepperValue(DevSettingsStepper entry, float v)
        {
	        return entry.WholeNumbers ? Mathf.RoundToInt(v).ToString() : v.ToString("0.##");
        }
    }
}
