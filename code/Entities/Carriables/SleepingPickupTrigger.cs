using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Sandbox;

namespace infinitearcade
{
	[Display(Name = "Sleeping Pickup Trigger"), Icon("select_all")]
	public partial class SleepingPickupTrigger : PickupTrigger
	{
		private TimeSince m_timeSinceDrop = 0;
		private float m_sleepFor;

		public bool IsSleeping { get { return m_timeSinceDrop < m_sleepFor; } set { } }

		public override void Spawn()
		{
			base.Spawn();

			// Set the default size
			SetTriggerSize(16);

			// Client doesn't need to know about htis
			Transmit = TransmitType.Never;
		}

		public void SleepFor(float secs)
		{
			m_sleepFor = secs;
			m_timeSinceDrop = 0;
		}
	}
}
