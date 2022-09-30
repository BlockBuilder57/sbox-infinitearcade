using CubicKitsune;
using Sandbox;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CubicKitsune
{
	[GameResource("Tool Definition", "tool", "The definition for a tool.", Icon = "build")]
	public class CKToolDefinition : CKCarriableDefinition, ICKTool
	{
		[HideInEditor] public static IReadOnlyList<CKToolDefinition> AllTools => _allTools;
		[HideInEditor] internal static List<CKToolDefinition> _allTools = new();

		public ICKTool.InputSettings PrimaryInputSettings { get; set; }
		public ICKTool.InputSettings SecondaryInputSettings { get; set; }
		public ICKTool.InputSettings ReloadInputSettings { get; set; }

		[HideInEditor] public IDictionary<string, SoundEvent> SoundEvents { get; set; }
		public ICKTool.Sounds[] Sounds { get; set; }

		protected override void PostLoad()
		{
			base.PostLoad();

			if (!_allTools.Contains(this))
				_allTools.Add(this);
		}
	}
}
