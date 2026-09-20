using UnityEngine;
using TMPro;
public class TypeText : MonoBehaviour
{
   public TMP_Text textComponent;
   public string textValue;
   void Start()
   {
        TweenUtilities.Typewriter(textComponent, textValue, 28);
   }

}
