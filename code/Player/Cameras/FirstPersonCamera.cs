using Sandbox;

namespace infinitearcade
{
	public partial class FirstPersonCamera : CameraMode
	{
		private float m_realZNear = 0;
		private float m_lastScale = 0;

		/*public FirstPersonCamera(float zNear = 0, float zFar = 0) : base()
		{
			m_realZNear = zNear;

			ZFar = zFar;
			ZNear = m_realZNear * m_lastScale;
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

			if (m_lastScale != pawn.Scale || m_realZNear == 0)
			{
				m_lastScale = pawn.Scale;
				ZNear = m_realZNear * m_lastScale;
			}

			Viewer = pawn;
		}

		public void SetZNear(float zNear)
		{
			m_realZNear = zNear;
		}
	}
}
