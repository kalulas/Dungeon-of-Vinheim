using UnityEngine;
using System.Collections.Generic;
using UnityEngine.Audio;

namespace Invector
{
    public class vAudioSurface : ScriptableObject
    {
        public AudioSource audioSource;
        public AudioMixerGroup audioMixerGroup;                 // The AudioSource that will play the clips.   
        public List<string> TextureOrMaterialNames;             // The tag on the surfaces that play these sounds.
        public List<AudioClip> audioClips;                      // The different clips that can be played on this surface.    
        public GameObject particleObject;

        private vFisherYatesRandom randomSource = new vFisherYatesRandom();       // For randomly reordering clips.   

        public bool useStepMark;
        [vHideInInspector("useStepMark")]
        public GameObject stepMark;
        [vHideInInspector("useStepMark")]
        public LayerMask stepLayer;
        [vHideInInspector("useStepMark")]
        public float timeToDestroy = 5f;

        public vAudioSurface()
        {
            audioClips = new List<AudioClip>();
            TextureOrMaterialNames = new List<string>();
        }

        public void PlayRandomClip(FootStepObject footStepObject, bool spawnParticle = true, bool spawnStepMark = true, float volume = 1f)
        {
            // if there are no clips to play return.
            if (audioClips == null || audioClips.Count == 0)
                return;

            // initialize variable if not already started
            if (randomSource == null)
                randomSource = new vFisherYatesRandom();

            // find a random clip and play it.
            GameObject audioObject = null;
            if (audioSource != null)
            {
                audioObject = Instantiate(audioSource.gameObject, footStepObject.sender.position, Quaternion.identity) as GameObject;
            }                
            else
            {
                audioObject = new GameObject("audioObject");
                audioObject.transform.position = footStepObject.sender.position;
            }

            var source = audioObject.AddComponent<vAudioSurfaceControl>();
            if (audioMixerGroup != null)
            {
                source.outputAudioMixerGroup = audioMixerGroup;
            }
            int index = randomSource.Next(audioClips.Count);
            
            if (spawnParticle && particleObject && footStepObject.ground && stepLayer.ContainsLayer(footStepObject.ground.gameObject.layer))
            {
              var obj =  Instantiate(particleObject, footStepObject.sender.position, footStepObject.sender.rotation) as GameObject;
                obj.transform.SetParent(vObjectContainer.root,true);
            }
            if (spawnStepMark && useStepMark)
                StepMark(footStepObject);

            source.PlayOneShot(audioClips[index], volume);
        }

        void StepMark(FootStepObject footStep)
        {
            RaycastHit hit;
            if (Physics.Raycast(footStep.sender.transform.position + new Vector3(0, 0.1f, 0), -footStep.sender.up, out hit, 1f, stepLayer))
            {
                if (stepMark)
                {
                    var angle = Quaternion.FromToRotation(footStep.sender.up, hit.normal);
                    var step = Instantiate(stepMark, hit.point, angle * footStep.sender.rotation) as GameObject;
                    step.transform.SetParent(vObjectContainer.root,true);                
                    Destroy(step, timeToDestroy);
                }
            }
        }
    }
}