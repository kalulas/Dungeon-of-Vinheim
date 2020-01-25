using UnityEngine;

namespace Invector.vCharacterController
{
    using vEventSystems;
    using vMelee;

    // here you can modify the Melee Combat inputs
    // if you want to modify the Basic Locomotion inputs, go to the vThirdPersonInput
    [vClassHeader("MELEE INPUT MANAGER", iconName = "inputIcon")]
    public class vMeleeCombatInput : vThirdPersonInput, vIMeleeFighter
    {
        #region Variables                

        [vEditorToolbar("Inputs")]
        [Header("Melee Inputs")]
        public GenericInput weakAttackInput = new GenericInput("Mouse0", "RB", "RB");
        public GenericInput strongAttackInput = new GenericInput("Alpha1", false, "RT", true, "RT", false);
        public GenericInput blockInput = new GenericInput("Mouse1", "LB", "LB");

        internal vMeleeManager meleeManager;
        public bool isAttacking { get; protected set; }
        public bool isBlocking { get; protected set; }
        public bool isArmed { get { return meleeManager != null && (meleeManager.rightWeapon != null || (meleeManager.leftWeapon != null && meleeManager.leftWeapon.meleeType != vMeleeType.OnlyDefense)); } }
        public bool isEquipping { get; protected set; }

        [HideInInspector]
        public bool lockMeleeInput;

        public void SetLockMeleeInput(bool value)
        {
            lockMeleeInput = value;

            if (value)
            {
                isAttacking = false;
                isBlocking = false;
                cc.isStrafing = false;
            }
        }

        public override void SetLockAllInput(bool value)
        {
            base.SetLockAllInput(value);
            SetLockMeleeInput(value);
        }

        #endregion

        public virtual bool lockInventory
        {
            get
            {
                return isAttacking || cc.isDead;
            }
        }

        protected override void FixedUpdate()
        {
            UpdateMeleeAnimations();
            base.FixedUpdate();
        }

        protected override void InputHandle()
        {
            if (cc == null)
                return;

            base.InputHandle();

            if (MeleeAttackConditions() && !lockMeleeInput)
            {
                MeleeWeakAttackInput();
                MeleeStrongAttackInput();
                BlockingInput();
            }
            else
            {
                ResetAttackTriggers();
                isBlocking = false;
            }            
        }

        #region MeleeCombat Input Methods

        /// <summary>
        /// WEAK ATK INPUT
        /// </summary>
        public virtual void MeleeWeakAttackInput()
        {
            if (cc.animator == null) return;

            if (weakAttackInput.GetButtonDown() && MeleeAttackStaminaConditions())
            {
                TriggerWeakAttack();
            }
        }

        public virtual void TriggerWeakAttack()
        {                
            cc.animator.SetInteger(vAnimatorParameters.AttackID, meleeManager.GetAttackID());
            cc.animator.SetTrigger(vAnimatorParameters.WeakAttack);
        }

        /// <summary>
        /// STRONG ATK INPUT
        /// </summary>
        public virtual void MeleeStrongAttackInput()
        {
            if (cc.animator == null) return;

            if (strongAttackInput.GetButtonDown() && (!meleeManager.CurrentActiveAttackWeapon || meleeManager.CurrentActiveAttackWeapon.useStrongAttack) && MeleeAttackStaminaConditions())
            {
                TriggerStrongAttack();
            }
        }

        public virtual void TriggerStrongAttack()
        {
            cc.animator.SetInteger(vAnimatorParameters.AttackID, meleeManager.GetAttackID());
            cc.animator.SetTrigger(vAnimatorParameters.StrongAttack);
        }

        /// <summary>
        /// BLOCK INPUT
        /// </summary>
        public virtual void BlockingInput()
        {
            if (cc.animator == null) return;

            isBlocking = blockInput.GetButton() && cc.currentStamina > 0 && !cc.customAction && !isAttacking;
        }

        #endregion

        #region Conditions

        protected virtual bool MeleeAttackStaminaConditions()
        {
            var result = cc.currentStamina - meleeManager.GetAttackStaminaCost();
            return result >= 0;
        }

        protected virtual bool MeleeAttackConditions()
        {           
            if (meleeManager == null) meleeManager = GetComponent<vMeleeManager>();
            return meleeManager != null && cc.isGrounded && !cc.customAction && !cc.isJumping && !cc.isCrouching && !cc.isRolling && !isEquipping;           
        }

        protected override bool JumpConditions()
        {
            return !isAttacking && !cc.customAction && !cc.isCrouching && cc.isGrounded && cc.GroundAngle() < cc.slopeLimit && cc.currentStamina >= cc.jumpStamina && !cc.isJumping;
        }

        protected override bool RollConditions()
        {
            return (!cc.isRolling || cc.canRollAgain) && cc.input != Vector3.zero && !cc.customAction && cc.isGrounded && cc.currentStamina > cc.rollStamina && !cc.isJumping && !isAttacking;
        }

        #endregion

        #region Update Animations        

        protected virtual void UpdateMeleeAnimations()
        {
            if (cc.animator == null || meleeManager == null) return;

            cc.animator.SetInteger(vAnimatorParameters.AttackID, meleeManager.GetAttackID());
            cc.animator.SetInteger(vAnimatorParameters.DefenseID, meleeManager.GetDefenseID());
            cc.animator.SetBool(vAnimatorParameters.IsBlocking, isBlocking);
            cc.animator.SetFloat(vAnimatorParameters.MoveSet_ID, meleeManager.GetMoveSetID(), .2f, Time.deltaTime);
            isEquipping = cc.IsAnimatorTag("IsEquipping");
        }

        public virtual void ResetMeleeAnimations()
        {
            if (meleeManager == null || !animator) return;
            cc.animator.SetInteger(vAnimatorParameters.AttackID, 0);
            cc.animator.SetInteger(vAnimatorParameters.DefenseID, 0);
            cc.animator.SetBool(vAnimatorParameters.IsBlocking, false);
            cc.animator.SetFloat(vAnimatorParameters.MoveSet_ID, 0f, .2f, Time.deltaTime);
        }

        #endregion

        #region Melee Methods

        public virtual void OnEnableAttack()
        {
            if (meleeManager == null) meleeManager = GetComponent<vMeleeManager>();
            if (meleeManager == null) return;

            cc.currentStaminaRecoveryDelay = meleeManager.GetAttackStaminaRecoveryDelay();
            cc.currentStamina -= meleeManager.GetAttackStaminaCost();
            isAttacking = true;
        }

        public virtual void OnDisableAttack()
        {
            isAttacking = false;
        }

        public virtual void ResetAttackTriggers()
        {
            cc.animator.ResetTrigger(vAnimatorParameters.WeakAttack);
            cc.animator.ResetTrigger(vAnimatorParameters.StrongAttack);
        }

        public virtual void BreakAttack(int breakAtkID)
        {
            ResetAttackTriggers();
            OnRecoil(breakAtkID);
        }

        public virtual void OnRecoil(int recoilID)
        {
            cc.animator.SetInteger(vAnimatorParameters.RecoilID, recoilID);
            cc.animator.SetTrigger(vAnimatorParameters.TriggerRecoil);
            cc.animator.SetTrigger(vAnimatorParameters.ResetState);
            cc.animator.ResetTrigger(vAnimatorParameters.WeakAttack);
            cc.animator.ResetTrigger(vAnimatorParameters.StrongAttack);
        }

        public virtual void OnReceiveAttack(vDamage damage, vIMeleeFighter attacker)
        {
            // character is blocking
            if (!damage.ignoreDefense && isBlocking && meleeManager != null && meleeManager.CanBlockAttack(damage.sender.position))
            {
                var damageReduction = meleeManager.GetDefenseRate();
                if (damageReduction > 0)
                    damage.ReduceDamage(damageReduction);
                if (attacker != null && meleeManager != null && meleeManager.CanBreakAttack())
                    attacker.BreakAttack(meleeManager.GetDefenseRecoilID());
                meleeManager.OnDefense();
                cc.currentStaminaRecoveryDelay = damage.staminaRecoveryDelay;
                cc.currentStamina -= damage.staminaBlockCost;
            }
            // apply damage
            damage.hitReaction = !isBlocking;
            cc.TakeDamage(damage);
        }

        public virtual vICharacter character
        {
            get { return cc; }
        }

        #endregion

    }

    public static partial class vAnimatorParameters
    {
        public static int AttackID = Animator.StringToHash("AttackID");
        public static int DefenseID = Animator.StringToHash("DefenseID");
        public static int IsBlocking = Animator.StringToHash("IsBlocking");
        public static int MoveSet_ID = Animator.StringToHash("MoveSet_ID");
        public static int RecoilID = Animator.StringToHash("RecoilID");
        public static int TriggerRecoil = Animator.StringToHash("TriggerRecoil");
        public static int WeakAttack = Animator.StringToHash("WeakAttack");
        public static int StrongAttack = Animator.StringToHash("StrongAttack");
    }
}