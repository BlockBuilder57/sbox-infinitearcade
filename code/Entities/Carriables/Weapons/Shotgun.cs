using CubicKitsune;
using Sandbox;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace infinitearcade
{
	[Library("firearm_shotgun", Title = "Shotgun")]
	public partial class Shotgun : CKWeaponFirearm
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

			SecondaryInput.TimeSince = SecondaryInput.Rate - PrimaryInput.Rate;

			if (PrimaryCapacity.CanTakeClip())
			{
				PrimaryCapacity.TakeClip();

				if (Owner is AnimatedEntity anim)
					anim.SetAnimParameter("b_attack", true);

				ViewModelEntity?.SetAnimParameter("fire", true);

				if (SoundEvents != null && SoundEvents.ContainsKey("primaryfire"))
					Sound.FromWorld(SoundEvents["primaryfire"].ResourceName, Position);
				ShootProjectile(PrimaryCapacity.Projectile, Owner.EyePosition, Owner.EyeRotation.Forward);
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

			PrimaryInput.TimeSince -= SecondaryInput.Rate;

			if (PrimaryCapacity.CanTakeClip(2))
			{
				PrimaryCapacity.TakeClip(2);

				if (Owner is AnimatedEntity anim)
					anim.SetAnimParameter("b_attack", true);

				ViewModelEntity?.SetAnimParameter("fire_double", true);

				if (SoundEvents != null && SoundEvents.ContainsKey("secondaryfire"))
					Sound.FromWorld(SoundEvents["secondaryfire"].ResourceName, Position);
				ShootProjectile(PrimaryCapacity.Projectile, Owner.EyePosition, Owner.EyeRotation.Forward);
				ShootProjectile(PrimaryCapacity.Projectile, Owner.EyePosition, Owner.EyeRotation.Forward);
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
			if (PrimaryCapacity.TryReload(1) && !CanAttack(InputButton.PrimaryAttack, PrimaryInput) && !CanAttack(InputButton.SecondaryAttack, SecondaryInput))
			{
				// we can reload, so keep going!
				Reload();
				return;
			}
			else
			{
				// either we're out of ammo or the internal mag is full, so we're done
				PrimaryInput.ResetTime();
				SecondaryInput.ResetTime();
			}
		}
	}
}
