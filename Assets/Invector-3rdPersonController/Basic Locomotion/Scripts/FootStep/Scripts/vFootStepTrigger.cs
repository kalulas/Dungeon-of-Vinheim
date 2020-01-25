using UnityEngine;

namespace Invector
{
    public class vFootStepTrigger : MonoBehaviour
    {
        protected Collider _trigger;
        protected vFootStep _fT;

        void OnDrawGizmos()
        {
            if (!trigger) return;
            Color color = Color.green;
            color.a = 0.5f;
            Gizmos.color = color;
            if (trigger is SphereCollider)
            {
                Gizmos.DrawSphere((trigger.bounds.center), (trigger as SphereCollider).radius);
            }
        }

        void Start()
        {
            _fT = GetComponentInParent<vFootStep>();
            if (_fT == null)
            {
                Debug.Log(gameObject.name + " can't find the FootStepFromTexture");
                gameObject.SetActive(false);
            }
        }

        public Collider trigger
        {
            get
            {
                if (_trigger == null) _trigger = GetComponent<Collider>();
                return _trigger;
            }
        }

        protected Collider lastCollider;
        protected FootStepObject footstepObj;

        void OnTriggerEnter(Collider other)
        {
            if (_fT == null) return;

            if ((lastCollider == null || lastCollider != other) || footstepObj == null)
            {
                footstepObj = new FootStepObject(transform, other);
                lastCollider = other;
            }            

            if (footstepObj.isTerrain) //Check if trigger objet is a terrain
            {
                _fT.StepOnTerrain(footstepObj);
            }
            else
            {                                             
                _fT.StepOnMesh(footstepObj);             
            }
        }
    }

    /// <summary>
    /// Foot step Object work with FootStepFromTexture
    /// </summary>
    public class FootStepObject
    {
        public string name;
        public Transform sender;
        public Collider ground;
        public Terrain terrain;
        public bool isTerrain { get { return terrain != null; } }
        public vFootStepHandler stepHandle;
        public Renderer renderer;

        public FootStepObject(Transform sender, Collider ground)
        {            
            this.name = "";
            this.sender = sender;
            this.ground = ground;
            this.terrain = ground.GetComponent<Terrain>();
            this.stepHandle = ground.GetComponent<vFootStepHandler>();
            this.renderer = ground.GetComponent<Renderer>();

            if (renderer != null && renderer.material != null)
            {
                var index = 0;
                this.name = string.Empty;
                if (stepHandle != null && stepHandle.material_ID > 0)// if trigger contains a StepHandler to pass material ID. Default is (0)
                    index = stepHandle.material_ID;
                if (stepHandle)
                {
                    // check  stepHandlerType
                    switch (stepHandle.stepHandleType)
                    {
                        case vFootStepHandler.StepHandleType.materialName:
                            this.name = renderer.materials[index].name;
                            break;
                        case vFootStepHandler.StepHandleType.textureName:
                            this.name = renderer.materials[index].mainTexture.name;
                            break;
                    }
                }
                else
                    this.name = renderer.materials[index].name;
            }
        }
    }
}