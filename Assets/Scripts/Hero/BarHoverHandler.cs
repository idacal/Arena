using UnityEngine;
using UnityEngine.EventSystems;

namespace Photon.Pun.Demo.Asteroids
{
    public class BarHoverHandler : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        public enum BarType
        {
            Health,
            Mana
        }

        public BarType barType;
        private HeroUIController uiController;

        private void Awake()
        {
            uiController = GetComponentInParent<HeroUIController>();
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (uiController == null) return;

            switch (barType)
            {
                case BarType.Health:
                    uiController.ShowHealthRegenPanel();
                    break;
                case BarType.Mana:
                    uiController.ShowManaRegenPanel();
                    break;
            }
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            if (uiController == null) return;

            switch (barType)
            {
                case BarType.Health:
                    uiController.HideHealthRegenPanel();
                    break;
                case BarType.Mana:
                    uiController.HideManaRegenPanel();
                    break;
            }
        }
    }
} 