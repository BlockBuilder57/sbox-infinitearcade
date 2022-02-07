using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

internal static class VectorExtensions
{
	public static Vector3 RotateAroundPoint(this Vector3 vec, Vector3 pivot, Rotation rot)
	{
		return rot * (vec - pivot) + pivot;
	}
}
