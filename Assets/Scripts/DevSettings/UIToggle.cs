using System;
using UnityEngine;
using UnityEngine.UI;

namespace PeopleFun.UI
{
    /// <summary>
    /// Button-based toggle used by the DevSettings dialog. Clicking flips
    /// <see cref="On"/> and raises <see cref="OnToggledAction"/>; the optional
    /// <see cref="OnState"/>/<see cref="OffState"/> objects mirror the current
    /// value. Field names match the serialized data in the imported prefab.
    /// </summary>
    public class UIToggle : Button
    {
        public GameObject OnState;
        public GameObject OffState;
        public AudioClip ClickSound;
        public AudioClip ErrorSound;
        public string eventName;

        public Action<bool> OnToggledAction;

        [SerializeField] private bool on;

        public bool On
        {
            get => on;
            set
            {
                on = value;
                ApplyVisual();
            }
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            onClick.AddListener(HandleClick);
            ApplyVisual();
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            onClick.RemoveListener(HandleClick);
        }

        private void HandleClick()
        {
            On = !on;
            OnToggledAction?.Invoke(on);
        }

        private void ApplyVisual()
        {
            if (OnState != null) OnState.SetActive(on);
            if (OffState != null) OffState.SetActive(!on);
        }
    }
}
