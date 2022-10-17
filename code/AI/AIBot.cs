using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CubicKitsune;
using Sandbox;

namespace infinitearcade
{
	public class AIBot : Bot
	{
		public NavSteer Steer = new();

		private Player m_player;

		public AIBot() : this(null) { }

		public AIBot(string name) : base(name) { }

		[ConCmd.Admin("ia_bot_add", Help = "Spawns a bot.")]
		internal static void SpawnCustomBot(int count = 1, string name = null)
		{
			Host.AssertServer();

			// Create an instance of your custom bot.
			for (int i = 0; i < count; i++)
			{
				var bot = new AIBot(name);

				if (bot.Client.Pawn is Player player)
					bot.m_player = player;
			}
		}

		public override void BuildInput(InputBuilder builder)
		{
			/*Client cl = null;
			if (Entity.FindByIndex(1) != null)
				cl = Entity.FindByIndex(1) as Client;

			if (cl.IsValid())
			{
				builder.CopyLastInput(cl);
				builder.ViewAngles = builder.ViewAngles.WithYaw(builder.ViewAngles.yaw + 180);
			}*/

			if (Steer != null)
			{
				var output = Steer.Tick(m_player.Position);

				if (!output.Finished)
					builder.ViewAngles = output.Direction.EulerAngles;
				
				Vector3 test = output.Direction * builder.ViewAngles.ToRotation().Inverse;

				if (!output.Finished)
				{
					builder.ClearButton(InputButton.Jump);
					builder.SetButton(InputButton.Run);
					builder.InputDirection = test;
				}
				else
				{
					builder.InputDirection = Vector3.Zero;
				}
			}
			else
			{
				builder.InputDirection = Vector3.Zero;
			}
		}

		[ConCmd.Admin("ia_nav_pathtest")]
		public static void SetTarget()
		{
			if (!ConsoleSystem.Caller.IsValid())
				return;

			Entity pawn = ConsoleSystem.Caller.Pawn;

			var tr = Trace.Ray( pawn.EyePosition, pawn.EyePosition + pawn.EyeRotation.Forward * 4096 )
				.WithoutTags("trigger")
				.Ignore(pawn)
				.Run();
			
			//DebugOverlay.Line(tr.StartPosition, tr.EndPosition, 2f);
			
			foreach (AIBot bot in All.Where(x => x is AIBot).Cast<AIBot>())
			{
				bot.Steer.ToFollow = tr.Entity;
				bot.Steer.Target = tr.EndPosition;
				bot.Steer.Driver.MakePath(bot.m_player.Position, bot.Steer.Target);
				
				bot.Steer.Driver.DebugDraw(2f);
			}
		}

		public override void Tick()
		{
			base.Tick();
			
			//Steer?.Driver?.DebugDraw(0.01f);
		}
	}
}
