using Sandbox;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using infinitearcade.UI;

namespace infinitearcade
{
	public partial class IAInventory : BaseNetworkable, IBaseInventory
	{
		public Entity Owner { get; init; }
		[Net] public List<Entity> List { get; set; } = new List<Entity>();
		[Net] public Dictionary<string, List<Entity>> BucketList { get; set; } = new();

		public virtual int Count() => List.Count;
		public virtual bool Contains(Entity ent) => List.Contains(ent);
		public bool IsCarryingType(Type t) => List.Any(x => x?.GetType() == t);

		public IAInventory(Entity owner)
		{
			Owner = owner;

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

		public virtual void DeleteContents()
		{
			Host.AssertServer();

			foreach (var item in List.ToArray())
				item.Delete();

			List.Clear();
			BucketList.Clear();
		}

		public virtual Entity GetSlot(int i)
		{
			if (List.Count <= i) return null;
			if (i < 0) return null;

			return List[i];
		}

		public virtual int GetActiveSlot()
		{
			var ae = Owner.ActiveChild;
			var count = Count();

			for (int i = 0; i < count; i++)
			{
				if (List[i] == ae)
					return i;
			}

			return -1;
		}

		public virtual bool CanAdd(Entity ent)
		{
			if (!ent.IsValid())
				return false;

			if (!ent.CanCarry(Owner))
				return false;

			return true;
		}

		public virtual bool Add(Entity ent, bool makeActive = false)
		{
			Host.AssertServer();

			if (!ent.IsValid())
				return false;

			// Can't pickup if already owned
			if (ent.Owner != null)
				return false;

			// Let the inventory reject the entity
			if (!CanAdd(ent))
				return false;

			// Let the entity reject the inventory
			if (!ent.CanCarry(Owner))
				return false;

			// 
			//if (IsCarryingType(entity.GetType()))
			//	return false;

			if (ent is IACarriable carriable && carriable.PickupTrigger != null && carriable.PickupTrigger.IsSleeping)
				return false;

			if (ent is IAWeapon weapon && weapon.TimeSinceDropped < 0.5f)
				return false;

			// we're good :)

			ent.Parent = Owner;

			// Let the item know
			ent.OnCarryStart(Owner);

			if (makeActive)
				SetActive(ent);

			return true;
		}

		

		public virtual void OnChildAdded(Entity child)
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

		public virtual void OnChildRemoved(Entity child)
		{
			if (child is IACarriable carriable)
			{
				List<Entity> list = BucketList.GetValueOrDefault(carriable.BucketIdent);
				if (list != null)
					list.Remove(child);
			}

			RecalculateFlatList();
		}

		public virtual bool Drop(Entity ent)
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

		public virtual Entity DropActive()
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

		/// <summary>
		/// Returns true if this inventory contains this entity
		/// </summary>
		


		/// <summary>
		/// Returns the active entity
		/// </summary>
		public virtual Entity Active => Owner.ActiveChild;

		/// <summary>
		/// Make this entity the active one
		/// </summary>
		public virtual bool SetActive(Entity ent)
		{
			if (Active == ent) return false;
			if (!Contains(ent)) return false;

			Owner.ActiveChild = ent;
			return true;
		}

		public virtual bool SetActiveSlot(int i, bool evenIfEmpty = false)
		{
			Entity prev = Owner.ActiveChild;
			if (prev is IAWeaponFirearm firearm && firearm.IsReloading)
			{
				firearm.TimeSinceReload = 0;
				firearm.IsReloading = false;
			}

			var ent = GetSlot(i);
			if (Owner.ActiveChild == ent)
				return false;

			if (!evenIfEmpty && ent == null)
				return false;

			Owner.ActiveChild = ent;

			if (Host.IsClient)
				InfiniteArcadeHud.Current.InventorySwitchActive(prev as IACarriable, Owner.ActiveChild as IACarriable);

			return ent.IsValid();
		}

		public virtual bool SwitchActiveSlot(int idelta, bool loop)
		{
			var count = Count();
			if (count == 0) return false;

			var slot = GetActiveSlot();
			var nextSlot = slot + idelta;

			if (loop)
			{
				while (nextSlot < 0) nextSlot += count;
				while (nextSlot >= count) nextSlot -= count;
			}
			else
			{
				if (nextSlot < 0) return false;
				if (nextSlot >= count) return false;
			}

			return SetActiveSlot(nextSlot, false);
		}

		public void RecalculateFlatList()
		{
			List.Clear();
			foreach (KeyValuePair<string, List<Entity>> kvp in BucketList)
			{
				if (kvp.Value != null)
				{
					foreach (Entity ent in kvp.Value)
						List.Add(ent);
				}
			}

			if (Host.IsClient)
				InfiniteArcadeHud.Current.InventoryFullUpdate(BucketList);
		}
	}
}
