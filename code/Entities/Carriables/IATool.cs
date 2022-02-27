using Sandbox;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace infinitearcade
{
	public partial class IATool : IACarriable, IUse
	{
		[Flags]
		public enum InputMode
		{
			None       = 0,
			Single     = 1 << 0,
			FullAuto   = 1 << 1,
			Burst      = 1 << 2,
		}

		public partial class InputSettings : BaseNetworkable
		{
			[Net] public float Rate { get; set; } = 1.0f;
			[Net] public int BurstAmount { get; set; } = 3;
			[Net] public InputMode AllowedModes { get; set; }
			[Net] public TimeSince TimeSince { get; set; }
			[Net] public InputMode CurMode { get; set; }
			[Net] public int FiredBursts { get; set; }
		}

		[Net] public InputSettings Primary { get; set; }
		[Net] public InputSettings Secondary { get; set; }

		[Net] protected IAToolDefinition m_toolDef { get; set; }

		public override IACarriable SetupFromDefinition(IACarriableDefinition def)
		{
			base.SetupFromDefinition(def);

			if (def is IAToolDefinition toolDef)
			{
				Primary = new()
				{
					Rate = toolDef.PrimaryInput.Rate,
					BurstAmount = toolDef.PrimaryInput.BurstAmount,
					AllowedModes = toolDef.PrimaryInput.AllowedModes
				};
				Secondary = new()
				{
					Rate = toolDef.SecondaryInput.Rate,
					BurstAmount = toolDef.SecondaryInput.BurstAmount,
					AllowedModes = toolDef.SecondaryInput.AllowedModes
				};

				m_toolDef = toolDef;

				NextPrimaryFireMode();
				NextSecondaryFireMode();
			}

			return this;
		}

		public override void Simulate(Client cl)
		{
			if (CanReload())
				TryReload();

			// Reload could have changed our owner
			if (!Owner.IsValid())
				return;

			if (CanPrimaryAttack())
			{
				using (LagCompensation())
				{
					Primary.TimeSince = 0;
					TryAttackPrimary();
				}
			}

			// AttackPrimary could have changed our owner
			if (!Owner.IsValid())
				return;

			if (CanSecondaryAttack())
			{
				using (LagCompensation())
				{
					Secondary.TimeSince = 0;
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

		public virtual bool CanPrimaryAttack()
		{
			if (!Owner.IsValid()) return false;

			if (!Input.Down(InputButton.Attack1))
			{
				Primary.FiredBursts = 0;
				return false;
			}

			var rate = Primary.Rate;

			if (rate > 0)
				if (Primary.TimeSince < rate) return false;

			bool canFire = false;
			switch (Primary.CurMode)
			{
				case InputMode.Single:
					if (Input.Pressed(InputButton.Attack1))
						canFire = true;
					break;
				case InputMode.FullAuto:
					if (Input.Down(InputButton.Attack1))
						canFire = true;
					break;
				case InputMode.Burst:
					if (Input.Down(InputButton.Attack1) && Primary.FiredBursts < Primary.BurstAmount)
					{
						Primary.FiredBursts++;
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

		public virtual bool CanSecondaryAttack()
		{
			if (!Owner.IsValid()) return false;

			if (!Input.Down(InputButton.Attack2))
			{
				Secondary.FiredBursts = 0;
				return false;
			}

			var rate = Secondary.Rate;

			if (rate > 0)
				if (Secondary.TimeSince < rate) return false;

			bool canFire = false;
			switch (Secondary.CurMode)
			{
				case InputMode.Single:
					if (Input.Pressed(InputButton.Attack2))
						canFire = true;
					break;
				case InputMode.FullAuto:
					if (Input.Down(InputButton.Attack2))
						canFire = true;
					break;
				case InputMode.Burst:
					if (Input.Down(InputButton.Attack2) && Secondary.FiredBursts < Secondary.BurstAmount)
					{
						Secondary.FiredBursts++;
						canFire = true;
					}
					break;
			}

			return canFire;
		}

		public virtual void TryAttackSecondary()
		{
			AttackSecondary();
		}

		public virtual void AttackSecondary()
		{

		}

		public InputMode NextFireMode(InputMode allowed, InputMode current)
		{
			List<InputMode> modes = new();

			// modified https://stackoverflow.com/a/42557518
			bool[] bits = new BitArray(new[] { (int)allowed }).OfType<bool>().ToArray();
			for (int i = 0; i < bits.Length; i++)
			{
				if (bits[i])
					modes.Add((InputMode)(1 << i));
			}

			if (modes.Count < 1)
				return current; // we have nothing, don't even bother

			int newIndex = modes.IndexOf(Primary.CurMode) + 1;

			if (newIndex > modes.Count - 1)
				newIndex = 0;

			//Log.Info($"setting firemode to {modes[newIndex]}");
			return modes[newIndex];
		}

		public void NextPrimaryFireMode() => Primary.CurMode = NextFireMode(m_toolDef.PrimaryInput.AllowedModes, Primary.CurMode);
		public void NextSecondaryFireMode() => Secondary.CurMode = NextFireMode(m_toolDef.SecondaryInput.AllowedModes, Secondary.CurMode);

		/// <summary>
		/// Does a trace from start to end, does bullet impact effects. Coded as an IEnumerable so you can return multiple
		/// hits, like if you're going through layers or ricocet'ing or something.
		/// </summary>
		public virtual IEnumerable<TraceResult> TraceBullet(Vector3 start, Vector3 end, float radius = 2.0f)
		{
			bool InWater = Map.Physics.IsPointWater(start);

			var tr = Trace.Ray(start, end)
					.UseHitboxes()
					.HitLayer(CollisionLayer.Water, !InWater)
					.HitLayer(CollisionLayer.Debris)
					.Ignore(Owner)
					.Ignore(this)
					.Size(radius)
					.Run();

			yield return tr;

			// Another trace, bullet going through thin material, penetrating water surface?
		}
	}
}
