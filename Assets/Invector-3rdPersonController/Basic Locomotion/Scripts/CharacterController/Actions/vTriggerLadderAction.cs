using UnityEngine;
using System.Collections;
using UnityEngine.Events;
namespace Invector.vCharacterController.vActions
{
    [vClassHeader("Trigger Ladder Action", false)]
    public class vTriggerLadderAction : vMonoBehaviour
    {
        [Header("Trigger Action Options")]
        [Tooltip("Automatically execute the action without the need to press a Button")]
        public bool autoAction;

        [Header("Enter")]
        [Tooltip("Trigger an Animation - Use the exactly same name of the AnimationState you want to trigger")]
        public string playAnimation;        
        [Tooltip("Use a transform to help the character climb any height, take a look at the Example Scene ClimbUp, StepUp, JumpOver objects.")]
        public Transform matchTarget;
        [Tooltip("Start the match target of the animation")]
        public float startMatchTarget;
        [Tooltip("End the match target of the animation")]
        public float endMatchTarget;

        [Header("Exit")]
        [Tooltip("Trigger an Animation - Use the exactly same name of the AnimationState you want to trigger")]
        public string exitAnimation;
        [Tooltip("Use a empty gameObject as a reference for the character to exit")]
        public Transform exitMatchTarget;
        [Tooltip("Start the match target of the animation")]
        public float exitStartMatchTarget;
        [Tooltip("End the match target of the animation")]
        public float exitEndMatchTarget;

        [Tooltip("Use this to limit the trigger to active if forward of character is close to this forward")]
        public bool activeFromForward;
        [Tooltip("Rotate Character for this rotation when active")]
        public bool useTriggerRotation;

        public UnityEvent OnDoAction;
        public UnityEvent OnPlayerEnter;
        public UnityEvent OnPlayerStay;
        public UnityEvent OnPlayerExit;
    }
}