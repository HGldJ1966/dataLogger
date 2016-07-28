using Android.App;
using Android.Widget;
using Android.OS;
using Java.IO;
using Android.Content;
using Android.Content.PM;
using Android.Graphics;
using System.Collections.Generic;
using Android.Provider;
using Android.Locations;
using Android.Util;
using System.Threading.Tasks;
using Java.Util;
using DataLogger.Droid.Services;
using System;
using System.Net;
using Android.Hardware;
using Android.Views;
using Android.Runtime;
using System.IO;
using Android.Media;
using System.Linq;

namespace DataLogger.Droid
{
	[Activity(Label = "Pedestrian report", MainLauncher = true, Icon = "@mipmap/icon", ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation,
			  ScreenOrientation = ScreenOrientation.Portrait)]

	public class MainActivity : Activity, ISensorEventListener
	{
		ImageView _imageView;
		static readonly string TAG = "X:" + typeof(Activity).Name;

		readonly string logTag = "MainActivity";
		// make our labels
		TextView latText;
		TextView longText;
		TextView altText;
		TextView speedText;
		TextView accText;
		string txt; //storing information to write in SD card
		static readonly object _syncLock = new object();
		SensorManager _sensorManager;
		TextView _sensorTextView;


		protected override void OnCreate(Bundle bundle)
		{
			base.OnCreate(bundle);

			Log.Debug(logTag, "OnCreate: Location app is becoming active");

			// Set our view from the "main" layout resource
			SetContentView(Resource.Layout.ReportLayout);

			latText = FindViewById<TextView>(Resource.Id.lat);
			longText = FindViewById<TextView>(Resource.Id.lon);
			altText = FindViewById<TextView>(Resource.Id.alt);
			speedText = FindViewById<TextView>(Resource.Id.speed);
			accText = FindViewById<TextView>(Resource.Id.acc);

			latText.Text = "Latitude:";
			longText.Text = "Longitude: ";
			altText.Text = "Altitude";
			speedText.Text = "Speed";
			accText.Text = "Accuracy";

			Button bnt_navigation = FindViewById<Button>(Resource.Id.bnt_navigation);
			Button bnt_sendData = FindViewById<Button>(Resource.Id.bnt_sendData);

			Button bnt_report = FindViewById<Button>(Resource.Id.button_report);



			bnt_navigation.Click += delegate
			{
				var navigation_activity = new Intent(this, typeof(NavigationActivity));

				StartActivity(navigation_activity);

			};

			bnt_sendData.Click += delegate
			{
				var sendData_activity = new Intent(this, typeof(sendLogData));

				StartActivity(sendData_activity);

			};


			bnt_report.Click += delegate
			{
				string message = FindViewById<EditText>(Resource.Id.editText_msg).Text;
				var typeOfPath = FindViewById<Spinner>(Resource.Id.spinner_mode).SelectedItem;

				try
				{
					string data = string.Format("{0},{1}",message,typeOfPath);
					string url = System.String.Concat("http://203.131.209.92/datalogger/getReport.php?data=",data);

				
					List<byte> rawBuffer = new List<byte>();
					var contentUri = Android.Net.Uri.FromFile(GlobalVariables.image_file);
					using (System.IO.Stream stream = ContentResolver.OpenInputStream(contentUri))
					{
						byte[] buffer = new byte[4096];
						int len = 0;
						while ((len = stream.Read(buffer, 0, buffer.Length)) > 0)
						{
							rawBuffer.AddRange(buffer.Take(len));
							       
						}
					}

					string base64 = System.Convert.ToBase64String(rawBuffer.ToArray());

					try
					{
						var webClient = new System.Net.WebClient();
						webClient.Headers[HttpRequestHeader.ContentType] = "binary/octet-stream";
						//webClient.UploadFile(new Uri("http://203.131.209.92/datalogger/upload.php"), base64);
						Toast.MakeText(this, base64, ToastLength.Short).Show();
					}
					catch (Exception ex)
					{
						Toast.MakeText(this,"error", ToastLength.Long).Show();
					}


					HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);

					//request.Method = "GET/POST";
					//using GET - request.Headers.Add ("Authorization","Authorizaation value");


					//request.ContentType = "application/json";
					HttpWebResponse myResp = (HttpWebResponse)request.GetResponse();


					string responseText;

					using (var response = request.GetResponse())
					{
						using (var reader = new StreamReader(response.GetResponseStream()))
						{
							responseText = reader.ReadToEnd();
							Toast.MakeText(this, responseText, ToastLength.Short).Show();


						}
					}


				}

				catch (WebException exception)
				{
					/*
					string responseText;
					using (var reader = new StreamReader(exception.Response.GetResponseStream()))
					{
						responseText = reader.ReadToEnd();

					}
					*/
				}

			};



			Spinner spinner = FindViewById<Spinner>(Resource.Id.spinner_mode);

			spinner.ItemSelected += new System.EventHandler<AdapterView.ItemSelectedEventArgs>(spinner_ItemSelected);
			var adapter = ArrayAdapter.CreateFromResource(
				this, Resource.Array.choice_array, Android.Resource.Layout.SimpleSpinnerItem);

			adapter.SetDropDownViewResource(Android.Resource.Layout.SimpleSpinnerDropDownItem);
			spinner.Adapter = adapter;

			//report message
			var editText_msg = FindViewById<EditText>(Resource.Id.editText_msg);
			//var textView = FindViewById<TextView>(Resource.Id.textView_address);

			//if text is change, do
			/*
			editText_msg.TextChanged += (object sender, Android.Text.TextChangedEventArgs e) =>
			{
				textView.Text = e.Text.ToString();
			};
			*/

			FindViewById<Button>(Resource.Id.bnt_start_stop).Click += trackingGPS;

			//Picture
			if (IsThereAnAppToTakePictures())
			{

				CreateDirectoryForPictures();


				Button button = FindViewById<Button>(Resource.Id.bnt_takePic);
				_imageView = FindViewById<ImageView>(Resource.Id.imageView1);
				button.Click += TakeAPicture;
			}


			//LOCATION 

			//InitializeLocationManager();

			App.Current.LocationServiceConnected += (object sender, ServiceConnectedEventArgs e) =>
			{
				Log.Debug(logTag, "ServiceConnected Event Raised");
				// notifies us of location changes from the system
				App.Current.LocationService.LocationChanged += HandleLocationChanged;
				//notifies us of user changes to the location provider (ie the user disables or enables GPS)
				App.Current.LocationService.ProviderDisabled += HandleProviderDisabled;
				App.Current.LocationService.ProviderEnabled += HandleProviderEnabled;
				// notifies us of the changing status of a provider (ie GPS no longer available)
				App.Current.LocationService.StatusChanged += HandleStatusChanged;
			};



			//Accelerometer data
			_sensorManager = (SensorManager)GetSystemService(Context.SensorService);
			_sensorTextView = FindViewById<TextView>(Resource.Id.accelerometer_text);


			if (GlobalVariables.isTracking)
			{
				FindViewById<Button>(Resource.Id.bnt_start_stop).SetBackgroundColor(Color.Red);
				FindViewById<Button>(Resource.Id.bnt_start_stop).Text = "Stop tracking";

				FindViewById<TextView>(Resource.Id.lat).Text = GlobalVariables.prevLat;
				FindViewById<TextView>(Resource.Id.lon).Text = GlobalVariables.prevLon;
				FindViewById<TextView>(Resource.Id.acc).Text = GlobalVariables.prevAccuracy;
				FindViewById<TextView>(Resource.Id.speed).Text = GlobalVariables.prevSpeed;
				FindViewById<TextView>(Resource.Id.alt).Text = GlobalVariables.prevAlt;

			}
			else {
				FindViewById<Button>(Resource.Id.bnt_start_stop).SetBackgroundColor(Color.Blue);
				FindViewById<Button>(Resource.Id.bnt_start_stop).Text = "Start tracking";

				/*
				FindViewById<TextView>(Resource.Id.lat).Text = GlobalVariables.prevLat;
				FindViewById<TextView>(Resource.Id.lon).Text = GlobalVariables.prevLon;
				FindViewById<TextView>(Resource.Id.acc).Text = GlobalVariables.prevAccuracy;
				FindViewById<TextView>(Resource.Id.speed).Text = GlobalVariables.prevSpeed;
				FindViewById<TextView>(Resource.Id.alt).Text = GlobalVariables.prevAlt;
				*/
			}

		}

		void writeLog(string txt)
		{
			//create file name for date

			string today = DateTime.Now.ToString("dd-MM-yyy");
			var sdCardpath = Android.OS.Environment.ExternalStorageDirectory.Path;
			var filePath = System.IO.Path.Combine(sdCardpath, string.Concat(today, "_datalogger.txt"));


			using (var writer = new System.IO.StreamWriter(filePath, true))
			{
				writer.WriteLine(txt);

			}

		}

		string getCurrentTime()
		{
			DateTime now = DateTime.Now.ToLocalTime();


			if (DateTime.Now.IsDaylightSavingTime() == true)
			{
				now = now.AddHours(1);
			}

			return (string.Format("{0}", now)); ;
		}


		// stop tracking GPS
		void trackingGPS(object sender, EventArgs eventArgs)
		{

			string toast;

			if (GlobalVariables.isStartTracking)
			{
				App.StartLocationService();
				GlobalVariables.isStartTracking = false;
				FindViewById<Button>(Resource.Id.bnt_start_stop).Text = "Stop tracking";
				FindViewById<Button>(Resource.Id.bnt_start_stop).SetBackgroundColor(Color.Red);
				toast = string.Format("START");
				FindViewById<TextView>(Resource.Id.lat).Text = "Latitude: {wait}";
				FindViewById<TextView>(Resource.Id.lon).Text = "Longitude: {wait}";
				FindViewById<TextView>(Resource.Id.alt).Text = "Altitude: {wait}";
				FindViewById<TextView>(Resource.Id.speed).Text = "Speed: {wait}";
				FindViewById<TextView>(Resource.Id.acc).Text = "Accuracy: {wait}";


				GlobalVariables.isTracking = true; //this app is tracking GPS!!
			}

			else {
				App.StopLocationService();
				GlobalVariables.isStartTracking = true;
				FindViewById<Button>(Resource.Id.bnt_start_stop).Text = "Start tracking";
				FindViewById<Button>(Resource.Id.bnt_start_stop).SetBackgroundColor(Color.Gray);
				toast = string.Format("STOP");

				GlobalVariables.isTracking = false; //this app is not tracking GPS!!

			}

			Toast.MakeText(this, toast, ToastLength.Short).Show();
		}

		bool IsThereAnAppToTakePictures()
		{

			Intent intent = new Intent(MediaStore.ActionImageCapture);
			IList<ResolveInfo> availableActivities =
				PackageManager.QueryIntentActivities(intent, PackageInfoFlags.MatchDefaultOnly);
			return availableActivities != null && availableActivities.Count > 0;


		}

		private void spinner_ItemSelected(object sender, AdapterView.ItemSelectedEventArgs e)
		{
			Spinner spinner = (Spinner)sender;

			string toast = string.Format("{0}", spinner.GetItemAtPosition(e.Position));
			Toast.MakeText(this, toast, ToastLength.Long).Show();
		}

		private void CreateDirectoryForPictures()
		{
			App._dir = new Java.IO.File(
				Android.OS.Environment.GetExternalStoragePublicDirectory(
					Android.OS.Environment.DirectoryPictures), "CameraAppDemo");
			if (!App._dir.Exists())
			{
				App._dir.Mkdirs();
			}
		}

		private void TakeAPicture(object sender, EventArgs eventArgs)
		{
			//Pause collect GPS points.
			OnPause();

			Intent intent = new Intent(MediaStore.ActionImageCapture);

			App._file = new Java.IO.File(App._dir, string.Format("myPhoto_{0}.jpg", UUID.RandomUUID()));

			//App._file = new File(App._dir, String.Format("myPhoto_{0}.jpg", guid.NewGuid()));
			intent.PutExtra(MediaStore.ExtraOutput, Android.Net.Uri.FromFile(App._file));


			//Toast.MakeText(this, App._file.ToString(), ToastLength.Short).Show();
			GlobalVariables.image_file = App._file;


			StartActivityForResult(intent, 0);

			//Resume collect GPS
			OnResume();
		}

		protected override void OnActivityResult(int requestCode, Result resultCode, Intent data)
		{
			base.OnActivityResult(requestCode, resultCode, data);

			// Make it available in the gallery

			Intent mediaScanIntent = new Intent(Intent.ActionMediaScannerScanFile);
			Android.Net.Uri contentUri = Android.Net.Uri.FromFile(App._file);
			mediaScanIntent.SetData(contentUri);



			SendBroadcast(mediaScanIntent);

			// Display in ImageView. We will resize the bitmap to fit the display.
			// Loading the full sized image will consume to much memory
			// and cause the application to crash.

			int height = Resources.DisplayMetrics.HeightPixels;
			int width = _imageView.Height;
			App.bitmap = App._file.Path.LoadAndResizeBitmap(width, height);
			if (App.bitmap != null)
			{
				_imageView.SetImageBitmap(App.bitmap);
				App.bitmap = null;
			}

			// Dispose of the Java side bitmap.
			GC.Collect();
		}


		public void OnProviderDisabled(string provider)
		{

		}

		protected override void OnPause()
		{
			Log.Debug(logTag, "OnPause: Location app is moving to background");
			base.OnPause();

		}

		public void OnProviderEnabled(string provider) { }

		public void OnStatusChanged(string provider, Availability status, Bundle extras)
		{
			Log.Debug(TAG, "{0}, {1}", provider, status);
		}


		protected override void OnSaveInstanceState(Bundle outState)
		{
			/*
			if (GlobalVariables.isStartTracking)
			{
				FindViewById<Button>(Resource.Id.bnt_start_stop).Text = "Start tracking";
				FindViewById<Button>(Resource.Id.bnt_start_stop).SetBackgroundColor(Color.Gray);
			}
			else { 
				FindViewById<Button>(Resource.Id.bnt_start_stop).Text = "Stop tracking";
				FindViewById<Button>(Resource.Id.bnt_start_stop).SetBackgroundColor(Color.Red);
			}

			// always call the base implementation!
			outState.PutString(savestate_tracking, FindViewById<Button>(Resource.Id.bnt_start_stop).Text);
			string toast = string.Format("save state {0}",savestate_tracking);
			Toast.MakeText(this, toast, ToastLength.Short).Show();
			*/
			base.OnSaveInstanceState(outState);
		}

		protected override void OnResume()
		{
			

			Log.Debug(logTag, "OnResume: Location app is moving into foreground");
			base.OnResume();
			_sensorManager.RegisterListener(this,
									_sensorManager.GetDefaultSensor(SensorType.Accelerometer),
									SensorDelay.Ui);
		}

		protected override void OnDestroy()
		{
			Log.Debug(logTag, "OnDestroy: Location app is becoming inactive");
			base.OnDestroy();
			//_sensorManager.UnregisterListener(this);
			// Stop the location service:
			//App.StopLocationService();
		}

		#region Android Location Service methods

		///<summary>
		/// Updates UI with location data
		/// </summary>
		public void HandleLocationChanged(object sender, LocationChangedEventArgs e)
		{
			Location location = e.Location;
			Log.Debug(logTag, "Foreground updating");

			// these events are on a background thread, need to update on the UI thread
			RunOnUiThread(() =>
			{
				latText.Text = string.Format("Latitude: {0:f6}", location.Latitude);
				longText.Text = string.Format("Longitude: {0:f6}", location.Longitude);
				altText.Text = string.Format("Altitude: {0:f6}", location.Altitude);
				speedText.Text = string.Format("Speed: {0:f2}", location.Speed);
				accText.Text = string.Format("Accuracy: {0:f2} m.", location.Accuracy);

				GlobalVariables.prevLat = string.Format("Latitude: {0:f6}", location.Latitude);
				GlobalVariables.prevLon = string.Format("Longitude: {0:f6}", location.Longitude);
				GlobalVariables.prevAlt = string.Format("Altitude: {0:f6}", location.Altitude);
				GlobalVariables.prevSpeed = string.Format("Speed: {0:f2}", location.Speed);
				GlobalVariables.prevAccuracy = string.Format("Accuracy: {0:f2} m.", location.Accuracy);

				//write location
				string currentTime = getCurrentTime();
				if (GlobalVariables.isWriteLog)
				{
					//Write lat,lon to file
					txt = string.Format("{0}, {1:f6}, {2:f6}, {3:f6}, {4:f6}, {5:f6}, {6}",
										currentTime,
										location.Latitude,
										location.Longitude,
										location.Altitude,
										location.Speed,
										location.Accuracy,
										GlobalVariables.accelerometer
										);
					writeLog(txt);

				}

			});




		}

		public void HandleProviderDisabled(object sender, ProviderDisabledEventArgs e)
		{
			Log.Debug(logTag, "Location provider disabled event raised");
		}

		public void HandleProviderEnabled(object sender, ProviderEnabledEventArgs e)
		{
			Log.Debug(logTag, "Location provider enabled event raised");
		}

		public void HandleStatusChanged(object sender, StatusChangedEventArgs e)
		{
			Log.Debug(logTag, "Location status changed, event raised");
		}

		#endregion


		public void OnAccuracyChanged(Sensor sensor, SensorStatus accuracy)
		{
			// We don't want to do anything here.
		}

		public void OnSensorChanged(SensorEvent e)
		{
			lock (_syncLock)
			{
				_sensorTextView.Text = string.Format("Accelerometer: x={0:f}, y={1:f}, z={2:f}", e.Values[0], e.Values[1], e.Values[2]);
				GlobalVariables.accelerometer = string.Format("x={0:f}, y={1:f}, z={2:f}", e.Values[0], e.Values[1], e.Values[2]);
			}
		}

		public void isUpdateUI() { 
			
		}

	}

	public static class BitmapHelpers
	{
		public static Bitmap LoadAndResizeBitmap(this string fileName, int width, int height)
		{
			// First we get the the dimensions of the file on disk
			BitmapFactory.Options options = new BitmapFactory.Options { InJustDecodeBounds = true };
			BitmapFactory.DecodeFile(fileName, options);

			// Next we calculate the ratio that we need to resize the image by
			// in order to fit the requested dimensions.
			int outHeight = options.OutHeight;
			int outWidth = options.OutWidth;
			int inSampleSize = 1;

			if (outHeight > height || outWidth > width)
			{
				inSampleSize = outWidth > outHeight
								   ? outHeight / height
								   : outWidth / width;
			}

			// Now we will load the image and have BitmapFactory resize it for us.
			options.InSampleSize = inSampleSize;
			options.InJustDecodeBounds = false;
			Bitmap resizedBitmap = BitmapFactory.DecodeFile(fileName, options);

			return resizedBitmap;
		}
	}


	public class App
	{
		public static Java.IO.File _file;
		public static Java.IO.File _dir;
		public static Bitmap bitmap;


		// events

		public event EventHandler<ServiceConnectedEventArgs> LocationServiceConnected = delegate { };

		// declarations
		protected readonly string logTag = "App";
		protected static LocationServiceConnection locationServiceConnection;

		// properties

		public static App Current
		{
			get { return current; }
		}
		private static App current;

		public LocationService LocationService
		{

			get
			{
				if (locationServiceConnection.Binder == null)
					throw new Java.Lang.Exception("Service not bound yet");
				// note that we use the ServiceConnection to get the Binder, and the Binder to get the Service here
				return locationServiceConnection.Binder.Service;
			}
		}

		#region Application context

		static App()
		{
			current = new App();
		}

		protected App()
		{
			// create a new service connection so we can get a binder to the service
			locationServiceConnection = new LocationServiceConnection(null);

			// this event will fire when the Service connection in the OnServiceConnected call 
			locationServiceConnection.ServiceConnected += (object sender, ServiceConnectedEventArgs e) =>
			{

				Log.Debug(logTag, "Service Connected");
				// we will use this event to notify MainActivity when to start updating the UI
				LocationServiceConnected(this, e);

			};
		}


		public static void StartLocationService()
		{
			// Starting a service like this is blocking, so we want to do it on a background thread
			new Task(() =>
			{

				// Start our main service
				Log.Debug("App", "Calling StartService");
				Application.Context.StartService(new Intent(Application.Context, typeof(LocationService)));

				// bind our service (Android goes and finds the running service by type, and puts a reference
				// on the binder to that service)
				// The Intent tells the OS where to find our Service (the Context) and the Type of Service
				// we're looking for (LocationService)
				Intent locationServiceIntent = new Intent(Application.Context, typeof(LocationService));
				Log.Debug("App", "Calling service binding");

				// Finally, we can bind to the Service using our Intent and the ServiceConnection we
				// created in a previous step.
				Application.Context.BindService(locationServiceIntent, locationServiceConnection, Bind.AutoCreate);

				GlobalVariables.isWriteLog = true;
			}).Start();

		}

		public static void StopLocationService()
		{
			// Check for nulls in case StartLocationService task has not yet completed.
			Log.Debug("App", "StopLocationService");

			// Unbind from the LocationService; otherwise, StopSelf (below) will not work:
			if (locationServiceConnection != null)
			{
				Log.Debug("App", "Unbinding from LocationService");
				Application.Context.UnbindService(locationServiceConnection);
			}

			// Stop the LocationService:
			if (Current.LocationService != null)
			{
				Log.Debug("App", "Stopping the LocationService");
				Current.LocationService.StopSelf();
			}

			GlobalVariables.isWriteLog = false;
		}

		#endregion

	}



}