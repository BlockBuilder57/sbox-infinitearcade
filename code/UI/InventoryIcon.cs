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
		public ScenePanel Scene;

		public InventoryIcon(Panel parent, IACarriable carriable)
		{
			Parent = parent;
			Carriable = carriable;
			Name = Add.Label(carriable.Definition?.Identifier);
			//Icon = Add.Image("https://thiscatdoesnotexist.com/");

			StyleSheet.Load("UI/InventoryIcon.scss");

			SceneWorld world = new();

			new SceneModel(world, carriable.Model, Transform.Zero);

			Vector3 center = carriable.Model.Bounds.Center + Vector3.Left * 64;
			new SceneLight(world, center, 512, Color.White);
			Scene = Add.ScenePanel(world, center, Rotation.From(0, -90, 0), 45);
			Scene.RenderOnce = true;
			Scene.Style.Width = Length.Percent(100);
			Scene.Style.Height = Length.Percent(100);
		}
	}
}
