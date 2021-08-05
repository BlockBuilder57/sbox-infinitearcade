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
		private WeaponAmmoDisplay m_primaryDisplay;

		public WeaponStatus()
		{
			StyleSheet.Load("/ui/WeaponStatus.scss");

			m_primaryDisplay = AddChild<WeaponAmmoDisplay>();
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
				m_primaryDisplay.Update(firearm.Clip1, firearm.Ammo1);
			}

		}

		private class WeaponAmmoDisplay : Panel
		{
			public Label Clip;
			public Label Ammo;
			public Image Icon;

			public WeaponAmmoDisplay()
			{
				Clip = Add.Label("0", "clip");
				Ammo = Add.Label("0", "ammo");
				Icon = Add.Image(null);
			}

			public void Update(int clip, int ammo)
			{
				if (Clip != null)
					Clip.Text = clip.ToString();
				if (Ammo != null)
					Ammo.Text = ammo.ToString();
			}
		}
	}
}
