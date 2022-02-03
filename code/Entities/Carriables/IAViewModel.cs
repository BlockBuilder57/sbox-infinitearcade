using Sandbox;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace infinitearcade
{
	public class IAViewModel : BaseViewModel
	{
		public override void PostCameraSetup(ref CameraSetup camSetup)
		{
			EnableDrawing = Local.Pawn == Owner;

			if (Local.Pawn == Owner)
				base.PostCameraSetup(ref camSetup);
		}
	}
}
