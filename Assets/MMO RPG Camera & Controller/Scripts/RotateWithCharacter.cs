using UnityEngine;
using System.Collections;

/* Enum for controlling if the camera should rotate together with the character */
public enum RotateWithCharacter {
	Never,					// Never rotate with the character
	RotationStoppingInput,	// The rotation stops when the stopping input is pressed. The input can be set inside the RPGCamera
	Always					// Always rotate together with the character
};