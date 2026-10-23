using System;
using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;

namespace VirtualPet.Home
{
    /// <summary>
    /// The pet's behaviour: idle breathing/sway, and a code-driven squash-and-stretch jump
    /// when tapped. Taps arrive through the EventSystem's PhysicsRaycaster on the camera.
    /// </summary>
    public sealed class PetView : MonoBehaviour, IPointerClickHandler
    {
        [SerializeField] Transform visual;
        [SerializeField] float jumpHeight = 1.1f;
        [SerializeField] float airTime = 0.45f;

        Coroutine reaction;
        float idleClock;
        int reactionCount;

        public event Action Tapped;

        public bool IsReacting => reaction != null;

        public void SetVisual(Transform visualRoot) => visual = visualRoot;

        public void OnPointerClick(PointerEventData eventData) => Tapped?.Invoke();

        /// <summary>Plays a jump; every third reaction adds a spin.</summary>
        public void React()
        {
            if (visual == null) return;
            if (reaction != null) StopCoroutine(reaction);
            reactionCount++;
            reaction = StartCoroutine(Jump(spin: reactionCount % 3 == 0));
        }

        void OnDisable()
        {
            reaction = null;
            ResetPose();
        }

        void Update()
        {
            if (reaction != null || visual == null) return;

            idleClock += Time.deltaTime;
            var breathe = 1f + Mathf.Sin(idleClock * 2.4f) * 0.025f;
            visual.localScale = new Vector3(2f - breathe, breathe, 2f - breathe);
            visual.localRotation = Quaternion.Euler(0f, Mathf.Sin(idleClock * 0.8f) * 12f, 0f);
        }

        IEnumerator Jump(bool spin)
        {
            var startRotation = visual.localRotation;

            yield return Tween(0.10f, t =>
                visual.localScale = Vector3.Lerp(Vector3.one, new Vector3(1.2f, 0.75f, 1.2f), Ease(t)));

            yield return Tween(airTime, t =>
            {
                visual.localPosition = new Vector3(0f, 4f * jumpHeight * t * (1f - t), 0f);
                var stretch = Mathf.Lerp(1.2f, 1f, t);
                visual.localScale = new Vector3(2f - stretch, stretch, 2f - stretch);
                var spinAngle = spin ? Mathf.SmoothStep(0f, 360f, t) : 0f;
                visual.localRotation = Quaternion.Slerp(startRotation, Quaternion.identity, t) *
                                       Quaternion.Euler(0f, spinAngle, 0f);
            });

            yield return Tween(0.12f, t =>
                visual.localScale = Vector3.Lerp(new Vector3(1.25f, 0.7f, 1.25f), Vector3.one, Ease(t)));

            ResetPose();
            idleClock = 0f;
            reaction = null;
        }

        void ResetPose()
        {
            if (visual == null) return;
            visual.localPosition = Vector3.zero;
            visual.localScale = Vector3.one;
            visual.localRotation = Quaternion.identity;
        }

        static IEnumerator Tween(float duration, Action<float> step)
        {
            for (var elapsed = 0f; elapsed < duration; elapsed += Time.deltaTime)
            {
                step(elapsed / duration);
                yield return null;
            }

            step(1f);
        }

        static float Ease(float t) => 1f - (1f - t) * (1f - t);
    }
}
