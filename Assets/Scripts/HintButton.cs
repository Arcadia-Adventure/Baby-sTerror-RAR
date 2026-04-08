using UnityEngine;
using TMPro;
using Ommy.Prefs;
using Ommy.Audio;
public class HintButton : BaseButton
{
    public int HintCount => GamePreference.hintCount;
    public TMP_Text hintCountText;
    private void Start() 
    {
        UpdateHintCountText();
    }
    public void UpdateHintCountText()
    {
        hintCountText.text = HintCount > 0 ? HintCount.ToString() : "AD";
        hintCountText.color = HintCount > 0 ? Color.white : Color.yellow;
    }
    public override void OnClick()
    {
        AudioManager.Instance.PlaySFX(SFX.Click);
        if(HintCount <= 0) RechargeAndShowHint();
        else ShowHint();
    }
    public void RechargeAndShowHint()
    {
        ArcadiaSdkManager.Agent.ShowRewardedAd((int x) =>
        {
            GamePreference.hintCount++;
            ShowHint();
        });
    }
    public void ShowHint()
    {
        // return if already activated
        //if(HintManager.Instance.IsCurrentHintActive()) return;   
        HintManager.Instance.ShowCurrentHint();
        GamePreference.hintCount--;
        UpdateHintCountText();
    }
}
