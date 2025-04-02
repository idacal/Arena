using UnityEngine;
using UnityEngine.EventSystems;
using TMPro;

namespace Photon.Pun.Demo.Asteroids
{
    /// <summary>
    /// Componente para mostrar un tooltip al pasar el cursor sobre un elemento de UI
    /// </summary>
    public class TooltipTrigger : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        [Tooltip("Texto que se mostrará en el tooltip")]
        public string tooltipText = "";
        
        [Tooltip("Objeto que contiene el tooltip visual")]
        public GameObject tooltipObject;
        
        [Tooltip("Componente de texto del tooltip")]
        public TMP_Text tooltipTextComponent;
        
        [Tooltip("Delay antes de mostrar el tooltip (en segundos)")]
        public float showDelay = 0.5f;
        
        // Variables privadas
        private bool isHovering = false;
        private float hoverStartTime;
        
        private void Start()
        {
            // Si no se ha asignado el objeto tooltip, intentar buscarlo
            if (tooltipObject == null)
            {
                // Buscar un objeto hijo con "Tooltip" en el nombre
                Transform tooltipTransform = transform.Find("Tooltip");
                if (tooltipTransform != null)
                {
                    tooltipObject = tooltipTransform.gameObject;
                    
                    // Intentar encontrar el componente de texto
                    if (tooltipTextComponent == null)
                    {
                        tooltipTextComponent = tooltipObject.GetComponentInChildren<TMP_Text>();
                    }
                }
            }
            
            // Ocultar el tooltip al inicio
            if (tooltipObject != null)
            {
                tooltipObject.SetActive(false);
            }
        }
        
        private void Update()
        {
            // Verificar si debemos mostrar el tooltip
            if (isHovering && !IsTooltipVisible())
            {
                if (Time.time - hoverStartTime >= showDelay)
                {
                    ShowTooltip();
                }
            }
        }
        
        public void OnPointerEnter(PointerEventData eventData)
        {
            isHovering = true;
            hoverStartTime = Time.time;
        }
        
        public void OnPointerExit(PointerEventData eventData)
        {
            isHovering = false;
            HideTooltip();
        }
        
        /// <summary>
        /// Muestra el tooltip con el texto configurado
        /// </summary>
        private void ShowTooltip()
        {
            if (tooltipObject != null)
            {
                // Actualizar el texto si hay un componente de texto
                if (tooltipTextComponent != null)
                {
                    tooltipTextComponent.text = tooltipText;
                }
                
                tooltipObject.SetActive(true);
            }
        }
        
        /// <summary>
        /// Oculta el tooltip
        /// </summary>
        private void HideTooltip()
        {
            if (tooltipObject != null)
            {
                tooltipObject.SetActive(false);
            }
        }
        
        /// <summary>
        /// Verifica si el tooltip está visible actualmente
        /// </summary>
        private bool IsTooltipVisible()
        {
            return tooltipObject != null && tooltipObject.activeSelf;
        }
    }
} 