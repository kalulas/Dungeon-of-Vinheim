using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

public class RPGViewFrustum : MonoBehaviour {

	public OcclusionHandling OcclusionHandling = OcclusionHandling.TagDependent;
	// Specifies the object layers which get considered inside the view frustum
	public LayerMask OccludingLayers = 1;
	// If "OcclusionHandling" is set to "TagDependent", these object tags affect the camera zoom
	public List<string> AffectingTags = new List<string>(){ "AffectCameraZoom" };
	public float FadeOutAlpha = 0.2f;
	public float FadeInAlpha = 1.0f;
	public float FadeOutDuration = 0.2f;
	public float FadeInDuration = 0.2f;
	// If true, the character fades out too when zooming into first person
	public bool EnableCharacterFading = true;
	public float CharacterFadeOutAlpha = 0;
	public float CharacterFadeStartDistance = 1.2f;
	public float CharacterFadeEndDistance = 0.8f;

	// The used camera (temporarily saved for gizmo purposes only)
	private Camera _usedCamera;
	// Start position (e.g. the camera position) for clip plane computations
	private Vector3 _startPosition;
	// Target position (e.g. the camera pivot position) for raycasting
	private Vector3 _targetPosition;
	// Shift to the upper left corner etc. of the near clip plane
	private Vector3 _shiftUpperLeft;
	private Vector3 _shiftUpperRight;
	private Vector3 _shiftLowerLeft;
	private Vector3 _shiftLowerRight;
	// Half near clip plane width
	private float _halfWidth;
	// Half near clip plane height
	private float _halfHeight;
	
	// Contains the objects to fade from the last frame that are currently faded out
	private SortedDictionary<int, GameObject> _previousObjectsToFade = new SortedDictionary<int, GameObject>();
	// Contains all currently active fade out coroutines
	private Dictionary<int, IEnumerator> _fadeOutCoroutines = new Dictionary<int, IEnumerator>();
	// Contains all currently active fade in coroutines
	private Dictionary<int, IEnumerator> _fadeInCoroutines = new Dictionary<int, IEnumerator>();
	// Contains all renderes attached to the character which should be faded out
	private Renderer[] _characterRenderersToFade;

	private void Awake() {
		UpdateCharacterRenderersToFade();
	}

	/* Sets up the camera clipping planes defining the view frustum */
	private void setUpClippingPlanes(Vector3 atPosition, Vector3 target, Camera usedCamera) {
		_usedCamera = usedCamera;
		_startPosition = atPosition;
		_targetPosition = target;
		
		float halfFieldOfView = usedCamera.fieldOfView * 0.5f * Mathf.Deg2Rad;
		_halfHeight = usedCamera.nearClipPlane * Mathf.Tan(halfFieldOfView);
		_halfWidth = _halfHeight * usedCamera.aspect;
		
		Vector3 targetDirection = target - atPosition;
		targetDirection.Normalize();
		
		Vector3 localRight = usedCamera.transform.right;
		Vector3 localUp = Vector3.Cross(targetDirection, localRight);
		localUp.Normalize();
		
		float offset = usedCamera.nearClipPlane;
		
		_shiftUpperLeft = -localRight * _halfWidth;
		_shiftUpperLeft += localUp * _halfHeight;
		_shiftUpperLeft += targetDirection * offset;
		
		_shiftUpperRight = localRight * _halfWidth;
		_shiftUpperRight += localUp * _halfHeight;
		_shiftUpperRight += targetDirection * offset;
		
		_shiftLowerLeft = -localRight * _halfWidth;
		_shiftLowerLeft -= localUp * _halfHeight;
		_shiftLowerLeft += targetDirection * offset;
		
		_shiftLowerRight = localRight * _halfWidth;
		_shiftLowerRight -= localUp * _halfHeight;
		_shiftLowerRight += targetDirection * offset;
	}

	/* Checks for objects inside the view frustum and - depending on the handling - fades them out or lets the camera zoom in. 
	 * Returns -1 if there is no ambient occlusion, otherwise returns the closest possible distance so that the sight to the target is clear */
	public float CheckForOcclusion(Vector3 atPosition, Vector3 target, Camera usedCamera) {
		OcclusionHandling handling = OcclusionHandling;

		if (handling == OcclusionHandling.DoNothing) {
			// All objects inside the view frustum should be ignored, hence there is no occlusion => return -1
			return -1;
		}
		// Return -1 if there was no collision with the view frustum
		float closestDistance = -1;
		
		// Set up the clipping planes
		setUpClippingPlanes(atPosition, target, usedCamera);
		// Compute the view frustum direction and length
		Vector3 rayDirection = _startPosition - _targetPosition;
		float rayDistance = rayDirection.magnitude;

		SortedDictionary<int, GameObject> objectsToFade = new SortedDictionary<int, GameObject>();
		List<RaycastHit> closestRaycastHits /* of each ray */ = new List<RaycastHit>();
		
		// Cast rays through center of the view frustum and along its edges. Add the closest hits to "closestRaycastHits" and hit objects to fade to "objectsToFade"		
		ImprovedRaycastAll(_targetPosition, rayDirection, rayDistance, OccludingLayers, handling, ref objectsToFade, ref closestRaycastHits);
		ImprovedRaycastAll(_targetPosition + _shiftUpperLeft, rayDirection, rayDistance, OccludingLayers, handling, ref objectsToFade, ref closestRaycastHits);
		ImprovedRaycastAll(_targetPosition + _shiftUpperRight, rayDirection, rayDistance, OccludingLayers, handling, ref objectsToFade, ref closestRaycastHits);
		ImprovedRaycastAll(_targetPosition + _shiftLowerLeft, rayDirection, rayDistance, OccludingLayers, handling, ref objectsToFade, ref closestRaycastHits);
		ImprovedRaycastAll(_targetPosition + _shiftLowerRight, rayDirection, rayDistance, OccludingLayers, handling, ref objectsToFade, ref closestRaycastHits);

		RaycastHit[] closestRaycastHitsArray = closestRaycastHits.ToArray();
		// Sort the hits by their distance
		Array.Sort(closestRaycastHitsArray, RaycastHitComparator);

		if (closestRaycastHitsArray.Length > 0) {
			// At least one object intersects with the view frustum => take the closest hit's distance
			closestDistance = closestRaycastHitsArray[0].distance;
		}

		if (handling == OcclusionHandling.TagDependent) {
			// OcclusionHandling is "TagDependent" => enable the fading of objects
			// Create lists for objects to fade in or out
			List<GameObject> fadeOut = new List<GameObject>();
			List<GameObject> fadeIn = new List<GameObject>();
			
			// The following lines do the following: 
			// - Compare the objects to fade of the last frame and the objects hit in this frame
			// - If an object is in "_previousObjectsToFade" but not in "objectsToFade", fade it back in (as it is no longer inside the view frustum
			// - If an object is not in "_previousObjectsToFade" but in "objectsToFade", fade it out (as it is enters the view frustum this frame)
			// - If an object is in both lists, do nothing and continue (as the object was already inside the view frustum and still is)
			SortedDictionary<int, GameObject>.Enumerator i = _previousObjectsToFade.GetEnumerator();
			SortedDictionary<int, GameObject>.Enumerator j = objectsToFade.GetEnumerator();

			bool iFinished = !i.MoveNext();
			bool jFinished = !j.MoveNext();
			bool aListFinished = iFinished || jFinished;

			while (!aListFinished) {
				int iKey = i.Current.Key;
				int jKey = j.Current.Key;

				if (iKey == jKey) {
					iFinished = !i.MoveNext();
					jFinished = !j.MoveNext();
					aListFinished = iFinished || jFinished;
				} else if (iKey < jKey) {
					if (i.Current.Value != null) {
						fadeIn.Add(i.Current.Value);
					}
					aListFinished = !i.MoveNext();
					iFinished = true;
					jFinished = false;
				} else {
					if (j.Current.Value != null) {
						fadeOut.Add(j.Current.Value);
					}
					aListFinished = !j.MoveNext();
					iFinished = false;
					jFinished = true;
				}
			}

			if (iFinished && !jFinished) {
				do {
					if (j.Current.Value != null) {
						fadeOut.Add(j.Current.Value);
					}
				} while (j.MoveNext());
			} else if (!iFinished && jFinished) {
				do {
					if (i.Current.Value != null) {
						fadeIn.Add(i.Current.Value);
					}
				} while (i.MoveNext());
			}
					
			foreach (GameObject o in fadeOut) {
				int objectID = o.transform.GetInstanceID();
				// Create a new coroutine for fading out the object
				IEnumerator coroutine = FadeObjectCoroutine(FadeOutAlpha, FadeOutDuration, o);

				// Check if there is a running fade in coroutine for this object
				IEnumerator runningCoroutine;
				if (_fadeInCoroutines.TryGetValue(objectID, out runningCoroutine)) {
					// Stop the already running coroutine
					StopCoroutine(runningCoroutine);
					// Remove it from the fade in coroutines
					_fadeInCoroutines.Remove(objectID);
				}
				// Add the new fade out coroutine to the list of fade out coroutines
				_fadeOutCoroutines.Add(objectID, coroutine);
				// Start the coroutine
				StartCoroutine(coroutine);
			}

			foreach (GameObject o in fadeIn) {

				int objectID = o.transform.GetInstanceID();
				// Create a new coroutine for fading in the object
				IEnumerator coroutine = FadeObjectCoroutine(FadeInAlpha, FadeInDuration, o);
				
				// Check if there is a running fade out coroutine for this object
				IEnumerator runningCoroutine;
				if (_fadeOutCoroutines.TryGetValue(objectID, out runningCoroutine)) {
					// Stop the already running coroutine
					StopCoroutine(runningCoroutine);
					// Remove it from the fade out coroutines
					_fadeOutCoroutines.Remove(objectID);
				}
				// Add the new fade in coroutine to the list of fade in coroutines
				_fadeInCoroutines.Add(objectID, coroutine);
				// Start the coroutine
				StartCoroutine(coroutine);
			}
			
			// Set the "_previousObjectsToFade" for the next frame occlusion computations
			_previousObjectsToFade = objectsToFade;
		}

		if (EnableCharacterFading) {
			// Let the character fade in/out
			CharacterFade(usedCamera);
		}
	
		return closestDistance;
	}

	/*
		Casts a ray from "start" to "direction" with distance "distance", considering all layers in "layerMask" and taking into
		account the occlusion handling "handling". Adds all objects hit with a layer of the layer mask "layerMask" to 
		"objectsToFade". Additionally, adds the RaycastHit closest to "start" to the list "closestRaycastHits" 
	*/
	private void ImprovedRaycastAll(Vector3 start, 
	                                Vector3 direction, 
	                                float distance, 
	                                int layerMask, 
	                                OcclusionHandling handling,
	                                ref SortedDictionary<int, GameObject> objectsToFade, 
	                                ref List<RaycastHit> closestRaycastHits) 
	{	
		// Cast a ray and store all hits
		RaycastHit[] hitArray = Physics.RaycastAll(start, direction, distance, layerMask);

		if (hitArray.Length > 0) {
			// Objects got hit, sort the hits by their distance to "start"
			Array.Sort(hitArray, RaycastHitComparator);
			
			if (handling == OcclusionHandling.AlwaysZoomIn) {
				// Add the closest hit to "closestRaycastHits" as we consider all objects regardless of the tag
				closestRaycastHits.Add(hitArray[0]);
			} else  if (handling == OcclusionHandling.TagDependent) {
				// Find the closest hit of an object with a camera affecting tag and add every object with no affecting tag
				// to the "objcetsToFade" list
				for (int i = 0; i < hitArray.Length; i++) {
					RaycastHit hit = hitArray[i];
					int hitObjectID = hit.transform.GetInstanceID();
					
					if (AffectingTags.Contains(hit.transform.tag)) {
						closestRaycastHits.Add(hitArray[i]);
						return;
					} else if (!objectsToFade.ContainsKey(hitObjectID)) {
						objectsToFade.Add(hitObjectID, hit.transform.gameObject);
					}
				}
			}
		}
	}
	
	/* Comparator for comparing two RaycastHits by their distance */
	private int RaycastHitComparator(RaycastHit a, RaycastHit b) {
		return a.distance.CompareTo(b.distance);
	}

	/* Create a coroutine for fading an object "o" to alpha value "to" over "duration" */
	private IEnumerator FadeObjectCoroutine(float to, float duration, GameObject o) {
		bool continueFading = true;
		// Get all renderers of object "o"
		Renderer[] objectRenderers = o.transform.GetComponentsInChildren<Renderer>();

		if (objectRenderers.Length > 0) {
			// There are renderers to fade, create a current velocity array for each renderer fade out
			float[] currentVelocity = new float[objectRenderers.Length];

			while (continueFading) {
				for (int i = 0; i < objectRenderers.Length; i++) {
					Renderer r = objectRenderers[i];
					Material[] mats = r.materials;
					float alpha = -1.0f;

					foreach (Material m in mats) {
						if (m.HasProperty("_Color")) {
							// Material has a color property
							if (alpha == -1.0f) {
								// Compute the alpha for every material only once
								alpha = Mathf.SmoothDamp(m.color.a, to, ref currentVelocity[i], duration);
							}
							
							// Apply the modified alpha value
							Color color = m.color;
							color.a = alpha;						
							m.color = color;
						}
					}

					r.materials = mats;

					if (IsAlmostEqual(alpha, to, 0.01f)) {
						// The current alpha is almost equal to the target alpha value "to" => stop fading
						continueFading = false;
					}
				}
				
				// Continue computation in the next frame
				yield return null;
			}
		}

		int objectID = o.transform.GetInstanceID();
		// Coroutine done => remove the coroutine from both coroutine lists
		_fadeOutCoroutines.Remove(objectID);
		_fadeInCoroutines.Remove(objectID);
	}

	/* Equal method for float equality. Returns true if the distance between "a" and "b" is smaller than "epsilon" */
	private bool IsAlmostEqual(float a, float b, float epsilon) {
		return Mathf.Abs(a - b) < epsilon;
	}

	/* Lets the character fade */
	private void CharacterFade(Camera usedCamera) {
		// Get the actual distance
		float actualDistance = Vector3.Distance(_targetPosition, usedCamera.transform.position);
		// Compute the new alpha value depending on the fading start and end distance
		float t = Mathf.Floor(Mathf.Clamp((actualDistance - CharacterFadeEndDistance) / (CharacterFadeStartDistance - CharacterFadeEndDistance), 0, 1) * 100) / 100;
		float maximumAlpha = 1.0f;

		if (OcclusionHandling == OcclusionHandling.TagDependent && Convert.ToBoolean(OccludingLayers & (1 << gameObject.layer))) {
			// Occlusion is tag dependent and the character's layer is part of the occluding layers 
			// => set the maximum possible alpha to the "FadeOutAlpha"
			maximumAlpha = FadeOutAlpha;
		}

		foreach (Renderer r in _characterRenderersToFade) {
			// Adjust all character renderers' color
			Color c = r.material.color;
			// Interpolate the new alpha "t" between the minimun and maximum possible alpha
			c.a = Mathf.SmoothStep(CharacterFadeOutAlpha, maximumAlpha, t);
			r.material.color = c;
		}
	}

	/* Updates the "_characterRenderersToFade", has to be called when the character renderers changed and character fading is on */
	public void UpdateCharacterRenderersToFade() {
		List<Renderer> renderers = new List<Renderer>();
		Renderer[] temp = GetComponentsInChildren<Renderer>();
		
		foreach (Renderer r in temp) {
			if (r.material.HasProperty("_Color")) {
				// Add only if the color property is available
				renderers.Add(r);
			}
		}
		
		_characterRenderersToFade = renderers.ToArray();
	}

	public OcclusionHandling GetOcclusionHandling() {
		return OcclusionHandling;
	}
	
	public List<string> GetAffectingTags() {
		return AffectingTags;
	}
	
	/* If Gizmos are turned on, this method draws the clip plane and the resulting view frustum */
	private void OnDrawGizmos() {		

		Gizmos.color = Color.gray;
		// Calculate the near clip plane at "_startPosition" (usually the desired camera position)
		Vector3 UpperLeft = _startPosition + _shiftUpperLeft;
		Vector3 UpperRight = _startPosition + _shiftUpperRight;
		Vector3 LowerLeft = _startPosition + _shiftLowerLeft;
		Vector3 LowerRight = _startPosition + _shiftLowerRight;
		// Draw the near clip plane at "_startPosition"
		Gizmos.DrawLine(UpperLeft, UpperRight);
		Gizmos.DrawLine(UpperLeft, LowerLeft);
		Gizmos.DrawLine(UpperRight, LowerRight);
		Gizmos.DrawLine(LowerLeft, LowerRight);
		
		Gizmos.color = Color.red;

		if (_usedCamera != null) {			
			// Calculate the near clip plane at the current camera position
			Vector3 camPos = _usedCamera.transform.position;
			UpperLeft =  camPos + _shiftUpperLeft;
			UpperRight = camPos + _shiftUpperRight;
			LowerLeft = camPos + _shiftLowerLeft;
			LowerRight = camPos + _shiftLowerRight;
			// Draw the near clip plane at the current camera position
			Gizmos.DrawLine(UpperLeft, UpperRight);
			Gizmos.DrawLine(UpperLeft, LowerLeft);
			Gizmos.DrawLine(UpperRight, LowerRight);
			Gizmos.DrawLine(LowerLeft, LowerRight);
		}
		
		// Calculate the clip plane at the target (usually the camera pivot position position)
		UpperLeft = _targetPosition + _shiftUpperLeft;
		UpperRight = _targetPosition + _shiftUpperRight;
		LowerLeft = _targetPosition + _shiftLowerLeft;
		LowerRight = _targetPosition + _shiftLowerRight;			
		// Draw the clip plane at the target
		Gizmos.DrawLine(UpperLeft, UpperRight);
		Gizmos.DrawLine(UpperLeft, LowerLeft);
		Gizmos.DrawLine(UpperRight, LowerRight);
		Gizmos.DrawLine(LowerLeft, LowerRight);
		
		Gizmos.color = Color.white;
		Vector3 viewFrustumDirection = _startPosition - _targetPosition;
		// Draw the lines which are tested for collisions and form the view frustum
		Gizmos.DrawRay(UpperLeft, viewFrustumDirection);
		Gizmos.DrawRay(UpperRight, viewFrustumDirection);
		Gizmos.DrawRay(LowerLeft, viewFrustumDirection);
		Gizmos.DrawRay(LowerRight, viewFrustumDirection);

		if (_usedCamera != null) {	
			// Draw the current possible camera distance
			Gizmos.color = Color.cyan;
			Gizmos.DrawRay(_targetPosition, _usedCamera.transform.position - _targetPosition);
		}
	}
}
