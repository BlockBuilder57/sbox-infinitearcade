using Sandbox;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace infinitearcade
{
	[Library("weapon_pistol", Title = "Pistol", Spawnable = true)]
	public class Pistol : IAWeapon
	{
		public override string ViewModelPath => "weapons/rust_pistol/v_rust_pistol.vmdl";

		public override float PrimaryRate => 8f;

		public override void Spawn()
		{
			base.Spawn();

			SetModel("weapons/rust_pistol/rust_pistol.vmdl");
		}

		public override bool CanPrimaryAttack()
		{
			return base.CanPrimaryAttack() && Input.Down(InputButton.Attack1);
		}

		public override void AttackPrimary()
		{
			TimeSincePrimaryAttack = 0;
			TimeSinceSecondaryAttack = 0;

			if (Owner is AnimEntity anim)
				anim.SetAnimBool("b_attack", true);

			ViewModelEntity?.SetAnimBool("fire", true);

			PlaySound("rust_pistol.shoot");
			ShootBullet(Owner.EyePos, Owner.EyeRot.Forward, 0f, 20f, 5f, 2f);
		}

		public virtual void ShootBullet(Vector3 pos, Vector3 dir, float spread, float force, float damage, float bulletSize)
		{
			var forward = dir;
			forward += (Vector3.Random + Vector3.Random + Vector3.Random + Vector3.Random) * spread * 0.25f;
			forward = forward.Normal;

			foreach (var tr in TraceBullet(pos, pos + forward * short.MaxValue, bulletSize))
			{
				tr.Surface.DoBulletImpact(tr);

				if (!IsServer || !tr.Entity.IsValid())
					continue;

				// why is prediction turned off here
				using (Prediction.Off())
				{
					var damageInfo = DamageInfo.FromBullet(tr.EndPos, forward * 100 * force, damage)
						.UsingTraceResult(tr).WithAttacker(Owner).WithWeapon(this);

					tr.Entity.TakeDamage(damageInfo);
				}
			}
		}
	}
}
