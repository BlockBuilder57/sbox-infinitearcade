using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Sandbox;

namespace infinitearcade
{
	public static partial class IADebugging
	{
		[ConVar.Replicated]
		public static bool debug_camera { get; set; } = false;

		public static Vector2 Offset = new Vector2(20, 20) + (Host.IsClient ? Vector2.Left * Screen.Width / 2 : 0);
		public static int LineOffset = 0;

		private static readonly Color m_colClient = new Color(0.41f, 0.49f, 0.61f) * 1.5f; // tf2 blue * 1.5
		private static readonly Color m_colServer = new Color(0.71f, 0.36f, 0.30f) * 1.5f; // tf2 red * 1.5

		public static void ResetLineOffset() => LineOffset = 0;

		public static void ScreenText(string text, float duration = 0f)
		{
			Color col = Host.IsMenuOrClient ? m_colClient : m_colServer;
			DebugOverlay.ScreenText(Offset, LineOffset++, col, text, duration);
		}

		public static void ScreenText(string text, Color color, float duration = 0f)
		{
			DebugOverlay.ScreenText(Offset, LineOffset++, color, text, duration);
		}

		public static void FrameSimulate(Client cl)
		{
			// all clientside stuff

			if (debug_camera)
			{
				const int pad = 14;
				Camera cam = Game.Current.FindActiveCamera() as Camera;
				float fov = cam.FieldOfView;

				if (cam.FieldOfView == 0)
					fov = float.Parse(ConsoleSystem.GetValue("default_fov"), CultureInfo.InvariantCulture);

				if (LineOffset > 0)
					LineOffset++;

				ScreenText($"{"Cam Type".PadLeft(pad)}: {cam.GetType()}");
				ScreenText($"{"Cam Viewer".PadLeft(pad)}: {cam.Viewer}");
				ScreenText($"{"Cam Position".PadLeft(pad)}: {cam.Position:F2}");
				ScreenText($"{"Cam Rotation".PadLeft(pad)}: {cam.Rotation.Angles():F2}");
				ScreenText($"{"Cam Projection".PadLeft(pad)}: {(cam.Ortho ? "Orthographic" : "Perspective")}");
				if (!cam.Ortho)
					ScreenText($"{"Cam FOV".PadLeft(pad)}: {fov} {(cam.FieldOfView == 0 ? "(from default_fov)" : "")}");
				else
					ScreenText($"{"Cam Ortho Size".PadLeft(pad)}: {cam.OrthoSize}");
			}
		}
	}
}
