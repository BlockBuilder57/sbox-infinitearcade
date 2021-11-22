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
		[ConVar.Replicated]
		public static bool debug_firearm { get; set; } = false;

		public IAWeaponFirearmDefinition Definition { get; set; }

		[Net]
		public WeaponAmmo Primary { get; set; }

		public virtual float ReloadTime => 1.35f;
		public virtual float ReloadTimeMult => 1.0f;

		[Net]
		public TimeSince TimeSinceReload { get; set; }
		[Net]
		public bool IsReloading { get; set; }

		public override void Simulate(Client owner)
		{
			if (TimeSinceDeployed < 0.6f)
				return;

			if (!IsReloading)
				base.Simulate(owner);
			else if (TimeSinceReload > ReloadTime * 1 / ReloadTimeMult)
				OnReloadFinish();

			if (debug_firearm && IsActiveChild())
			{
				if (IsServer)
					DebugOverlay.Text(Position, "srv: " + Primary.ToString());
				if (IsClient)
					DebugOverlay.Text(Position + Vector3.Down * 2, "cli: " + Primary.ToString(), 0.05f);
			}
		}

		public override void ActiveStart(Entity ent)
		{
			base.ActiveStart(ent);
		}

		public override void OnCarryDrop(Entity dropper)
		{
			base.OnCarryDrop(dropper);
		}

		public override bool CanReload()
		{
			if (!Owner.IsValid() || !Input.Down(InputButton.Reload))
				return false;

			if (Primary.Clip == Primary.MaxClip || (Primary.Ammo <= 0 && Primary.Clip <= Primary.MaxClip))
				return false;

			return true;
		}

		public override void Reload()
		{
			if (IsReloading || (Primary.Ammo <= 0 && Primary.Clip <= Primary.MaxClip))
				return;

			TimeSinceReload = 0;
			IsReloading = true;

			StartReloadEffects();
		}

		public virtual void OnReloadFinish()
		{
			IsReloading = false;

			Primary.TryReload();
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
				firearm.Primary.SetClip(clip);
		}

		[ServerCmd("setammo")]
		public static void SetAmmoCommand(int ammo)
		{
			IAWeaponFirearm firearm = ConsoleSystem.Caller?.Pawn?.ActiveChild as IAWeaponFirearm;

			if (firearm.IsValid())
				firearm.Primary.SetAmmo(ammo);
		}
	}

	public partial class WeaponAmmo : NetworkComponent
	{
		[Net] public int Clip { get; set; }
		[Net] public int MaxClip { get; set; }
		[Net] public int Ammo { get; set; }
		[Net] public int MaxAmmo { get; set; }

		[Net] public bool InfiniteClip { get; set; }
		[Net] public bool InfiniteAmmo { get; set; }

		public WeaponAmmo()
		{
			Clip = MaxClip = 8;
			Ammo = MaxAmmo = 32;

			InfiniteClip = false;
			InfiniteAmmo = false;
		}

		public WeaponAmmo(int clip, int ammo)
		{
			Clip = MaxClip = clip;
			Ammo = MaxAmmo = ammo;

			InfiniteClip = false;
			InfiniteAmmo = false;
		}

		public void SetClip(int amount) => Clip = amount;
		public void SetAmmo(int amount) => Ammo = amount;

		public void TryReload()
		{
			int clipDiff = MaxClip - Clip;

			if (Ammo >= clipDiff)
			{
				if (!InfiniteAmmo)
					Ammo -= clipDiff;
				Clip = MaxClip;
			}
			else
			{
				Clip += Math.Clamp(Ammo, 0, Ammo);
				if (!InfiniteAmmo)
					Ammo = 0;
			}
		}

		public override string ToString()
		{
			return $"{Clip}/{Ammo} (max {MaxClip}/{MaxAmmo})";
		}
	}
}
