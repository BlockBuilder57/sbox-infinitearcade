using System.Collections.Generic;
using Sandbox;

namespace CubicKitsune
{
	[GameResource("Player Loadout", "loadout", "Describes the loadout and stats for a player.", Icon = "engineering")]
	public class CKPlayerLoadoutResource : GameResource
	{
		[Category("Stats")] public float StartingHealth { get; set; } = 100f;
		[Category("Stats")] public float MaxHealth { get; set; } = 100f;
		[Category("Stats")] public float StartingArmor { get; set; } = 100f;
		[Category("Stats")] public float MaxArmor { get; set; } = 100f;
		[Category("Stats")] public float StartingArmorPower { get; set; } = 1f;
		
		[Category("Items")] public List<CKCarriableResource> Carriables { get; set; }
	}

}
