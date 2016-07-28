
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
	[Activity(Label = "NavigationActivity")]
	public class NavigationActivity : Activity
	{
		protected override void OnCreate(Bundle savedInstanceState)
		{
			base.OnCreate(savedInstanceState);
			SetContentView(Resource.Layout.NavigationLayout);
			// Create your application here

			/*

			Button bnt_report = FindViewById<Button>(Resource.Id.bnt_report);
			Button bnt_navigation = FindViewById<Button>(Resource.Id.bnt_navigation);
			Button bnt_sendData = FindViewById<Button>(Resource.Id.bnt_sendData);

			bnt_report.Click += delegate
			{
				var report_activity = new Intent(this, typeof(MainActivity));

				StartActivity(report_activity);

				bnt_report.Enabled = false;
				bnt_navigation.Enabled = true;
				bnt_sendData.Enabled = true;


			};

			bnt_navigation.Click += delegate
			{
				var navigation_activity = new Intent(this, typeof(NavigationActivity));

				StartActivity(navigation_activity);

				bnt_report.Enabled = true;
				bnt_navigation.Enabled = false;
				bnt_sendData.Enabled = true;



			};

			bnt_sendData.Click += delegate
			{
				var sendData_activity = new Intent(this, typeof(sendLogData));

				StartActivity(sendData_activity);

				bnt_report.Enabled = true;
				bnt_navigation.Enabled = true;
				bnt_sendData.Enabled = false;

			};

*/
		}
	}
}

