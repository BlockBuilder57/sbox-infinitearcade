using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Sandbox;

namespace CubicKitsune
{
	public class VRCamera : CameraMode
	{

		public override void Update()
		{
			ZNear = 1f;
			Viewer = Local.Pawn;
		}
	}
}
