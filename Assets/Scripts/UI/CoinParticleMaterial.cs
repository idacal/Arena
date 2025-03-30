using UnityEngine;

namespace Photon.Pun.Demo.Asteroids
{
    public class CoinParticleMaterial : MonoBehaviour
    {
        private static Material coinMaterial;
        
        // Método para obtener o crear el material de moneda
        public static Material GetCoinMaterial()
        {
            if (coinMaterial == null)
            {
                // Intentar encontrar el material en los recursos
                coinMaterial = Resources.Load<Material>("Materials/CoinParticle");
                
                // Si no existe, crear uno nuevo
                if (coinMaterial == null)
                {
                    coinMaterial = CreateCoinMaterial();
                }
            }
            
            return coinMaterial;
        }
        
        // Método para crear un material para partículas de moneda
        private static Material CreateCoinMaterial()
        {
            // Crear un material con shader de partículas
            Material material = new Material(Shader.Find("Particles/Standard Unlit"));
            
            // Configurar propiedades
            material.SetColor("_Color", new Color(1f, 0.84f, 0.0f, 1f)); // Color oro
            material.SetFloat("_Glossiness", 0.8f);
            material.SetFloat("_Metallic", 1.0f);
            
            // Crear una textura circular para simular monedas
            Texture2D coinTexture = CreateCoinTexture();
            material.mainTexture = coinTexture;
            
            return material;
        }
        
        // Método para generar una textura circular para las monedas
        private static Texture2D CreateCoinTexture()
        {
            int size = 64;
            Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            
            Color goldColor = new Color(1f, 0.84f, 0.0f, 1f);
            Color darkGoldColor = new Color(0.8f, 0.6f, 0.0f, 1f);
            Color transparent = new Color(0, 0, 0, 0);
            
            float centerX = size / 2f;
            float centerY = size / 2f;
            float radius = size / 2f - 1f;
            float innerRadius = radius * 0.8f;
            float border = radius * 0.1f;
            
            // Generar textura circular
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float distance = Mathf.Sqrt((x - centerX) * (x - centerX) + (y - centerY) * (y - centerY));
                    
                    if (distance < radius)
                    {
                        // Añadir borde
                        if (distance > radius - border)
                        {
                            texture.SetPixel(x, y, darkGoldColor);
                        }
                        // Añadir brillo en el centro
                        else if (distance < innerRadius)
                        {
                            // Calcular brillo basado en la distancia al centro
                            float brightness = 1f - (distance / innerRadius) * 0.5f;
                            Color brightGold = new Color(
                                goldColor.r * brightness + 0.2f,
                                goldColor.g * brightness + 0.1f,
                                goldColor.b * brightness,
                                1f
                            );
                            texture.SetPixel(x, y, brightGold);
                        }
                        else
                        {
                            texture.SetPixel(x, y, goldColor);
                        }
                    }
                    else
                    {
                        texture.SetPixel(x, y, transparent);
                    }
                }
            }
            
            // Aplicar los cambios
            texture.Apply();
            
            return texture;
        }
    }
} 