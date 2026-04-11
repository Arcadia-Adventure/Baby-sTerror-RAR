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
        foreach (BabyAnimationType type in System.Enum.GetValues(typeof(BabyAnimationType)))
        {
            if (type == BabyAnimationType.None) continue;
            babyAnimator.ResetTrigger(type.ToString());
        }

        if (animationType != BabyAnimationType.None)
        {
            babyAnimator.SetTrigger(animationType.ToString());
        }

        initialAndCurrentAnim = animationType;
    }
}
