using Sandbox;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace infinitearcade
{
	[Library( "infinitearcade" )]
	public partial class InfiniteArcadeGame : Sandbox.Game
	{
		public InfiniteArcadeGame()
		{
			if ( IsServer )
			{
				new InfiniteArcadeHud();
			}

			if ( IsClient )
			{
				//clientside gubbins
			}
		}

		public override void MoveToSpawnpoint( Entity pawn )
		{
			if ( pawn is ArcadePlayer )
			{
				ArcadePlayer player = pawn as ArcadePlayer;
				Transform spawnpoint = player.GetSpawnpoint();

				if ( spawnpoint == Transform.Zero )
				{
					Log.Warning( $"Couldn't find spawnpoint for {player}!" );
					return;
				}

				player.Position = spawnpoint.Position;
				player.Rotation = spawnpoint.Rotation;
				player.Scale = spawnpoint.Scale;
			}
		}

		public override void ClientJoined( Client client )
		{
			base.ClientJoined( client );

			var player = new ArcadePlayer();
			client.Pawn = player;

			player.Respawn();
		}
	}
}
