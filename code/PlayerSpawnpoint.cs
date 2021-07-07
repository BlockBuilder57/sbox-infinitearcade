using Sandbox;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace infinitearcade
{
	[Library( "info_arcadeplayer_start" )]
	[Hammer.EditorModel( "models/citizen/citizen.vmdl" )]
	[Hammer.EntityTool( "Arcade Player Spawnpoint", "Player", "Defines a point where the player can (re)spawn" )]
	public class PlayerSpawnpoint : Sandbox.SpawnPoint
	{
		public enum SpawnType
		{
			ArcadePlayer,
			MachinePlayer
		}

		[Property("PlayerType", "Player Type", "What type of player will spawn here.")]
		public SpawnType PlayerType { get; set; }
	}
}
