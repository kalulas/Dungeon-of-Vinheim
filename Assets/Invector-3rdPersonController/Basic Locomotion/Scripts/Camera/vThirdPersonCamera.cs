using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace Invector.vCamera
{
    public class vThirdPersonCamera : MonoBehaviour
    {
        private static vThirdPersonCamera _instance;
        public static vThirdPersonCamera instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = GameObject.FindObjectOfType<vThirdPersonCamera>();

                    //Tell unity not to destroy this object when loading a new scene!
                    //DontDestroyOnLoad(_instance.gameObject);
                }

                return _instance;
            }
        }

        #region inspector properties    
        public Transform target;
       
        [Tooltip("Lerp speed between Camera States")]
        public float smoothBetweenState = 6f;
        public float smoothCameraRotation = 6f;
        public float scrollSpeed = 10f;

        [Tooltip("What layer will be culled")]
        public LayerMask cullingLayer = 1 << 0;
        [Tooltip("Change this value If the camera pass through the wall")]
        public float clipPlaneMargin;
        public float checkHeightRadius;
        public bool showGizmos;
        public bool startUsingTargetRotation = true;
        public bool startSmooth = false;
        [Tooltip("Debug purposes, lock the camera behind the character for better align the states")]
        public bool lockCamera;

        public Vector2 offsetMouse;

        #endregion

        #region hide properties    
        [HideInInspector]
        public int indexList, indexLookPoint;
        [HideInInspector]
        public float offSetPlayerPivot;
        [HideInInspector]
        public float distance = 5f;
        [HideInInspector]
        public string currentStateName;
        [HideInInspector]
        public Transform currentTarget;
        [HideInInspector]
        public vThirdPersonCameraState currentState;
        [HideInInspector]
        public vThirdPersonCameraListData CameraStateList;
        [HideInInspector]
        public Transform lockTarget;
        [HideInInspector]
        public Vector2 movementSpeed;
        [HideInInspector]
        public vThirdPersonCameraState lerpState;
        private Transform targetLookAt;
        private Vector3 currentTargetPos;
        private Vector3 lookPoint;
        private Vector3 current_cPos;
        private Vector3 desired_cPos;
        private Vector3 lookTargetAdjust;
        private Camera targetCamera;
        private float mouseY = 0f;
        private float mouseX = 0f;
        private float currentHeight;
        private float currentZoom;
        private float cullingHeight;
        private float cullingDistance;
        private float switchRight, currentSwitchRight;
        private float heightOffset;
        private bool isInit;
        private bool useSmooth;
        private bool isNewTarget;
        private bool firstStateIsInit;
        private Quaternion fixedRotation;

        public float CullingDistance
        {
            get
            {
                return cullingDistance;
            }
        }
        #endregion
      
        private Vector3 cameraVelocityDamp;
        private bool firstUpdated;
        void OnDrawGizmos()
        {
            if (showGizmos)
            {
                if (currentTarget)
                {
                    var targetPos = new Vector3(currentTarget.position.x, currentTarget.position.y + offSetPlayerPivot, currentTarget.position.z);
                    Gizmos.DrawWireSphere(targetPos + Vector3.up * cullingHeight, checkHeightRadius);
                    Gizmos.DrawLine(targetPos, targetPos + Vector3.up * cullingHeight);
                }
            }
        }

        void Start()
        {
            Init();
        }

        public void Init()
        {
            if (target == null)
                return;
            firstUpdated = true;
            useSmooth = true;
            if (!targetLookAt)targetLookAt = new GameObject("targetLookAt").transform;

          
            targetLookAt.rotation = transform.rotation;
            targetLookAt.hideFlags = HideFlags.HideInHierarchy;
            if (startSmooth)
                distance = Vector3.Distance(targetLookAt.position, transform.position);
            if(!targetCamera) targetCamera = Camera.main;
            currentTarget = target;
            switchRight = 1f;
            currentSwitchRight = 1f;
            if (startUsingTargetRotation)
            {
                transform.rotation = currentTarget.rotation;
                mouseY = currentTarget.root.eulerAngles.x;
                mouseX = currentTarget.root.eulerAngles.y;
            }
            else
            {
                mouseY = transform.root.eulerAngles.x;
                mouseX = transform.root.eulerAngles.y;
            }
            ChangeState("Default", startSmooth);
            currentZoom = currentState.defaultDistance;
            currentHeight = currentState.height;
          
            currentTargetPos = new Vector3(currentTarget.position.x, currentTarget.position.y + offSetPlayerPivot, currentTarget.position.z) + currentTarget.transform.up * lerpState.height;
            targetLookAt.position = currentTargetPos;      

            isInit = true;
        }

        void FixedUpdate()
        {
            if (target == null || targetLookAt == null || currentState == null || lerpState == null || !isInit) return;

            switch (currentState.cameraMode)
            {
                case TPCameraMode.FreeDirectional:
                    CameraMovement();
                    break;
                case TPCameraMode.FixedAngle:
                    CameraMovement();
                    break;
                case TPCameraMode.FixedPoint:
                    CameraFixed();
                    break;
            }
        }

        public void SetLockTarget(Transform _lockTarget, float heightOffset)
        {
            if (lockTarget != null && lockTarget == _lockTarget) return;
            isNewTarget = _lockTarget != lockTarget;
            lockTarget = _lockTarget;
            this.heightOffset = heightOffset;
        }

        public void RemoveLockTarget()
        {
            lockTarget = null;
        }

        /// <summary>
        /// Set the target for the camera
        /// </summary>
        /// <param name="New cursorObject"></param>
        public void SetTarget(Transform newTarget)
        {
            currentTarget = newTarget ? newTarget : target;
        }

        public void SetMainTarget(Transform newTarget)
        {
            target = newTarget;
            currentTarget = newTarget;
            if (!isInit) Init();
        }

        /// <summary>    
        /// Convert a point in the screen in a Ray for the world
        /// </summary>
        /// <param name="Point"></param>
        /// <returns></returns>
        public Ray ScreenPointToRay(Vector3 Point)
        {
            return this.GetComponent<Camera>().ScreenPointToRay(Point);
        }

        /// <summary>
        /// Change CameraState
        /// </summary>
        /// <param name="stateName"></param>       
        public void ChangeState(string stateName)
        {
            ChangeState(stateName, true);
        }

        /// <summary>
        /// Change CameraState
        /// </summary>
        /// <param name="stateName"></param>
        /// <param name="Use smoth"></param>
        public void ChangeState(string stateName, bool hasSmooth)
        {
            if (currentState != null && currentState.Name.Equals(stateName) || !isInit && firstStateIsInit)
            {
                if (firstStateIsInit) useSmooth = hasSmooth;
                return;
            }
            useSmooth = !firstStateIsInit ? startSmooth : hasSmooth;
            // search for the camera state string name
            vThirdPersonCameraState state = CameraStateList != null ? CameraStateList.tpCameraStates.Find(delegate (vThirdPersonCameraState obj) { return obj.Name.Equals(stateName); }) : new vThirdPersonCameraState("Default");

            if (state != null)
            {
                currentStateName = stateName;
                currentState.cameraMode = state.cameraMode;
                lerpState = state; // set the state of transition (lerpstate) to the state finded on the list
                if (!firstStateIsInit)
                {                   
                    currentState.defaultDistance = Vector3.Distance(targetLookAt.position, transform.position);
                    currentState.forward = lerpState.forward;
                    currentState.height = state.height;
                    currentState.fov = state.fov;
                    if (useSmooth)
                        StartCoroutine(ResetFirstState());
                    else
                    {
                        distance = lerpState.defaultDistance;
                        firstStateIsInit = true;
                    }
                }
                // in case there is no smooth, a copy will be make without the transition values
                if (currentState != null && !useSmooth)
                    currentState.CopyState(state);
            }
            else
            {
                // if the state choosed if not real, the first state will be set up as default
                if (CameraStateList != null && CameraStateList.tpCameraStates.Count > 0)
                {
                    state = CameraStateList.tpCameraStates[0];
                    currentStateName = state.Name;
                    currentState.cameraMode = state.cameraMode;
                    lerpState = state;

                    if (currentState != null && !useSmooth)
                        currentState.CopyState(state);
                }
            }
            // in case a list of states does not exist, a default state will be created
            if (currentState == null)
            {
                currentState = new vThirdPersonCameraState("Null");
                currentStateName = currentState.Name;
            }
            if (CameraStateList != null)
                indexList = CameraStateList.tpCameraStates.IndexOf(state);
            currentZoom = state.defaultDistance;

            if (currentState.cameraMode == TPCameraMode.FixedAngle)
            {
                mouseX = currentState.fixedAngle.x;
                mouseY = currentState.fixedAngle.y;
            }

            currentState.fixedAngle = new Vector3(mouseX, mouseY);
            indexLookPoint = 0;
            if (!isInit) CameraMovement(true);
        }

        /// <summary>
        /// Change State using look at point if the cameraMode is FixedPoint  
        /// </summary>
        /// <param name="stateName"></param>
        /// <param name="pointName"></param>
        /// <param name="hasSmooth"></param>
        public void ChangeState(string stateName, string pointName, bool hasSmooth)
        {
            useSmooth = hasSmooth;
            if (!currentState.Name.Equals(stateName))
            {
                // search for the camera state string name
                var state = CameraStateList.tpCameraStates.Find(delegate (vThirdPersonCameraState obj)
               {
                   return obj.Name.Equals(stateName);
               });

                if (state != null)
                {
                    currentStateName = stateName;
                    currentState.cameraMode = state.cameraMode;
                    lerpState = state; // set the state of transition (lerpstate) to the state finded on the list
                                       // in case there is no smooth, a copy will be make without the transition values
                    if (currentState != null && !hasSmooth)
                        currentState.CopyState(state);
                }
                else
                {
                    // if the state choosed if not real, the first state will be set up as default
                    if (CameraStateList.tpCameraStates.Count > 0)
                    {
                        state = CameraStateList.tpCameraStates[0];
                        currentStateName = state.Name;
                        currentState.cameraMode = state.cameraMode;
                        lerpState = state;
                        if (currentState != null && !hasSmooth)
                            currentState.CopyState(state);
                    }
                }
                // in case a list of states does not exist, a default state will be created
                if (currentState == null)
                {
                    currentState = new vThirdPersonCameraState("Null");
                    currentStateName = currentState.Name;
                }

                indexList = CameraStateList.tpCameraStates.IndexOf(state);
                currentZoom = state.defaultDistance;
                currentState.fixedAngle = new Vector3(mouseX, mouseY);
                indexLookPoint = 0;
            }

            if (currentState.cameraMode == TPCameraMode.FixedPoint)
            {
                var point = currentState.lookPoints.Find(delegate (LookPoint obj)
               {
                   return obj.pointName.Equals(pointName);
               });
                if (point != null)
                {
                    indexLookPoint = currentState.lookPoints.IndexOf(point);
                }
                else
                {
                    indexLookPoint = 0;
                }
            }
        }

        IEnumerator ResetFirstState()
        {
            yield return new WaitForEndOfFrame();
            firstStateIsInit = true;
        }

        /// <summary>
        /// Change the lookAtPoint of current state if cameraMode is FixedPoint
        /// </summary>
        /// <param name="pointName"></param>
        public void ChangePoint(string pointName)
        {
            if (currentState == null || currentState.cameraMode != TPCameraMode.FixedPoint || currentState.lookPoints == null) return;
            var point = currentState.lookPoints.Find(delegate (LookPoint obj) { return obj.pointName.Equals(pointName); });
            if (point != null) indexLookPoint = currentState.lookPoints.IndexOf(point); else indexLookPoint = 0;
        }

        /// <summary>    
        /// Zoom baheviour 
        /// </summary>
        /// <param name="scroolValue"></param>
        /// <param name="zoomSpeed"></param>
        public void Zoom(float scroolValue)
        {
            currentZoom -= scroolValue * scrollSpeed;
        }

        /// <summary>
        /// Camera Rotation behaviour
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        public void RotateCamera(float x, float y)
        {
            if (currentState.cameraMode.Equals(TPCameraMode.FixedPoint) || !isInit) return;
            if (!currentState.cameraMode.Equals(TPCameraMode.FixedAngle))
            {
                // lock into a target            
                if (!lockTarget)
                {
                    // free rotation 
                    mouseX += x * currentState.xMouseSensitivity;
                    mouseY -= y * currentState.yMouseSensitivity;

                    movementSpeed.x = x;
                    movementSpeed.y = -y;
                    if (!lockCamera)
                    {
                        mouseY = vExtensions.ClampAngle(mouseY, lerpState.yMinLimit, lerpState.yMaxLimit);
                        mouseX = vExtensions.ClampAngle(mouseX, lerpState.xMinLimit, lerpState.xMaxLimit);
                    }
                    else
                    {
                        mouseY = currentTarget.root.eulerAngles.NormalizeAngle().x;
                        mouseX = currentTarget.root.eulerAngles.NormalizeAngle().y;
                    }
                }
            }
            else
            {
                // fixed rotation
                var _x = lerpState.fixedAngle.x;
                var _y = lerpState.fixedAngle.y;
                mouseX = useSmooth ? Mathf.LerpAngle(mouseX, _x, smoothBetweenState * Time.deltaTime) : _x;
                mouseY = useSmooth ? Mathf.LerpAngle(mouseY, _y, smoothBetweenState * Time.deltaTime) : _y;
            }

        }

        /// <summary>
        /// Switch Camera Right 
        /// </summary>
        /// <param name="value"></param>
        public void SwitchRight(bool value = false)
        {
            switchRight = value ? -1 : 1;
        }

        void CalculeLockOnPoint()
        {
            if (currentState.cameraMode.Equals(TPCameraMode.FixedAngle) && lockTarget) return;   // check if angle of camera is fixed         
            var collider = lockTarget.GetComponent<Collider>();                                  // collider to get center of bounds

            if (collider == null)
            {
                return;
            }

            var _point = collider.bounds.center;
            Vector3 relativePos = _point - (desired_cPos);                      // get position relative to transform
            Quaternion rotation = Quaternion.LookRotation(relativePos);         // convert to rotation

            //convert angle (360 to 180)
            var y = 0f;
            var x = rotation.eulerAngles.y;
            if (rotation.eulerAngles.x < -180)
                y = rotation.eulerAngles.x + 360;
            else if (rotation.eulerAngles.x > 180)
                y = rotation.eulerAngles.x - 360;
            else
                y = rotation.eulerAngles.x;

            mouseY = vExtensions.ClampAngle(y, currentState.yMinLimit, currentState.yMaxLimit);
            mouseX = vExtensions.ClampAngle(x, currentState.xMinLimit, currentState.xMaxLimit);
        }

        void CameraMovement(bool forceUpdate = false)
        {
            if (currentTarget == null || targetCamera == null ||(!firstStateIsInit &&!forceUpdate))
                return;

            if (useSmooth)
            {
                currentState.Slerp(lerpState, smoothBetweenState * Time.deltaTime);
            }
            else
                currentState.CopyState(lerpState);

            if (currentState.useZoom)
            {
                currentZoom = Mathf.Clamp(currentZoom, currentState.minDistance, currentState.maxDistance);
                distance = useSmooth ? Mathf.Lerp(distance, currentZoom, lerpState.smooth * Time.deltaTime) : currentZoom;
            }
            else
            {
                distance = useSmooth ? Mathf.Lerp(distance, currentState.defaultDistance, lerpState.smooth * Time.deltaTime) : currentState.defaultDistance;
                currentZoom = currentState.defaultDistance;
            }
            targetCamera.fieldOfView = currentState.fov;
            cullingDistance = Mathf.Lerp(cullingDistance, currentZoom, smoothBetweenState * Time.deltaTime);
            currentSwitchRight = Mathf.Lerp(currentSwitchRight, switchRight, smoothBetweenState * Time.deltaTime);
            var camDir = (currentState.forward * targetLookAt.forward) + ((currentState.right * currentSwitchRight) * targetLookAt.right);

            camDir = camDir.normalized;

            var targetPos = new Vector3(currentTarget.position.x, currentTarget.position.y, currentTarget.position.z) + currentTarget.transform.up * offSetPlayerPivot;
            currentTargetPos = targetPos;
            desired_cPos = targetPos + currentTarget.transform.up * currentState.height;
            current_cPos = firstUpdated ? targetPos + currentTarget.transform.up * currentHeight:  Vector3.SmoothDamp(current_cPos, targetPos + currentTarget.transform.up * currentHeight,ref cameraVelocityDamp, lerpState.smoothDamp*Time.deltaTime);
            firstUpdated = false;
            RaycastHit hitInfo;

            ClipPlanePoints planePoints = targetCamera.NearClipPlanePoints(current_cPos + (camDir * (distance)), clipPlaneMargin);
            ClipPlanePoints oldPoints = targetCamera.NearClipPlanePoints(desired_cPos + (camDir * currentZoom), clipPlaneMargin);
            //Check if Height is not blocked 
            if (Physics.SphereCast(targetPos, checkHeightRadius, currentTarget.transform.up, out hitInfo, currentState.cullingHeight + 0.2f, cullingLayer))
            {
                var t = hitInfo.distance - 0.2f;
                t -= currentState.height;
                t /= (currentState.cullingHeight - currentState.height);
                cullingHeight = Mathf.Lerp(currentState.height, currentState.cullingHeight, Mathf.Clamp(t, 0.0f, 1.0f));
            }
            else
            {
                cullingHeight = useSmooth ? Mathf.Lerp(cullingHeight, currentState.cullingHeight, smoothBetweenState * Time.deltaTime) : currentState.cullingHeight;
            }
            //Check if desired target position is not blocked       
            if (CullingRayCast(desired_cPos, oldPoints, out hitInfo, currentZoom + 0.2f, cullingLayer, Color.blue))
            {
                distance = hitInfo.distance - 0.2f;
                if (distance < currentState.defaultDistance)
                {
                    var t = hitInfo.distance;
                    t -= currentState.cullingMinDist;
                    t /= (currentZoom - currentState.cullingMinDist);
                    currentHeight = Mathf.Lerp(cullingHeight, currentState.height, Mathf.Clamp(t, 0.0f, 1.0f));
                    current_cPos = targetPos + currentTarget.transform.up * currentHeight;
                }
            }
            else
            {
                currentHeight = useSmooth ? Mathf.Lerp(currentHeight, currentState.height, smoothBetweenState * Time.deltaTime) : currentState.height;
            }
            //Check if target position with culling height applied is not blocked
            if (CullingRayCast(current_cPos, planePoints, out hitInfo, distance, cullingLayer, Color.cyan)) distance = Mathf.Clamp(cullingDistance, 0.0f, currentState.defaultDistance);
            var lookPoint = current_cPos + targetLookAt.forward * 2f;
            lookPoint += (targetLookAt.right * Vector3.Dot(camDir * (distance), targetLookAt.right));
            targetLookAt.position = current_cPos;

            Quaternion newRot = Quaternion.Euler(mouseY + offsetMouse.y, mouseX + offsetMouse.x, 0);
            targetLookAt.rotation = useSmooth ? Quaternion.Lerp(targetLookAt.rotation, newRot, smoothCameraRotation * Time.deltaTime) : newRot;
            transform.position = current_cPos + (camDir * (distance));
            var rotation = Quaternion.LookRotation((lookPoint) - transform.position);

            if (lockTarget)
            {
                CalculeLockOnPoint();

                if (!(currentState.cameraMode.Equals(TPCameraMode.FixedAngle)))
                {
                    var collider = lockTarget.GetComponent<Collider>();
                    if (collider != null)
                    {
                        var point = (collider.bounds.center + Vector3.up * heightOffset) - transform.position;
                        var euler = Quaternion.LookRotation(point).eulerAngles - rotation.eulerAngles;
                        if (isNewTarget)
                        {
                            lookTargetAdjust.x = Mathf.LerpAngle(lookTargetAdjust.x, euler.x, currentState.smooth * Time.deltaTime);
                            lookTargetAdjust.y = Mathf.LerpAngle(lookTargetAdjust.y, euler.y, currentState.smooth * Time.deltaTime);
                            lookTargetAdjust.z = Mathf.LerpAngle(lookTargetAdjust.z, euler.z, currentState.smooth * Time.deltaTime);
                            // Quaternion.LerpUnclamped(lookTargetAdjust, Quaternion.Euler(euler), currentState.smoothFollow * Time.deltaTime);
                            if (Vector3.Distance(lookTargetAdjust, euler) < .5f)
                                isNewTarget = false;
                        }
                        else
                            lookTargetAdjust = euler;
                    }
                }
            }
            else
            {
                lookTargetAdjust.x = Mathf.LerpAngle(lookTargetAdjust.x, 0, currentState.smooth * Time.deltaTime);
                lookTargetAdjust.y = Mathf.LerpAngle(lookTargetAdjust.y, 0, currentState.smooth * Time.deltaTime);
                lookTargetAdjust.z = Mathf.LerpAngle(lookTargetAdjust.z, 0, currentState.smooth * Time.deltaTime);
                //lookTargetAdjust = Quaternion.LerpUnclamped(lookTargetAdjust, Quaternion.Euler(Vector3.zero), 1 * Time.deltaTime);
            }
            var _euler = rotation.eulerAngles + lookTargetAdjust;
            _euler.z = 0;
            var _rot = Quaternion.Euler(_euler + currentState.rotationOffSet);
            transform.rotation = _rot;
            movementSpeed = Vector2.zero;
        }

        void CameraFixed()
        {
            if (useSmooth) currentState.Slerp(lerpState, smoothBetweenState);
            else currentState.CopyState(lerpState);

            var targetPos = new Vector3(currentTarget.position.x, currentTarget.position.y + offSetPlayerPivot + currentState.height, currentTarget.position.z);
            currentTargetPos = useSmooth ? Vector3.MoveTowards(currentTargetPos, targetPos, currentState.smooth * Time.deltaTime) : targetPos;
            current_cPos = currentTargetPos;
            var pos = isValidFixedPoint ? currentState.lookPoints[indexLookPoint].positionPoint : transform.position;
            transform.position = useSmooth ? Vector3.Lerp(transform.position, pos, currentState.smooth * Time.deltaTime) : pos;
            targetLookAt.position = current_cPos;
            if (isValidFixedPoint && currentState.lookPoints[indexLookPoint].freeRotation)
            {
                var rot = Quaternion.Euler(currentState.lookPoints[indexLookPoint].eulerAngle);
                transform.rotation = useSmooth ? Quaternion.Slerp(transform.rotation, rot, (currentState.smooth * 0.5f) * Time.deltaTime) : rot;
            }
            else if (isValidFixedPoint)
            {
                var rot = Quaternion.LookRotation(currentTargetPos - transform.position);
                transform.rotation = useSmooth ? Quaternion.Slerp(transform.rotation, rot, (currentState.smooth) * Time.deltaTime) : rot;
            }
            targetCamera.fieldOfView = currentState.fov;
        }

        bool isValidFixedPoint
        {
            get
            {
                return (currentState.lookPoints != null && currentState.cameraMode.Equals(TPCameraMode.FixedPoint) && (indexLookPoint < currentState.lookPoints.Count || currentState.lookPoints.Count > 0));
            }
        }

        bool CullingRayCast(Vector3 from, ClipPlanePoints _to, out RaycastHit hitInfo, float distance, LayerMask cullingLayer, Color color)
        {
            bool value = false;
            if (showGizmos)
            {
                Debug.DrawRay(from, _to.LowerLeft - from, color);
                Debug.DrawLine(_to.LowerLeft, _to.LowerRight, color);
                Debug.DrawLine(_to.UpperLeft, _to.UpperRight, color);
                Debug.DrawLine(_to.UpperLeft, _to.LowerLeft, color);
                Debug.DrawLine(_to.UpperRight, _to.LowerRight, color);
                Debug.DrawRay(from, _to.LowerRight - from, color);
                Debug.DrawRay(from, _to.UpperLeft - from, color);
                Debug.DrawRay(from, _to.UpperRight - from, color);
            }
            if (Physics.Raycast(from, _to.LowerLeft - from, out hitInfo, distance, cullingLayer))
            {
                value = true;
                cullingDistance = hitInfo.distance;
            }

            if (Physics.Raycast(from, _to.LowerRight - from, out hitInfo, distance, cullingLayer))
            {
                value = true;
                if (cullingDistance > hitInfo.distance) cullingDistance = hitInfo.distance;
            }

            if (Physics.Raycast(from, _to.UpperLeft - from, out hitInfo, distance, cullingLayer))
            {
                value = true;
                if (cullingDistance > hitInfo.distance) cullingDistance = hitInfo.distance;
            }

            if (Physics.Raycast(from, _to.UpperRight - from, out hitInfo, distance, cullingLayer))
            {
                value = true;
                if (cullingDistance > hitInfo.distance) cullingDistance = hitInfo.distance;
            }

            return hitInfo.collider && value;
        }
    }
}