using System;
using System.Collections;
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

		[Net] public WeaponCapacity PrimaryCapacity { get; set; }
		[Net] public WeaponCapacity SecondaryCapacity { get; set; }

		[Net] protected IAWeaponFirearmDefinition m_firearmDef { get; set; }

		[Net] public TimeSince TimeSinceReload { get; set; }
		[Net] public bool IsReloading { get; set; }

		public override IACarriable SetupFromDefinition(IACarriableDefinition def)
		{
			base.SetupFromDefinition(def);

			if (def is IAWeaponFirearmDefinition firearmDef)
			{
				if (firearmDef.PrimaryCapacity.MaxClip > 0)
					PrimaryCapacity = new WeaponCapacity(firearmDef.PrimaryCapacity);
				if (firearmDef.SecondaryCapacity.MaxClip > 0)
					SecondaryCapacity = new WeaponCapacity(firearmDef.SecondaryCapacity);

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
			else if (TimeSinceReload > m_firearmDef.ReloadTime * 1)
				OnReloadFinish();

			if (debug_firearm && (Owner as Player).ActiveChild == this)
			{
				if (IsServer)
					DebugOverlay.Text(Position, $"srv: {Primary.CurMode}");
				if (IsClient)
					DebugOverlay.Text(Position + Vector3.Down * 2, $"cli: {Primary.CurMode}");
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

			if (PrimaryCapacity == null || PrimaryCapacity.Clip == PrimaryCapacity.MaxClip || (PrimaryCapacity.Ammo <= 0 && PrimaryCapacity.Clip <= PrimaryCapacity.MaxClip))
				return false;

			return true;
		}

		public override void Reload()
		{
			if (IsReloading || (PrimaryCapacity.Ammo <= 0 && PrimaryCapacity.Clip <= PrimaryCapacity.MaxClip))
				return;

			TimeSinceReload = 0;
			IsReloading = true;

			StartReloadEffects();
		}

		public virtual void OnReloadFinish()
		{
			IsReloading = false;

			PrimaryCapacity.TryReload();
		}

		[ClientRpc]
		public virtual void StartReloadEffects()
		{
			ViewModelEntity?.SetAnimParameter("reload", true);
			(Owner as AnimEntity)?.SetAnimParameter("b_reload", true);
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

					var damageInfo = DamageInfo.FromBullet(tr.EndPosition, forward * 100 * force * Scale, damage * Scale)
						.UsingTraceResult(tr).WithAttacker(Owner).WithWeapon(this);

					tr.Entity.TakeDamage(damageInfo);
				}
			}
		}

		public virtual void ShootBullet(int numPellets, Vector3 pos, Vector3 dir, float spread, float force, float damage, float bulletSize, bool perPellet = false)
		{
			for (int i = 0; i < numPellets; i++)
			{
				if (perPellet)
					ShootBullet(pos, dir, spread, force / numPellets, damage / numPellets, bulletSize);
				else
					ShootBullet(pos, dir, spread, force, damage, bulletSize);
			}
		}

		public virtual void ShootBullet(WeaponCapacity cap, Vector3 pos, Vector3 dir)
		{
			ShootBullet(cap.BulletSettings.Pellets, pos, dir,
				cap.BulletSettings.Spread, cap.BulletSettings.Force,
				cap.BulletSettings.Damage, cap.BulletSettings.BulletSize,
				cap.BulletSettings.DividedAcrossPellets);
		}

		[AdminCmd("setclip")]
		public static void SetClipCommand(int clip)
		{
			Entity pawn = ConsoleSystem.Caller?.Pawn;
			Player player = pawn as Player;
			if (!player.IsValid())
				return;

			IAWeaponFirearm firearm = player.ActiveChild as IAWeaponFirearm;

			if (firearm.IsValid())
				firearm.PrimaryCapacity.SetClip(clip);
		}

		[AdminCmd("setammo")]
		public static void SetAmmoCommand(int ammo)
		{
			Entity pawn = ConsoleSystem.Caller?.Pawn;
			Player player = pawn as Player;
			if (!player.IsValid())
				return;

			IAWeaponFirearm firearm = player.ActiveChild as IAWeaponFirearm;

			if (firearm.IsValid())
				firearm.PrimaryCapacity.SetAmmo(ammo);
		}

		[AdminCmd("givecurrentammo")]
		public static void GiveCurrentAmmoCommand()
		{
			Entity pawn = ConsoleSystem.Caller?.Pawn;
			Player player = pawn as Player;
			if (!player.IsValid())
				return;

			IAWeaponFirearm firearm = player.ActiveChild as IAWeaponFirearm;

			if (firearm.IsValid())
				firearm.PrimaryCapacity.SetAmmo(firearm.PrimaryCapacity.MaxAmmo);
		}
	}

	public partial class WeaponCapacity : BaseNetworkable
	{
		[Net] public int Clip { get; private set; }
		[Net] public int Ammo { get; private set; }
		[Net] public int MaxClip { get; private set; }
		[Net] public int MaxAmmo { get; private set; }

		public struct BulletSetting
		{
			public int Pellets { get; set; } = 1;
			public float Spread { get; set; } = 0.05f;
			public float Force { get; set; } = 0.6f;
			public float Damage { get; set; } = 5f;
			public float BulletSize { get; set; } = 2f;
			public bool DividedAcrossPellets { get; set; } = false;
		}

		[Net] public BulletSetting BulletSettings { get; set; }

		[Net] public bool InfiniteClip { get; set; } = false;
		[Net] public bool InfiniteAmmo { get; set; } = false;

		public WeaponCapacity()
		{
			Clip = MaxClip = 8;
			Ammo = MaxAmmo = 24;
		}

		public WeaponCapacity(IAWeaponFirearmDefinition.CapacitySetting settings)
		{
			Clip = MaxClip = settings.MaxClip;
			Ammo = MaxAmmo = settings.MaxAmmo;

			if (settings.BulletSettings != null)
			{
				BulletSettings = new BulletSetting
				{
					Pellets = settings.BulletSettings.Pellets,
					Spread = settings.BulletSettings.Spread,
					Force = settings.BulletSettings.Force,
					Damage = settings.BulletSettings.Damage,
					BulletSize = settings.BulletSettings.BulletSize,
					DividedAcrossPellets = settings.BulletSettings.DividedAcrossPellets,
				};
			}
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
