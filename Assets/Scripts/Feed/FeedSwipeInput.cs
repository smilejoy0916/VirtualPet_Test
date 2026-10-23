using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace VirtualPet.Feed
{
    /// <summary>Turns vertical drags into canvas-space deltas and a release velocity.</summary>
    public sealed class FeedSwipeInput : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        Canvas canvas;
        float velocity;

        public event Action DragStarted;

        /// <summary>Vertical delta in canvas units; positive = finger moved up.</summary>
        public event Action<float> Dragged;

        /// <summary>Release velocity in canvas units per second; positive = flick up.</summary>
        public event Action<float> DragEnded;

        float ScaleFactor
        {
            get
            {
                if (canvas == null) canvas = GetComponentInParent<Canvas>().rootCanvas;
                return canvas != null ? canvas.scaleFactor : 1f;
            }
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            velocity = 0f;
            DragStarted?.Invoke();
        }

        public void OnDrag(PointerEventData eventData)
        {
            var delta = eventData.delta.y / ScaleFactor;
            var instantaneous = delta / Mathf.Max(Time.unscaledDeltaTime, 0.001f);
            velocity = Mathf.Lerp(velocity, instantaneous, 0.5f);
            Dragged?.Invoke(delta);
        }

        public void OnEndDrag(PointerEventData eventData) => DragEnded?.Invoke(velocity);
    }
}
