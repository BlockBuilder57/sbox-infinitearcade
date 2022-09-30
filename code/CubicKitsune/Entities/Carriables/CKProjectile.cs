using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Sandbox;

namespace CubicKitsune
{
	[Library("projectile_base", Title = "Base Projectile")]
	public partial class CKProjectile : Prop
	{
		[Net] public float Damage { get; set; } = -1;
		[Net] public float Lifetime { get; set; } = -1;

		[Net] private TimeSince m_sinceSpawn { get; set; }

		public CKProjectile()
		{
			m_sinceSpawn = 0;

			Tags.Clear();
			Tags.Add("weapon");
		}

		[Event.Tick]
		public virtual void OnTick()
		{
			if (Lifetime > 0 && m_sinceSpawn > Lifetime)
				OnLifetimeRunout();
		}

		public virtual void OnLifetimeRunout()
		{
			if (Host.IsServer)
				Delete();
		}

		// THIS REQUIRES EDITING PROP.CS (for now!)
		protected override void DoExplosion()
		{
			if (Model == null || Model.IsError)
				return;

			// Damage and push away all other entities
			var srcPos = Position;
			if (PhysicsBody.IsValid()) srcPos = PhysicsBody.MassCenter;

			if (Model.TryGetData(out ModelExplosionBehavior explosionBehavior) && explosionBehavior.Radius > 0.0f)
			{
				new ExplosionEntity
				{
					Position = srcPos,
					Radius = explosionBehavior.Radius,
					Damage = Damage == -1 ? explosionBehavior.Damage : Damage,
					ForceScale = explosionBehavior.Force,
					ParticleOverride = explosionBehavior.Effect,
					SoundOverride = explosionBehavior.Sound
				}.Explode(this);
			}
			else
			{
				new ExplosionEntity
				{
					Position = srcPos,
					Radius = 128,
					Damage = Damage,
					ForceScale = 40
				}.Explode(this);
			}
		}
	}
}
