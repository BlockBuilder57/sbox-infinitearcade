using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Sandbox;

namespace infinitearcade
{
	public partial class IACarriable : BaseCarriable
	{
		[Net] public IACarriableDefinition Definition { get; set; }

		[Net, Predicted] public TimeSince TimeSinceDeployed { get; set; }
		[Net] public TimeSince TimeSinceDropped { get; set; }

		public SleepingPickupTrigger PickupTrigger { get; set; }

		private bool m_definitionLoaded;

		public virtual IACarriable SetupFromDefinition(IACarriableDefinition def)
		{
			if (def == null)
			{
				Log.Error($"{this} trying to set up with a null definition!");
				Delete();
				return null;
			}

			Definition = def;
			m_definitionLoaded = true;

			Model = def.WorldModel;

			return this;
		}

		public override void Spawn()
		{
			base.Spawn();

			if (!m_definitionLoaded)
				SetupFromDefinition(Asset.FromPath<IACarriableDefinition>("carriables/default.carry"));

			if (!m_definitionLoaded)
				Delete();

			CollisionGroup = CollisionGroup.Weapon; // so players touch it as a trigger but not as a solid
			SetInteractsAs(CollisionLayer.Debris); // so player movement doesn't walk into it

			PickupTrigger = new SleepingPickupTrigger
			{
				Parent = this,
				Position = Position,
				EnableTouch = true,
				EnableSelfCollisions = false
			};

			PickupTrigger.PhysicsBody.EnableAutoSleeping = false;
		}

		public override void ActiveStart(Entity ent)
		{
			base.ActiveStart(ent);

			TimeSinceDeployed = 0;
		}

		public override void OnCarryStart(Entity carrier)
		{
			base.OnCarryStart(carrier);
		}

		public override void OnCarryDrop(Entity dropper)
		{
			base.OnCarryDrop(dropper);

			TimeSinceDropped = 0;
			PickupTrigger?.SleepFor(1f);
		}

		public bool OnUse(Entity user)
		{
			if (!user.IsValid() || Owner != null)
				return false;

			// pretend we touched it
			user.StartTouch(this);

			return false;
		}

		public virtual bool IsUsable(Entity user)
		{
			if (Owner != null)
				return false;

			if (user.Inventory is IAInventory inventory)
				return inventory.CanAdd(this);

			return true;
		}

		public void Remove()
		{
			PhysicsGroup?.Wake();
			Delete();
		}

		public override void SimulateAnimator(PawnAnimator anim)
		{
			if (Definition != null && Definition.AnimGraphSettings != null && Definition.AnimGraphSettings.Length > 0)
			{
				foreach (IACarriableDefinition.AnimGraphSetting setting in Definition.AnimGraphSettings)
				{
					//IADebugging.ScreenText($"processing {setting.Key}: {setting.Value}");
					switch (setting.Type)
					{
						case IACarriableDefinition.AnimGraphTypes.Bool:
							anim.SetParam(setting.Key, bool.Parse(setting.Value));
							break;
						case IACarriableDefinition.AnimGraphTypes.Int:
							anim.SetParam(setting.Key, int.Parse(setting.Value));
							break;
						case IACarriableDefinition.AnimGraphTypes.Float:
							anim.SetParam(setting.Key, float.Parse(setting.Value));
							break;
						case IACarriableDefinition.AnimGraphTypes.Vector3:
							anim.SetParam(setting.Key, Vector3.Parse(setting.Value));
							break;
					}
				}
			}
			else
			{
				base.SimulateAnimator(anim);
			}
		}

		public override void CreateViewModel()
		{
			Host.AssertClient();

			if (Definition == null)
				return;

			ViewModelEntity = new IAViewModel
			{
				Position = Position,
				Owner = Owner,
				Model = Definition.ViewModel,
				EnableViewmodelRendering = true
			};
		}
	}
}
