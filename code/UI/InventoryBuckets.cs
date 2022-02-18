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

		public void FullUpdate(IACarriable[] inv)
		{
			foreach (var item in icons.Values)
				item.ForEach(x => x.Delete());
			icons.Clear();

			foreach (IACarriable carriable in inv)
			{
				InventoryIcon icon = new(this, carriable);
				if (carriable.IsActiveChild())
					icon.AddClass("active");

				icons.AddOrCreate(carriable.Definition.BucketIdentifier).Add(icon);
			}
		}

		public void SwitchActive(IACarriable prev, IACarriable cur)
		{
			//Log.Info($"called with {prev}, {cur}");

			if (prev != null)
			{
				var list = icons.GetValueOrDefault(prev.Definition.BucketIdentifier);
				if (list != null)
					list.Find(x => x.Carriable == prev)?.RemoveClass("active");
				//else
				//	Log.Error($"{prev.BucketIdent} missing from dictionary");
			}
			if (cur != null)
			{
				var list = icons.GetValueOrDefault(cur.Definition.BucketIdentifier);
				if (list != null)
					list.Find(x => x.Carriable == cur)?.AddClass("active");
				//else
				//	Log.Error($"{cur.BucketIdent} missing from dictionary");
			}
		}
	}
}
