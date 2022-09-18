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

			SecondaryInput.ResetTime();

			if (PrimaryCapacity.CanTakeClip())
			{
				PrimaryCapacity.TakeClip();

				if (Owner is AnimatedEntity anim)
					anim.SetAnimParameter("b_attack", true);

				ViewModelEntity?.SetAnimParameter("fire", true);

				//if (SoundEvents.ContainsKey("primaryfire"))
				//	Sound.FromWorld(SoundEvents["primaryfire"].ResourceName, Position);
				ShootBullet(PrimaryCapacity.RoundDefinition, Owner.EyePosition, Owner.EyeRotation.Forward);
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

			PrimaryInput.ResetTime();

			if (PrimaryCapacity.CanTakeClip(2))
			{
				PrimaryCapacity.TakeClip(2);

				if (Owner is AnimatedEntity anim)
					anim.SetAnimParameter("b_attack", true);

				ViewModelEntity?.SetAnimParameter("fire", true);

				//if (SoundEvents.ContainsKey("secondaryfire"))
				//	Sound.FromWorld(SoundEvents["secondaryfire"].ResourceName, Position);
				ShootBullet(PrimaryCapacity.RoundDefinition, Owner.EyePosition, Owner.EyeRotation.Forward);
				ShootBullet(PrimaryCapacity.RoundDefinition, Owner.EyePosition, Owner.EyeRotation.Forward);
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
			}
			else
			{
				// either we're out of ammo or the internal mag is full, so we're done
				PrimaryInput.ResetTime();
				SecondaryInput.ResetTime();

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
