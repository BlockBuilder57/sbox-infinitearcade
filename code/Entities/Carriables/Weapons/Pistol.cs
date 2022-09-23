using CubicKitsune;
using Sandbox;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace infinitearcade
{
	[Library("firearm_pistol", Title = "Pistol")]
	public partial class Pistol : CKWeaponFirearm
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

				//if (SoundEvents.ContainsKey("primaryfire"))
				//	Sound.FromWorld(SoundEvents["primaryfire"].ResourceName, Position);
				ShootProjectile(PrimaryCapacity.Projectile, Owner.EyePosition, Owner.EyeRotation.Forward);
			}
		}

		public override void AttackSecondary()
		{
			if (SecondaryCapacity.Clip <= 0)
			{
				PlaySound("weapon_empty_click");
				//Reload();
				return;
			}

			if (SecondaryCapacity.CanTakeClip())
			{
				SecondaryCapacity.TakeClip();

				if (Owner is AnimatedEntity anim)
					anim.SetAnimParameter("b_attack", true);

				ViewModelEntity?.SetAnimParameter("fire", true);

				ShootProjectile(SecondaryCapacity.Projectile, Owner.EyePosition, Owner.EyeRotation.Forward);
			}
		}
	}
}
