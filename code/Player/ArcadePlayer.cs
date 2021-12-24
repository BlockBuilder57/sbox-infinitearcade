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
		[Net] public new float Health { get; set; }
		[Net] public float MaxHealth { get; set; }

		[Net] public float Armor { get; set; }
		[Net] public float MaxArmor { get; set; }
		[Net] public float ArmorMultiplier { get; set; }

		[Net] public ArcadeMachine UsingMachine { get; set; }
		protected BasePlayerController m_machineController;

		public Transform VRSeatedOffset { private get; set; } = Transform.Zero;

		private bool m_clothed = false;

		public ArcadePlayer()
		{
			Inventory = new IAInventory(this);
		}

		public override void Respawn()
		{
			SetModel("models/citizen/citizen.vmdl");

			Controller = new QPhysController();

			if (m_machineController == null)
				m_machineController = new GravityOnlyController();

			Animator = new EyePosAnimator();
			ChangeCamera();

			ResetSeatedPos();

			EnableAllCollisions = true;
			EnableDrawing = true;
			EnableHideInFirstPerson = true;
			EnableShadowInFirstPerson = true;

			Host.AssertServer();

			LifeState = LifeState.Alive;
			Velocity = Vector3.Zero;
			WaterLevel.Clear();

			InitStats();

			//Undress();
			if (m_clothed)
				ClothesSetVisiblity(true);
			else
			{
				Clothe();
				m_clothed = true;
			}

			CreateHull();

			Game.Current?.MoveToSpawnpoint(this);
			ResetInterpolation();
		}

		public virtual void InitStats()
		{
			Health = 40f;
			MaxHealth = 40f;

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

			splitClothes.AddRange(Client.GetClientData("ia_player_clothes_head").Split(';'));
			splitClothes.AddRange(Client.GetClientData("ia_player_clothes_torso").Split(';'));
			splitClothes.AddRange(Client.GetClientData("ia_player_clothes_legs").Split(';'));

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
				if (propInfo?.ParentBodygroupName != null)
				{
					SetBodyGroup(propInfo.ParentBodygroupName, propInfo.ParentBodygroupValue);
				}
			}
		}

		public virtual void Undress()
		{
			Clothing?.ForEach(entity => entity.Delete());
			Clothing?.Clear();

			m_clothed = false;
		}

		public void ClothesSetVisiblity(bool visible)
		{
			Clothing?.ForEach(entity => entity.EnableDrawing = visible);
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
			if (IsServer && LifeState == LifeState.Dead && Input.Released(InputButton.Attack1))
			{
				Respawn();
			}

			if (LifeState != LifeState.Alive)
				return;

			var controller = GetActiveController();
			controller?.Simulate(cl, this, GetActiveAnimator());

			if (cl.Pawn == this)
			{
				TickPlayerUse();
				SimulateActiveChild(cl, ActiveChild);

				if (Input.Down(InputButton.Drop) && ActiveChild.IsValid())
				{
					Entity active = Inventory?.DropActive();

					if (active.IsValid())
					{
						active.PhysicsGroup.Velocity = Velocity + BaseVelocity;

						const float throwForce = 500f;

						if (GetActiveController() is QPhysController qPhys && !qPhys.Ducked)
						{
							active.PhysicsGroup.AddAngularVelocity(active.Rotation.Left * 20f);
							Vector3 throwVector = ((EyeRot.Forward * 500) + (Vector3.Up * 200)).Normal;
							active.PhysicsGroup.AddVelocity(throwVector * throwForce);
						}
						else
						{
							TraceResult tr = Trace.Ray(EyePos, EyePos + EyeRot.Forward * 128 + Vector3.Down * 32).WorldAndEntities().Run();
							active.Rotation = EyeRot.RotateAroundAxis(tr.Normal + Vector3.Forward, 90);
							Vector3 throwVector = ((EyeRot.Forward * 600) + (Vector3.Up * 100)).Normal;
							active.PhysicsGroup.AddVelocity(throwVector * throwForce);
						}
					}
				}
			}

			if (Input.Pressed(InputButton.View) && LifeState == LifeState.Alive)
			{
				ChangeCamera();
			}
		}

		public void ChangeCamera()
		{
			// block: by default, Z near and far are 0
			// this is just a lie, their true values are:
			// ZNear: 7 (3 in HL1/HL:S)
			//  ZFar: ~28378 (r_mapextents * 1.73205080757f)

			Game.Current.LastCamera = Camera as Camera;

			if (Input.VR.IsActive || VR.Enabled)
			{
				ResetSeatedPos();
				if (Camera is not VRCamera)
				{
					Camera = new VRCamera();
				}
			}
			else
			{
				if (Camera is not FirstPersonCamera)
				{
					Camera = new FirstPersonCamera();
					(Camera as FirstPersonCamera).SetZNear(7);
				}
				else
				{
					Camera = new ThirdPersonCamera();
				}
			}
		}

		public Transform ResetSeatedPos()
		{
			if (IsServer)
				return Transform.Zero;

			Vector3 relativeHeadPos = VR.Anchor.PointToLocal(Input.VR.Head.Position);
			Rotation relativeHeadRot = VR.Anchor.RotationToLocal(Input.VR.Head.Rotation);
			Transform offset = new Transform(relativeHeadPos, relativeHeadRot);
			VRSeatedOffset = offset;
			//yLog.Info($"VRSeatedOffset: {offset.Position}, {offset.Rotation.Angles()}");
			return offset;
		}

		public override void PostCameraSetup(ref CameraSetup setup)
		{
			base.PostCameraSetup(ref setup);

			Rotation rot = Rotation.FromYaw(EyeRot.Yaw() - VRSeatedOffset.Rotation.Yaw());
			//RotatePointAroundPivotWithEuler(Vector3 point, Vector3 pivot, Vector3 angles)
			Vector3 point = Position;
			Vector3 pivot = Position + VRSeatedOffset.Position;
			Angles angles = rot.Angles();

			//DebugOverlay.Circle(point, Rotation.From(Vector3.Up.EulerAngles), 2, Color.Magenta);
			//DebugOverlay.Sphere(pivot, 2, Color.Yellow);

			Vector3 result = Rotation.From(angles) * (point - pivot) + pivot;
			result -= VRSeatedOffset.Position;

			Transform anchor = new Transform(result + EyePosLocal, Rotation.LookAt(rot.Forward, rot.Up));
			//Transform head = anchor.ToWorld(VRSeatedOffset);

			//DebugOverlay.Axis(anchor.Position, anchor.Rotation);
			//DebugOverlay.Axis(head.Position, head.Rotation);

			//VR.Anchor = Transform.WithPosition(new Vector3(-128, 128, 32)).WithRotation(Rotation.Identity);
			//VR.Anchor = Transform.WithPosition(new Vector3(0, 0, 256)).WithRotation(Rotation.Identity);
			VR.Anchor = anchor;
		}

		public override float FootstepVolume()
		{
			return Math.Clamp(base.FootstepVolume(), 0.1f, 2f);
		}

		TimeSince timeSinceLastFootstep = 0;

		public virtual void OnAnimEventFootstep(Vector3 pos, int foot, float volume, bool force)
		{
			if (!IsClient || LifeState != LifeState.Alive || GetActiveController()?.GroundEntity == null)
				return;

			if (!force && timeSinceLastFootstep < 0.1f)
				return;

			volume *= FootstepVolume();

			timeSinceLastFootstep = 0;

			//DebugOverlay.Box(5, pos, -1, 1, Color.Red);
			//DebugOverlay.Text(pos, $"{volume}", Color.White, 5);
			//DebugOverlay.Line(pos, pos + Vector3.Down * 28, Color.Yellow, 5);

			var tr = Trace.Ray(pos, pos + Vector3.Down * 28).Ignore(this).Run();

			if (!tr.Hit)
				return;

			tr.Surface.DoFootstep(this, tr, foot, volume);
		}
		public override void OnAnimEventFootstep(Vector3 pos, int foot, float volume)
		{
			OnAnimEventFootstep(pos, foot, volume, false);
		}

		public override PawnController GetActiveController()
		{
			if (this is not ArcadeMachinePlayer)
			{
				if (UsingMachine.IsValid())
					return m_machineController;
			}

			return base.GetActiveController();
		}

		public override void TakeDamage(DamageInfo info)
		{
			LastAttacker = info.Attacker;
			LastAttackerWeapon = info.Weapon;

			if (ArmorMultiplier == 0)
				ArmorMultiplier = 1f;

			//int debugLine = -1;
			//const float debugTime = 0.5f;
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

				//DebugOverlay.ScreenText(debugPos, debugLine += 1, Color.Yellow, $"Final: {Health} {Armor} (x{ArmorMultiplier}:F1)", debugTime);
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

			ArcadeMachine machine = UsingMachine;
			List<ArcadeMachine> bubbleUp = new List<ArcadeMachine>();
			while (machine != null && machine.BeingPlayed)
			{
				bubbleUp.Add(machine);
				machine = machine.CreatedPlayer.UsingMachine;
			}
			bubbleUp.Reverse();
			bubbleUp.ForEach(x => x.ExitMachine());

			if (this is not ArcadeMachinePlayer)
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
			ent.RenderColor = RenderColor;

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
					clothing.TakeDecalsFrom(e);
					clothing.RenderColor = e.RenderColor;
				}
			}

			ClothesSetVisiblity(false);

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
