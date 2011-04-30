using System;
using Microsoft.SPOT;
using System.Threading;

namespace Controller
{
	public enum Category : uint { Input=1, Output=2 };
    public interface IPlugin
    {
		

        IPluginData[] GetData();
		Category PluginCategory();
		void PluginTimerCallback(Object state);
		int PluginTimerInterval();

		/// <summary>
		/// Event triggered when the plugin Timer Interval hits
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="data"></param>
		void PluginEventHandler(Object sender, IPluginData data);
    }
}
