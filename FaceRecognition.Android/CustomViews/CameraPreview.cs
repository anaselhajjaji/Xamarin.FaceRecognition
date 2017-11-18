using System;

using Android.Content;
using Android.Runtime;
using Android.Views;
using Android.Util;
using Android.Gms.Vision;
using Android.Graphics;

namespace FaceRecognition.Droid
{
    public sealed class CameraPreview : ViewGroup, ISurfaceHolderCallback
    {
        static readonly String TAG = "CameraPreview";

        Context appContext;
        SurfaceView surfaceView;
        bool startRequested;
        bool surfaceAvailable;
        CameraSource theCameraSource;
        FaceOverlay theOverlay;

        public CameraPreview(Context context, IAttributeSet attrs) : base(context, attrs)
        {
            appContext = context;
            startRequested = false;
            surfaceAvailable = false;

            surfaceView = new SurfaceView(context);
            surfaceView.Holder.AddCallback(this);

            AddView(surfaceView);
        }

        public void Start(CameraSource cameraSource)
        {
            if (cameraSource == null)
            {
                Stop();
            }

            theCameraSource = cameraSource;

            if (theCameraSource != null)
            {
                startRequested = true;
                StartIfReady();
            }
        }

        public void Start(CameraSource cameraSource, FaceOverlay overlay)
        {
            theOverlay = overlay;
            Start(cameraSource);
        }

        public void Stop()
        {
            if (theCameraSource != null)
            {
                theCameraSource.Stop();
            }
        }

        public void Release()
        {
            if (theCameraSource != null)
            {
                theCameraSource.Release();
                theCameraSource = null;
            }
        }
        private void StartIfReady()
        {
            if (startRequested && surfaceAvailable)
            {
                theCameraSource.Start(surfaceView.Holder);
                if (theOverlay != null)
                {
                    var size = theCameraSource.PreviewSize;
                    var min = Math.Min(size.Width, size.Height);
                    var max = Math.Max(size.Width, size.Height);
                    if (IsPortraitMode())
                    {
                        // Swap width and height sizes when in portrait, since it will be rotated by
                        // 90 degrees
                        theOverlay.SetCameraInfo(min, max, theCameraSource.CameraFacing);
                    }
                    else
                    {
                        theOverlay.SetCameraInfo(max, min, theCameraSource.CameraFacing);
                    }
                    theOverlay.Clear();
                }
                startRequested = false;
            }
        }

        private bool IsPortraitMode()
        {
            var orientation = appContext.Resources.Configuration.Orientation;
            if (orientation == Android.Content.Res.Orientation.Landscape)
            {
                return false;
            }
            if (orientation == Android.Content.Res.Orientation.Portrait)
            {
                return true;
            }

            Log.Debug(TAG, "isPortraitMode returning false by default");
            return false;
        }

        public void SurfaceChanged(ISurfaceHolder holder, [GeneratedEnum] Format format, int width, int height)
        {
            Log.Debug(TAG, "Surface changed.");
        }

        public void SurfaceCreated(ISurfaceHolder holder)
        {
            surfaceAvailable = true;

            try
            {
                StartIfReady();
            }
            catch (Exception e)
            {
                Log.Error(TAG, "Could not start camera source.", e);
            }
        }

        public void SurfaceDestroyed(ISurfaceHolder holder)
        {
            surfaceAvailable = false;
        }

        protected override void OnLayout(bool changed, int l, int t, int r, int b)
        {
            int width = 320;
            int height = 240;
            if (theCameraSource != null)
            {
                var size = theCameraSource.PreviewSize;
                if (size != null)
                {
                    width = size.Width;
                    height = size.Height;
                }
            }

            // Swap width and height sizes when in portrait, since it will be rotated 90 degrees
            if (IsPortraitMode())
            {
                int tmp = width;
                width = height;
                height = tmp;
            }

            int layoutWidth = r - l;
            int layoutHeight = b - t;

            // Computes height and width for potentially doing fit width.
            int childWidth = layoutWidth;
            int childHeight = (int)(((float)layoutWidth / (float)width) * height);

            // If height is too tall using fit width, does fit height instead.
            if (childHeight > layoutHeight)
            {
                childHeight = layoutHeight;
                childWidth = (int)(((float)layoutHeight / (float)height) * width);
            }

            for (int i = 0; i < ChildCount; ++i)
            {

                GetChildAt(i).Layout(0, 0, childWidth, childHeight);
            }

            try
            {
                StartIfReady();
            }
            catch (Exception e)
            {
                Log.Error(TAG, "Could not start camera source.", e);
            }
        }
    }
}