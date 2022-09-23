using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Sandbox;

namespace CubicKitsune
{
	[Library("projectile_base", Title = "Base Projectile")]
	public class CKProjectile : Prop
	{
		private TimeSince m_sinceSpawn;

		public CKProjectile()
		{
			m_sinceSpawn = 0;

			Tags.Clear();
			Tags.Add("weapon");
		}
	}
}
