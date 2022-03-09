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

		Particles FxPhysBeam;
		Particles FxPhysBeamEnd;

		Particles FxGravPull;
		Particles FxGravBeam;

		[Event.Frame]
		public void OnFrame()
		{
			UpdateEffects();
		}

		public void UpdateEffects()
		{
			switch (Mode)
			{
				case ManipulationMode.Phys:
					UpdatePhysEffects();
					break;
				case ManipulationMode.Grav:
					UpdateGravEffects();
					break;
				default:
					EndEffects();
					break;
			}
		}

		public void UpdatePhysEffects()
		{
			if (Owner == null || OwnerPlayer.ActiveChild != this)
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

			FxPhysBeam.SetEntityAttachment(0, EffectEntity, "muzzle");
			FxPhysBeamEnd.SetPosition(0, tr.StartPosition + tr.Direction * (tr.Distance - 6));

			if (HeldEntity.IsValid() && !HeldEntity.IsWorld)
			{
				if (HeldEntity.PhysicsGroup?.BodyCount > 0 && HeldGroupIndex >= 0)
				{
					var body = HeldEntity.PhysicsGroup.GetBody(HeldGroupIndex);
					if (body.IsValid())
						FxPhysBeam.SetPosition(1, body.Transform.PointToWorld(HeldBodyLocalPos));
				}
				else
					FxPhysBeam.SetEntity(1, HeldEntity, HeldBodyLocalPos);
			}
			else
				FxPhysBeam.SetPosition(1, tr.EndPosition);
		}

		public void UpdateGravEffects()
		{

		}

		public void EndEffects()
		{
			FxPhysBeam?.Destroy(true);
			FxPhysBeam = null;

			FxPhysBeamEnd?.Destroy(true);
			FxPhysBeamEnd = null;
		}
	}
}
