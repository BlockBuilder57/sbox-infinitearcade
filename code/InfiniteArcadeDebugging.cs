using System;
using System.Collections.Generic;
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

		public static Vector2 Offset = Vector2.One * 30;
		public static int LineOffset = 0;

		private static readonly Color m_colClient = new Color(0.41f, 0.49f, 0.61f) * 1.5f; // tf2 blue * 1.5
		private static readonly Color m_colServer = new Color(0.71f, 0.36f, 0.30f) * 1.5f; // tf2 red * 1.5

		public static void ResetOffset() => LineOffset = Host.IsMenuOrClient ? 0 : 10;

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
			if (debug_camera)
			{
				const int pad = 14;
				Camera cam = Game.Current.FindActiveCamera() as Camera;

				IADebugging.ScreenText($"{"Cam Type".PadLeft(pad)}: {cam.GetType()}");
				IADebugging.ScreenText($"{"Cam Viewer".PadLeft(pad)}: {cam.Viewer}");
				IADebugging.ScreenText($"{"Cam Position".PadLeft(pad)}: {cam.Position:F2}");
				IADebugging.ScreenText($"{"Cam Rotation".PadLeft(pad)}: {cam.Rotation.Angles():F2}");
				IADebugging.ScreenText($"{"Cam FOV".PadLeft(pad)}: {cam.FieldOfView}");
				IADebugging.ScreenText($"{"Is ortho?".PadLeft(pad)}: {(cam.Ortho ? $"yes, size of {cam.OrthoSize}" : "no")}");
			}
		}
	}
}
