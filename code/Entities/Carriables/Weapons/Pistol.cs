using Sandbox;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace infinitearcade
{
	[Library("weapon_pistol", Title = "Pistol")]
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

			if (PrimaryCapacity.CanTakeClip())
			{
				PrimaryCapacity.TakeClip();

				if (Owner is AnimatedEntity anim)
					anim.SetAnimParameter("b_attack", true);

				ViewModelEntity?.SetAnimParameter("fire", true);

				Sound.FromWorld(m_firearmDef.PrimaryFireSound, this.Position);
				ShootBullet(PrimaryCapacity, Owner.EyePosition, Owner.EyeRotation.Forward);
			}
		}
	}
}
