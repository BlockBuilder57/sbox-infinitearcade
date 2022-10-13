using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CubicKitsune;
using Sandbox;

namespace CubicKitsune
{
	[Library("firearm_generic")]
	public partial class CKWeaponFirearm : CKTool, ICKWeaponFirearm
	{
		[ConVar.Replicated] public static bool debug_firearm { get; set; } = false;

		[Net] public WeaponCapacity PrimaryCapacity { get; set; }
		[Net] public WeaponCapacity SecondaryCapacity { get; set; }
		public ICKWeaponFirearm.CapacitySettings PrimaryCapacitySettings { get; set; }
		public ICKWeaponFirearm.CapacitySettings SecondaryCapacitySettings { get; set; }

		[Net] public ICKWeaponFirearm.InputFunction PrimaryFunction { get; set; }
		[Net] public ICKWeaponFirearm.InputFunction SecondaryFunction { get; set; }
		[Net] public ICKWeaponFirearm.InputFunction ReloadFunction { get; set; }

		[Net] public float ReloadTime { get; set; }

		[Net] public TimeSince TimeSinceReload { get; set; }
		[Net] public bool IsReloading { get; set; }

		public CKWeaponFirearm SetupFromInterface(ICKCarriable carry, ICKTool tool, ICKWeaponFirearm firearm)
		{
			if (firearm == null)
			{
				Log.Error($"{this} trying to set up with a null firearm definition!");
				Delete();
				return null;
			}

			if (Host.IsServer)
			{
				if (!string.IsNullOrEmpty(firearm.PrimaryCapacitySettings.ProjectileAsset))
					PrimaryCapacity = new WeaponCapacity(firearm.PrimaryCapacitySettings);
				if (!string.IsNullOrEmpty(firearm.SecondaryCapacitySettings.ProjectileAsset))
					SecondaryCapacity = new WeaponCapacity(firearm.SecondaryCapacitySettings);

				PrimaryFunction = firearm.PrimaryFunction;
				SecondaryFunction = firearm.SecondaryFunction;
				ReloadFunction = firearm.ReloadFunction;

				ReloadTime = firearm.ReloadTime;
			}

			return (CKWeaponFirearm)SetupFromInterface(carry, tool);
		}
		public override CKCarriable SetupFromDefinition(CKCarriableDefinition def) => SetupFromInterface(def, def as ICKTool, def as ICKWeaponFirearm);

		public override void Simulate(Client cl)
		{
			if (TimeSinceDeployed < 0.6f)
				return;

			if (!IsReloading)
				base.Simulate(cl);
			else if (TimeSinceReload > ReloadTime * 1)
			{
				OnReloadFinish();
				FinishReloadEffects();
			}

			if (debug_firearm && (Owner as Player).ActiveChild == this)
			{
				string debug = IsServer ? "~ srv ~\n" : "~ cli ~\n";
					debug += $"Primary: {PrimaryInput.CurMode} func {PrimaryFunction} | {(PrimaryCapacity == null ? "(null)" : PrimaryCapacity)}\n";
					debug += $"Secondary: {SecondaryInput.CurMode} func {SecondaryFunction} | {(SecondaryCapacity == null ? "(null)" : SecondaryCapacity)}, \n";
					debug += $"Reload: {ReloadInput.CurMode} func {ReloadFunction}\n";

				// debug code does not have to be good, it just has to debug
				Vector3 offset = IsServer ? Vector3.Down * (debug.Where(x => x == '\n').Count() + 2) * 2 : 0;
				DebugOverlay.Text(debug, Position + offset);
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

		public void TryInput(ICKWeaponFirearm.InputFunction inputFunc, InputHelper modeSelector)
		{
			switch (inputFunc)
			{
				case ICKWeaponFirearm.InputFunction.FirePrimary:
					AttackPrimary();
					break;
				case ICKWeaponFirearm.InputFunction.FireSecondary:
					AttackSecondary();
					break;
				case ICKWeaponFirearm.InputFunction.ModeSelector:
					modeSelector.NextFireMode();
					break;
				case ICKWeaponFirearm.InputFunction.Reload:
					Reload();
					break;
			}
		}

		public override void TryAttackPrimary() => TryInput(PrimaryFunction, SecondaryInput);
		public override void TryAttackSecondary() => TryInput(SecondaryFunction, PrimaryInput);
		public override void TryReload() => TryInput(ReloadFunction, SecondaryInput);

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
			(Owner as AnimatedEntity)?.SetAnimParameter("b_reload", true);
		}

		[ClientRpc]
		public virtual void FinishReloadEffects()
		{
			ViewModelEntity?.SetAnimParameter("reload_finished", true);
		}

		public virtual void FireHitscanBullet(Vector3 pos, Vector3 dir, float physForce, float damage, float bulletSize, ICKProjectile.BounceParameters bounceParams)
		{
			var forward = dir.Normal;

			foreach (var tr in TraceHitscan(pos, pos + forward * short.MaxValue, bulletSize, true, bounceParams))
			{
				if (!IsServer || !tr.Entity.IsValid())
					continue;

				//DebugOverlay.Line(tr.StartPosition, tr.EndPosition, Color.Yellow, 2f);

				// prediction is turned off here to prevent bullet traces from being desynced
				using (Prediction.Off())
				{
					tr.Surface.DoBulletImpact(tr);

					var damageInfo = DamageInfo.FromBullet(tr.EndPosition, forward * physForce * Scale, damage * Scale)
						.UsingTraceResult(tr).WithAttacker(Owner).WithWeapon(this);

					tr.Entity.TakeDamage(damageInfo);
				}
			}
		}

		public virtual void ShootProjectile(ICKProjectile proj, Vector3 pos, Vector3 dir)
		{
			if (proj == null)
				throw new Exception("Projectile was null");

			if (!Host.IsServer)
				return;

			ICKProjectile.SpawnStats stats = proj.Stats;
			proj.Count = Math.Max(1, proj.Count);

			if (string.IsNullOrEmpty(proj.TypeLibraryName))
			{
				// fire bullet(s)

				for (int i = 0; i < proj.Count; i++)
				{
					Vector3 forceLinear = stats.ForceLinear + (stats.ForceLinearRandom * Vector3.Random);
					// rotate to dir
					Vector3 forceLinearDir = (Rotation.From(dir.Normal.EulerAngles) * forceLinear);

					float physForce = forceLinear.x;
					float damage = proj.Damage / proj.Count;

					FireHitscanBullet(pos, forceLinearDir, physForce, damage, stats.Size, proj.BounceParams);
				}
			}
			else
			{
				// fire projectile(s)

				for (int i = 0; i < proj.Count; i++)
				{
					CKProjectile ent = TypeLibrary.Create<CKProjectile>(proj.TypeLibraryName);

					if (!ent.IsValid())
						throw new Exception("Projectile library name didn't make a projectile entity");

					Vector3 forceLinearDir = Rotation.From(dir.Normal.EulerAngles) * (stats.ForceLinear + (stats.ForceLinearRandom * Vector3.Random));
					Angles forceAngular = stats.ForceAngular + (new Vector3(stats.ForceAngularRandom.pitch, stats.ForceAngularRandom.yaw, stats.ForceAngularRandom.roll) * Vector3.Random);

					ent.Owner = Owner;
					ent.Model = proj.WorldModel;
					ent.Health = stats.Health;
					ent.Lifetime = stats.Lifetime;
					ent.Damage = proj.Damage;

					ent.Position = pos + dir; // bump it out a little
					ent.Rotation = Rotation.From(dir.Normal.EulerAngles);

					ent.Velocity = Owner.Velocity + forceLinearDir;
					ent.ApplyLocalAngularImpulse(new Vector3(forceAngular.pitch, forceAngular.yaw, forceAngular.roll));

					// hacky projectile thing
					PhysicsJoint.CreatePulley(ent.PhysicsGroup.GetBody(0), Owner.PhysicsGroup.GetBody(0), Vector3.Zero, Vector3.Zero, Vector3.Zero, Vector3.Zero);
				}
			}
		}

		[ConCmd.Admin("firearm_setclip")]
		public static void SetClipCommand(int clip, bool secondary = false)
		{
			if (ConsoleSystem.Caller?.Pawn is Player player && player.IsValid() && player.ActiveChild is CKWeaponFirearm firearm)
			{
				WeaponCapacity cap = secondary ? firearm.SecondaryCapacity : firearm.PrimaryCapacity;
				cap.SetClip(clip);
			}
		}

		[ConCmd.Admin("firearm_setammo")]
		public static void SetAmmoCommand(int ammo, bool secondary = false)
		{
			if (ConsoleSystem.Caller?.Pawn is Player player && player.IsValid() && player.ActiveChild is CKWeaponFirearm firearm)
			{
				WeaponCapacity cap = secondary ? firearm.SecondaryCapacity : firearm.PrimaryCapacity;
				cap.SetAmmo(ammo);
			}
		}

		[ConCmd.Admin("firearm_givecurrentammo")]
		public static void GiveCurrentAmmoCommand()
		{
			if (ConsoleSystem.Caller?.Pawn is Player player && player.IsValid())
			{
				if (player.ActiveChild is CKWeaponFirearm firearm)
				{
					if (firearm.PrimaryCapacity != null)
					{
						int missingPrimary = firearm.PrimaryCapacity.MaxClip - firearm.PrimaryCapacity.Clip;

						if (firearm.PrimaryCapacity.MaxAmmo > 0)
							firearm.PrimaryCapacity.SetAmmo(firearm.PrimaryCapacity.MaxAmmo + missingPrimary);
						else
							firearm.PrimaryCapacity.SetClip(firearm.PrimaryCapacity.MaxClip);
					}
					
					if (firearm.SecondaryCapacity != null)
					{
						int missingSecondary = firearm.SecondaryCapacity.MaxClip - firearm.SecondaryCapacity.Clip;
						
						if (firearm.SecondaryCapacity.MaxAmmo > 0)
							firearm.SecondaryCapacity.SetAmmo(firearm.SecondaryCapacity.MaxAmmo + missingSecondary);
						else
							firearm.SecondaryCapacity.SetClip(firearm.SecondaryCapacity.MaxClip);
					}
				}
			}
		}
	}

	public partial class WeaponCapacity : BaseNetworkable
	{
		[ConVar.Server] public static bool firearm_infinite_clip { get; set; } = false;
		[ConVar.Server] public static bool firearm_infinite_ammo { get; set; } = false;

		[Net] public int Clip { get; private set; }
		[Net] public int Ammo { get; private set; }
		[Net] public int MaxClip { get; private set; }
		[Net] public int MaxAmmo { get; private set; }

		[Net] public CKProjectileDefinition Projectile { get; set; }

		[Net] public bool InfiniteClip { get; set; } = false;
		[Net] public bool InfiniteAmmo { get; set; } = false;

		public WeaponCapacity()
		{
			Clip = MaxClip = 8;
			Ammo = MaxAmmo = 24;
		}

		public WeaponCapacity(ICKWeaponFirearm.CapacitySettings settings)
		{
			Clip = MaxClip = settings.MaxClip;
			Ammo = MaxAmmo = settings.MaxAmmo;
			Projectile = ResourceLibrary.Get<CKProjectileDefinition>(settings.ProjectileAsset);

			//Log.Info($"{NetworkIdent} being setup by: {settings}");
		}

		public void SetClip(int amount) => Clip = amount;
		public void SetAmmo(int amount) => Ammo = amount;

		public bool CanTakeClip(int amount = 1) { return firearm_infinite_clip || InfiniteClip || Clip >= amount; }
		public bool CanTakeAmmo(int amount = 1) { return firearm_infinite_ammo || InfiniteAmmo || Ammo >= amount; }

		public int TakeClip(int amount = 1) { if (!(firearm_infinite_clip || InfiniteClip)) { Clip -= amount; } return Clip; }
		public int TakeAmmo(int amount = 1) { if (!(firearm_infinite_ammo || InfiniteAmmo)) { Ammo -= amount; } return Ammo; }
		public int GiveClip(int amount = 1) { Clip += amount; return Clip; }
		public int GiveAmmo(int amount = 1) { Ammo += amount; return Ammo; }

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
