using UnityEngine;
using System.Collections;
using System.Collections.Generic;

[RequireComponent(typeof(RPGViewFrustum))]

public class RPGCamera : MonoBehaviour {

	public Camera UsedCamera;
	public Material UsedSkybox;
	public Vector3 CameraPivotLocalPosition = new Vector3(0, 1.1f, 0);
	public bool ActivateCameraControl = true;
	public bool AlwaysRotateCamera = false;
	public RotateWithCharacter RotateWithCharacter = RotateWithCharacter.RotationStoppingInput;
	public string RotationStoppingInput = "Fire1";
	public CursorLockMode CursorLockMode = CursorLockMode.Confined;
	public bool HideCursorWhenPressed = true;
	public bool LockMouseX = false;
	public bool LockMouseY = false;
	public bool InvertMouseX = false;
	public bool InvertMouseY = true;
	public float MouseXSensitivity = 8.0f;
	public float MouseYSensitivity = 8.0f;
	public bool ConstrainMouseX = false;
	public float MouseXMin = -90.0f;
	public float MouseXMax = 90.0f;
	public float MouseYMin = -89.5f;
	public float MouseYMax = 89.5f;
	public float MouseScrollSensitivity = 15.0f;
	public float MouseSmoothTime = 0.08f;
	public float MinDistance = 0;
	public float MaxDistance = 20.0f;
	public float DistanceSmoothTime = 0.7f;
	public float StartMouseX = 0;
	public float StartMouseY = 15.0f;
	public float StartDistance = 2.0f;
	public AlignCharacter AlignCharacter = AlignCharacter.OnAlignmentInput;
	public string AlignmentInput = "Fire2";
	public bool AlignCameraWhenMoving = true;
	public bool SupportWalkingBackwards = true;
	public float AlignCameraSmoothTime = 0.2f;

	private Skybox _skybox;
	private bool _skyboxChanged = false;
	// Camera pivot position in world coordinates
	private Vector3 _cameraPivotPosition;
	// Used view frustum script for camera distance/constraints computations
	private RPGViewFrustum _rpgViewFrustum;
	// Reference to the RPGMotor script
	private RPGMotor _rpgMotor;
	// Desired camera position, can be unequal to the current position because of ambient occlusion
	private Vector3 _desiredPosition;
	// Analogous to _desiredPosition
	private float _desiredDistance;
	private float _distanceSmooth = 0;
	private float _distanceCurrentVelocity;
	// If true, automatically align the camera with the character
	private bool _alignCameraWithCharacter = false;
	// Current mouse/camera X rotation
	private float _mouseX = 0;
	private float _mouseXSmooth = 0;
	private float _mouseXCurrentVelocity;
	// Current mouse/camera Y rotation
	private float _mouseY = 0;
	private float _mouseYSmooth = 0;
	private float _mouseYCurrentVelocity;
	// Desired mouse/camera Y rotation, as the Y rotation can be constrained by terrain
	private float _desiredMouseY = 0;
	// If true, the character was already aligned with the camera 
	private bool _characterAligned = false;

	private void Awake() {
		// Check if there is a prescribed camera to use
		if (UsedCamera == null) {
			// Create one for usage in the following code
			GameObject camObject = new GameObject(transform.name + transform.GetInstanceID() + " Camera");
			camObject.AddComponent<Camera>();
			camObject.AddComponent<FlareLayer>();
			camObject.AddComponent<Skybox>();
			_skybox = camObject.GetComponent<Skybox>();
			UsedCamera = camObject.GetComponent<Camera>();
			UsedCamera.transform.eulerAngles = new Vector3(0, transform.eulerAngles.y, 0);
		}
		
		_skybox = UsedCamera.GetComponent<Skybox>();
		// Check if the used camera has a skybox attached
		if (_skybox == null) {
			// No skybox attached => add a skybox and assign it to the _skybox variable
			UsedCamera.gameObject.AddComponent<Skybox>();
			_skybox = UsedCamera.gameObject.GetComponent<Skybox>();
		}		
		// Set the used camera's skybox to the user prescribed one
		_skybox.material = UsedSkybox;

		ResetView();
		// Assign the remaining script variables
		_rpgViewFrustum = GetComponent<RPGViewFrustum>();
		_rpgMotor = GetComponent<RPGMotor>();		
	}

	private void LateUpdate() {
		// Make AlwaysRotateCamera and AlignCameraWhenMoving mutual exclusive
		if (AlwaysRotateCamera) {
			AlignCameraWhenMoving = false;
		}
		
		// Check if the UsedSkybox variable has been changed through SetUsedSkybox()
		if (_skyboxChanged) {
			// Update the used camera's skybox
			_skybox.material = UsedSkybox;
			_skyboxChanged = false;
		}		
			
		// Set the camera's pivot position in world coordinates
		_cameraPivotPosition = transform.position + transform.TransformVector(CameraPivotLocalPosition);
		
		// Check if the camera's Y rotation is contrained by terrain
		bool mouseYConstrained = false;
		OcclusionHandling occlusionHandling = _rpgViewFrustum.GetOcclusionHandling();
		List<string> affectingTags = _rpgViewFrustum.GetAffectingTags();
		if (occlusionHandling == OcclusionHandling.AlwaysZoomIn || occlusionHandling == OcclusionHandling.TagDependent) {
			RaycastHit hitInfo;
			mouseYConstrained = Physics.Raycast(UsedCamera.transform.position, Vector3.down, out hitInfo, 1.0f);
			
			// mouseYConstrained = "Did the ray hit something?" AND "Was it terrain?" AND "Is the camera's Y position under that of the pivot?"
			mouseYConstrained = mouseYConstrained && hitInfo.transform.GetComponent<Terrain>() && UsedCamera.transform.position.y < _cameraPivotPosition.y;
			
			if (occlusionHandling == OcclusionHandling.TagDependent) {
				// Additionally take into account if the hit terrain has a camera affecting tag
				mouseYConstrained = mouseYConstrained && affectingTags.Contains(hitInfo.transform.tag);
			}
		}

		#region Get inputs

		float smoothTime = MouseSmoothTime;
		float mouseYMinLimit = _mouseY;
		// Get mouse input
		if (ActivateCameraControl && (Input.GetButton("Fire1") || Input.GetButton("Fire2") || AlwaysRotateCamera)) {
			// Apply the prescribed cursor lock mode and visibility
			Cursor.lockState = CursorLockMode;
			Cursor.visible = !HideCursorWhenPressed;

			// Get mouse X axis input
			if (!LockMouseX) {
				float mouseXinput = 0;
			
				if (InvertMouseX) {
					mouseXinput = -Input.GetAxis("Mouse X");
				} else {
					mouseXinput = Input.GetAxis("Mouse X");
				}
				
				// Check the character alignment mode
				if (AlignCharacter == AlignCharacter.Always || (AlignCharacter == AlignCharacter.OnAlignmentInput && Input.GetButton(AlignmentInput))) {
					// Check if the character already has been aligned
					if (!_characterAligned) {
						// Align the character and set _characterAligned to true (so that the character only gets aligned on the first frame)
						float cameraYrotation = UsedCamera.transform.eulerAngles.y;
						transform.eulerAngles = new Vector3(transform.eulerAngles.x, cameraYrotation, transform.eulerAngles.z);
						_mouseX = 0;
						_mouseXSmooth = 0;
						_mouseXCurrentVelocity = 0;
						
						_characterAligned = true;
					}

					if (_rpgMotor != null) {
						// Let the character rotate according to the mouse X axis input
						_rpgMotor.SetLocalRotationFire2Input(mouseXinput * MouseXSensitivity);
					}
				} else {
					// No character alignment needed => allow the character to be aligned again and let the camera rotate normally as well
					_characterAligned = false;
					_mouseX += mouseXinput * MouseXSensitivity;

					if (_rpgMotor != null) {
						_rpgMotor.SetLocalRotationFire2Input(0);
					}
				}
				
				if (ConstrainMouseX) {
					// Clamp the rotation in X axis direction
					_mouseX = Mathf.Clamp(_mouseX, MouseXMin, MouseXMax);
				}
			}
			
			// Get mouse Y axis input
			if (!LockMouseY) {
				if (InvertMouseY) {
					_desiredMouseY -= Input.GetAxis("Mouse Y") * MouseYSensitivity;
				} else {
					_desiredMouseY += Input.GetAxis("Mouse Y") * MouseYSensitivity;
				}
			}
			
			// Check if the camera's Y rotation is constrained by terrain
			if (mouseYConstrained) {
				_mouseY = Mathf.Clamp(_desiredMouseY, Mathf.Max(mouseYMinLimit, MouseYMin), MouseYMax);
				// Set the desired mouse Y rotation to compute the degrees of looking up with the camera
				_desiredMouseY = Mathf.Max(_desiredMouseY, _mouseY - 90.0f);
			} else {
				// Clamp the mouse between the maximum values
				_mouseY = Mathf.Clamp(_desiredMouseY, MouseYMin, MouseYMax);
			}

			_desiredMouseY = Mathf.Clamp(_desiredMouseY, MouseYMin, MouseYMax);
		} else {
			// Unlock the cursor and make it visible again
			Cursor.lockState = CursorLockMode.None;
			Cursor.visible = true;

			if (_rpgMotor != null) {
				_rpgMotor.SetLocalRotationFire2Input(0);
			}
		}

		// Check if the camera shouldn't rotate with the character
		if (_rpgMotor != null && Input.GetAxisRaw("Horizontal") != 0 && !Input.GetButton ("Fire2")) {
			// The character turns and doesn't strafe via Fire2 => Check the RotateWithCharacter value
			if (RotateWithCharacter == RotateWithCharacter.Never || (RotateWithCharacter == RotateWithCharacter.RotationStoppingInput && Input.GetButton(RotationStoppingInput))) {
				// Counter the character's rotation so that the camera stays in place
				_mouseX -= Input.GetAxisRaw("Horizontal") * _rpgMotor.GetRotatingSpeed() * 100.0f * Time.deltaTime;
				smoothTime = 0;
			}
		}

		if (ActivateCameraControl) {
			// Get scroll wheel input
			_desiredDistance = _desiredDistance - Input.GetAxis("Mouse ScrollWheel") * MouseScrollSensitivity;
			_desiredDistance = Mathf.Clamp(_desiredDistance, MinDistance, MaxDistance);
		
			// Check if one of the switch buttons is pressed
			if (Input.GetButton("First Person Zoom")) {
				_desiredDistance = MinDistance;
			} else if (Input.GetButton("Maximum Distance Zoom")) {
				_desiredDistance = MaxDistance;
			}
		}

		if (_rpgMotor != null) {
			// Align the camera with the character when moving forward or backwards
			Vector3 playerDirection = _rpgMotor.GetPlayerDirection();
			// Set _alignCameraWithCharacter. If true, allow alignment of the camera with the character
			_alignCameraWithCharacter = SetAlignCameraWithCharacter(playerDirection.z != 0 || playerDirection.x != 0);
			if (AlignCameraWhenMoving && _alignCameraWithCharacter) {
				// Alignment is desired and an action occured which should result in an alignment => align the camera
				AlignCameraWithCharacter(!SupportWalkingBackwards || playerDirection.z > 0 || playerDirection.x != 0);
			}
		}

		#endregion

		#region Smooth the inputs

		if (AlignCameraWhenMoving && _alignCameraWithCharacter) {
			smoothTime = AlignCameraSmoothTime;
		}

		_mouseXSmooth = Mathf.SmoothDamp(_mouseXSmooth, _mouseX, ref _mouseXCurrentVelocity, smoothTime);
		_mouseYSmooth = Mathf.SmoothDamp(_mouseYSmooth, _mouseY, ref _mouseYCurrentVelocity, smoothTime);

		#endregion

		#region Compute the new camera position
		Vector3 newCameraPosition;
		// Compute the desired position
		_desiredPosition = GetCameraPosition(_mouseYSmooth, _mouseXSmooth, _desiredDistance);
		// Compute the closest possible camera distance by checking if there is something inside the view frustum
		float closestDistance = _rpgViewFrustum.CheckForOcclusion(_desiredPosition, _cameraPivotPosition, UsedCamera);
		
		if (closestDistance != -1) {
			// Camera view is constrained => set the camera distance to the closest possible distance 
			closestDistance -= UsedCamera.nearClipPlane;
			if (_distanceSmooth < closestDistance) {
				// Smooth the distance if we move from a smaller constrained distance to a bigger constrained distance
				_distanceSmooth = Mathf.SmoothDamp(_distanceSmooth, closestDistance, ref _distanceCurrentVelocity, DistanceSmoothTime);
			} else {
				// Do not smooth if the new closest distance is smaller than the current distance
				_distanceSmooth = closestDistance;
			}
		
		} else {
			// The camera view at the desired position is not contrained but we have to check if it is when zooming to the desired position
			Vector3 currentCameraPosition = GetCameraPosition(_mouseYSmooth, _mouseXSmooth, _distanceSmooth);
			// Check again for occlusion. This time for the current camera position
			closestDistance = _rpgViewFrustum.CheckForOcclusion(currentCameraPosition, _cameraPivotPosition, UsedCamera);

			if (closestDistance != -1) {
				// The camera is/will be constrained on the way to the desired position => set the camera distance to the closest possible distance 
				closestDistance -= UsedCamera.nearClipPlane;
				_distanceSmooth = closestDistance;
			} else {
				// The camera is not constrained on the way to the desired position => smooth the distance change
				_distanceSmooth = Mathf.SmoothDamp(_distanceSmooth, _desiredDistance, ref _distanceCurrentVelocity, DistanceSmoothTime);
			}
		}
		// Compute the new camera position
		newCameraPosition = GetCameraPosition(_mouseYSmooth, _mouseXSmooth, _distanceSmooth);
		
		#endregion

		#region Update the camera transform

		UsedCamera.transform.position = newCameraPosition;
		// Check if we are in third or first person and adjust the camera rotation behavior
		if (_distanceSmooth > 0.1f) {
			// In third person => orbit camera
			UsedCamera.transform.LookAt(_cameraPivotPosition);
		} else {
			// In first person => normal camera rotation with rotating the character as well
			Quaternion characterRotation = transform.rotation;
			Quaternion cameraRotation = Quaternion.Euler(new Vector3(_mouseYSmooth, _mouseXSmooth, 0));
			UsedCamera.transform.rotation = characterRotation * cameraRotation;
		}

		if (mouseYConstrained /*|| _distanceSmooth <= 0.1f*/) {
			// Camera lies on terrain => enable looking up			
			float lookUpDegrees = _desiredMouseY - _mouseY;
			UsedCamera.transform.Rotate(Vector3.right, lookUpDegrees);
		}

		#endregion
	}
	
	/* Compute the camera position with rotation around the X axis by xAxisDegrees degrees, around 
	 * the Y axis by yAxisdegrees and with distance distance relative to the direction the 
	 * character is facing */
	private Vector3 GetCameraPosition(float xAxisDegrees, float yAxisDegrees, float distance) {
		Vector3 offset = -transform.forward;
		offset.y = 0.0f;
		offset *= distance;
		
		Quaternion rotXaxis = Quaternion.AngleAxis(xAxisDegrees, transform.right);
		Quaternion rotYaxis = Quaternion.AngleAxis(yAxisDegrees, Vector3.up);
		Quaternion rotation = rotYaxis * rotXaxis;
		
		return _cameraPivotPosition + rotation * offset;
	}

	/* Resets the camera view behind the character + starting X rotation, starting Y rotation and starting distance StartDistance */
	public void ResetView() {
		_mouseX = StartMouseX;
		_mouseY = _desiredMouseY = StartMouseY;
		_desiredDistance = StartDistance;
	}

	/* Rotates the camera by degree degrees */
	public void Rotate(float degree) {
		_mouseX += degree;
	}

	/* Sets the private variable _alignCameraWithCharacter depending on if the character is in motion */
	private bool SetAlignCameraWithCharacter(bool characterMoves) {
		// Check if camera controls are activated
		if (ActivateCameraControl) {
			// Align camera with character only when the character moves AND neither "Fire1" nor "Fire2" is pressed
			return characterMoves && !Input.GetButton("Fire1") && !Input.GetButton("Fire2");			
		} else {
			// Only align the camera with the character when the character moves
			return characterMoves;
		}
	}

	/* Aligns the camera with the character depending on behindCharacter. If behindCharacter is true, the camera aligns
	 * behind the character, otherwise it aligns so that it faces the character's front */
	private void AlignCameraWithCharacter(bool behindCharacter) {
		float offsetToCameraRotation = CustomModulo(_mouseX, 360.0f);

		float targetRotation = 180.0f;
		if (behindCharacter) {
			targetRotation = 0;
		}

		if (offsetToCameraRotation == targetRotation) {
			// There is no offset to the camera rotation => no alignment computation required
			return;
		}
	
		int numberOfFullRotations = (int)(_mouseX) / 360;
		
		if (_mouseX < 0) {
			if (offsetToCameraRotation < -180) {
				numberOfFullRotations--;
			} else {				
				targetRotation = -targetRotation;
			}
		} else {
			if (offsetToCameraRotation > 180) {
				// The shortest way to rotate behind the character is to fulfill the current rotation
				numberOfFullRotations++;
				targetRotation = -targetRotation;
			}
		}
		
		_mouseX = numberOfFullRotations * 360.0f + targetRotation;
	}

	/* A custom modulo operation for calculating mod of a negative number as well */
	private float CustomModulo(float dividend, float divisor) {
		if (dividend < 0) {
			return dividend - divisor * Mathf.Ceil(dividend / divisor);	
		} else {
			return dividend - divisor * Mathf.Floor(dividend / divisor);
		}
	}
	
	/* Updates the skybox of the camera UsedCamera */
	public void SetUsedSkybox(Material skybox) {
		// Set the new skybox
		UsedSkybox = skybox;
		// Signal that the skybox changed for the next frame
		_skyboxChanged = true;
	}
	
	/* Update the mouse/camera X rotation */
	public void UpdateMouseX(float mouseX) {
		_mouseX += mouseX;
	}
	
	/* If Gizmos are enabled, this method draws some debugging spheres */
	private void OnDrawGizmos() {
		// Draw the camera pivot at its position
		Gizmos.color = Color.cyan;
		Gizmos.DrawSphere(transform.position + transform.TransformVector(CameraPivotLocalPosition), 0.1f);
		
		// Draw the camera's possible orbit considering occlusions
		Gizmos.color = Color.white;
		Gizmos.DrawWireSphere(_cameraPivotPosition, _distanceSmooth);
	}
}
