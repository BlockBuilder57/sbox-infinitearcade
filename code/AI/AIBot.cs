using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Sandbox;

namespace infinitearcade
{
	public class AIBot : Bot
	{
		public NavDriver Driver = new();

		private Player m_player;

		public AIBot() : this(null) { }
		public AIBot(string name) : base(name) { }

		[AdminCmd("bot", Help = "Spawns an AI bot.")]
		internal static void SpawnCustomBot(string name = null)
		{
			Host.AssertServer();

			// Create an instance of your custom bot.
			var bot = new AIBot(name);

			if (bot.Client.Pawn is Player player)
				bot.m_player = player;
		}

		public override void BuildInput(InputBuilder builder)
		{
		}

		public override void Tick()
		{
			base.Tick();
		}
	}
}
