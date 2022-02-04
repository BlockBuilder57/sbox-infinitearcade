using Sandbox;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace infinitearcade
{
	public class IAInventory : BaseInventory
	{
		public IAInventory(Entity owner) : base(owner)
		{

		}

		public override bool CanAdd(Entity ent)
		{
			if (!ent.IsValid())
				return false;

			if (!base.CanAdd(ent))
				return false;

			return !IsCarryingType(ent.GetType());
		}

		public override bool Add(Entity ent, bool makeActive = false)
		{
			if (!ent.IsValid())
				return false;

			//if (IsCarryingType(entity.GetType()))
			//	return false;

			if (ent is IACarriable carriable && carriable.PickupTrigger != null && carriable.PickupTrigger.IsSleeping)
				return false;

			if (ent is IAWeapon weapon && weapon.TimeSinceDropped < 0.5f)
				return false;

			return base.Add(ent, makeActive);
		}

		public bool IsCarryingType(Type t)
		{
			return List.Any(x => x?.GetType() == t);
		}

		public override bool Drop(Entity ent)
		{
			if (!Host.IsServer)
				return false;

			if (!Contains(ent))
				return false;

			if (ent is IAWeaponFirearm firearm && firearm.IsReloading)
				return false;

			ent.SetParent(null);
			ent.OnCarryDrop(Owner);

			return true;
		}

		public override Entity DropActive()
		{
			if (!Host.IsServer) return null;

			var ac = Owner.ActiveChild;
			if (!ac.IsValid()) return null;

			if (Drop(ac))
			{
				Owner.ActiveChild = null;
				return ac;
			}

			return null;
		}

		public override bool SetActiveSlot(int i, bool evenIfEmpty = false)
		{
			if (Owner.ActiveChild is IAWeaponFirearm firearm && firearm.IsReloading)
			{
				firearm.TimeSinceReload = 0;
				firearm.IsReloading = false;
			}

			return base.SetActiveSlot(i, evenIfEmpty);
		}
	}
}
