using UnityEngine;

namespace Photon.Pun.Demo.Asteroids
{
    public class CoinSpriteCreator : MonoBehaviour
    {
        // Variables para personalizar la apariencia de las monedas
        [Header("Apariencia")]
        public Color mainColor = new Color(1f, 0.84f, 0.0f);
        public Color edgeColor = new Color(0.8f, 0.6f, 0.0f);
        public Color highlightColor = new Color(1f, 0.95f, 0.7f);
        public int textureSize = 64;
        
        private static Sprite coinSprite;
        
        void Awake()
        {
            if (coinSprite == null)
            {
                CreateCoinSprite();
            }
        }
        
        // Crea un sprite de moneda en tiempo de ejecución
        private void CreateCoinSprite()
        {
            Texture2D texture = CreateCoinTexture(textureSize, mainColor, edgeColor, highlightColor);
            coinSprite = Sprite.Create(
                texture, 
                new Rect(0, 0, texture.width, texture.height),
                new Vector2(0.5f, 0.5f),
                100f
            );
            
            // Establecer nombre para referencia
            coinSprite.name = "CoinSprite";
        }
        
        // Método estático para obtener el sprite de moneda
        public static Sprite GetCoinSprite()
        {
            if (coinSprite == null)
            {
                // Intentar cargar desde recursos
                coinSprite = Resources.Load<Sprite>("Sprites/Coin");
                
                // Si no se encuentra, crear uno
                if (coinSprite == null)
                {
                    Texture2D texture = CreateCoinTexture(64, 
                        new Color(1f, 0.84f, 0.0f),
                        new Color(0.8f, 0.6f, 0.0f),
                        new Color(1f, 0.95f, 0.7f));
                        
                    coinSprite = Sprite.Create(
                        texture, 
                        new Rect(0, 0, texture.width, texture.height),
                        new Vector2(0.5f, 0.5f),
                        100f
                    );
                    
                    coinSprite.name = "CoinSprite";
                }
            }
            
            return coinSprite;
        }
        
        // Método estático para crear una textura de moneda
        public static Texture2D CreateCoinTexture(int size, Color mainColor, Color edgeColor, Color highlightColor)
        {
            Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            texture.filterMode = FilterMode.Bilinear;
            
            Color transparent = new Color(0, 0, 0, 0);
            
            float centerX = size / 2f;
            float centerY = size / 2f;
            float radius = size / 2f - 1f;
            float innerRadius = radius * 0.85f;
            float borderWidth = radius * 0.15f;
            float highlightRadius = radius * 0.4f;
            float highlightOffsetX = radius * 0.15f;
            float highlightOffsetY = radius * 0.15f;
            
            // Generar textura circular
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float distanceFromCenter = Mathf.Sqrt((x - centerX) * (x - centerX) + (y - centerY) * (y - centerY));
                    
                    if (distanceFromCenter <= radius)
                    {
                        // Borde exterior
                        if (distanceFromCenter > radius - borderWidth)
                        {
                            float blend = (distanceFromCenter - (radius - borderWidth)) / borderWidth;
                            Color blendedColor = Color.Lerp(mainColor, edgeColor, blend);
                            texture.SetPixel(x, y, blendedColor);
                        }
                        // Interior de la moneda
                        else
                        {
                            // Añadir pequeño patrón de líneas (simulando grabado)
                            float angle = Mathf.Atan2(y - centerY, x - centerX);
                            float patternValue = Mathf.Sin(angle * 8) * 0.5f + 0.5f;
                            
                            // Oscurecer ligeramente hacia los bordes
                            float darkening = distanceFromCenter / innerRadius;
                            darkening = Mathf.Clamp01(darkening);
                            
                            // Aplicar color base con patrón
                            Color baseColor = Color.Lerp(mainColor, 
                                                        Color.Lerp(mainColor, edgeColor, 0.3f), 
                                                        patternValue * 0.2f * darkening);
                            
                            // Añadir brillo
                            float distanceFromHighlight = Mathf.Sqrt(
                                (x - (centerX - highlightOffsetX)) * (x - (centerX - highlightOffsetX)) + 
                                (y - (centerY - highlightOffsetY)) * (y - (centerY - highlightOffsetY))
                            );
                            
                            if (distanceFromHighlight < highlightRadius)
                            {
                                float highlightStrength = 1 - (distanceFromHighlight / highlightRadius);
                                highlightStrength = Mathf.Pow(highlightStrength, 2); // Exponencial para suavizar
                                
                                baseColor = Color.Lerp(baseColor, highlightColor, highlightStrength * 0.6f);
                            }
                            
                            texture.SetPixel(x, y, baseColor);
                        }
                    }
                    else
                    {
                        texture.SetPixel(x, y, transparent);
                    }
                }
            }
            
            texture.Apply();
            return texture;
        }
    }
} 