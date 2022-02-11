using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Sandbox;

namespace infinitearcade
{
	public partial class TestCarriable : IACarriable
	{
		public override string ViewModelPath => "models/error.vmdl";
		private string m_identOverride;

		public TestCarriable()
		{

		}

		public TestCarriable(string identOverride = "") : this()
		{
			m_identOverride = identOverride;
			RandomizeIdent(m_identOverride);
		}

		public override void Spawn()
		{
			base.Spawn();
			RandomizeIdent(m_identOverride);
		}

		public override void ClientSpawn()
		{
			base.ClientSpawn();
			RandomizeIdent(m_identOverride);
		}

		void RandomizeIdent(string identOverride = "")
		{
			if (string.IsNullOrEmpty(identOverride))
				BucketIdent = Rand.Int(0, 69420).ToString("X");
			else
				BucketIdent = identOverride;
		}

		public override void Simulate(Client cl)
		{
			base.Simulate(cl);

			if (Input.Pressed(InputButton.Attack2))
			{
				Log.Error($"{(Host.IsClient ? "CLIENT" : "SERVER")} says the ident is: {BucketIdent}");
			}
		}
	}
}
