using Sandbox;
using Sandbox.UI;
using Sandbox.UI.Construct;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace infinitearcade.UI
{
	public class PlayerStatus : Panel
	{
		public Label pawnstatus;

		public PlayerStatus()
		{
			StyleSheet.Load("/ui/PlayerStatus.scss");

			pawnstatus = Add.Label("YOU'RE IN AN ARCADE MACHINE (press use to leave)", "pawnstatus");
			HealthDisplay healthDisplay = AddChild<HealthDisplay>();
		}

		public override void Tick()
		{
			base.Tick();

			ArcadePlayer player = Local.Pawn as ArcadePlayer;
			if (player == null) return;

			SetClass("hidden", player.LifeState == LifeState.Dead);
			if (player.LifeState == LifeState.Dead)
				return;

			pawnstatus?.SetClass("hidden", !(player is ArcadeMachinePlayer));
		}

		private class HealthDisplay : Panel
		{
			public Label Health;
			public Label Armor;
			public Label ArmorMult;

			public HealthDisplay()
			{
				Health = Add.Label("100", "numberDisplay");
				Armor = Add.Label("100", "numberDisplay");
				ArmorMult = Armor.Add.Label("x1.0", "armorMult");
			}

			public override void Tick()
			{
				base.Tick();

				ArcadePlayer player = Local.Pawn as ArcadePlayer;
				if (player == null) return;

				if (Health != null)
					Health.Text = player.Health.CeilToInt().ToString();

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
}
