using UnityEngine;
using System.Collections;

[RequireComponent(typeof(CharacterController))]

public class RPGMotor : MonoBehaviour {

	public float WalkSpeed = 2.0f;
	public float RunSpeed = 10.0f;
	public float StrafeSpeed = 10.0f;
	public float AirborneSpeed = 2.0f;
	public float RotatingSpeed = 2.5f;
	public float SprintSpeedMultiplier = 2.0f;
	public float BackwardsSpeedMultiplier = 0.2f;
	public float JumpHeight = 10.0f;
	public int AllowedAirborneMoves = 1;	
	public bool MoveWithMovingGround = true;	
	public bool RotateWithRotatingGround = true;	
	public bool GroundObjectAffectsJump = true;
	public float SlidingThreshold = 40.0f;
	public float FallingThreshold = 6.0f;
	public float Gravity = 20.0f;

	private CharacterController _characterController;
	private Animator _animator;
	private MotionState _currentMotionState;
	// Local player direction
	private Vector3 _playerDirection;
	// Player direction in world coordinates
	private Vector3 _playerDirectionWorld;
	private float _localRotation;
	private float _localRotationFire2Input;
	private float _localRotationHorizontalInput;
	// True if the character should jump in the current frame
	private bool _jump = false;
	private bool _autorunning = false;
	// True if the character should walk
	private bool _walking = false;
	// True if the character is sprinting
	private bool _sprinting = false;
	// True if the character is sliding
	private bool _sliding = false;
	private bool _allowAirborneMovement = false;
	// Allowed moves while airborne
	private int _airborneMovesCount = 0;
	// True if the character hits another collider while jumping
	private bool _jumpingCollision = false;
	// True if the character performed a jump while it was running
	private bool _runningJump = false;	
	// The object the character is standing on
	private GameObject _groundObject;
	// The character's position of the last frame in world coordinates
	private Vector3 _lastCharacterPosition;
	// The character's position in ground object coordinates
	private Vector3 _groundObjectLocalPosition;
	// The character's rotation of the last frame
	private Quaternion _lastCharacterRotation;
	// The character's rotation relative to the ground object's rotation
	private Quaternion _groundObjectLocalRotation;

	private void Awake() {
		_characterController = GetComponent<CharacterController>();
		_animator = GetComponent<Animator>();
		_characterController.slopeLimit = SlidingThreshold;
	}

	public void StartMotor() {

		if (_characterController.isGrounded) {
			// Reset the counter for the number of remaining moves while airborne
			_airborneMovesCount = 0;
			// Reset the running jump flag
			_runningJump = false;
			
			if (_autorunning) {
				_playerDirection.z = 1.0f;
			}
			
			// Transform the local movement direction to world space
			_playerDirectionWorld = transform.TransformDirection(_playerDirection);
			// Normalize the player's movement direction
			if (_playerDirectionWorld.magnitude > 1) {
				_playerDirectionWorld = Vector3.Normalize(_playerDirectionWorld);
			}

			float resultingSpeed = 0f;
			// Compute the speed combined of strafe and run speed
			if (_playerDirection.x != 0 || _playerDirection.z != 0) {
				resultingSpeed = (StrafeSpeed * Mathf.Abs(_playerDirection.x)
						+ RunSpeed * Mathf.Abs(_playerDirection.z))
						/ (Mathf.Abs(_playerDirection.x) + Mathf.Abs(_playerDirection.z));
			}

			// Multiply with the sprint multiplier if sprinting is active
			if (_sprinting) {
				resultingSpeed *= SprintSpeedMultiplier;
			}
			// Adjust the speed if moving backwards
			if (_playerDirection.z < 0) {
				resultingSpeed *= BackwardsSpeedMultiplier;
			}
			// Adjust the speed if walking is enabled
			if (_walking) {
				resultingSpeed = WalkSpeed;
			}

			// Apply the resulting speed
			_playerDirectionWorld *= resultingSpeed;

			// Apply the falling threshold
			_playerDirectionWorld.y = -FallingThreshold;
			// Apply sliding
			ApplySliding();

			// Check if the character should jump this frame
			if (_jump) {
				_jump = false;

				if (_playerDirection.x != 0 || _playerDirection.z != 0) {
					_runningJump = true;
				}

				if (!_sliding) {
					// Only jump if we are not sliding
					_playerDirectionWorld.y = JumpHeight;
				}
			}

		} else if (_allowAirborneMovement && !_runningJump) {
			// Allow slight movement while airborne only after a standing jump
			Vector3 playerDirectionWorld = transform.TransformDirection(_playerDirection);
			// Normalize the player's movement direction
			if (_playerDirectionWorld.magnitude > 1) {
				playerDirectionWorld = Vector3.Normalize(playerDirectionWorld);
			}
			// Apply the airborne speed
			playerDirectionWorld *= AirborneSpeed;
			// Set the x and z direction to move the character continuously
			_playerDirectionWorld.x = playerDirectionWorld.x;
			_playerDirectionWorld.z = playerDirectionWorld.z;
		}
		
		if (_jumpingCollision) {
			// Got an airborne collision => prevent further soaring
			_playerDirectionWorld.y = -Gravity * Time.deltaTime;
			// Let this happen only once per collision
			_jumpingCollision = false;
		}


		if (MoveWithMovingGround) {
			// Apply ground/passive movement
			ApplyGroundMovement();
		}

		if (RotateWithRotatingGround) {
			// Apply ground/passive rotation
			ApplyGroundRotation();
		}

		// Check if we've left the last inertial space and if so, reset the ground object
		RaycastHit hit;
		if (GroundObjectAffectsJump && Physics.Raycast(transform.position + Vector3.up, Vector3.down, out hit, JumpHeight)) {
			if (hit.transform.gameObject != _groundObject) {
				// Reset the ground object
				_groundObject = null;
			}
		} else {
			// Reset the ground object
			_groundObject = null;
		}

		// Apply gravity
		_playerDirectionWorld.y -= Gravity * Time.deltaTime;
		// Move the character
		_characterController.Move(_playerDirectionWorld * Time.deltaTime);

		if (MoveWithMovingGround || RotateWithRotatingGround) {
			// Store the ground object's pose in case there was a collision detected while moving
			StoreGroundObjectPose();
		}

		// Rotate the character
		_localRotation = _localRotationHorizontalInput + _localRotationFire2Input;
		transform.Rotate(Vector3.up * _localRotation);

		// Determine the current motion state
		_currentMotionState = DetermineMotionState();
		if (_animator) {
			float transitionDamping = 10.0f;
			// Pass values important for animation to the animator
			_animator.SetInteger("MotionState", (int)_currentMotionState);
			_animator.SetBool("Shuffle", _localRotation != 0);
			_animator.SetFloat("StrafeDirection X", _playerDirection.x, 1.0f, transitionDamping * Time.deltaTime);
			_animator.SetFloat("StrafeDirection Z", _playerDirection.z, 1.0f, transitionDamping * Time.deltaTime);
		}
	}

	/* Applies passive movement if the character stands on moving ground */
	private void ApplyGroundMovement() {		
		if (_groundObject != null) {
			// Compute the delta between the ground object's position of the last and the current frame
			Vector3 newGlobalPlatformPoint = _groundObject.transform.TransformPoint(_groundObjectLocalPosition);
			Vector3 moveDirection = newGlobalPlatformPoint - _lastCharacterPosition;
			if (moveDirection != Vector3.zero) {
				// Move the character in the move direction
				transform.position += moveDirection;
			}
		}		
	}

	/* Applies passive rotation if the character stands on rotating ground */
	private void ApplyGroundRotation() {
		if (_groundObject != null) {			
			// Compute the delta between the ground object's rotation of the last and the current frame
			Quaternion newGlobalPlatformRotation = _groundObject.transform.rotation * _groundObjectLocalRotation;
			Quaternion rotationDelta = newGlobalPlatformRotation * Quaternion.Inverse(_lastCharacterRotation);			
			// Prevent rotation of the character's y-axis
			rotationDelta = Quaternion.FromToRotation(rotationDelta * transform.up, transform.up) * rotationDelta;
			// Rotate the character by the rotation delta
			transform.rotation = rotationDelta * transform.rotation;
		}
	}

	// Stores the ground object's pose for the next frame if there is a ground object
	private void StoreGroundObjectPose() {
		if (_groundObject != null) {
			// Store ground object's position for next frame computations
			_lastCharacterPosition = transform.position;
			_groundObjectLocalPosition = _groundObject.transform.InverseTransformPoint(transform.position);
			
			// Store ground object's rotation for next frame computations
			_lastCharacterRotation = transform.rotation;
			_groundObjectLocalRotation = Quaternion.Inverse(_groundObject.transform.rotation) * transform.rotation; 
		}
	}

	/* Applies sliding to the character if it is standing on too steep terrain  */
	private void ApplySliding() {
		RaycastHit hitInfo;

		// Cast a ray down to the ground to get the ground's normal vector
		if (Physics.Raycast(transform.position, Vector3.down, out hitInfo)) {
			//Debug.DrawLine(transform.position, transform.position + hitInfo.normal);
			//Debug.DrawLine(transform.position, transform.position + slopeDirection);

			Vector3 hitNormal = hitInfo.normal;
			// Compute the slope in degrees
			float slope = Vector3.Angle(hitNormal, Vector3.up);
			// Compute the sliding direction
			Vector3 slidingDirection = new Vector3(hitNormal.x, -hitNormal.y, hitNormal.z);
			// Normalize the sliding direction and make it orthogonal to the hit normal
			Vector3.OrthoNormalize(ref hitNormal, ref slidingDirection);
			// Check if the slope is too steep
			if (slope > SlidingThreshold) {
				_sliding = true;
				// Apply sliding
				_playerDirectionWorld = slidingDirection * slope * 0.2f;
			} else {
				_sliding = false;
			}
		}
	}

	/* Determines the current motion state of the character by using set variables */
	private MotionState DetermineMotionState() {
		MotionState result;

		if (_characterController.isGrounded) {
			if (_playerDirection.magnitude > 0) {
				if (_walking) {
					result = MotionState.Walking;
				} else if (_sprinting) {
					result = MotionState.Sprinting;
				} else {
					result = MotionState.Running;
				}
			} else if (_sliding) {
				result = MotionState.Falling;
			} else {
				result = MotionState.Standing;
			}
		} else {
			if (_playerDirectionWorld.y >= 0) {
				result = MotionState.Jumping;
			} else {
				result = MotionState.Falling;
			}
		}

		return result;
	}

	/* Lets the character jump in the current frame, only works when character is grounded */		
	public void Jump() {
		if (_characterController.isGrounded) {
			// Only allow jumping when the character is grounded
			_jump = true;
		}
	}
	
	/* Enables/Disables sprinting */
	public void Sprint(bool on) {
		_sprinting = on;
	}

	/* Enables/Disables sprinting with speed "speed" */
	public void Sprint(bool on, float speed) {
		_sprinting = on;
		SprintSpeedMultiplier = speed;
	}
	
	/* Toggles walking */
	public void ToggleWalking(bool toggle) {
		if (toggle) {
			_walking = !_walking;
		}
	}
	
	/* Toggles autorun */
	public void ToggleAutorun(bool toggle) {
		if (toggle) {
			_autorunning = !_autorunning;
		}
	}
	
	/* Cancels autorun */
	public void StopAutorun(bool stop) {
		if (stop && _autorunning) {
			_autorunning = false;
		}
	}

	/* Sets the character's direction inputted by the player/controller */ 
	public void SetPlayerDirectionInput(Vector3 direction) {
		_playerDirection = direction;
	}

	/* Moves the character in mid air if character is not grounded, an mid air movement key is pressed
	 * and the maximum of mid air moves isn't reached */
	public void MoveInMidAir(bool movement) {
		_allowAirborneMovement = false;
		// Allow airborne movement for the current frame and increase the airborne moves counter if we are not grounded
		if (!_characterController.isGrounded && movement && _airborneMovesCount < AllowedAirborneMoves) {
			_allowAirborneMovement = true;
			_airborneMovesCount++;
		}
	}
	
	/* Set the local rotation input around the Y axis done by pressing the horizontal input */
	public void SetLocalRotationHorizontalInput(float rotation) {	
		_localRotationHorizontalInput = rotation * RotatingSpeed * 100.0f * Time.deltaTime;
	}
	
	/* Set the local rotation input around the Y axis done by pressing Fire2 */
	public void SetLocalRotationFire2Input(float rotation) {
		_localRotationFire2Input = rotation;
	}

	/* Gets this frame's player direction */
	public Vector3 GetPlayerDirection() {
		return _playerDirection;
	}

	/* Gets the current rotating speed */
	public float GetRotatingSpeed() {
		return RotatingSpeed;
	}

	/* "OnControllerColliderHit is called when the controller hits a collider while performing a Move" - Unity Documentation */
	public void OnControllerColliderHit(ControllerColliderHit hit) {
		// Check if we have hit something while jumping
		if (_playerDirectionWorld.y > 0) {
			// Signalize a jumping collision for the next frame
			_jumpingCollision = true;
		}

		//Debug.DrawRay(hit.point, hit.normal, Color.yellow);
		// Set the ground object only if we are really standing on it
		if ((_characterController.collisionFlags & CollisionFlags.Below) != 0 
		    && Vector3.Distance(transform.position, hit.point) < 0.3f * _characterController.radius) {
			_groundObject = hit.gameObject;
		}
	}
}
