using UnityEngine;
using UnityEngine.UI;
using System;

public class DemoGUI : MonoBehaviour {
	
	public RPGCamera RpgCamera;
	public RPGViewFrustum RpgViewFrustum;
	public RPGController RpgController;
	public RPGMotor RpgMotor;
	public Material[] PossibleSkyboxes;
	
	public GameObject _controlsWindow;
	public Text _controlsWindowButtonText;
	public GameObject _variablesWindow;
	public Text _variablesWindowButtonText;
	// RPGCamera
	public Text _usedSkybox;
	public InputField _cameraPivotLocalPositionX;
	public InputField _cameraPivotLocalPositionY;
	public InputField _cameraPivotLocalPositionZ;
	public Toggle _activateCameraControl;
	public Toggle _alwaysRotateCamera; 
	public Text _rotateWithCharacter; 
	public Text _cursorLockMode;
	public Toggle _hideCursorWhenPressed;
	public Toggle _lockMouseX;
	public Toggle _lockMouseY;
	public Toggle _invertMouseX;
	public Toggle _invertMouseY;
	public InputField _mouseXSensitivity;
	public InputField _mouseYSensitivity;
	public Toggle _constrainMouseX;
	public InputField _mouseXMin;
	public InputField _mouseXMax;
	public InputField _mouseYMin;
	public InputField _mouseYMax;
	public InputField _mouseScrollSensitivity;
	public InputField _mouseSmoothTime;
	public InputField _minDistance;
	public InputField _maxDistance;
	public InputField _distanceSmoothTime;	
	public Text _alignCharacterWithCam;
	public Toggle _alignCameraWhenMoving;
	public Toggle _supportWalkingBackwards;
	public InputField _alignCameraSmoothTime;
	// RPGViewFrustum
	public Text _occultationHandling;
	public Text _occultingLayers;
	public InputField _fadeOutAlpha;
	public InputField _fadeInAlpha;
	public InputField _fadeOutDuration;
	public InputField _fadeInDuration;
	public Toggle _enableCharacterFading;
	public InputField _characterFadeOutAlpha;
	public InputField _characterFadeStartDistance;
	public InputField _characterFadeEndDistance;
	// RPGMotor
	public InputField _walkSpeed;
	public InputField _runSpeed;
	public InputField _strafeSpeed;
	public InputField _airborneSpeed;
	public InputField _rotatingSpeed;
	public InputField _sprintSpeedMultiplier;
	public InputField _backwardsSpeedMultiplier;
	public InputField _jumpHeight;
	public InputField _allowedAirborneMoves;
	public Toggle _moveWithMovingGround;
	public Toggle _rotateWithRotatingGround;
	public Toggle _groundObjectAffectsJump;
	public InputField _slidingThreshold;
	public InputField _fallingThreshold;
	public InputField _gravity;
	
	public string[] TestSkyboxes = {"Sunny2 Skybox", "Default-Skybox"};
	public string[] CursorLockModes = {"None", "Locked", "Confined"};
	public string[] RotateWithCharacterOptions = {"Never", "RotationStoppingInput", "Always"};
	public string[] OccultationHandlings = {"DoNothing", "TagDependent", "AlwaysZoomIn"};
	public string[] AlignCharacterOptions = {"Never", "OnAlignmentInput", "Always"};
	public string[] OccultingLayersOptions = {"Nothing", "Everything", "Default", "Player"};
	
	private bool _awoken = false;
	private bool _showControls = true;
	private bool _showVariables = true;
	private bool _dragEvent = false;
	private bool _mouseOver = false;

	private void Awake() {		
		GameObject character = GameObject.Find("Character");
		
		if (character == null) {
			return;
		}
		
		RpgCamera = character.GetComponent<RPGCamera>();
		RpgViewFrustum = character.GetComponent<RPGViewFrustum>();
		RpgController = character.GetComponent<RPGController>();
		RpgMotor = character.GetComponent<RPGMotor>();
		
		_usedSkybox.text = "" + TestSkyboxes[0];
		_cameraPivotLocalPositionX.text = "" + RpgCamera.CameraPivotLocalPosition.x;
		_cameraPivotLocalPositionY.text = "" + RpgCamera.CameraPivotLocalPosition.y;
		_cameraPivotLocalPositionZ.text = "" + RpgCamera.CameraPivotLocalPosition.z;
		_activateCameraControl.isOn = RpgCamera.ActivateCameraControl;
		_alwaysRotateCamera.isOn = RpgCamera.AlwaysRotateCamera;
		_rotateWithCharacter.text = RpgCamera.RotateWithCharacter.ToString(); // enum 
		_cursorLockMode.text = RpgCamera.CursorLockMode.ToString(); // enum
		_hideCursorWhenPressed.isOn = RpgCamera.HideCursorWhenPressed;
		_lockMouseX.isOn = RpgCamera.LockMouseX;
		_lockMouseY.isOn = RpgCamera.LockMouseY;
		_invertMouseX.isOn = RpgCamera.InvertMouseX;
		_invertMouseY.isOn = RpgCamera.InvertMouseY;
		_mouseXSensitivity.text = "" + RpgCamera.MouseXSensitivity;
		_mouseYSensitivity.text = "" + RpgCamera.MouseYSensitivity;
		_constrainMouseX.isOn = RpgCamera.ConstrainMouseX;
		_mouseXMin.text = "" + RpgCamera.MouseXMin;
		_mouseXMax.text = "" + RpgCamera.MouseXMax;
		_mouseYMin.text = "" + RpgCamera.MouseYMin;
		_mouseYMax.text = "" + RpgCamera.MouseYMax;
		_mouseScrollSensitivity.text = "" + RpgCamera.MouseScrollSensitivity;
		_mouseSmoothTime.text = "" + RpgCamera.MouseSmoothTime;
		_minDistance.text = "" + RpgCamera.MinDistance;
		_maxDistance.text = "" + RpgCamera.MaxDistance;
		_distanceSmoothTime.text = "" + RpgCamera.DistanceSmoothTime;
		_alignCharacterWithCam.text = RpgCamera.AlignCharacter.ToString();
		_alignCameraWhenMoving.isOn = RpgCamera.AlignCameraWhenMoving;
		_supportWalkingBackwards.isOn = RpgCamera.SupportWalkingBackwards;
		_alignCameraSmoothTime.text = "" + RpgCamera.AlignCameraSmoothTime;
		
		_occultationHandling.text = RpgViewFrustum.OcclusionHandling.ToString(); // enum
		_occultingLayers.text = "Default";
		_fadeOutAlpha.text = "" + RpgViewFrustum.FadeOutAlpha;
		_fadeInAlpha.text = "" + RpgViewFrustum.FadeInAlpha;
		_fadeOutDuration.text = "" + RpgViewFrustum.FadeOutDuration;
		_fadeInDuration.text = "" + RpgViewFrustum.FadeInDuration;
		_enableCharacterFading.isOn = RpgViewFrustum.EnableCharacterFading;
		_characterFadeOutAlpha.text = "" + RpgViewFrustum.CharacterFadeOutAlpha;
		_characterFadeStartDistance.text = "" + RpgViewFrustum.CharacterFadeStartDistance;
		_characterFadeEndDistance.text = "" + RpgViewFrustum.CharacterFadeEndDistance;
		
		_walkSpeed.text = "" + RpgMotor.WalkSpeed;
		_runSpeed.text = "" + RpgMotor.RunSpeed;
		_strafeSpeed.text = "" + RpgMotor.StrafeSpeed;
		_airborneSpeed.text = "" + RpgMotor.AirborneSpeed;
		_rotatingSpeed.text = "" + RpgMotor.RotatingSpeed;
		_sprintSpeedMultiplier.text = "" + RpgMotor.SprintSpeedMultiplier;
		_backwardsSpeedMultiplier.text = "" + RpgMotor.BackwardsSpeedMultiplier;
		_jumpHeight.text = "" + RpgMotor.JumpHeight;
		_allowedAirborneMoves.text = "" + RpgMotor.AllowedAirborneMoves;
		_moveWithMovingGround.isOn = RpgMotor.MoveWithMovingGround;
		_rotateWithRotatingGround.isOn = RpgMotor.RotateWithRotatingGround;
		_groundObjectAffectsJump.isOn = RpgMotor.GroundObjectAffectsJump;
		_slidingThreshold.text = "" + RpgMotor.SlidingThreshold;
		_fallingThreshold.text = "" + RpgMotor.FallingThreshold;
		_gravity.text = "" + RpgMotor.Gravity;
		
		ToggleVariablesWindow();
		
		_awoken = true;
	}

	private void Update() {		
		if (Input.GetButton("Fire1") || Input.GetButton("Fire2")) {
			_dragEvent = true;
		} else {
			_dragEvent = false;
			
			if (_mouseOver) {
				MouseEnter();
			} else {
				MouseOut();
			}
		}
	}
	
	public void MouseEnter() {
		if (RpgCamera == null) {
			return;
		}
		
		_mouseOver = true;
		
		if (!_dragEvent) {
			RpgCamera.ActivateCameraControl = false;
		}		
	}
	
	public void MouseOut() {
		if (RpgCamera == null) {
			return;
		}
		
		_mouseOver = false;
		
		if (!_dragEvent) {
			RpgCamera.ActivateCameraControl = true;
		}
	}
	
	public void ToggleControlsWindow() {
		_showControls = !_showControls;
		
		if (_showControls) {
			_controlsWindowButtonText.text = "<\n<\n<\n<";
			_controlsWindow.SetActive(true);
		} else {
			_controlsWindowButtonText.text = ">\n>\n>\n>";
			_controlsWindow.SetActive(false);		
		}
	}

	public void ToggleVariablesWindow() {
		_showVariables = !_showVariables;
		
		if (_showVariables) {
			_variablesWindowButtonText.text = ">\n>\n>\n>";
			_variablesWindow.SetActive(true);
		} else {
			_variablesWindowButtonText.text = "<\n<\n<\n<";
			_variablesWindow.SetActive(false);		
		}
	}
	
	public void ValueChanged() {
		if (!_awoken) {
			return;
		}
	
		if (RpgCamera == null || RpgViewFrustum == null || RpgController == null || RpgMotor == null) {
			return;
		}
		
		RpgCamera.CameraPivotLocalPosition.x = float.Parse(_cameraPivotLocalPositionX.text);
		RpgCamera.CameraPivotLocalPosition.y = float.Parse(_cameraPivotLocalPositionY.text);
		RpgCamera.CameraPivotLocalPosition.z = float.Parse(_cameraPivotLocalPositionZ.text);
		RpgCamera.ActivateCameraControl = _activateCameraControl.isOn;
		RpgCamera.AlwaysRotateCamera = _alwaysRotateCamera.isOn; 		
		RpgCamera.HideCursorWhenPressed = _hideCursorWhenPressed.isOn;
		RpgCamera.LockMouseX = _lockMouseX.isOn;
		RpgCamera.LockMouseY = _lockMouseY.isOn;
		RpgCamera.InvertMouseX = _invertMouseX.isOn;
		RpgCamera.InvertMouseY = _invertMouseY.isOn;
		RpgCamera.MouseXSensitivity = float.Parse(_mouseXSensitivity.text);
		RpgCamera.MouseYSensitivity = float.Parse(_mouseYSensitivity.text);
		RpgCamera.ConstrainMouseX = _constrainMouseX.isOn;
		RpgCamera.MouseXMin = float.Parse(_mouseXMin.text);
		RpgCamera.MouseXMax = float.Parse(_mouseXMax.text);
		RpgCamera.MouseYMin = float.Parse(_mouseYMin.text);
		RpgCamera.MouseYMax = float.Parse(_mouseYMax.text);
		RpgCamera.MouseScrollSensitivity = float.Parse(_mouseScrollSensitivity.text);
		RpgCamera.MouseSmoothTime = float.Parse(_mouseSmoothTime.text);
		RpgCamera.MinDistance = float.Parse(_minDistance.text);
		RpgCamera.MaxDistance = float.Parse(_maxDistance.text);
		RpgCamera.DistanceSmoothTime = float.Parse(_distanceSmoothTime.text);
		RpgCamera.AlignCameraWhenMoving = _alignCameraWhenMoving.isOn;
		RpgCamera.SupportWalkingBackwards = _supportWalkingBackwards.isOn;
		RpgCamera.AlignCameraSmoothTime = float.Parse(_alignCameraSmoothTime.text);
		
		RpgViewFrustum.FadeOutAlpha = float.Parse(_fadeOutAlpha.text);
		RpgViewFrustum.FadeInAlpha = float.Parse(_fadeInAlpha.text);
		RpgViewFrustum.FadeOutDuration = float.Parse(_fadeOutDuration.text);
		RpgViewFrustum.FadeInDuration = float.Parse(_fadeInDuration.text);
		RpgViewFrustum.EnableCharacterFading = _enableCharacterFading.isOn;
		RpgViewFrustum.CharacterFadeOutAlpha = float.Parse(_characterFadeOutAlpha.text);
		RpgViewFrustum.CharacterFadeStartDistance = float.Parse(_characterFadeStartDistance.text);
		RpgViewFrustum.CharacterFadeEndDistance = float.Parse(_characterFadeEndDistance.text);
				
		RpgMotor.WalkSpeed = float.Parse(_walkSpeed.text);
		RpgMotor.RunSpeed = float.Parse(_runSpeed.text);
		RpgMotor.StrafeSpeed = float.Parse(_strafeSpeed.text);
		RpgMotor.AirborneSpeed = float.Parse(_airborneSpeed.text);
		RpgMotor.RotatingSpeed = float.Parse(_rotatingSpeed.text);
		RpgMotor.SprintSpeedMultiplier = float.Parse(_sprintSpeedMultiplier.text);
		RpgMotor.BackwardsSpeedMultiplier = float.Parse(_backwardsSpeedMultiplier.text);
		RpgMotor.JumpHeight = float.Parse(_jumpHeight.text);
		RpgMotor.AllowedAirborneMoves = int.Parse(_allowedAirborneMoves.text);
		RpgMotor.MoveWithMovingGround = _moveWithMovingGround.isOn;
		RpgMotor.RotateWithRotatingGround = _rotateWithRotatingGround.isOn;
		RpgMotor.GroundObjectAffectsJump = _groundObjectAffectsJump.isOn;
		RpgMotor.SlidingThreshold = float.Parse(_slidingThreshold.text);
		RpgMotor.FallingThreshold = float.Parse(_fallingThreshold.text);
		RpgMotor.Gravity = float.Parse(_gravity.text);
	}

	public void ClickPreset1() {
		Application.LoadLevel("Preset 1");
	}

	public void ClickPreset2() {
		Application.LoadLevel("Preset 2");
	}

	public void ClickPreset3() {
		Application.LoadLevel("Preset 3");
	}

	public void ClickPreset4() {
		Application.LoadLevel("Preset 4");
	}
	
	public void ClickUsedSkybox() {
		int index = Array.IndexOf(TestSkyboxes, _usedSkybox.text);
		index = (index + 1) % PossibleSkyboxes.Length;
		RpgCamera.SetUsedSkybox(PossibleSkyboxes[index]);
		_usedSkybox.text = TestSkyboxes[index];
	}

	public void ClickRotateWithCharater() {
		int index = Array.IndexOf(RotateWithCharacterOptions, _rotateWithCharacter.text);
		index = (index + 1) % RotateWithCharacterOptions.Length;
		//RpgCamera.RotateWithCharacter = (RotateWithCharacter)index;
		_rotateWithCharacter.text = RotateWithCharacterOptions[index];
	}
	
	public void ClickCursorLockMode() {
		int index = Array.IndexOf(CursorLockModes, _cursorLockMode.text);
		index = (index + 1) % CursorLockModes.Length;
		RpgCamera.CursorLockMode = (CursorLockMode)index;
		_cursorLockMode.text = CursorLockModes[index];
	}
	
	public void ClickOccultationHandling() {
		int index = Array.IndexOf(OccultationHandlings, _occultationHandling.text);
		index = (index + 1) % OccultationHandlings.Length;
		RpgViewFrustum.OcclusionHandling = (OcclusionHandling)index;
		_occultationHandling.text = OccultationHandlings[index];
	}
	
	public void ClickOccultatingLayers() {
		int index = Array.IndexOf(OccultingLayersOptions, _occultingLayers.text);
		index = (index + 1) % OccultingLayersOptions.Length;
		if (OccultingLayersOptions[index] == "Nothing") {
			RpgViewFrustum.OccludingLayers = 0;
		} else if (OccultingLayersOptions[index] == "Everything") {
			RpgViewFrustum.OccludingLayers = -1;
		} else {
			RpgViewFrustum.OccludingLayers = 1 << LayerMask.NameToLayer(OccultingLayersOptions[index]);		
		}
		_occultingLayers.text = OccultingLayersOptions[index];
	}
	
	public void ClickAlignCharacter() {
		int index = Array.IndexOf(AlignCharacterOptions, _alignCharacterWithCam.text);
		index = (index + 1) % AlignCharacterOptions.Length;
		RpgCamera.AlignCharacter = (AlignCharacter)index;
		_alignCharacterWithCam.text = AlignCharacterOptions[index];
	}
	
	public void ResetValues() {
		_usedSkybox.text = TestSkyboxes[0];
		RpgCamera.SetUsedSkybox(PossibleSkyboxes[0]);
		_cameraPivotLocalPositionX.text = "" + 0;
		_cameraPivotLocalPositionY.text = "" + 1.1;
		_cameraPivotLocalPositionZ.text = "" + 0;
		_activateCameraControl.isOn = true;
		_alwaysRotateCamera.isOn = false; 
		_rotateWithCharacter.text = "RotationStoppingInput"; 
		RpgCamera.RotateWithCharacter = (RotateWithCharacter)Array.IndexOf(RotateWithCharacterOptions, _rotateWithCharacter.text);
		_cursorLockMode.text = "Confined";
		RpgCamera.CursorLockMode = (CursorLockMode)Array.IndexOf(CursorLockModes, _cursorLockMode.text);
		_hideCursorWhenPressed.isOn = true;
		_lockMouseX.isOn = false;
		_lockMouseY.isOn = false;
		_invertMouseX.isOn = false;
		_invertMouseY.isOn = true;
		_mouseXSensitivity.text = "" + 8.0;
		_mouseYSensitivity.text = "" + 8.0;
		_constrainMouseX.isOn = false;
		_mouseXMin.text = "" + (-90.0);
		_mouseXMax.text = "" + 90.0;
		_mouseYMin.text = "" + (-89.5);
		_mouseYMax.text = "" + 89.5;
		_mouseScrollSensitivity.text = "" + 15.0;
		_mouseSmoothTime.text = "" + 0.08;
		_minDistance.text = "" + 0;
		_maxDistance.text = "" + 20.0;
		_distanceSmoothTime.text = "" + 0.7;
		_alignCharacterWithCam.text = "OnAlignmentInput";
		RpgCamera.AlignCharacter = (AlignCharacter)Array.IndexOf(AlignCharacterOptions, _alignCharacterWithCam.text);	
		_alignCameraWhenMoving.isOn = true;
		_supportWalkingBackwards.isOn = true;
		_alignCameraSmoothTime.text = "" + 0.2;
		
		_occultationHandling.text = "TagDependent";
		RpgViewFrustum.OcclusionHandling = (OcclusionHandling)Array.IndexOf(OccultationHandlings, _occultationHandling.text);
		_occultingLayers.text = "Default";
		RpgViewFrustum.OccludingLayers = 1;
		_fadeOutAlpha.text = "" + 0.2;
		_fadeInAlpha.text = "" + 1.0;
		_fadeOutDuration.text = "" + 0.2;
		_fadeInDuration.text = "" + 0.2;
		_enableCharacterFading.isOn = true;
		_characterFadeOutAlpha.text = "" + 0;
		_characterFadeStartDistance.text = "" + 1.2;
		_characterFadeEndDistance.text = "" + 0.8;
		
		_walkSpeed.text = "" + 2.0;
		_runSpeed.text = "" + 10.0;
		_strafeSpeed.text = "" + 10.0;
		_airborneSpeed.text = "" + 2.0;
		_rotatingSpeed.text = "" + 2.5;
		_sprintSpeedMultiplier.text = "" + 2.0;
		_backwardsSpeedMultiplier.text = "" + 0.2;
		_jumpHeight.text = "" + 10.0;
		_allowedAirborneMoves.text = "" + 1;		
		_moveWithMovingGround.isOn = true;
		_rotateWithRotatingGround.isOn = true;
		_groundObjectAffectsJump.isOn = true;
		_slidingThreshold.text = "" + 40.0;
		_fallingThreshold.text = "" + 6.0;
		_gravity.text = "" + 20.0;
	}
}
