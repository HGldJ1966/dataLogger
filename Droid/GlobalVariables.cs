using Android.App;


namespace DataLogger.Droid
{
	[Activity(Label = "GlobalVariables")]
	public static class GlobalVariables
	{
		public static bool isWriteLog = false;
		public static bool isStartTracking = true;
		public static string accelerometer = "";
		public static bool isTracking = false;
		public static string prevLat = "";
		public static string prevLon = "";
		public static string prevAlt = "";
		public static string prevSpeed = "";
		public static string prevAccuracy = "";


	}
}

