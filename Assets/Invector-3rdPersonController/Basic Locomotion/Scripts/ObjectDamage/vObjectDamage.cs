using UnityEngine;
using System.Collections.Generic;

namespace Invector
{   [vClassHeader("OBJECT DAMAGE", iconName = "DamageIcon")]
    public class vObjectDamage : vMonoBehaviour
    {
        [System.Serializable]
        public class OnHitEvent : UnityEngine.Events.UnityEvent<Collider> { }
        public vDamage damage;       
        [Tooltip("Assign this to set other damage sender")]
        public Transform overrideDamageSender;
        [Tooltip("List of tags that can be hit")]
        public List<string> tags;        
        [Tooltip("Check to use the damage Frequence")]
        public bool continuousDamage;      
        [Tooltip("Apply damage to each end of the frequency in seconds ")]
        public float damageFrequency = 0.5f;
        private List<Collider> targets;
        private List<Collider> disabledTarget;
        private float currentTime;
        public OnHitEvent onHit;

        public enum CollisionMethod
        {
            OnTriggerEnter, 
            OnColliderEnter, 
            OnParticleCollision
        }

        public CollisionMethod collisionMethod = CollisionMethod.OnTriggerEnter;

        public ParticleSystem part;
        public List<ParticleCollisionEvent> collisionEvents;

        protected virtual void Start()
        {
            targets = new List<Collider>();
            disabledTarget = new List<Collider>();
            if(collisionMethod == CollisionMethod.OnParticleCollision)
            {
                part = GetComponent<ParticleSystem>();
                collisionEvents = new List<ParticleCollisionEvent>();
            }            
        }

        protected virtual void Update()
        {
            if (continuousDamage && targets != null && targets.Count > 0)
            {
                if (currentTime > 0)
                {
                    currentTime -= Time.deltaTime;
                }
                else
                {
                    currentTime = damageFrequency;
                    foreach (Collider collider in targets)
                        if (collider != null)
                        {
                            if (collider.enabled)
                            {
                                onHit.Invoke(collider);
                                ApplyDamage(collider.transform, transform.position); // apply damage to enabled collider
                            }                              
                            else
                                disabledTarget.Add(collider);// add disabled collider to list of disabled
                        }
                    //remove all disabled colliders of target list
                    if (disabledTarget.Count > 0)
                    {
                        for (int i = disabledTarget.Count; i >= 0; i--)
                        {
                            if (disabledTarget.Count == 0) break;
                            try
                            {
                                if (targets.Contains(disabledTarget[i]))
                                    targets.Remove(disabledTarget[i]);
                            }
                            catch
                            {
                                break;
                            }
                        }
                    }

                    if (disabledTarget.Count > 0) disabledTarget.Clear();
                }
            }
        }

        protected virtual void OnCollisionEnter(Collision hit)
        {
            if (collisionMethod != CollisionMethod.OnColliderEnter || continuousDamage) return;

            if (tags.Contains(hit.gameObject.tag))
            {
                ApplyDamage(hit.transform, hit.contacts[0].point);
            }               
        }

        protected virtual void OnTriggerEnter(Collider hit)
        {
            if (collisionMethod != CollisionMethod.OnTriggerEnter) return;
            if (continuousDamage && tags.Contains(hit.transform.tag) && !targets.Contains(hit))
            {
                targets.Add(hit);
            }
            else if (tags.Contains(hit.gameObject.tag))
            {
                onHit.Invoke(hit);
                ApplyDamage(hit.transform, transform.position);
            }                
        }

        protected virtual void OnTriggerExit(Collider hit)
        {
            if (collisionMethod == CollisionMethod.OnColliderEnter && !continuousDamage) return;

            if (tags.Contains(hit.gameObject.tag) && targets.Contains(hit))
            {
                targets.Remove(hit);
            }
        }

        protected virtual void OnParticleCollision(GameObject hit)
        {
            if (collisionMethod != CollisionMethod.OnParticleCollision) return;

            int numCollisionEvents =  part.GetCollisionEvents(hit, collisionEvents);

            Collider collider = hit.GetComponent<Collider>();
            int i = 0;

            while (i < numCollisionEvents)
            {
                if (collider)
                {                    
                    if (continuousDamage && tags.Contains(hit.transform.tag) && !targets.Contains(collider))
                    {
                        targets.Add(collider);
                    }
                    else if (tags.Contains(hit.gameObject.tag))
                    {
                        onHit.Invoke(collider);
                        ApplyDamage(hit.transform, transform.position);
                    }
                }               
                i++;
            }           
        }

        public virtual void ClearTargets()
        {                        
            targets.Clear();
        }

        protected virtual void ApplyDamage(Transform target, Vector3 hitPoint)
        {   
            damage.hitReaction = true;
            damage.sender = overrideDamageSender? overrideDamageSender: transform;
            damage.hitPosition = hitPoint;
            damage.receiver = target;
            
            target.gameObject.ApplyDamage( new vDamage(damage));
        }
    }
}