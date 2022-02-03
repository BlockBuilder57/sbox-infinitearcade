using System;
using System.Globalization;
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
		public static bool LockInput = false;

		public override void Activated()
		{
			if (Local.Pawn?.Camera is Camera currentCam)
			{
				TargetPos = currentCam.Position;
				TargetRot = currentCam.Rotation;

				FovOverride = currentCam.FieldOfView;
				Ortho = currentCam.Ortho;
				OrthoSize = Math.Min(0.001f, currentCam.OrthoSize);

				ZNear = currentCam.ZNear;
				ZFar = currentCam.ZFar;
			}

			Position = TargetPos;
			Rotation = TargetRot;
			LookAngles = Rotation.Angles();

			if (FovOverride == 0)
				FovOverride = float.Parse(ConsoleSystem.GetValue("default_fov", "90"), CultureInfo.InvariantCulture);

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
					string debugText = "";
					const int pad = 6;

					debugText += $"{"Entity".PadLeft(pad)}: {tr.Entity} ({tr.Entity.GetType().FullName}, engine name {tr.Entity.EngineEntityName})";
					if (tr.Entity is ModelEntity model)
						debugText += $"\n{"Model".PadLeft(pad)}: {model.GetModelName()}";
					debugText += $"\n{"Index".PadLeft(pad)}: {tr.Entity.NetworkIdent}";
					debugText += $"\n{"Health".PadLeft(pad)}: {tr.Entity.Health}";
					if (tr.Entity.IsClientOnly)
						debugText += $"\n{"Clientside".PadLeft(pad)}: Clientside only";

					DebugOverlay.Text(tr.EndPos + Vector3.Up * 20, debugText, Color.White);

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
			if (LockInput)
			{
				if (input.Pressed(InputButton.View))
					LockInput = false;

				input.ClearButton(InputButton.View);
				base.BuildInput(input);
			}
			else
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
					var tr = Trace.Ray(Position, Position + Rotation.Forward * Int32.MaxValue).HitLayer(CollisionLayer.All).UseHitboxes().Run();
					if (tr.Hit)
					{
						PivotPos = tr.EndPos;
						PivotDist = Vector3.DistanceBetween(tr.EndPos, Position);
					}
				}

				if (input.Down(InputButton.Jump))
					MoveInput.z = 1;

				if (input.Pressed(InputButton.Flashlight))
					FovOverride = 90;

				if (input.Pressed(InputButton.Drop))
				{
					Ortho = !Ortho;

					if (Ortho && OrthoSize == 0)
					{
						// we (probably) didn't have an ortho size before, so let's make one
						// it's pretty hacky, but it does approximate the normal perspective cam
						OrthoSize = (float)(Vector3.DistanceBetween(Position, Local.Pawn.Position) * Math.Tan(FovOverride * 0.5f * (Math.PI / 180f))) / 409.6f;
					}
				}

				if (input.Pressed(InputButton.Attack1))
				{
					var tr = Trace.Ray(Position, Position + Rotation.Forward * 4096).HitLayer(CollisionLayer.All).UseHitboxes().Run();
					tr.Entity?.ToggleDebugBits(DebugOverlayBits.OVERLAY_TEXT_BIT);
				}

				if (input.Down(InputButton.Attack2))
				{
					if (Ortho)
					{
						OrthoSize += input.AnalogLook.pitch * (OrthoSize / 30.0f);
						OrthoSize = OrthoSize.Clamp(0.001f, 5);
					}
					else
					{
						FovOverride += input.AnalogLook.pitch * (FovOverride / 30.0f);
						FovOverride = FovOverride.Clamp(5, 150);
					}

					input.AnalogLook = default;
				}

				if (input.Pressed(InputButton.View))
					LockInput = true;

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
