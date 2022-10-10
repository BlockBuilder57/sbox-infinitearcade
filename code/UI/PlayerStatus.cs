using Sandbox;
using Sandbox.UI;
using Sandbox.UI.Construct;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace infinitearcade.UI
{
	public class PlayerStatus : Panel
	{
		public Label pawnstatus;
		private HealthDisplay m_healthDisplay;

		public PlayerStatus()
		{
			StyleSheet.Load("/ui/PlayerStatus.scss");

			pawnstatus = Add.Label("YOU'RE IN AN ARCADE MACHINE (press use to leave)", "pawnstatus");

			m_healthDisplay = new HealthDisplay(Local.Pawn);
			m_healthDisplay.Parent = this;
		}

		public override void Tick()
		{
			base.Tick();

			ArcadePlayer player = Local.Pawn as ArcadePlayer;
			if (player == null) return;

			m_healthDisplay.Ent = player;

			SetClass("hidden", player.LifeState == LifeState.Dead);
			if (player.LifeState == LifeState.Dead)
				return;

			pawnstatus?.SetClass("hidden", !(player is ArcadeMachinePlayer));
		}
	}
}
