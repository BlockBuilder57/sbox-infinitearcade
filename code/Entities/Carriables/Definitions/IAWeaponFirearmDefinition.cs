using Sandbox;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace infinitearcade
{
	[Library("firearm"), AutoGenerate]
	public partial class IAWeaponFirearmDefinition : IAToolDefinition
	{
		[Hammer.Skip] public static IReadOnlyList<IAToolDefinition> AllFirearms => _allFirearms;
		[Hammer.Skip] internal static List<IAToolDefinition> _allFirearms = new();

		public float ReloadTime { get; set; } = 1.0f;

		public bool HasPrimary { get; set; } = true;
		public int ClipPrimary { get; set; } = 8;
		public int AmmoPrimary { get; set; } = 24;
		public bool HasSecondary { get; set; } = false;
		public int Secondary { get; set; }
		public int ClipSecondary { get; set; }
		public int AmmoSecondary { get; set; }

		protected override void PostLoad()
		{
			base.PostLoad();

			if (!_allFirearms.Contains(this))
				_allFirearms.Add(this);
		}
	}
}
