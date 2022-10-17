using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Sandbox;

namespace infinitearcade
{
	public class NavSteer
	{
		public Entity ToFollow;

		private Vector3 m_targetPos;

		public Vector3 Target
		{
			get
			{
				if (ToFollow.IsValid() && !ToFollow.IsWorld)
					return ToFollow.Position;
				else
					return m_targetPos;
			}
			set => m_targetPos = value;
		}

		public NavDriver Driver { get; set; }

		public NavSteer()
		{
			Driver = new NavDriver();
		}

		public virtual NavSteerOutput Tick( Vector3 currentPosition )
		{
			Driver.Update( currentPosition, Target );

			NavSteerOutput output;
				
			output.Finished = Driver.IsEmpty;

			if ( output.Finished )
			{
				output.Direction = Vector3.Zero;
				return output;
			}

			//using ( Sandbox.Debug.Profile.Scope( "Update Direction" ) )
			{
				output.Direction = Driver.GetDirection( currentPosition );
			}

			var avoid = GetAvoidance( currentPosition, output.Direction, 500 );
			if ( !avoid.IsNearlyZero() )
			{
				output.Direction = (output.Direction + avoid).Normal;
			}

			return output;
		}

		Vector3 GetAvoidance( Vector3 position, Vector3 direction, float radius )
		{
			var center = position + direction * radius * 0.5f;

			var objectRadius = 20.0f;
			Vector3 avoidance = default;

			foreach ( var ent in Entity.FindInSphere( center, radius ) )
			{
				if ( ent is not Player ) continue;
				if ( ent.IsWorld ) continue;

				var delta = (position - ent.Position).WithZ( 0 );
				var closeness = delta.Length;
				if ( closeness < 0.001f ) continue;
				var thrust = ((objectRadius - closeness) / objectRadius).Clamp( 0, 1 );
				if ( thrust <= 0 ) continue;

				//avoidance += delta.Cross( Output.Direction ).Normal * thrust * 2.5f;
				avoidance += delta.Normal * thrust * thrust;
			}

			return avoidance;
		}
		
		public struct NavSteerOutput
		{
			public bool Finished;
			public Vector3 Direction;
		}
	}
}
