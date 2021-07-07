using Sandbox;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace infinitearcade
{
	public partial class ArcadePlayer : Sandbox.Player
	{
		public override void Respawn()
		{
			SetModel("models/citizen/citizen.vmdl");

			// Use WalkController for movement (you can make your own PlayerController for 100% control)
			if (Controller == null)
				Controller = new WalkController();

			// Use StandardPlayerAnimator  (you can make your own PlayerAnimator for 100% control)
			if (Animator == null)
				Animator = new StandardPlayerAnimator();

			// Use FirstPersonCamera (you can make your own Camera for 100% control)
			Camera = new FirstPersonCamera();

			EnableAllCollisions = true;
			EnableDrawing = true;
			EnableHideInFirstPerson = true;
			EnableShadowInFirstPerson = true;

			base.Respawn();
		}

		public virtual Transform GetSpawnpoint()
		{
			PlayerSpawnpoint spawnpoint = Entity.All.OfType<PlayerSpawnpoint>().Where(x => x.PlayerType == PlayerSpawnpoint.SpawnType.ArcadePlayer).OrderBy(x => Guid.NewGuid()).FirstOrDefault();

			if (spawnpoint != null)
				return spawnpoint.Transform;
			else
				return Transform.Zero;
		}

		public override void Simulate(Client cl)
		{
			base.Simulate(cl);

			if (LifeState != LifeState.Alive)
				return;

			if (cl.Pawn == this)
				TickPlayerUse();

			if (Input.Pressed(InputButton.View) && LifeState == LifeState.Alive)
			{
				if (Camera is not FirstPersonCamera)
				{
					Camera = new FirstPersonCamera();
				}
				else
				{
					Camera = new ThirdPersonCamera();
				}
			}
		}

		private void BecomeRagdollOnClient(Vector3 velocity, DamageFlags damageFlags, Vector3 forcePos, Vector3 force, int bone)
		{
			var ent = new ModelEntity();
			ent.Position = Position;
			ent.Rotation = Rotation;
			ent.Scale = Scale;
			ent.MoveType = MoveType.Physics;
			ent.UsePhysicsCollision = true;
			ent.EnableAllCollisions = true;
			ent.CollisionGroup = CollisionGroup.Debris;
			ent.SetModel(GetModelName());
			ent.CopyBonesFrom(this);
			ent.CopyBodyGroups(this);
			ent.CopyMaterialGroup(this);
			ent.TakeDecalsFrom(this);
			ent.EnableHitboxes = true;
			ent.EnableAllCollisions = true;
			ent.SurroundingBoundsMode = SurroundingBoundsType.Physics;
			ent.RenderColorAndAlpha = RenderColorAndAlpha;
			ent.PhysicsGroup.Velocity = velocity;

			ent.SetInteractsAs(CollisionLayer.Debris);
			ent.SetInteractsWith(CollisionLayer.WORLD_GEOMETRY);
			ent.SetInteractsExclude(CollisionLayer.Player | CollisionLayer.Debris);

			foreach (var child in Children)
			{
				if (child is ModelEntity e)
				{
					var model = e.GetModelName();
					if (model != null && !model.Contains("clothes"))
						continue;

					var clothing = new ModelEntity();
					clothing.SetModel(model);
					clothing.SetParent(ent, true);
					clothing.RenderColorAndAlpha = e.RenderColorAndAlpha;
				}
			}

			if (damageFlags.HasFlag(DamageFlags.Bullet) ||
				 damageFlags.HasFlag(DamageFlags.PhysicsImpact))
			{
				PhysicsBody body = bone > 0 ? ent.GetBonePhysicsBody(bone) : null;

				if (body != null)
				{
					body.ApplyImpulseAt(forcePos, force * body.Mass);
				}
				else
				{
					ent.PhysicsGroup.ApplyImpulse(force);
				}
			}

			if (damageFlags.HasFlag(DamageFlags.Blast))
			{
				if (ent.PhysicsGroup != null)
				{
					ent.PhysicsGroup.AddVelocity((Position - (forcePos + Vector3.Down * 100.0f)).Normal * (force.Length * 0.2f));
					var angularDir = (Rotation.FromYaw(90) * force.WithZ(0).Normal).Normal;
					ent.PhysicsGroup.AddAngularVelocity(angularDir * (force.Length * 0.02f));
				}
			}

			Corpse = ent;

			ent.DeleteAsync(10.0f);
		}

		public override void OnKilled()
		{
			base.OnKilled();

			BecomeRagdollOnClient(Velocity, DamageFlags.Generic, Vector3.Zero, Vector3.Zero, GetHitboxBone(0));
			Camera = new SpectateRagdollCamera();
			Controller = null;

			EnableAllCollisions = false;
			EnableDrawing = false;

			Inventory?.DropActive();
			Inventory?.DeleteContents();
		}

		protected bool IsUseDisabled()
		{
			return ActiveChild is IUse use && use.IsUsable(this);
		}

		protected override Entity FindUsable()
		{
			if (IsUseDisabled())
				return null;

			var tr = Trace.Ray(EyePos, EyePos + EyeRot.Forward * 64).Ignore(this).Run();

			if (tr.Entity == null) return null;
			if (tr.Entity is not IUse use) return null;
			if (!use.IsUsable(this)) return null;

			return tr.Entity;
		}

		protected override void UseFail()
		{
			if (IsUseDisabled())
				return;

			base.UseFail();
		}
	}
}
