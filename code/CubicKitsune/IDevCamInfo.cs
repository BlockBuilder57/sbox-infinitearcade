using Sandbox;

namespace CubicKitsune
{
	public interface IDevCamInfo
	{
		/// <summary>
		/// Prints information for the DevCamera.
		/// </summary>
		/// <param name="tr"></param>
		/// <returns>Should return true if "default" info should be printed as well.</returns>
		public bool DevCameraInfo(TraceResult tr);
	}
}
