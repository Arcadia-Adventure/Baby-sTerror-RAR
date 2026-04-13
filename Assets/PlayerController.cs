using UnityEngine;

public class PlayerController : MonoBehaviour
{
    [SerializeField] private PlayerAnimationController playerAnimationController;
    [SerializeField] private FirstPersonController firstPersonController;
    [SerializeField] private Rigidbody rb;
    public void SetAnimation(PlayerAnimation animation)
    {
        playerAnimationController.SetAnimation(animation);
    }
    public void SetAnimation(int animation)
    {
        playerAnimationController.SetAnimation(animation);
    }
}
