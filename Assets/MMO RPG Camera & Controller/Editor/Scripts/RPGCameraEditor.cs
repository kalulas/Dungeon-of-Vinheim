using UnityEngine;
using System.Collections;
using UnityEditor;

[CustomEditor(typeof(RPGCamera))]
public class RPGCameraEditor : Editor {

	private bool _showGeneralSettings = false;
	private bool _showCursorSettings = false;
	private bool _showMouseXSettings = false;
	private bool _showMouseYSettings = false;
	private bool _showDistanceSettings = false;
	private bool _showAlignmentSettings = false;

	private bool _sortedView = true;
	
	void OnEnable() {
		_showGeneralSettings = EditorPrefs.GetBool("_showGeneralSettings", false);
		_showCursorSettings = EditorPrefs.GetBool("_showCursorSettings", false);
		_showMouseXSettings = EditorPrefs.GetBool("_showMouseXSettings", false);
		_showMouseYSettings = EditorPrefs.GetBool("_showMouseYSettings", false);
		_showDistanceSettings = EditorPrefs.GetBool("_showDistanceSettings", false);
		_showAlignmentSettings = EditorPrefs.GetBool("_showAlignmentSettings", false);
		
		_sortedView = EditorPrefs.GetBool("_sortedView", false);
	}

	void OnDestroy() {
		EditorPrefs.SetBool("_showGeneralSettings", _showGeneralSettings);
		EditorPrefs.SetBool("_showCursorSettings", _showCursorSettings);
		EditorPrefs.SetBool("_showMouseXSettings", _showMouseXSettings);
		EditorPrefs.SetBool("_showMouseYSettings", _showMouseYSettings);
		EditorPrefs.SetBool("_showDistanceSettings", _showDistanceSettings);
		EditorPrefs.SetBool("_showAlignmentSettings", _showAlignmentSettings);
		
		EditorPrefs.SetBool("_sortedView", _sortedView);
	}

	public override void OnInspectorGUI() {
		RPGCamera script = (RPGCamera)target;

		GUIStyle foldoutStyle = new GUIStyle(EditorStyles.foldout);
		foldoutStyle.fontStyle = FontStyle.Bold;
		foldoutStyle.fontSize = 11;

		_sortedView = EditorGUILayout.Toggle("Group Variables", _sortedView);
		if (!_sortedView) {
			DrawDefaultInspector();
			return;
		}

		_showGeneralSettings = EditorGUILayout.Foldout(_showGeneralSettings, "General", foldoutStyle);

		if (_showGeneralSettings) {
			script.UsedCamera = (Camera)EditorGUILayout.ObjectField("Used Camera", script.UsedCamera, typeof(Camera), true);
			script.UsedSkybox = (Material)EditorGUILayout.ObjectField("Used Skybox", script.UsedSkybox, typeof(Material), true);
			script.CameraPivotLocalPosition = EditorGUILayout.Vector3Field("Camera Pivot Local Position", script.CameraPivotLocalPosition);
			script.ActivateCameraControl = EditorGUILayout.Toggle("Activate Camera Control", script.ActivateCameraControl);
			script.AlwaysRotateCamera = EditorGUILayout.Toggle("Always Rotate Camera", script.AlwaysRotateCamera);
			script.RotateWithCharacter = (RotateWithCharacter)EditorGUILayout.EnumPopup("RotateWithCharacter", script.RotateWithCharacter);
		}

		_showCursorSettings = EditorGUILayout.Foldout(_showCursorSettings, "Cursor", foldoutStyle);

		if (_showCursorSettings) {
			script.CursorLockMode = (CursorLockMode)EditorGUILayout.EnumPopup("Cursor Lock Mode", script.CursorLockMode);
			script.HideCursorWhenPressed = EditorGUILayout.Toggle("Hide Cursor When Pressed", script.HideCursorWhenPressed);
		}
		
		_showMouseXSettings = EditorGUILayout.Foldout(_showMouseXSettings, "Mouse X", foldoutStyle);

		if (_showMouseXSettings) {
			script.StartMouseX = EditorGUILayout.FloatField("Start Mouse X", script.StartMouseX);
			script.LockMouseX = EditorGUILayout.Toggle("Lock Mouse X", script.LockMouseX);
			script.InvertMouseX = EditorGUILayout.Toggle("Invert Mouse X", script.InvertMouseX);
			script.MouseXSensitivity = EditorGUILayout.FloatField("Mouse X Sensitivity", script.MouseXSensitivity);
			script.ConstrainMouseX = EditorGUILayout.Toggle("Constrain Mouse X", script.ConstrainMouseX);
			script.MouseXMin = EditorGUILayout.FloatField("Mouse X Min", script.MouseXMin);
			script.MouseXMax = EditorGUILayout.FloatField("Mouse X Max", script.MouseXMax);
		}
		
		_showMouseYSettings = EditorGUILayout.Foldout(_showMouseYSettings, "Mouse Y", foldoutStyle);

		if (_showMouseYSettings) {
			script.StartMouseY = EditorGUILayout.FloatField("Start Mouse Y", script.StartMouseY);
			script.LockMouseY = EditorGUILayout.Toggle("Lock Mouse Y", script.LockMouseY);			
			script.InvertMouseY = EditorGUILayout.Toggle("Invert Mouse Y", script.InvertMouseY);			
			script.MouseYSensitivity = EditorGUILayout.FloatField("Mouse Y Sensitivity", script.MouseYSensitivity);			
			script.MouseYMin = EditorGUILayout.FloatField("Mouse Y Min", script.MouseYMin);
			script.MouseYMax = EditorGUILayout.FloatField("Mouse Y Max", script.MouseYMax);
		}

		_showDistanceSettings = EditorGUILayout.Foldout(_showDistanceSettings, "Distance", foldoutStyle);

		if (_showDistanceSettings) {			
			script.StartDistance = EditorGUILayout.FloatField("Start Distance", script.StartDistance);script.MinDistance = EditorGUILayout.FloatField("Min Distance", script.MinDistance);
			script.MaxDistance = EditorGUILayout.FloatField("Max Distance", script.MaxDistance);
			script.DistanceSmoothTime = EditorGUILayout.FloatField("Distance Smooth Time", script.DistanceSmoothTime);
			script.MouseScrollSensitivity = EditorGUILayout.FloatField("Mouse Scroll Sensitivity", script.MouseScrollSensitivity);
			script.MouseSmoothTime = EditorGUILayout.FloatField("Mouse Smooth Time", script.MouseSmoothTime);
		}
		
		_showAlignmentSettings = EditorGUILayout.Foldout(_showAlignmentSettings, "Alignment", foldoutStyle);

		if (_showAlignmentSettings) {			
			script.AlignCharacter = (AlignCharacter)EditorGUILayout.EnumPopup("Align Character", script.AlignCharacter);
			script.AlignCameraWhenMoving = EditorGUILayout.Toggle("Align Camera When Moving", script.AlignCameraWhenMoving);
			script.SupportWalkingBackwards = EditorGUILayout.Toggle("Support Walking Backwards", script.SupportWalkingBackwards);
			script.AlignCameraSmoothTime = EditorGUILayout.FloatField("Align Camera Smooth Time", script.AlignCameraSmoothTime);
		}

		if (GUI.changed) {
			EditorUtility.SetDirty(script);
		}
	}
}
