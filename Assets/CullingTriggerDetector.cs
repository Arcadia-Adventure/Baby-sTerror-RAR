using UnityEngine;

/// <summary>
/// Attach this to the player to detect culling area triggers.
/// This script invokes the CullingManager when entering/exiting culling area triggers.
/// </summary>
public class CullingTriggerDetector : MonoBehaviour
{
    [Tooltip("Optional: Tag to filter triggers. Leave empty to accept all triggers.")]
    public string cullingTriggerTag = "CullingTrigger";
    public bool useTagFilter = false;

    private void OnTriggerEnter(Collider other)
    {
        if (useTagFilter && !string.IsNullOrEmpty(cullingTriggerTag))
        {
            if (!other.CompareTag(cullingTriggerTag))
                return;
        }

        if (CullingManager.Instance != null)
        {
            CullingManager.Instance.OnPlayerTriggerEnter(other);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (useTagFilter && !string.IsNullOrEmpty(cullingTriggerTag))
        {
            if (!other.CompareTag(cullingTriggerTag))
                return;
        }

        if (CullingManager.Instance != null)
        {
            CullingManager.Instance.OnPlayerTriggerExit(other);
        }
    }
}

