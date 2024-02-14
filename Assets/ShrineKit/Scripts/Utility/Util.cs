using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public static class Util
{
	public static float Map(this float x, float x1, float x2, float y1, float y2)
	{
		var m = (y2 - y1) / (x2 - x1);
		var c = y1 - m * x1; // point of interest: c is also equal to y2 - m * x2, though float math might lead to slightly different results.

		return m * x + c;
	}

	public static float MapClamped(float OldValue, float OldMin, float OldMax, float NewMin, float NewMax)
	{
		float OldRange = (OldMax - OldMin);
		float NewRange = (NewMax - NewMin);
		float NewValue = (((OldValue - OldMin) * NewRange) / OldRange) + NewMin;

		return (NewValue);
	}

	public static Vector3 Vector3Random()
	{
		return new Vector3(Random.Range(-1f, 1f), Random.Range(-1f, 1f), Random.Range(-1f, 1f)).normalized;
	}

	public static string RemoveWhitespace(this string str)
	{
		if (str == null)
		{
			return "";
		}
		return string.Join("", str.Split(default(string[]), System.StringSplitOptions.RemoveEmptyEntries));
	}

	public static bool inBounds(int index, int arraySize)
	{
		return (index >= 0) && (index < arraySize);
	}

	//credit to DerDicke on Unity Awnsers: 
	//https://answers.unity.com/questions/659932/how-do-i-clamp-my-rotation.html
	public static float ClampAngle(float angle, float from, float to)
	{
		// accepts e.g. -80, 80
		if (angle < 0f) angle = 360 + angle;
		if (angle > 180f) return Mathf.Max(angle, 360 + from);
		return Mathf.Min(angle, to);
	}

	//UNTESTED
	public static bool AngleInLimits(float angle, float from, float to)
	{
		// accepts e.g. -80, 80
		if (angle < 0f) angle = 360 + angle;
		if (angle > 180f) return ((Mathf.Max(angle, 360 + from) == angle) ? true : false);
		return (Mathf.Min(angle, to) == angle) ? true : false;
	}

	//https://stackoverflow.com/questions/51905268/how-to-find-closest-point-on-line
	// For finite lines:
	public static Vector3 GetClosestPointOnFiniteLine(Vector3 point, Vector3 line_start, Vector3 line_end)
	{
		Vector3 line_direction = line_end - line_start;
		float line_length = line_direction.magnitude;
		line_direction.Normalize();
		float project_length = Mathf.Clamp(Vector3.Dot(point - line_start, line_direction), 0f, line_length);
		return line_start + line_direction * project_length;
	}

	// For infinite lines:
	public static Vector3 GetClosestPointOnInfiniteLine(Vector3 point, Vector3 line_start, Vector3 line_end)
	{
		return line_start + Vector3.Project(point - line_start, line_end - line_start);
	}

	public static bool ValidateAs<T> (object @object, out object result)
	{
		if(@object is T)
		{
			result = (T)@object;
			return true;
		}
		else
		{
			result = @object;
			return false;
		}
	}

	public static Color ColorModifyAlpha (Color c, float a)
	{
		c.a = a;
		return c;
	}

	public static float ElapsedSince (float t)
	{
		return Time.time - t;
	}
}