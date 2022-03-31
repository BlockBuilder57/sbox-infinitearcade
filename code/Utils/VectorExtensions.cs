using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Sandbox;

internal static class VectorExtensions
{
	public static Vector3 RotateAroundPoint(this Vector3 vec, Vector3 pivot, Rotation rot)
	{
		return rot * (vec - pivot) + pivot;
	}

	public static float NormalDot(this Vector3 a, Vector3 b)
	{
		return Vector3.Dot(a.Normal, b.Normal).Clamp(-1f, 1f);
	}
}
