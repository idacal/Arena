using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using DG.Tweening;

namespace Photon.Pun.Demo.Asteroids
{
    public class KillFeedEntry : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private TextMeshProUGUI killerText;
        [SerializeField] private TextMeshProUGUI victimText;
        [SerializeField] private Image killIcon;
        [SerializeField] private Image streakIcon;
        [SerializeField] private TextMeshProUGUI streakText;
        
        [Header("Colors")]
        [SerializeField] private Color killerColor = Color.green;
        [SerializeField] private Color victimColor = Color.red;
        [SerializeField] private Color streakColor = Color.yellow;
        
        [Header("Animation Settings")]
        [SerializeField] private float slideInDuration = 0.3f;
        [SerializeField] private float slideOutDuration = 0.3f;
        [SerializeField] private float stayDuration = 5f;
        [SerializeField] private Ease slideInEase = Ease.OutBack;
        [SerializeField] private Ease slideOutEase = Ease.InBack;
        
        private CanvasGroup canvasGroup;
        private RectTransform rectTransform;
        private Sequence currentSequence;
        private bool isFirstBlood;
        private int streakCount;
        private bool isMultiKill;
        
        private void Awake()
        {
            // Obtener o crear el CanvasGroup
            canvasGroup = GetComponent<CanvasGroup>();
            if (canvasGroup == null)
            {
                canvasGroup = gameObject.AddComponent<CanvasGroup>();
            }
            
            // Obtener el RectTransform
            rectTransform = GetComponent<RectTransform>();
            
            // Configurar posición inicial fuera de la pantalla
            if (rectTransform != null)
            {
                rectTransform.anchoredPosition = new Vector2(-rectTransform.rect.width, 0);
                canvasGroup.alpha = 0;
            }
        }
        
        public void Setup(string killerName, string victimName, Sprite streakIconSprite, string streakTextStr, float entryDuration, bool firstBlood = false, int streak = 0, bool isMultiKill = false)
        {
            isFirstBlood = firstBlood;
            streakCount = streak;
            this.isMultiKill = isMultiKill;
            
            // Asegurarnos de que el objeto esté activo
            gameObject.SetActive(true);
            
            // Configurar textos
            if (killerText != null)
            {
                killerText.text = killerName;
                killerText.color = killerColor;
                killerText.gameObject.SetActive(true);
            }
            
            if (victimText != null)
            {
                victimText.text = victimName;
                victimText.color = victimColor;
                victimText.gameObject.SetActive(true);
            }
            
            // Configurar icono y texto de racha
            if (streakIcon != null)
            {
                streakIcon.sprite = streakIconSprite;
                streakIcon.gameObject.SetActive(streakIconSprite != null);
            }
            
            if (streakText != null)
            {
                streakText.text = streakTextStr;
                streakText.color = streakColor;
                streakText.gameObject.SetActive(!string.IsNullOrEmpty(streakTextStr));
            }
            
            // Detener cualquier secuencia existente
            if (currentSequence != null)
            {
                currentSequence.Kill();
                currentSequence = null;
            }
            
            // Crear nueva secuencia de animación
            currentSequence = DOTween.Sequence();
            
            // Animación de entrada
            currentSequence.Append(rectTransform.DOAnchorPosX(0, slideInDuration).SetEase(slideInEase));
            currentSequence.Join(canvasGroup.DOFade(1, slideInDuration));
            
            // Reproducir sonidos
            PlayKillSounds();
            
            // Esperar la duración especificada
            currentSequence.AppendInterval(entryDuration);
            
            // Animación de salida
            currentSequence.Append(rectTransform.DOAnchorPosX(rectTransform.rect.width, slideOutDuration).SetEase(slideOutEase));
            currentSequence.Join(canvasGroup.DOFade(0, slideOutDuration));
            
            // Desactivar el objeto al finalizar
            currentSequence.OnComplete(() => {
                gameObject.SetActive(false);
                currentSequence = null;
            });
        }
        
        private void PlayKillSounds()
        {
            if (KillFeedSoundManager.Instance == null) return;
            
            // Reproducir sonido de kill normal
            KillFeedSoundManager.Instance.PlayKillSound();
            
            // Reproducir sonido de First Blood si corresponde
            if (isFirstBlood)
            {
                KillFeedSoundManager.Instance.PlayFirstBloodSound();
            }
            // Reproducir sonido de Multi-Kill si corresponde
            else if (isMultiKill)
            {
                KillFeedSoundManager.Instance.PlayMultiKillSound(streakCount);
            }
            // Reproducir sonido de Kill Streak si corresponde
            else
            {
                KillFeedSoundManager.Instance.PlayKillStreakSound(streakCount);
            }
        }
        
        private void OnDestroy()
        {
            // Limpiar las secuencias al destruir el objeto
            if (currentSequence != null)
            {
                currentSequence.Kill();
                currentSequence = null;
            }
        }
    }
} 