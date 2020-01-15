using UnityEngine;
using System.Collections;

/* Enum for controlling if the charater should be aligned to the camera, i.e. view in the same direction */
public enum AlignCharacter {
	Never,				// Never align the character with the camera
	OnAlignmentInput,	// Only align when the Alignment input set inside the RPGCamera is pressed
	Always				// Always align the character with the camera
};