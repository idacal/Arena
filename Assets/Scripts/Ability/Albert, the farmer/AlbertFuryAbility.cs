using UnityEngine;
using Photon.Pun;

namespace Photon.Pun.Demo.Asteroids
{
    public class AlbertFuryAbility : BuffAbility
    {
        [Header("Attack Speed Buff")]
        [SerializeField] private BuffType attackSpeedType = BuffType.AttackSpeed;
        [SerializeField] private float attackSpeedValue = 100f;
        [SerializeField] private bool attackSpeedIsPercentage = true;
        [SerializeField] private float attackSpeedDuration = 6f;
        [SerializeField] private bool attackSpeedApplyToAllies = false;
        [SerializeField] private bool attackSpeedApplyToSelf = true;
        [SerializeField] private float attackSpeedRadius = 0f;
        [SerializeField] private LayerMask attackSpeedTargetLayers;

        [Header("Movement Speed Buff")]
        [SerializeField] private BuffType moveSpeedType = BuffType.MoveSpeed;
        [SerializeField] private float moveSpeedValue = 30f;
        [SerializeField] private bool moveSpeedIsPercentage = true;
        [SerializeField] private float moveSpeedDuration = 6f;
        [SerializeField] private bool moveSpeedApplyToAllies = false;
        [SerializeField] private bool moveSpeedApplyToSelf = true;
        [SerializeField] private float moveSpeedRadius = 0f;
        [SerializeField] private LayerMask moveSpeedTargetLayers;
        
        private GameObject furyEffect;
        
        protected override void OnAbilityInitialized()
        {
            if (caster == null) return;
            
            // Aplicar buff de velocidad de ataque
            buffType = attackSpeedType;
            buffValue = attackSpeedValue;
            isPercentage = attackSpeedIsPercentage;
            duration = attackSpeedDuration;
            applyToAllies = attackSpeedApplyToAllies;
            applyToSelf = attackSpeedApplyToSelf;
            radius = attackSpeedRadius;
            targetLayers = attackSpeedTargetLayers;
            
            base.OnAbilityInitialized();
            
            // Crear una nueva instancia de BuffAbility para el movimiento
            GameObject moveSpeedBuff = new GameObject("MoveSpeedBuff");
            moveSpeedBuff.transform.position = transform.position;
            
            BuffAbility moveSpeedAbility = moveSpeedBuff.AddComponent<BuffAbility>();
            moveSpeedAbility.buffType = moveSpeedType;
            moveSpeedAbility.buffValue = moveSpeedValue;
            moveSpeedAbility.isPercentage = moveSpeedIsPercentage;
            moveSpeedAbility.duration = moveSpeedDuration;
            moveSpeedAbility.applyToAllies = moveSpeedApplyToAllies;
            moveSpeedAbility.applyToSelf = moveSpeedApplyToSelf;
            moveSpeedAbility.radius = moveSpeedRadius;
            moveSpeedAbility.targetLayers = moveSpeedTargetLayers;
            
            // Inicializar el buff de movimiento
            moveSpeedAbility.Initialize(caster, abilityData, casterId);
            
            // Crear efecto visual
            if (photonView.IsMine)
            {
                photonView.RPC("RPC_CreateFuryEffect", RpcTarget.AllBuffered);
            }
        }
        
        [PunRPC]
        private void RPC_CreateFuryEffect()
        {
            if (caster == null || furyEffect != null) return;
            
            furyEffect = new GameObject("FuryEffect");
            furyEffect.transform.SetParent(caster.transform);
            furyEffect.transform.localPosition = Vector3.up;
            
            var particles = furyEffect.AddComponent<ParticleSystem>();
            var main = particles.main;
            main.startColor = new Color(1f, 0.5f, 0f, 0.7f);
            main.startLifetime = 1f;
            particles.Play();
        }
        
        protected override void DestroyAbility()
        {
            if (photonView.IsMine)
            {
                photonView.RPC("RPC_DestroyFuryEffect", RpcTarget.AllBuffered);
            }
            base.DestroyAbility();
        }
        
        [PunRPC]
        private void RPC_DestroyFuryEffect()
        {
            if (furyEffect != null)
            {
                Destroy(furyEffect);
                furyEffect = null;
            }
        }
        
        private void OnDestroy()
        {
            if (furyEffect != null)
            {
                Destroy(furyEffect);
                furyEffect = null;
            }
        }
    }
}