using System;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace PeopleFun.UI
{
	/// <summary>
	/// Handles collapsible UI sections, allowing for the dynamic expansion and contraction of content. Manages size adjustments
	/// and layout updates based on the visibility of content, with support for notifying parent groups of size changes.
	/// </summary>
	public class UICollapsibleGroup : MonoBehaviour
	{
		public event Action<UICollapsibleGroup> OnGroupInitialized;
		private event Action ResizeParent;

		[SerializeField] private RectTransform content;
		[SerializeField] private Button button;
		[SerializeField] private LayoutElement layoutElement;
		[SerializeField] public bool animate = false;

		public List<DevSettingsEntry> StoredEntries { get; private set; } = new();
		public RectTransform Content => content;
		public UICollapsibleGroup ParentGroup { get; private set; }

		private bool open = false;
		private float startingHeight = -1;
		private Tween sizeTween = null;
		private bool isInitialized = false;

		private void Awake()
		{
			content.gameObject.SetActive(false);
		}

		private void Start()
		{
			button.onClick.AddListener(OnClick);
		}

		private void OnClick()
		{
			if (sizeTween != null)
			{
				sizeTween.Kill(true);
				sizeTween = null;
			}

			if (!isInitialized)
			{
				OnGroupInitialized?.Invoke(this);
				isInitialized = true;
			}

			StartCoroutine(ToggleContentVisibility());
		}

		public void SetEntries(List<DevSettingsEntry> entries, UICollapsibleGroup group)
		{
			StoredEntries = entries;
			ParentGroup = group;
		}

		private IEnumerator ToggleContentVisibility()
		{
			content.gameObject.SetActive(!open);
			yield return new WaitForEndOfFrame();
			AdjustMinHeight();
			open = !open;
		}

		private void SetMinHeight(float height)
		{
			if (animate)
			{
				sizeTween = layoutElement.DOMinSize(new Vector2(0, height), .3f).SetEase(Ease.InOutQuad);
			}
			else
			{
				layoutElement.minHeight = height;
				LayoutRebuilder.ForceRebuildLayoutImmediate(this.RectTransform());
			}

			if (sizeTween != null)
				sizeTween.OnComplete(() => ResizeParent?.Invoke());
			else
				ResizeParent?.Invoke();
		}

		public void AddItem(RectTransform item)
		{
			item.SetParent(content, false);
		}

		private void AdjustMinHeight()
		{
			if (open)
			{
				SetMinHeight(startingHeight);
			}
			else
			{
				if (startingHeight < 0)
					startingHeight = layoutElement.minHeight;
				SetMinHeight(content.rect.height + startingHeight);
			}
		}

		public void RegisterParentGroupResize(Action resizeParent)
		{
			ResizeParent = resizeParent;
		}

		public void OnChildResize()
		{
			StartCoroutine(AdjustSize());
		}

		private IEnumerator AdjustSize()
		{
				yield return new WaitForEndOfFrame();
				if (startingHeight < 0)
					startingHeight = layoutElement.minHeight;
				SetMinHeight(content.rect.height + startingHeight);
		}
	}
}