using System;
using System.Collections.Generic;
using System.Globalization;
using Sandbox;

namespace infinitearcade
{
	public class DevCamera : CameraMode
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

		float LerpMode = 0.01f;

		public static bool Overlays = true;
		public static bool LockInput = false;

		public override void Activated()
		{
			if (Local.Pawn is Player player && player.CameraMode != null)
			{
				TargetPos = player.CameraMode.Position;
				TargetRot = player.CameraMode.Rotation;

				FovOverride = player.CameraMode.FieldOfView;
				Ortho = player.CameraMode.Ortho;
				OrthoSize = Math.Min(0.001f, player.CameraMode.OrthoSize);

				ZNear = player.CameraMode.ZNear;
				ZFar = player.CameraMode.ZFar;
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
			var tr = Trace.Ray(Position, Position + Rotation.Forward * 4096).HitLayer(CollisionLayer.All).UseHitboxes().Run();
			FieldOfView = FovOverride;

			if (PivotEnabled)
				PivotMove();
			else
				FreeMove();

			if (Overlays)
			{
				var normalRot = Rotation.LookAt(tr.Normal);
				DebugOverlay.Axis(tr.EndPosition, normalRot, 3.0f);

				if (tr.Entity != null && !tr.Entity.IsWorld)
				{
					string debugText = "";
					const int pad = 6;

					debugText += $"{"Entity",pad}: {tr.Entity} (managed {tr.Entity.GetType().FullName}, native {tr.Entity.EngineEntityName})";
					if (tr.Entity.Owner.IsValid() && tr.Entity.Owner != tr.Entity.Client)
						debugText += $"\n{"Owner",pad}: {tr.Entity.Owner}";
					if (tr.Entity.Client.IsValid())
						debugText += $"\n{"Client",pad}: {tr.Entity.Client.Name} (ident {tr.Entity.Client.NetworkIdent}) {(tr.Entity.Client.IsBot ? "[BOT]" : "")}";
					if (tr.Entity is ModelEntity model)
						debugText += $"\n{"Model",pad}: {model.GetModelName()}";
					if (!tr.Entity.ToString().EndsWith(tr.Entity.NetworkIdent.ToString()))
						debugText += $"\n{"Index",pad}: {tr.Entity.NetworkIdent}";
					debugText += $"\n{"Health",pad}: {tr.Entity.Health}";
					if (tr.Entity.IsClientOnly)
						debugText += $"\n{"Clientside",pad}: Clientside only";

					DebugOverlay.Text(tr.EndPosition + Vector3.Up * 20, debugText, Color.White);

					var bbox_world = tr.Entity.WorldSpaceBounds;
					DebugOverlay.Box(0, Vector3.Zero, Rotation.Identity, bbox_world.Mins, bbox_world.Maxs, Color.Red.WithAlpha(0.4f));

					if (tr.Entity is ModelEntity modelEnt)
					{
						var bbox_collision = modelEnt.CollisionBounds;
						DebugOverlay.Box(0, tr.Entity.Position, tr.Entity.Rotation, bbox_collision.Mins * tr.Entity.LocalScale, bbox_collision.Maxs * tr.Entity.LocalScale, Color.Green.WithAlpha(0.4f));

						for (int i = 0; i < modelEnt.BoneCount; i++)
						{
							var tx = modelEnt.GetBoneTransform(i);
							var name = modelEnt.GetBoneName(i);
							var parent = modelEnt.GetBoneParent(i);

							if (parent > -1)
							{
								var ptx = modelEnt.GetBoneTransform(parent);
								DebugOverlay.Line(tx.Position, ptx.Position, Color.White.WithAlpha(0.2f), depthTest: false);
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

				if (input.Pressed(InputButton.Slot1)) LerpSpeed(0.0f);
				if (input.Pressed(InputButton.Slot2)) LerpSpeed(0.01f);
				if (input.Pressed(InputButton.Slot3)) LerpSpeed(0.5f);
				if (input.Pressed(InputButton.Slot4)) LerpSpeed(0.9f);

				if (input.Pressed(InputButton.Walk))
				{
					var tr = Trace.Ray(Position, Position + Rotation.Forward * Int32.MaxValue).HitLayer(CollisionLayer.All).UseHitboxes().Run();
					if (tr.Hit)
					{
						PivotPos = tr.EndPosition;
						PivotDist = Vector3.DistanceBetween(tr.EndPosition, Position);
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
					//if (tr.Entity.IsValid())
					//	tr.Entity.DebugFlags ^= EntityDebugFlags.Text;
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

			if (LerpMode > 0)
			{
				Position = Vector3.Lerp(Position, TargetPos, 10 * RealTime.Delta * (1 - LerpMode));
				Rotation = Rotation.Slerp(Rotation, TargetRot, 10 * RealTime.Delta * (1 - LerpMode));
			}
			else
			{
				Position = TargetPos;
				Rotation = TargetRot;
			}
		}

		void PivotMove()
		{
			PivotDist -= MoveInput.x * RealTime.Delta * 100 * (PivotDist / 50);
			PivotDist = PivotDist.Clamp(1, 1000);

			TargetRot = Rotation.From(LookAngles);
			if (LerpMode > 0)
				Rotation = Rotation.Slerp(Rotation, TargetRot, 10 * RealTime.Delta * (1 - LerpMode));
			else
				Rotation = TargetRot;

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
