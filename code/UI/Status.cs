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
	public class Status : Panel
	{
		public Label health;
		public Label pawnstatus;

		public Status()
		{
			health = Add.Label("100", "health");
			pawnstatus = Add.Label("", "pawnstatus");
		}

		public override void Tick()
		{
			var player = Local.Pawn;
			if (player == null) return;

			health.Text = $"{player.Health.CeilToInt()}";

			if (player is ArcadeMachinePlayer)
				pawnstatus.Text = "YOU'RE IN AN ARCADE MACHINE";
			else
				pawnstatus.Text = "";
		}
	}
}
