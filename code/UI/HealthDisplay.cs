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

		public Entity Ent { get; set; }

		public HealthDisplay(Entity entity)
		{
			StyleSheet.Load("/ui/HealthDisplay.scss");

			Ent = entity;

			Health = Add.Label("...", "numberDisplay");
			Armor = Add.Label("...", "numberDisplay hidden");
			ArmorMult = Armor.Add.Label("...", "armorMult");
		}

		public override void Tick()
		{
			base.Tick();

			if (!Ent.IsValid())
			{
				if (Health != null)
					Health.Text = "...";
				return;
			}

			if (Health != null)
				Health.Text = Ent.Health.CeilToInt().ToString();

			Armor?.SetClass("hidden", true);

			ArcadePlayer player = Ent as ArcadePlayer;
			if (player == null)
				return;

			if (player.GodMode != ArcadePlayer.GodModes.Mortal && Health != null)
				Health.Text += " (∞)";

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
