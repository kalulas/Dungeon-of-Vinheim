using Invector.vEventSystems;
using UnityEngine;

namespace Invector.vCharacterController
{
    public class vThirdPersonAnimator : vThirdPersonMotor
    {
        #region Variables                
        [HideInInspector]
        public Transform matchTarget;
        public vAnimatorStateInfos animatorStateInfos;
        private float randomIdleCount;
        private float _speed = 0;
        private float _direction = 0;
        public const float walkSpeed = 0.5f;
        public const float runningSpeed = 1f;
        public const float sprintSpeed = 1.5f;
        private bool triggerDieBehaviour;

        public int baseLayer { get { return animator.GetLayerIndex("Base Layer"); } }
        public int underBodyLayer { get { return animator.GetLayerIndex("UnderBody"); } }
        public int rightArmLayer { get { return animator.GetLayerIndex("RightArm"); } }
        public int leftArmLayer { get { return animator.GetLayerIndex("LeftArm"); } }
        public int upperBodyLayer { get { return animator.GetLayerIndex("UpperBody"); } }
        public int fullbodyLayer { get { return animator.GetLayerIndex("FullBody"); } }

        #endregion

        protected override void Start()
        {
            base.Start();
            animatorStateInfos = new vAnimatorStateInfos(GetComponent<Animator>());
            animatorStateInfos.RegisterListener();
        }

        protected virtual void OnEnable()
        {
            if (animatorStateInfos != null)
                animatorStateInfos.RegisterListener();
        }

        protected virtual void OnDisable()
        {
            if (animatorStateInfos != null)
                animatorStateInfos.RemoveListener();
        }

        public virtual void UpdateAnimator()
        {
            if (animator == null || !animator.enabled) return;            

            AnimatorLayerControl();
            ActionsControl();

            TriggerRandomIdle();

            UpdateAnimatorParameters();
            DeadAnimation();            
        }

        public virtual void AnimatorLayerControl()
        {
            baseLayerInfo = animator.GetCurrentAnimatorStateInfo(baseLayer);
            underBodyInfo = animator.GetCurrentAnimatorStateInfo(underBodyLayer);
            rightArmInfo = animator.GetCurrentAnimatorStateInfo(rightArmLayer);
            leftArmInfo = animator.GetCurrentAnimatorStateInfo(leftArmLayer);
            upperBodyInfo = animator.GetCurrentAnimatorStateInfo(upperBodyLayer);
            fullBodyInfo = animator.GetCurrentAnimatorStateInfo(fullbodyLayer);
        }

        public virtual void ActionsControl()
        {
            // to have better control of your actions, you can assign bools to know if an animation is playing or not
            // this way you can use this bool to create custom behavior for the controller
            
            // identify if the rolling animations is playing
            isRolling = IsAnimatorTag("IsRolling");
            // identify if a turn on spot animation is playing
            isTurningOnSpot = IsAnimatorTag("TurnOnSpot");
            // locks player movement while a animation with tag 'LockMovement' is playing
            lockAnimMovement = IsAnimatorTag("LockMovement");
            // locks player rotation while a animation with tag 'LockRotation' is playing
            lockAnimRotation = IsAnimatorTag("LockRotation");
            // ! -- you can add the Tag "CustomAction" into a AnimatonState and the character will not perform any Melee action -- !            
            customAction = IsAnimatorTag("CustomAction");
            // identify if the controller is airborne
            isInAirborne = IsAnimatorTag("Airborne");
        }

        public virtual void UpdateAnimatorParameters()
        {
            if (disableAnimations) return;

            animator.SetBool(vAnimatorParameters.IsStrafing, isStrafing); ;
            animator.SetBool(vAnimatorParameters.IsSprinting, isSprinting);
            animator.SetBool(vAnimatorParameters.IsSliding, isSliding);
            animator.SetBool(vAnimatorParameters.IsCrouching, isCrouching);
            animator.SetBool(vAnimatorParameters.IsGrounded, isGrounded);
            animator.SetBool(vAnimatorParameters.IsDead, isDead);
            animator.SetFloat(vAnimatorParameters.GroundDistance, groundDistance);
            animator.SetFloat(vAnimatorParameters.GroundAngle, GroundAngleFromDirection());

            if (!isGrounded)
                animator.SetFloat(vAnimatorParameters.VerticalVelocity, verticalVelocity);

            if (!lockAnimMovement)
            {
                if (isStrafing)
                {
                    animator.SetFloat(vAnimatorParameters.InputHorizontal, !stopMove ? horizontalSpeed : 0f, strafeSpeed.animationSmooth, Time.deltaTime);
                    animator.SetFloat(vAnimatorParameters.InputVertical, !stopMove ? verticalSpeed : 0f, strafeSpeed.animationSmooth, Time.deltaTime);
                }
                else
                {
                    var dir = transform.InverseTransformDirection(moveDirection);
                    animator.SetFloat(vAnimatorParameters.InputVertical, stopMove ? 0 : verticalSpeed, freeSpeed.animationSmooth, Time.deltaTime);
                    animator.SetFloat(vAnimatorParameters.InputHorizontal, stopMove ? 0 : useLeanMovement ? dir.x : 0f, freeSpeed.animationSmooth, Time.deltaTime);
                }
                
                animator.SetFloat(vAnimatorParameters.InputMagnitude, stopMove ? 0f : inputMagnitude, isStrafing ? strafeSpeed.animationSmooth : freeSpeed.animationSmooth, Time.deltaTime);
            }

            if (turnOnSpotAnim)
            {
                GetTurnOnSpotDirection(transform, Camera.main.transform, ref _speed, ref _direction, input);
                FreeTurnOnSpot(_direction * 180);
                StrafeTurnOnSpot();
            }
        }

        public virtual void SetAnimatorMoveSpeed(vMovementSpeed speed)
        {
            Vector3 relativeInput = transform.InverseTransformDirection(moveDirection);
            verticalSpeed = relativeInput.z;
            horizontalSpeed = relativeInput.x;

            var newInput = new Vector2(verticalSpeed, horizontalSpeed);

            if (speed.walkByDefault)
                inputMagnitude = Mathf.Clamp(newInput.magnitude, 0, isSprinting ? runningSpeed : walkSpeed);
            else
                inputMagnitude = Mathf.Clamp(isSprinting ? newInput.magnitude + 0.5f : newInput.magnitude, 0, isSprinting ? sprintSpeed : runningSpeed);
        }

        public virtual void ResetInputAnimatorParameters()
        {
            animator.SetFloat("InputHorizontal", 0f, 0f, Time.deltaTime);
            animator.SetFloat("InputVertical", 0f, 0f, Time.deltaTime);
            animator.SetFloat("InputMagnitude", 0f, 0f, Time.deltaTime);
        }

        protected virtual void TriggerRandomIdle()
        {
            if (input != Vector3.zero || customAction) return;

            if (randomIdleTime > 0)
            {
                if (input.sqrMagnitude == 0 && !isCrouching && _capsuleCollider.enabled && isGrounded)
                {
                    randomIdleCount += Time.fixedDeltaTime;
                    if (randomIdleCount > 6)
                    {
                        randomIdleCount = 0;
                        animator.SetTrigger(vAnimatorParameters.IdleRandomTrigger);
                        animator.SetInteger(vAnimatorParameters.IdleRandom, Random.Range(1, 4));
                    }
                }
                else
                {
                    randomIdleCount = 0;
                    animator.SetInteger(vAnimatorParameters.IdleRandom, 0);
                }
            }
        }

        protected virtual void DeadAnimation()
        {
            if (!isDead) return;

            if (!triggerDieBehaviour)
            {
                triggerDieBehaviour = true;
                DeathBehaviour();
            }

            // death by animation
            if (deathBy == DeathBy.Animation)
            {
                if (fullBodyInfo.IsName("Dead"))
                {
                    if (fullBodyInfo.normalizedTime >= 0.99f && groundDistance <= 0.15f)
                        RemoveComponents();
                }
            }
            // death by animation & ragdoll after a time
            else if (deathBy == DeathBy.AnimationWithRagdoll)
            {
                if (fullBodyInfo.IsName("Dead"))
                {
                    // activate the ragdoll after the animation finish played
                    if (fullBodyInfo.normalizedTime >= 0.8f)
                        onActiveRagdoll.Invoke();
                }
            }
            // death by ragdoll
            else if (deathBy == DeathBy.Ragdoll)
                onActiveRagdoll.Invoke();
        }

        #region TurnOnSpot

        public static float AngleSigned(Vector3 v1, Vector3 v2, Vector3 n)
        {
            return Mathf.Atan2(
                Vector3.Dot(n, Vector3.Cross(v1, v2)),
                Vector3.Dot(v1, v2)) * Mathf.Rad2Deg;
        }

        protected virtual void StrafeTurnOnSpot()
        {
            if (!isStrafing || input.sqrMagnitude >= 0.25f || isTurningOnSpot || customAction || !strafeSpeed.rotateWithCamera)
            {
                animator.SetFloat(vAnimatorParameters.TurnOnSpotDirection, 0);
                return;
            }

            var localFwd = transform.InverseTransformDirection(Camera.main.transform.forward);
            var angle = System.Math.Round(localFwd.x, 1);

            if (angle >= 0.01f && !isTurningOnSpot)
                animator.SetFloat(vAnimatorParameters.TurnOnSpotDirection, 10);
            else if (angle <= -0.01f && !isTurningOnSpot)
                animator.SetFloat(vAnimatorParameters.TurnOnSpotDirection, -10);
            else
                animator.SetFloat(vAnimatorParameters.TurnOnSpotDirection, 0);
        }

        protected virtual void FreeTurnOnSpot(float direction)
        {
            if (isStrafing || !freeSpeed.rotateWithCamera) return;

            bool inTransition = animator.IsInTransition(0);
            float directionDampTime = isTurningOnSpot || inTransition ? 1000000 : 0;
            animator.SetFloat(vAnimatorParameters.TurnOnSpotDirection, direction, directionDampTime, Time.deltaTime);
        }

        protected virtual void GetTurnOnSpotDirection(Transform root, Transform camera, ref float _speed, ref float _direction, Vector2 input)
        {
            Vector3 rootDirection = root.forward;
            Vector3 stickDirection = new Vector3(input.x, 0, input.y);

            // Get camera rotation.    
            Vector3 CameraDirection = camera.forward;
            CameraDirection.y = 0.0f; // kill Y
            Quaternion referentialShift = Quaternion.FromToRotation(Vector3.forward, CameraDirection);
            // Convert joystick input in Worldspace coordinates            
            Vector3 moveDirection = rotateByWorld ? stickDirection : referentialShift * stickDirection;

            Vector2 speedVec = new Vector2(input.x, input.y);
            _speed = Mathf.Clamp(speedVec.magnitude, 0, 1);

            if (_speed > 0.01f) // dead zone
            {
                Vector3 axis = Vector3.Cross(rootDirection, moveDirection);
                _direction = Vector3.Angle(rootDirection, moveDirection) / 180.0f * (axis.y < 0 ? -1 : 1);
            }
            else
            {
                _direction = 0.0f;
            }
        }

        #endregion

        #region Generic Animations Methods

        public virtual void SetActionState(int value)
        {
            animator.SetInteger(vAnimatorParameters.ActionState, value);
        }

        public virtual bool IsAnimatorTag(string tag)
        {
            if (animator == null) return false;
            if (animatorStateInfos != null)
            {
                if (animatorStateInfos.HasTag(tag))
                {
                    return true;
                }
            }
            if (baseLayerInfo.IsTag(tag)) return true;
            if (underBodyInfo.IsTag(tag)) return true;
            if (rightArmInfo.IsTag(tag)) return true;
            if (leftArmInfo.IsTag(tag)) return true;
            if (upperBodyInfo.IsTag(tag)) return true;
            if (fullBodyInfo.IsTag(tag)) return true;
            return false;
        }

        public virtual void MatchTarget(Vector3 matchPosition, Quaternion matchRotation, AvatarTarget target, MatchTargetWeightMask weightMask, float normalisedStartTime, float normalisedEndTime)
        {
            if (animator.isMatchingTarget || animator.IsInTransition(0))
                return;

            float normalizeTime = Mathf.Repeat(animator.GetCurrentAnimatorStateInfo(0).normalizedTime, 1f);

            if (normalizeTime > normalisedEndTime)
                return;

            animator.MatchTarget(matchPosition, matchRotation, target, weightMask, normalisedStartTime, normalisedEndTime);
        }

        #endregion

    }

    public static partial class vAnimatorParameters
    {
        public static int InputHorizontal = Animator.StringToHash("InputHorizontal");
        public static int InputVertical = Animator.StringToHash("InputVertical");
        public static int InputMagnitude = Animator.StringToHash("InputMagnitude");
        public static int TurnOnSpotDirection = Animator.StringToHash("TurnOnSpotDirection");
        public static int ActionState = Animator.StringToHash("ActionState");
        public static int ResetState = Animator.StringToHash("ResetState");
        public static int IsDead = Animator.StringToHash("isDead");
        public static int IsGrounded = Animator.StringToHash("IsGrounded");
        public static int IsCrouching = Animator.StringToHash("IsCrouching");
        public static int IsStrafing = Animator.StringToHash("IsStrafing");
        public static int IsSprinting = Animator.StringToHash("IsSprinting");
        public static int IsSliding = Animator.StringToHash("IsSliding");
        public static int GroundDistance = Animator.StringToHash("GroundDistance");
        public static int GroundAngle = Animator.StringToHash("GroundAngle");
        public static int VerticalVelocity = Animator.StringToHash("VerticalVelocity");
        public static int IdleRandom = Animator.StringToHash("IdleRandom");
        public static int IdleRandomTrigger = Animator.StringToHash("IdleRandomTrigger");
    }
}