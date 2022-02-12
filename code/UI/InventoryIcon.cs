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
			Name = Add.Label(carriable.Name);
			//Icon = Add.Image("https://thiscatdoesnotexist.com/");

			StyleSheet.Load("UI/InventoryIcon.scss");

			using (SceneWorld.SetCurrent(new SceneWorld()))
			{
				Vector3 center = carriable.Model.Bounds.Center + Vector3.Left * 64;
				SceneObject.CreateModel(carriable.WorldModelPath, Transform.Zero);
				Light.Point(center, 512, Color.White);

				Scene = Add.ScenePanel(SceneWorld.Current, center, Rotation.From(0, -90, 0), 45);
				Scene.RenderOnce = true;
			}
		}
	}
}
