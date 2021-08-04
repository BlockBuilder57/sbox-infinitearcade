using Sandbox;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace infinitearcade
{
	public partial class IAWeapon : BaseWeapon, IUse
	{
		[Net, Predicted]
		public TimeSince TimeSinceDeployed { get; set; }

		protected PickupTrigger PickupTrigger { get; set; }
		[Net]
		public TimeSince TimeSinceDropped { get; set; }

		public override void Spawn()
		{
			base.Spawn();

			PickupTrigger = new PickupTrigger
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

		public override void OnCarryDrop(Entity dropper)
		{
			base.OnCarryDrop(dropper);

			TimeSinceDropped = 0;
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
