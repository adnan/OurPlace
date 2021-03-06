#region copyright
/*
    OurPlace is a mobile learning platform, designed to support communities
    in creating and sharing interactive learning activities about the places they care most about.
    https://github.com/GSDan/OurPlace
    Copyright (C) 2018 Dan Richardson

    This program is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    This program is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with this program.  If not, see https://www.gnu.org/licenses.
*/
#endregion
using Android.App;
using Android.Content;
using Android.Hardware;
using Android.Media;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Java.Util;
using OurPlace.Android.Activities;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;

#pragma warning disable CS0618 // Camera 1 API is obsolete, use only when Camera2 isn't available

namespace OurPlace.Android.Fragments
{
    public class Camera1Fragment : Fragment, Camera.IPictureCallback
    {
        private Camera mCamera;
        private CameraPreview mPreview;

        public static Camera1Fragment NewInstance()
        {
            Camera1Fragment fragment = new Camera1Fragment();
            return fragment;
        }

        public override View OnCreateView(LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState)
        {
            View view = inflater.Inflate(Resource.Layout.Camera1Fragment, container, false);

            bool opened = SafeCameraOpenInView(view);

            if (!opened)
            {
                Console.WriteLine("Camera failed to open");
                return view;
            }

            Button captureBtn = view.FindViewById<Button>(Resource.Id.button_capture);
            captureBtn.Click += (a, e) =>
            {
                mCamera.TakePicture(null, null, this);
            };

            return view;
        }

        public override void OnViewCreated(View view, Bundle savedInstanceState)
        {
            base.OnViewCreated(view, savedInstanceState);

            CameraActivity thisAct = ((CameraActivity)Activity);
            thisAct.LoadIfPhotoMatch(view);
        }

        private bool SafeCameraOpenInView(View view)
        {
            ReleaseCameraAndPreview();
            mCamera = GetCameraInstance();

            if (mCamera == null)
            {
                return false;
            }

            mPreview = new CameraPreview(Activity.BaseContext, mCamera, view);
            FrameLayout preview = view.FindViewById<FrameLayout>(Resource.Id.camera_preview);
            preview.AddView(mPreview, 0);
            mPreview.StartCameraPreview();

            return true;
        }

        public static Camera GetCameraInstance(int target = 1600 * 1200, bool video = false) //2mp
        {
            Camera c = null;
            try
            {
                c = Camera.Open();
                Camera.Parameters camParams = c.GetParameters();
                IList<Camera.Size> sizes = (video) ? camParams.SupportedVideoSizes :
                                camParams.SupportedPictureSizes;

                Camera.Size size = sizes[0];
                int currentDiff = System.Math.Abs(size.Height * size.Width - target);

                foreach (Camera.Size thisSize in sizes)
                {
                    int thisDiff = System.Math.Abs(thisSize.Height * thisSize.Width - target);
                    if (thisDiff >= currentDiff)
                    {
                        continue;
                    }

                    size = thisSize;
                    currentDiff = thisDiff;
                }
                camParams.SetPictureSize(size.Width, size.Height);
                c.SetParameters(camParams);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
            return c;
        }

        public override void OnDestroy()
        {
            base.OnDestroy();
            ReleaseCameraAndPreview();
        }

        private void ReleaseCameraAndPreview()
        {
            if (mCamera != null)
            {
                mCamera.StopPreview();
                mCamera.Release();
                mCamera = null;
            }

            if (mPreview == null)
            {
                return;
            }

            mPreview.DestroyDrawingCache();
            mPreview.Camera = null;
        }

        public static global::Android.Graphics.Bitmap Rotate(global::Android.Graphics.Bitmap bitmap, int degree)
        {
            int w = bitmap.Width;
            int h = bitmap.Height;

            global::Android.Graphics.Matrix mtx = new global::Android.Graphics.Matrix();
            mtx.PostRotate(degree);

            return global::Android.Graphics.Bitmap.CreateBitmap(bitmap, 0, 0, w, h, mtx, true);
        }

        public void OnPictureTaken(byte[] data, Camera camera)
        {
            string id = null;

            if (((CameraActivity)Activity).activityId != -1)
            {
                id = ((CameraActivity)Activity).activityId.ToString();
            }

            string filename = Path.Combine(
                Common.LocalData.Storage.GetCacheFolder(id),
                DateTime.Now.ToString("MM-dd-yyyy-HH-mm-ss-fff", CultureInfo.InvariantCulture) + ".jpg");
            File.WriteAllBytes(filename, data);

            global::Android.Graphics.Bitmap bitmap = global::Android.Graphics.BitmapFactory.DecodeFile(filename);
            ExifInterface exif = new ExifInterface(filename);
            string orientation = exif.GetAttribute(ExifInterface.TagOrientation);

            switch (orientation)
            {
                case "6":
                    bitmap = Rotate(bitmap, 90);
                    break;
                case "8":
                    bitmap = Rotate(bitmap, 270);
                    break;
                case "3":
                    bitmap = Rotate(bitmap, 180);
                    break;
            }

            var suppress = AndroidUtils.WriteBitmapToFile(filename, bitmap);
            OnPause();
            ((CameraActivity)Activity).ReturnWithFile(filename);
        }
    }

    public sealed class CameraPreview : SurfaceView, ISurfaceHolderCallback
    {
        public Camera Camera;
        private readonly Context context;
        private Camera.Size previewSize;
        private IList<Camera.Size> mSupportedPreviewSizes;
        private readonly View cameraView;
        private readonly bool isVideo;

        public CameraPreview(Context context, Camera camera, View cameraView, bool video = false) : base(context)
        {
            this.cameraView = cameraView;
            this.context = context;
            SetCamera(camera);
            Holder.AddCallback(this);
            Holder.SetKeepScreenOn(true);
            isVideo = video;
        }

        public void StartCameraPreview()
        {
            try
            {
                Camera.SetPreviewDisplay(Holder);
                Camera.StartPreview();
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
        }

        private void SetCamera(Camera camera)
        {
            Camera = camera;
            mSupportedPreviewSizes = Camera.GetParameters().SupportedPreviewSizes;
            RequestLayout();
        }

        public void SurfaceCreated(ISurfaceHolder holder)
        {
            try
            {
                Camera.SetPreviewDisplay(holder);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
        }

        public void SurfaceDestroyed(ISurfaceHolder holder)
        {
            Camera?.StopPreview();
        }

        public void SurfaceChanged(ISurfaceHolder holder, [GeneratedEnum] global::Android.Graphics.Format format, int width, int height)
        {
            if (Holder.Surface == null)
            {
                return;
            }

            try
            {
                Camera.Parameters parameters = Camera.GetParameters();
                parameters.FocusMode = Camera.Parameters.FocusModeContinuousPicture;
                parameters.PreviewFrameRate = 30;

                if (previewSize != null)
                {
                    Camera.Size tempPreviewSize = previewSize;
                    parameters.SetPreviewSize(tempPreviewSize.Width, tempPreviewSize.Height);
                }

                Camera.SetParameters(parameters);
                Camera.StartPreview();
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
        }

        protected override void OnMeasure(int widthMeasureSpec, int heightMeasureSpec)
        {
            int width = ResolveSize(SuggestedMinimumWidth, widthMeasureSpec);
            int height = ResolveSize(SuggestedMinimumHeight, heightMeasureSpec);
            SetMeasuredDimension(width, height);

            if (mSupportedPreviewSizes != null)
            {
                previewSize = GetOptimalPreviewSize(mSupportedPreviewSizes, width, height);
            }
        }

        protected override void OnLayout(bool changed, int left, int top, int right, int bottom)
        {
            if (!changed)
            {
                return;
            }

            int width = right - left;
            int height = bottom - top;
            int previewWidth = width;
            int previewHeight = height;

            if (previewSize != null)
            {
                Display display = context.GetSystemService(Context.WindowService).JavaCast<IWindowManager>().DefaultDisplay;

                switch (display.Rotation)
                {
                    case SurfaceOrientation.Rotation0:
                        previewWidth = previewSize.Height;
                        previewHeight = previewSize.Width;
                        break;
                    case SurfaceOrientation.Rotation90:
                        previewWidth = previewSize.Width;
                        previewHeight = previewSize.Height;
                        break;
                    case SurfaceOrientation.Rotation180:
                        previewWidth = previewSize.Height;
                        previewHeight = previewSize.Width;
                        break;
                    case SurfaceOrientation.Rotation270:
                        previewWidth = previewSize.Width;
                        previewHeight = previewSize.Height;
                        break;
                }
            }

            int scaledChildHeight = previewHeight * width / previewWidth;
            cameraView.Layout(0, height - scaledChildHeight, width, height);

            Camera.Parameters camParams = Camera.GetParameters();

            if (base.Resources.Configuration.Orientation == global::Android.Content.Res.Orientation.Landscape)
            {
                camParams.Set("orientation", "landscape");
                camParams.SetRotation(0);
                Camera.SetDisplayOrientation(0);
            }
            else
            {
                camParams.Set("orientation", "portrait");
                camParams.SetRotation(90);
                Camera.SetDisplayOrientation(90);
            }

            if (isVideo && camParams.SupportedFocusModes.Contains(Camera.Parameters.FocusModeContinuousVideo))
            {
                camParams.FocusMode = Camera.Parameters.FocusModeContinuousVideo;
            }
            else if (!isVideo && camParams.SupportedFocusModes.Contains(Camera.Parameters.FocusModeContinuousPicture))
            {
                camParams.FocusMode = Camera.Parameters.FocusModeContinuousPicture;
            }
            else if (camParams.SupportedFocusModes.Contains(Camera.Parameters.FocusModeAuto))
            {
                camParams.FocusMode = Camera.Parameters.FocusModeAuto;
            }

            Camera.SetParameters(camParams);
        }

        private Camera.Size GetOptimalPreviewSize(IList<Camera.Size> sizes, int width, int height)
        {
            // Collect the supported resolutions that are at least as big as the preview Surface
            var bigEnough = new List<Camera.Size>();
            // Collect the supported resolutions that are smaller than the preview Surface
            var notBigEnough = new List<Camera.Size>();
            int aspectWidth = (isVideo) ? 16 : 4;
            int aspectHeight = (isVideo) ? 9 : 3;
            int maxWidth = (isVideo) ? 1300 : 2048;
            int maxHeight = (isVideo) ? 1300 : 2048;

            foreach (Camera.Size option in sizes)
            {
                if ((option.Width > maxWidth) || (option.Height > maxHeight) ||
                    option.Height != option.Width * aspectHeight / aspectWidth)
                {
                    continue;
                }

                if (option.Width >= width &&
                    option.Height >= height)
                {
                    bigEnough.Add(option);
                }
                else
                {
                    notBigEnough.Add(option);
                }
            }

            // Pick the smallest of those big enough. If there is no one big enough, pick the
            // largest of those not big enough.
            if (bigEnough.Count > 0)
            {
                return (Camera.Size)Collections.Min(bigEnough, new CompareCamSizesByArea());
            }

            if (notBigEnough.Count > 0)
            {
                return (Camera.Size)Collections.Max(notBigEnough, new CompareCamSizesByArea());
            }

            return sizes[0];
        }
    }

    public class CompareCamSizesByArea : Java.Lang.Object, IComparator
    {
        public int Compare(Java.Lang.Object lhs, Java.Lang.Object rhs)
        {
            var lhsSize = (Camera.Size)lhs;
            var rhsSize = (Camera.Size)rhs;
            // We cast here to ensure the multiplications won't overflow
            return Java.Lang.Long.Signum((long)lhsSize.Width * lhsSize.Height - (long)rhsSize.Width * rhsSize.Height);
        }
    }
}
#pragma warning restore CS0618 // Type or member is obsolete