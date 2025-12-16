using UnityEngine;

public class HintButton : BaseButton
{
    public void OnClick()
    {
        base.OnClick();
        HintManager.Instance.ActivateIndicator(GameManager.instance.selectedLevel-1, ObjectiveController.instance.currentTask);
    }
}
