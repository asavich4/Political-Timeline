using UnityEngine;
using UnityEngine.EventSystems;

namespace PoliticalTimeline
{
    public class CardDrag : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        public PresidencyGame game;
        public void OnBeginDrag(PointerEventData e) => game.BeginDrag(e);
        public void OnDrag(PointerEventData e) => game.Drag(e);
        public void OnEndDrag(PointerEventData e) => game.EndDrag(e);
    }
}
