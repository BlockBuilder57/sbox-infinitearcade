using System;
using System.Collections.Generic;
using System.Linq;
using Sandbox;

namespace CubicKitsune
{
	public partial class CKPlayer : Player
	{
		//[Net] public new float Health { get; set; }
		[Net] public float MaxHealth { get; set; }

		[Net] public float Armor { get; set; }
		[Net] public float MaxArmor { get; set; }
		[Net] public float ArmorPower { get; set; }
		
		public enum GodModes
		{
			Mortal,
			God,
			Buddha,
			TargetDummy
		}
		[Net] public GodModes GodMode { get; set; }
		
		public ClothingContainer ClothingContainer = new();
		protected bool m_clothed = false;
		
		public Transform VRSeatedOffset { protected get; set; } = Transform.Zero;
		
		protected DamageInfo m_lastDamage;

		protected readonly int m_slotOffset = (int)Math.Log2((int)InputButton.Slot1);

		public CKPlayer()
		{
			Inventory = new CKInventory(this);
		}
		
		public CKPlayer(Client cl) : this()
		{
			ClothingContainer.LoadFromClient(cl);
		}

		public override void Respawn()
		{
			Host.AssertServer();

			SetModel("models/citizen/citizen.vmdl");

			Controller = new QPhysController();

			Animator = new EyePosAnimator();
			ChangeCamera();

			ResetSeatedPos();

			EnableAllCollisions = true;
			EnableDrawing = true;
			EnableHideInFirstPerson = true;
			EnableShadowInFirstPerson = true;

			LifeState = LifeState.Alive;
			Velocity = Vector3.Zero;
			WaterLevel = 0;

			InitLoadout();

			//Undress();
			if (m_clothed)
				ClothesSetVisiblity(true);
			else
				Clothe();

			CreateHull();

			Game.Current?.MoveToSpawnpoint(this);
			ResetInterpolation();

			PostRespawn(To.Single(Client));
		}

		[ClientRpc]
		public void PostRespawn()
		{
			if (Host.IsClient)
			{
				if (Inventory is CKInventory inv)
					inv.ListReorder();
			}
		}

		public virtual void InitLoadout()
		{
			CKPlayerLoadoutResource loadout = ResourceLibrary.Get<CKPlayerLoadoutResource>("assets/loadouts/default.loadout");
			SetupFromLoadoutResource(loadout);
		}

		public void SetupFromLoadoutResource(CKPlayerLoadoutResource loadout)
		{
			if (loadout == null)
			{
				Health = MaxHealth = 100f;
				Armor = MaxArmor = ArmorPower = 0f;
				throw new Exception("Loadout was null!");
			}

			Health = loadout.StartingHealth;
			MaxHealth = loadout.MaxHealth;
			Armor = loadout.StartingArmor;
			MaxArmor = loadout.MaxArmor;
			ArmorPower = loadout.StartingArmorPower;
			
			if (Client.IsBot || loadout.Carriables == null)
				return;

			foreach (CKCarriableResource def in loadout.Carriables)
			{
				// temporary workaround
				var def2 = ResourceLibrary.Get<CKCarriableResource>(def.ResourcePath);
				
				if (def2 == null)
					throw new Exception("Null definition in loadout carriables!");
				
				CKCarriable carriable = TypeLibrary.Create<CKCarriable>(def2.LibraryType);

				if (carriable != null)
				{
					carriable.SetupFromResource(def2);
					Inventory?.Add(carriable);
				}
			}
		}

		public virtual Transform GetSpawnpoint()
		{
			SpawnPoint spawnpoint = Entity.All.OfType<SpawnPoint>().OrderBy(x => Guid.NewGuid()).FirstOrDefault();

			if (spawnpoint != null)
				return spawnpoint.Transform;
			else
				return Transform.Zero;
		}

		public List<ModelEntity> Clothing = new();

		public virtual void Clothe()
		{
			ClothingContainer.DressEntity(this);
			m_clothed = true;
		}

		public virtual void Undress()
		{
			ClothingContainer.ClearEntities();
			m_clothed = false;
		}

		public void AddAsClothes(string modelStr, string specificBone = null, Transform? offsetTransform = null)
		{
			ModelEntity model = new();
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

				ModelPropData propInfo = model.Model.GetData<ModelPropData>();
				if (propInfo?.ParentBodygroupName != null)
				{
					SetBodyGroup(propInfo.ParentBodygroupName, propInfo.ParentBodygroupValue);
				}
			}
		}

		public void ClothesSetVisiblity(bool visible)
		{
			// we can't get the entities directly for some reason
			foreach (ModelEntity model in Children)
				if (model.Tags.Has("clothes"))
					model.EnableDrawing = visible;
		}

		public override void BuildInput(InputBuilder input)
		{
			base.BuildInput(input);

			//if (GetActiveController() is QPhysController qPhys)
			//	Input.Rotation = Rotation.From(qPhys.BaseAngularVelocity);	
		}

		public override void Simulate(Client cl)
		{
			if (IsServer && LifeState == LifeState.Dead && (Input.Pressed(InputButton.PrimaryAttack) || Client.IsBot))
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

				if (Inventory is CKInventory inv)
				{
					if (Input.MouseWheel != 0)
						inv.SwitchActiveSlot(-Input.MouseWheel, true);
					if (Input.Pressed(InputButton.SlotNext))
						inv.SwitchActiveSlot(1, true);
					if (Input.Pressed(InputButton.SlotPrev))
						inv.SwitchActiveSlot(-1, true);

					void SlotInput(InputButton btn)
					{
						if (Input.Pressed(btn))
							inv.SetActiveSlot((int)Math.Log2((int)btn) - m_slotOffset, true);
					}

					SlotInput(InputButton.Slot1);
					SlotInput(InputButton.Slot2);
					SlotInput(InputButton.Slot3);
					SlotInput(InputButton.Slot4);
					SlotInput(InputButton.Slot5);
					SlotInput(InputButton.Slot6);
					SlotInput(InputButton.Slot7);
					SlotInput(InputButton.Slot8);
					SlotInput(InputButton.Slot9);
					SlotInput(InputButton.Slot0);

					if (Input.Pressed(InputButton.Drop) && ActiveChild.IsValid())
					{
						Entity active = inv.DropActive();

						if (active.IsValid() && active.PhysicsGroup != null)
						{
							active.PhysicsGroup.Velocity = Velocity + BaseVelocity;

							const float throwForce = 500f;

							if (GetActiveController() is QPhysController qPhys && !qPhys.Ducking)
							{
								active.PhysicsGroup.AddAngularVelocity(active.Rotation.Left * 20f);
								Vector3 throwVector = ((EyeRotation.Forward * 500) + (Vector3.Up * 200)).Normal;
								active.PhysicsGroup.AddVelocity(throwVector * throwForce);
							}
							else
							{
								TraceResult tr = Trace.Ray(EyePosition, EyePosition + EyeRotation.Forward * 128 + Vector3.Down * 32).WorldAndEntities().Run();
								active.Rotation = EyeRotation.RotateAroundAxis(tr.Normal + Vector3.Forward, 90);
								Vector3 throwVector = ((EyeRotation.Forward * 600) + (Vector3.Up * 100)).Normal;
								active.PhysicsGroup.AddVelocity(throwVector * throwForce);
							}
						}

						// switch to the next active thing (if there is one!)
						inv.SwitchActiveSlot(0, true);
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
			Game.Current.LastCamera = CameraMode;

			if (Input.VR.IsActive || VR.Enabled)
			{
				ResetSeatedPos();
				if (CameraMode is not VRCamera)
				{
					CameraMode = new VRCamera();
				}
			}
			else
			{
				if (CameraMode is not FirstPersonCamera)
				{
					CameraMode = new FirstPersonCamera();
				}
				else
				{
					CameraMode = new ThirdPersonCamera();
				}
			}
		}

		public Transform ResetSeatedPos()
		{
			if (IsServer)
				return Transform.Zero;

			Vector3 relativeHeadPos = VR.Anchor.PointToLocal(Input.VR.Head.Position);
			Rotation relativeHeadRot = VR.Anchor.RotationToLocal(Input.VR.Head.Rotation);
			Transform offset = new(relativeHeadPos, relativeHeadRot);
			VRSeatedOffset = offset;
			//yLog.Info($"VRSeatedOffset: {offset.Position}, {offset.Rotation.Angles()}");
			return offset;
		}

		public override void PostCameraSetup(ref CameraSetup setup)
		{
			base.PostCameraSetup(ref setup);

			if (!Local.Client.IsUsingVr)
				return;

			Rotation rot = Rotation.FromYaw(EyeRotation.Yaw() - VRSeatedOffset.Rotation.Yaw());
			//RotatePointAroundPivotWithEuler(Vector3 point, Vector3 pivot, Vector3 angles)
			Vector3 point = Position;
			Vector3 pivot = Position + VRSeatedOffset.Position;
			Angles angles = rot.Angles();

			//DebugOverlay.Circle(point, Rotation.From(Vector3.Up.EulerAngles), 2, Color.Magenta);
			//DebugOverlay.Sphere(pivot, 2, Color.Yellow);

			Vector3 result = Rotation.From(angles) * (point - pivot) + pivot;
			result -= VRSeatedOffset.Position;

			Transform anchor = new(result + EyeLocalPosition, Rotation.LookAt(rot.Forward, rot.Up));
			//Transform head = anchor.ToWorld(VRSeatedOffset);

			//DebugOverlay.Axis(anchor.Position, anchor.Rotation);
			//DebugOverlay.Axis(head.Position, head.Rotation);

			//VR.Anchor = Transform.WithPosition(new Vector3(-128, 128, 32)).WithRotation(Rotation.Identity);
			//VR.Anchor = Transform.WithPosition(new Vector3(0, 0, 256)).WithRotation(Rotation.Identity);
			VR.Anchor = anchor;
		}

		public override float FootstepVolume()
		{
			return Math.Clamp(base.FootstepVolume(), 0.1f, 2f) * 5f;
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

		public override void StartTouch(Entity other)
		{
			base.StartTouch(other);

			if (IsClient) return;

			if (other is CKCarriable)
			{
				Inventory?.Add(other, Inventory.Active == null);
			}
		}
		
		public override void TakeDamage(DamageInfo info)
		{
			if (info.Damage == 0)
				return;
			if (!Host.IsServer || LifeState != LifeState.Alive)
				return;
			if (info.Damage > 0 && (GodMode == GodModes.God || (GodMode == GodModes.Buddha && Health == 1)))
				return;

			if (info.Damage > 0)
				this.ProceduralHitReaction(info);

			LastAttacker = info.Attacker;
			LastAttackerWeapon = info.Weapon;
			info.Damage *= 1f / Scale;

			float scratchHealth = Health;
			float scratchArmor = Armor;
			float scratchArmorPower = ArmorPower;

			//float debugTime = 4f;
			//string debugText = $"Incoming damage: {info.Damage:F2}";
			//float debugPreHealth = scratchHealth;
			//float debugPreArmor = scratchArmor;
			//float debugPreArmorPower = scratchArmorPower;
			
			if (info.Flags.HasFlag(DamageFlags.Blast))
				ApplyAbsoluteImpulse(info.Force * 0.2f);

			if (info.Damage > 0)
			{
				// desired behavior:
				//     no armor:
				//         do nothing
				//     armor < 1: (ex 0.6)
				//         take 60% of damage to armor
				//         apply remaining 40% of damage directly to health
				//     armor > 1: (ex 1.5)
				//         take (damage/1.5) to armor
				//     
				//     in all cases, apply negative suit as health
				
				// we have some armor, so let's run the armor calculations
				if (scratchArmor > 0)
				{
					//debugText += $"\n\tWe have some armor ({scratchArmor:F2}@{scratchArmorPower:F1})";
					
					float armorDamage = scratchArmorPower > 1 ? info.Damage / scratchArmorPower : info.Damage * scratchArmorPower;
					scratchArmor -= armorDamage;

					//debugText += $"\n\tArmor {(scratchArmor > 0 ? "had enough to tank" : "will be destroyed")}, now {scratchArmor:F2}";
					
					// don't overflow the damage into healing ever please thank you
					// also if we have >1 power, be sure to remove the "true" armor from the damage
					info.Damage = Math.Max(0, info.Damage - (scratchArmorPower > 1 ? info.Damage : armorDamage));
					
					//debugText += $"\n\tDamage reduced to {info.Damage:F2} by armor";
				}
				//else
					//debugText += $"\n\tWe have no armor";

				// take any negative armor values as health
				if (scratchArmor < 0)
				{
					// apply it to the damageinfo
					info.Damage -= scratchArmor;
					//debugText += $"\n\t\t...but knocked to {info.Damage:F2} by it breaking";
					scratchArmor = 0;
					scratchArmorPower = 0;
				}

				// armor didn't protect us all the way
				if (info.Damage > 0)
				{
					//debugText += $"\n\tTaking {info.Damage:F2} damage directly";
					scratchHealth -= info.Damage;
				}
			}
			else
			{
				// taking healing!
				scratchHealth -= info.Damage;
				//debugText += "\n\tIt's healing";
				
				//if (info.Weapon is CKTool)
					//debugTime = 0;
			}

			if (GodMode == GodModes.Buddha && scratchHealth <= 0)
				scratchHealth = 1;

			if (GodMode != GodModes.TargetDummy)
			{
				ArmorPower = scratchArmorPower;
				Armor = scratchArmor;
				Health = scratchHealth;

				if (scratchHealth <= 0)
				{
					//scratchHealth = 0;
					// set this early
					m_lastDamage = info;
					OnKilled();
				}
			}

			//debugText += $"\nFinal: {Health:F2} {Armor:F2}@{ArmorPower:F1} (Δ of {(Health - debugPreHealth):F2} {(Armor - debugPreArmor):F2}@{(ArmorPower - debugPreArmorPower):F1})";

			//DebugOverlay.Text(debugText.Replace("\t", "    "), Position.WithZ(Position.z + CollisionBounds.Maxs.z), CKDebugging.GetSideColor(), debugTime);
			//CKDebugging.ScreenText(debugText, debugTime);
			
			m_lastDamage = info;
		}

		public override void OnKilled()
		{
			base.OnKilled();

			BecomeRagdollOnClient(Velocity, m_lastDamage.Flags, m_lastDamage.Position, m_lastDamage.Force, GetBoneIndex(m_lastDamage.Hitbox.GetName()));
			ClothesSetVisiblity(false);
			RemoveAllDecals();

			CameraMode = new SpectateRagdollCamera();
			Controller = null;

			EnableAllCollisions = false;
			EnableDrawing = false;

			//if (this is not ArcadeMachinePlayer)
			//	Inventory?.DropActive();
			Inventory?.DeleteContents();

			// Add a score to the killer
			if (LifeState == LifeState.Dead && LastAttacker != null)
				if (LastAttacker.Client != null && LastAttacker != this)
					LastAttacker.Client.AddInt("kills");
		}

		[ClientRpc]
		private void BecomeRagdollOnClient(Vector3 velocity, DamageFlags damageFlags, Vector3 forcePos, Vector3 force, int bone)
		{
			Corpse ent = CreateDeathRagdoll();
			if (!ent.IsValid())
				return;

			ent.DeleteAsync(10.0f);

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
		}

		public Corpse CreateDeathRagdoll()
		{
			if (string.IsNullOrEmpty(GetModelName()))
				return null;

			var ent = new Corpse
			{
				Position = Position,
				Rotation = Rotation,
				Scale = Scale,

				UsePhysicsCollision = true,
				EnableAllCollisions = true,
				PhysicsEnabled = true,
				SurroundingBoundsMode = SurroundingBoundsType.Physics
			};

			ent.Tags.Add("corpse");

			ent.SetModel(GetModelName());

			ent.CopyBonesFrom(this);
			ent.CopyBodyGroups(this);
			ent.CopyMaterialGroup(this);
			ent.CopyMaterialOverrides(this);
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
					clothing.CopyBodyGroups(e);
					clothing.CopyMaterialGroup(e);
					clothing.CopyMaterialOverrides(e);
					clothing.TakeDecalsFrom(e);
					clothing.RenderColor = e.RenderColor;
				}
			}

			return ent;
		}

		protected bool IsUseDisabled()
		{
			return ActiveChild is IUse use && use.IsUsable(this);
		}

		protected override Entity FindUsable()
		{
			if (IsUseDisabled())
				return null;

			var tr = Trace.Ray(EyePosition, EyePosition + EyeRotation.Forward * 64).Ignore(this).Run();

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
