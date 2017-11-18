using System;
using System.Threading.Tasks;

using Android.App;
using Android.OS;
using Android.Gms.Vision;
using Android.Support.V4.App;
using Android.Support.V7.App;
using Android.Util;
using Android;
using Android.Support.Design.Widget;
using Android.Gms.Vision.Faces;
using Android.Runtime;
using static Android.Gms.Vision.MultiProcessor;
using Android.Content.PM;
using Android.Gms.Common;

namespace FaceRecognition.Droid
{
    [Activity(MainLauncher = true, ScreenOrientation = ScreenOrientation.FullSensor)]
    public class MainActivity : AppCompatActivity, IFactory
    {
        static readonly string TAG = "FaceTracker";
        static readonly int RC_HANDLE_GMS = 9001;
        // permission request codes need to be < 256
        static readonly int RC_HANDLE_CAMERA_PERM = 2;

        CameraSource cameraSource;
        CameraPreview cameraPreview;
        FaceOverlay faceOverlay;

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            // Set our view from the "main" layout resource
            SetContentView(Resource.Layout.Main);
            cameraPreview = FindViewById<CameraPreview>(Resource.Id.preview);
            faceOverlay = FindViewById<FaceOverlay>(Resource.Id.faceOverlay);

            // Check permission for camera
            if (ActivityCompat.CheckSelfPermission(this, Manifest.Permission.Camera) == Permission.Granted)
            {
                CreateCamera();
            }
            else { RequestCameraPermission(); }
        }

        protected override void OnResume()
        {
            base.OnResume();
            StartCamera();
        }

        protected override void OnPause()
        {
            base.OnPause();
            cameraPreview.Stop();
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            if (cameraSource != null)
            {
                cameraSource.Release();
            }
        }

        void RequestCameraPermission()
        {
            Log.Warn(TAG, "Camera permission is not granted. Requesting permission");

            var permissions = new string[] { Manifest.Permission.Camera };

            if (!ActivityCompat.ShouldShowRequestPermissionRationale(this,
                    Manifest.Permission.Camera))
            {
                ActivityCompat.RequestPermissions(this, permissions, RC_HANDLE_CAMERA_PERM);
                return;
            }

            Snackbar.Make(faceOverlay, Resource.String.permission_camera_rationale,
                    Snackbar.LengthIndefinite)
                    .SetAction(Resource.String.ok, (o) => { ActivityCompat.RequestPermissions(this, permissions, RC_HANDLE_CAMERA_PERM); })
                    .Show();
        }

        /// <summary>
        /// Creates and starts the camera.
        /// </summary>
        void CreateCamera()
        {
            var context = Application.Context;
            FaceDetector detector = new FaceDetector.Builder(context)
                    .SetClassificationType(ClassificationType.All)
                    .Build();

            detector.SetProcessor(
                    new MultiProcessor.Builder(this)
                            .Build());

            if (!detector.IsOperational)
            {
                // Note: The first time that an app using face API is installed on a device, GMS will
                // download a native library to the device in order to do detection.  Usually this
                // completes before the app is run for the first time.  But if that download has not yet
                // completed, then the above call will not detect any faces.
                //
                // isOperational() can be used to check if the required native library is currently
                // available.  The detector will automatically become operational once the library
                // download completes on device.
                Log.Warn(TAG, "Face detector dependencies are not yet available.");
            }

            cameraSource = new CameraSource.Builder(context, detector)
                    .SetRequestedPreviewSize(640, 480)
                                            .SetFacing(CameraFacing.Back)
                    .SetRequestedFps(30.0f)
                    .Build();
        }

        /// <summary>
        /// Starts or restarts the camera source, if it exists.  If the camera source doesn't exist yet
        ///  (e.g., because onResume was called before the camera source was created), this will be called
        /// again when the camera source is created.
        /// </summary>
        void StartCamera()
        {
            // check that the device has play services available.
            int code = GoogleApiAvailability.Instance.IsGooglePlayServicesAvailable(
                    this.ApplicationContext);
            if (code != ConnectionResult.Success)
            {
                Dialog dlg =
                        GoogleApiAvailability.Instance.GetErrorDialog(this, code, RC_HANDLE_GMS);
                dlg.Show();
            }

            if (cameraSource != null)
            {
                try
                {
                    cameraPreview.Start(cameraSource, faceOverlay);
                }
                catch (System.Exception e)
                {
                    Log.Error(TAG, "Unable to start camera source.", e);
                    cameraSource.Release();
                    cameraSource = null;
                }
            }
        }

        public Tracker Create(Java.Lang.Object item)
        {
            return new GraphicFaceTracker(faceOverlay, cameraSource);
        }

        public override void OnRequestPermissionsResult(int requestCode, string[] permissions, [GeneratedEnum] Permission[] grantResults)
        {
            if (requestCode != RC_HANDLE_CAMERA_PERM)
            {
                Log.Debug(TAG, "Got unexpected permission result: " + requestCode);
                base.OnRequestPermissionsResult(requestCode, permissions, grantResults);

                return;
            }

            if (grantResults.Length != 0 && grantResults[0] == Permission.Granted)
            {
                Log.Debug(TAG, "Camera permission granted - initialize the camera source");
                // we have permission, so create the camerasource
                CreateCamera();
                return;
            }

            Log.Error(TAG, "Permission not granted: results len = " + grantResults.Length +
                    " Result code = " + (grantResults.Length > 0 ? grantResults[0].ToString() : "(empty)"));
            
            var builder = new Android.Support.V7.App.AlertDialog.Builder(this);
            builder.SetTitle("LiveCam")
                    .SetMessage(Resource.String.no_camera_permission)
                    .SetPositiveButton(Resource.String.ok, (o, e) => Finish())
                    .Show();

        }
    }

    class GraphicFaceTracker : Tracker, CameraSource.IPictureCallback
    {
        FaceOverlay overlay;
        FaceGraphic faceGraphic;
        CameraSource cameraSource;

        public GraphicFaceTracker(FaceOverlay theOverlay, CameraSource theCameraSource = null)
        {
            overlay = theOverlay;
            faceGraphic = new FaceGraphic(overlay);
            cameraSource = theCameraSource;
        }

        public override void OnNewItem(int id, Java.Lang.Object item)
        {
            faceGraphic.SetId(id);
            /* TODO If we want to take a picture once a face is detected
            if (cameraSource != null)
                cameraSource.TakePicture(null, this);*/
        }

        public override void OnUpdate(Detector.Detections detections, Java.Lang.Object item)
        {
            var face = item as Face;
            overlay.Add(faceGraphic);
            faceGraphic.UpdateFace(face);

        }

        public override void OnMissing(Detector.Detections detections)
        {
            overlay.Remove(faceGraphic);
        }

        public override void OnDone()
        {
            overlay.Remove(faceGraphic);
        }

        public void OnPictureTaken(byte[] data)
        {
            // TODO we can analyse the face here
            Console.WriteLine("face detected: ");
        }
    }

}


