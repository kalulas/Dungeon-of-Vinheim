using UnityEngine;
using System.Collections;
using UnityEngine.Events;
using UnityEngine.Serialization;

namespace Invector.vCharacterController.vActions
{
    [vClassHeader("Trigger Generic Action", false, iconName = "triggerIcon")]
    public class vTriggerGenericAction : vMonoBehaviour
    {
        [vEditorToolbar("Input", order = 1)]

        [Tooltip("Input to make the action")]
        public GenericInput actionInput = new GenericInput("E", "A", "A");      
        
        public enum InputType
        {
            GetButtonDown, 
            GetDoubleButton,
            GetButtonTimer, 
            AutoAction
        };

        public InputType inputType = InputType.GetButtonDown;
       
        [Header("- GetButtonTimer Settings:")]
        [vHelpBox("How much time you have to press the button")]
        public float buttonTimer = 3f;
        [vHelpBox("Needs a delay to work correctly")]
        public float inputDelay = 0.1f;
        [vHelpBox("<b>TRUE: </b> The action starts as soon as you press the input, if you release before it finishes the buttomTimer counter it will stop the action and reset the counter. \n<b>FALSE: </b> it will perform the action/animation only after the buttonTimer is finished")]
        public bool doActionWhilePressingButton = true;

        [Header("- DoubleButtonDown Settings:")]
        [vHelpBox("Time to press the button twice")]
        public float doubleButtomTime = 0.25f;

        [vEditorToolbar("Trigger", order = 2)]
        public string actionTag = "Action";
        [vHelpBox("Disable this trigger OnStart")]
        public bool disableOnStart = false;
        [vHelpBox("Disable the Player's Capsule Collider Collision, useful for animations with closer interactions")]
        public bool disableCollision;
        [vHelpBox("Disable the Player's Rigidbody Gravity, useful for on air animations")]
        public bool disableGravity;
        [vHelpBox("It will only use the trigger if the forward of the character is close to the forward of this transform")]
        public bool activeFromForward;
        [vHelpBox("Rotate Character to the Forward Rotation of this Trigger")]
        public bool useTriggerRotation;
        [vHelpBox("Destroy this Trigger after pressing the Input or AutoAction or finishing the Action")]
        public bool destroyAfter = false;
        [vHideInInspector("destroyAfter")]        
        public float destroyDelay = 0f;        
        [vHelpBox("Change your CameraState to a Custom State while playing the animation")]
        public string customCameraState;

        [vEditorToolbar("Animation", order = 2)]

        [vHelpBox("Trigger a Animation - Use the exactly same name of the AnimationState you want to trigger, don't forget to add a vAnimatorTag to your State")]
        public string playAnimation;
        [vHelpBox("Check the Exit Time of your animation (if it doesn't loop) and insert here. \n\nFor example if your Exit Time is 0.8 and the Transition Duration is 0.2 you need to insert 0.5 or lower as the final value. " +
            "\n\nAlways check with the Debug of the GenericAction if your animation is finishing correctly, otherwise the controller won't reset to the default physics and collision.", vHelpBoxAttribute.MessageType.Warning)]
        public float endExitTimeAnimation = 0.8f;        
        [vHelpBox("Use a ActionState value to apply special conditions for your AnimatorController transitions")]
        public int animatorActionState = 0;
        [vHelpBox("Reset the ActionState parameter to 0 after playing the animation")]
        public bool resetAnimatorActionState = true;
        [vHelpBox("Select the bone you want to use as reference to the Match Target")]
        public AvatarTarget avatarTarget;
        [vHelpBox("Check what positions XYZ you want the matchTarget to work")]
        [FormerlySerializedAs("matchTargetMask")]
        public Vector3 matchPos;
        [vHelpBox("Rotate Weight for your character to use the matchTarget rotation")]
        [Range(0, 1f)]
        public float matchRot;
        [vHelpBox("Use a empty transform as reference for the MatchTarget")]
        public Transform matchTarget;
        [vHelpBox("Time of the animation to start the MatchTarget goes from 0 to 1")]
        public float startMatchTarget;
        [vHelpBox("Time of the animation to end the MatchTarget goes from 0 to 1")]
        public float endMatchTarget;
      

        [vEditorToolbar("Events", order = 3)]

        [Tooltip("Delay to run the OnDoAction Event")]
        [FormerlySerializedAs("onDoActionDelay")]
        public float onPressActionDelay;

        [Header("--- INPUT EVENTS ---")]
        [FormerlySerializedAs("OnDoAction")]
        public UnityEvent OnPressActionInput;
        public OnDoActionWithTarget onPressActionInputWithTarget;

        [Header("--- ONLY FOR GET BUTTON TIMER ---")]
        public UnityEvent OnCancelActionInput;
        public UnityEvent OnFinishActionInput;
        public OnUpdateValue OnUpdateButtonTimer;

        [Header("--- ANIMATION EVENTS ---")]
        public UnityEvent OnStartAnimation;
        public UnityEvent OnEndAnimation;

        [Header("--- PLAYER AND TRIGGER DETECTION ---")]        
        public OnDoActionWithTarget OnPlayerEnter;
        public OnDoActionWithTarget OnPlayerStay;
        public OnDoActionWithTarget OnPlayerExit;

        
        private float currentButtonTimer;
        internal Collider _collider;

        //void OnDrawGizmos()
        //{
        //    if(_collider != null)
        //    {
        //        //Color red = new Color(1, 0, 0, 0.2f);
        //        Color green = new Color(0, 1, 0, 0.2f);
        //        Gizmos.matrix = Matrix4x4.TRS(transform.position, transform.rotation, (transform.lossyScale));
        //        //Gizmos.color = inCollision && Application.isPlaying ? red : green;
        //        Gizmos.color = green;
        //        Gizmos.DrawCube(Vector3.zero, Vector3.one);
        //    }            
        //    else
        //    {
        //        _collider = GetComponent<Collider>();
        //    }
        //}

        protected virtual void Start()
        {
            this.gameObject.tag = actionTag;
            this.gameObject.layer = LayerMask.NameToLayer("Triggers");
            _collider = GetComponent<Collider>();
            _collider.isTrigger = true;
            if (disableOnStart)
                this.enabled = false;
        }

        public virtual IEnumerator OnDoActionDelay(GameObject obj)
        {            
            yield return new WaitForSeconds(onPressActionDelay);
            OnPressActionInput.Invoke();
            if (obj)
                onPressActionInputWithTarget.Invoke(obj);
        }

        public void UpdateButtonTimer(float value)
        {
            if(value != currentButtonTimer)
            {
                currentButtonTimer = value;
                OnUpdateButtonTimer.Invoke(value);                    
            }
        }

        [System.Serializable]
        public class OnUpdateValue : UnityEvent<float>
        {
            
        }
    }

    [System.Serializable]
    public class OnDoActionWithTarget : UnityEvent<GameObject>
    {

    }

}