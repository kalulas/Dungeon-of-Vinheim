using UnityEngine;
using UnityEngine.Events;

namespace Invector.vCharacterController.vActions
{
    [vClassHeader("Ladder Action", iconName = "ladderIcon")]
    public class vLadderAction : vActionListener
    {
        #region public variables

        [vEditorToolbar("Settings", overrideChildOrder: true, order = 0)]
        [Tooltip("Tag of the object you want to access")]
        public string actionTag = "LadderTrigger";
        [Tooltip("Speed multiplier for the climb ladder animations")]
        public float climbSpeed = 1.5f;
        [Tooltip("Speed multiplier for the climb ladder animations when the fastClimbInput is pressed")]
        public float fastClimbSpeed = 3f;
        [Tooltip("How much Stamina will be consumed when climbing faster")]
        public float fastClimbStamina = 30f;
        [Tooltip("Input to use the ladder going up or down")]
        public GenericInput verticallInput = new GenericInput("Vertical", "LeftAnalogVertical", "Vertical");
        [Tooltip("Input to enter the ladder")]
        public GenericInput enterInput = new GenericInput("E", "A", "A");
        [Tooltip("Input to exit the ladder")]
        public GenericInput exitInput = new GenericInput("Space", "B", "B");
        [Tooltip("Input to climb faster")]
        public GenericInput fastClimbInput = new GenericInput("LeftShift", "LeftStickClick", "LeftStickClick");
        [Tooltip("Input to climb faster")]
        public GenericInput slideDownInput = new GenericInput("Q", "X", "X");

        public AnimationCurve enterRotationCurve, exitRotationCurve;

        [vEditorToolbar("Events")]
        public UnityEvent OnEnterLadder;
        public UnityEvent OnExitLadder;


        [vEditorToolbar("Debug")]
        public bool debugMode;
        [vReadOnly(false)]
        [SerializeField]
        protected vTriggerLadderAction ladderAction;
        [vReadOnly(false)]
        [SerializeField]
        protected vTriggerLadderAction ladderActionTemp;
        [vReadOnly(false)]
        [SerializeField]
        protected float speed;
        [vReadOnly(false)]
        [SerializeField]
        protected float currentClimbSpeed;
        [vReadOnly(false)]
        [SerializeField]
        protected bool isUsingLadder;
        [vReadOnly(false)]
        [SerializeField]
        protected bool isEnterLadder;
        [vReadOnly(false)]
        [SerializeField]
        protected bool isExitingLadder;
        [vReadOnly(false)]
        [SerializeField]
        protected bool triggerEnterOnce;
        [vReadOnly(false)]
        [SerializeField]
        protected bool triggerExitOnce;

        #endregion      

        protected vThirdPersonInput tpInput;

        private void Awake()
        {
            actionStay = true;
            actionExit = true;
        }

        protected override void Start()
        {
            base.Start();
            tpInput = GetComponent<vThirdPersonInput>();
            if (tpInput)
            {
                tpInput.onUpdate -= UpdateLadderBehavior;
                tpInput.onUpdate += UpdateLadderBehavior;

                tpInput.onAnimatorMove -= UseLadder;
                tpInput.onAnimatorMove += UseLadder;
            }
        }

        void UpdateLadderBehavior()
        {
            AutoEnterLadder();
            EnterLadderInput();
            ExitLadderInput();
        }

        void EnterLadderInput()
        {
            if (ladderAction == null || tpInput.cc.customAction || tpInput.cc.isJumping || !tpInput.cc.isGrounded || tpInput.cc.isRolling) return;

            if (enterInput.GetButtonDown() && !isEnterLadder && !isUsingLadder && !ladderAction.autoAction)
            {
                TriggerEnterLadder();
            }
        }

        void TriggerEnterLadder()
        {
            if (debugMode) Debug.Log("Enter Ladder");

            tpInput.cc.isCrouching = false;
            tpInput.cc.ControlCapsuleHeight();
            tpInput.UpdateCameraStates();
            tpInput.cc.UpdateAnimator();
            OnEnterLadder.Invoke();
            triggerEnterOnce = true;
            isEnterLadder = true;
            tpInput.cc.animator.SetInteger(vAnimatorParameters.ActionState, 1);     // set actionState 1 to avoid falling transitions
            tpInput.SetLockAllInput(true);
            tpInput.cc.ResetInputAnimatorParameters();
            ladderAction.OnDoAction.Invoke();
            ladderActionTemp = ladderAction;

            if (!string.IsNullOrEmpty(ladderActionTemp.playAnimation))
            {
                tpInput.cc.animator.CrossFadeInFixedTime(ladderActionTemp.playAnimation, 0.25f);     // trigger the action animation clip                                           
                isUsingLadder = true;
                tpInput.cc.disableAnimations = true;
                tpInput.cc.StopCharacter();
                isEnterLadder = false;
            }
        }

        void UseLadder()
        {
            if (!isUsingLadder) return;

            // update the base layer to know what animations are being played
            tpInput.cc.AnimatorLayerControl();
            tpInput.cc.ActionsControl();
            // update camera movement
            tpInput.CameraInput();

            // go up or down 
            speed = verticallInput.GetAxis();
            tpInput.cc.animator.SetFloat(vAnimatorParameters.InputVertical, speed, 0.1f, Time.deltaTime);
            if (speed >= 0.05f || speed <= -0.05f)
                tpInput.cc.animator.speed = Mathf.Lerp(tpInput.cc.animator.speed, currentClimbSpeed, 2f * Time.deltaTime);
            else
                tpInput.cc.animator.speed = Mathf.Lerp(tpInput.cc.animator.speed, 1f, 2f * Time.deltaTime);

            // increase speed by input and consume stamina
            if (fastClimbInput.GetButton() && tpInput.cc.currentStamina > 0)
            {
                currentClimbSpeed = fastClimbSpeed;
                StaminaConsumption();
            }
            else
            {
                currentClimbSpeed = climbSpeed;
            }

            // enter ladder behaviour           
            if (tpInput.cc.baseLayerInfo.IsName("EnterLadderTop") || tpInput.cc.baseLayerInfo.IsName("EnterLadderBottom") && !tpInput.cc.animator.IsInTransition(0))
            {
                tpInput.cc.DisableGravityAndCollision();              // disable gravity & turn collision trigger
                // disable ingame hud
                if (ladderActionTemp != null) ladderActionTemp.OnPlayerExit.Invoke();

                if (ladderActionTemp.useTriggerRotation)
                {                    
                    if (debugMode) Debug.Log("Rotating to target...");
                    //Debug.Log(enterRotationCurve.Evaluate(tpInput.cc.baseLayerInfo.normalizedTime));
                    // smoothly rotate the character to the target                    
                    //var newRot = new Vector3(transform.eulerAngles.x, ladderActionTemp.matchTarget.transform.eulerAngles.y, transform.eulerAngles.z);
                    transform.rotation = Quaternion.Lerp(transform.rotation, ladderActionTemp.matchTarget.transform.rotation, enterRotationCurve.Evaluate(tpInput.cc.baseLayerInfo.normalizedTime));
                }

                if (ladderActionTemp.matchTarget != null)
                {
                    if (debugMode) Debug.Log("Match Target to enter...");
                    // use match target to match the Y and Z target 
                    tpInput.cc.MatchTarget(ladderActionTemp.matchTarget.transform.position, ladderActionTemp.matchTarget.transform.rotation, AvatarTarget.Root, new MatchTargetWeightMask(new Vector3(1, 1, 1), 0), ladderActionTemp.startMatchTarget, ladderActionTemp.endMatchTarget);
                }
            }

            // exit ladder behaviour
            isExitingLadder = tpInput.cc.baseLayerInfo.IsName("ExitLadderTop") || tpInput.cc.baseLayerInfo.IsName("ExitLadderBottom") || tpInput.cc.baseLayerInfo.IsName("QuickExitLadder");

            if (isExitingLadder)
            {
                tpInput.cc.animator.speed = 1;

                if (ladderActionTemp.exitMatchTarget != null && !tpInput.cc.baseLayerInfo.IsName("QuickExitLadder"))
                {
                    if (debugMode) Debug.Log("Match Target to exit...");
                    // use match target to match the Y and Z target 
                    tpInput.cc.MatchTarget(ladderActionTemp.exitMatchTarget.transform.position, ladderActionTemp.exitMatchTarget.transform.rotation, AvatarTarget.Root, new MatchTargetWeightMask(new Vector3(1, 1, 1), 0), ladderActionTemp.exitStartMatchTarget, ladderActionTemp.exitEndMatchTarget);

                }
                var newRot = new Vector3(0, tpInput.animator.rootRotation.eulerAngles.y, 0);
                transform.rotation = Quaternion.Lerp(tpInput.animator.rootRotation, Quaternion.Euler(newRot), exitRotationCurve.Evaluate(tpInput.cc.animator.GetCurrentAnimatorStateInfo(0).normalizedTime));
                if (tpInput.cc.baseLayerInfo.normalizedTime >= 0.8f)
                {
                    // after playing the animation we reset some values
                    ResetPlayerSettings();
                }
            }
        }

        private void StaminaConsumption()
        {
            if (tpInput.cc.currentStamina <= 0) return;
            tpInput.cc.ReduceStamina(fastClimbStamina, true);        // call the ReduceStamina method from the player
            tpInput.cc.currentStaminaRecoveryDelay = 0.25f;    // delay to start recovery stamina
        }

        void ExitLadderInput()
        {
            if (!isUsingLadder) return;
            if (tpInput.cc.baseLayerInfo.IsName("EnterLadderTop") || tpInput.cc.baseLayerInfo.IsName("EnterLadderBottom")) return;

            if (ladderAction == null)
            {
                if (tpInput.cc.IsAnimatorTag("ClimbLadder"))
                {
                    if (slideDownInput.GetButtonDown() && !isExitingLadder)
                    {
                        tpInput.cc.animator.CrossFadeInFixedTime("Ladder_SlideDown", 0.2f);
                    }

                    // exit ladder at any moment by pressing the cancelInput
                    if (exitInput.GetButtonDown())
                    {
                        if (debugMode) Debug.Log("Quick Exit");
                        tpInput.cc.animator.speed = 1;
                        tpInput.cc.animator.CrossFadeInFixedTime("QuickExitLadder", 0.1f);
                        Invoke("ResetPlayerSettings", .5f);
                    }
                }
            }
            else
            {
                ladderActionTemp = ladderAction;
                var animationClip = ladderAction.exitAnimation;
                if (animationClip == "ExitLadderBottom")
                {
                    // exit ladder when reach the bottom by pressing the cancelInput or pressing down at
                    if (exitInput.GetButtonDown() && !triggerExitOnce || (speed <= -0.05f && !triggerExitOnce) || (tpInput.cc.IsAnimatorTag("LadderSlideDown") && ladderAction != null && !triggerExitOnce))
                    {
                        if (debugMode) Debug.Log("Exit Bottom");
                        triggerExitOnce = true;
                        tpInput.cc.animator.CrossFadeInFixedTime(ladderAction.exitAnimation, 0.1f);             // trigger the animation clip        
                    }
                }
                else if (animationClip == "ExitLadderTop" && tpInput.cc.IsAnimatorTag("ClimbLadder"))    // exit the ladder from the top
                {
                    if ((speed >= 0.05f) && !triggerExitOnce && !tpInput.cc.animator.IsInTransition(0))         // trigger the exit animation by pressing up
                    {
                        if (debugMode) Debug.Log("Exit Top");
                        triggerExitOnce = true;
                        tpInput.cc.animator.CrossFadeInFixedTime(ladderAction.exitAnimation, 0.1f);             // trigger the animation clip
                    }
                }
            }
        }

        void AutoEnterLadder()
        {
            if (ladderAction == null || !ladderAction.autoAction) return;
            if (tpInput.cc.customAction || isUsingLadder || tpInput.cc.animator.IsInTransition(0)) return;

            // enter the ladder automatically if checked with autoAction
            if (ladderAction.autoAction && tpInput.cc.input != Vector3.zero && !tpInput.cc.customAction)
            {
                var inputDir = Camera.main.transform.TransformDirection(new Vector3(tpInput.cc.input.x, 0f, tpInput.cc.input.z));
                inputDir.y = 0f;
                var dist = Vector3.Distance(inputDir.normalized, ladderAction.transform.forward);
                if (dist < 0.8f)
                    TriggerEnterLadder();
            }
        }

        public void ResetPlayerSettings()
        {
            if (debugMode) Debug.Log("Reset Player Settings");
            speed = 0f;
            ladderAction = null;
            isUsingLadder = false;
            OnExitLadder.Invoke();
            triggerExitOnce = false;
            triggerEnterOnce = false;
            tpInput.cc.animator.SetInteger(vAnimatorParameters.ActionState, 0);
            tpInput.cc._rigidbody.useGravity = true;
            tpInput.cc._capsuleCollider.isTrigger = false;
            tpInput.SetLockAllInput(false);
            tpInput.cc.StopCharacter();
            tpInput.cc.disableAnimations = false;
        }

        public override void OnActionStay(Collider other)
        {
            if (other.gameObject.CompareTag(actionTag))
            {
                CheckForTriggerAction(other);
            }
        }

        public override void OnActionExit(Collider other)
        {
            if (other.gameObject.CompareTag(actionTag))
            {
                // disable ingame hud
                if (ladderAction != null) ladderAction.OnPlayerExit.Invoke();
                ladderAction = null;
            }
        }

        void CheckForTriggerAction(Collider other)
        {
            // assign the component - it will be null when he exit the trigger area
            var _ladderAction = other.GetComponent<vTriggerLadderAction>();
            if (!_ladderAction)
            {
                return;
            }
            // check the maxAngle too see if the character can do the action
            var dist = Vector3.Distance(transform.forward, _ladderAction.transform.forward);

            if (isUsingLadder && _ladderAction != null)
                ladderAction = _ladderAction;
            else if (dist <= 0.8f && !isUsingLadder)
            {
                ladderAction = _ladderAction;
                ladderAction.OnPlayerEnter.Invoke();
            }
            else
            {
                if (ladderAction != null) ladderAction.OnPlayerExit.Invoke();
                ladderAction = null;
            }
        }
    }
}