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
	[Library("infinitearcade")]
	public partial class InfiniteArcadeGame : Sandbox.Game
	{
		public InfiniteArcadeGame()
		{
			if (IsServer)
			{
				new InfiniteArcadeHud();
			}

			if (IsClient)
			{
				//clientside gubbins
			}
		}

		private int ServerSim = 0;
		private int ClientSim = 0;
		private int ClientFrameSim = 0;

		public override void Simulate(Client cl)
		{
			/*if (IsClient)
				DebugOverlay.ScreenText(Vector2.Zero, 5, Color.White, $"Cli: {ClientSim} - {ClientFrameSim}", 1 / 30f);
			else if (IsServer)
				DebugOverlay.ScreenText(Vector2.Zero, 6, Color.White, $"Srv: {ServerSim}", 1 / 30f);*/

			Entity pawn = cl.Pawn;

			if (pawn is ArcadePlayer player)
				player.Simulate(cl);

			if (IsClient)
				ClientSim++;
			else if (IsServer)
			{
				ServerSim++;
				IADebugging.ResetOffset();
			}
		}

		public override void FrameSimulate(Client cl)
		{
			Host.AssertClient();
			IADebugging.ResetOffset();

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

			var player = new ArcadePlayer();
			client.Pawn = player;

			player.Respawn();
		}

		public override void ClientDisconnect(Client cl, NetworkDisconnectionReason reason)
		{
			Log.Info($"\"{cl.Name}\" has left the game ({reason})");
			ChatBox.AddInformation(To.Everyone, $"{cl.Name} has left ({reason})", $"avatar:{cl.PlayerId}");

			foreach (ArcadeMachine machine in Entity.All.OfType<ArcadeMachine>().Where(x => x.CurrentClient == cl))
			{
				if (machine.CreatorPlayer.IsValid() && machine.CreatorPlayer is not ArcadeMachinePlayer)
					machine.CreatorPlayer.Delete();

				machine.CurrentClient = null;
			}
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
	}
}
