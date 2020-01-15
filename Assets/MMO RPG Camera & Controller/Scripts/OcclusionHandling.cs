using UnityEngine;
using System.Collections;

/* Enum describing the mode of occlusion handling */
public enum OcclusionHandling {
	DoNothing,		// Never zoom in
	TagDependent,	// Only zoom in when the object's tag is set to a prescribed tag 
	AlwaysZoomIn	// Always zoom in regardless the tag
};
