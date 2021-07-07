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
		public InfiniteArcadeHud()
		{
			if (!IsClient)
				return;

			RootPanel.AddChild<ChatBox>();
			RootPanel.AddChild<VoiceList>();

			RootPanel.AddChild<Status>();
		}
	}
}
