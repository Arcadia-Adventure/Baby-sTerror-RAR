using UnityEngine;
using Ommy.Audio;
public class BaseButton : MonoBehaviour
{
    public AudioClip clickSound;
    public void OnClick()
    {
        AudioManager.Instance.PlaySFX(SFX.Click);
    }
}
