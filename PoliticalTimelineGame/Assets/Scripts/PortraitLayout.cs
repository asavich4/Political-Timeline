using UnityEngine;

namespace PoliticalTimeline
{
    /// <summary>Keeps the entire portrait board inside the notch and home-indicator safe area.</summary>
    [ExecuteAlways]
    public class PortraitLayout : MonoBehaviour
    {
        public RectTransform safeArea;
        public RectTransform content;
        public Vector2 designSize = new Vector2(390, 640);

        void LateUpdate() => Apply(Screen.safeArea, new Vector2(Screen.width, Screen.height));

        public void Apply(Rect safePixels, Vector2 screenPixels)
        {
            if (safeArea == null || content == null || screenPixels.x <= 0 || screenPixels.y <= 0) return;
            safeArea.anchorMin = new Vector2(safePixels.xMin / screenPixels.x, safePixels.yMin / screenPixels.y);
            safeArea.anchorMax = new Vector2(safePixels.xMax / screenPixels.x, safePixels.yMax / screenPixels.y);
            safeArea.offsetMin = safeArea.offsetMax = Vector2.zero;
            var canvasRect = (RectTransform)safeArea.parent;
            float width = canvasRect.rect.width * safePixels.width / screenPixels.x;
            float height = canvasRect.rect.height * safePixels.height / screenPixels.y;
            float scale = Mathf.Min(width / designSize.x, height / designSize.y);
            if (scale <= 0) return;
            content.localScale = Vector3.one * scale;
        }
    }
}
