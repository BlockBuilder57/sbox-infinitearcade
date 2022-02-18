using Sandbox;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace infinitearcade
{
	[Library("tool"), AutoGenerate]
	public partial class IAToolDefinition : IACarriableDefinition
	{
		[Hammer.Skip] public static IReadOnlyList<IAToolDefinition> AllTools => _allTools;
		[Hammer.Skip] internal static List<IAToolDefinition> _allTools = new();

		public float PrimaryRate { get; set; }
		public float SecondaryRate { get; set; }

		protected override void PostLoad()
		{
			base.PostLoad();

			if (!_allTools.Contains(this))
				_allTools.Add(this);
		}
	}
}
