using Sandbox;

namespace infinitearcade
{
	public partial class FirstPersonCamera : CameraMode
	{
		// block: by default, Z near and far are 0
		// this is just a lie, their true values are:
		// ZNear: 7 (3 in HL1/HL:S)
		//  ZFar: ~28378 (r_mapextents * 1.73205080757f)

		private float m_realZNear = 7;

		/*public FirstPersonCamera(float zNear = 0, float zFar = 0) : base()
		{
			m_realZNear = zNear;

			ZFar = zFar;
			ZNear = m_realZNear;

			if (Local.Pawn.IsValid())
				ZNear = Local.Pawn.Scale;
		}*/

		public override void Activated()
		{
			var pawn = Local.Pawn;
			if (pawn == null) return;

			Position = pawn.EyePosition;
			Rotation = pawn.EyeRotation;
		}

		public override void Update()
		{
			var pawn = Local.Pawn;
			if (pawn == null) return;

			Position = pawn.EyePosition;
			Rotation = pawn.EyeRotation;

			ZNear = m_realZNear * pawn.Scale;

			Viewer = pawn;
		}

		public void SetZNear(float zNear)
		{
			m_realZNear = zNear;
		}
	}
}
