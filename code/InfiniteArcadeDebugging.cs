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
		// clientside-only convars
		[ClientVar] public static bool debug_camera { get; set; } = false;
		[ClientVar] public static bool cl_showmap { get; set; } = false;

		// shared convars
		[ConVar.ClientData(Help = "Debugs a client by network identity (entity number).")]
		public static int debug_client { get; set; } = 0;
		[ConVar.ClientData(Help = "Enable debugging a client's inventory?")]
		public static bool debug_client_inventory { get; set; } = false;
		[ConVar.ClientData(Help = "Enable debugging a client's pawn controller?")]
		public static bool debug_client_pawncontroller { get; set; } = false;

		public static Vector2 Offset = new Vector2(20, 20) + (Host.IsClient ? Vector2.Left * Screen.Width / 2 : 0);
		public static Color GetSideColor() => Host.IsMenuOrClient ? m_colClient : m_colServer;
		public static float TicksafeDuration => Host.IsClient ? Global.TickInterval : 0;
		public static To ToLocal = default(To);

		public static List<QueuedText> QueuedTexts = new();

		public struct QueuedText
		{
			public string Text = "";
			public Vector2 ScreenPosition = Vector2.Zero;
			public Color Color = Color.Yellow;
			public TimeUntil TimeUntil
			{ 
				get { return m_timeUntil; }
				set { m_timeUntil = value; Duration = Math.Abs(value); } 
			}
			public float Duration { private set; get; } = 0;

			private TimeUntil m_timeUntil;
		}

		private static readonly Color m_colClient = new Color(0.41f, 0.49f, 0.61f) * 1.5f; // tf2 blue * 1.5
		private static readonly Color m_colServer = new Color(0.71f, 0.36f, 0.30f) * 1.5f; // tf2 red * 1.5

		private static readonly Vector2 m_gamemodeOffset = new(2, -8 + Screen.Height);

		public static void ScreenText(To to, string text, float duration = 0f)
		{
			if (!to.Contains(ToLocal.FirstOrDefault()))
				return;

			//text = $"{ToLocal.First()} - {to.First()}";

			QueuedTexts.Add(new QueuedText()
			{
				Text = text,
				ScreenPosition = Offset,
				Color = GetSideColor(),
				TimeUntil = duration
			});
		}
		public static void ScreenText(string text, float duration = 0f) => ScreenText(ToLocal, text, duration);

		public static void ScreenText(To to, string text, Color color, float duration = 0f)
		{
			if (!to.Contains(Local.Client))
				return;

			QueuedTexts.Add(new QueuedText()
			{
				Text = text,
				ScreenPosition = Offset,
				Color = color,
				TimeUntil = duration
			});
		}

		public static void Simulate(Client cl)
		{
			// server or clientside stuff
			var cldata_debug_client = cl.GetClientData<int>(nameof(debug_client), 0);
			var cldata_debug_client_inventory = cl.GetClientData<bool>(nameof(debug_client_inventory), false);

			ToLocal = To.Single(cl);

			if (cldata_debug_client > 0 && Client.All.FirstOrDefault(x => x.NetworkIdent == cldata_debug_client)?.Pawn is ArcadePlayer player)
			{
				To toClient = To.Single(player.Client);

				if (cldata_debug_client_inventory && player.Inventory is IAInventory inv && Host.IsServer)
				{
					string debugText = $"{player.Client.Name}'s inventory: ";

					if (inv.List.Count > 0)
					{
						for (int i = 0; i < inv.List?.Count; i++)
						{
							IACarriable ent = inv.List[i];

							if (ent.IsValid())
								debugText += $"\n\t[{i}] {ent.Definition.Identifier} {ent.NetworkIdent} {ent.Definition.Bucket}:{(int)ent.Definition.Bucket}, {ent.Definition.SubBucket}";
							else
								debugText += $"\n\t[{i}] (null)";
						}
					}
					else
						debugText += "(empty)";

					ScreenText(debugText, TicksafeDuration);
				}
			}

			if (Host.IsServer)
				ProcessQueuedTexts();
		}

		public static void FrameSimulate(Client cl)
		{
			// all clientside stuff
			var cldata_debug_client = cl.GetClientData<int>("debug_client", 0);

			if (debug_camera && Game.Current.FindActiveCamera() is CameraMode camMode)
			{
				const int pad = 14;
				float fov = camMode.FieldOfView;

				if (camMode.FieldOfView == 0)
					fov = float.Parse(ConsoleSystem.GetValue("default_fov"), CultureInfo.InvariantCulture);

				ScreenText($"{"Cam Type",pad}: {camMode.GetType()}");
				ScreenText($"{"Cam Position",pad}: {camMode.Position:F2}");
				ScreenText($"{"Cam Rotation",pad}: {camMode.Rotation.Angles():F2}");
				ScreenText($"{"Cam Viewer",pad}: {camMode.Viewer}");
				ScreenText($"{"Cam Projection",pad}: {(camMode.Ortho ? "Orthographic" : "Perspective")}");
				if (!camMode.Ortho)
					ScreenText($"{"Cam FOV",pad}: {fov} {(camMode.FieldOfView == 0 ? "(from default_fov)" : "")}");
				else
					ScreenText($"{"Cam Ortho Size",pad}: {camMode.OrthoSize}");
			}

			if (cl_showmap)
			{
				DebugOverlay.ScreenText(m_gamemodeOffset, 0, Color.White, $"{Global.GameIdent} on {(!Global.MapName.Contains('.') ? @"maps\" + Global.MapName + ".vpk" : Global.MapName)}", Global.TickInterval);
			}

			// considering clientside things may update per-frame, run this here
			ProcessQueuedTexts();
		}

		public static void ProcessQueuedTexts()
		{
			int curLine = 0;

			// cap loops so we aren't drawing a million of these
			for (int i = 0; i < Math.Min(QueuedTexts.Count, 256); i++)
			{
				QueuedText text = QueuedTexts[i];

				if (string.IsNullOrWhiteSpace(text.Text))
				{
					// just skip over blank texts
					curLine++;
					QueuedTexts.RemoveAt(i--);
				}

				string sanitizedText = text.Text.Replace("\t", "    ");
				float textDuration = text.Duration <= 0 ? TicksafeDuration : text.Duration;

				// if we have time left OR we're a one-frame text and we should be displaying in the frame/tick
				if (text.TimeUntil >= 0 || (text.Duration <= 0 && text.TimeUntil > textDuration))
				{
					//sanitizedText = text.TimeUntil.ToString();
					DebugOverlay.ScreenText(text.ScreenPosition, curLine, text.Color, sanitizedText, 0);
					curLine += sanitizedText.Count(x => x == '\n') + 1;

					// remove one-frame texts
					if (text.Duration <= 0)
						QueuedTexts.RemoveAt(i--);
				}
				else // remove texts that have timed out
					QueuedTexts.RemoveAt(i--);
				
			}
		}
	}
}
