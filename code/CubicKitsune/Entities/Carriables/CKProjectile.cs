using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Sandbox;

namespace CubicKitsune
{
	[Library("projectile_base", Title = "Base Projectile")]
	public partial class CKProjectile : BasePhysics
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
			OnKilled();
		}

		public override void OnNewModel(Model model)
		{
			base.OnNewModel(model);

			// When a model is reloaded, all entities get set to NULL model first
			if (model == null || model.IsError) return;

			if (IsServer)
				UpdatePropData(model);
		}

		/// <summary>
		/// Called on new model to update the prop with <see cref="ModelPropData"/> data of the new model.
		/// </summary>
		protected virtual void UpdatePropData(Model model)
		{
			Host.AssertServer();

			if (model.TryGetData(out ModelPropData propInfo))
				Health = propInfo.Health;

			if (Health <= 0)
				Health = -1;
		}

		DamageInfo LastDamage;

		public override void TakeDamage(DamageInfo info)
		{
			LastDamage = info;
			base.TakeDamage(info);
		}

		public override void OnKilled()
		{
			if (LifeState != LifeState.Alive)
				return;

			LifeState = LifeState.Dead;

			if (LastDamage.Flags.HasFlag(DamageFlags.PhysicsImpact))
				Velocity = lastCollision.This.PreVelocity;

			if (HasExplosionBehavior())
			{
				if (LastDamage.Flags.HasFlag(DamageFlags.Blast))
				{
					LifeState = LifeState.Dying;

					// Don't explode right away and cause a stack overflow
					var rand = new Random();
					_ = ExplodeAsync(RandomExtension.Float(rand, 0.05f, 0.1f));

					return;
				}
				else
				{
					DoExplosion();
					Delete(); // LifeState.Dead prevents this in OnKilled
				}
			}
			else
			{
				Delete(); // LifeState.Dead prevents this in OnKilled
			}

			base.OnKilled();
		}
		
		CollisionEventData lastCollision;

		protected override void OnPhysicsCollision( CollisionEventData eventData )
		{
			lastCollision = eventData;
			base.OnPhysicsCollision( eventData );
		}

		private bool HasExplosionBehavior()
		{
			if ( Model == null || Model.IsError )
				return false;

			return Model.HasData<ModelExplosionBehavior>();
		}

		public async Task ExplodeAsync(float fTime)
		{
			if (LifeState != LifeState.Alive && LifeState != LifeState.Dying)
				return;

			LifeState = LifeState.Dead;

			await Task.DelaySeconds(fTime);
			DoExplosion();
			Delete();
		}

		protected void DoExplosion()
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
