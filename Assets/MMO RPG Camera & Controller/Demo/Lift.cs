using UnityEngine;
using System.Collections;

public class Lift : MonoBehaviour {

	public Transform Pose1;
	public Transform Pose2;
	public float smoothTime = 1.0f;
	
	private Transform _currentTargetPose;

	private void Start() {
		_currentTargetPose = Pose2;
	}

	private void FixedUpdate() {
		if (Vector3.Distance(transform.position, _currentTargetPose.position) < 0.05f
		    && Quaternion.Angle(transform.rotation, _currentTargetPose.rotation) < 1.0f) {
			if (_currentTargetPose == Pose1) {
				_currentTargetPose = Pose2;
			} else {
				_currentTargetPose = Pose1;
			}
		}

		transform.position = Vector3.Lerp(transform.position, _currentTargetPose.position, smoothTime * Time.deltaTime);
		transform.rotation = Quaternion.Lerp(transform.rotation, _currentTargetPose.rotation, smoothTime * Time.deltaTime);
	}
}
