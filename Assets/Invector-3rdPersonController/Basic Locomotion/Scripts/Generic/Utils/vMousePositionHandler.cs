using UnityEngine;

#if MOBILE_INPUT
using UnityStandardAssets.CrossPlatformInput;
#endif

namespace Invector.vCharacterController
{
    public class vMousePositionHandler : MonoBehaviour
    {
        public Camera mainCamera;
        protected static vMousePositionHandler _instance;
        public static vMousePositionHandler Instance
        {
            get
            {
                if (_instance == null) _instance = FindObjectOfType<vMousePositionHandler>();
                if (_instance == null)
                {
                    var go = new GameObject("MousePositionHandler");
                    _instance = go.AddComponent<vMousePositionHandler>();
                    _instance.mainCamera = Camera.main;
                }
                return _instance;
            }
        }

        public string joystickHorizontalAxis = "RightAnalogHorizontal";
        public string joystickVerticalAxis = "RightAnalogVertical";
        public float joystickSensitivity = 25f;
        Vector2 joystickMousePos;

        public virtual Vector2 mousePosition
        {
            get
            {
                var inputDevice = vInput.instance.inputDevice;
                switch (inputDevice)
                {
                    case InputDevice.MouseKeyboard:
                        return Input.mousePosition;
                    case InputDevice.Joystick:
                        joystickMousePos.x += Input.GetAxis("RightAnalogHorizontal") * joystickSensitivity;
                        joystickMousePos.x = Mathf.Clamp(joystickMousePos.x, -(Screen.width * 0.5f), (Screen.width * 0.5f));
                        joystickMousePos.y += Input.GetAxis("RightAnalogVertical") * joystickSensitivity;
                        joystickMousePos.y = Mathf.Clamp(joystickMousePos.y, -(Screen.height * 0.5f), (Screen.height * 0.5f));
                        var screenCenter = new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);
                        var result = joystickMousePos + screenCenter;
                        result.x = Mathf.Clamp(result.x, 0, Screen.width);
                        result.y = Mathf.Clamp(result.y, 0, Screen.height);
                        return result;
                    case InputDevice.Mobile:
#if MOBILE_INPUT
                        joystickMousePos.x += CrossPlatformInputManager.GetAxis("RightAnalogHorizontal") * joystickSensitivity;
                        joystickMousePos.x = Mathf.Clamp(joystickMousePos.x, -(Screen.width * 0.5f), (Screen.width * 0.5f));
                        joystickMousePos.y += CrossPlatformInputManager.GetAxis("RightAnalogVertical") * joystickSensitivity;
                        joystickMousePos.y = Mathf.Clamp(joystickMousePos.y, -(Screen.height * 0.5f), (Screen.height * 0.5f));
                        var mobileScreenCenter = new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);
                        var mobileResult = joystickMousePos + mobileScreenCenter;
                        mobileResult.x = Mathf.Clamp(mobileResult.x, 0, Screen.width);
                        mobileResult.y = Mathf.Clamp(mobileResult.y, 0, Screen.height);
                        return mobileResult;
#else
                        return Input.GetTouch(0).deltaPosition;
#endif

                    default: return Input.mousePosition;
                }
            }
        }

        public virtual Vector3 WorldMousePosition(LayerMask castLayer)
        {
            if (!mainCamera)
            {              
                if(!Camera.main)
                {
                    Debug.LogWarning("Trying to get the world mouse position but a MainCamera is missing from the scene");
                    return Vector3.zero;
                }
               else
                {
                    mainCamera = Camera.main;
                    return Vector3.zero;
                }
            }
            else
            {
                Ray ray = mainCamera.ScreenPointToRay(mousePosition);
                RaycastHit hit;

                if (Physics.Raycast(ray, out hit, mainCamera.farClipPlane, castLayer)) return hit.point;
                else return ray.GetPoint(mainCamera.farClipPlane);
            }
        }
    }
}