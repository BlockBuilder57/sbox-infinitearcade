using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CubicKitsune;
using Sandbox;
using Sandbox.UI;
using Sandbox.UI.Construct;

namespace infinitearcade.UI
{
	public class InventoryIcon : Panel
	{
		public CKCarriable Carriable;
		public Label Name;
		public Image Icon;
		public ScenePanel Scene;

		public InventoryIcon(Panel parent, CKCarriable carriable)
		{
			Parent = parent;
			Carriable = carriable;
			Name = Add.Label(carriable.Identifier);

			StyleSheet.Load("UI/InventoryIcon.scss");

			if (carriable.Model == null || carriable.Model.IsError)
				return;

			SceneWorld world = new();

			SceneModel scene_carriable = new(world, carriable.Model, Transform.Zero);

			Vector3 center = carriable.Model.Bounds.Center + Vector3.Left * 64;
			SceneLight scene_light = new(world, center, 512, Color.White);
			Scene = Add.ScenePanel(world, center, Rotation.From(0, -90, 0), 45);
			Scene.RenderOnce = true;
			Scene.Style.Width = Length.Percent(100);
			Scene.Style.Height = Length.Percent(100);
		}
	}
}
