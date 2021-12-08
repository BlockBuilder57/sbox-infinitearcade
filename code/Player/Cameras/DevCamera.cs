﻿using System;
using Sandbox;

namespace infinitearcade
{
	public class DevCamera : Camera
	{
		Angles LookAngles;
		Vector3 MoveInput;

		Vector3 TargetPos;
		Rotation TargetRot;

		bool PivotEnabled;
		Vector3 PivotPos;
		float PivotDist;

		float MoveSpeed;
		float FovOverride = 0;

		float LerpMode = 0;

		public static bool Overlays = true;

		public override void Activated()
		{
			base.Activated();

			TargetPos = CurrentView.Position;
			TargetRot = CurrentView.Rotation;

			Position = TargetPos;
			Rotation = TargetRot;
			LookAngles = Rotation.Angles();
			FovOverride = CurrentView.FieldOfView;

			DoFPoint = 0.0f;
			DoFBlurSize = 0.0f;

			Overlays = Cookie.Get("debugcam.overlays", Overlays);

			// Set the devcamera class on the HUD. It's up to the HUD what it does with it.
			Local.Hud?.SetClass("devcamera", true);
		}

		public override void Deactivated()
		{
			base.Deactivated();

			Local.Hud?.SetClass("devcamera", false);
		}

		public override void Update()
		{
			var player = Local.Client;
			if (player == null) return;

			var tr = Trace.Ray(Position, Position + Rotation.Forward * 4096).HitLayer(CollisionLayer.All).UseHitboxes().Run();
			FieldOfView = FovOverride;

			Viewer = null;
			{
				var lerpTarget = tr.EndPos.Distance(Position);
				DoFPoint = lerpTarget;// DoFPoint.LerpTo( lerpTarget, Time.Delta * 10 );
			}

			if (PivotEnabled)
				PivotMove();
			else
				FreeMove();

			if (Overlays)
			{
				var normalRot = Rotation.LookAt(tr.Normal);
				DebugOverlay.Axis(tr.EndPos, normalRot, 3.0f);

				if (tr.Entity != null && !tr.Entity.IsWorld)
				{
					DebugOverlay.Text(tr.EndPos + Vector3.Up * 20, $"Entity: {tr.Entity} ({tr.Entity.EngineEntityName})\n" +
																	$" Index: {tr.Entity.NetworkIdent}\n" +
																	$"Health: {tr.Entity.Health}", Color.White);

					if (tr.Entity is ModelEntity modelEnt)
					{
						var bbox = modelEnt.CollisionBounds;
						DebugOverlay.Box(0, tr.Entity.Position, tr.Entity.Rotation, bbox.Mins * tr.Entity.LocalScale, bbox.Maxs * tr.Entity.LocalScale, Color.Green);

						for (int i = 0; i < modelEnt.BoneCount; i++)
						{
							var tx = modelEnt.GetBoneTransform(i);
							var name = modelEnt.GetBoneName(i);
							var parent = modelEnt.GetBoneParent(i);

							if (parent > -1)
							{
								var ptx = modelEnt.GetBoneTransform(parent);
								DebugOverlay.Line(tx.Position, ptx.Position, Color.White, depthTest: false);
							}
						}

					}
				}
			}
		}

		public override void BuildInput(InputBuilder input)
		{
			MoveInput = input.AnalogMove;

			MoveSpeed = 1;
			if (input.Down(InputButton.Run)) MoveSpeed = 5;
			if (input.Down(InputButton.Duck)) MoveSpeed = 0.2f;

			PivotEnabled = input.Down(InputButton.Walk);

			if (input.Down(InputButton.Slot1)) LerpSpeed(0.0f);
			if (input.Down(InputButton.Slot2)) LerpSpeed(0.5f);
			if (input.Down(InputButton.Slot3)) LerpSpeed(0.9f);

			if (input.Pressed(InputButton.Walk))
			{
				var tr = Trace.Ray(Position, Position + Rotation.Forward * 4096).HitLayer(CollisionLayer.All).UseHitboxes().Run();
				PivotPos = tr.EndPos;
				PivotDist = Vector3.DistanceBetween(tr.EndPos, Position);
			}

			if (input.Down(InputButton.Jump))
				MoveInput.z = 1;

			if (input.Pressed(InputButton.Flashlight))
				FovOverride = 90;

			if (input.Down(InputButton.Use))
				DoFBlurSize = Math.Clamp(DoFBlurSize + (Time.Delta * 3.0f), 0.0f, 100.0f);

			if (input.Down(InputButton.Menu))
				DoFBlurSize = Math.Clamp(DoFBlurSize - (Time.Delta * 3.0f), 0.0f, 100.0f);

			if (input.Pressed(InputButton.Attack1))
			{
				var tr = Trace.Ray(Position, Position + Rotation.Forward * 4096).HitLayer(CollisionLayer.All).UseHitboxes().Run();
				tr.Entity.ToggleDebugBits(DebugOverlayBits.OVERLAY_TEXT_BIT);
			}

			if (input.Down(InputButton.Attack2))
			{
				FovOverride += input.AnalogLook.pitch * (FovOverride / 30.0f);
				FovOverride = FovOverride.Clamp(5, 150);
				input.AnalogLook = default;
			}

			if (input.Pressed(InputButton.Reload))
			{
				Overlays = !Overlays;
				Cookie.Set("debugcam.overlays", Overlays);
			}

			LookAngles += input.AnalogLook * (FovOverride / 80.0f);
			LookAngles.roll = 0;

			input.Clear();
			input.StopProcessing = true;
		}

		void LerpSpeed(float amount)
		{
			LerpMode = amount;
			IADebugging.ScreenText($"Set devcam lerp to {LerpMode}", 0.5f);
		}

		void FreeMove()
		{
			var mv = MoveInput.Normal * 300 * RealTime.Delta * Rotation * MoveSpeed;

			TargetRot = Rotation.From(LookAngles);
			TargetPos += mv;

			Position = Vector3.Lerp(Position, TargetPos, 10 * RealTime.Delta * (1 - LerpMode));
			Rotation = Rotation.Slerp(Rotation, TargetRot, 10 * RealTime.Delta * (1 - LerpMode));
		}

		void PivotMove()
		{
			PivotDist -= MoveInput.x * RealTime.Delta * 100 * (PivotDist / 50);
			PivotDist = PivotDist.Clamp(1, 1000);

			TargetRot = Rotation.From(LookAngles);
			Rotation = Rotation.Slerp(Rotation, TargetRot, 10 * RealTime.Delta * (1 - LerpMode));

			TargetPos = PivotPos + Rotation.Forward * -PivotDist;
			Position = TargetPos;

			if (Overlays)
			{
				var scale = Vector3.One * (2 + MathF.Sin(RealTime.Now * 10) * 0.3f);
				DebugOverlay.Box(PivotPos, scale * -1, scale, Color.Green);
			}
		}
	}
}
