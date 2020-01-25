using System.Collections;
using UnityEngine;

namespace Invector.vCharacterController.vActions
{
    using System.Collections.Generic;
    using vCharacterController;
    [vClassHeader("GENERIC ACTION", "Use the vTriggerGenericAction to trigger a simple animation.", iconName = "triggerIcon")]
    public class vGenericAction : vActionListener
    {
        [Tooltip("Tag of the object you want to access")]
        public string actionTag = "Action";
        [Tooltip("Use root motion of the animation")]
        public bool useRootMotion = true;

        [Header("--- Debug Only ---")]

        [Tooltip("Check this to enter the debug mode")]
        public bool debugMode;
        [vReadOnly] public vTriggerGenericAction triggerAction;
        [vReadOnly, SerializeField]
        protected bool _playingAnimation;
        [vReadOnly, SerializeField]
        protected bool actionStarted;
        [vReadOnly]
        public bool isLockTriggerEvents;
        [vReadOnly, SerializeField]
        protected List<Collider> colliders = new List<Collider>();
        public Camera mainCamera;
        public UnityEngine.Events.UnityEvent OnStartAction;
        public UnityEngine.Events.UnityEvent OnCancelAction;
        public UnityEngine.Events.UnityEvent OnEndAction;

        internal vThirdPersonInput tpInput;
        private float _currentInputDelay;
        private Vector3 _screenCenter;
        private float timeInTrigger;
        private float animationBehaviourDelay;

        protected virtual Vector3 screenCenter
        {
            get
            {
                _screenCenter.x = Screen.width * 0.5f;
                _screenCenter.y = Screen.height * 0.5f;
                _screenCenter.z = 0;
                return _screenCenter;
            }
        }

        internal Dictionary<Collider, vTriggerGenericAction> actions;

        protected virtual void Awake()
        {
            actionEnter = true;
            actionStay = true;
            actionExit = true;
            actions = new Dictionary<Collider, vTriggerGenericAction>();
        }

        protected override void Start()
        {
            base.Start();
            tpInput = GetComponent<vThirdPersonInput>();
            if (tpInput != null)
            {
                tpInput.onUpdate -= UpdateGenericAction;
                tpInput.onUpdate += UpdateGenericAction;
            }
            if (!mainCamera) mainCamera = Camera.main;
        }

        protected virtual void UpdateGenericAction()
        {
            if (!mainCamera) mainCamera = Camera.main;
            if (!mainCamera) return;

            CheckForTriggerAction();
            AnimationBehaviour();
            colliders.Clear();
            foreach (var key in actions.Keys)
            {
                colliders.Add(key);
            }
            if (!doingAction && triggerAction && !isLockTriggerEvents)
            {
                if (timeInTrigger <= 0)
                {
                    actions.Clear();
                    triggerAction = null;
                }
                else timeInTrigger -= Time.deltaTime;
            }
        }

        protected virtual bool inActionAnimation
        {
            get
            {
                return !string.IsNullOrEmpty(triggerAction.playAnimation)
                    && tpInput.cc.baseLayerInfo.IsName(triggerAction.playAnimation);
            }
        }

        protected virtual void CheckForTriggerAction()
        {
            if (actions.Count == 0 && !triggerAction || isLockTriggerEvents) return;
            vTriggerGenericAction _triggerAction = GetNearAction();
            if (!doingAction && triggerAction != _triggerAction)
            {
                triggerAction = _triggerAction;
                if (triggerAction)
                    triggerAction.OnPlayerEnter.Invoke(gameObject);
            }

            TriggerActionInput();
        }

        protected vTriggerGenericAction GetNearAction()
        {
            if (isLockTriggerEvents || _doingAction || playingAnimation) return null;
            float distance = Mathf.Infinity;
            vTriggerGenericAction _targetAction = null;

            foreach (var key in actions.Keys)
            {
                if (key)
                {
                    var screenP = mainCamera ? mainCamera.WorldToScreenPoint(key.transform.position) : screenCenter;
                    if (mainCamera)
                    {
                        bool isValid = actions[key].enabled && actions[key].gameObject.activeInHierarchy && (!actions[key].activeFromForward && (screenP - screenCenter).magnitude < distance || IsInForward(actions[key].transform) && (screenP - screenCenter).magnitude < distance);
                        if (isValid)
                        {
                            distance = (screenP - screenCenter).magnitude;
                            if (_targetAction && _targetAction != actions[key])
                            {
                                _targetAction.OnPlayerExit.Invoke(gameObject);
                                _targetAction = actions[key];
                            }
                            else if (_targetAction == null)
                            {
                                _targetAction = actions[key];
                            }
                        }
                        else
                        {
                            actions[key].OnPlayerExit.Invoke(gameObject);
                        }
                    }
                    else
                    {
                        if (!_targetAction)
                        {
                            _targetAction = actions[key];
                        }
                        else
                        {
                            actions[key].OnPlayerExit.Invoke(gameObject);
                        }
                    }
                }
                else
                {
                    actions.Remove(key);
                    return null;
                }
            }

            return _targetAction;
        }

        protected virtual bool IsInForward(Transform target)
        {
            var dist = Vector3.Distance(transform.forward, target.forward);
            return dist <= 0.8f;
        }

        protected virtual void AnimationBehaviour()
        {            
            if (animationBehaviourDelay > 0 && !playingAnimation)
            {
                animationBehaviourDelay -= Time.deltaTime; return;
            }

            if (playingAnimation)
            {
                OnStartAction.Invoke();
                triggerAction.OnStartAnimation.Invoke();

                if (triggerAction.matchTarget != null)
                {
                    if (debugMode) Debug.Log("Match Target...");
                    // use match target to match the Y and Z target 
                    tpInput.cc.MatchTarget(triggerAction.matchTarget.transform.position, triggerAction.matchTarget.transform.rotation, triggerAction.avatarTarget,
                        new MatchTargetWeightMask(triggerAction.matchPos, triggerAction.matchRot), triggerAction.startMatchTarget, triggerAction.endMatchTarget);
                }

                if (triggerAction.useTriggerRotation)
                {
                    if (debugMode) Debug.Log("Rotate to Target...");
                    // smoothly rotate the character to the target
                    var newRot = new Vector3(transform.eulerAngles.x, triggerAction.transform.eulerAngles.y, transform.eulerAngles.z);
                    transform.rotation = Quaternion.Lerp(transform.rotation, Quaternion.Euler(newRot), tpInput.cc.animator.GetCurrentAnimatorStateInfo(0).normalizedTime);
                }

                if (triggerAction.inputType != vTriggerGenericAction.InputType.GetButtonTimer && tpInput.cc.animator.GetCurrentAnimatorStateInfo(0).normalizedTime >= triggerAction.endExitTimeAnimation)
                {
                    if (debugMode) Debug.Log("Finish Animation");

                    // triggers the OnEndAnimation Event
                    triggerAction.OnEndAnimation.Invoke();
                    // reset GenericAction variables so you can use it again
                    ResetTriggerSettings();
                }
            }
            else if (doingAction && actionStarted)
            {
                //when using a GetButtonTimer the ResetTriggerSettings will be automatically called at the end of the timer or by releasing the input
                if (triggerAction != null && triggerAction.inputType == vTriggerGenericAction.InputType.GetButtonTimer) return;

                if (debugMode) Debug.Log("Force ResetTriggerSettings");
                // triggers the OnEndAnimation Event
                triggerAction.OnEndAnimation.Invoke();
                ResetTriggerSettings();
            }
        }

        public virtual bool playingAnimation
        {
            get
            {
                if (triggerAction == null) return _playingAnimation = false;
                if (!_playingAnimation && inActionAnimation)
                {
                    _playingAnimation = true;
                    DisablePlayerGravityAndCollision();
                }
                else
                if (_playingAnimation && !inActionAnimation)
                {
                    _playingAnimation = false;
                }
                return _playingAnimation;
            }
            protected set
            {
                _playingAnimation = true;
            }
        }

        public virtual bool actionConditions
        {
            get
            {
                return (!tpInput.cc.isJumping || !tpInput.cc.customAction || !playingAnimation) && !tpInput.cc.animator.IsInTransition(0);
            }
        }

        public override void OnActionEnter(Collider other)
        {
            if (isLockTriggerEvents) return;

            if (other != null && other.gameObject.CompareTag(actionTag) && !actions.ContainsKey(other))
            {
                vTriggerGenericAction _triggerAction = other.GetComponent<vTriggerGenericAction>();
                if (_triggerAction && _triggerAction.enabled) actions.Add(other, _triggerAction);
            }
        }

        public override void OnActionExit(Collider other)
        {
            if (isLockTriggerEvents) return;
            if (other.gameObject.CompareTag(actionTag) && actions.ContainsKey(other) && (!_doingAction || other != triggerAction._collider))
            {
                actions[other].OnPlayerExit.Invoke(gameObject);
                actions.Remove(other);
            }
        }

        public override void OnActionStay(Collider other)
        {
            if (isLockTriggerEvents) return;
            if (other != null && other.gameObject.CompareTag(actionTag))
            {
                OnActionEnter(other);
                timeInTrigger = .5f;
            }
        }

        public virtual void TriggerActionInput()
        {
            if (triggerAction == null || !triggerAction.gameObject.activeInHierarchy) return;

            // if (canTriggerAction)
            {
                if (triggerAction.inputType == vTriggerGenericAction.InputType.AutoAction && actionConditions)
                {
                    TriggerAnimation();
                }
                else if (triggerAction.inputType == vTriggerGenericAction.InputType.GetButtonDown && actionConditions)
                {
                    if (triggerAction.actionInput.GetButtonDown())
                    {
                        TriggerAnimation();

                    }
                }
                else if (triggerAction.inputType == vTriggerGenericAction.InputType.GetDoubleButton && actionConditions)
                {
                    if (triggerAction.actionInput.GetDoubleButtonDown(triggerAction.doubleButtomTime))
                        TriggerAnimation();
                }
                else if (triggerAction.inputType == vTriggerGenericAction.InputType.GetButtonTimer)
                {
                    if (_currentInputDelay <= 0)
                    {
                        // this mode will animate while you press the button and call the OnEndAction once you finish pressing the button
                        if (triggerAction.doActionWhilePressingButton)
                        {
                            var up = false;
                            var t = 0f;
                            // call the OnEndAction after the buttomTimer 
                            if (triggerAction.actionInput.GetButtonTimer(ref t, ref up, triggerAction.buttonTimer))
                            {
                                triggerAction.OnFinishActionInput.Invoke();

                                ResetActionState();
                                ResetTriggerSettings();
                            }
                            // trigger the animation when you start pressing the action button
                            if (triggerAction && triggerAction.actionInput.inButtomTimer)
                            {
                                triggerAction.UpdateButtonTimer(t);
                                TriggerAnimation();
                            }
                            // call OnCancelActionInput if the button is released
                            if (up && triggerAction)
                            {
                                if (debugMode) Debug.Log("Cancel Action");
                                triggerAction.OnCancelActionInput.Invoke();
                                _currentInputDelay = triggerAction.inputDelay;
                                triggerAction.UpdateButtonTimer(0);
                                ResetActionState();
                                ResetTriggerSettings();
                            }
                        }
                        // this mode will call the animation and event only when the buttonTimer finished 
                        else
                        {
                            if (triggerAction.actionInput.GetButtonTimer(triggerAction.buttonTimer))
                            {
                                TriggerAnimation();

                                if (playingAnimation)
                                {
                                    if (debugMode) Debug.Log("call OnFinishInput Event");
                                    triggerAction.OnFinishActionInput.Invoke();
                                }
                            }
                        }
                    }
                    else
                    {
                        _currentInputDelay -= Time.deltaTime;
                    }
                }
            }
        }

        public virtual void TriggerAnimation()
        {
            if (playingAnimation || actionStarted) return;
            doingAction = true;
            OnDoAction.Invoke(triggerAction);

            //triggerAction.OnPlayerExit.Invoke();
            if (debugMode) Debug.Log("TriggerAnimation", gameObject);

            if (triggerAction.animatorActionState != 0)
            {
                if (debugMode) Debug.Log("Applied ActionState: " + triggerAction.animatorActionState, gameObject);
                tpInput.cc.SetActionState(triggerAction.animatorActionState);
            }

            // trigger the animation behaviour & match target
            if (!string.IsNullOrEmpty(triggerAction.playAnimation))
            {
                if (!actionStarted)
                {
                    actionStarted = true;
                    playingAnimation = true;
                    tpInput.cc.animator.CrossFadeInFixedTime(triggerAction.playAnimation, 0.1f);    // trigger the action animation clip
                    if (!string.IsNullOrEmpty(triggerAction.customCameraState))
                        tpInput.ChangeCameraState(triggerAction.customCameraState, true);                 // change current camera state to a custom
                }
            }
            else
            {
                actionStarted = true;
            }
            animationBehaviourDelay = 0.2f;
            // trigger OnDoAction Event, you can add a delay in the inspector   
            StartCoroutine(triggerAction.OnDoActionDelay(gameObject));
            // destroy the triggerAction if checked with destroyAfter
            if (triggerAction.destroyAfter)
                StartCoroutine(DestroyActionDelay(triggerAction));
        }

        public virtual void ResetActionState()
        {
            if (triggerAction && triggerAction.resetAnimatorActionState)
                tpInput.cc.SetActionState(0);
        }

        public virtual void ResetTriggerSettings()
        {
            if (debugMode) Debug.Log("Reset Trigger Settings");

            // reset player gravity and collision
            EnablePlayerGravityAndCollision();
            // reset the Animator parameter ActionState back to 0 
            ResetActionState();
            // reset the CameraState to the Default state
            tpInput.ResetCameraState();
            // reset canTriggerAction so you can trigger another action
            //canTriggerAction = false;
            if (triggerAction != null && actions.ContainsKey(triggerAction._collider))
            {
                actions.Remove(triggerAction._collider);
            }
            triggerAction = null;
            // reset triggerActionOnce so you can trigger again
            //  triggerActionOnce = false;
            doingAction = false;
            actionStarted = false;
        }

        public virtual void DisablePlayerGravityAndCollision()
        {
            if (triggerAction && triggerAction.disableGravity)
            {
                if (debugMode) Debug.Log("Disable Player's Gravity");
                tpInput.cc._rigidbody.useGravity = false;
                tpInput.cc._rigidbody.velocity = Vector3.zero;
            }
            if (triggerAction && triggerAction.disableCollision)
            {
                if (debugMode) Debug.Log("Disable Player's Collision");
                tpInput.cc._capsuleCollider.isTrigger = true;
            }
        }

        public virtual void EnablePlayerGravityAndCollision()
        {
            if (debugMode) Debug.Log("Enable Player's Gravity");
            tpInput.cc._rigidbody.useGravity = true;

            if (debugMode) Debug.Log("Enable Player's Collision");
            tpInput.cc._capsuleCollider.isTrigger = false;
        }

        public virtual IEnumerator DestroyActionDelay(vTriggerGenericAction triggerAction)
        {
            var _triggerAction = triggerAction;
            yield return new WaitForSeconds(_triggerAction.destroyDelay);
            OnEndAction.Invoke();
            ResetTriggerSettings();
            Destroy(_triggerAction.gameObject);
        }

        public virtual void SetLockTriggerEvents(bool value)
        {
            foreach (var key in actions.Keys)
            {
                if (key)
                {
                    actions[key].OnPlayerExit.Invoke(gameObject);
                }
            }
            actions.Clear();
            isLockTriggerEvents = value;
        }
    }
}