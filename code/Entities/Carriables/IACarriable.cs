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
		public virtual string WorldModelPath { get; set; } = "models/error.vmdl";
		[Net] public string BucketIdent { get; set; } = "none";

		[Net, Predicted] public TimeSince TimeSinceDeployed { get; set; }
		[Net] public TimeSince TimeSinceDropped { get; set; }

		public SleepingPickupTrigger PickupTrigger { get; set; }

		public IACarriable()
		{
			BucketIdent = "none";
		}

		public override void Spawn()
		{
			base.Spawn();

			if (!string.IsNullOrWhiteSpace(WorldModelPath))
				SetModel(WorldModelPath);

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

		public override void CreateViewModel()
		{
			Host.AssertClient();

			if (string.IsNullOrEmpty(ViewModelPath))
				return;

			ViewModelEntity = new IAViewModel
			{
				Position = Position,
				Owner = Owner,
				EnableViewmodelRendering = true
			};

			ViewModelEntity.SetModel(ViewModelPath);
		}
	}
}
