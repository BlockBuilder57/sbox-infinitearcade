using Sandbox;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace infinitearcade
{
	[Library("weapon_shotgun", Title = "Shotgun", Spawnable = true)]
	public partial class Shotgun : IAWeaponFirearm
	{
		public bool ReloadAnimHasInitialShellLoad = true;

		public override bool CanPrimaryAttack()
		{
			return base.CanPrimaryAttack() && Input.Down(InputButton.Attack1);
		}

		public override void AttackPrimary()
		{
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
			// TODO: double blast
			return false;
		}

		public override void StartReloadEffects()
		{
			// the rust shotgun reloads a shell in its reload start, so let's account for that
			if (ReloadAnimHasInitialShellLoad && Primary.Clip == Primary.MaxClip)
				return;

			base.StartReloadEffects();
		}

		public override void OnReloadFinish()
		{
			if (!IsReloading)
				return;

			IsReloading = false;

			// if reloading worked and we aren't attacking, try reloading
			if (Primary.TryReload(1) && !CanPrimaryAttack())
			{
				// we can reload, so keep going!
				Reload();
			}
			else
			{
				// either we're out of ammo or the internal mag is full, so we're done
				TimeSincePrimaryAttack = 0;
				TimeSinceSecondaryAttack = 0;

				FinishReloadEffects();
			}
		}

		[ClientRpc]
		public virtual void FinishReloadEffects()
		{
			ViewModelEntity?.SetAnimBool("reload_finished", true);
		}
	}
}
