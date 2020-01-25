using UnityEngine;
using UnityEngine.Events;
using System.Collections;

namespace Invector.vCharacterController
{
    [System.Serializable]
    public class OnActionHandle : UnityEvent<Collider> { }
    [System.Serializable]

    [vClassHeader("vCharacter")]
    public class vCharacter : vHealthController, vICharacter
    {
        #region Character Variables 

        public enum DeathBy
        {
            Animation,
            AnimationWithRagdoll,
            Ragdoll
        }

        [vEditorToolbar("Health")]
        public DeathBy deathBy = DeathBy.Animation;
        public bool removeComponentsAfterDie;

        [vEditorToolbar("Debug", order = 9)]
        public bool debugActionListener;
        public Animator animator { get; private set; }
        public bool ragdolled { get; set; }

        [vEditorToolbar("Events")]

        public UnityEvent OnCrouch;
        public UnityEvent OnStandUp;

        [SerializeField] protected OnActiveRagdoll _onActiveRagdoll = new OnActiveRagdoll();
        public OnActiveRagdoll onActiveRagdoll { get { return _onActiveRagdoll; } protected set { _onActiveRagdoll = value; } }
        [Header("Check if Character is in Trigger with tag Action")]
        [HideInInspector]
        public OnActionHandle onActionEnter = new OnActionHandle();
        [HideInInspector]
        public OnActionHandle onActionStay = new OnActionHandle();
        [HideInInspector]
        public OnActionHandle onActionExit = new OnActionHandle();

        protected vAnimatorParameter hitDirectionHash;
        protected vAnimatorParameter reactionIDHash;
        protected vAnimatorParameter triggerReactionHash;
        protected vAnimatorParameter triggerResetStateHash;
        protected vAnimatorParameter recoilIDHash;
        protected vAnimatorParameter triggerRecoilHash;

        protected bool isInit;

        public virtual bool isCrouching
        {
            get
            {
                return _isCrouching;
            }
            set
            {
                if (value != _isCrouching)
                {
                    if (value)
                        OnCrouch.Invoke();
                    else
                        OnStandUp.Invoke();
                }
                _isCrouching = value;
            }
        }

        private bool _isCrouching;

        #endregion        

        public virtual void Init()
        {
            animator = GetComponent<Animator>();
            if (animator)
            {
                hitDirectionHash = new vAnimatorParameter(animator, "HitDirection");
                reactionIDHash = new vAnimatorParameter(animator, "ReactionID");
                triggerReactionHash = new vAnimatorParameter(animator, "TriggerReaction");
                triggerResetStateHash = new vAnimatorParameter(animator, "ResetState");
                recoilIDHash = new vAnimatorParameter(animator, "RecoilID");
                triggerRecoilHash = new vAnimatorParameter(animator, "TriggerRecoil");
            }

            this.LoadActionControllers(debugActionListener);
        }

        public virtual void ResetRagdoll()
        {

        }

        public virtual void EnableRagdoll()
        {

        }

        protected virtual void OnTriggerEnter(Collider other)
        {
            onActionEnter.Invoke(other);
        }

        protected virtual void OnTriggerStay(Collider other)
        {
            onActionStay.Invoke(other);
        }

        protected virtual void OnTriggerExit(Collider other)
        {
            onActionExit.Invoke(other);
        }

        public override void TakeDamage(vDamage damage)
        {
            base.TakeDamage(damage);
            TriggerDamageReaction(damage);
        }

        protected virtual void TriggerDamageReaction(vDamage damage)
        {
            if (animator != null && animator.enabled && !damage.activeRagdoll && currentHealth > 0)
            {
                if (hitDirectionHash.isValid && damage.sender) animator.SetInteger(hitDirectionHash, (int)transform.HitAngle(damage.sender.position));

                // trigger hitReaction animation
                if (damage.hitReaction)
                {
                    // set the ID of the reaction based on the attack animation state of the attacker - Check the MeleeAttackBehaviour script
                    if (reactionIDHash.isValid) animator.SetInteger(reactionIDHash, damage.reaction_id);
                    if (triggerReactionHash.isValid) SetTrigger(triggerReactionHash);
                    if (triggerResetStateHash.isValid) SetTrigger(triggerResetStateHash);
                }
                else
                {
                    if (recoilIDHash.isValid) animator.SetInteger(recoilIDHash, damage.recoil_id);
                    if (triggerRecoilHash.isValid) SetTrigger(triggerRecoilHash);
                    if (triggerResetStateHash.isValid) SetTrigger(triggerResetStateHash);
                }
            }
            if (damage.activeRagdoll)
                onActiveRagdoll.Invoke();
        }

        private IEnumerator SetTriggerRoutine(int trigger)
        {            
            animator.SetTrigger(trigger);
            yield return new WaitForSeconds(0.1f);
            animator.ResetTrigger(trigger);
        }

        public virtual void SetTrigger(int trigger)
        {
            StartCoroutine(SetTriggerRoutine(trigger));
        }
    }
}