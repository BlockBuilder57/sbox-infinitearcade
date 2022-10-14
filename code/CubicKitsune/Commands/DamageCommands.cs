using Sandbox;

namespace CubicKitsune
{
	public class DamageCommands
	{
		[ConCmd.Server("hurtme")]
		public static void HurtMeCommand(float amount)
		{
			Client cl = ConsoleSystem.Caller;
			if (!cl.IsValid() || !cl.HasPermission("debug"))
				return;

			cl.Pawn?.TakeDamage(DamageInfo.Generic(amount));
		}
		
		[ConCmd.Server("setme")]
		public static void SetMeCommand(float health, float armor = -1, float armorPower = -1)
		{
			Client cl = ConsoleSystem.Caller;
			if (!cl.IsValid() || !cl.HasPermission("debug"))
				return;

			if (cl.Pawn.IsValid())
			{
				cl.Pawn.Health = health;
				if (cl.Pawn is CKPlayer player)
				{
					if (armor != -1)
						player.Armor = armor;
					if (armorPower != -1)
						player.ArmorPower = armorPower;
				}
			}
		}

		private static void GodModeSwitcher(CKPlayer player, CKPlayer.GodModes mode)
		{
			if (player.GodMode == mode)
				player.GodMode = CKPlayer.GodModes.Mortal;
			else
				player.GodMode = mode;
		}

		[ConCmd.Server("god")]
		public static void GodModeGodCommand(string search = "!self")
		{
			Client cl = ConsoleSystem.Caller;
			if (!cl.IsValid() || !cl.HasPermission("debug"))
				return;

			Client cl2 = cl.TryGetClient(search);

			if (cl2.IsValid() && cl2.Pawn is CKPlayer player)
				GodModeSwitcher(player, CKPlayer.GodModes.God);
		}

		[ConCmd.Server("buddha")]
		public static void GodModeBuddhaCommand(string search = "!self")
		{
			Client cl = ConsoleSystem.Caller;
			if (!cl.IsValid() || !cl.HasPermission("debug"))
				return;

			Client cl2 = cl.TryGetClient(search);

			if (cl2.IsValid() && cl2.Pawn is CKPlayer player)
				GodModeSwitcher(player, CKPlayer.GodModes.Buddha);
		}

		[ConCmd.Server("targetdummy")]
		public static void GodModeTargetDummyCommand(string search = "!self")
		{
			Client cl = ConsoleSystem.Caller;
			if (!cl.IsValid() || !cl.HasPermission("debug"))
				return;

			Client cl2 = cl.TryGetClient(search);

			if (cl2.IsValid() && cl2.Pawn is CKPlayer player)
				GodModeSwitcher(player, CKPlayer.GodModes.TargetDummy);
		}
	}
}

