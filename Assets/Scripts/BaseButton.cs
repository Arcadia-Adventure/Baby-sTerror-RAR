using UnityEngine;
using UnityEngine.UI;
using Ommy.Audio;
public class BaseButton : MonoBehaviour
{
    public Button button;
    public AudioClip clickSound;
    private void OnEnable() {
        button.onClick.AddListener(OnClick);
    }
    private void OnDisable() {
        button.onClick.RemoveListener(OnClick);
    }
    public virtual void OnClick()
    {
        if(clickSound != null) {
            AudioManager.Instance.PlaySFX(clickSound);
        }
    }
}
