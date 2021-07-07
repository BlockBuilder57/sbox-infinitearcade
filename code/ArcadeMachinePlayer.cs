using Sandbox;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace infinitearcade
{
	public partial class ArcadeMachinePlayer : ArcadePlayer
	{
		public ArcadeMachine ParentMachine { get; set; }

		public override void Respawn()
		{
			base.Respawn();
		}

		public override Transform GetSpawnpoint()
		{
			PlayerSpawnpoint spawnpoint = Entity.All.OfType<PlayerSpawnpoint>().Where( x => x.PlayerType == PlayerSpawnpoint.SpawnType.MachinePlayer && x.EntityName == ParentMachine.SpawnpointName ).OrderBy( x => Guid.NewGuid() ).FirstOrDefault();
			Transform transform = Transform.Zero;
			
			if ( spawnpoint != null )
			{
				return spawnpoint.Transform;
			}
			else if (ParentMachine != null)
			{
				transform = ParentMachine.Transform;
				transform.Position = ParentMachine.Position + ParentMachine.Rotation.Right * 32;
				transform.Scale = ParentMachine.Scale;
			}
			
			return transform;
		}

		protected override void UseFail()
		{
			base.UseFail();

			if ( ParentMachine != null )
			{
				ParentMachine.ExitMachine();
				return;
			}
		}
	}
}
