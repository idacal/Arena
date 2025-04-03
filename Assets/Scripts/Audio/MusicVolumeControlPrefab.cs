using UnityEngine;
using UnityEngine.UI;
using TMPro;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Photon.Pun.Demo.Asteroids
{
    /// <summary>
    /// Clase para crear un prefab de control de volumen de música
    /// </summary>
    public class MusicVolumeControlPrefab : MonoBehaviour
    {
#if UNITY_EDITOR
        [MenuItem("Tools/Audio/Create Music Volume Control")]
        public static void CreateMusicVolumeControl()
        {
            // Crear el canvas principal
            GameObject canvasObj = new GameObject("MusicVolumeCanvas");
            Canvas canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;
            
            // Agregar CanvasScaler
            CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            
            // Agregar GraphicRaycaster
            canvasObj.AddComponent<GraphicRaycaster>();
            
            // Crear panel contenedor
            GameObject panelObj = new GameObject("VolumeControlPanel");
            panelObj.transform.SetParent(canvasObj.transform, false);
            
            RectTransform panelRect = panelObj.AddComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(1, 1);
            panelRect.anchorMax = new Vector2(1, 1);
            panelRect.pivot = new Vector2(1, 1);
            panelRect.anchoredPosition = new Vector2(-20, -20);
            panelRect.sizeDelta = new Vector2(300, 80);
            
            // Agregar imagen de fondo del panel
            Image panelImage = panelObj.AddComponent<Image>();
            panelImage.color = new Color(0.1f, 0.1f, 0.1f, 0.8f);
            
            // Crear botón de silencio
            GameObject muteButtonObj = new GameObject("MuteButton");
            muteButtonObj.transform.SetParent(panelObj.transform, false);
            
            RectTransform muteButtonRect = muteButtonObj.AddComponent<RectTransform>();
            muteButtonRect.anchorMin = new Vector2(0, 0.5f);
            muteButtonRect.anchorMax = new Vector2(0, 0.5f);
            muteButtonRect.pivot = new Vector2(0, 0.5f);
            muteButtonRect.anchoredPosition = new Vector2(10, 0);
            muteButtonRect.sizeDelta = new Vector2(40, 40);
            
            // Agregar componente de botón
            Button muteButton = muteButtonObj.AddComponent<Button>();
            Image muteButtonImage = muteButtonObj.AddComponent<Image>();
            muteButtonImage.color = Color.white;
            muteButton.targetGraphic = muteButtonImage;
            
            // Crear icono de sonido activado
            GameObject soundOnObj = new GameObject("SoundOnIcon");
            soundOnObj.transform.SetParent(muteButtonObj.transform, false);
            
            RectTransform soundOnRect = soundOnObj.AddComponent<RectTransform>();
            soundOnRect.anchorMin = Vector2.zero;
            soundOnRect.anchorMax = Vector2.one;
            soundOnRect.sizeDelta = Vector2.zero;
            
            Image soundOnImage = soundOnObj.AddComponent<Image>();
            soundOnImage.color = new Color(0.2f, 0.8f, 0.2f);
            
            // Crear icono de sonido desactivado
            GameObject soundOffObj = new GameObject("SoundOffIcon");
            soundOffObj.transform.SetParent(muteButtonObj.transform, false);
            
            RectTransform soundOffRect = soundOffObj.AddComponent<RectTransform>();
            soundOffRect.anchorMin = Vector2.zero;
            soundOffRect.anchorMax = Vector2.one;
            soundOffRect.sizeDelta = Vector2.zero;
            
            Image soundOffImage = soundOffObj.AddComponent<Image>();
            soundOffImage.color = new Color(0.8f, 0.2f, 0.2f);
            soundOffObj.SetActive(false);
            
            // Crear slider de volumen
            GameObject sliderObj = new GameObject("VolumeSlider");
            sliderObj.transform.SetParent(panelObj.transform, false);
            
            RectTransform sliderRect = sliderObj.AddComponent<RectTransform>();
            sliderRect.anchorMin = new Vector2(0, 0.5f);
            sliderRect.anchorMax = new Vector2(1, 0.5f);
            sliderRect.pivot = new Vector2(0.5f, 0.5f);
            sliderRect.anchoredPosition = new Vector2(30, 0);
            sliderRect.sizeDelta = new Vector2(-100, 20);
            
            // Agregar componente de slider
            Slider volumeSlider = sliderObj.AddComponent<Slider>();
            
            // Crear fondo del slider
            GameObject sliderBgObj = new GameObject("Background");
            sliderBgObj.transform.SetParent(sliderObj.transform, false);
            
            RectTransform sliderBgRect = sliderBgObj.AddComponent<RectTransform>();
            sliderBgRect.anchorMin = Vector2.zero;
            sliderBgRect.anchorMax = Vector2.one;
            sliderBgRect.sizeDelta = Vector2.zero;
            
            Image sliderBgImage = sliderBgObj.AddComponent<Image>();
            sliderBgImage.color = new Color(0.3f, 0.3f, 0.3f, 1);
            
            // Crear área de relleno
            GameObject fillAreaObj = new GameObject("Fill Area");
            fillAreaObj.transform.SetParent(sliderObj.transform, false);
            
            RectTransform fillAreaRect = fillAreaObj.AddComponent<RectTransform>();
            fillAreaRect.anchorMin = new Vector2(0, 0.5f);
            fillAreaRect.anchorMax = new Vector2(1, 0.5f);
            fillAreaRect.pivot = new Vector2(0.5f, 0.5f);
            fillAreaRect.sizeDelta = new Vector2(-10, 10);
            
            // Crear relleno
            GameObject fillObj = new GameObject("Fill");
            fillObj.transform.SetParent(fillAreaObj.transform, false);
            
            RectTransform fillRect = fillObj.AddComponent<RectTransform>();
            fillRect.anchorMin = Vector2.zero;
            fillRect.anchorMax = new Vector2(1, 1);
            fillRect.pivot = new Vector2(0.5f, 0.5f);
            fillRect.sizeDelta = Vector2.zero;
            
            Image fillImage = fillObj.AddComponent<Image>();
            fillImage.color = new Color(0.2f, 0.6f, 1f);
            
            // Configurar el slider
            volumeSlider.fillRect = fillRect;
            volumeSlider.targetGraphic = sliderBgImage;
            volumeSlider.direction = Slider.Direction.LeftToRight;
            volumeSlider.value = 0.5f;
            
            // Crear handle
            GameObject handleAreaObj = new GameObject("Handle Area");
            handleAreaObj.transform.SetParent(sliderObj.transform, false);
            
            RectTransform handleAreaRect = handleAreaObj.AddComponent<RectTransform>();
            handleAreaRect.anchorMin = Vector2.zero;
            handleAreaRect.anchorMax = Vector2.one;
            handleAreaRect.sizeDelta = Vector2.zero;
            
            // Crear el handle
            GameObject handleObj = new GameObject("Handle");
            handleObj.transform.SetParent(handleAreaObj.transform, false);
            
            RectTransform handleRect = handleObj.AddComponent<RectTransform>();
            handleRect.anchorMin = new Vector2(0.5f, 0.5f);
            handleRect.anchorMax = new Vector2(0.5f, 0.5f);
            handleRect.pivot = new Vector2(0.5f, 0.5f);
            handleRect.sizeDelta = new Vector2(20, 20);
            
            Image handleImage = handleObj.AddComponent<Image>();
            handleImage.color = new Color(1f, 1f, 1f);
            
            volumeSlider.handleRect = handleRect;
            
            // Crear texto de valor
            GameObject valueTextObj = new GameObject("ValueText");
            valueTextObj.transform.SetParent(panelObj.transform, false);
            
            RectTransform valueTextRect = valueTextObj.AddComponent<RectTransform>();
            valueTextRect.anchorMin = new Vector2(1, 0.5f);
            valueTextRect.anchorMax = new Vector2(1, 0.5f);
            valueTextRect.pivot = new Vector2(1, 0.5f);
            valueTextRect.anchoredPosition = new Vector2(-10, 0);
            valueTextRect.sizeDelta = new Vector2(50, 30);
            
            TMP_Text valueText = valueTextObj.AddComponent<TextMeshProUGUI>();
            valueText.text = "50%";
            valueText.color = Color.white;
            valueText.alignment = TextAlignmentOptions.Center;
            valueText.font = TMP_Settings.defaultFontAsset;
            valueText.fontSize = 16;
            
            // Agregar el componente MusicVolumeUI
            MusicVolumeUI volumeUI = panelObj.AddComponent<MusicVolumeUI>();
            volumeUI.volumeSlider = volumeSlider;
            volumeUI.muteButton = muteButton;
            volumeUI.soundOnIcon = soundOnObj;
            volumeUI.soundOffIcon = soundOffObj;
            volumeUI.volumeValueText = valueText;
            volumeUI.showAsPercentage = true;
            
            // Guardar prefab
            string prefabPath = "Assets/Prefabs/UI/MusicVolumeControl.prefab";
            
            // Asegurarse que existe el directorio
            if (!AssetDatabase.IsValidFolder("Assets/Prefabs"))
            {
                AssetDatabase.CreateFolder("Assets", "Prefabs");
            }
            
            if (!AssetDatabase.IsValidFolder("Assets/Prefabs/UI"))
            {
                AssetDatabase.CreateFolder("Assets/Prefabs", "UI");
            }
            
            // Guardar el prefab
            PrefabUtility.SaveAsPrefabAsset(canvasObj, prefabPath);
            
            // Mostrar mensaje de éxito
            Debug.Log($"Prefab de control de volumen creado en: {prefabPath}");
            
            // Seleccionar el prefab en el proyecto
            UnityEngine.Object prefabAsset = AssetDatabase.LoadAssetAtPath(prefabPath, typeof(GameObject));
            Selection.activeObject = prefabAsset;
            
            // Destruir el objeto temporal
            Object.DestroyImmediate(canvasObj);
        }
#endif
    }
} 