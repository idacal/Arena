using UnityEngine;
using Photon.Pun;

namespace Photon.Pun.Demo.Asteroids
{
    // Añade este componente a tu prefab de héroe para sincronizar las animaciones
    public class HeroAnimationSync : MonoBehaviourPun, IPunObservable
    {
        private Animator anim;
        private HeroBase heroBase;
        
        // Parámetros de animación que queremos sincronizar
        private readonly string[] syncedParameters = {
            "MoveSpeed",  // Para caminar/correr
            "Attack",     // Para ataques
            "Die",        // Para muerte
            "Respawn"     // Para respawn
        };
        
        private float lastMoveSpeed = 0f;
        private bool lastAttackState = false;
        
        void Awake()
        {
            anim = GetComponent<Animator>();
            if (anim == null)
            {
                anim = GetComponentInChildren<Animator>();
            }
            
            heroBase = GetComponent<HeroBase>();
        }
        
        void Start()
        {
            if (anim == null)
            {
                Debug.LogError("HeroAnimationSync: No se encontró un Animator para sincronizar");
                enabled = false;
                return;
            }
            
            Debug.Log($"HeroAnimationSync inicializado para {gameObject.name}. IsMine: {photonView.IsMine}");
        }
        
        void Update()
        {
            // Solo sincronizamos desde el dueño a los demás
            if (!photonView.IsMine) return;
            
            // Verificar si "Attack" cambió a true
            bool currentAttackState = IsAttackParameterActive();
            if (!lastAttackState && currentAttackState)
            {
                photonView.RPC("RPC_TriggerAnimation", RpcTarget.Others, "Attack");
            }
            lastAttackState = currentAttackState;
        }
        
        private bool IsAttackParameterActive()
        {
            if (anim == null) return false;
            
            // Buscar cualquier parámetro de ataque (algunos animators usan nombres diferentes)
            foreach (AnimatorControllerParameter param in anim.parameters)
            {
                if (param.type == AnimatorControllerParameterType.Trigger && 
                    param.name.Contains("Attack"))
                {
                    return anim.GetBool(param.name);
                }
            }
            
            return false;
        }
        
        public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
        {
            if (anim == null) return;
            
            if (stream.IsWriting)
            {
                // Enviar el valor de MoveSpeed
                float moveSpeed = anim.GetFloat("MoveSpeed");
                stream.SendNext(moveSpeed);
                
                // Enviar estado de vida
                stream.SendNext(heroBase.IsDead);
            }
            else
            {
                // Recibir y aplicar valores
                float moveSpeed = (float)stream.ReceiveNext();
                anim.SetFloat("MoveSpeed", moveSpeed);
                
                // Recibir estado de vida
                bool isDead = (bool)stream.ReceiveNext();
                if (isDead && !heroBase.IsDead)
                {
                    anim.SetTrigger("Die");
                }
            }
        }
        
        [PunRPC]
        private void RPC_TriggerAnimation(string paramName)
        {
            if (anim == null) return;
            
            Debug.Log($"RPC_TriggerAnimation: Activando {paramName} en {gameObject.name}");
            anim.SetTrigger(paramName);
        }
        
        // Método público para que otros sistemas disparen animaciones
        public void TriggerAnimationForAll(string paramName)
        {
            if (anim == null) return;
            
            // Activar localmente
            anim.SetTrigger(paramName);
            
            // Enviar a otros clientes
            if (photonView.IsMine)
            {
                photonView.RPC("RPC_TriggerAnimation", RpcTarget.Others, paramName);
            }
        }
    }
}