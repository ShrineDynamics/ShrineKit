using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HeadbobController : MonoBehaviour
{
	private PlayerMovement movementScript;
	public Transform camAnimatorHandle;
	public SpringUtils.tDampedSpringMotionParams headbounceSpringParams = new SpringUtils.tDampedSpringMotionParams();
	public float headbounceAngularFrequency;
	public float headbounceDampingRatio;
	private float headbounceVeloRef;
	private float headbouncePosRef;
	public Transform camEquilib;

	private bool dead;

	private void Start ()
	{
		movementScript = GetComponent<PlayerMovement>();
		headbouncePosRef = camAnimatorHandle.position.y;
	}

	private void LateUpdate ()
	{
		if(!dead)
		MoveCamera();
	}

	private float HeadBounce ()
	{
		SpringUtils.CalcDampedSpringMotionParams(ref headbounceSpringParams, Time.deltaTime, headbounceAngularFrequency, headbounceDampingRatio);
		SpringUtils.UpdateDampedSpringMotion(ref headbouncePosRef, ref headbounceVeloRef, camEquilib.transform.position.y, headbounceSpringParams);
		//Debug.Log($"pos: {headbouncePosRef} vel: {headbounceVeloRef} gol: {camEquilib.transform.position.y}");
		return headbouncePosRef;
	}

	private void MoveCamera ()
	{
		camAnimatorHandle.position = new Vector3(camAnimatorHandle.position.x, HeadBounce(), camAnimatorHandle.position.z);
	}

	public void PlayerDeath ()
	{
		dead = true;
		camAnimatorHandle.localPosition = Vector3.zero;
	}
}
