using System.Collections.Generic;
using UnityEngine;

namespace Invector
{
    public class vFootStep : vFootPlantingPlayer
    {
        public AnimationType animationType = AnimationType.Humanoid;
        public bool debugTextureName;

        [SerializeField, Range(0, 1f)]
        protected float _volume = 1f;
        public float volume { get { return _volume; } set { _volume = value; } }
        public bool spawnParticle { get; set; }
        public bool spawnStepMark { get; set; }

        private int surfaceIndex = 0;
        private Terrain terrain;
        private TerrainCollider terrainCollider;
        private TerrainData terrainData;
        private Vector3 terrainPos;

        public vFootStepTrigger leftFootTrigger;
        public vFootStepTrigger rightFootTrigger;
        public Transform currentStep;
        public List<vFootStepTrigger> footStepTriggers;

        void Start()
        {
            var colls = GetComponentsInChildren<Collider>();
            if (animationType == AnimationType.Humanoid)
            {
                if (leftFootTrigger == null && rightFootTrigger == null)
                {
                    Debug.Log("Missing FootStep Sphere Trigger, please unfold the FootStep Component to create the triggers.");
                    return;
                }
                else
                {
                    leftFootTrigger.trigger.isTrigger = true;
                    rightFootTrigger.trigger.isTrigger = true;
                    Physics.IgnoreCollision(leftFootTrigger.trigger, rightFootTrigger.trigger);
                    for (int i = 0; i < colls.Length; i++)
                    {
                        var coll = colls[i];
                        if (coll.enabled && coll.gameObject != leftFootTrigger.gameObject)
                            Physics.IgnoreCollision(leftFootTrigger.trigger, coll);
                        if (coll.enabled && coll.gameObject != rightFootTrigger.gameObject)
                            Physics.IgnoreCollision(rightFootTrigger.trigger, coll);
                    }
                }
            }
            else
            {
                for (int i = 0; i < colls.Length; i++)
                {
                    var coll = colls[i];
                    for (int a = 0; a < footStepTriggers.Count; a++)
                    {
                        var trigger = footStepTriggers[a];
                        trigger.trigger.isTrigger = true;
                        if (coll.enabled && coll.gameObject != trigger.gameObject)
                            Physics.IgnoreCollision(trigger.trigger, coll);
                    }
                }
            }
            spawnStepMark = true;
            spawnParticle = true;
        }

        private void UpdateTerrainInfo(Terrain newTerrain)
        {
            if (terrain == null || terrain != newTerrain)
            {
                terrain = newTerrain;
                if (terrain != null)
                {
                    terrainData = terrain.terrainData;
                    terrainPos = terrain.transform.position;
                    terrainCollider = terrain.GetComponent<TerrainCollider>();
                }
            }
        }

        private float[] GetTextureMix(FootStepObject footStepObj)
        {
            // returns an array containing the relative mix of textures
            // on the main terrain at this world position.

            // The number of values in the array will equal the number
            // of textures added to the terrain.

            UpdateTerrainInfo(footStepObj.terrain);

            // calculate which splat map cell the worldPos falls within (ignoring y)
            var worldPos = footStepObj.sender.position;
            int mapX = (int)(((worldPos.x - terrainPos.x) / terrainData.size.x) * terrainData.alphamapWidth);
            int mapZ = (int)(((worldPos.z - terrainPos.z) / terrainData.size.z) * terrainData.alphamapHeight);

            // get the splat data for this cell as a 1x1xN 3d array (where N = number of textures)
            if (!terrainCollider.bounds.Contains(worldPos)) return new float[0];
            float[,,] splatmapData = terrainData.GetAlphamaps(mapX, mapZ, 1, 1);

            // extract the 3D array data to a 1D array:
            float[] cellMix = new float[splatmapData.GetUpperBound(2) + 1];

            for (int n = 0; n < cellMix.Length; n++)
            {
                cellMix[n] = splatmapData[0, 0, n];
            }
            return cellMix;
        }

        private int GetMainTexture(FootStepObject footStepObj)
        {
            // returns the zero-based index of the most dominant texture
            // on the main terrain at this world position.
            float[] mix = GetTextureMix(footStepObj);

            if (mix == null)
                return -1;

            float maxMix = 0;
            int maxIndex = 0;

            // loop through each mix value and find the maximum
            for (int n = 0; n < mix.Length; n++)
            {
                if (mix[n] > maxMix)
                {
                    maxIndex = n;
                    maxMix = mix[n];
                }
            }
            return maxIndex;
        }

        /// <summary>
        /// Step on Terrain
        /// </summary>
        /// <param name="footStepObject"></param>
        public void StepOnTerrain(FootStepObject footStepObject)
        {
            if (currentStep != null && currentStep == footStepObject.sender) return;
            currentStep = footStepObject.sender;
            surfaceIndex = GetMainTexture(footStepObject);

            if (surfaceIndex != -1)
            {
#if UNITY_2018_3_OR_NEWER
                var name = (terrainData != null && terrainData.terrainLayers.Length > 0) ? (terrainData.terrainLayers[surfaceIndex]).diffuseTexture.name : "";
#else
                var name = (terrainData != null && terrainData.splatPrototypes.Length > 0) ? (terrainData.splatPrototypes[surfaceIndex]).texture.name : "";
#endif
                footStepObject.name = name;
                PlayFootFallSound(footStepObject, spawnParticle, spawnStepMark, volume);

                if (debugTextureName)
                    Debug.Log(terrain.name + " " + name);
            }
        }

        /// <summary>
        /// Step on Mesh
        /// </summary>
        /// <param name="footStepObject"></param>
        public void StepOnMesh(FootStepObject footStepObject)
        {
            if (currentStep != null && currentStep == footStepObject.sender) return;
            currentStep = footStepObject.sender;
            PlayFootFallSound(footStepObject, spawnParticle, spawnStepMark, volume);
            if (debugTextureName)
                Debug.Log(footStepObject.name);
        }

        private void OnDestroy()
        {
            if (leftFootTrigger != null)
                Destroy(leftFootTrigger.gameObject);
            if (rightFootTrigger != null)
                Destroy(rightFootTrigger.gameObject);

            if (footStepTriggers != null && footStepTriggers.Count > 0)
            {
                foreach (var comp in footStepTriggers)
                {
                    Destroy(comp.gameObject);
                }
            }
        }
    }

    public enum AnimationType
    {
        Humanoid, Generic
    }
}