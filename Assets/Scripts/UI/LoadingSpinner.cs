using UnityEngine;
using UnityEngine.UI;

namespace VirtualPet.UI
{
    public sealed class LoadingSpinner : MonoBehaviour
    {
        const float DegreesPerSecond = 360f;

        public static LoadingSpinner Create(Transform parent, float size, Color color)
        {
            var image = UIFactory.CreateImage("Spinner", parent, color, UISprites.SpinnerRing);
            image.rectTransform.Place(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero,
                new Vector2(size, size));
            return image.gameObject.AddComponent<LoadingSpinner>();
        }

        public void SetVisible(bool visible) => gameObject.SetActive(visible);

        void Update() => transform.Rotate(0f, 0f, -DegreesPerSecond * Time.unscaledDeltaTime);
    }
}
