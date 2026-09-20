using System;
using System.Collections.Generic;

	public class DevSettingsMenu
	{
		public static Dictionary<string, List<DevSettingsEntry>> Entries = new Dictionary<string, List<DevSettingsEntry>>();

		public static void AddEntry(DevSettingsEntry entry)
		{
			var heading = "NO_HEADING";
			if (!Entries.ContainsKey(heading))
				Entries.Add(heading, new List<DevSettingsEntry>());
			Entries[heading].Add(entry);
		}

		public static void AddEntry(string heading, DevSettingsEntry entry)
		{
			if (!Entries.ContainsKey(heading))
				Entries.Add(heading, new List<DevSettingsEntry>());
			Entries[heading].Add(entry);
		}

		public static void RemoveSection(string heading)
		{
			Entries.Remove(heading);
		}
	}

	public class DevSettingsEntry
	{
		public string Text => textGetter?.Invoke() ?? text;
		protected string text;
		protected Func<string> textGetter;
		public bool Polled;
		public DevSettingsEntry(string text = "")
		{
			this.text = text;
		}

		public DevSettingsEntry(Func<string> getter, bool polled = false)
		{
			textGetter = getter;
			Polled = polled;
		}
	}

	public class DevSettingsGroup : DevSettingsEntry
	{
		public List<DevSettingsEntry> Entries = new List<DevSettingsEntry>();

		public DevSettingsGroup(string text) : base(text) { }

		public void AddEntry(DevSettingsEntry entry)
		{
			Entries.Add(entry);
		}
	}

	public class DevSettingsTitledEntry : DevSettingsEntry
	{
		public string title;

		public DevSettingsTitledEntry(string title, string text)
		{
			this.text = text;
			this.title = title;
		}

		public DevSettingsTitledEntry(string title, Func<string> getter, bool polled = true)
		{
			textGetter = getter;
			this.title = title;
			this.Polled = polled;
		}
	}

	public class DevSettingsButton : DevSettingsEntry
	{
		public Action OnClick;

		public DevSettingsButton(string text, Action onClick)
		{
			this.text = text;
			OnClick = onClick;
		}
	}

	public class DevSettingsPlayTestButton : DevSettingsButton
	{
		public DevSettingsPlayTestButton(string text, Action onClick) : base(text, onClick) { }
	}

	public class DevSettingsToggle : DevSettingsEntry
	{
		public Action<bool> OnToggle;
		public Func<bool> DefaultValue;

		public DevSettingsToggle(string text, Action<bool> onToggle, Func<bool> defaultValue)
		{
			this.text = text;
			OnToggle = onToggle;
			DefaultValue = defaultValue;
		}
	}

	public class DevSettingsStepper : DevSettingsEntry
	{
		public float Min;
		public float Max;
		public float Step;
		public bool WholeNumbers;
		public Action<float> OnValueChanged;
		public Func<float> GetValue;

		public DevSettingsStepper(string text, float min, float max, float step,
			Action<float> onValueChanged, Func<float> getValue, bool wholeNumbers = false)
		{
			this.text = text;
			Min = min;
			Max = max;
			Step = step;
			OnValueChanged = onValueChanged;
			GetValue = getValue;
			WholeNumbers = wholeNumbers;
		}
	}

	public class DevSettingsDropDown : DevSettingsEntry
	{
		public List<string> Options;
		public Func<List<string>> OptionsGetter;
		public Action<int> OnSelectionChanged;
		public Func<int> DefaultOption;

		public DevSettingsDropDown(string text, List<string> options, Action<int> onSelectionChanged, Func<int> defaultOption = null)
		{
			this.text = text;
			OnSelectionChanged = onSelectionChanged;
			DefaultOption = defaultOption ?? (() => 0);
			Options = options;
		}

		public DevSettingsDropDown(string text, Func<List<string>> options, Action<int> onSelectionChanged, Func<int> defaultOption = null)
		{
			this.text = text;
			OnSelectionChanged = onSelectionChanged;
			DefaultOption = defaultOption ?? (() => 0);
			OptionsGetter = options;
		}
	}