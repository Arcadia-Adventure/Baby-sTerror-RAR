using UnityEngine;

/// <summary>
/// Names a scene object so level assets can point at it. Put a SceneObjectTag with this key on the
/// object, then drag the key into any module or action field that asks for one.
/// </summary>
[CreateAssetMenu(fileName = "Key", menuName = "Baby's Terror/Scene Object Key")]
public class SceneObjectKey : ScriptableObject
{
}
