using Sandbox;
using Sandbox.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace infinitearcade.UI
{
	public partial class InfiniteArcadeHud : Sandbox.HudEntity<RootPanel>
	{
		public static InfiniteArcadeHud Current { get; set; }

		public InfiniteArcadeHud()
		{
			if (!IsClient)
				return;

			Current = this;

			RootPanel.StyleSheet.Load("UI/InfiniteArcadeHud.scss");

			// s&box base things
			RootPanel.AddChild<ChatBox>();
			RootPanel.AddChild<VoiceList>();
			RootPanel.AddChild<KillFeed>();

			// custom stuff
			RootPanel.AddChild<Status>();
		}

		[Event.Hotload]
		public static void OnHotReloaded()
		{
			if (Host.IsClient)
			{
				Local.Hud?.Delete();
				new InfiniteArcadeHud();
			}
		}
	}
}
