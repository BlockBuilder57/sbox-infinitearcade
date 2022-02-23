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
	public class InventoryLayoutFlat : Panel
	{
		List<InventoryIcon> icons = new();

		InventoryIcon m_curActive;

		public InventoryLayoutFlat()
		{
			StyleSheet.Load("/UI/InventoryLayoutFlat.scss");
		}

		public void FullUpdate(IACarriable[] inv)
		{
			icons.ForEach(x => x.Delete());
			icons.Clear();

			foreach (IACarriable carriable in inv)
			{
				InventoryIcon icon = new(this, carriable);
				if (carriable.Owner is Player player && player.ActiveChild == carriable)
				{
					icon.AddClass("active");
					m_curActive = icon;
				}

				icons.Add(icon);
			}
		}

		public void SwitchActive(IACarriable newActive)
		{
			if (m_curActive != null)
				m_curActive.RemoveClass("active");

			if (newActive != null)
			{
				InventoryIcon icon = icons.Find(x => x.Carriable == newActive);
				m_curActive = icon;
				if (m_curActive != null)
					m_curActive.AddClass("active");
			}
		}
	}
}
