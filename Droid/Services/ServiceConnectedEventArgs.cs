using System;
using Android.OS;

namespace DataLogger.Droid.Services
{
	public class ServiceConnectedEventArgs : EventArgs
	{
		public IBinder Binder { get; set; }
	}
}