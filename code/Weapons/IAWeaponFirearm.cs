using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Sandbox;

namespace infinitearcade
{
	public partial class IAWeaponFirearm : IAWeapon
	{
		

		[Net]
		public int Clip1 { get; protected set; } = 8;
		[Net]
		public int Ammo1 { get; protected set; } = 32;

		[Net]
		public int MaxClip1 { get; protected set; } = 8;
		[Net]
		public int MaxAmmo1 { get; protected set; } = 128;

		[Net]
		public bool InfiniteClip { get; set; } = false;
		[Net]
		public bool InfiniteAmmo { get; set; } = false;

		public virtual float ReloadTime => 1.35f;
		[Net]
		public virtual float ReloadTimeMult => 1.0f;

		[Net, Predicted]
		public TimeSince TimeSinceReload { get; set; }
		[Net, Predicted]
		public bool IsReloading { get; set; }

		public override void Simulate(Client owner)
		{
			if (TimeSinceDeployed < 0.6f)
				return;

			if (!IsReloading)
				base.Simulate(owner);
			else if (TimeSinceReload > ReloadTime * ReloadTimeMult)
				OnReloadFinish();
		}

		public override bool CanReload()
		{
			if (!Owner.IsValid() || !Input.Down(InputButton.Reload) || Clip1 == MaxClip1 || (Ammo1 <= 0 && Clip1 <= MaxClip1))
				return false;

			return true;
		}

		public override void Reload()
		{
			if (IsReloading || (Ammo1 <= 0 && Clip1 <= MaxClip1))
				return;

			TimeSinceReload = 0;
			IsReloading = true;

			StartReloadEffects();
		}

		public virtual void OnReloadFinish()
		{
			IsReloading = false;

			int clipDiff = MaxClip1 - Clip1;

			if (Ammo1 >= clipDiff)
			{
				if (!InfiniteAmmo)
					Ammo1 -= clipDiff;
				Clip1 = MaxClip1;
			}
			else
			{
				Clip1 += Math.Clamp(Ammo1, 0, Ammo1);
				if (!InfiniteAmmo)
					Ammo1 = 0;
			}

		}

		[ClientRpc]
		public virtual void StartReloadEffects()
		{
			ViewModelEntity?.SetAnimBool("reload", true);
			(Owner as AnimEntity)?.SetAnimBool("b_reload", true);
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
					var damageInfo = DamageInfo.FromBullet(tr.EndPos, forward * 100 * force * Scale, damage * Scale)
						.UsingTraceResult(tr).WithAttacker(Owner).WithWeapon(this);

					tr.Entity.TakeDamage(damageInfo);
				}
			}
		}

		[ServerCmd("setclip")]
		public static void SetClipCommand(int clip)
		{
			IAWeaponFirearm firearm = ConsoleSystem.Caller?.Pawn?.ActiveChild as IAWeaponFirearm;

			if (firearm.IsValid())
				firearm.Clip1 = clip;
		}

		[ServerCmd("setammo")]
		public static void SetAmmoCommand(int ammo)
		{
			IAWeaponFirearm firearm = ConsoleSystem.Caller?.Pawn?.ActiveChild as IAWeaponFirearm;

			if (firearm.IsValid())
				firearm.Ammo1 = ammo;
		}
	}

	// why doesn't this work :(
	public struct WeaponAmmo : IEquatable<WeaponAmmo>
	{
		public int Clip;
		public int MaxClip;
		public int Ammo;
		public int MaxAmmo;

		public WeaponAmmo(int clip, int ammo)
		{
			Clip = MaxClip = clip;
			Ammo = MaxAmmo = ammo;
		}

		public int SetClip(int amount) => Clip = amount;
		public int DeltaClip(int amount) => Clip += amount;
		public int SetMaxClip(int amount) => MaxClip = amount;
		public int DeltaMaxClip(int amount) => MaxClip += amount;
		public int SetAmmo(int amount) => Ammo = amount;
		public int DeltaAmmo(int amount) => Ammo += amount;
		public int SetMaxAmmo(int amount) => MaxAmmo = amount;
		public int DeltaMaxAmmo(int amount) => MaxAmmo += amount;

		public bool Equals(WeaponAmmo other)
		{
			if (this.Clip == other.Clip)
				return true;

			return false;
		}
	}
}
