using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Sandbox;

namespace infinitearcade
{
	public partial class IAWeaponFirearm : IATool
	{
		[ConVar.Replicated] public static bool debug_firearm { get; set; } = false;

		[Net] public WeaponAmmo Primary { get; set; }
		[Net] public WeaponAmmo[] Secondaries { get; set; }

		[Net] protected IAWeaponFirearmDefinition m_firearmDef { get; set; }

		[Net] public float ReloadTime { get; set; } = 1.35f; // default
		[Net] public TimeSince TimeSinceReload { get; set; }
		[Net] public bool IsReloading { get; set; }

		public override IACarriable SetupFromDefinition(IACarriableDefinition def)
		{
			base.SetupFromDefinition(def);

			if (def is IAWeaponFirearmDefinition firearmDef)
			{
				Primary = new WeaponAmmo(firearmDef.Primary.MaxClip, firearmDef.Primary.MaxAmmo);

				Secondaries = new WeaponAmmo[firearmDef.Secondaries.Length];
				for (int i = 0; i < firearmDef.Secondaries.Length; i++)
				{
					IAWeaponFirearmDefinition.AmmoSetting setting = firearmDef.Secondaries[i];
					Secondaries[i] = new WeaponAmmo(setting.MaxClip, setting.MaxAmmo);
				}

				ReloadTime = firearmDef.ReloadTime;

				m_firearmDef = firearmDef;
			}

			return this;
		}

		public override void Simulate(Client cl)
		{
			if (TimeSinceDeployed < 0.6f)
				return;

			if (!IsReloading)
				base.Simulate(cl);
			else if (TimeSinceReload > ReloadTime * 1)
				OnReloadFinish();

			if (debug_firearm && IsActiveChild())
			{
				if (IsServer)
					DebugOverlay.Text(Position, "srv: " + Primary.ToString());
				if (IsClient)
					DebugOverlay.Text(Position + Vector3.Down * 2, "cli: " + Primary.ToString());
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

			if (Primary == null || Primary.Clip == Primary.MaxClip || (Primary.Ammo <= 0 && Primary.Clip <= Primary.MaxClip))
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
				if (!IsServer || !tr.Entity.IsValid())
					continue;

				// prediction is turned off here to prevent bullet traces from being desynced
				using (Prediction.Off())
				{
					tr.Surface.DoBulletImpact(tr);

					var damageInfo = DamageInfo.FromBullet(tr.EndPos, forward * 100 * force * Scale, damage * Scale)
						.UsingTraceResult(tr).WithAttacker(Owner).WithWeapon(this);

					tr.Entity.TakeDamage(damageInfo);
				}
			}
		}

		public virtual void ShootBullet(int numBullets, Vector3 pos, Vector3 dir, float spread, float force, float damage, float bulletSize, bool perBullet = false)
		{
			for (int i = 0; i < numBullets; i++)
			{
				if (perBullet)
					ShootBullet(pos, dir, spread, force, damage, bulletSize);
				else
					ShootBullet(pos, dir, spread, force / numBullets, damage / numBullets, bulletSize);
			}
		}

		public virtual void ShootBullet(Vector3 pos, Vector3 dir)
		{
			ShootBullet(m_firearmDef.BulletSettings.Pellets, pos, dir,
				m_firearmDef.BulletSettings.Spread, m_firearmDef.BulletSettings.Force,
				m_firearmDef.BulletSettings.Damage, m_firearmDef.BulletSettings.BulletSize,
				m_firearmDef.BulletSettings.CalculatedPerPellet);
		}

		[AdminCmd("setclip")]
		public static void SetClipCommand(int clip)
		{
			IAWeaponFirearm firearm = ConsoleSystem.Caller?.Pawn?.ActiveChild as IAWeaponFirearm;

			if (firearm.IsValid())
				firearm.Primary.SetClip(clip);
		}

		[AdminCmd("setammo")]
		public static void SetAmmoCommand(int ammo)
		{
			IAWeaponFirearm firearm = ConsoleSystem.Caller?.Pawn?.ActiveChild as IAWeaponFirearm;

			if (firearm.IsValid())
				firearm.Primary.SetAmmo(ammo);
		}
	}

	public partial class WeaponAmmo : BaseNetworkable
	{
		[Net] public int Clip { get; private set; }
		[Net] public int MaxClip { get; private set; }
		[Net] public int Ammo { get; private set; }
		[Net] public int MaxAmmo { get; private set; }

		[Net] public bool InfiniteClip { get; set; } = false;
		[Net] public bool InfiniteAmmo { get; set; } = false;

		public WeaponAmmo()
		{
			Clip = MaxClip = 8;
			Ammo = MaxAmmo = 32;
		}

		public WeaponAmmo(int clip, int ammo)
		{
			Clip = MaxClip = clip;
			Ammo = MaxAmmo = ammo;
		}

		public void SetClip(int amount) => Clip = amount;
		public void SetAmmo(int amount) => Ammo = amount;

		public int TakeClip(int amount = 1) { if (!InfiniteClip) { Clip -= amount; } return Clip; }
		public int TakeAmmo(int amount = 1) { if (!InfiniteAmmo) { Ammo -= amount; } return Ammo; }
		public int GiveClip(int amount = 1) { if (!InfiniteClip) { Clip += amount; } return Clip; }
		public int GiveAmmo(int amount = 1) { if (!InfiniteAmmo) { Ammo += amount; } return Ammo; }

		public bool TryReload(int thisMany = -1)
		{
			int neededRounds = MaxClip - Clip;

			if (thisMany != -1)
				neededRounds = thisMany;

			if (neededRounds == 0)
				return false;

			// if we're overfilled, give the ammo back
			if (Clip > MaxClip)
			{
				GiveAmmo(Clip - MaxClip);
				Clip = MaxClip;
				return false;
			}

			if (Ammo >= neededRounds)
			{
				// if the clip needs ammo and we're doing a full reload
				TakeAmmo(neededRounds);

				// if clip + needed is less than max clip, add them
				if (Clip + neededRounds <= MaxClip)
				{
					GiveClip(neededRounds);
					// do we need to continue or not?
					return Clip != MaxClip;
				}

				return false;
			}
			else
			{
				// clamp the ammo we're taking out
				GiveClip(Math.Clamp(Ammo, 0, Ammo));

				if (!InfiniteAmmo)
					SetAmmo(0);

				// done reloading
				return false;
			}
		}

		public override string ToString()
		{
			return $"{Clip}/{Ammo} (max {MaxClip}/{MaxAmmo})";
		}
	}
}
