
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;

namespace DataLogger.Droid
{
	[Activity(Label = "GlobalVariables")]
	public static class GlobalVariables
	{
		public static bool isWriteLog = false;
		public static bool isStartTracking = true;
		public static string accelerometer = "";

	}
}

