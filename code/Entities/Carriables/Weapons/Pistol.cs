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
		public override bool CanPrimaryAttack()
		{
			return base.CanPrimaryAttack() && Input.Pressed(InputButton.Attack1);
		}

		public override void AttackPrimary()
		{
			if (Primary == null)
				return;

			if (Primary.Clip <= 0)
			{
				PlaySound("weapon_empty_click");
				Reload();
				return;
			}

			Primary.TakeClip();

			TimeSincePrimaryAttack = 0;
			TimeSinceSecondaryAttack = 0;

			if (Owner is AnimEntity anim)
				anim.SetAnimBool("b_attack", true);

			ViewModelEntity?.SetAnimBool("fire", true);

			PlaySound(m_firearmDef.FireSound);
			ShootBullet(Owner.EyePosition, Owner.EyeRotation.Forward);
		}

		public override bool CanSecondaryAttack()
		{
			return base.CanSecondaryAttack() && Input.Down(InputButton.Attack2);
		}

		public override void AttackSecondary()
		{
			AttackPrimary();
		}
	}
}
