using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Sandbox;
using Sandbox.UI;
using Sandbox.UI.Construct;

namespace infinitearcade.UI
{
	public class InventoryBuckets : Panel
	{
		Dictionary<string, List<InventoryIcon>> icons = new();

		public InventoryBuckets()
		{
			StyleSheet.Load("/UI/InventoryBuckets.scss");
		}

		public void FullUpdate(Dictionary<string, List<Entity>> invBuckets)
		{
			foreach (var item in icons.Values)
			{
				item.ForEach(x => x.Delete());
			}

			icons.Clear();

			foreach (var key in invBuckets.Keys)
				icons.Add(key, new());

			foreach (var value in invBuckets.Values)
				foreach (Entity entity in value)
					if (entity is IACarriable carriable)
						icons[carriable.BucketIdent].Add(new InventoryIcon(this, carriable));
		}

		public void SwitchActive(IACarriable prev, IACarriable cur)
		{
			Log.Info($"called with {prev}, {cur}");

			if (prev != null)
			{
				var thing = icons.GetValueOrDefault(prev.BucketIdent);
				if (thing != null)
				{
					var elem = thing.Find(x => x.Carriable == prev);
					if (elem != null)
						elem.RemoveClass("active");
					else
						Log.Error($"element for {prev} not found");
				}
				else
					Log.Error($"{prev.BucketIdent} missing from dictionary");
			}
			if (cur != null)
			{
				var thing = icons.GetValueOrDefault(cur.BucketIdent);
				if (thing != null)
				{
					var elem = thing.Find(x => x.Carriable == cur);
					if (elem != null)
						elem.AddClass("active");
					else
						Log.Error($"element for {cur} not found");
				}
				else
					Log.Error($"{cur.BucketIdent} missing from dictionary");
			}
		}
	}
}
