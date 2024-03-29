﻿using Sandbox;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using infinitearcade.UI;

namespace CubicKitsune
{
	public partial class CKInventory : IBaseInventory
	{
		[ConVar.ClientData] public static bool ck_inventory_flat_deploy_toggle { get; set; } = true;

		public CKPlayer Owner { get; init; }
		public List<CKCarriable> List = new List<CKCarriable>();

		public enum BucketTypes
		{
			FlatUnordered, // eg fp's sandbox
			FlatOrdered,
			Bucketed // eg Source games, ULTRAKILL
		}

		public BucketTypes BucketType;

		public virtual Entity Active => Owner.ActiveChild;
		public virtual int Count() => List.Count;
		public virtual bool Contains(Entity ent) => List.Contains(ent);
		public bool IsCarryingType(Type t) => List.Any(x => x?.GetType() == t);

		public CKInventory(CKPlayer owner)
		{
			Owner = owner;
			BucketType = BucketTypes.FlatOrdered;
		}

		[ConCmd.Server("inv_clear")]
		public static void ClearCommand()
		{
			Client cl = ConsoleSystem.Caller;
			if (!cl.HasPermission("debug"))
				return;

			if (cl?.Pawn is CKPlayer player)
			{
				player.Inventory?.DeleteContents();
			}
		}

		public virtual void DeleteContents()
		{
			foreach (var item in List.ToArray())
				item.Delete();

			List.Clear();
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

			for (int i = 0; i < Count(); i++)
				if (List[i] == ae)
					return i;

			return -1;
		}

		public virtual bool CanAdd(Entity ent)
		{
			if (!ent.IsValid())
				return false;

			// if we do not have this A LOT of things break
			// mostly because OnTouch would add literally everything
			if (ent is not BaseCarriable)
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

			// 
			//if (IsCarryingType(entity.GetType()))
			//	return false;

			if (ent is CKCarriable carriable && carriable.PickupTrigger != null && carriable.TimeSinceDropped < 0.5f)
				return false;

			// we're good :)

			ent.SetParent(Owner);

			// Let the item know
			if (ent is BaseCarriable carry)
				carry.OnCarryStart(Owner);

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

			if (child is CKCarriable carriable)
				List?.Add(carriable);

			ListReorder();
		}

		public virtual void OnChildRemoved(Entity child)
		{
			if (child is CKCarriable carriable)
				List?.Remove(carriable);

			ListReorder();
		}

		public virtual bool Drop(Entity ent)
		{
			if (!Host.IsServer)
				return false;

			if (!Contains(ent))
				return false;

			if (ent is CKWeaponFirearm firearm && firearm.IsReloading)
				return false;

			ent.SetParent(null);
			if (ent is BaseCarriable carry)
				carry.OnCarryDrop(Owner);

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
			if (prev is CKWeaponFirearm firearm && firearm.IsReloading)
			{
				firearm.TimeSinceReload = 0;
				firearm.IsReloading = false;
			}

			var ent = GetSlot(i);

			if (Owner.ActiveChild == ent)
			{
				// press the slot number again to remove as active
				if (evenIfEmpty && BucketType != BucketTypes.Bucketed && Owner.Client.GetClientData<bool>(nameof(ck_inventory_flat_deploy_toggle)))
				{
					ent = null;
					InfiniteArcadeHud.InventorySwitchActive(To.Single(Owner), null);
				}
				else
					return false;
			}

			if (!evenIfEmpty && ent == null)
				return false;
		
			Owner.ActiveChild = ent;

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

		public void ListReorder()
		{
			if (BucketType != BucketTypes.FlatUnordered)
				List = List.OrderBy(x => x.Bucket).ThenBy(x => x.SubBucket).ThenBy(x => x.NetworkIdent).ToList();
			
			InfiniteArcadeHud.InventoryFullUpdate(To.Single(Owner), List.ToArray());
		}
	}
}
