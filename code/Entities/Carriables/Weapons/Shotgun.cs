using Sandbox;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace infinitearcade
{
	[Library("weapon_shotgun", Title = "Shotgun")]
	public partial class Shotgun : IAWeaponFirearm
	{
		public bool ReloadAnimHasInitialShellLoad = true;

		public override void AttackPrimary()
		{
			if (PrimaryCapacity.Clip <= 0)
			{
				PlaySound("weapon_empty_click");
				Reload();
				return;
			}

			Secondary.TimeSince = 0;

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

		public override void AttackSecondary()
		{
			if (PrimaryCapacity.Clip <= 0)
			{
				PlaySound("weapon_empty_click");
				Reload();
				return;
			}

			Primary.TimeSince = 0;

			if (PrimaryCapacity.CanTakeClip(2))
			{
				PrimaryCapacity.TakeClip(2);

				if (Owner is AnimatedEntity anim)
					anim.SetAnimParameter("b_attack", true);

				ViewModelEntity?.SetAnimParameter("fire", true);

				Sound.FromWorld(m_firearmDef.SecondaryFireSound, this.Position);
				ShootBullet(PrimaryCapacity, Owner.EyePosition, Owner.EyeRotation.Forward);
				ShootBullet(PrimaryCapacity, Owner.EyePosition, Owner.EyeRotation.Forward);
			}
			else
				AttackPrimary();
		}

		public override void StartReloadEffects()
		{
			// the rust shotgun reloads a shell in its reload start, so let's account for that
			if (ReloadAnimHasInitialShellLoad && PrimaryCapacity.Clip == PrimaryCapacity.MaxClip)
				return;

			base.StartReloadEffects();
		}

		public override void OnReloadFinish()
		{
			if (!IsReloading)
				return;

			IsReloading = false;

			// if reloading worked and we aren't attacking, try reloading
			if (PrimaryCapacity.TryReload(1) && !CanPrimaryAttack() && !CanSecondaryAttack())
			{
				// we can reload, so keep going!
				Reload();
			}
			else
			{
				// either we're out of ammo or the internal mag is full, so we're done
				Primary.TimeSince = 0;
				Secondary.TimeSince = 0;

				FinishReloadEffects();
			}
		}

		[ClientRpc]
		public virtual void FinishReloadEffects()
		{
			ViewModelEntity?.SetAnimParameter("reload_finished", true);
		}
	}
}
