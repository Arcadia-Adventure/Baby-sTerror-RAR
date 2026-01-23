using UnityEngine;

public class BabyAnimationController : MonoBehaviour
{
    public Animator babyAnimator;
    public BabyAnimationType initialanimation;
    public void Start()
    {
        babyAnimator = GetComponent<Animator>();
        if(initialanimation != BabyAnimationType.None) SetAnimation(initialanimation);
    }
    public void SetAnimation(BabyAnimationType animationType)
    {
        // Reset all bool to avoid conflicts
        foreach (BabyAnimationType type in System.Enum.GetValues(typeof(BabyAnimationType)))
        {
            babyAnimator.ResetTrigger(type.ToString());
        }
        babyAnimator.SetTrigger(animationType.ToString());
    }
    public void SetAnimation(string animation)
    {
        foreach (BabyAnimationType type in System.Enum.GetValues(typeof(BabyAnimationType)))
        {
            babyAnimator.ResetTrigger(type.ToString());
        }
        babyAnimator.SetTrigger(animation);
    }
}
