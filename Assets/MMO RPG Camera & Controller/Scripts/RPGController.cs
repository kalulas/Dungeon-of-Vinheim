using UnityEngine;
using System.Collections;

[RequireComponent(typeof(RPGMotor))]

public class RPGController : MonoBehaviour {

	private RPGMotor _rpgMotor;

	private void Awake() {
		_rpgMotor = GetComponent<RPGMotor>();

		try {
			Input.GetButton("Horizontal Strafe");
			Input.GetButton("Autorun Toggle");
			Input.GetButton("Walk Toggle");
		} catch (UnityException e) {
			Debug.LogWarning(e.Message);
		}
	}

	private void Update() {

		#region Check inputs
		// Get the vertical movement direction/input
		float vertical = Input.GetAxisRaw("Vertical");
		// Check if Fire1 and Fire2 are pressed both
		if (Input.GetButton("Fire1") && Input.GetButton("Fire2")) {
			// Let the character move forward
			vertical = 1.0f;
		}
		
		// Check the autorun input
		_rpgMotor.ToggleAutorun(Input.GetButtonDown("Autorun Toggle"));
		// Get all actions that can cancel an active autorun
		bool stopAutorunAction = (Input.GetButtonDown("Fire1") && Input.GetButton("Fire2")) || (Input.GetButton("Fire1") && Input.GetButtonDown("Fire2"));
		stopAutorunAction = stopAutorunAction || Input.GetButtonDown("Vertical");
		// Signal the usage of actions cancelling the autorunning
		_rpgMotor.StopAutorun(stopAutorunAction);
		
		// Get the horizontal movement direction/input
		float horizontal = Input.GetAxisRaw("Horizontal");
		// Get the horizontal strafe direction/input				
		float horizontalStrafe = Input.GetAxisRaw("Horizontal Strafe");

		// Strafe if the right mouse button and the Horizontal input are pressed at once
		if (Input.GetButton("Fire2") && Input.GetAxisRaw("Horizontal") != 0) {
			// Let the character strafe instead rotating
			horizontalStrafe = horizontal;
			horizontal = 0f;
		}
		// Create and set the player's input direction inside the motor
		Vector3 playerDirectionInput = new Vector3(horizontalStrafe, 0, vertical);
		_rpgMotor.SetPlayerDirectionInput(playerDirectionInput);

		// Allow movement while airborne if the player wants to move forward/backwards or strafe
		_rpgMotor.MoveInMidAir(Input.GetButtonDown("Vertical") 
		                       || Input.GetButtonDown("Horizontal Strafe") 
		                       || (Input.GetButtonDown("Fire2") && Input.GetAxisRaw("Horizontal") != 0)
		                       || (Input.GetButton("Fire2") && Input.GetButtonDown("Horizontal")));

		// Set the local Y axis rotation input to horizontal inside motor
		_rpgMotor.SetLocalRotationHorizontalInput(horizontal);

		// Enable sprinting inside the motor if the sprint modifier is pressed down
		_rpgMotor.Sprint(Input.GetButton("Sprint"));
		
		// Toggle walking inside the motor
		_rpgMotor.ToggleWalking(Input.GetButtonUp("Walk Toggle"));
		
		// Check if the jump button is pressed down
		if (Input.GetButtonDown("Jump")) {
			// Signal the motor to jump
			_rpgMotor.Jump();
		}

		#endregion
		
		// Start the motor
		_rpgMotor.StartMotor();
	}
}
