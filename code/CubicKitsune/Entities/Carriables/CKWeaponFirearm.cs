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
				PrimaryCapacity = new WeaponCapacity(firearm.PrimaryCapacitySettings);
				//SecondaryCapacity = new WeaponCapacity(firearm.SecondaryCapacitySettings);

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
				OnReloadFinish();

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

		public virtual void ShootBullet(CKRoundDefinition round, Vector3 pos, Vector3 dir)
		{
			if (round != null)
				ShootBullet(round.Pellets, pos, dir, round.Spread, round.Force, round.Damage, round.BulletSize, round.DividedAcrossPellets);
			else
				ShootBullet(8, pos, dir, 0.2f, 0.2f, 16, 2, true);
		}

		[ConCmd.Admin("firearm_setclip")]
		public static void SetClipCommand(int clip)
		{
			if (ConsoleSystem.Caller?.Pawn is Player player && player.IsValid())
			{
				if (player.ActiveChild is CKWeaponFirearm firearm)
					firearm.PrimaryCapacity.SetClip(clip);
			}
		}

		[ConCmd.Admin("firearm_setammo")]
		public static void SetAmmoCommand(int ammo)
		{
			if (ConsoleSystem.Caller?.Pawn is Player player && player.IsValid())
			{
				if (player.ActiveChild is CKWeaponFirearm firearm)
					firearm.PrimaryCapacity.SetAmmo(ammo);
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
						firearm.PrimaryCapacity.SetAmmo(firearm.PrimaryCapacity.MaxAmmo + missingPrimary);
					}
					
					if (firearm.SecondaryCapacity != null)
					{
						int missingSecondary = firearm.SecondaryCapacity.MaxClip - firearm.SecondaryCapacity.Clip;
						firearm.SecondaryCapacity.SetAmmo(firearm.SecondaryCapacity.MaxAmmo + missingSecondary);
					}
				}
			}
		}
	}

	public partial class WeaponCapacity : BaseNetworkable
	{
		[Net] public int Clip { get; private set; }
		[Net] public int Ammo { get; private set; }
		[Net] public int MaxClip { get; private set; }
		[Net] public int MaxAmmo { get; private set; }

		[Net] public CKRoundDefinition RoundDefinition { get; set; }

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
			RoundDefinition = settings.RoundDefinition;

			//Log.Info($"{NetworkIdent} being setup by: {settings}");
		}

		public void SetClip(int amount) => Clip = amount;
		public void SetAmmo(int amount) => Ammo = amount;

		public bool CanTakeClip(int amount = 1) { return InfiniteClip || Clip >= amount; }
		public bool CanTakeAmmo(int amount = 1) { return InfiniteAmmo || Ammo >= amount; }

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
