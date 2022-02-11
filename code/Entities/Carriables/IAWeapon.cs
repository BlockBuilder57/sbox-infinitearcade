using Sandbox;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace infinitearcade
{
	public partial class IAWeapon : IACarriable, IUse
	{
		public virtual float PrimaryRate => 5.0f;
		public virtual float SecondaryRate => 15.0f;
		[Net, Predicted] public TimeSince TimeSincePrimaryAttack { get; set; }
		[Net, Predicted] public TimeSince TimeSinceSecondaryAttack { get; set; }

		public override void Simulate(Client player)
		{
			if (CanReload())
				Reload();

			// Reload could have changed our owner
			if (!Owner.IsValid())
				return;

			if (CanPrimaryAttack())
			{
				using (LagCompensation())
				{
					TimeSincePrimaryAttack = 0;
					AttackPrimary();
				}
			}

			// AttackPrimary could have changed our owner
			if (!Owner.IsValid())
				return;

			if (CanSecondaryAttack())
			{
				using (LagCompensation())
				{
					TimeSinceSecondaryAttack = 0;
					AttackSecondary();
				}
			}
		}

		public virtual bool CanReload()
		{
			if (!Owner.IsValid() || !Input.Down(InputButton.Reload)) return false;

			return true;
		}

		public virtual void Reload()
		{

		}

		public virtual bool CanPrimaryAttack()
		{
			if (!Owner.IsValid() || !Input.Down(InputButton.Attack1)) return false;

			var rate = PrimaryRate;
			if (rate <= 0) return true;

			return TimeSincePrimaryAttack > (1 / rate);
		}

		public virtual void AttackPrimary()
		{

		}

		public virtual bool CanSecondaryAttack()
		{
			if (!Owner.IsValid() || !Input.Down(InputButton.Attack2)) return false;

			var rate = SecondaryRate;
			if (rate <= 0) return true;

			return TimeSinceSecondaryAttack > (1 / rate);
		}

		public virtual void AttackSecondary()
		{

		}

		/// <summary>
		/// Does a trace from start to end, does bullet impact effects. Coded as an IEnumerable so you can return multiple
		/// hits, like if you're going through layers or ricocet'ing or something.
		/// </summary>
		public virtual IEnumerable<TraceResult> TraceBullet(Vector3 start, Vector3 end, float radius = 2.0f)
		{
			bool InWater = Physics.TestPointContents(start, CollisionLayer.Water);

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
