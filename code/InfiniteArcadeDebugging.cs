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
		[ClientVar] public static bool debug_camera { get; set; } = false;
		[ClientVar] public static bool cl_showmap { get; set; } = false;

		[ConVar.ClientData(Help = "Debugs a client by network identity (entity number).")]
		public static int debug_client { get; set; } = 0;

		public static Vector2 Offset = new Vector2(20, 20) + (Host.IsClient ? Vector2.Left * Screen.Width / 2 : 0);
		public static int LineOffset = 0;

		private static readonly Color m_colClient = new Color(0.41f, 0.49f, 0.61f) * 1.5f; // tf2 blue * 1.5
		private static readonly Color m_colServer = new Color(0.71f, 0.36f, 0.30f) * 1.5f; // tf2 red * 1.5
		private static readonly Vector2 m_gamemodeOffset = new(2, -8 + Screen.Height);

		public static void ResetLineOffset() => LineOffset = 0;
		public static Color GetSideColor() => Host.IsMenuOrClient ? m_colClient : m_colServer;

		public static void ScreenText(string text, float duration = 0f)
		{
			DebugOverlay.ScreenText(Offset, LineOffset++, GetSideColor(), text, duration == 0f && Host.IsClient ? Global.TickInterval : duration);
		}

		public static void ScreenText(string text, Color color, float duration = 0f)
		{
			DebugOverlay.ScreenText(Offset, LineOffset++, color, text, duration == 0f && Host.IsClient ? Global.TickInterval : duration);
		}

		public static void ScreenText(Vector2 position, string text, int line = 0, float duration = 0f)
		{
			DebugOverlay.ScreenText(position, line, GetSideColor(), text, duration == 0f && Host.IsClient ? Global.TickInterval : duration);
		}

		public static void Simulate(Client cl)
		{
			// server or clientside stuff
			int cl_debug_client = cl.GetClientData<int>("debug_client", 0);

			if (Host.IsServer && cl.HasPermission("debug") && cl_debug_client > 0 && Client.All.FirstOrDefault(x => x.NetworkIdent == cl_debug_client)?.Pawn is ArcadePlayer player)
			{
				if (player.Inventory is IAInventory inv)
				{
					Entity ent;
					for (int i = 0; i < inv.List?.Count; i++)
					{
						ent = inv.List[i];

						if (ent.IsValid())
							ScreenText($"[{i}] {ent.Name}");
						else
							ScreenText($"[{i}] (null)");
					}

					foreach (var kvp in inv.BucketList)
					{
						ScreenText(kvp.Key);

						for (int i = 0; i < inv.BucketList[kvp.Key].Count; i++)
						{
							ent = inv.BucketList[kvp.Key][i];

							if (ent.IsValid())
								ScreenText($"    [{i}] {ent.Name}");
							else
								ScreenText($"    [{i}] (null)");
						}
					}
				}
			}
		}

		public static void FrameSimulate(Client cl)
		{
			// all clientside stuff

			if (debug_camera)
			{
				const int pad = 14;
				CameraMode cam = Game.Current.FindActiveCamera();
				float fov = cam.FieldOfView;

				if (cam.FieldOfView == 0)
					fov = float.Parse(ConsoleSystem.GetValue("default_fov"), CultureInfo.InvariantCulture);

				if (LineOffset > 0)
					LineOffset++;

				ScreenText($"{"Cam Type",pad}: {cam.GetType()}");
				ScreenText($"{"Cam Position",pad}: {cam.Position:F2}");
				ScreenText($"{"Cam Rotation",pad}: {cam.Rotation.Angles():F2}");
				ScreenText($"{"Cam Viewer",pad}: {cam.Viewer}");
				ScreenText($"{"Cam Projection",pad}: {(cam.Ortho ? "Orthographic" : "Perspective")}");
				if (!cam.Ortho)
					ScreenText($"{"Cam FOV",pad}: {fov} {(cam.FieldOfView == 0 ? "(from default_fov)" : "")}");
				else
					ScreenText($"{"Cam Ortho Size",pad}: {cam.OrthoSize}");
			}

			if (cl_showmap)
			{
				ScreenText(m_gamemodeOffset, $"{Global.GameIdent} on {(!Global.MapName.Contains('.') ? @"maps\" + Global.MapName + ".vpk" : Global.MapName)}");
			}
		}
	}
}
