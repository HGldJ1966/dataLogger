using Android.App;
using Android.Widget;
using Android.OS;
using Java.IO;
using Android.Content;
using Android.Content.PM;
using Android.Graphics;
using System.Collections.Generic;
using Android.Provider;
using Java.Lang;
using Android.Net;
using Android.Locations;
using System.Linq;


namespace DataLogger.Droid
{
	[Activity(Label = "DataLogger", MainLauncher = true, Icon = "@mipmap/icon")]



	public class MainActivity : Activity, ILocationListener
	{

		ImageView _imageView;
		//static readonly string TAG = "X:" + typeof(Activity).Name;
		//TextView _addressText;
		Location _currentLocation;
		LocationManager _locationManager;
		string _locationProvider;
		TextView _locationText;

		protected override void OnCreate(Bundle savedInstanceState)
		{
			base.OnCreate(savedInstanceState);

			// Set our view from the "main" layout resource
			SetContentView(Resource.Layout.Main);


			// Set our view from the "Main" layout resource
			SetContentView(Resource.Layout.Main);

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


			//Picture
			if (IsThereAnAppToTakePictures())
			{
				CreateDirectoryForPictures();

				Button button = FindViewById<Button>(Resource.Id.bnt_takePic);
				_imageView = FindViewById<ImageView>(Resource.Id.imageView1);
				button.Click += TakeAPicture;
			}

			//LOCATION
			//_addressText = FindViewById<TextView>(Resource.Id.textView_address);
			_locationText = FindViewById<TextView>(Resource.Id.textView_location);


			InitializeLocationManager();

		}

		private void spinner_ItemSelected(object sender, AdapterView.ItemSelectedEventArgs e)
		{
			Spinner spinner = (Spinner)sender;

			string toast = string.Format("{0}", spinner.GetItemAtPosition(e.Position));
			Toast.MakeText(this, toast, ToastLength.Long).Show();
		}

		private void CreateDirectoryForPictures()
		{
			App._dir = new File(
				Environment.GetExternalStoragePublicDirectory(
					Environment.DirectoryPictures), "CameraAppDemo");
			if (!App._dir.Exists())
			{
				App._dir.Mkdirs();
			}
		}

		private bool IsThereAnAppToTakePictures()
		{
			Intent intent = new Intent(MediaStore.ActionImageCapture);
			IList<ResolveInfo> availableActivities =
				PackageManager.QueryIntentActivities(intent, PackageInfoFlags.MatchDefaultOnly);
			return availableActivities != null && availableActivities.Count > 0;
		}

		private void TakeAPicture(object sender, System.EventArgs eventArgs)
		{
			Intent intent = new Intent(MediaStore.ActionImageCapture);

			App._file = new File(App._dir, String.Format("myPhoto_{0}.jpg"));

			intent.PutExtra(MediaStore.ExtraOutput, Uri.FromFile(App._file));
			StartActivityForResult(intent, 0);
		}

		protected override void OnActivityResult(int requestCode, Result resultCode, Intent data)
		{
			base.OnActivityResult(requestCode, resultCode, data);

			// Make it available in the gallery

			Intent mediaScanIntent = new Intent(Intent.ActionMediaScannerScanFile);
			Uri contentUri = Uri.FromFile(App._file);
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
			System.GC.Collect();
		}

		void InitializeLocationManager()
		{
			string toast2 = string.Format("initial location");
			Toast.MakeText(this, toast2, ToastLength.Long).Show();
			_locationManager = (LocationManager)GetSystemService(LocationService);
			Criteria criteriaForLocationService = new Criteria
			{
				Accuracy = Accuracy.Fine
			};
			IList<string> acceptableLocationProviders = _locationManager.GetProviders(criteriaForLocationService, true);

			if (acceptableLocationProviders.Any())
			{
				_locationProvider = acceptableLocationProviders.First();
			}
			else
			{
				_locationProvider = string.Empty;
			}
			//Log.Debug(TAG, "Using " + _locationProvider + ".");
		}

		public void OnLocationChanged(Location location)
		{

			_currentLocation = location;
			if (_currentLocation == null)
			{
				_locationText.Text = "Unable to determine your location. Try again in a short while.";
			}
			else
			{
				_locationText.Text = string.Format("{0:f6},{1:f6}", _currentLocation.Latitude, _currentLocation.Longitude);
				//Address address = await ReverseGeocodeCurrentLocation();
				//DisplayAddress(address);
			}
		}

		public void OnProviderDisabled(string provider) { 
		
		}

		public void OnProviderEnabled(string provider) { }

		public void OnStatusChanged(string provider, Availability status, Bundle extras) { }

		protected override void OnResume()
		{
			base.OnResume();
			_locationManager.RequestLocationUpdates(_locationProvider, 0, 0, this);
		}

		protected override void OnPause()
		{
			base.OnPause();
			_locationManager.RemoveUpdates(this);
		}
	}

	public static class App
	{
		public static File _file;
		public static File _dir;
		public static Bitmap bitmap;
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


}