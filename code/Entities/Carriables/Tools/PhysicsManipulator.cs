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
		private PhysicsBody m_grabBody;
		private FixedJoint m_grabJoint;

		private PhysicsBody m_heldBody;
		private Rotation m_heldRot;

		private float m_holdDistance;
		private ArcadePlayer m_owner;
		private bool m_holding;
		private bool m_stickyHold;

		// Phys inputs
		private const InputButton m_inputHold = InputButton.Attack1;
		private const InputButton m_inputFreeze = InputButton.Attack2;
		private const InputButton m_inputGravity = InputButton.Reload;
		private const InputButton m_inputSlow = InputButton.Walk;
		private const InputButton m_inputRotate = InputButton.Use;
		private const InputButton m_inputRotationSnap = InputButton.Use | InputButton.Run;

		// Grav inputs
		private const InputButton m_inputPunt = InputButton.Attack1;
		private const InputButton m_inputPull = InputButton.Attack2;

		// Shared settings
		[Net] public float LinearFrequency { get; set; } = 20.0f;
		[Net] public float LinearDampingRatio { get; set; } = 1.0f;
		[Net] public float AngularFrequency { get; set; } = 20.0f;
		[Net] public float AngularDampingRatio { get; set; } = 1.0f;

		// Phys settings
		[Net] public float DistanceTargetDeltaSpeed { get; set; } = 20f;
		[Net] public float DistanceTargetDeltaFadeDist { get; set; } = 128;
		[Net] public float DistanceTargetDeltaFadeMult { get; set; } = 0.7f;
		[Net] public float RotationSpeed { get; set; } = 0.25f;
		[Net] public float RotationSpeedSlowMult { get; set; } = 0.25f;
		[Net] public float RotationSnapAngle { get; set; } = 45f;

		public bool Holding => HeldEntity.IsValid();
		[Net] public Entity HeldEntity { get; private set; }

		public override void Simulate(Client cl)
		{
			if (Owner is not ArcadePlayer owner)
				return;
			m_owner = owner;

			if (IsServer)
			{
				Vector3 eyePos = m_owner.EyePosition;
				Vector3 eyeDir = m_owner.EyeRotation.Forward;
				Rotation eyeRot = Rotation.From(new Angles(0.0f, m_owner.EyeRotation.Angles().yaw, 0.0f));

				using (Prediction.Off())
				{
					if (Input.Down(m_inputHold))
					{
						if (m_heldBody.IsValid())
							UpdatePhysicsHold(eyePos, eyeDir, eyeRot);
						else
							StartPhysicsHold(eyePos, eyeDir, eyeRot);
					}
					else
						EndPhysicsHold();
				}
			}

			if (m_stickyHold && Input.Released(m_inputHold))
				m_stickyHold = false;

			// don't let the inventory scroll out of this
			if (Holding)
				Input.MouseWheel = 0;
		}

		public override void ActiveStart(Entity ent)
		{
			base.ActiveStart(ent);
			EnableManipulator();
		}

		public override void ActiveEnd(Entity ent, bool dropped)
		{
			base.ActiveEnd(ent, dropped);
			DisableManipulator();
		}

		protected override void OnDestroy()
		{
			base.OnDestroy();
			DisableManipulator();
		}

		public void EnableManipulator()
		{
			if (!Host.IsServer)
				return;

			if (!m_grabBody.IsValid())
			{
				m_grabBody = new PhysicsBody(Map.Physics)
				{
					BodyType = PhysicsBodyType.Keyframed
				};
			}
		}

		public void DisableManipulator()
		{
			EndPhysicsHold();

			m_grabBody?.Remove();
			m_grabBody = null;
		}

		public void StartPhysicsHold(Vector3 eyePos, Vector3 eyeDir, Rotation eyeRot)
		{
			var tr = Trace.Ray(eyePos, eyePos + eyeDir * short.MaxValue)
						.UseHitboxes(true)
						.Ignore(m_owner, false)
						.HitLayer(CollisionLayer.Debris)
						.Run();

			if (!tr.Hit || !tr.Entity.IsValid() || tr.Entity.IsWorld)
				return;

			// after freezing, we want to make sure the hold button gets repressed
			if (m_stickyHold)
				return;

			Entity rootEnt = tr.Entity.Root;
			PhysicsBody body = tr.Body;

			if (!body.IsValid())
				return;

			// don't move keyframed (animated/code controlled) bodies
			if (body.BodyType == PhysicsBodyType.Keyframed)
				return;

			if (body.BodyType == PhysicsBodyType.Static)
				body.BodyType = PhysicsBodyType.Dynamic;

			// make sure we wipe other holds
			EndPhysicsHold();

			m_holding = true;
			HeldEntity = tr.Entity;

			m_heldBody = body;
			m_holdDistance = Vector3.DistanceBetween(eyePos, tr.EndPosition);

			m_heldRot = eyeRot.Inverse * m_heldBody.Rotation;

			m_grabBody.Position = tr.EndPosition;
			m_grabBody.Rotation = m_heldBody.Rotation;

			m_heldBody.Sleeping = false;
			m_heldBody.AutoSleep = false;

			m_grabJoint = PhysicsJoint.CreateFixed(m_grabBody, m_heldBody.WorldPoint(tr.EndPosition));
			m_grabJoint.SpringLinear = new PhysicsSpring(LinearFrequency, LinearDampingRatio);
			m_grabJoint.SpringAngular = new PhysicsSpring(AngularFrequency, AngularDampingRatio);
		}

		public void UpdatePhysicsHold(Vector3 eyePos, Vector3 eyeDir, Rotation eyeRot)
		{
			if (!m_heldBody.IsValid())
			{
				EndPhysicsHold();
				return;
			}

			if (Input.Pressed(m_inputFreeze))
			{
				if (m_heldBody.BodyType == PhysicsBodyType.Dynamic)
					m_heldBody.BodyType = PhysicsBodyType.Static;

				m_stickyHold = true;
				EndPhysicsHold();
				return;
			}

			float distanceDelta = Input.MouseWheel * DistanceTargetDeltaSpeed;
			if (Input.Down(m_inputSlow))
			{
				// slow the beam as it gets closer to the player
				float targetfrac = m_holdDistance / DistanceTargetDeltaFadeDist;
				distanceDelta *= MathX.LerpTo(-MathF.Sin(targetfrac) * DistanceTargetDeltaFadeMult, 1, targetfrac);
			}
			m_holdDistance += distanceDelta;
			m_holdDistance = Math.Max(m_holdDistance, 0);

			// do the rotation here, this'll modify m_heldRot
			if (Input.Down(m_inputRotate))
				Rotate(eyeRot, Input.MouseDelta * RotationSpeed * (Input.Down(m_inputSlow) ? RotationSpeedSlowMult : 1));

			//DebugOverlay.Axis(m_grabBody.Position, m_grabBody.Rotation, 10, 0, false);
			//DebugOverlay.Axis(m_heldBody.Position, m_heldBody.Rotation, 10, 0, false);

			// if m_inputGravity is held down, disable the angular constraint so it's like pinning it on the end of the beam
			// having this will ignore the rest of this
			if (m_grabJoint != null)
				m_grabJoint.EnableAngularConstraint = !Input.Down(m_inputGravity);

			// once the above is released, set m_heldRot to it to avoid snapping weirdness
			if (Input.Released(m_inputGravity))
				m_heldRot = eyeRot.Inverse * m_heldBody.Rotation;

			m_grabBody.Position = eyePos + eyeDir * m_holdDistance;
			m_grabBody.Rotation = eyeRot * m_heldRot;

			if (Input.Down(m_inputRotationSnap))
			{
				Angles euler = m_grabBody.Rotation.Angles();

				m_grabBody.Rotation = Rotation.From(
					MathF.Round(euler.pitch / RotationSnapAngle) * RotationSnapAngle,
					MathF.Round(euler.yaw / RotationSnapAngle) * RotationSnapAngle,
					MathF.Round(euler.roll / RotationSnapAngle) * RotationSnapAngle
				);
			}
		}

		public void EndPhysicsHold()
		{
			m_holding = false;
			HeldEntity = null;

			m_grabJoint?.Remove();
			m_grabJoint = null;

			m_heldBody = null;
			m_heldRot = Rotation.Identity;

			if (m_heldBody.IsValid())
				m_heldBody.AutoSleep = true;
		}

		public override void BuildInput(InputBuilder inputBuilder)
		{
			if (!Holding)
				return;

			if (inputBuilder.Down(m_inputRotate))
				inputBuilder.ViewAngles = inputBuilder.OriginalViewAngles;
		}

		public void Rotate(Rotation eyeRot, Vector3 input)
		{
			var localRot = eyeRot;
			localRot *= Rotation.FromAxis(Vector3.Up, input.x);
			localRot *= Rotation.FromAxis(Vector3.Right, input.y);
			localRot = eyeRot.Inverse * localRot;

			// order of operations matters with quaterions!!
			m_heldRot = localRot * m_heldRot;
		}
	}
}
