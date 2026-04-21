using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

[Serializable]
public class ItemSpriteEntry
{
    public ItemType itemType;
    public Sprite sprite;
}

public class RequireItemIndicator : MonoBehaviour
{
    [SerializeField] private Image itemImage;
    [SerializeField] private Image backgroundImage;
    [SerializeField] private List<ItemSpriteEntry> itemSprites = new();
    [SerializeField] private float fadeDuration = 0.3f;

    CanvasGroup _canvasGroup;
    bool _hasValidSprite;
    bool _isVisible;

    void Awake()
    {
        _canvasGroup = backgroundImage.GetComponent<CanvasGroup>();
        if (_canvasGroup == null)
            _canvasGroup = backgroundImage.gameObject.AddComponent<CanvasGroup>();

        _canvasGroup.alpha = 0f;
        backgroundImage.gameObject.SetActive(false);
    }

    public void SetItem(ItemType itemType)
    {
        if (itemImage == null) return;

        DOTween.Kill(_canvasGroup);
        _isVisible = false;
        _hasValidSprite = false;
        _canvasGroup.alpha = 0f;
        backgroundImage.gameObject.SetActive(false);

        if (itemType == ItemType.None) return;

        var entry = itemSprites.Find(e => e.itemType == itemType);
        if (entry == null || entry.sprite == null) return;

        itemImage.sprite = entry.sprite;
        _hasValidSprite = true;
    }

    public void Show()
    {
        if (!_hasValidSprite || _isVisible) return;
        _isVisible = true;

        DOTween.Kill(_canvasGroup);
        backgroundImage.gameObject.SetActive(true);
        _canvasGroup.DOFade(1f, fadeDuration);
    }

    public void Hide()
    {
        if (!_isVisible) return;
        _isVisible = false;

        DOTween.Kill(_canvasGroup);
        _canvasGroup.DOFade(0f, fadeDuration)
            .OnComplete(() => backgroundImage.gameObject.SetActive(false));
    }

    void OnDisable()
    {
        DOTween.Kill(_canvasGroup);
        _isVisible = false;
    }
}
