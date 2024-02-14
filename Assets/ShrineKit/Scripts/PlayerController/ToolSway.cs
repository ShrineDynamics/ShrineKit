using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ToolSway : MonoBehaviour
{
    public Transform toolSwayPivot;
    public float toolSwaySpeed;
	public float toolSwaySpeedVerticalMult;
    public float toolSwayLerpSpeed;

	private void Update()
	{
		if(!Settings.paused)
		{
			UpdatePivot();
		}
	}

	private void UpdatePivot()
	{
		toolSwayPivot.localRotation = SwayTool(toolSwayPivot.localRotation, Vector3.right, Vector3.up, 1.0f);
	}

	Quaternion SwayTool(Quaternion source, Vector3 rAngle, Vector3 uAngle, float mult = 1.0f)
	{
		Quaternion rotX = Quaternion.AngleAxis(-Input.GetAxisRaw("Mouse Y") * (toolSwaySpeed * toolSwaySpeedVerticalMult) * mult, rAngle);
		Quaternion rotY = Quaternion.AngleAxis(Input.GetAxisRaw("Mouse X") * toolSwaySpeed * mult, uAngle);
		Quaternion tSway = rotX * rotY;
		return Quaternion.Slerp(source, tSway, toolSwayLerpSpeed * Time.deltaTime);
	}
}
