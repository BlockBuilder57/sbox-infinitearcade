using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Sandbox;

namespace infinitearcade
{
	[Library("tool_flashlight", Title = "Flashlight", Spawnable = true)]
	public partial class Flashlight : IATool
	{
		public override string WorldModelPath => "weapons/rust_pistol/rust_pistol.vmdl";
		public override string ViewModelPath => "weapons/rust_flashlight/v_rust_flashlight.vmdl";

		private bool m_on = true;

		// these are separate for a few reasons:
		// 1. spawn and createviewmodel are server and clientside respectfully
		// 2. to achieve the HL-esque flashlight coming from the face locally but from the flashlight globally
		// 3. it's what facepunch did lol
		private SpotLightEntity m_spotlightWorld;
		private SpotLightEntity m_spotlightLocal;

		private Vector3 m_spotlightWorldOffset = (Vector3.Forward * 10) + (Vector3.Up * 5);

		public Flashlight()
		{
			BucketIdent = "tool";
		}

		public override void Spawn()
		{
			base.Spawn();

			m_spotlightWorld = CreateSpotlight();
			m_spotlightWorld.SetParent(this, "slide", new Transform(m_spotlightWorldOffset));
			m_spotlightWorld.EnableHideInFirstPerson = true;
			m_spotlightWorld.Enabled = m_on;
		}

		public override void CreateViewModel()
		{
			base.CreateViewModel();

			m_spotlightLocal = CreateSpotlight();
			m_spotlightLocal.SetParent(ViewModelEntity, "light", Transform.Zero);
			m_spotlightLocal.EnableViewmodelRendering = true;
			m_spotlightLocal.Enabled = m_on;
		}

		public override void Simulate(Client cl)
		{
			base.Simulate(cl);

			if (Input.Pressed(InputButton.Attack1))
			{
				m_on = !m_on;

				PlaySound(m_on ? "flashlight-on" : "flashlight-off");

				if (m_spotlightWorld.IsValid())
					m_spotlightWorld.Enabled = m_on;
				if (m_spotlightLocal.IsValid())
					m_spotlightLocal.Enabled = m_on;
			}
		}

		private SpotLightEntity CreateSpotlight()
		{
			return new SpotLightEntity
			{
				Enabled = true,
				DynamicShadows = true,
				Range = 512,
				Falloff = 1.0f,
				LinearAttenuation = 0.0f,
				QuadraticAttenuation = 1.0f,
				Brightness = 1.5f,
				Color = Color.White,
				InnerConeAngle = 20,
				OuterConeAngle = 40,
				FogStength = 1.0f,
				Owner = Owner,
				LightCookie = Texture.Load("materials/effects/lightcookie.vtex")
			};
		}

		public override void SimulateAnimator(PawnAnimator anim)
		{
			anim.SetParam("holdtype", 4);
			anim.SetParam("holdtype_handedness", 1);
			anim.SetParam("holdtype_pose_hand", 0.14f);
			anim.SetParam("aimat_weight", 1.0f);
		}
	}
}
