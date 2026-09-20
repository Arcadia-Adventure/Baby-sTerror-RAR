using UnityEngine.UI;

namespace PeopleFun.UI
{
    /// <summary>
    /// Invisible graphic that still receives raycasts. Used by the imported
    /// DevSettings prefab to create tappable areas without rendering anything.
    /// </summary>
    public class NonDrawingGraphic : Graphic
    {
        public override void SetMaterialDirty() { }
        public override void SetVerticesDirty() { }

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
        }
    }
}
