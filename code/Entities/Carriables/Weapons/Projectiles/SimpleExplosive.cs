using CubicKitsune;
using Sandbox;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace infinitearcade
{
	[Library("projectile_simple", Title = "Simple Explosive")]
	public partial class SimpleExplosive : CKProjectile
	{
		public override void OnLifetimeRunout()
		{
			if (Host.IsServer)
				_ = ExplodeAsync(0);
		}

		public override void TakeDamage(DamageInfo info)
		{
			if (Lifetime > 0 || info.Flags.HasFlag(DamageFlags.Generic) && info.Flags.HasFlag(DamageFlags.PhysicsImpact))
				return;

			base.TakeDamage(info);
		}

		protected override void OnPhysicsCollision(CollisionEventData eventData)
		{
			base.OnPhysicsCollision(eventData);

			if (Lifetime <= 0 && eventData.Other.Entity != Owner)
				OnLifetimeRunout();
		}
	}
}
