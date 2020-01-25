using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.Events;

namespace Invector
{
    [vClassHeader("HITDAMAGE PARTICLE", "Default hit Particle to instantiate every time you receive damage and Custom hit Particle to instantiate based on a custom DamageType that comes from the MeleeControl Behaviour (AnimatorController)")]
    public class vHitDamageParticle : vMonoBehaviour
    {
        public GameObject defaultDamageEffect;
        public List<vDamageEffect> customDamageEffects = new List<vDamageEffect>();

        IEnumerator Start()
        {
            yield return new WaitForEndOfFrame();
            var healthController = GetComponent<vHealthController>();
            if (healthController != null)
            {
                healthController.onReceiveDamage.AddListener(OnReceiveDamage);
            }
        }

        public void OnReceiveDamage(vDamage damage)
        {
            // instantiate the hitDamage particle - check if your character has a HitDamageParticle component
            var damageDirection = damage.hitPosition - new Vector3(transform.position.x, damage.hitPosition.y, transform.position.z);
            var hitrotation = damageDirection != Vector3.zero ? Quaternion.LookRotation(damageDirection) : transform.rotation;

            if (damage.damageValue > 0)
                TriggerEffect(new vDamageEffectInfo(new Vector3(transform.position.x, damage.hitPosition.y, transform.position.z), hitrotation, damage.damageType, damage.receiver));
        }

        /// <summary>
        /// Raises the hit event.
        /// </summary>
        /// <param name="damageEffectInfo">Hit effect info.</param>
        void TriggerEffect(vDamageEffectInfo damageEffectInfo)
        {
            var damageEffect = customDamageEffects.Find(effect => effect.damageType.Equals(damageEffectInfo.damageType));

            if (damageEffect != null)
            {
                damageEffect.onTriggerEffect.Invoke();
                if (damageEffect.effectPrefab != null)
                {
                    Instantiate(damageEffect.effectPrefab, damageEffectInfo.position,
                        damageEffect.rotateToHitDirection ? damageEffectInfo.rotation : damageEffect.effectPrefab.transform.rotation,
                        damageEffect.attachInReceiver && damageEffectInfo.receiver ? damageEffectInfo.receiver : vObjectContainer.root);
                }
            }
            else if (defaultDamageEffect != null)
            {
                Instantiate(defaultDamageEffect, damageEffectInfo.position, damageEffectInfo.rotation, vObjectContainer.root);
            }
        }
    }

    public class vDamageEffectInfo
    {
        public Transform receiver;
        public Vector3 position;
        public Quaternion rotation;
        public string damageType;

        public vDamageEffectInfo(Vector3 position, Quaternion rotation, string damageType = "", Transform receiver = null)
        {
            this.receiver = receiver;
            this.position = position;
            this.rotation = rotation;
            this.damageType = damageType;
        }
    }

    [System.Serializable]
    public class vDamageEffect
    {
        public string damageType = "";
        public GameObject effectPrefab;
        public bool rotateToHitDirection = true;
        [Tooltip("Attach prefab in Damage Receiver transform")]
        public bool attachInReceiver = false;
        public UnityEvent onTriggerEffect;
    }
}
