
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
using System.IO;

namespace DataLogger.Droid
{
	[Activity(Label = "sendLogData")]
	public class sendLogData : ListActivity
	{
		string[] items;
		string[] items_path;

		protected override void OnCreate(Bundle savedInstanceState)
		{
			base.OnCreate(savedInstanceState);

			// Create your application here
			// Create your application here

			var currentDir = Android.OS.Environment.ExternalStorageDirectory.Path;


			var path = global::Android.OS.Environment.ExternalStorageDirectory.AbsolutePath;

			items_path = Directory.GetFiles(path);


			var pathList = new List<string>();

			foreach (string p in items_path)
			{
				pathList.Add(Path.GetFileName(p));

			}

			items = pathList.ToArray();

			//items = new string[] { "Vegetables", "Fruits", "Flower Buds", "Legumes", "Bulbs", "Tubers" };
			ListAdapter = new ArrayAdapter<String>(this, Android.Resource.Layout.SimpleListItem1, items);


		}

		protected override void OnListItemClick(ListView l, View v, int position, long id) { 
			var t = items[position];
			Toast.MakeText(this, t, ToastLength.Short).Show();

			//var logPath = FileSystemUtils.CopyFileToPersonalFolder(media.FilePathUri().AbsolutePath);
			var email = new Intent(Intent.ActionSend);

			var sdCardpath = Android.OS.Environment.ExternalStorageDirectory.Path;
			var filePath = System.IO.Path.Combine(sdCardpath, t); 
			var uri = Android.Net.Uri.FromFile(new Java.IO.File(filePath));

			email.PutExtra(Intent.ExtraEmail, new string[] { "pichpichaya7@gmail.com" });

			email.PutExtra(Intent.ExtraSubject, "DataLogger Record");

			email.PutExtra(Intent.ExtraText,"Data record");

			email.PutExtra(Intent.ExtraStream, uri);

			email.SetType("message/rfc822");

			StartActivity(email);


		}


	}
}

