using infinitearcade.UI;
using Sandbox;
using Sandbox.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace infinitearcade
{
	public partial class InfiniteArcadeGame : Sandbox.Game
	{
		public InfiniteArcadeGame()
		{
			// dunno why this isn't default, but
			Global.TickRate = 66;

			if (IsServer)
			{
				// serverside gubbins
			}

			if (IsClient)
			{
				// clientside gubbins
				Local.Hud = new InfiniteArcadeHud();
			}
		}

		public override void Simulate(Client cl)
		{
			Entity pawn = cl.Pawn;

			if (pawn is ArcadePlayer player)
				player.Simulate(cl);

			if (IsServer)
				IADebugging.ResetLineOffset();

			IADebugging.Simulate(cl);
		}

		public override void FrameSimulate(Client cl)
		{
			Host.AssertClient();
			IADebugging.ResetLineOffset();

			Entity pawn = cl.Pawn;

			if (pawn is ArcadePlayer player)
				player.FrameSimulate(cl);

			IADebugging.FrameSimulate(cl);
		}

		public override void MoveToSpawnpoint(Entity pawn)
		{
			if (pawn is ArcadePlayer)
			{
				ArcadePlayer player = pawn as ArcadePlayer;
				Transform spawnpoint = player.GetSpawnpoint();

				if (spawnpoint == Transform.Zero)
				{
					Log.Warning($"Couldn't find spawnpoint for {player}!");
					return;
				}

				player.Position = spawnpoint.Position;
				player.Rotation = spawnpoint.Rotation;
				player.Scale = spawnpoint.Scale;
			}
		}

		public override void ClientJoined(Client client)
		{
			base.ClientJoined(client);

			var player = new ArcadePlayer(client);
			client.Pawn = player;

			player.Respawn();
		}

		public override void ClientDisconnect(Client cl, NetworkDisconnectionReason reason)
		{
			foreach (ArcadeMachine machine in Entity.All.OfType<ArcadeMachine>().Where(x => x.CurrentClient == cl))
			{
				if (machine.CreatorPlayer.IsValid() && machine.CreatorPlayer is not ArcadeMachinePlayer)
					machine.CreatorPlayer.Delete();

				machine.CurrentClient = null;
			}

			base.ClientDisconnect(cl, reason);
		}

		public override void DoPlayerSuicide(Client cl)
		{
			ArcadePlayer player = cl.Pawn as ArcadePlayer;

			if (player != null && player.LifeState == LifeState.Alive)
			{
				float damage = player.Health + (player.Armor * player.ArmorMultiplier);
				player.TakeDamage(DamageInfo.Generic(damage * 100f));
			}
		}

		public override void DoPlayerDevCam(Client player)
		{
			Host.AssertServer();

			if (!player.HasPermission("devcam"))
				return;

			player.DevCamera = player.DevCamera == null ? new DevCamera() : null;
		}
	}
}
