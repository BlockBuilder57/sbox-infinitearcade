using Sandbox;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace infinitearcade
{
	public partial class GravityOnlyController : WalkController
	{
		public override void FrameSimulate()
		{
			// do nothing
		}

		public override void Simulate()
		{
			if (!Pawn.IsValid())
				return;

			//if (Time.Tick % Global.TickRate * 2 == 0)
				//Log.Info($"hey this is {(Pawn.IsServer ? "server" : "client")}");

			WishVelocity = Vector3.Zero;

			UpdateBBox();

			if (Unstuck.TestAndFix())
				return;

			if (!Swimming)
			{
				Velocity -= new Vector3(0, 0, Gravity * 0.5f) * Time.Delta;
				Velocity += new Vector3(0, 0, BaseVelocity.z) * Time.Delta;

				BaseVelocity = BaseVelocity.WithZ(0);
			}

			// Fricion is handled before we add in any base velocity. That way, if we are on a conveyor, 
			//  we don't slow when standing still, relative to the conveyor.
			bool bStartOnGround = GroundEntity != null;
			//bool bDropSound = false;
			if (bStartOnGround)
			{
				//if ( Velocity.z < FallSoundZ ) bDropSound = true;

				Velocity = Velocity.WithZ(0);
				//player->m_Local.m_flFallVelocity = 0.0f;

				if (GroundEntity != null)
				{
					ApplyFriction(GroundFriction * SurfaceFriction);
				}
			}

			bool bStayOnGround = false;
			if (Swimming)
			{
				ApplyFriction(1);
				WaterMove();
			}
			else if (GroundEntity != null)
			{
				bStayOnGround = true;
				WalkMove();
			}
			else
			{
				AirMove();
			}

			CategorizePosition(bStayOnGround);

			if (!Swimming)
			{
				Velocity -= new Vector3(0, 0, Gravity * 0.5f) * Time.Delta;
			}

			if (GroundEntity != null)
			{
				Velocity = Velocity.WithZ(0);
			}
		}
	}
}
