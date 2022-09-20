﻿using CubicKitsune;
using Sandbox;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CubicKitsune
{
	[Library("tool_generic")]
	public partial class CKTool : CKCarriable, ICKTool, IUse
	{
		[Net] public InputHelper PrimaryInput { get; set; }
		[Net] public InputHelper SecondaryInput { get; set; }
		[Net] public InputHelper ReloadInput { get; set; }
		public ICKTool.InputSettings PrimaryInputSettings { get; set; }
		public ICKTool.InputSettings SecondaryInputSettings { get; set; }
		public ICKTool.InputSettings ReloadInputSettings { get; set; }

		//[Net] public IDictionary<string, SoundEvent> SoundEvents { get; set; }

		public CKTool SetupFromInterface(ICKCarriable carry, ICKTool tool)
		{
			if (tool == null)
			{
				Log.Error($"{this} trying to set up with a null tool definition!");
				Delete();
				return null;
			}

			if (Host.IsServer)
			{
				PrimaryInput = new(tool.PrimaryInputSettings);
				SecondaryInput = new(tool.SecondaryInputSettings);
				ReloadInput = new(tool.ReloadInputSettings);

				//SoundEvents = tool.SoundEvents;
			}

			return (CKTool)SetupFromInterface(carry);
		}
		public override CKCarriable SetupFromDefinition(CKCarriableDefinition def) => SetupFromInterface(def, def as ICKTool);

		public override void Simulate(Client cl)
		{
			if (CanReload())
				TryReload();

			// Reload could have changed our owner
			if (!Owner.IsValid())
				return;

			if (CanAttack(InputButton.PrimaryAttack, PrimaryInput))
			{
				using (LagCompensation())
				{
					PrimaryInput?.ResetTime();
					TryAttackPrimary();
				}
			}

			// AttackPrimary could have changed our owner
			if (!Owner.IsValid())
				return;

			if (CanAttack(InputButton.SecondaryAttack, SecondaryInput))
			{
				using (LagCompensation())
				{
					SecondaryInput?.ResetTime();
					TryAttackSecondary();
				}
			}
		}

		public virtual bool CanReload()
		{
			if (!Owner.IsValid()) return false;

			return true;
		}

		public virtual void TryReload()
		{
			Reload();
		}

		public virtual void Reload()
		{

		}

		public virtual bool CanAttack(InputButton button, InputHelper input)
		{
			if (!Owner.IsValid()) return false;
			if (input == null) return true;

			if (!Input.Down(button))
			{
				input.FiredBursts = 0;
				return false;
			}

			var rate = input.Rate;

			if (rate > 0)
				if (input.TimeSince < rate) return false;

			bool canFire = false;
			switch (input.CurMode)
			{
				case ICKTool.InputMode.Single:
					if (Input.Pressed(button))
						canFire = true;
					break;
				case ICKTool.InputMode.FullAuto:
					if (Input.Down(button))
						canFire = true;
					break;
				case ICKTool.InputMode.Burst:
					if (Input.Down(button) && input.FiredBursts < input.BurstAmount)
					{
						input.FiredBursts++;
						canFire = true;
					}
					break;
			}

			return canFire;
		}

		public virtual void TryAttackPrimary()
		{
			AttackPrimary();
		}
		public virtual void AttackPrimary()
		{

		}

		public virtual void TryAttackSecondary()
		{
			AttackSecondary();
		}

		public virtual void AttackSecondary()
		{

		}

		/// <summary>
		/// Does a trace from start to end. Coded as an IEnumerable so you can return multiple
		/// hits, like if you're going through layers or ricocet'ing or something.
		/// </summary>
		public virtual IEnumerable<TraceResult> TraceHitscan(Vector3 start, Vector3 end, float radius = 2.0f, bool testWater = true, int maxBounces = 0, float maxGlanceAngle = 8f)
		{
			List<string> withoutTags = new() { "trigger" };
			int hits = 0;

			if (testWater)
			{
				bool InWater = Trace.TestPoint(start, "water");

				if (InWater)
				{
					// dampen bullets SIGNIFICANTLY in water
					Vector3 diff = end - start;
					end = start + (diff.Normal * 64);

					withoutTags.Add("water");
				}
			}

			var tr = Trace.Ray(start, end)
					.UseHitboxes()
					.Ignore(Owner)
					.Ignore(this)
					.WithoutTags(withoutTags.ToArray())
					.Size(radius)
					.Run();

			yield return tr;

			float vecLength = Vector3.DistanceBetween(start, end);

			

			while (tr.Hit && tr.Entity.IsWorld)
			{
				hits++;

				float angle = Vector3.GetAngle(tr.Normal, tr.Direction);
				Vector3 newDir = Vector3.Reflect(tr.Direction, tr.Normal);

				/*if (Host.IsServer)
				{
					DebugOverlay.Line(tr.StartPosition, tr.EndPosition, Color.Yellow, 2f);
					DebugOverlay.Text($"hit {hits} occured (ang {angle-90f})", tr.EndPosition, 2f);
				}*/

				if ((angle - 90f) < maxGlanceAngle && hits < maxBounces)
				{
					tr = Trace.Ray(tr.EndPosition, tr.EndPosition + newDir * vecLength)
						.UseHitboxes()
						.Ignore(Owner)
						.Ignore(this)
						.WithoutTags(withoutTags.ToArray())
						.Size(radius)
						.Run();

					yield return tr;
				}
				else
					break;
			}

			// Another trace, bullet going through thin material, penetrating water surface?
		}
	}

	public partial class InputHelper : BaseNetworkable
	{
		[Net] public float Rate { get; private set; } = 1.0f;
		[Net] public int BurstAmount { get; private set; } = 3;
		[Net] [BitFlags] public ICKTool.InputMode AllowedModes { get; private set; }
		[Net] public TimeSince TimeSince { get; private set; }
		[Net] public ICKTool.InputMode CurMode { get; private set; } = ICKTool.InputMode.None;
		[Net] public int FiredBursts { get; set; }

		public InputHelper()
		{
			Rate = 1.0f;
			BurstAmount = 3;
			AllowedModes = ICKTool.InputMode.Single;
			NextFireMode();
		}

		public InputHelper(ICKTool.InputSettings settings)
		{
			Rate = settings.Rate;
			BurstAmount = settings.BurstAmount;
			AllowedModes = settings.AllowedModes;
			NextFireMode();
		}

		public void ResetTime() => TimeSince = 0;

		public void NextFireMode()
		{
			var modes = Enum.GetValues(typeof(ICKTool.InputMode)).Cast<ICKTool.InputMode>().Where(m => AllowedModes.HasFlag(m) && m != 0).ToList();

			if (modes.Count < 1)
				return; // we have nothing, don't even bother

			int newIndex = modes.IndexOf(CurMode) + 1;

			if (newIndex > modes.Count - 1)
				newIndex = 0;

			//Log.Info($"setting firemode to {modes[newIndex]}");
			CurMode = modes[newIndex];
		}
	}
}
