using System.Collections;
using UnityEngine;
using UnityEngine.Events;

namespace Invector.vCharacterController
{
    public class vThirdPersonMotor : vCharacter
    {
        #region Variables               

        #region Stamina       

        [vEditorToolbar("Stamina", order = 2)]
        public float maxStamina = 200f;
        public float staminaRecovery = 1.2f;
        internal float currentStamina;
        internal float currentStaminaRecoveryDelay;
        public float sprintStamina = 30f;
        public float jumpStamina = 30f;
        public float rollStamina = 25f;

        [vEditorToolbar("Events", order = 7)]
        public UnityEvent OnJump;
        public UnityEvent OnStartSprinting;
        public UnityEvent OnFinishSprinting;
        public UnityEvent OnFinishSprintingByStamina;
        public UnityEvent OnStaminaEnd;

        #endregion

        #region Layers
        [vEditorToolbar("Layers", order = 3)]
        [Tooltip("Layers that the character can walk on")]
        public LayerMask groundLayer = 1 << 0;
        [Tooltip("What objects can make the character auto crouch")]
        public LayerMask autoCrouchLayer = 1 << 0;
        [Tooltip("[SPHERECAST] ADJUST IN PLAY MODE - White Spherecast put just above the head, this will make the character Auto-Crouch if something hit the sphere.")]
        public float headDetect = 0.95f;
        [Tooltip("Select the layers the your character will stop moving when close to")]
        public LayerMask stopMoveLayer;
        [Tooltip("[RAYCAST] Stopmove Raycast Height")]
        public float stopMoveHeight = 0.65f;
        [Tooltip("[RAYCAST] Stopmove Raycast Distance")]
        public float stopMoveDistance = 0.1f;
        #endregion

        #region Character Variables       

        [vEditorToolbar("Locomotion", order = 0)]
        [Tooltip("Turn off if you have 'in place' animations and use this values above to move the character, or use with root motion as extra speed")]
        [vHelpBox("When 'Use RootMotion' is checked, make sure to reset all speeds to zero to use the original root motion velocity.")]
        public bool useRootMotion = false;
        public enum LocomotionType
        {
            FreeWithStrafe,
            OnlyStrafe,
            OnlyFree,
        }
        public LocomotionType locomotionType = LocomotionType.FreeWithStrafe;

        public bool disableAnimations;
        [Tooltip("While in Free Locomotion the character will lean to left/right when making turns")]
        public bool useLeanMovement = true;
        public vMovementSpeed freeSpeed, strafeSpeed;
        [Tooltip("Use this to rotate the character using the World axis, or false to use the camera axis - CHECK for Isometric Camera")]
        public bool rotateByWorld = false;
        [Tooltip("Check this to use the TurnOnSpot animations, you also need to check the option 'RotateWithCamera' in the strafe speed options")]
        public bool turnOnSpotAnim = false;
        [Tooltip("Put your Random Idle animations at the AnimatorController and select a value to randomize, 0 is disable.")]
        public float randomIdleTime = 0f;
        [Tooltip("Check This to use sprint on press button to your Character run until the stamina finish or movement stops\nIf uncheck your Character will sprint as long as the SprintInput is pressed or the stamina finishes")]
        public bool useContinuousSprint = true;
        [Tooltip("Check this to sprint always in free movement")]
        public bool sprintOnlyFree = true;
        [Range(1, 2.5f)]
        public float crouchHeightReduction = 1.5f;

        [vEditorToolbar("Jump / Airborne", order = 3)]

        [Header("Jump")]
        [Tooltip("Use the currently Rigidbody Velocity to influence on the Jump Distance")]
        public bool jumpWithRigidbodyForce = false;
        [Tooltip("Rotate or not while airborne")]
        public bool jumpAndRotate = true;
        [Tooltip("How much time the character will be jumping")]
        public float jumpTimer = 0.3f;
        internal float jumpCounter;
        [Tooltip("Add Extra jump height, if you want to jump only with Root Motion leave the value with 0.")]
        public float jumpHeight = 4f;

        [Header("Falling")]

        [Tooltip("Speed that the character will move while airborne")]
        public float airSpeed = 5f;
        [Tooltip("Smoothness of the direction while airborne")]
        public float airSmooth = 6f;
        [Tooltip("Apply extra gravity when the character is not grounded")]
        public float extraGravity = -10f;
        [Tooltip("Limit of the vertival velocity when Falling")]
        public float limitFallVelocity = -15f;
        [Tooltip("Turn the Ragdoll On when falling at high speed (check VerticalVelocity) - leave the value with 0 if you don't want this feature")]
        public float ragdollVelocity = -15f;
        [Header("Fall Damage")]
        public float fallMinHeight = 6f;
        public float fallMinVerticalVelocity = -10f;
        public float fallDamage = 10f;

        [vEditorToolbar("Roll", order = 4)]
        public bool useRollRootMotion = true;
        [Tooltip("Can control the Roll Direction")]
        public bool rollControl = true;
        [Tooltip("Speed of the Roll Movement")]
        public float rollSpeed = 0f;
        [Tooltip("Speed of the Roll Rotation")]
        public float rollRotationSpeed = 10f;
        [vHideInInspector("Roll use gravity inflence")]
        public bool rollUseGravity = true;
        [vHideInInspector("rollUseGravity")]
        [Tooltip("Normalized Time of the roll animation to enable gravity influence")]
        public float rollUseGravityTime = 0.2f;
        [Tooltip("Use the normalized time of the animation to know when you can roll again")]
        [Range(0, 1)]
        public float timeToRollAgain = 0.75f;

        public enum GroundCheckMethod
        {
            Low, High
        }
        [vEditorToolbar("Grounded", order = 3)]

        [Header("Ground")]

        [Tooltip("Ground Check Method To check ground Distance and ground angle\n*Simple: Use just a single Raycast\n*Normal: Use Raycast and SphereCast\n*Complex: Use SphereCastAll")]
        public GroundCheckMethod groundCheckMethod = GroundCheckMethod.High;
        [Tooltip("Snaps the capsule collider to the ground surface, recommend when using complex terrains or inclined ramps")]
        public bool useSnapGround = true;
        [Tooltip("Distance to became not grounded")]
        public float groundMinDistance = 0.25f;
        public float groundMaxDistance = 0.5f;
        [Tooltip("Max angle to walk")]
        [Range(30, 80)]
        public float slopeLimit = 75f;

        [Header("Slide Slopes")]

        [Tooltip("Velocity to slide down when on a slope limit ramp")]
        [Range(0, 30)]
        public float slideDownVelocity = 7f;
        [Tooltip("Velocity to slide sideways when on a slope limit ramp")]
        [Range(0, 15)]
        public float slideSidewaysVelocity = 5f;
        [Range(0.1f, 1f)]
        [Tooltip("Delay to start sliding once the character is standing on a slope")]
        public float slidingEnterTime = 0.1f;
        internal float _slidingEnterTime;

        [Header("Step Offset")]

        [Tooltip("Offset max height to walk on steps - YELLOW Raycast in front of the legs")]
        [Range(0, 1)]
        public float stepOffsetMaxHeight = 0.5f;
        [Tooltip("Offset min height to walk on steps. Make sure to keep slight above the floor - YELLOW Raycast in front of the legs")]
        [Range(0, 1)]
        public float stepOffsetMinHeight = 0f;
        [Tooltip("Offset distance to walk on steps - YELLOW Raycast in front of the legs")]
        [Range(0, 1)]
        public float stepOffsetDistance = 0.1f;

        protected float groundDistance;
        public RaycastHit groundHit;

        [vEditorToolbar("Debug", order = 9)]
        [Header("--- Debug Info ---")]
        public bool debugWindow;

        #endregion

        #region Actions

        public bool isStrafing
        {
            get
            {
                return _isStrafing || lockInStrafe;
            }
            set
            {
                _isStrafing = value;
            }
        }

        // movement bools
        public bool isGrounded { get; set; }
        /// <summary>
        /// use to stop update the Check Ground method and return true for IsGrounded
        /// </summary>
        public bool disableCheckGround { get; set; }
        public bool inCrouchArea { get; protected set; }
        public bool isSprinting { get; set; }
        public bool isSliding { get; protected set; }
        public bool stopMove { get; protected set; }
        public bool autoCrouch { get; protected set; }

        // action bools
        internal bool
            isRolling,
            isJumping,
            isInAirborne,
            isTurningOnSpot;

        internal bool customAction;

        protected void RemoveComponents()
        {
            if (!removeComponentsAfterDie) return;
            if (_capsuleCollider != null) Destroy(_capsuleCollider);
            if (_rigidbody != null) Destroy(_rigidbody);
            if (animator != null) Destroy(animator);
            var comps = GetComponents<MonoBehaviour>();
            for (int i = 0; i < comps.Length; i++)
            {
                Destroy(comps[i]);
            }
        }

        #endregion      

        #region Components

        internal Rigidbody _rigidbody;                                                      // access the Rigidbody component
        internal PhysicMaterial frictionPhysics, maxFrictionPhysics, slippyPhysics;         // create PhysicMaterial for the Rigidbody
        internal CapsuleCollider _capsuleCollider;                                          // access CapsuleCollider information

        #endregion

        #region Hide Variables

        internal float inputMagnitude;                      // sets the inputMagnitude to update the animations in the animator controller
        internal float verticalSpeed;                       // set the verticalSpeed based on the verticalInput
        internal float horizontalSpeed;                     // set the horizontalSpeed based on the horizontalInput       
        internal float moveSpeed;                           // set the current moveSpeed for the MoveCharacter method
        internal float verticalVelocity;                    // set the vertical velocity of the rigidbody        
        internal float colliderRadius, colliderHeight;      // storage capsule collider extra information                       
        internal float jumpMultiplier = 1;                  // internally used to set the jumpMultiplier
        internal float timeToResetJumpMultiplier;           // internally used to reset the jump multiplier
        internal float heightReached;                       // max height that character reached in air;
        internal bool lockMovement = false;                 // lock the movement of the controller (not the animation)
        internal bool lockRotation = false;                 // lock the rotation of the controller (not the animation)
        internal bool lockSetMoveSpeed = false;             // locks the method to update the moveset based on the locomotion type, so you can modify externally
        internal bool _isStrafing;                          // internally used to set the strafe movement
        internal bool lockInStrafe;                         // locks the controller to only used the strafe locomotion type        
        internal bool forceRootMotion = false;              // force the controller to use root motion
        internal bool keepDirection;                        // keeps the character direction even if the camera direction changes
        internal bool finishStaminaOnSprint;                // used to trigger the OnFinishStamina event
        [HideInInspector] public bool applyingStepOffset;   // internally used to apply the StepOffset       
        protected internal bool lockAnimMovement;           // internaly used with the vAnimatorTag("LockMovement"), use on the animator to lock the movement of a specific animation clip        
        protected internal bool lockAnimRotation;           // internaly used with the vAnimatorTag("LockRotation"), use on the animator to lock a rotation of a specific animation clip
      
        internal Transform rotateTarget;
        internal Vector3 input;                              // generate raw input for the controller
        internal Vector3 oldInput;                           // used internally to identify oldinput from the current input
        internal Vector3 colliderCenter;                     // storage the center of the capsule collider info                
        [HideInInspector] public Vector3 inputSmooth;        // generate smooth input based on the inputSmooth value       
        [HideInInspector] public Vector3 moveDirection;
        internal Quaternion targetRotation = Quaternion.identity;
        internal Quaternion rotationDirection = Quaternion.identity;
        public RaycastHit stepOffsetHit;
        internal AnimatorStateInfo baseLayerInfo, underBodyInfo, rightArmInfo, leftArmInfo, fullBodyInfo, upperBodyInfo;
        internal bool blockFallActions { get { return jumpMultiplier > 1; } }
        #endregion

        #endregion

        public override void Init()
        {
            base.Init();

            animator.updateMode = AnimatorUpdateMode.AnimatePhysics;
            // slides the character through walls and edges
            frictionPhysics = new PhysicMaterial();
            frictionPhysics.name = "frictionPhysics";
            frictionPhysics.staticFriction = .25f;
            frictionPhysics.dynamicFriction = .25f;
            frictionPhysics.frictionCombine = PhysicMaterialCombine.Multiply;

            // prevents the collider from slipping on ramps
            maxFrictionPhysics = new PhysicMaterial();
            maxFrictionPhysics.name = "maxFrictionPhysics";
            maxFrictionPhysics.staticFriction = 1f;
            maxFrictionPhysics.dynamicFriction = 1f;
            maxFrictionPhysics.frictionCombine = PhysicMaterialCombine.Maximum;

            // air physics 
            slippyPhysics = new PhysicMaterial();
            slippyPhysics.name = "slippyPhysics";
            slippyPhysics.staticFriction = 0f;
            slippyPhysics.dynamicFriction = 0f;
            slippyPhysics.frictionCombine = PhysicMaterialCombine.Minimum;

            // rigidbody info
            _rigidbody = GetComponent<Rigidbody>();

            // capsule collider info
            _capsuleCollider = GetComponent<CapsuleCollider>();

            // save your collider preferences 
            colliderCenter = GetComponent<CapsuleCollider>().center;
            colliderRadius = GetComponent<CapsuleCollider>().radius;
            colliderHeight = GetComponent<CapsuleCollider>().height;

            // avoid collision detection with inside colliders 
            Collider[] AllColliders = this.GetComponentsInChildren<Collider>();
            for (int i = 0; i < AllColliders.Length; i++)
            {
                Physics.IgnoreCollision(_capsuleCollider, AllColliders[i]);
            }

            // health info
            if (fillHealthOnStart)
                currentHealth = maxHealth;
            currentHealthRecoveryDelay = healthRecoveryDelay;
            currentStamina = maxStamina;
            ResetJumpMultiplier();
            isGrounded = true;
        }

        public virtual void UpdateMotor()
        {
            CheckHealth();
            CheckStamina();
            CheckGround();
            CheckRagdoll();
            StopMove();
            ControlCapsuleHeight();
            ControlJumpBehaviour();
            AirControl();
            StaminaRecovery();
            HealthRecovery();
        }

        #region Health & Stamina

        public override void TakeDamage(vDamage damage)
        {
            // don't apply damage if the character is rolling, you can add more conditions here
            if (currentHealth <= 0 || (!damage.ignoreDefense && isRolling))
                return;

            base.TakeDamage(damage);
        }

        protected override void TriggerDamageReaction(vDamage damage)
        {
            if (!customAction)
                base.TriggerDamageReaction(damage);
            else if (damage.activeRagdoll)
                onActiveRagdoll.Invoke();
        }

        public virtual void ReduceStamina(float value, bool accumulative)
        {
            if (accumulative) currentStamina -= value * Time.deltaTime;
            else currentStamina -= value;
            if (currentStamina < 0)
            {
                currentStamina = 0;
                OnStaminaEnd.Invoke();
            }
        }

        /// <summary>
        /// Change the currentStamina of Character
        /// </summary>
        /// <param name="value"></param>
        public virtual void ChangeStamina(int value)
        {
            currentStamina += value;
            currentStamina = Mathf.Clamp(currentStamina, 0, maxStamina);
        }

        /// <summary>
        /// Change the MaxStamina of Character
        /// </summary>
        /// <param name="value"></param>
        public virtual void ChangeMaxStamina(int value)
        {
            maxStamina += value;
            if (maxStamina < 0)
                maxStamina = 0;
        }

        public virtual void DeathBehaviour()
        {
            // lock the player input
            lockAnimMovement = true;
            // change the culling mode to render the animation until finish
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            // trigger die animation            
            if (deathBy == DeathBy.Animation || deathBy == DeathBy.AnimationWithRagdoll)
                animator.SetBool("isDead", true);
        }

        void CheckHealth()
        {
            if (isDead && currentHealth > 0)
            {
                isDead = false;
            }
        }

        void CheckStamina()
        {
            // check how much stamina this action will consume
            if (isSprinting)
            {
                currentStaminaRecoveryDelay = 0.25f;
                ReduceStamina(sprintStamina, true);
            }
        }

        public void StaminaRecovery()
        {
            if (currentStaminaRecoveryDelay > 0)
            {
                currentStaminaRecoveryDelay -= Time.deltaTime;
            }
            else
            {
                if (currentStamina > maxStamina)
                    currentStamina = maxStamina;
                if (currentStamina < maxStamina)
                    currentStamina += staminaRecovery;
            }
        }

        #endregion

        #region Locomotion

        public virtual void SetControllerMoveSpeed(vMovementSpeed speed)
        {
            if (isCrouching)
            {
                moveSpeed = Mathf.Lerp(moveSpeed, speed.crouchSpeed, speed.movementSmooth * Time.deltaTime);
                return;
            }

            if (speed.walkByDefault)
                moveSpeed = Mathf.Lerp(moveSpeed, isSprinting ? speed.runningSpeed : speed.walkSpeed, speed.movementSmooth * Time.deltaTime);
            else
                moveSpeed = Mathf.Lerp(moveSpeed, isSprinting ? speed.sprintSpeed : speed.runningSpeed, speed.movementSmooth * Time.deltaTime);
        }

        public virtual void MoveCharacter(Vector3 _direction)
        {
            // calculate input smooth
            inputSmooth = Vector3.Lerp(inputSmooth, input, (isStrafing ? strafeSpeed.movementSmooth : freeSpeed.movementSmooth) * Time.deltaTime);

            if (isSliding||ragdolled) return;

            _direction.y = 0;
            _direction.x = Mathf.Clamp(_direction.x, -1f, 1f);
            _direction.z = Mathf.Clamp(_direction.z, -1f, 1f);

            Vector3 targetPosition = (useRootMotion ? animator.rootPosition : _rigidbody.position) + _direction * (stopMove ? 0 : moveSpeed) * Time.deltaTime;
            Vector3 targetVelocity = (targetPosition - transform.position) / Time.deltaTime;

            bool useVerticalVelocity = true;
            if (useSnapGround)
                SnapToGround(ref targetVelocity, ref useVerticalVelocity);

            CalculateStepOffset(_direction.normalized, ref targetVelocity, ref useVerticalVelocity);

            if (useVerticalVelocity) targetVelocity.y = _rigidbody.velocity.y;
            _rigidbody.velocity = targetVelocity;
        }

        private void SnapToGround(ref Vector3 targetVelocity, ref bool useVerticalVelocity)
        {
            if (applyingStepOffset) return;
            var snapConditions = isGrounded && groundHit.collider != null && GroundAngle() <= slopeLimit && !disableCheckGround && !isSliding && !isJumping && !customAction && input.magnitude > 0.1f && !isInAirborne;
            if (snapConditions)
            {
                var y = ((groundHit.point - transform.position) / Time.deltaTime).y;
                if (y < limitFallVelocity)
                    y = limitFallVelocity;
                targetVelocity.y = y;
                useVerticalVelocity = false;
            }

        }

        void CalculateStepOffset(Vector3 moveDir, ref Vector3 targetVelocity, ref bool useVerticalVelocity)
        {
            if (isGrounded && !disableCheckGround && !isSliding && !isJumping && !customAction && !isInAirborne)
            {
                Vector3 dir = Vector3.Lerp(transform.forward, moveDir.normalized, inputSmooth.magnitude);
                float distance = _capsuleCollider.radius + stepOffsetDistance;
                float height = (stepOffsetMaxHeight + 0.01f + _capsuleCollider.radius * 0.5f);
                Vector3 pA = transform.position + transform.up * (stepOffsetMinHeight + 0.05f);
                Vector3 pB = pA + dir.normalized * distance;
                if (Physics.Linecast(pA, pB, out stepOffsetHit, groundLayer))
                {
                    Debug.DrawLine(pA, stepOffsetHit.point);
                    distance = stepOffsetHit.distance + 0.1f;
                }
                Ray ray = new Ray(transform.position + transform.up * height + dir.normalized * distance, Vector3.down);

                if (Physics.SphereCast(ray, _capsuleCollider.radius * 0.5f, out stepOffsetHit, (stepOffsetMaxHeight - stepOffsetMinHeight), groundLayer) && stepOffsetHit.point.y > transform.position.y)
                {
                    dir = (stepOffsetHit.point) - transform.position;
                    dir.Normalize();
                    //var v = targetVelocity;
                    //v.y = 0;
                    //targetVelocity = dir * v.magnitude;
                    targetVelocity = Vector3.Project(targetVelocity, dir);
                    applyingStepOffset = true;
                    useVerticalVelocity = false;
                    return;
                }
            }

            applyingStepOffset = false;
        }

        public virtual void StopCharacterWithLerp()
        {
            input = Vector3.Lerp(input, Vector3.zero, 2f * Time.deltaTime);
            _rigidbody.velocity = Vector3.Lerp(_rigidbody.velocity, Vector3.zero, 4f * Time.deltaTime);
            inputMagnitude = Mathf.Lerp(inputMagnitude, 0f, 2f * Time.deltaTime);
            moveSpeed = Mathf.Lerp(moveSpeed, 0f, 2f * Time.deltaTime);
        }

        public virtual void StopCharacter()
        {
            input = Vector3.zero;
            _rigidbody.velocity = Vector3.zero;
            inputMagnitude = 0f;
            moveSpeed = 0f;
        }

        public virtual void StopMove()
        {
            if (input.sqrMagnitude < 0.1) return;

            RaycastHit hitinfo;
            Ray ray = new Ray(transform.position + Vector3.up * stopMoveHeight, moveDirection.normalized);
            var hitAngle = 0f;
            if (debugWindow) Debug.DrawRay(ray.origin, ray.direction * stopMoveDistance, Color.red);
            if (Physics.Raycast(ray, out hitinfo, _capsuleCollider.radius + stopMoveDistance, stopMoveLayer))
            {
                stopMove = true;
                return;
            }

            if (Physics.Linecast(transform.position + Vector3.up * (_capsuleCollider.height * 0.5f), transform.position + moveDirection.normalized * (_capsuleCollider.radius + 0.2f), out hitinfo, groundLayer))
            {
                hitAngle = Vector3.Angle(Vector3.up, hitinfo.normal);
                if (debugWindow)
                    Debug.DrawLine(transform.position + Vector3.up * (_capsuleCollider.height * 0.5f), transform.position + moveDirection.normalized * (_capsuleCollider.radius + 0.2f), (hitAngle > slopeLimit) ? Color.yellow : Color.blue, 0.01f);
                var targetPoint = hitinfo.point + moveDirection.normalized * _capsuleCollider.radius;
                if ((hitAngle > slopeLimit) && Physics.Linecast(transform.position + Vector3.up * (_capsuleCollider.height * 0.5f), targetPoint, out hitinfo, groundLayer))
                {
                    if (debugWindow)
                        Debug.DrawRay(hitinfo.point, hitinfo.normal);
                    hitAngle = Vector3.Angle(Vector3.up, hitinfo.normal);

                    if (hitAngle > slopeLimit && hitAngle < 85f)
                    {
                        if (debugWindow)
                            Debug.DrawLine(transform.position + Vector3.up * (_capsuleCollider.height * 0.5f), hitinfo.point, Color.red, 0.01f);
                        stopMove = true;
                        return;
                    }
                    else
                    {
                        if (debugWindow)
                            Debug.DrawLine(transform.position + Vector3.up * (_capsuleCollider.height * 0.5f), hitinfo.point, Color.green, 0.01f);
                    }
                }
            }
            else if (debugWindow)
                Debug.DrawLine(transform.position + Vector3.up * (_capsuleCollider.height * 0.5f), transform.position + moveDirection.normalized * (_capsuleCollider.radius * 0.2f), Color.blue, 0.01f);

            stopMove = false;
        }

        public virtual void RotateToPosition(Vector3 position)
        {
            Vector3 desiredDirection = position - transform.position;
            RotateToDirection(desiredDirection.normalized);
        }

        public virtual void RotateToDirection(Vector3 direction)
        {
            RotateToDirection(direction, isStrafing ? strafeSpeed.rotationSpeed : freeSpeed.rotationSpeed);
        }

        public virtual void RotateToDirection(Vector3 direction, float rotationSpeed)
        {
            if (lockAnimRotation || customAction || (!jumpAndRotate && !isGrounded)|| isSliding || ragdolled) return;
            direction.y = 0f;
            Vector3 desiredForward = Vector3.RotateTowards(transform.forward, direction.normalized, rotationSpeed * Time.deltaTime, .1f);
            Quaternion _newRotation = Quaternion.LookRotation(desiredForward);
            transform.rotation  = _newRotation;
        }

        #endregion

        #region Jump Methods

        protected virtual void ControlJumpBehaviour()
        {
            if (!isJumping) return;

            jumpCounter -= Time.deltaTime;
            if (jumpCounter <= 0)
            {
                jumpCounter = 0;
                isJumping = false;
            }
            // apply extra force to the jump height   
            var vel = _rigidbody.velocity;
            vel.y = jumpHeight * jumpMultiplier;
            _rigidbody.velocity = vel;
        }

        public virtual void SetJumpMultiplier(float jumpMultiplier, float timeToReset = 1f)
        {
            this.jumpMultiplier = jumpMultiplier;

            if (timeToResetJumpMultiplier <= 0)
            {
                timeToResetJumpMultiplier = timeToReset;
                StartCoroutine(ResetJumpMultiplierRoutine());
            }
            else timeToResetJumpMultiplier = timeToReset;
        }

        public virtual void ResetJumpMultiplier()
        {
            StopCoroutine("ResetJumpMultiplierRoutine");
            timeToResetJumpMultiplier = 0;
            jumpMultiplier = 1;
        }

        protected IEnumerator ResetJumpMultiplierRoutine()
        {

            while (timeToResetJumpMultiplier > 0 && jumpMultiplier != 1)
            {
                timeToResetJumpMultiplier -= Time.deltaTime;
                yield return null;
            }
            jumpMultiplier = 1;
        }

        public virtual void AirControl()
        {
            if (isGrounded || isSliding) return;
            if (transform.position.y > heightReached) heightReached = transform.position.y;
            inputSmooth = Vector3.Lerp(inputSmooth, input, airSmooth * Time.deltaTime);

            if (jumpWithRigidbodyForce && !isGrounded)
            {
                _rigidbody.AddForce(moveDirection * airSpeed * Time.deltaTime, ForceMode.VelocityChange);
                return;
            }

            moveDirection.y = 0;
            moveDirection.x = Mathf.Clamp(moveDirection.x, -1f, 1f);
            moveDirection.z = Mathf.Clamp(moveDirection.z, -1f, 1f);

            Vector3 targetPosition = _rigidbody.position + (moveDirection * airSpeed) * Time.deltaTime;
            Vector3 targetVelocity = (targetPosition - transform.position) / Time.deltaTime;

            targetVelocity.y = _rigidbody.velocity.y;
            _rigidbody.velocity = Vector3.Lerp(_rigidbody.velocity, targetVelocity, airSmooth * Time.deltaTime);
        }

        protected virtual bool jumpFwdCondition
        {
            get
            {
                Vector3 p1 = transform.position + _capsuleCollider.center + Vector3.up * -_capsuleCollider.height * 0.5F;
                Vector3 p2 = p1 + Vector3.up * _capsuleCollider.height;
                return Physics.CapsuleCastAll(p1, p2, _capsuleCollider.radius * 0.5f, transform.forward, 0.6f, groundLayer).Length == 0;
            }
        }

        #endregion

        #region Crouch Methods

        public virtual void UseAutoCrouch(bool value)
        {
            autoCrouch = value;
        }

        public virtual void AutoCrouch()
        {
            if (autoCrouch)
                isCrouching = true;

            if (autoCrouch && !inCrouchArea && CanExitCrouch())
            {
                autoCrouch = false;
                isCrouching = false;
            }
        }

        public virtual bool CanExitCrouch()
        {
            // radius of SphereCast
            float radius = _capsuleCollider.radius * 0.9f;
            // Position of SphereCast origin stating in base of capsule
            Vector3 pos = transform.position + Vector3.up * ((colliderHeight * 0.5f) - colliderRadius);
            // ray for SphereCast
            Ray ray2 = new Ray(pos, Vector3.up);

            // sphere cast around the base of capsule for check ground distance
            if (Physics.SphereCast(ray2, radius, out groundHit, headDetect - (colliderRadius * 0.1f), autoCrouchLayer))
                return false;
            else
                return true;
        }

        protected virtual void AutoCrouchExit(Collider other)
        {
            if (other.CompareTag("AutoCrouch"))
            {
                inCrouchArea = false;
            }
        }

        protected virtual void CheckForAutoCrouch(Collider other)
        {
            if (other.gameObject.CompareTag("AutoCrouch"))
            {
                autoCrouch = true;
                inCrouchArea = true;
            }
        }

        #endregion

        #region Roll Methods

        internal bool canRollAgain { get { return isRolling && baseLayerInfo.normalizedTime >= timeToRollAgain; } }

        protected virtual void RollBehavior()
        {
            if (!isRolling) return;

            if (rollControl)
            {
                // calculate input smooth
                inputSmooth = Vector3.Lerp(inputSmooth, input, (isStrafing ? strafeSpeed.movementSmooth : freeSpeed.movementSmooth) * Time.deltaTime);
            }

            // rotation
            RotateToDirection(moveDirection, rollRotationSpeed);

            // movement
            Vector3 deltaPosition = useRollRootMotion ? new Vector3(animator.deltaPosition.x, 0f, animator.deltaPosition.z) : transform.forward * Time.deltaTime;
            Vector3 v = (deltaPosition * (rollSpeed > 0 ? rollSpeed : 1f)) / Time.deltaTime;
            if (rollUseGravity && baseLayerInfo.normalizedTime >= rollUseGravityTime) v.y = _rigidbody.velocity.y;

            _rigidbody.velocity = v;
        }

        #endregion

        #region Ground Check                

        protected virtual void CheckGround()
        {
            CheckGroundDistance();
            Sliding();
            ControlMaterialPhysics();

            if (isDead || customAction || disableCheckGround || isSliding)
            {
                isGrounded = true;
                return;
            }

            if (groundDistance <= groundMinDistance || applyingStepOffset)
            {
                CheckFallDamage();
                isGrounded = true;
                if (!useSnapGround && !applyingStepOffset && !isJumping && groundDistance > 0.05f)
                    _rigidbody.AddForce(transform.up * (extraGravity * 2 * Time.deltaTime), ForceMode.VelocityChange);
            }
            else
            {
                if (groundDistance >= groundMaxDistance)
                {
                    if (!isRolling)
                        isGrounded = false;

                    // check vertical velocity
                    verticalVelocity = _rigidbody.velocity.y;
                    // apply extra gravity when falling
                    if (!applyingStepOffset && !isJumping)
                    {
                        _rigidbody.AddForce(transform.up * extraGravity * Time.deltaTime, ForceMode.VelocityChange);
                    }
                }
                else if (!applyingStepOffset && !isJumping)
                {
                    _rigidbody.AddForce(transform.up * (extraGravity * 2 * Time.deltaTime), ForceMode.VelocityChange);
                }
            }
        }

        protected virtual void CheckFallDamage()
        {
            if (isGrounded || verticalVelocity > fallMinVerticalVelocity && blockFallActions || fallMinHeight == 0) return;
            float fallHeight = (heightReached - transform.position.y);

            fallHeight -= fallMinHeight;
            if (fallHeight > 0)
            {
                int damage = (int)(fallDamage * fallHeight);
                TakeDamage(new vDamage(damage, true));
            }

            heightReached = 0;
        }

        private void ControlMaterialPhysics()
        {
            // change the physics material to very slip when not grounded
            _capsuleCollider.material = (isGrounded && GroundAngle() <= slopeLimit + 1) ? frictionPhysics : slippyPhysics;

            if (isGrounded && input == Vector3.zero)
                _capsuleCollider.material = maxFrictionPhysics;
            else if (isGrounded && input != Vector3.zero)
                _capsuleCollider.material = frictionPhysics;
            else
                _capsuleCollider.material = slippyPhysics;
        }

        protected virtual void CheckGroundDistance()
        {
            if (isDead) return;
            if (_capsuleCollider != null)
            {
                // radius of the SphereCast
                float radius = _capsuleCollider.radius * 0.9f;
                var dist = 10f;
                // ray for RayCast
                Ray ray2 = new Ray(transform.position + new Vector3(0, colliderHeight / 2, 0), Vector3.down);
                // raycast for check the ground distance
                if (Physics.Raycast(ray2, out groundHit, (colliderHeight / 2) + dist, groundLayer) && !groundHit.collider.isTrigger)
                    dist = transform.position.y - groundHit.point.y;
                // sphere cast around the base of the capsule to check the ground distance
                if (groundCheckMethod == GroundCheckMethod.High && dist >= groundMinDistance)
                {
                    Vector3 pos = transform.position + Vector3.up * (_capsuleCollider.radius);
                    Ray ray = new Ray(pos, -Vector3.up);
                    if (Physics.SphereCast(ray, radius, out groundHit, _capsuleCollider.radius + groundMaxDistance, groundLayer) && !groundHit.collider.isTrigger)
                    {
                        Physics.Linecast(groundHit.point + (Vector3.up * 0.1f), groundHit.point + Vector3.down * 0.15f, out groundHit, groundLayer);
                        float newDist = transform.position.y - groundHit.point.y;
                        if (dist > newDist) dist = newDist;
                    }
                }
                groundDistance = (float)System.Math.Round(dist, 2);
            }
        }

        /// <summary>
        /// Return the ground angle
        /// </summary>
        /// <returns></returns>
        public virtual float GroundAngle()
        {
            var groundAngle = Vector3.Angle(groundHit.normal, Vector3.up);
            return groundAngle;
        }

        /// <summary>
        /// Return the angle of ground based on movement direction
        /// </summary>
        /// <returns></returns>
        public virtual float GroundAngleFromDirection()
        {
            var dir = isStrafing && input.magnitude > 0 ? (transform.right * input.x + transform.forward * input.z).normalized : transform.forward;
            var movementAngle = Vector3.Angle(dir, groundHit.normal) - 90;
            return movementAngle;
        }

        /// <summary>
        /// Prototype to align capsule collider with surface normal
        /// </summary>
        protected virtual void AlignWithSurface()
        {
            Ray ray = new Ray(transform.position, -transform.up);
            RaycastHit hit;
            var surfaceRot = transform.rotation;

            if (Physics.Raycast(ray, out hit, 1.5f, groundLayer))
            {
                surfaceRot = Quaternion.FromToRotation(transform.up, hit.normal) * transform.localRotation;
            }
            transform.rotation = Quaternion.Lerp(transform.rotation, surfaceRot, 10f * Time.deltaTime);
        }

        protected virtual void Sliding()
        {
            if (groundDistance <= groundMinDistance && GroundAngle() > slopeLimit)
            {
                if (_slidingEnterTime <= 0f || isSliding)
                {
                    var normal = groundHit.normal;
                    normal.y = 0f;
                    var dir = Vector3.ProjectOnPlane(normal.normalized, groundHit.normal).normalized;

                    if (Physics.Raycast(transform.position + Vector3.up * groundMinDistance, dir, groundMaxDistance, groundLayer))
                    {
                        isSliding = false;
                    }
                    else
                    {
                        isSliding = true;
                        SlideMovementBehavior();
                    }
                }
                else
                {
                    _slidingEnterTime -= Time.deltaTime;
                }
            }
            else
            {
                _slidingEnterTime = slidingEnterTime;
                isSliding = false;
            }
        }

        protected virtual void SlideMovementBehavior()
        {
            var normal = groundHit.normal;
            normal.y = 0f;
            var dir = Vector3.ProjectOnPlane(normal.normalized, groundHit.normal).normalized;
            _rigidbody.velocity = dir * slideDownVelocity;
            dir.y = 0f;

            Vector3 desiredForward = Vector3.RotateTowards(transform.forward, dir, 10f * Time.deltaTime, 0f);
            Quaternion _newRotation = Quaternion.LookRotation(desiredForward);
            _rigidbody.MoveRotation(_newRotation);

            var rightMovement = transform.InverseTransformDirection(moveDirection);
            rightMovement.y = 0f;
            rightMovement.z = 0f;
            rightMovement = transform.TransformDirection(rightMovement);

            _rigidbody.AddForce(rightMovement * slideSidewaysVelocity, ForceMode.VelocityChange);

            if (debugWindow)
            {
                Debug.DrawRay(transform.position, Vector3.ProjectOnPlane(normal.normalized, groundHit.normal).normalized, Color.blue);
                Debug.DrawRay(transform.position, Quaternion.AngleAxis(90, groundHit.normal) * Vector3.ProjectOnPlane(normal.normalized, groundHit.normal).normalized, Color.red);
                Debug.DrawRay(transform.position, transform.TransformDirection(rightMovement.normalized * 2f), Color.green);
            }
        }

        #endregion

        #region Colliders Check

        public virtual void ControlCapsuleHeight()
        {
            if (isCrouching || isRolling)
            {
                _capsuleCollider.center = colliderCenter / crouchHeightReduction;
                _capsuleCollider.height = colliderHeight / crouchHeightReduction;
            }
            else
            {
                // back to the original values
                _capsuleCollider.center = colliderCenter;
                _capsuleCollider.radius = colliderRadius;
                _capsuleCollider.height = colliderHeight;
            }
        }

        /// <summary>
        /// Disables rigibody gravity, turn the capsule collider trigger and reset all input from the animator.
        /// </summary>
        public virtual void DisableGravityAndCollision()
        {
            animator.SetFloat("InputHorizontal", 0f);
            animator.SetFloat("InputVertical", 0f);
            animator.SetFloat("VerticalVelocity", 0f);
            _rigidbody.useGravity = false;
            _rigidbody.velocity = Vector3.zero;
            _capsuleCollider.isTrigger = true;
        }

        /// <summary>
        /// Turn rigidbody gravity on the uncheck the capsulle collider as Trigger when the animation has finish playing
        /// </summary>
        /// <param name="normalizedTime">Check the value of your animation Exit Time and insert here</param>
        public virtual void EnableGravityAndCollision(float normalizedTime)
        {
            // enable collider and gravity at the end of the animation
            if (baseLayerInfo.normalizedTime >= normalizedTime)
            {
                _capsuleCollider.isTrigger = false;
                _rigidbody.useGravity = true;
            }
        }
        #endregion

        #region Ragdoll 

        protected virtual void CheckRagdoll()
        {
            if (ragdollVelocity == 0) return;

            // check your verticalVelocity and assign a value on the variable RagdollVel at the Player Inspector
            if (verticalVelocity <= ragdollVelocity && groundDistance <= 0.1f && !blockFallActions)
            {
                onActiveRagdoll.Invoke();
            }
        }

        public override void ResetRagdoll()
        {
            StopCharacter();
            verticalVelocity = 0f;
            ragdolled = false;
            _rigidbody.WakeUp();

            _rigidbody.useGravity = true;
            _rigidbody.isKinematic = false;
            _capsuleCollider.isTrigger = false;
        }

        public override void EnableRagdoll()
        {
            animator.SetFloat("InputHorizontal", 0f);
            animator.SetFloat("InputVertical", 0f);
            animator.SetFloat("VerticalVelocity", 0f);
            ragdolled = true;
            _capsuleCollider.isTrigger = true;
            _rigidbody.useGravity = false;
            _rigidbody.isKinematic = true;
            lockAnimMovement = true;
        }

        #endregion

        #region Debug

        public virtual string DebugInfo(string additionalText = "")
        {
            string debugInfo = string.Empty;
            if (debugWindow)
            {
                float delta = Time.smoothDeltaTime;
                float fps = 1 / delta;

                debugInfo =
                    " \n" +
                    "FPS " + fps.ToString("#,##0 fps") + "\n" +
                    "Health = " + currentHealth.ToString() + "\n" +
                    "Input Vertical = " + input.z.ToString("0.0") + "\n" +
                    "Input Horizontal = " + input.x.ToString("0.0") + "\n" +
                    "Input Magnitude = " + inputMagnitude.ToString("0.0") + "\n" +
                    "Vertical Velocity = " + verticalVelocity.ToString("0.00") + "\n" +
                    "Current MoveSpeed = " + moveSpeed.ToString("0.00") + "\n" +
                    "Ground Distance = " + groundDistance.ToString("0.00") + "\n" +
                    "Ground Angle = " + GroundAngleFromDirection().ToString("0.00") + "\n" +
                    "Is Grounded = " + BoolToRichText(isGrounded) + "\n" +
                    "Is Strafing = " + BoolToRichText(isStrafing) + "\n" +
                    "Is Trigger = " + BoolToRichText(_capsuleCollider.isTrigger) + "\n" +
                    "Use Gravity = " + BoolToRichText(_rigidbody.useGravity) + "\n" +
                    "Lock Movement = " + BoolToRichText(lockAnimMovement) + "\n" +
                    "Lock Rotation = " + BoolToRichText(lockAnimRotation) + "\n" +
                    "Stop Move = " + BoolToRichText(stopMove) + "\n" +
                    "--- Actions Bools ---" + "\n" +
                    "Is Sliding = " + BoolToRichText(isSliding) + "\n" +
                    "Is Sprinting = " + BoolToRichText(isSprinting) + "\n" +
                    "Is Crouching = " + BoolToRichText(isCrouching) + "\n" +
                    "Is Rolling = " + BoolToRichText(isRolling) + "\n" +
                    "Is Jumping = " + BoolToRichText(isJumping) + "\n" +
                    "Is Airborne = " + BoolToRichText(isInAirborne) + "\n" +
                    "Is Ragdolled = " + BoolToRichText(ragdolled) + "\n" +
                    "CustomAction = " + BoolToRichText(customAction) + "\n" + additionalText;
            }
            return debugInfo;
        }

        protected virtual string BoolToRichText(bool value)
        {
            return value ? "<color=yellow> True </color>" : "<color=red> False </color>";
        }

        protected virtual void OnDrawGizmos()
        {
            if (Application.isPlaying && debugWindow)
            {
                // debug auto crouch
                Vector3 posHead = transform.position + Vector3.up * ((colliderHeight * 0.5f) - colliderRadius);
                Ray ray1 = new Ray(posHead, Vector3.up);
                Gizmos.DrawWireSphere(ray1.GetPoint((headDetect - (colliderRadius * 0.1f))), colliderRadius * 0.9f);
                // debug stopmove            
                Ray ray3 = new Ray(transform.position + new Vector3(0, stopMoveHeight, 0), transform.forward);
                Debug.DrawRay(ray3.origin, ray3.direction * (_capsuleCollider.radius + stopMoveDistance), Color.blue);
                // debug slopelimit            
                Ray ray4 = new Ray(transform.position + new Vector3(0, colliderHeight / 3.5f, 0), transform.forward);
                Debug.DrawRay(ray4.origin, ray4.direction * 1f, Color.cyan);
            }

        }

        #endregion

        [System.Serializable]
        public class vMovementSpeed
        {
            [vHelpBox("Higher means faster/responsive movement, lower means smooth movement")]
            [Range(1f, 20f)]
            public float movementSmooth = 6f;
            [vHelpBox("Lower means faster transitions between animations, higher means slower")]
            [Range(0f, 1f)]
            public float animationSmooth = 0.2f;
            [Tooltip("Rotation speed of the character")]
            public float rotationSpeed = 16f;
            [Tooltip("Character will limit the movement to walk instead of running")]
            public bool walkByDefault = false;
            [Tooltip("Rotate with the Camera forward when standing idle")]
            public bool rotateWithCamera = false;
            [Tooltip("Speed to Walk using rigidbody or extra speed if you're using RootMotion")]
            public float walkSpeed = 2f;
            [Tooltip("Speed to Run using rigidbody or extra speed if you're using RootMotion")]
            public float runningSpeed = 4f;
            [Tooltip("Speed to Sprint using rigidbody or extra speed if you're using RootMotion")]
            public float sprintSpeed = 6f;
            [Tooltip("Speed to Crouch using rigidbody or extra speed if you're using RootMotion")]
            public float crouchSpeed = 2f;
        }
    }
}