using UnityEngine;
using UnityEditor;
using Invector.Utils;

namespace Invector.vCharacterController.vActions
{
    // BASIC FEATURES
    public partial class vMenuComponent
    {
        [MenuItem("Invector/Basic Locomotion/Actions/Generic Action")]
        static void GenericActionMenu()
        {
            if (Selection.activeGameObject)
                Selection.activeGameObject.AddComponent<vGenericAction>();
            else
                Debug.Log("Please select the Player to add this component.");
        }

        [MenuItem("Invector/Basic Locomotion/Components/Generic Animation")]
        static void GenericAnimationMenu()
        {
            if (Selection.activeGameObject)
                Selection.activeGameObject.AddComponent<vGenericAnimation>();
            else
                Debug.Log("Please select the Player to add this component.");
        }       

        [MenuItem("Invector/Basic Locomotion/Actions/Ladder Action")]
        static void LadderActionMenu()
        {
            if (Selection.activeGameObject)
                Selection.activeGameObject.AddComponent<vLadderAction>();
            else
                Debug.Log("Please select the Player to add this component.");
        }   

        [MenuItem("Invector/Basic Locomotion/Components/HitDamageParticle")]
        static void HitDamageMenu()
        {
            if (Selection.activeGameObject)
                Selection.activeGameObject.AddComponent<vHitDamageParticle>();
            else
                Debug.Log("Please select a vCharacter to add the component.");
        }

        //[MenuItem("Invector/Basic Locomotion/Components/MoveSetSpeed")]
        //static void MoveSetMenu()
        //{
        //    if (Selection.activeGameObject)
        //        Selection.activeGameObject.AddComponent<vMoveSetSpeed>();
        //    else
        //        Debug.Log("Please select the Player to add the component.");
        //}

        [MenuItem("Invector/Basic Locomotion/Components/HeadTrack")]
        static void HeadTrackMenu()
        {
            if (Selection.activeGameObject)
                Selection.activeGameObject.AddComponent<vHeadTrack>();
            else
                Debug.Log("Please select a vCharacter to add the component.");
        }

        [MenuItem("Invector/Basic Locomotion/Components/FootStep")]
        static void FootStepMenu()
        {
            if (Selection.activeGameObject)
                Selection.activeGameObject.AddComponent<vFootStep>();
            else
                Debug.Log("Please select a GameObject to add the component.");
        }

        [MenuItem("Invector/Basic Locomotion/Resources/New AudioSurface")]
        static void NewAudioSurface()
        {
            vScriptableObjectUtility.CreateAsset<vAudioSurface>();
        }

        [MenuItem("Invector/Basic Locomotion/Resources/New Ragdoll Generic Template")]
        static void RagdollGenericTemplate()
        {
            vScriptableObjectUtility.CreateAsset<vRagdollGenericTemplate>();
        }
    }
}