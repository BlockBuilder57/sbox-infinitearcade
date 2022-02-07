using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Sandbox;
using Sandbox.UI;
using Sandbox.UI.Construct;

namespace infinitearcade.UI
{
	public class InventoryIcon : Panel
	{
		public IACarriable Carriable;
		public Label Name;
		public Image Icon;

		public InventoryIcon(Panel parent, IACarriable carriable)
		{
			Parent = parent;
			Carriable = carriable;
			Name = Add.Label(carriable.Name);
			//Icon = Add.Image("https://thiscatdoesnotexist.com/");

			StyleSheet.Load("UI/InventoryIcon.scss");
		}
	}
}
