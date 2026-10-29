using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace CPS.KitchenCatch
{
    [RequireComponent(typeof(Outline))]
    public sealed class ButtonFocusOutline : MonoBehaviour, ISelectHandler, IDeselectHandler
    {
        public void OnSelect(BaseEventData eventData) => GetComponent<Outline>().enabled = true;
        public void OnDeselect(BaseEventData eventData) => GetComponent<Outline>().enabled = false;
        private void OnDisable() => GetComponent<Outline>().enabled = false;
        private void OnEnable() => GetComponent<Outline>().enabled =
            EventSystem.current != null && EventSystem.current.currentSelectedGameObject == gameObject;
    }
}
