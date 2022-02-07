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
		public Dictionary<string, List<Entity>> BucketList = new();

		public IAInventory(Entity owner) : base(owner)
		{
			BucketList = new() {
				{ "primary", new() },
				{ "secondary", new() },
				{ "tool", new() }
			};
		}

		[ServerCmd("inv_clear")]
		public static void ClearCommand()
		{
			Client client = ConsoleSystem.Caller;
			if (!client.HasPermission("debug"))
				return;

			if (client?.Pawn is ArcadePlayer player)
			{
				player.Inventory.DeleteContents();
			}
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

		public override void DeleteContents()
		{
			base.DeleteContents();

			BucketList.Clear();
		}

		public bool IsCarryingType(Type t)
		{
			return List.Any(x => x?.GetType() == t);
		}

		public override void OnChildAdded(Entity child)
		{
			if (!CanAdd(child))
				return;

			if (List.Contains(child))
				throw new System.Exception("Trying to add to inventory multiple times. This is gated by Entity:OnChildAdded and should never happen!");

			if (child is IACarriable carriable)
			{
				BucketList.AddOrCreate(carriable.BucketIdent).Add(child);
			}

			RecalculateFlatList();
		}

		public override void OnChildRemoved(Entity child)
		{
			if (child is IACarriable carriable)
			{
				List<Entity> list = BucketList.GetValueOrDefault(carriable.BucketIdent);
				if (list != null)
					list.Remove(child);
			}

			RecalculateFlatList();
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

		public void RecalculateFlatList()
		{
			List.Clear();
			foreach (KeyValuePair<string, List<Entity>> kvp in BucketList)
			{
				if (kvp.Value != null)
					List.AddRange(kvp.Value);
			}
		}
	}
}
