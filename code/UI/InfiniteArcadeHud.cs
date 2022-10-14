using CubicKitsune;
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

		public CKPlayer Player { get; private set; }

		private PlayerStatus m_playerStatus;
		private WeaponStatus m_weaponStatus;
		private TargetStatus m_targetStatus;
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

			OnNewPawn();
		}

		public void OnNewPawn()
		{
			m_playerStatus?.Delete();
			m_weaponStatus?.Delete();
			m_targetStatus?.Delete();
			m_invFlat?.Delete();

			m_playerStatus = AddChild<PlayerStatus>();
			m_weaponStatus = AddChild<WeaponStatus>();
			m_targetStatus = AddChild<TargetStatus>();
			m_targetStatus.AddClass("hidden");
			m_invFlat = AddChild<InventoryLayoutFlat>();
		}

		public override void Tick()
		{
			base.Tick();

			if (Player == null)
				Player = Local.Pawn as CKPlayer;
			if (!Player.IsValid())
				return;

			m_invFlat?.SetClass("active", TimeSinceInventoryUpdate < 2f);
		}

		[Event.Hotload]
		public static void OnHotReloaded()
		{
			if (!Host.IsClient)
				return;

			Current?.Delete();
			InfiniteArcadeHud hud = new();

			if (Local.Pawn is Player player && player.Inventory is CKInventory inv)
				hud.InventoryFullUpdate(inv.List.ToArray());
		}

		public void InventoryFullUpdate(CKCarriable[] inv)
		{
			m_invFlat?.FullUpdate(inv);

			TimeSinceInventoryUpdate = 0;
		}

		public void InventorySwitchActive(CKCarriable newActive)
		{
			m_invFlat?.SwitchActive(newActive);

			TimeSinceInventoryUpdate = 0;
		}

		public void EnableTargetStatus()
		{
			m_targetStatus.RemoveClass("hidden");
		}

		public void DisableTargetStatus()
		{
			m_targetStatus.AddClass("hidden");
		}
	}
}
