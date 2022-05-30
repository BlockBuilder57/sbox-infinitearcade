using Sandbox;
using Sandbox.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace infinitearcade.UI
{
	public partial class InfiniteArcadeHud : RootPanel
	{
		public static InfiniteArcadeHud Current { get; set; }

		private ArcadePlayer m_player;
		private InventoryLayoutFlat m_invFlat;

		private TimeSince TimeSinceInventoryUpdate;

		public InfiniteArcadeHud()
		{
			if (!Host.IsClient)
				return;

			Current = this;
			Local.Hud = this;

			StyleSheet.Load("UI/InfiniteArcadeHud.scss");

			// s&box base things
			AddChild<ChatBox>();
			AddChild<VoiceList>();

			// custom stuff
			AddChild<PlayerStatus>();
			AddChild<WeaponStatus>();
			m_invFlat = AddChild<InventoryLayoutFlat>();
		}

		public override void Tick()
		{
			base.Tick();

			if (m_player == null)
				m_player = Local.Pawn as ArcadePlayer;
			if (!m_player.IsValid())
				return;

			m_invFlat?.SetClass("active", TimeSinceInventoryUpdate < 2f);
		}

		[Event.Hotload]
		public static void OnHotReloaded()
		{
			if (!Host.IsClient)
				return;

			InfiniteArcadeHud.Current?.Delete();
			InfiniteArcadeHud hud = new();

			if (Local.Pawn is Player player && player.Inventory is IAInventory inv)
				hud.InventoryFullUpdate(inv.List.ToArray());
		}

		public void InventoryFullUpdate(IACarriable[] inv)
		{
			m_invFlat?.FullUpdate(inv);

			TimeSinceInventoryUpdate = 0;
		}

		public void InventorySwitchActive(IACarriable newActive)
		{
			m_invFlat?.SwitchActive(newActive);

			TimeSinceInventoryUpdate = 0;
		}
	}
}
