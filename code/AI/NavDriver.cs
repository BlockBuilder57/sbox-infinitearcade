using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Sandbox;

namespace infinitearcade
{
	public class NavDriver
	{
		public Vector3 Target;
		public NavPath OurPath;

		public List<Vector3> Points = new List<Vector3>();
		public bool IsEmpty => Points.Count <= 1;

		public void Update(Vector3 from, Vector3 to)
		{
			bool needsBuild = false;

			if (!Target.AlmostEqual(to, 5))
			{
				Target = to;
				needsBuild = true;
			}

			if (needsBuild && Time.Tick % 10 == 0)
				MakePath(from, to);

			if (Points.Count <= 1)
				return;

			var deltaToCurrent = from - Points[0];
			var deltaToNext = from - Points[1];
			var delta = Points[1] - Points[0];
			var deltaNormal = delta.Normal;

			if (deltaToNext.WithZ(0).Length < 20)
			{
				Points.RemoveAt(0);
				return;
			}

			// If we're in front of this line then
			// remove it and move on to next one
			if (deltaToNext.Normal.Dot(deltaNormal) >= 1.0f)
			{
				Points.RemoveAt(0);
			}
		}

		public float Distance(int point, Vector3 from)
		{
			if (Points.Count <= point) return float.MaxValue;

			return Points[point].WithZ(from.z).Distance(from);
		}

		public Vector3 GetDirection(Vector3 position)
		{
			if (Points.Count == 1)
			{
				return (Points[0] - position).WithZ(0).Normal;
			}

			return (Points[1] - position).WithZ(0).Normal;
		}


		public void MakePath(Vector3 start, Vector3 end)
		{
			Vector3? closestStart = NavMesh.GetClosestPoint(start);
			Vector3? closestEnd = NavMesh.GetClosestPoint(end);

			if (closestStart == null || closestEnd == null)
				return;

			NavPathBuilder buildy = NavPathBuilder.Create(closestStart.Value);
			OurPath = buildy.Build(closestEnd.Value);

			if (OurPath == null)
				return;

			//Log.Info($"making path! {OurPath.Segments.Count} segments");

			if (Points == null)
				Points = new();
			else
				Points.Clear();

			foreach (NavPathSegment seg in OurPath.Segments)
				Points.Add(seg.Position);

			DebugDraw(0.2f);
		}

		public void DebugDraw(float time, float opacity = 1.0f)
		{
			if (OurPath == null)
				return;

			var draw = CubicKitsune.Draw.ForSeconds(time);
			var lift = Vector3.Up * 2;

			draw.DepthTest = false;
			draw.WithColor(Color.White.WithAlpha(opacity)).Circle(lift + Target, Vector3.Up, 20.0f);

			int i = 0;
			var lastPoint = Vector3.Zero;
			foreach (var seg in Points)
			{
				if (i > 0)
				{
					draw.WithColor(i == 1 ? Color.Green.WithAlpha(opacity) : Color.Cyan.WithAlpha(opacity))
						.Arrow(lastPoint + lift, seg + lift, Vector3.Up, 5.0f);
				}

				lastPoint = seg;
				i++;
			}
		}
	}
}
