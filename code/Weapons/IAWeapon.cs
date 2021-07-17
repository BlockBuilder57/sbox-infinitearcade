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
		public override void Simulate(Client player)
		{
			base.Simulate(player);
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
