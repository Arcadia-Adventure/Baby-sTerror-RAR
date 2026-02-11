using UnityEngine;

public class BabyAnimationController : MonoBehaviour
{
    public Animator babyAnimator;
    public BabyAnimationType initialAndCurrentAnim;
    public void Start()
    {
        babyAnimator = GetComponent<Animator>();
        if(initialAndCurrentAnim != BabyAnimationType.None) SetAnimation(initialAndCurrentAnim);
    }
    public void SetAnimation(BabyAnimationType animationType)
    {
        // Reset all bool to avoid conflicts
        foreach (BabyAnimationType type in System.Enum.GetValues(typeof(BabyAnimationType)))
        {
            babyAnimator.ResetTrigger(type.ToString());
        }
        babyAnimator.SetTrigger(animationType.ToString());
        initialAndCurrentAnim = animationType;
    }
    public void SetAnimation(string animation)
    {
        foreach (BabyAnimationType type in System.Enum.GetValues(typeof(BabyAnimationType)))
        {
            babyAnimator.ResetTrigger(type.ToString());
        }
        babyAnimator.SetTrigger(animation);
        initialAndCurrentAnim = (BabyAnimationType)System.Enum.Parse(typeof(BabyAnimationType), animation);
    }
}
