using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CubicKitsune;
using Sandbox;
using Sandbox.UI;

namespace infinitearcade
{
	public partial class InfiniteArcadeGame : Sandbox.Game
	{
		[ConVar.Server(Saved = true)]
		public static bool ia_debug { get; set; }
		
		[ConCmd.Server("vr_reset_seated_pos")]
		public static void VRResetSeatedCommand()
		{
			Client cl = ConsoleSystem.Caller;

			if (cl?.Pawn is CKPlayer player)
			{
				player.ResetSeatedPos();
			}
		}
	}
}
