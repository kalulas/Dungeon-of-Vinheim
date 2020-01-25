using UnityEngine;
using System.Collections;
using UnityEngine.UI;
namespace Invector.vCharacterController
{
    [vClassHeader("MELEE LOCK-ON")]
    public class vLockOn : vLockOnBehaviour
    {
        #region variables
        [System.Serializable]
        public class LockOnEvent : UnityEngine.Events.UnityEvent<Transform> { }

        public bool strafeWhileLockOn = true;
        [Tooltip("Create a Image inside the UI and assign here")]
        public RectTransform aimImagePrefab;
        public Vector2 aimImageSize = new Vector2(30, 30);
        [Tooltip("True: Hide the sprite when not Lock On, False: Always show the Sprite")]
        public bool hideSprite = true;
        [Tooltip("Create a offset for the sprite based at the center of the target")]
        [Range(-0.5f, 0.5f)]
        public float spriteHeight = 0.25f;
        [Tooltip("Offset for the camera height")]
        public float cameraHeightOffset;

        [Header("LockOn Inputs")]
        public GenericInput lockOnInput = new GenericInput("Tab", "RightStickClick", "RightStickClick");
        public GenericInput nexTargetInput = new GenericInput("X", false, false, "RightAnalogHorizontal", true, false, "X", false, false);
        public GenericInput previousTargetInput = new GenericInput("Z", false, false, "RightAnalogHorizontal", true, true, "Z", false, false);

        internal bool isLockingOn;
        public LockOnEvent onLockOnTarget;
        public LockOnEvent onUnLockOnTarget;
        private Canvas _aimCanvas;
        private RectTransform _aimImage;

        protected bool inTarget;
        protected vMeleeCombatInput tpInput;

        #endregion

        protected virtual void Start()
        {
            Init();
            tpInput = GetComponent<vMeleeCombatInput>();
            if (tpInput)
            {
                tpInput.onUpdate -= UpdateLockOn;
                tpInput.onUpdate += UpdateLockOn;

                // access the HealthController to Reset the LockOn when Dead
                GetComponent<vHealthController>().onDead.AddListener((GameObject g) => 
                {
                    // action to reset lockOn
                    isLockingOn = false;
                    LockOn(false);
                    UpdateLockOn();
                });
            }
        }

        public Canvas aimCanvas
        {
            get
            {
                if (_aimCanvas) return _aimCanvas;
                _aimCanvas = vHUDController.instance.GetComponentInParent<Canvas>();
                if (_aimCanvas == null) FindObjectOfType<Canvas>();
                return _aimCanvas;
            }
        }

        public RectTransform aimImage
        {
            get
            {
                if (_aimImage) return _aimImage;
                if (aimCanvas)
                {
                    _aimImage = Instantiate(aimImagePrefab, Vector2.zero, Quaternion.identity) as RectTransform;
                    _aimImage.SetParent(aimCanvas.transform);
                    return _aimImage;
                }
                return null;
            }
        }

        protected virtual void UpdateLockOn()
        {
            if (this.tpInput == null) return;
            LockOnInput();
            SwitchTargetsInput();
            CheckForCharacterAlive();
            UpdateAimImage();
        }

        protected virtual void LockOnInput()
        {
            if (tpInput.tpCamera == null || tpInput.cc == null) return;

            // lock the camera into a target, if there is any around
            if (lockOnInput.GetButtonDown() && !tpInput.cc.customAction)
            {
                isLockingOn = !isLockingOn;
                LockOn(isLockingOn);
            }
            // unlock the camera if the target is null
            else if (isLockingOn && (tpInput.tpCamera.lockTarget == null))
            {
                isLockingOn = false;
                LockOn(false);
            }
            // choose to use lock-on with strafe of free movement
            if (strafeWhileLockOn && !tpInput.cc.locomotionType.Equals(vThirdPersonMotor.LocomotionType.OnlyStrafe))
            {
                if (isLockingOn && tpInput.tpCamera.lockTarget != null)
                    tpInput.cc.isStrafing = true;
                else
                    tpInput.cc.isStrafing = false;
            }
        }

        protected override void SetTarget()
        {
            if (tpInput.tpCamera != null)
            {
                tpInput.tpCamera.SetLockTarget(currentTarget.transform, cameraHeightOffset);
                onLockOnTarget.Invoke(currentTarget);
            }
        }

        protected virtual void SwitchTargetsInput()
        {
            if (tpInput.tpCamera == null) return;

            if (tpInput.tpCamera.lockTarget)
            {
                // switch between targets using Keyboard
                if (previousTargetInput.GetButtonDown()) PreviousTarget();
                else if (nexTargetInput.GetButtonDown()) NextTarget();
            }
        }

        protected virtual void CheckForCharacterAlive()
        {
            if (currentTarget && !isCharacterAlive() && inTarget || (inTarget && !isCharacterAlive()))
            {
                ResetLockOn();
                inTarget = false;
                LockOn(true);
                StopLockOn();
            }
        }

        protected virtual void LockOn(bool value)
        {
            base.UpdateLockOn(value);
            if (!inTarget && currentTarget)
            {
                inTarget = true;
                // send current target if inTarget           
                SetTarget();
            }
            else if (inTarget && !currentTarget)
            {
                inTarget = false;
                // send message to clear current target
                StopLockOn();
            }
        }

        protected virtual void UpdateAimImage()
        {
            if (!aimCanvas || !aimImage) return;
            if (hideSprite)
            {
                aimImage.sizeDelta = aimImageSize;
                if (currentTarget && !aimImage.transform.gameObject.activeSelf && isCharacterAlive())
                    aimImage.transform.gameObject.SetActive(true);
                else if (!currentTarget && aimImage.transform.gameObject.activeSelf)
                    aimImage.transform.gameObject.SetActive(false);
                else if (_aimImage.transform.gameObject.activeSelf && !isCharacterAlive())
                    aimImage.transform.gameObject.SetActive(false);
            }
            if (currentTarget && aimImage && aimCanvas)
                aimImage.anchoredPosition = currentTarget.GetScreenPointOffBoundsCenter(aimCanvas, Camera.main, spriteHeight);
            else if (aimCanvas)
                aimImage.anchoredPosition = Vector2.zero;
        }

        public virtual void StopLockOn()
        {
            if (currentTarget == null && tpInput.tpCamera != null)
            {
                tpInput.tpCamera.RemoveLockTarget();
                onUnLockOnTarget.Invoke(currentTarget);
                isLockingOn = false;
                inTarget = false;
            }
        }       

        public virtual void NextTarget()
        {
            base.ChangeTarget(1);
        }

        public virtual void PreviousTarget()
        {
            base.ChangeTarget(-1);
        }



    }
}