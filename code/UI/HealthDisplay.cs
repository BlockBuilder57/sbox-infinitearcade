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
	public class HealthDisplay : Panel
	{
		public Label Health;
		public Label Armor;
		public Label ArmorMult;

		private Entity ent;

		public HealthDisplay(Entity entity)
		{
			StyleSheet.Load("/ui/HealthDisplay.scss");

			ent = entity;

			Health = Add.Label("100", "numberDisplay");
			Armor = Add.Label("100", "numberDisplay hidden");
			ArmorMult = Armor.Add.Label("x1.0", "armorMult");
		}

		public override void Tick()
		{
			base.Tick();

			if (!ent.IsValid())
				return;

			if (Health != null)
				Health.Text = ent.Health.CeilToInt().ToString();

			Armor?.SetClass("hidden", true);

			ArcadePlayer player = ent as ArcadePlayer;
			if (player == null)
				return;

			Armor?.SetClass("hidden", player.Armor <= 0);
			if (Armor != null && !Armor.HasClass("hidden"))
			{
				Armor.Text = player.Armor.CeilToInt().ToString();
				if (ArmorMult != null)
					ArmorMult.Text = $"x{player.ArmorMultiplier:F1}";
			}
		}
	}
}
