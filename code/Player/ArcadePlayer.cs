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
		// public float Health (already exists)
		[Net]
		public float MaxHealth { get; set; }

		[Net]
		public float Armor { get; set; }
		[Net]
		public float MaxArmor { get; set; }

		[Net]
		public float ArmorMultiplier { get; set; }

		private bool m_clothed = false;

		public override void Respawn()
		{
			SetModel("models/citizen/citizen.vmdl");

			// Use WalkController for movement (you can make your own PlayerController for 100% control)
			Controller = new WalkController();

			// Use StandardPlayerAnimator  (you can make your own PlayerAnimator for 100% control)
			Animator = new StandardPlayerAnimator();

			// Use FirstPersonCamera (you can make your own Camera for 100% control)
			Camera = new FirstPersonCamera();

			EnableAllCollisions = true;
			EnableDrawing = true;
			EnableHideInFirstPerson = true;
			EnableShadowInFirstPerson = true;

			Host.AssertServer();

			LifeState = LifeState.Alive;
			Velocity = Vector3.Zero;
			WaterLevel.Clear();

			InitStats();

			if (IsServer)
			{
				//Undress();
				if (!m_clothed)
				{
					Clothe();
					m_clothed = true;
				}
			}

			CreateHull();

			Game.Current?.MoveToSpawnpoint(this);
			ResetInterpolation();
		}

		public virtual void InitStats()
		{
			Health = 20f;
			MaxHealth = 20f;

			Armor = 0f;
			MaxArmor = 50f;
			ArmorMultiplier = 1.0f;
		}

		[ConVar.ClientData("ia_player_clothes_head", Saved = true)]
		public string ClothesHead { get; set; } = "models/citizen_clothes/hair/hair_femalebun.black.vmdl";
		[ConVar.ClientData("ia_player_clothes_torso", Saved = true)]
		public string ClothesTorso { get; set; } = "models/citizen_clothes/jacket/jacket.red.vmdl";
		[ConVar.ClientData("ia_player_clothes_legs", Saved = true)]
		public string ClothesLegs { get; set; } = "models/citizen_clothes/trousers/trousers.jeans.vmdl;models/citizen_clothes/shoes/trainers.vmdl";

		public readonly List<ModelEntity> Clothing = new List<ModelEntity>();

		public virtual void Clothe()
		{
			List<string> splitClothes = new();

			splitClothes.AddRange(ConsoleSystem.GetValue("ia_player_clothes_head").Split(';'));
			splitClothes.AddRange(ConsoleSystem.GetValue("ia_player_clothes_torso").Split(';'));
			splitClothes.AddRange(ConsoleSystem.GetValue("ia_player_clothes_legs").Split(';'));

			foreach (string modelStr in splitClothes)
			{
				AddAsClothes(modelStr);
			}
		}

		public void AddAsClothes(string modelStr, string specificBone = null, Transform? offsetTransform = null)
		{
			ModelEntity model = new ModelEntity();
			model.SetModel(modelStr);

			if (string.IsNullOrEmpty(specificBone))
				model.SetParent(this, true);
			else
				model.SetParent(this, specificBone, offsetTransform);

			model.EnableShadowInFirstPerson = true;
			model.EnableHideInFirstPerson = true;
			model.Tags.Add("clothes");

			if (model != null && Clothing != null)
			{
				Clothing.Add(model);

				ModelPropData propInfo = model.GetModel().GetPropData();
				if (propInfo.ParentBodyGroupName != null)
				{
					SetBodyGroup(propInfo.ParentBodyGroupName, propInfo.ParentBodyGroupValue);
				}
			}
		}

		public virtual void Undress()
		{
			Clothing?.ForEach(entity => entity.Delete());
			Clothing?.Clear();

			m_clothed = false;
		}

		public virtual Transform GetSpawnpoint()
		{
			SpawnPoint spawnpoint = Entity.All.OfType<PlayerSpawnpoint>().Where(x => x.PlayerType == PlayerSpawnpoint.SpawnType.ArcadePlayer).OrderBy(x => Guid.NewGuid()).FirstOrDefault();

			if (spawnpoint == null)
				spawnpoint = Entity.All.OfType<SpawnPoint>().OrderBy(x => Guid.NewGuid()).FirstOrDefault();

			if (spawnpoint != null)
				return spawnpoint.Transform;
			else
				return Transform.Zero;
		}

		public override void Simulate(Client cl)
		{
			if (IsServer && LifeState == LifeState.Dead && Input.Down(InputButton.Attack1))
			{
				Respawn();
			}

			var controller = GetActiveController();
			controller?.Simulate(cl, this, GetActiveAnimator());

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

		TimeSince timeSinceLastFootstep = 0;

		public override void OnAnimEventFootstep(Vector3 pos, int foot, float volume)
		{
			if (!IsClient || LifeState != LifeState.Alive || GetActiveController()?.GroundEntity == null)
				return;

			if (timeSinceLastFootstep < 0.1f)
				return;

			timeSinceLastFootstep = 0;

			//DebugOverlay.Box( 5, pos, -1, 1, Color.Red );
			//DebugOverlay.Text( pos, $"{volume}", Color.White, 5 );
			//DebugOverlay.Line( pos, pos + Vector3.Down * 26, Color.Yellow, 5 );

			var tr = Trace.Ray(pos, pos + Vector3.Down * 26).Ignore(this).Run();

			if (!tr.Hit)
				return;

			tr.Surface.DoFootstep(this, tr, foot, volume);
		}

		public override void TakeDamage(DamageInfo info)
		{
			LastAttacker = info.Attacker;
			LastAttackerWeapon = info.Weapon;

			if (ArmorMultiplier == 0)
				ArmorMultiplier = 1f;

			//int debugLine = -1;
			//const float debugTime = 8f;
			//Vector2 debugPos = Vector2.One * 40;

			if (IsServer)
			{
				//DebugOverlay.ScreenText(debugPos, debugLine += 1, Color.Yellow, $"Incoming damage: {info.Damage}", debugTime);

				bool hadArmor = false;

				//DebugOverlay.ScreenText(debugPos, debugLine += 1, Color.Yellow, $"We have {(Armor <= 0 || info.Damage < 0 ? "no" : "some")} armor ({Armor} * {ArmorMultiplier} == {Armor * ArmorMultiplier} functional)", debugTime);

				// we have no armor, so let's not run the armor calculations
				if (Armor <= 0 || info.Damage < 0)
					ArmorMultiplier = 1.0f;
				else
				{
					hadArmor = true;
					//debugPos.x += 32;

					float trueArmor = Armor * ArmorMultiplier;
					float min = Math.Min(info.Damage, trueArmor);

					//DebugOverlay.ScreenText(debugPos, debugLine += 1, Color.Yellow, $"min = min({info.Damage}, {trueArmor})\ntrueArmor >= min ({trueArmor} >= {min}) is {trueArmor >= min}", debugTime);

					// if we either have enough armor to tank it, or the damage is so big it nukes our armor
					if (trueArmor >= min)
					{
						//DebugOverlay.ScreenText(debugPos, debugLine += 3, Color.Yellow, $"{(trueArmor > min ? "had enough to tank" : "will destroy armor")}, armor ({Armor}) -= {info.Damage / ArmorMultiplier}, now {Armor - info.Damage / ArmorMultiplier}", debugTime);
						// subtract the damage value, dampened by the armor multiplier
						Armor -= info.Damage / ArmorMultiplier;
					}

					//debugPos.x -= 32;
				}

				// take any negative armor values as health
				if (Armor < 0)
				{
					// make sure the damage left is not modified by the armor multiplier as our armor is supposed to be gone
					Armor *= ArmorMultiplier;
					Health += Armor;
					Armor = 0;
				}

				// if we didn't have any armor
				if (!hadArmor)
				{
					//DebugOverlay.ScreenText(debugPos, debugLine += 1, Color.Yellow, "Taking damage directly", debugTime);
					Health -= info.Damage;
				}

				if (Health <= 0)
				{
					//Health = 0;
					this.OnKilled();
				}

			}
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

		private void BecomeRagdollOnClient(Vector3 velocity, DamageFlags damageFlags, Vector3 forcePos, Vector3 force, int bone)
		{
			if (string.IsNullOrEmpty(GetModelName()))
				return;

			var ent = new ModelEntity();
			ent.Position = Position;
			ent.Rotation = Rotation;
			ent.Scale = Scale;

			ent.MoveType = MoveType.Physics;
			ent.UsePhysicsCollision = true;
			ent.EnableAllCollisions = true;
			ent.CollisionGroup = CollisionGroup.Debris;
			ent.EnableHitboxes = true;
			ent.SurroundingBoundsMode = SurroundingBoundsType.Physics;

			ent.SetInteractsAs(CollisionLayer.Debris);
			ent.SetInteractsWith(CollisionLayer.WORLD_GEOMETRY);
			ent.SetInteractsExclude(CollisionLayer.Player | CollisionLayer.Debris);

			ent.SetModel(GetModelName());

			ent.CopyBonesFrom(this);
			ent.CopyBodyGroups(this);
			ent.CopyMaterialGroup(this);
			ent.TakeDecalsFrom(this);
			ent.RenderColorAndAlpha = RenderColorAndAlpha;

			foreach (var child in Children)
			{
				if (child is ModelEntity e)
				{
					string model = e.GetModelName();
					if (string.IsNullOrEmpty(model) || !child.Tags.Has("clothes"))
						continue;

					var clothing = new ModelEntity();
					clothing.SetModel(model);
					clothing.SetParent(ent, true);
					clothing.RenderColorAndAlpha = e.RenderColorAndAlpha;
				}
			}

			ent.PhysicsGroup.Velocity = velocity;

			if (damageFlags.HasFlag(DamageFlags.Bullet) || damageFlags.HasFlag(DamageFlags.PhysicsImpact))
			{
				PhysicsBody body = bone > 0 ? ent.GetBonePhysicsBody(bone) : null;

				if (body != null)
					body.ApplyImpulseAt(forcePos, force * body.Mass);
				else
					ent.PhysicsGroup.ApplyImpulse(force);
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
