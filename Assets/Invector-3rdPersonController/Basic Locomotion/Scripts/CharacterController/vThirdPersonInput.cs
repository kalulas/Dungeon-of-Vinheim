using System.Collections;
using UnityEngine;

namespace Invector.vCharacterController
{
    [vClassHeader("Input Manager", iconName = "inputIcon")]
    public class vThirdPersonInput : vMonoBehaviour
    {
        [System.Serializable]
        public delegate void OnUpdateEvent();

        public event OnUpdateEvent onLateUpdate;
        public event OnUpdateEvent onFixedUpdate;
        public event OnUpdateEvent onAnimatorMove;
        public event OnUpdateEvent onUpdate;

        #region Variables        

        [vEditorToolbar("Inputs")]
        [Header("Default Input")]
        public bool lockInput;
        [Header("Uncheck if you need to use the cursor")]
        public bool unlockCursorOnStart = false;
        public bool showCursorOnStart = false;
        public GenericInput horizontalInput = new GenericInput("Horizontal", "LeftAnalogHorizontal", "Horizontal");
        public GenericInput verticallInput = new GenericInput("Vertical", "LeftAnalogVertical", "Vertical");
        public GenericInput jumpInput = new GenericInput("Space", "X", "X");
        public GenericInput rollInput = new GenericInput("Q", "B", "B");
        public GenericInput strafeInput = new GenericInput("Tab", "RightStickClick", "RightStickClick");
        public GenericInput sprintInput = new GenericInput("LeftShift", "LeftStickClick", "LeftStickClick");
        public GenericInput crouchInput = new GenericInput("C", "Y", "Y");

        [vEditorToolbar("Camera Settings")]
        public bool lockCameraInput;
        public bool ignoreCameraRotation;

        [vEditorToolbar("Inputs")]
        [Header("Camera Input")]
        public GenericInput rotateCameraXInput = new GenericInput("Mouse X", "RightAnalogHorizontal", "Mouse X");
        public GenericInput rotateCameraYInput = new GenericInput("Mouse Y", "RightAnalogVertical", "Mouse Y");
        public GenericInput cameraZoomInput = new GenericInput("Mouse ScrollWheel", "", "");
        [HideInInspector]
        public vCamera.vThirdPersonCamera tpCamera;         // acess tpCamera info
        [HideInInspector] public Camera cameraMain;
        [HideInInspector]
        public string customCameraState;                    // generic string to change the CameraState
        [HideInInspector]
        public string customlookAtPoint;                    // generic string to change the CameraPoint of the Fixed Point Mode
        [HideInInspector]
        public bool changeCameraState;                      // generic bool to change the CameraState
        [HideInInspector]
        public bool smoothCameraState;                      // generic bool to know if the state will change with or without lerp
        [HideInInspector]
        public vThirdPersonController cc;                   // access the ThirdPersonController component
        [HideInInspector]
        public vHUDController hud;                          // acess vHUDController component
        protected bool updateIK = false;
        protected bool isInit;
        
        protected InputDevice inputDevice { get { return vInput.instance.inputDevice; } }
        public Animator animator
        {
            get
            {
                if (cc == null) cc = GetComponent<vThirdPersonController>();
                if (cc.animator == null) return GetComponent<Animator>();
                return cc.animator;
            }
        }

        #endregion

        #region Initialize Character, Camera & HUD when LoadScene

        protected virtual void Start()
        {
            cc = GetComponent<vThirdPersonController>();

            if (cc != null)
                cc.Init();

            if (vThirdPersonController.instance == cc || vThirdPersonController.instance == null)
            {
                StartCoroutine(CharacterInit());
            }

            ShowCursor(showCursorOnStart);
            LockCursor(unlockCursorOnStart);
        }

        protected virtual IEnumerator CharacterInit()
        {
            yield return new WaitForEndOfFrame();
            if (tpCamera == null)
            {
                tpCamera = FindObjectOfType<vCamera.vThirdPersonCamera>();
                if (tpCamera && tpCamera.target != transform) tpCamera.SetMainTarget(this.transform);
            }
            if (hud == null && vHUDController.instance != null)
            {
                hud = vHUDController.instance;
                hud.Init(cc);
            }
        }

        #endregion

        protected virtual void LateUpdate()
        {            
            if (cc == null || Time.timeScale == 0) return;           
            if (!updateIK) return;
            if (onLateUpdate != null) onLateUpdate.Invoke();

            CameraInput();                      // update camera input
            UpdateCameraStates();               // update camera states            
            updateIK = false;            
        }

        protected virtual void FixedUpdate()
        {
            if (onFixedUpdate != null) onFixedUpdate.Invoke();

            cc.UpdateMotor();                                                             // handle the ThirdPersonMotor methods            
            cc.ControlLocomotionType();                                                   // handle the controller locomotion type and movespeed   
            if (tpCamera == null || !tpCamera.lockTarget) cc.ControlRotationType();       // handle the controller rotation type
            cc.UpdateAnimator();                                                          // handle the ThirdPersonAnimator methods
            updateIK = true;           
        }

        protected virtual void Update()
        {            
            if (cc == null || Time.timeScale == 0) return;
            if (onUpdate != null) onUpdate.Invoke();

            InputHandle();                      // update input methods            
            UpdateHUD();                        // update hud graphics            
        }

        public virtual void OnAnimatorMove()
        {            
            cc.ControlAnimatorRootMotion();
            if (onAnimatorMove != null) onAnimatorMove.Invoke();
        }

        #region Generic Methods
        // you can call this methods anywhere in the inspector or third party assets to have better control of the controller or cutscenes

        /// <summary>
        /// Lock all the Input from the Player
        /// </summary>
        /// <param name="value"></param>
        public virtual void SetLockBasicInput(bool value)
        {
            lockInput = value;
            if (value)
            {
                cc.input = Vector2.zero;
                cc.isSprinting = false;
                cc.animator.SetFloat("InputHorizontal", 0, 0.25f, Time.deltaTime);
                cc.animator.SetFloat("InputVertical", 0, 0.25f, Time.deltaTime);
                cc.animator.SetFloat("InputMagnitude", 0, 0.25f, Time.deltaTime);
            }
        }

        public virtual void SetLockAllInput(bool value)
        {
            SetLockBasicInput(value);
        }

        /// <summary>
        /// Show/Hide Cursor
        /// </summary>
        /// <param name="value"></param>
        public virtual void ShowCursor(bool value)
        {
            Cursor.visible = value;
        }

        /// <summary>
        /// Lock/Unlock the cursor to the center of screen
        /// </summary>
        /// <param name="value"></param>
        public virtual void LockCursor(bool value)
        {
            if (!value)
                Cursor.lockState = CursorLockMode.Locked;
            else
                Cursor.lockState = CursorLockMode.None;
        }

        /// <summary>
        /// Lock the Camera Input
        /// </summary>
        /// <param name="value"></param>
        public virtual void SetLockCameraInput(bool value)
        {
            lockCameraInput = value;
        }

        /// <summary>
        /// If you're using the MoveCharacter method with a custom targetDirection, check this true to align the character with your custom targetDirection
        /// </summary>
        /// <param name="value"></param>
        public virtual void IgnoreCameraRotation(bool value)
        {
            ignoreCameraRotation = value;
        }

        /// <summary>
        /// Limits the character to walk only, useful for cutscenes and 'indoor' areas
        /// </summary>
        /// <param name="value"></param>
        public virtual void SetWalkByDefault(bool value)
        {
            cc.freeSpeed.walkByDefault = value;
            cc.strafeSpeed.walkByDefault = value;
        }

        /// <summary>
        /// Set the character to Strafe Locomotion
        /// </summary>
        /// <param name="value"></param>
        public virtual void SetStrafeLocomotion(bool value)
        {
            cc.lockInStrafe = value;
            cc.isStrafing = value;
        }        

        #endregion

        #region Basic Locomotion Inputs

        protected virtual void InputHandle()
        {
            if (lockInput || cc.ragdolled) return;

            MoveInput();
            SprintInput();
            CrouchInput();
            StrafeInput();
            JumpInput();
            RollInput();
        }

        public virtual void MoveInput()
        {
            // gets input
            cc.input.x = horizontalInput.GetAxisRaw();
            cc.input.z = verticallInput.GetAxisRaw();            

            cc.ControlKeepDirection();
        }

        protected virtual void StrafeInput()
        {
            if (strafeInput.GetButtonDown())
                cc.Strafe();
        }

        protected virtual void SprintInput()
        { 
            cc.Sprint(cc.useContinuousSprint ? sprintInput.GetButtonDown() : sprintInput.GetButton());
        }

        protected virtual void CrouchInput()
        {
            cc.AutoCrouch();

            if (crouchInput.GetButtonDown())
                cc.Crouch();
        }

        /// <summary>
        /// Conditions to trigger the Jump animation & behavior
        /// </summary>
        /// <returns></returns>
        protected virtual bool JumpConditions()
        {
            return !cc.customAction && !cc.isCrouching && cc.isGrounded && cc.GroundAngle() < cc.slopeLimit && cc.currentStamina >= cc.jumpStamina && !cc.isJumping;
        }
       
        /// <summary>
        /// Input to trigger the Jump 
        /// </summary>
        protected virtual void JumpInput()
        {
            if (jumpInput.GetButtonDown() && JumpConditions())
                cc.Jump(true);
        }

        /// <summary>
        /// Conditions to trigger the Roll animation & behavior
        /// </summary>
        /// <returns></returns>
        protected virtual bool RollConditions()
        {
            return (!cc.isRolling || cc.canRollAgain) && cc.input != Vector3.zero && !cc.customAction && cc.isGrounded && cc.currentStamina > cc.rollStamina && !cc.isJumping;
        }

        /// <summary>
        /// Input to trigger the Roll
        /// </summary>
        protected virtual void RollInput()
        {
            if (rollInput.GetButtonDown() && RollConditions())
                cc.Roll();
        }

        #endregion       

        #region Camera Methods

        public virtual void CameraInput()
        {
            if (!cameraMain)
            {
                if (!Camera.main) Debug.Log("Missing a Camera with the tag MainCamera, please add one.");
                else
                {
                    cameraMain = Camera.main;
                    cc.rotateTarget = cameraMain.transform;
                }
            }

            if (cameraMain && !ignoreCameraRotation)
            {
                if (!cc.keepDirection)
                    cc.UpdateMoveDirection(cameraMain.transform);
                if (tpCamera != null && tpCamera.lockTarget)
                    cc.RotateToPosition(tpCamera.lockTarget.position);
            }

            if (tpCamera == null)
                return;

            var Y = lockCameraInput ? 0f : rotateCameraYInput.GetAxis();
            var X = lockCameraInput ? 0f : rotateCameraXInput.GetAxis();
            var zoom = cameraZoomInput.GetAxis();

            tpCamera.RotateCamera(X, Y);
            tpCamera.Zoom(zoom);
        }

        public virtual void UpdateCameraStates()
        {
            // CAMERA STATE - you can change the CameraState here, the bool means if you want lerp of not, make sure to use the same CameraState String that you named on TPCameraListData

            if (tpCamera == null)
            {
                tpCamera = FindObjectOfType<vCamera.vThirdPersonCamera>();
                if (tpCamera == null)
                    return;
                if (tpCamera)
                {
                    tpCamera.SetMainTarget(this.transform);
                    tpCamera.Init();
                }
            }

            if (changeCameraState)
                tpCamera.ChangeState(customCameraState, customlookAtPoint, smoothCameraState);
            else if (cc.isCrouching)
                tpCamera.ChangeState("Crouch", true);
            else if (cc.isStrafing)
                tpCamera.ChangeState("Strafing", true);
            else
                tpCamera.ChangeState("Default", true);
        }

        public virtual void ChangeCameraState(string cameraState, bool useLerp = true)
        {
            changeCameraState = true;
            customCameraState = cameraState;
            smoothCameraState = useLerp;
        }

        public virtual void ResetCameraState()
        {
            changeCameraState = false;
            customCameraState = string.Empty;
        }

       

        #endregion

        #region HUD       

        public virtual void UpdateHUD()
        {
            if (hud == null)
            {
                if (vHUDController.instance != null)
                {
                    hud = vHUDController.instance;
                    hud.Init(cc);
                }
                else return;
            }

            hud.UpdateHUD(cc);
        }

        #endregion
    }
}