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
		public override string WorldModelPath => "weapons/rust_pumpshotgun/rust_pumpshotgun.vmdl";
		public override string ViewModelPath => "weapons/rust_pumpshotgun/v_rust_pumpshotgun.vmdl";
		public bool ReloadAnimHasInitialShellLoad = true;

		public override float PrimaryRate => 1.5f;
		public override float ReloadTime => .5f; // time per shell

		public Shotgun()
		{
			BucketIdent = "primary";
			Primary = new WeaponAmmo(6, 48);
		}

		public override void SimulateAnimator(PawnAnimator anim)
		{
			anim.SetParam("holdtype", 3);
			anim.SetParam("aimat_weight", 1.0f);
		}

		public override bool CanPrimaryAttack()
		{
			return base.CanPrimaryAttack() && Input.Down(InputButton.Attack1);
		}

		public override void AttackPrimary()
		{
			if (Primary.Clip <= 0)
			{
				Reload();
				return;
			}

			Primary.TakeClip();

			TimeSincePrimaryAttack = 0;
			TimeSinceSecondaryAttack = 0;

			if (Owner is AnimEntity anim)
				anim.SetAnimBool("b_attack", true);

			ViewModelEntity?.SetAnimBool("fire", true);

			PlaySound("rust_pumpshotgun.shoot");
			ShootBullet(8, Owner.EyePosition, Owner.EyeRotation.Forward, 0.2f, 2.4f, 24f, .8f);
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
