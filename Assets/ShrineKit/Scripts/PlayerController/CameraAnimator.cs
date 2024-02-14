using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraAnimator : MonoBehaviour
{
	public AnimationCurve speedFOV;
	public Vector2 minMaxFOV = new Vector2(85f, 100f); //based on 90
	public float maxSpeedForFOV = 15f;
	public float maxSpeedForTilt = 15f;
	public Camera cam;
	public PlayerMovement pm;
	private float desiredFOV;
	public float fovLerpSpeed = 50f;
	public Transform pivotCam;
	private Vector3 pivCamDesiredRot;
	public float pivCamLerpSpeedMax = 8f;
	public float tiltIntensity = 0.2f;

	private void LateUpdate ()
	{
		UpdateFOV();
		UpdateTilt();
	}

	private void UpdateFOV ()
	{
		desiredFOV = NormFov(Mathf.Clamp(Util.Map(speedFOV.Evaluate(Mathf.Clamp01(pm.clampedVelo().magnitude / maxSpeedForFOV)), 0f, 1f, minMaxFOV.x, minMaxFOV.y), minMaxFOV.x, minMaxFOV.y), false);
		cam.fieldOfView = Mathf.MoveTowards(cam.fieldOfView, desiredFOV, Time.deltaTime * fovLerpSpeed);
	}

	private float NormFov (float fov, bool fp)
	{
		return ((fp)? Settings.defaultFPFOV : Settings.defaultFOV) * (fov / 90);
	}

	private void UpdateTilt ()
	{
		Vector3 lv = pivotCam.transform.InverseTransformDirection(pm.clampedVelo());
		pivCamDesiredRot = new Vector3(lv.z * tiltIntensity, 0f, -lv.x * tiltIntensity);

		float speedFrac = pm.clampedVelo().magnitude / maxSpeedForTilt;
		pivotCam.localRotation = Quaternion.RotateTowards(pivotCam.localRotation, Quaternion.Euler(pivCamDesiredRot), Time.deltaTime * pivCamLerpSpeedMax);
	}
}
