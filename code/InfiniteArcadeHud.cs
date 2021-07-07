using Sandbox;
using Sandbox.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace infinitearcade
{
	public partial class InfiniteArcadeHud : Sandbox.HudEntity<RootPanel>
	{
		public InfiniteArcadeHud()
		{
			if (IsClient)
			{
				RootPanel.SetTemplate("/InfiniteArcade.html");
			}
		}
	}
}
