using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraMan : MonoBehaviour
{
	public static CameraMan singleton;
	public Transform followPoint;
	public Transform toRotate;
	private bool dead;
	public GameObject FPRig;
	public Transform playerTransf;
	public float minMaxLookupAngle;
	public bool mouseLook = true;
	public float mouseSens = 5.0f;

	private void Awake()
	{
		singleton = this;
	}

	private void LateUpdate()
	{
		FollowPosition();
	}

	private void FollowPosition ()
	{
		transform.position = followPoint.position;

		if (canMouseLook())
		{
			float x = Util.ClampAngle(toRotate.localEulerAngles.x + ((-getMouseY()) * Time.fixedDeltaTime), -minMaxLookupAngle, minMaxLookupAngle);

			Quaternion r = Quaternion.Euler(x, toRotate.localEulerAngles.y + (getMouseX() * Time.fixedDeltaTime), 0f);

			toRotate.localRotation = r;
		}
	}

	private float getMouseY()
	{
		return Input.GetAxis("Mouse Y") * (mouseSens * Settings.lookSensitivity);
	}

	private float getMouseX()
	{
		return Input.GetAxis("Mouse X") * (mouseSens * Settings.lookSensitivity);
	}

	public bool canMouseLook()
	{
		return mouseLook && !Settings.paused;
	}
}
