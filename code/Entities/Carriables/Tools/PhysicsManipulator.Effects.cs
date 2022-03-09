using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Sandbox;

namespace infinitearcade
{
	public partial class PhysicsManipulator : IATool
	{
		// effects: particles and the like
		// remember, all this is clientside!!

		Particles PhysBeam;
		Particles PhysBeamEnd;

		[Event.Frame]
		public void OnFrame()
		{
			UpdateEffects();
		}

		public void UpdateEffects()
		{
			if (Owner == null || !Input.Down(m_inputHold) || OwnerPlayer.ActiveChild != this)
			{
				EndEffects();
				return;
			}

			var tr = Trace.Ray(Owner.EyePosition, Owner.EyePosition + Owner.EyeRotation.Forward * PhysMaxDistance)
						.UseHitboxes(true)
						.Ignore(Owner, false)
						.HitLayer(CollisionLayer.Debris)
						.Run();

			if (PhysBeam == null)
				PhysBeam = Particles.Create("particles/physmanip_beam.vpcf", Position);
			if (PhysBeamEnd == null)
				PhysBeamEnd = Particles.Create("particles/physmanip_beamend.vpcf", tr.EndPosition);

			PhysBeam.SetEntityAttachment(0, EffectEntity, "muzzle");
			PhysBeamEnd.SetPosition(0, tr.StartPosition + tr.Direction * (tr.Distance - 6));

			if (HeldEntity.IsValid() && !HeldEntity.IsWorld)
			{
				if (HeldEntity.PhysicsGroup?.BodyCount > 0 && HeldGroupIndex >= 0)
				{
					var body = HeldEntity.PhysicsGroup.GetBody(HeldGroupIndex);
					if (body.IsValid())
						PhysBeam.SetPosition(1, body.Transform.PointToWorld(HeldBodyLocalPos));
				}
				else
					PhysBeam.SetEntity(1, HeldEntity, HeldBodyLocalPos);
			}
			else
				PhysBeam.SetPosition(1, tr.EndPosition);
		}

		public void EndEffects()
		{
			PhysBeam?.Destroy(true);
			PhysBeam = null;

			PhysBeamEnd?.Destroy(true);
			PhysBeamEnd = null;
		}
	}
}
