using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UI;
using Photon.Pun;
using Photon.Realtime;

namespace Photon.Pun.Demo.Asteroids
{
    [RequireComponent(typeof(HeroBase))]
    public class HeroAbilityController : MonoBehaviourPun, IPunObservable
    {
        [System.Serializable]
        public class AbilitySlot
        {
            public KeyCode hotkey = KeyCode.Q;  // Tecla predeterminada
            public HeroAbility abilityData;     // Datos de la habilidad
            public float cooldownRemaining;     // Tiempo restante del enfriamiento
            public Image cooldownImage;         // Imagen de UI para el enfriamiento
            public Text hotkeyText;             // Texto para mostrar la tecla
            public GameObject abilityPrefab;    // Prefab para instanciar efectos visuales
        }
        
        [Header("Ability Configuration")]
        public List<AbilitySlot> abilitySlots = new List<AbilitySlot>();
        
        [Header("UI References")]
        public GameAbilityUI gameAbilityUI;
        
        [Header("Debug")]
        public bool debugMode = false;
        
        // Referencias privadas
        private HeroBase heroBase;
        private Camera mainCamera;
        
        // Variable para controlar tiempo entre instanciaciones
        private float lastAbilityTime = 0f;
        
        void Awake()
        {
            // Obtener componentes
            heroBase = GetComponent<HeroBase>();
        }
        
        void Start()
        {
            // Solo controlar si es el jugador local
            if (!photonView.IsMine)
                return;
                
            // Obtener la cámara principal
            mainCamera = Camera.main;
            
            // Inicializar cooldowns
            foreach (AbilitySlot slot in abilitySlots)
            {
                slot.cooldownRemaining = 0f;
                
                // Actualizar textos de hotkeys si están configurados
                if (slot.hotkeyText != null && slot.abilityData != null)
                {
                    slot.hotkeyText.text = slot.abilityData.Hotkey;
                }
            }
            
            // Buscar la referencia a la UI de habilidades si no está asignada
            if (gameAbilityUI == null)
            {
                gameAbilityUI = GetComponentInChildren<GameAbilityUI>();
                
                if (gameAbilityUI == null)
                {
                    gameAbilityUI = FindObjectOfType<GameAbilityUI>();
                }
            }
        }
        
        void Update()
        {
            // Solo controlar si es el jugador local
            if (!photonView.IsMine)
                return;
                
            // No permitir usar habilidades si está muerto
            if (heroBase.IsDead)
                return;
                
            // Actualizar cooldowns
            UpdateCooldowns();
            
            // Verificar entradas para habilidades
            CheckAbilityInput();
        }
        
        /// <summary>
        /// Configura las habilidades del héroe según los datos proporcionados
        /// </summary>
        public void SetupAbilities(List<HeroAbility> abilities)
        {
            // Asegurarse de que tenemos suficientes slots
            while (abilitySlots.Count < abilities.Count)
            {
                abilitySlots.Add(new AbilitySlot());
            }
            
            // Obtener el nombre del héroe para búsqueda dinámica
            string heroName = "";
            if (heroBase != null)
            {
                heroName = heroBase.heroName;
            }
            
            // Configurar cada habilidad
            for (int i = 0; i < abilities.Count; i++)
            {
                abilitySlots[i].abilityData = abilities[i];
                
                // Asignar hotkey basada en el dato de la habilidad
                switch (abilities[i].Hotkey.ToUpper())
                {
                    case "Q": abilitySlots[i].hotkey = KeyCode.Q; break;
                    case "W": abilitySlots[i].hotkey = KeyCode.W; break;
                    case "E": abilitySlots[i].hotkey = KeyCode.E; break;
                    case "R": abilitySlots[i].hotkey = KeyCode.R; break;
                    case "D": abilitySlots[i].hotkey = KeyCode.D; break;
                    case "F": abilitySlots[i].hotkey = KeyCode.F; break;
                    default: abilitySlots[i].hotkey = KeyCode.Alpha1 + i; break;
                }
                
                // Actualizar texto de hotkey si está configurado
                if (abilitySlots[i].hotkeyText != null)
                {
                    abilitySlots[i].hotkeyText.text = abilities[i].Hotkey;
                }
                
                // Limpiar espacios y caracteres especiales del nombre para usarlo en la ruta
                string safeAbilityName = abilities[i].Name.Replace(" ", "").Replace(",", "").Replace(".", "");
                
                // PRIORIDAD 1: Buscar en la ruta específica del héroe
                if (!string.IsNullOrEmpty(heroName))
                {
                    // Limpiar espacios y caracteres especiales del nombre del héroe
                    string safeHeroName = heroName.Replace(" ", "").Replace(",", "").Replace(".", "");
                    
                    // Intentar primera con la estructura de carpetas con subcarpeta del héroe
                    string heroSpecificPath = $"Abilities/{heroName}/{safeAbilityName}";
                    GameObject abilityPrefab = Resources.Load<GameObject>(heroSpecificPath);
                    
                    if (abilityPrefab == null)
                    {
                        // Intentar con nombre limpio del héroe
                        heroSpecificPath = $"Abilities/{safeHeroName}/{safeAbilityName}";
                        abilityPrefab = Resources.Load<GameObject>(heroSpecificPath);
                    }
                    
                    if (abilityPrefab != null)
                    {
                        abilitySlots[i].abilityPrefab = abilityPrefab;
                        if (debugMode) {
                            Debug.Log($"Prefab cargado desde ruta específica del héroe: {heroSpecificPath}");
                        }
                        continue; // Prefab encontrado, continuar con la siguiente habilidad
                    }
                }
                
                // PRIORIDAD 2: Buscar usando PrefabName del SO (si existiera y se hubiera transferido)
                // Esto sería ideal si HeroAbility tuviera una propiedad PrefabName
                
                // PRIORIDAD 3: Buscar en la ruta genérica (como fallback)
                string genericPath = $"Abilities/{safeAbilityName}";
                GameObject genericPrefab = Resources.Load<GameObject>(genericPath);
                
                if (genericPrefab != null)
                {
                    abilitySlots[i].abilityPrefab = genericPrefab;
                    if (debugMode) {
                        Debug.Log($"Prefab cargado desde ruta genérica: {genericPath}");
                    }
                }
                else
                {
                    Debug.LogWarning($"No se encontró prefab para la habilidad {abilities[i].Name} en {genericPath}");
                    
                    // PRIORIDAD 4: Intentar rutas adicionales comunes
                    string[] alternativePaths = {
                        $"Abilities/Prefabs/{safeAbilityName}",
                        $"Prefabs/Abilities/{safeAbilityName}",
                        $"Resources/Abilities/{safeAbilityName}"
                    };
                    
                    foreach (string path in alternativePaths)
                    {
                        GameObject altPrefab = Resources.Load<GameObject>(path);
                        if (altPrefab != null)
                        {
                            abilitySlots[i].abilityPrefab = altPrefab;
                            if (debugMode) {
                                Debug.Log($"Prefab cargado desde ruta alternativa: {path}");
                            }
                            break;
                        }
                    }
                }
            }
            
            // Actualizar la UI si existe
            if (gameAbilityUI != null)
            {
                gameAbilityUI.RefreshAbilityUI();
            }
            else
            {
                // Buscar la UI como componente hermano o hijo
                gameAbilityUI = GetComponentInChildren<GameAbilityUI>();
                
                if (gameAbilityUI == null && transform.parent != null)
                {
                    gameAbilityUI = transform.parent.GetComponentInChildren<GameAbilityUI>();
                }
                
                if (gameAbilityUI != null)
                {
                    gameAbilityUI.RefreshAbilityUI();
                }
                else
                {
                    Debug.LogWarning("No se encontró GameAbilityUI al configurar habilidades");
                }
            }
        }
        
        /// <summary>
        /// Actualiza los cooldowns de las habilidades
        /// </summary>
        private void UpdateCooldowns()
        {
            for (int i = 0; i < abilitySlots.Count; i++)
            {
                if (abilitySlots[i].cooldownRemaining > 0)
                {
                    abilitySlots[i].cooldownRemaining -= Time.deltaTime;
                    
                    // Asegurarse de que no sea negativo
                    if (abilitySlots[i].cooldownRemaining < 0)
                        abilitySlots[i].cooldownRemaining = 0;
                    
                    // Actualizar imagen de cooldown si está configurada
                    if (abilitySlots[i].cooldownImage != null && abilitySlots[i].abilityData != null)
                    {
                        float cooldownRatio = abilitySlots[i].cooldownRemaining / abilitySlots[i].abilityData.Cooldown;
                        abilitySlots[i].cooldownImage.fillAmount = cooldownRatio;
                    }
                }
            }
        }
        
        /// <summary>
        /// Verifica la entrada para usar habilidades
        /// </summary>
        private void CheckAbilityInput()
        {
            for (int i = 0; i < abilitySlots.Count; i++)
            {
                // Verificar si se presionó la tecla correspondiente
                if (Input.GetKeyDown(abilitySlots[i].hotkey))
                {
                    UseAbility(i);
                }
            }
        }
        
        /// <summary>
        /// Usa la habilidad en el slot especificado
        /// </summary>
        private void UseAbility(int slotIndex)
        {
            if (slotIndex < 0 || slotIndex >= abilitySlots.Count)
                return;
                
            AbilitySlot slot = abilitySlots[slotIndex];
            
            // Verificar si está en cooldown
            if (slot.cooldownRemaining > 0)
                return;
                
            // Verificar si tenemos datos de habilidad
            if (slot.abilityData == null)
                return;
                
            // Verificar si tenemos suficiente maná
            if (!heroBase.ConsumeMana(slot.abilityData.ManaCost))
                return;
                
            // Aplicar cooldown
            slot.cooldownRemaining = slot.abilityData.Cooldown;
            
            // Actualizar imagen de cooldown
            if (slot.cooldownImage != null)
            {
                slot.cooldownImage.fillAmount = 1.0f;
            }
            
            // Lanzar la habilidad en el servidor
            photonView.RPC("RPC_UseAbility", RpcTarget.All, slotIndex, transform.position, transform.forward);
        }
        
        /// <summary>
        /// Método público para usar una habilidad desde un botón de UI
        /// </summary>
        public void OnAbilityButtonClicked(int slotIndex)
        {
            if (photonView.IsMine)
            {
                UseAbility(slotIndex);
            }
        }
        
        /// <summary>
        /// Implementación de IPunObservable
        /// </summary>
        public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
        {
            // Solo necesitamos sincronizar los cooldowns para jugadores remotos
            if (stream.IsWriting)
            {
                // Enviar el número de habilidades
                stream.SendNext(abilitySlots.Count);
                
                // Enviar los cooldowns de cada habilidad
                for (int i = 0; i < abilitySlots.Count; i++)
                {
                    stream.SendNext(abilitySlots[i].cooldownRemaining);
                }
            }
            else
            {
                // Recibir el número de habilidades
                int count = (int)stream.ReceiveNext();
                
                // Asegurar que tenemos suficientes slots
                while (abilitySlots.Count < count)
                {
                    abilitySlots.Add(new AbilitySlot());
                }
                
                // Recibir los cooldowns
                for (int i = 0; i < count; i++)
                {
                    if (i < abilitySlots.Count)
                    {
                        abilitySlots[i].cooldownRemaining = (float)stream.ReceiveNext();
                        
                        // Actualizar la UI si es necesario
                        if (abilitySlots[i].cooldownImage != null && abilitySlots[i].abilityData != null)
                        {
                            float cooldownRatio = abilitySlots[i].cooldownRemaining / abilitySlots[i].abilityData.Cooldown;
                            abilitySlots[i].cooldownImage.fillAmount = cooldownRatio;
                        }
                    }
                    else
                    {
                        // Ignorar datos extra si hay
                        stream.ReceiveNext();
                    }
                }
            }
        }
        
        /// <summary>
        /// Obtiene los datos de habilidad por índice
        /// </summary>
        public HeroAbility GetAbilityData(int index)
        {
            if (index >= 0 && index < abilitySlots.Count)
            {
                return abilitySlots[index].abilityData;
            }
            return null;
        }
        
        /// <summary>
        /// Obtiene el tiempo de cooldown restante por índice
        /// </summary>
        public float GetCooldownRemaining(int index)
        {
            if (index >= 0 && index < abilitySlots.Count)
            {
                return abilitySlots[index].cooldownRemaining;
            }
            return 0f;
        }
        
        #region PHOTON RPC
        
        [PunRPC]
        private void RPC_UseAbility(int slotIndex, Vector3 position, Vector3 direction, PhotonMessageInfo info)
        {
            if (debugMode) {
                Debug.Log($"RPC_UseAbility llamado para slot {slotIndex}. Soy dueño: {photonView.IsMine}");
            }
            
            if (slotIndex < 0 || slotIndex >= abilitySlots.Count)
                return;
                
            AbilitySlot slot = abilitySlots[slotIndex];
            
            // Verificar si tenemos datos de habilidad
            if (slot.abilityData == null)
            {
                Debug.LogWarning($"No hay datos de habilidad para el slot {slotIndex}");
                return;
            }
            
            // Instanciar prefab de habilidad si existe
            if (slot.abilityPrefab != null)
            {
                if (debugMode) {
                    Debug.Log($"Instanciando prefab para {slot.abilityData.Name}");
                }
                
                // Crear un GameObject para la habilidad
                GameObject abilityObj;
                
                // Si es el dueño, usar PhotonNetwork.Instantiate
                if (photonView.IsMine)
                {
                    // Obtener la ruta relativa al Resources
                    string prefabPath = GetPrefabPathInResources(slot.abilityPrefab);
                    
                    // Instantiate through PhotonNetwork
                    if (!string.IsNullOrEmpty(prefabPath))
                    {
                        if (debugMode) {
                            Debug.Log($"Instanciando vía Photon: {prefabPath}");
                        }
                        
                        // Añadir un tiempo aleatorio pequeño para evitar que dos clientes intenten
                        // crear el mismo objeto en el mismo frame
                        if (Random.value > 0.5f) {
                            // Añadir un pequeño retraso para evitar conflictos de red
                            StartCoroutine(DelayedInstantiate(prefabPath, position, Quaternion.LookRotation(direction), 
                                           slot.abilityData, info.Sender.ActorNumber));
                            return;
                        } 
                        else {
                            abilityObj = PhotonNetwork.Instantiate(prefabPath, position, Quaternion.LookRotation(direction));
                        }
                    }
                    else
                    {
                        Debug.LogWarning($"No se pudo obtener ruta en Resources para {slot.abilityData.Name}, usando Instantiate local");
                        abilityObj = Instantiate(slot.abilityPrefab, position, Quaternion.LookRotation(direction));
                    }
                }
                else
                {
                    // Si no somos el dueño y ha pasado tiempo suficiente desde la última habilidad,
                    // creamos una instancia local para visualización
                    if (Time.time - lastAbilityTime > 0.5f) {
                        abilityObj = Instantiate(slot.abilityPrefab, position, Quaternion.LookRotation(direction));
                        lastAbilityTime = Time.time;
                    } else {
                        // Ya se creó una recientemente, probablemente ya está en red
                        return;
                    }
                }
                
                // Configurar la habilidad
                AbilityBehaviour abilityBehaviour = abilityObj.GetComponent<AbilityBehaviour>();
                if (abilityBehaviour != null)
                {
                    abilityBehaviour.Initialize(
                        heroBase,                // Caster
                        slot.abilityData,        // Datos de habilidad
                        info.Sender.ActorNumber  // ID del jugador que la lanzó
                    );
                }
                else
                {
                    Debug.LogError($"El prefab de habilidad no tiene un componente AbilityBehaviour: {slot.abilityPrefab.name}");
                }
            }
            else
            {
                Debug.LogWarning($"No hay prefab asignado para la habilidad {slot.abilityData.Name}");
            }
            
            // Activar animación
            HeroMovementController moveController = heroBase.GetComponent<HeroMovementController>();
            if (moveController != null)
            {
                moveController.PlayAttackAnimation();
            }
            
            // Reproducir efectos de sonido si es el cliente local
            if (photonView.IsMine)
            {
                // Reproducir sonido de habilidad si está configurado
                if (slot.abilityData != null && slot.abilityData.AbilitySound != null)
                {
                    AudioSource audioSource = GetComponent<AudioSource>();
                    if (audioSource == null)
                    {
                        audioSource = gameObject.AddComponent<AudioSource>();
                    }
                    
                    audioSource.PlayOneShot(slot.abilityData.AbilitySound);
                }
            }
        }
        
        // Coroutine para instanciar con un pequeño retraso
        private System.Collections.IEnumerator DelayedInstantiate(string prefabPath, Vector3 position, 
                                                               Quaternion rotation, HeroAbility abilityData, 
                                                               int senderActorNumber)
        {
            // Esperamos un tiempo aleatorio muy corto
            yield return new WaitForSeconds(Random.Range(0.02f, 0.05f));
            
            // Instanciamos la habilidad
            GameObject abilityObj = PhotonNetwork.Instantiate(prefabPath, position, rotation);
            
            // Configurar la habilidad
            AbilityBehaviour abilityBehaviour = abilityObj.GetComponent<AbilityBehaviour>();
            if (abilityBehaviour != null)
            {
                abilityBehaviour.Initialize(
                    heroBase,        // Caster
                    abilityData,     // Datos de habilidad
                    senderActorNumber // ID del jugador que la lanzó
                );
            }
            
            lastAbilityTime = Time.time;
        }
        
        /// <summary>
        /// Obtiene la ruta relativa al directorio Resources para un prefab
        /// </summary>
        private string GetPrefabPathInResources(GameObject prefab)
        {
            if (prefab == null) return null;
            
            // Lista de rutas posibles para buscar
            string[] possiblePaths = new string[] {
                // Ruta específica del héroe
                $"Abilities/{heroBase?.heroName}/{prefab.name}",
                // Ruta con nombre sanitizado del héroe
                $"Abilities/{SanitizeName(heroBase?.heroName)}/{prefab.name}",
                // Ruta directa de habilidades
                $"Abilities/{prefab.name}",
                // Rutas alternativas comunes
                $"Abilities/Prefabs/{prefab.name}",
                $"Prefabs/Abilities/{prefab.name}",
                // Fallback directo
                prefab.name
            };
            
            // Probar cada ruta
            foreach (string path in possiblePaths)
            {
                GameObject test = Resources.Load<GameObject>(path);
                if (test != null)
                {
                    if (debugMode) {
                        Debug.Log($"Encontrada ruta válida: {path}");
                    }
                    return path;
                }
            }
            
            if (debugMode) {
                Debug.LogWarning($"No se pudo determinar la ruta en Resources para {prefab.name}. Rutas probadas: " + 
                                string.Join(", ", possiblePaths));
            }
            
            return null;
        }
        
        // Método auxiliar para limpiar nombres
        private string SanitizeName(string name)
        {
            if (string.IsNullOrEmpty(name)) return "";
            return name.Replace(" ", "").Replace(",", "").Replace(".", "");
        }
        
        #endregion
    }
}