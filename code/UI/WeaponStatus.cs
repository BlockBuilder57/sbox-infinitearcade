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
	public class WeaponStatus : Panel
	{
		public Label Clip1;
		public Label Ammo1;
		public Label Clip2;
		public Label Ammo2;

		public WeaponStatus()
		{
			StyleSheet.Load("/ui/WeaponStatus.scss");

			Clip1 = Add.Label("0", "numberDisplay");
			Ammo1 = Add.Label("0", "numberDisplay");
		}

		public override void Tick()
		{
			base.Tick();

			SetClass("hidden", true);

			ArcadePlayer player = Local.Pawn as ArcadePlayer;
			if (player == null) return;
			IAWeapon weapon = player.ActiveChild as IAWeapon;
			if (weapon == null) return;

			SetClass("hidden", false);

			if (weapon is IAWeaponFirearm firearm)
			{
				if (Clip1 != null)
					Clip1.Text = firearm.Clip1.ToString();
				if (Ammo1 != null)
					Ammo1.Text = firearm.Ammo1.ToString();
			}

		}
	}
}
