﻿using Sandbox;
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
		[ConVar.ClientData] public static bool ia_inventory_flat_deploy_toggle { get; set; } = true;

		public ArcadePlayer Owner { get; init; }
		public List<IACarriable> List { get; set; } = new List<IACarriable>();
		public Dictionary<string, List<IACarriable>> BucketList { get; set; } = new();

		public enum Bucket
		{
			Flat, // eg fp's sandbox
			Bucketed // eg Source games, ULTRAKILL
		}
		public Bucket BucketType = Bucket.Flat;

		private int m_cachedSlot = -1;

		public virtual Entity Active => Owner.ActiveChild;
		public virtual int Count() => List.Count;
		public virtual bool Contains(Entity ent) => List.Contains(ent);
		public bool IsCarryingType(Type t) => List.Any(x => x?.GetType() == t);

		public IAInventory(ArcadePlayer owner)
		{
			Host.AssertServer();

			Owner = owner;

			DeleteContents();
		}

		[ServerCmd("inv_clear")]
		public static void ClearCommand()
		{
			Client cl = ConsoleSystem.Caller;
			if (!cl.HasPermission("debug"))
				return;

			if (cl?.Pawn is ArcadePlayer player)
			{
				player.Inventory.DeleteContents();
			}
		}

		public virtual void DeleteContents()
		{
			foreach (var item in List.ToArray())
				item.Delete();

			List.Clear();
			BucketList.Clear();
			BucketList = new()
			{
				{ "primary", new() },
				{ "secondary", new() },
				{ "tool", new() },
				{ "none", new() }
			};
		}

		public virtual Entity GetSlot(int i)
		{
			if (List.Count <= i) return null;
			if (i < 0) return null;

			return List[i];
		}

		public virtual int GetActiveSlot()
		{
			if (m_cachedSlot >= 0)
				return m_cachedSlot;

			var ae = Owner.ActiveChild;

			for (int i = 0; i < Count(); i++)
			{
				if (List[i] == ae)
				{
					m_cachedSlot = i;
					return i;
				}
			}

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

			if (ent is IACarriable carriable && carriable.PickupTrigger != null && carriable.PickupTrigger.IsSleeping
				&& carriable.TimeSinceDropped < 0.5f)
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

			if (child is IACarriable carriable)
				List?.Add(carriable);

			BucketListFullUpdate();
		}

		public virtual void OnChildRemoved(Entity child)
		{
			if (child is IACarriable carriable)
				List?.Remove(carriable);

			BucketListFullUpdate();
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
				using (Prediction.Off())
					Owner.ActiveChild = null;
				return ac;
			}

			return null;
		}

		public virtual bool SetActive(Entity ent)
		{
			if (Active == ent) return false;
			if (!Contains(ent)) return false;

			Entity prev = Owner.ActiveChild;
			using (Prediction.Off())
				Owner.ActiveChild = ent;

			Owner.HudSwitchActive(To.Single(Owner), prev as IACarriable, Owner.ActiveChild as IACarriable);

			m_cachedSlot = -1;

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
			{
				// press the slot number again to remove as active
				if (evenIfEmpty && BucketType == Bucket.Flat && Owner.Client.GetClientData<bool>(nameof(ia_inventory_flat_deploy_toggle)))
					ent = null;
				else
					return false;
			}

			if (!evenIfEmpty && ent == null)
				return false;

			using (Prediction.Off())
				Owner.ActiveChild = ent;

			Owner.HudSwitchActive(To.Single(Owner), prev as IACarriable, Owner.ActiveChild as IACarriable);

			m_cachedSlot = -1;

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

		public void BucketListFullUpdate()
		{
			//Log.Info($"BucketListFullUpdate called from {(Host.IsClient ? "CLIENT" : "SERVER")}");
			// dirty function, please replace me

			//BucketList.Clear();
			foreach (var list in BucketList.Values)
				list.Clear();

			foreach (IACarriable carriable in List)
				BucketList.AddOrCreate(carriable.Definition.BucketIdentifier).Add(carriable);

			List.Clear();

			foreach (var list in BucketList.Values)
				foreach (var carriable in list)
					List.Add(carriable);

			Owner.HudFullUpdate(To.Single(Owner), List.ToArray());
		}
	}
}
