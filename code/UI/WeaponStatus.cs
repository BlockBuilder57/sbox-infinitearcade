﻿using System;
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
	public class WeaponStatus : Panel
	{
		private WeaponAmmoDisplay m_primaryDisplay;
		private WeaponAmmoDisplay m_secondaryDisplay;

		public WeaponStatus()
		{
			StyleSheet.Load("/ui/WeaponStatus.scss");

			m_primaryDisplay = AddChild<WeaponAmmoDisplay>();
			m_secondaryDisplay = AddChild<WeaponAmmoDisplay>();
		}

		public override void Tick()
		{
			base.Tick();

			SetClass("hidden", true);

			CKPlayer player = Local.Pawn as CKPlayer;
			if (player == null) return;

			if (player.ActiveChild is CKWeaponFirearm firearm)
			{
				SetClass("hidden", false);

				m_primaryDisplay.Update(firearm.PrimaryCapacity);
				m_secondaryDisplay.Update(firearm.SecondaryCapacity);
			}

		}

		private class WeaponAmmoDisplay : Panel
		{
			public Label Clip;
			public Label Ammo;
			//public Image Icon;

			public WeaponAmmoDisplay()
			{
				Clip = Add.Label("0", "clip");
				Ammo = Add.Label("0", "ammo");
				//Icon = Add.Image(null);
			}

			public void Update(WeaponCapacity cap)
			{
				SetClass("hidden", cap == null);
				if (cap == null) return;

				if (Clip != null)
				{
					Clip.SetClass("hidden", cap.MaxClip <= 0);
					Clip.Text = cap.Clip.ToString();
				}
				if (Ammo != null)
				{
					Ammo.SetClass("hidden", cap.MaxAmmo <= 0);
					Ammo.Text = cap.Ammo.ToString();
				}
			}
		}
	}
}
