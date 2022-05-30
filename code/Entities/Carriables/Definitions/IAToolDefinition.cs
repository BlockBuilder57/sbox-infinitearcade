using Sandbox;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace infinitearcade
{
	[GameResource("Tool Definition", "tool", "The definition for a tool.")]
	public partial class IAToolDefinition : IACarriableDefinition
	{
		[HideInEditor] public static IReadOnlyList<IAToolDefinition> AllTools => _allTools;
		[HideInEditor] internal static List<IAToolDefinition> _allTools = new();

		public class InputSettings
		{
			public float Rate { get; set; }
			public int BurstAmount { get; set; }
			[BitFlags] public IATool.InputMode AllowedModes { get; set; } = IATool.InputMode.Single;
		}

		public InputSettings PrimaryInput { get; set; }
		public InputSettings SecondaryInput { get; set; }
		public InputSettings ReloadInput { get; set; }

		protected override void PostLoad()
		{
			base.PostLoad();

			if (!_allTools.Contains(this))
				_allTools.Add(this);
		}
	}
}
