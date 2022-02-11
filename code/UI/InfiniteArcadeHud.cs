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
		private CrosshairCanvas m_crosshairCanvas;
		private InventoryBuckets m_buckets;

		public InfiniteArcadeHud()
		{
			if (!Host.IsClient)
				return;

			Current = this;
			Local.Hud = this;

			StyleSheet.Load("UI/InfiniteArcadeHud.scss");

			// s&box base things
			m_crosshairCanvas = AddChild<CrosshairCanvas>();
			CrosshairCanvas.SetCrosshair(new StandardCrosshair());
			AddChild<ChatBox>();
			AddChild<VoiceList>();
			AddChild<KillFeed>();

			// custom stuff
			AddChild<PlayerStatus>();
			AddChild<WeaponStatus>();
			m_buckets = AddChild<InventoryBuckets>();
		}

		public override void Tick()
		{
			base.Tick();

			if (m_player == null)
				m_player = Local.Pawn as ArcadePlayer;

			m_crosshairCanvas?.SetClass("hidden", m_player.Camera is not FirstPersonCamera);
		}

		[Event.Hotload]
		public static void OnHotReloaded()
		{
			if (!Host.IsClient)
				return;

			InfiniteArcadeHud.Current?.Delete();
			InfiniteArcadeHud hud = new();

			if (Local.Pawn is ArcadePlayer player && player.Inventory is IAInventory inv)
			{
				hud.InventoryFullUpdate(inv);
			}
		}

		public void InventoryFullUpdate(IAInventory inv)
		{
			if (m_buckets != null)
				m_buckets.FullUpdate(inv);
		}

		public void InventorySwitchActive(IACarriable prev, IACarriable cur)
		{
			if (m_buckets != null)
				m_buckets.SwitchActive(prev, cur);
		}
	}
}
