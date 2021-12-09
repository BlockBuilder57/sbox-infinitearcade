using Sandbox;
using Sandbox.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace infinitearcade.UI
{
	public partial class InfiniteArcadeHud : RootPanel
	{
		public static InfiniteArcadeHud Current { get; set; }

		private CrosshairCanvas m_crosshairCanvas;

		public InfiniteArcadeHud()
		{
			if (!Host.IsClient)
				return;

			Current = this;
			Local.Hud = this;

			StyleSheet.Load("UI/InfiniteArcadeHud.scss");

			// s&box base things
			m_crosshairCanvas = AddChild<CrosshairCanvas>();
			CrosshairCanvas.SetCrosshair(new StandardCrosshair());
			AddChild<ChatBox>();
			AddChild<VoiceList>();
			AddChild<KillFeed>();

			// custom stuff
			AddChild<PlayerStatus>();
			AddChild<WeaponStatus>();
		}

		public override void Tick()
		{
			base.Tick();

			ArcadePlayer player = Local.Pawn as ArcadePlayer;
			if (player == null) return;

			// I don't need the player right now but I'm keeping it for the future
			m_crosshairCanvas?.SetClass("hidden", player.Camera is not FirstPersonCamera);
		}

		[Event.Hotload]
		public static void OnHotReloaded()
		{
			if (!Host.IsClient)
				return;

			InfiniteArcadeHud.Current?.Delete();
			InfiniteArcadeHud hud = new();
		}
	}
}
