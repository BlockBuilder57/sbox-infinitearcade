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
		public Label health;
		
		public InfiniteArcadeHud()
		{
			if (!IsClient)
				return;

			RootPanel.SetTemplate("/ui/infinitearcade.html");
		}
	}
}
