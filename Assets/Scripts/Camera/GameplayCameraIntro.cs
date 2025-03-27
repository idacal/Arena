using UnityEngine;
using System.Collections;
using Photon.Pun;
using Photon.Pun.Demo.Asteroids;  // Namespace correcto para PhotonMOBACamera

public class GameplayCameraIntro : MonoBehaviourPunCallbacks
{
    [Header("Intro Settings")]
    [Tooltip("Distancia inicial de la cámara")]
    public float startDistance = 50f;
    [Tooltip("Duración de la introducción en segundos")]
    public float introDuration = 3f;
    [Tooltip("Altura inicial de la cámara")]
    public float startHeight = 20f;
    [Tooltip("Curva de suavizado para el movimiento")]
    public AnimationCurve smoothCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    private Camera mainCamera;
    private PhotonMOBACamera cameraController;
    private Transform targetHero;
    private Vector3 startPosition;
    private Vector3 finalPosition;
    private bool introStarted = false;

    void Start()
    {
        Debug.Log("GameplayCameraIntro: Iniciando...");
        
        // Obtener referencias
        mainCamera = Camera.main;
        if (mainCamera == null)
        {
            Debug.LogError("GameplayCameraIntro: No se encontró la cámara principal!");
            Destroy(this);
            return;
        }

        cameraController = mainCamera.GetComponent<PhotonMOBACamera>();
        if (cameraController == null)
        {
            Debug.LogError("GameplayCameraIntro: No se encontró el PhotonMOBACamera!");
            Destroy(this);
            return;
        }

        // Desactivar temporalmente el PhotonMOBACamera
        if (cameraController != null)
        {
            cameraController.enabled = false;
            Debug.Log("GameplayCameraIntro: PhotonMOBACamera desactivado temporalmente");
        }

        // Iniciar la secuencia
        StartCoroutine(WaitForHeroAndStartIntro());
    }

    private Transform FindLocalHero()
    {
        PhotonView[] photonViews = FindObjectsOfType<PhotonView>();
        foreach (PhotonView pv in photonViews)
        {
            if (pv.IsMine && pv.gameObject.GetComponent<HeroBase>() != null)
            {
                Debug.Log($"GameplayCameraIntro: Héroe local encontrado - ViewID: {pv.ViewID}");
                return pv.transform;
            }
        }

        Debug.LogWarning("GameplayCameraIntro: No se encontró héroe local");
        return null;
    }

    private IEnumerator WaitForHeroAndStartIntro()
    {
        Debug.Log("GameplayCameraIntro: Esperando al héroe local...");
        float searchTime = 0f;
        float maxSearchTime = 10f;

        while (targetHero == null && searchTime < maxSearchTime)
        {
            targetHero = FindLocalHero();
            if (targetHero == null)
            {
                searchTime += 0.1f;
                yield return new WaitForSeconds(0.1f);
            }
        }

        if (targetHero == null)
        {
            Debug.LogError("GameplayCameraIntro: No se pudo encontrar el héroe local");
            EnableCameraControl();
            Destroy(this);
            yield break;
        }

        // Configurar posiciones inicial y final
        Vector3 heroPosition = targetHero.position;
        
        // Posición inicial (elevada y alejada)
        startPosition = heroPosition + Vector3.up * startHeight + Vector3.back * startDistance;
        
        // Posición final (usando los valores del PhotonMOBACamera)
        Vector3 cameraOffset = new Vector3(0, cameraController.cameraHeight, -cameraController.cameraDistance);
        finalPosition = heroPosition + cameraOffset;

        // Colocar la cámara en posición inicial
        mainCamera.transform.position = startPosition;
        mainCamera.transform.rotation = Quaternion.Euler(cameraController.cameraPitch, 0, 0);
        Debug.Log($"GameplayCameraIntro: Posición inicial configurada - {startPosition}");

        // Iniciar la animación
        StartCoroutine(PlayIntroAnimation());
    }

    private IEnumerator PlayIntroAnimation()
    {
        Debug.Log("GameplayCameraIntro: Iniciando animación...");
        float elapsedTime = 0f;

        while (elapsedTime < introDuration)
        {
            if (targetHero == null)
            {
                Debug.LogError("GameplayCameraIntro: Héroe perdido durante la animación");
                break;
            }

            float t = elapsedTime / introDuration;
            float smoothT = smoothCurve.Evaluate(t);

            // Actualizar posición de la cámara
            mainCamera.transform.position = Vector3.Lerp(startPosition, finalPosition, smoothT);
            mainCamera.transform.LookAt(targetHero.position);

            elapsedTime += Time.deltaTime;
            yield return null;
        }

        Debug.Log("GameplayCameraIntro: Animación completada");
        EnableCameraControl();
        Destroy(this);
    }

    private void EnableCameraControl()
    {
        if (cameraController != null)
        {
            cameraController.enabled = true;
            Debug.Log("GameplayCameraIntro: PhotonMOBACamera reactivado");
        }
    }

    public override void OnDisable()
    {
        EnableCameraControl();
        base.OnDisable();
    }
} 