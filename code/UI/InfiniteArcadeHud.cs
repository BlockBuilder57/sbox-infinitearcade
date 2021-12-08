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

		public InfiniteArcadeHud()
		{
			if (!Host.IsClient)
				return;

			Current = this;

			StyleSheet.Load("UI/InfiniteArcadeHud.scss");

			// s&box base things
			AddChild<CrosshairCanvas>();
			CrosshairCanvas.SetCrosshair(new StandardCrosshair());
			AddChild<ChatBox>();
			AddChild<VoiceList>();
			AddChild<KillFeed>();

			// custom stuff
			AddChild<PlayerStatus>();
			AddChild<WeaponStatus>();
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
