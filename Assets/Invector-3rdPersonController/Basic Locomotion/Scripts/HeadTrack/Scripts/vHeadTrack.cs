using Invector.vEventSystems;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
namespace Invector.vCharacterController
{

    [vClassHeader("HEAD TRACK", iconName = "headTrackIcon")]
    public class vHeadTrack : vMonoBehaviour
    {
        #region variables

        [vEditorToolbar("Settings")]
        
        [vHelpBox("If your character is not looking up/down, try changing the axis", vHelpBoxAttribute.MessageType.Info)]
        public Vector3 upDownAxis = Vector3.right;

        [Header("Head & Body Weight")]
        public float strafeHeadWeight = 0.6f;
        public float strafeBodyWeight = 0.6f;
        public float aimingHeadWeight = 0.8f;
        public float aimingBodyWeight = 0.8f;
        public float freeHeadWeight = 0.6f;
        public float freeBodyWeight = 0.6f;
        [SerializeField] protected float smooth = 10f;

        [Header("Default Offsets ")]
        [SerializeField] protected Vector2 defaultOffsetSpine;
        [SerializeField] protected Vector2 defaultOffsetHead;
        
        [vReadOnly(true)]        
        public Vector2 offsetSpine;
        [vReadOnly(true)] 
        public Vector2 offsetHead;

        [Header("Tracking")]       
        [Tooltip("Follow the Camera Forward")]
        public bool followCamera = true;
        [vHideInInspector("followCamera")]
        [Tooltip("Force to follow camera")]
        public bool alwaysFollowCamera = false;
        [Tooltip("Ignore the Limits and continue to follow the camera")]
        public bool cancelTrackOutOfAngle = true;
        [Tooltip("Considerer the head animation forward while tracking, try it to see different results")]
        public bool considerHeadAnimationForward;

        [Header("Limits")]
        [vMinMax(minLimit = -180f, maxLimit = 180f)] public Vector2 horizontalAngleLimit = new Vector2(-100, 100);
        [vMinMax(minLimit = -90f, maxLimit = 90f)] public Vector2 verticalAngleLimit = new Vector2(-80, 80);

        [vHelpBox("Animations with vAnimatorTag Behavior will ignore the HeadTrack while is being played")]
        [Header("Ignore AnimatorTags")]        
        public List<string> animatorIgnoreTags = new List<string>() { "Attack", "LockMovement", "CustomAction", "IsEquipping", "IgnoreHeadtrack" };

        [vEditorToolbar("Bones")]
        [vHelpBox("Auto Find Bones using Humanoid")]
        public bool autoFindBones = true;
        public Transform head;
        public List<Transform> spine = new List<Transform>();

        [vEditorToolbar("Detection")]
        public float updateTargetInteration = 1;
        public float distanceToDetect = 10f;
        public LayerMask obstacleLayer = 1 << 0;
        [vHelpBox("Gameobjects Tags to detect")]
        public List<string> tagsToDetect = new List<string>() { "LookAt" };

        internal UnityEvent onInitUpdate = new UnityEvent();
        internal UnityEvent onFinishUpdate = new UnityEvent();
        internal Camera cameraMain;
        internal vLookTarget currentLookTarget;
        internal vLookTarget lastLookTarget;
        internal Vector3 currentLookPosition;
        internal Quaternion currentLookRotation;
        internal List<vLookTarget> targetsInArea = new List<vLookTarget>();
        internal bool ignoreSmooth = false;
        private float yRotation, xRotation;
        private float _currentHeadWeight, _currentbodyWeight;
        private Animator animator;
        private vAnimatorStateInfos animatorStateInfos;
        private float headHeight;
        private Transform simpleTarget;
        private Vector3 temporaryLookPoint;
        private float temporaryLookTime;
        private vHeadTrackSensor sensor;
        private float interation;
        private vICharacter vchar;
        private float yAngle, xAngle;
        private float _yAngle, _xAngle;
        private Transform forwardReference;
        #endregion
        public float Smooth
        {
          get {
                return ignoreSmooth ? 1f : smooth * Time.deltaTime;
            }
        }
        protected virtual void OnEnable()
        {
            if (animatorStateInfos != null && animator)
                animatorStateInfos.RegisterListener();
        }

        void Start()
        {
            if (!sensor)
            {
                var sensorObj = new GameObject("HeadTrackSensor");
                sensor = sensorObj.AddComponent<vHeadTrackSensor>();
            }

            // updates the headtrack using the late update of the tpinput so we don't need to create another one
            var tpInput = GetComponent<vThirdPersonInput>();
            if (tpInput)
            {
                tpInput.onLateUpdate -= UpdateHeadTrack;
                tpInput.onLateUpdate += UpdateHeadTrack;
            }

            vchar = GetComponent<vICharacter>();
            sensor.headTrack = this;
            animator = GetComponentInChildren<Animator>();
            if (animator)
            {
                animatorStateInfos = new vAnimatorStateInfos(animator);
                animatorStateInfos.RegisterListener();
            }

            if (autoFindBones)
            {
                head = animator.GetBoneTransform(HumanBodyBones.Head);
                if (head)
                {
                    forwardReference = new GameObject("FWRF").transform;
                    forwardReference.SetParent(head);
                    forwardReference.transform.localPosition = Vector3.zero;
                    forwardReference.transform.rotation = transform.rotation;
                    var hips = animator.GetBoneTransform(HumanBodyBones.Hips);
                    if (hips)
                    {
                        var target = head;
                        for (int i = 0; i < 4; i++)
                        {
                            if (target.parent && target.parent.gameObject != hips.gameObject)
                            {
                                spine.Add(target.parent);
                                target = target.parent;
                            }
                            else break;
                        }
                    }
                }
            }

            cameraMain = Camera.main;
            if (head)
            {
                headHeight = Vector3.Distance(transform.position, head.position);
                sensor.transform.position = head.transform.position;
            }
            else
            {
                headHeight = 1f;
                sensor.transform.position = transform.position;
            }
            if (spine.Count == 0)
            {
                Debug.Log("Headtrack Spines missing");
            }
            var layer = LayerMask.NameToLayer("HeadTrack");
            sensor.transform.parent = transform;
            sensor.gameObject.layer = layer;
            sensor.gameObject.tag = transform.tag;
            spine.Reverse();
            GetLookPoint();
        }

        Vector3 headPoint { get { return transform.position + (transform.up * headHeight); } }

        public virtual void UpdateHeadTrack()
        {
            if (animator == null || !animator.enabled) return;

            if (vchar != null && vchar.currentHealth > 0f && animator != null && !vchar.ragdolled)
            {
                onInitUpdate.Invoke();
                currentLookPosition = GetLookPoint();
                SetLookAtPosition(currentLookPosition, _currentHeadWeight, _currentbodyWeight);
                onFinishUpdate.Invoke();
            }
        }

        public virtual void SetLookAtPosition(Vector3 point, float headWeight, float spineWeight)
        {
            var lookRotation = Quaternion.LookRotation(point - headPoint);
            currentLookRotation = lookRotation;
            var euler = lookRotation.eulerAngles - transform.rotation.eulerAngles;
            var y = NormalizeAngle(euler.y);
            var x = NormalizeAngle(euler.x);
            var eulerB = considerHeadAnimationForward ? forwardReference.eulerAngles - transform.eulerAngles : Vector3.zero;
            xAngle = Mathf.Clamp(Mathf.Lerp(xAngle, ((x) - eulerB.NormalizeAngle().x) + Quaternion.Euler(offsetSpine + defaultOffsetSpine).eulerAngles.NormalizeAngle().x, Smooth), verticalAngleLimit.x, verticalAngleLimit.y);
            yAngle = Mathf.Clamp(Mathf.Lerp(yAngle, ((y) - eulerB.NormalizeAngle().y) + Quaternion.Euler(offsetSpine + defaultOffsetSpine).eulerAngles.NormalizeAngle().y, Smooth), horizontalAngleLimit.x, horizontalAngleLimit.y);
            var xSpine = NormalizeAngle(xAngle);
            var ySpine = NormalizeAngle(yAngle);

            foreach (Transform segment in spine)
            {
                var rotY = Quaternion.AngleAxis((ySpine * spineWeight) / spine.Count, segment.InverseTransformDirection(transform.up));
                segment.rotation *= rotY;
                var rotX = Quaternion.AngleAxis((xSpine * spineWeight) / spine.Count, segment.InverseTransformDirection(transform.TransformDirection(upDownAxis)));
                segment.rotation *= rotX;
            }
            if (head)
            {
                var xHead = NormalizeAngle(xAngle - (xSpine * spineWeight) + Quaternion.Euler(offsetHead + defaultOffsetHead).eulerAngles.NormalizeAngle().x);
                var yHead = NormalizeAngle(yAngle - (ySpine * spineWeight) + Quaternion.Euler(offsetHead + defaultOffsetHead).eulerAngles.NormalizeAngle().y);
                var _rotY = Quaternion.AngleAxis(yHead * headWeight, head.InverseTransformDirection(transform.up));
                head.rotation *= _rotY;
                var _rotX = Quaternion.AngleAxis(xHead * headWeight, head.InverseTransformDirection(transform.TransformDirection(upDownAxis)));
                head.rotation *= _rotX;
            }
        }

        bool lookConditions
        {
            get
            {
                if (!cameraMain)
                {
                    cameraMain = Camera.main;
                }
                return head != null && (followCamera && cameraMain != null) || (!followCamera && (currentLookTarget || simpleTarget)) || temporaryLookTime > 0;
            }
        }

        Vector3 GetLookPoint()
        {
            var distanceToLoock = 100;
            if (lookConditions && !IgnoreHeadTrack())
            {
                var dir = transform.forward;
                if (temporaryLookTime <= 0)
                {
                    var lookPosition = headPoint + (transform.forward * distanceToLoock);
                    if (followCamera)
                    {
                        lookPosition = (cameraMain.transform.position + (cameraMain.transform.forward * distanceToLoock));
                    }

                    dir = lookPosition - headPoint;
                    if ((followCamera && !alwaysFollowCamera) || !followCamera)
                    {
                        if (currentLookTarget != null && (currentLookTarget.ignoreHeadTrackAngle || TargetIsOnRange(currentLookTarget.lookPoint - headPoint)) && currentLookTarget.IsVisible(headPoint, obstacleLayer))
                        {
                            dir = currentLookTarget.lookPoint - headPoint;
                            if (currentLookTarget != lastLookTarget)
                            {
                                currentLookTarget.EnterLook(this);
                                lastLookTarget = currentLookTarget;
                            }
                        }

                        else if (simpleTarget != null)
                        {
                            dir = simpleTarget.position - headPoint;
                            if (currentLookTarget && currentLookTarget == lastLookTarget)
                            {
                                currentLookTarget.ExitLook(this);
                                lastLookTarget = null;
                            }
                        }
                        else if (currentLookTarget && currentLookTarget == lastLookTarget)
                        {
                            currentLookTarget.ExitLook(this);
                            lastLookTarget = null;
                        }
                    }
                }
                else
                {
                    dir = temporaryLookPoint - headPoint;
                    temporaryLookTime -= Time.deltaTime;
                    if (currentLookTarget && currentLookTarget == lastLookTarget)
                    {
                        currentLookTarget.ExitLook(this);
                        lastLookTarget = null;
                    }
                }

                var angle = GetTargetAngle(dir);
                if (cancelTrackOutOfAngle && (lastLookTarget == null || !lastLookTarget.ignoreHeadTrackAngle))
                {
                    if (TargetIsOnRange(dir))
                    {
                        if (animator.GetBool("IsStrafing") && !IsAnimatorTag("Upperbody Pose"))
                            SmoothValues(strafeHeadWeight, strafeBodyWeight, angle.x, angle.y);
                        else if (animator.GetBool("IsStrafing") && IsAnimatorTag("Upperbody Pose"))
                            SmoothValues(aimingHeadWeight, aimingBodyWeight, angle.x, angle.y);
                        else
                            SmoothValues(freeHeadWeight, freeBodyWeight, angle.x, angle.y);
                    }
                    else
                        SmoothValues();
                }
                else
                {
                    if (animator.GetBool("IsStrafing") && !IsAnimatorTag("Upperbody Pose"))
                        SmoothValues(strafeHeadWeight, strafeBodyWeight, angle.x, angle.y);
                    else if (animator.GetBool("IsStrafing") && IsAnimatorTag("Upperbody Pose"))
                        SmoothValues(aimingHeadWeight, aimingBodyWeight, angle.x, angle.y);
                    else
                        SmoothValues(freeHeadWeight, freeBodyWeight, angle.x, angle.y);
                }
                if (targetsInArea.Count > 1)
                    SortTargets();
            }
            else
            {
                SmoothValues();
                if (targetsInArea.Count > 1)
                    SortTargets();
            }

            var rotA = Quaternion.AngleAxis(yRotation, transform.up);
            var rotB = Quaternion.AngleAxis(xRotation, transform.right);
            var finalRotation = (rotA * rotB);
            var lookDirection = finalRotation * transform.forward;
            return headPoint + (lookDirection * distanceToLoock);
        }

        Vector2 GetTargetAngle(Vector3 direction)
        {
            var lookRotation = Quaternion.LookRotation(direction, transform.up);
            var angleResult = lookRotation.eulerAngles - transform.eulerAngles;

            return new Vector2(angleResult.NormalizeAngle().x, angleResult.NormalizeAngle().y);
        }

        bool TargetIsOnRange(Vector3 direction)
        {
            var angle = GetTargetAngle(direction);
            return (angle.x >= verticalAngleLimit.x && angle.x <= verticalAngleLimit.y && angle.y >= horizontalAngleLimit.x && angle.y <= horizontalAngleLimit.y);
        }

        /// <summary>
        /// Set vLookTarget
        /// </summary>
        /// <param name="target"></param>
        public virtual void SetLookTarget(vLookTarget target, bool priority = false)
        {
            if (!targetsInArea.Contains(target)) targetsInArea.Add(target);
            if (priority)
                currentLookTarget = target;
        }

        /// <summary>
        /// Set Simple target
        /// </summary>
        /// <param name="target"></param>
        public virtual void SetLookTarget(Transform target)
        {
            simpleTarget = target;
        }

        /// <summary>
        /// Set a temporary look point to headtrack   
        /// </summary>
        /// <param name="point">look point</param>
        /// <param name="time">time to stay looking</param>
        public virtual void SetTemporaryLookPoint(Vector3 point, float time = 1f)
        {
            temporaryLookPoint = point;
            temporaryLookTime = time;
        }

        public virtual void RemoveLookTarget(vLookTarget target)
        {
            if (targetsInArea.Contains(target)) targetsInArea.Remove(target);
            if (currentLookTarget == target) currentLookTarget = null;
        }

        public virtual void RemoveLookTarget(Transform target)
        {
            if (simpleTarget == target) simpleTarget = null;
        }

        /// <summary>
        /// Make angle to work with -180 and 180 
        /// </summary>
        /// <param name="angle"></param>
        /// <returns></returns>
        float NormalizeAngle(float angle)
        {
            if (angle > 180) angle -= 360;
            else if (angle < -180) angle += 360;

            return angle;
        }

        void ResetValues()
        {
            _currentHeadWeight = 0;
            _currentbodyWeight = 0;
            yRotation = 0;
            xRotation = 0;
        }

        void SmoothValues(float _headWeight = 0, float _bodyWeight = 0, float _x = 0, float _y = 0)
        {
            _currentHeadWeight = Mathf.Lerp(_currentHeadWeight, _headWeight, Smooth);
            _currentbodyWeight = Mathf.Lerp(_currentbodyWeight, _bodyWeight, Smooth);
            yRotation = Mathf.Lerp(yRotation, _y, Smooth);
            xRotation = Mathf.Lerp(xRotation, _x, Smooth);
            yRotation = Mathf.Clamp(yRotation, horizontalAngleLimit.x, horizontalAngleLimit.y);
            xRotation = Mathf.Clamp(xRotation, verticalAngleLimit.x, verticalAngleLimit.y);
        }

        void SortTargets()
        {
            interation += Time.deltaTime;
            if (interation > updateTargetInteration)
            {
                interation -= updateTargetInteration;
                if (targetsInArea == null || targetsInArea.Count < 2)
                {
                    if (targetsInArea != null && targetsInArea.Count > 0)
                        currentLookTarget = targetsInArea[0];
                    return;
                }

                for (int i = targetsInArea.Count - 1; i >= 0; i--)
                {
                    if (targetsInArea[i] == null)
                    {
                        targetsInArea.RemoveAt(i);
                    }
                }
                targetsInArea.Sort(delegate (vLookTarget c1, vLookTarget c2)
                {
                    return Vector3.Distance(this.transform.position, c1 != null ? c1.transform.position : Vector3.one * Mathf.Infinity).CompareTo
                        ((Vector3.Distance(this.transform.position, c2 != null ? c2.transform.position : Vector3.one * Mathf.Infinity)));
                });
                if (targetsInArea.Count > 0)
                {
                    currentLookTarget = targetsInArea[0];
                }
            }
        }

        public virtual void OnDetect(Collider other)
        {
            if (tagsToDetect.Contains(other.gameObject.tag) && other.GetComponent<vLookTarget>() != null)
            {
                currentLookTarget = other.GetComponent<vLookTarget>();
                var headTrack = other.GetComponentInParent<vHeadTrack>();
                if (!targetsInArea.Contains(currentLookTarget) && (headTrack == null || headTrack != this))
                {
                    targetsInArea.Add(currentLookTarget);
                    SortTargets();
                    currentLookTarget = targetsInArea[0];
                }
            }
        }

        public virtual void OnLost(Collider other)
        {
            if (tagsToDetect.Contains(other.gameObject.tag) && other.GetComponentInParent<vLookTarget>() != null)
            {
                var _currentLookTarget = other.GetComponentInParent<vLookTarget>();

                if (targetsInArea.Contains(_currentLookTarget))
                {
                    targetsInArea.Remove(_currentLookTarget);


                    if (_currentLookTarget == lastLookTarget)
                    {
                        _currentLookTarget.ExitLook(this);
                    }
                }
                SortTargets();
                if (targetsInArea.Count > 0)
                    currentLookTarget = targetsInArea[0];
                else
                    currentLookTarget = null;
            }
        }

        public virtual bool IgnoreHeadTrack()
        {
            if (animatorIgnoreTags.Exists(tag => IsAnimatorTag(tag)))
            {
                return true;
            }
            return false;
        }

        public virtual bool IsAnimatorTag(string tag)
        {
            if (animator == null) return false;
            if (animatorStateInfos != null)
            {
                if (animatorStateInfos.HasTag(tag))
                {
                    return true;
                }
            }
            return false;
        }
    }
}