using Sandbox;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace infinitearcade
{
	[Library("weapon_pistol", Title = "Pistol", Spawnable = true)]
	public partial class Pistol : IAWeaponFirearm
	{

		public override void AttackPrimary()
		{
			if (PrimaryCapacity.Clip <= 0)
			{
				PlaySound("weapon_empty_click");
				Reload();
				return;
			}

			PrimaryCapacity.TakeClip();

			if (Owner is AnimEntity anim)
				anim.SetAnimParameter("b_attack", true);

			ViewModelEntity?.SetAnimParameter("fire", true);

			PlaySound(m_firearmDef.FireSound);
			ShootBullet(PrimaryCapacity, Owner.EyePosition, Owner.EyeRotation.Forward);
		}

		public override void AttackSecondary()
		{
			//AttackPrimary();
			NextPrimaryFireMode();
		}
	}
}
