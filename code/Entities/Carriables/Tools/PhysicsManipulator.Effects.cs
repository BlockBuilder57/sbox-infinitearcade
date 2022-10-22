using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using CubicKitsune;
using Sandbox;
using Sandbox.Component;

namespace infinitearcade
{
	public partial class PhysicsManipulator : CKTool
	{
		// effects: particles and the like
		// remember, all this is clientside!!

		[ConVar.Client(null, Help = "Controls the color of the physics manipulator's beam color", Saved = true)]
		private static string ia_physmanip_beam_color { get; set; } = "white";
		[ConVar.Client(null, Help = "Controls the color of the physics manipulator's beam color behind an object", Saved = true)]
		private static string ia_physmanip_beam_color_obscured { get; set; } = "transparent";
		[ConVar.Client(null, Help = "Controls the color of the physics manipulator's beam color behind an object", Min = 0, Saved = true)]
		private static float ia_physmanip_beam_width { get; set; } = 0.3f;

		private static Color m_physColor =>
			Color.Parse(ia_physmanip_beam_color.Replace("@", ((Time.Now * 100) % 360f)
				.ToString(CultureInfo.InvariantCulture))).GetValueOrDefault();
		
		private static Color m_physColorObscured =>
			Color.Parse(ia_physmanip_beam_color_obscured.Replace("@", ((Time.Now * 100) % 360f)
				.ToString(CultureInfo.InvariantCulture))).GetValueOrDefault();

		Particles FxPhysBeam;
		Particles FxPhysBeamEnd;

		//Particles FxGravPull;
		//Particles FxGravBeam;

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
			if (Owner == null || Owner != Local.Pawn || OwnerPlayer == null || OwnerPlayer.ActiveChild != this)
			{
				EndEffects();
				return;
			}

			var tr = Trace.Ray(Owner.EyePosition, Owner.EyePosition + Owner.EyeRotation.Forward * PhysMaxDistance)
						.UseHitboxes(true)
						.Ignore(Owner, false)
						.WithAnyTags("solid", "debris")
						.Run();

			if (FxPhysBeam == null)
				FxPhysBeam = Particles.Create("particles/physmanip_beam.vpcf", Position);
			if (FxPhysBeamEnd == null)
				FxPhysBeamEnd = Particles.Create("particles/physmanip_beamend.vpcf", tr.EndPosition);

			FxPhysBeam.SetEntityAttachment(0, EffectEntity, "muzzle");
			FxPhysBeam.SetPosition(2, m_physColor);
			FxPhysBeamEnd.SetPosition(1, m_physColor);

			if (HeldEntity.IsValid() && !HeldEntity.IsWorld)
			{
				if (HeldEntity is ModelEntity modelEnt)
				{
					Glow glow = modelEnt.Components.GetOrCreate<Glow>();
					glow.Enabled = true;
					glow.Width = ia_physmanip_beam_width;
					glow.Color = m_physColor;
					glow.ObscuredColor = m_physColorObscured;
				}
				
				

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
		
		[ClientRpc]
		public void EndEffects()
		{
			FxPhysBeam?.Destroy(true);
			FxPhysBeam = null;

			FxPhysBeamEnd?.Destroy(true);
			FxPhysBeamEnd = null;
			
			if (HeldEntity?.Components.Get<Glow>() != null)
				HeldEntity.Components.Get<Glow>().Enabled = false;
			if (PrevHeldEntity?.Components.Get<Glow>() != null)
				PrevHeldEntity.Components.Get<Glow>().Enabled = false;
		}
	}
}
