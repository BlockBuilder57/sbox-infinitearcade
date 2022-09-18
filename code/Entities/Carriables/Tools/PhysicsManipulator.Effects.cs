using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CubicKitsune;
using Sandbox;

namespace infinitearcade
{
	public partial class PhysicsManipulator : CKTool
	{
		// effects: particles and the like
		// remember, all this is clientside!!

		Particles FxPhysBeam;
		Particles FxPhysBeamEnd;

		Particles FxGravPull;
		Particles FxGravBeam;

		private bool m_fxStarted = false;

		[Event.Frame]
		public void OnFrame()
		{
			UpdateEffects();
		}

		public void UpdateEffects()
		{
			if ((Mode == ManipulationMode.None && Input.Down(m_inputHold) && !m_stickyHold) || Mode == ManipulationMode.Phys)
				UpdatePhysEffects();
			else if (Mode == ManipulationMode.Grav)
				UpdateGravEffects();
			else
				EndEffects();
		}

		public void UpdatePhysEffects()
		{
			if (Owner == null || OwnerPlayer == null || OwnerPlayer.ActiveChild != this)
			{
				EndEffects();
				return;
			}

			var tr = Trace.Ray(Owner.EyePosition, Owner.EyePosition + Owner.EyeRotation.Forward * PhysMaxDistance)
						.UseHitboxes(true)
						.Ignore(Owner, false)
						.HitLayer(CollisionLayer.Debris)
						.Run();

			if (FxPhysBeam == null)
				FxPhysBeam = Particles.Create("particles/physmanip_beam.vpcf", Position);
			if (FxPhysBeamEnd == null)
				FxPhysBeamEnd = Particles.Create("particles/physmanip_beamend.vpcf", tr.EndPosition);

			m_fxStarted = true;

			FxPhysBeam.SetEntityAttachment(0, EffectEntity, "muzzle");


			if (HeldEntity.IsValid() && !HeldEntity.IsWorld)
			{
				if (HeldEntity.PhysicsGroup?.BodyCount > 0 && HeldGroupIndex >= 0)
				{
					var body = HeldEntity.PhysicsGroup.GetBody(HeldGroupIndex);
					if (body.IsValid())
					{
						FxPhysBeam.SetPosition(1, body.Transform.PointToWorld(HeldBodyLocalPos));
						FxPhysBeamEnd.SetPosition(0, body.Transform.PointToWorld(HeldBodyLocalPos));
					}
				}
				else
				{ 
					FxPhysBeam.SetEntity(1, HeldEntity, HeldBodyLocalPos);
					FxPhysBeamEnd.SetEntity(0, HeldEntity, HeldBodyLocalPos);
				}
			}
			else
			{
				FxPhysBeam.SetPosition(1, tr.EndPosition);
				FxPhysBeamEnd.SetPosition(0, tr.StartPosition + tr.Direction * (tr.Distance - 6));
			}
		}

		public void UpdateGravEffects()
		{

		}

		public void EndEffects()
		{
			if (!m_fxStarted)
				return;

			FxPhysBeam?.Destroy(true);
			FxPhysBeam = null;

			FxPhysBeamEnd?.Destroy(true);
			FxPhysBeamEnd = null;

			m_fxStarted = false;
		}
	}
}
