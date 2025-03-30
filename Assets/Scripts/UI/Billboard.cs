using UnityEngine;

namespace Photon.Pun.Demo.Asteroids
{
    /// <summary>
    /// Hace que un objeto siempre mire hacia la cámara
    /// </summary>
    public class Billboard : MonoBehaviour
    {
        private Camera mainCamera;
        private bool isCameraFound = false;

        void Awake()
        {
            // Intentar encontrar la cámara de inmediato
            FindCamera();
        }
        
        void Start()
        {
            // Por si acaso no se encontró en Awake
            if (!isCameraFound)
            {
                FindCamera();
            }
        }

        void FindCamera()
        {
            mainCamera = Camera.main;
            
            if (mainCamera == null)
            {
                // Buscar la cámara por tipo
                mainCamera = FindObjectOfType<Camera>();
                
                if (mainCamera == null)
                {
                    Debug.LogWarning($"Billboard: No se pudo encontrar una cámara para {gameObject.name}");
                }
                else
                {
                    isCameraFound = true;
                }
            }
            else
            {
                isCameraFound = true;
            }
        }

        void LateUpdate()
        {
            if (mainCamera == null)
            {
                // Reintentar encontrar la cámara si se perdió por alguna razón
                FindCamera();
                if (!isCameraFound) return;
            }

            // Girar para mirar a la cámara - orientación completa
            transform.rotation = mainCamera.transform.rotation;
        }
    }
} 