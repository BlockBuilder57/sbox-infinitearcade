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
		private InventoryLayoutFlat m_invFlat;

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
			m_invFlat = AddChild<InventoryLayoutFlat>();
		}

		public override void Tick()
		{
			base.Tick();

			if (m_player == null)
				m_player = Local.Pawn as ArcadePlayer;
			if (!m_player.IsValid())
				return;

			m_crosshairCanvas?.SetClass("hidden", m_player.CameraMode is not FirstPersonCamera);
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
			if (m_invFlat != null)
				m_invFlat.FullUpdate(inv);
		}

		public void InventorySwitchActive(IACarriable newActive)
		{
			if (m_invFlat != null)
				m_invFlat.SwitchActive(newActive);
		}
	}
}
