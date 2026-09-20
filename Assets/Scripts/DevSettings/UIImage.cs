using UnityEngine.UI;

namespace PeopleFun.UI
{
    /// <summary>
    /// Image used throughout the ported PeopleFun DevSettings prefab. The
    /// DevSettings dialog only relies on the inherited <see cref="Graphic.color"/>;
    /// the extra 9-slice "collapse" flags are kept so the prefab's serialized
    /// data binds cleanly.
    /// </summary>
    public class UIImage : Image
    {
        public bool CollapseBottom;
        public bool CollapseTop;
        public bool CollapseLeft;
        public bool CollapseRight;
        public bool PreserveBorderAspect = true;
    }
}
