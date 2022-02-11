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
		public override string WorldModelPath => "weapons/rust_pistol/rust_pistol.vmdl";
		public override string ViewModelPath => "weapons/rust_pistol/v_rust_pistol.vmdl";

		public override float PrimaryRate => 5f;
		public override float SecondaryRate => 8f;
		public override float ReloadTimeMult => 1f;

		public Pistol()
		{
			BucketIdent = "secondary";
			Primary = new WeaponAmmo(8, 32);
		}

		public override bool CanPrimaryAttack()
		{
			return base.CanPrimaryAttack() && Input.Pressed(InputButton.Attack1);
		}

		public override void AttackPrimary()
		{
			if (Primary.Clip <= 0)
			{
				PlaySound("ui.button.deny");
				Reload();
				return;
			}

			Primary.TakeClip();

			TimeSincePrimaryAttack = 0;
			TimeSinceSecondaryAttack = 0;

			if (Owner is AnimEntity anim)
				anim.SetAnimBool("b_attack", true);

			ViewModelEntity?.SetAnimBool("fire", true);

			PlaySound("rust_pistol.shoot");
			ShootBullet(Owner.EyePos, Owner.EyeRot.Forward, 0.025f, .6f, 5f, 2f);
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
