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
using Android.Content.PM;
using Android.Graphics;
using Android.OS;
using Android.Support.V7.App;
using Android.Views;
using Android.Widget;
using ColorPicker;
using FFImageLoading;
using FFImageLoading.Views;
using Microsoft.AppCenter.Analytics;
using Newtonsoft.Json;
using OurPlace.Common.Models;
using System;
using System.Collections.Generic;
using System.IO;
using Debug = System.Diagnostics.Debug;

namespace OurPlace.Android.Activities
{
    public class PaintView : View
    {
        private Bitmap mBitmap;
        private Canvas mCanvas;
        private readonly global::Android.Graphics.Path mPath;
        private readonly Paint mBitmapPaint;
        public Paint MPaint;

        public PaintView(Context c, global::Android.Util.IAttributeSet att) : base(c, att)
        {
            mPath = new global::Android.Graphics.Path();
            mBitmapPaint = new Paint(PaintFlags.AntiAlias);
        }

        protected override void OnSizeChanged(int w, int h, int oldWidth, int oldHeight)
        {
            base.OnSizeChanged(w, h, oldWidth, oldHeight);
            mBitmap = Bitmap.CreateBitmap(w, h, Bitmap.Config.Argb8888);
            mCanvas = new Canvas(mBitmap);
        }

        protected override void OnDraw(Canvas canvas)
        {
            base.OnDraw(canvas);
            canvas.DrawBitmap(mBitmap, 0, 0, mBitmapPaint);
            canvas.DrawPath(mPath, MPaint);
        }

        private float mX, mY;
        private const float TouchTolerance = 4;

        private void Touch_start(float x, float y)
        {
            mPath.Reset();
            mPath.MoveTo(x, y);
            mX = x;
            mY = y;
        }

        private void Touch_move(float x, float y)
        {
            float dx = Math.Abs(x - mX);
            float dy = Math.Abs(y - mY);
            if (!(dx >= TouchTolerance) && !(dy >= TouchTolerance))
            {
                return;
            }

            mPath.QuadTo(mX, mY, (x + mX) / 2, (y + mY) / 2);
            mX = x;
            mY = y;
        }

        private void touch_up()
        {
            mPath.LineTo(mX, mY);
            // commit the path to our offscreen
            mCanvas.DrawPath(mPath, MPaint);
            // kill this so we don't double draw
            mPath.Reset();
        }

        public override bool OnTouchEvent(MotionEvent e)
        {
            float x = e.GetX();
            float y = e.GetY();

            switch (e.Action)
            {
                case MotionEventActions.Down:
                    Touch_start(x, y);
                    Invalidate();
                    break;
                case MotionEventActions.Move:
                    Touch_move(x, y);
                    Invalidate();
                    break;
                case MotionEventActions.Up:
                    touch_up();
                    Invalidate();
                    break;
            }
            return true;
        }
    }

    [Activity(Label = "Painting", Theme = "@style/OurPlaceActionBar", ScreenOrientation = ScreenOrientation.Portrait)]
    public class DrawingActivity : AppCompatActivity, ColorPickerView.OnColorChangedListener
    {
        private PaintView mv;
        private Paint mPaint;
        private ImageViewAsync bgImage;
        private ColorPickerView colorPickerView;
        private const int Save = Menu.First;
        public LearningTask LearningTask;
        public string PreviousImage;

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            SetContentView(Resource.Layout.PaintActivity);

            string thisJsonData = Intent.GetStringExtra("JSON") ?? "";
            LearningTask = JsonConvert.DeserializeObject<LearningTask>(thisJsonData, new JsonSerializerSettings { TypeNameHandling = TypeNameHandling.Auto });

            // Used if the task requires drawing on top of 
            // a photo taken in another task
            PreviousImage = Intent.GetStringExtra("PREVIOUS_PHOTO");

            SupportActionBar.Title = LearningTask.Description;

            mPaint = new Paint { AntiAlias = true, Color = Color.Black };
            mPaint.SetStyle(Paint.Style.Stroke);
            mPaint.StrokeJoin = (Paint.Join.Round);
            mPaint.StrokeCap = (Paint.Cap.Round);
            mPaint.StrokeWidth = 20;

            mv = FindViewById<PaintView>(Resource.Id.paintview);
            mv.MPaint = mPaint;
            mv.DrawingCacheEnabled = true;

            bgImage = FindViewById<ImageViewAsync>(Resource.Id.paintBackground);

            if (LearningTask.TaskType.IdName == "DRAW_PHOTO")
            {
                //used for accessing cache folder
                int activityId = Intent.GetIntExtra("ACTIVITY_ID", -1);

                if (string.IsNullOrWhiteSpace(PreviousImage))
                {
                    AndroidUtils.LoadActivityImageIntoView(bgImage, LearningTask.JsonData, activityId, 500);
                }
                else
                {
                    ImageService.Instance.LoadFile(PreviousImage)
                        .DownSample(500)
                        .Into(bgImage);
                }
            }
            else
            {
                bgImage.SetBackgroundColor(Color.White);
            }

            colorPickerView = FindViewById<ColorPickerView>(Resource.Id.color_picker_view);
            colorPickerView.setOnColorChangedListener(this);

            ImageButton saveBtn = FindViewById<ImageButton>(Resource.Id.saveBtn);
            saveBtn.Click += SaveBtn_Click;
        }

        public void ReturnWithImage(string imagePath)
        {
            Dictionary<string, string> properties = new Dictionary<string, string>
            {
                {"TaskId", LearningTask?.Id.ToString() }
            };
            Analytics.TrackEvent("DrawingActivity", properties);

            Intent myIntent = new Intent(this, typeof(ActTaskListActivity));

            Debug.Assert(LearningTask != null, nameof(LearningTask) + " != null");
            myIntent.PutExtra("TASK_ID", LearningTask.Id);
            myIntent.PutExtra("FILE_PATH", imagePath);
            SetResult(Result.Ok, myIntent);
            Finish();
        }

        private void SaveBtn_Click(object sender, EventArgs e)
        {
            Bitmap drawingLayer = mv.DrawingCache;

            Bitmap bgLayer = Bitmap.CreateBitmap(bgImage.Width, bgImage.Height, Bitmap.Config.Argb8888);
            Canvas c = new Canvas(bgLayer);
            bgImage.Layout(bgImage.Left, bgImage.Top, bgImage.Right, bgImage.Bottom);
            bgImage.Draw(c);

            Bitmap final = Bitmap.CreateBitmap(bgLayer.Width, bgLayer.Height, bgLayer.GetConfig());
            Canvas canvas = new Canvas(final);
            canvas.DrawBitmap(bgLayer, new Matrix(), null);
            canvas.DrawBitmap(drawingLayer, 0, 0, null);

            const int maxSize = 1280;
            int outWidth;
            int outHeight;
            int inWidth = final.Width;
            int inHeight = final.Height;
            if (inWidth > inHeight)
            {
                outWidth = maxSize;
                outHeight = (inHeight * maxSize) / inWidth;
            }
            else
            {
                outHeight = maxSize;
                outWidth = (inWidth * maxSize) / inHeight;
            }

            final = Bitmap.CreateScaledBitmap(final, outWidth, outHeight, false);

            string sdCardPath = GetExternalFilesDir(null).AbsolutePath;
            string filePath = System.IO.Path.Combine(sdCardPath, DateTime.UtcNow.ToString("MM-dd-yyyy-HH-mm-ss-fff") + ".jpg");

            var stream = new FileStream(filePath, FileMode.Create);
            final.Compress(Bitmap.CompressFormat.Jpeg, 80, stream);
            stream.Close();

            ReturnWithImage(filePath);
        }

        public override bool OnOptionsItemSelected(IMenuItem item)
        {
            mPaint.SetXfermode(null);
            mPaint.Alpha = 0xFF;

            switch (item.ItemId)
            {
                case Save:
                    global::Android.Support.V7.App.AlertDialog.Builder editalert = new global::Android.Support.V7.App.AlertDialog.Builder(this);
                    editalert.SetTitle("Please Enter the name with which you want to Save");
                    EditText input = new EditText(this);
                    LinearLayout.LayoutParams lp = new LinearLayout.LayoutParams(
                            ViewGroup.LayoutParams.MatchParent,
                            ViewGroup.LayoutParams.MatchParent);
                    input.LayoutParameters = lp;
                    editalert.SetView(input);
                    editalert.SetPositiveButton("OK", delegate
                    {

                        string name = input.Text;
                        Bitmap bitmap = mv.DrawingCache;

                        Bitmap image = Bitmap.CreateBitmap(bgImage.LayoutParameters.Width, bgImage.LayoutParameters.Height, Bitmap.Config.Argb8888);
                        Canvas c = new Canvas(image);
                        bgImage.Layout(bgImage.Left, bgImage.Top, bgImage.Right, bgImage.Bottom);
                        bgImage.Draw(c);

                        string sdCardPath = global::Android.OS.Environment.GetExternalStoragePublicDirectory(global::Android.OS.Environment.DirectoryDownloads).AbsolutePath;
                        string filePath = System.IO.Path.Combine(sdCardPath, name + ".png");

                        try
                        {
                            var stream = new FileStream(filePath, FileMode.Create);
                            bitmap.Compress(Bitmap.CompressFormat.Jpeg, 80, stream);
                            stream.Close();
                        }
                        catch (Exception e)
                        {
                            System.Console.WriteLine(e.Message);
                        }
                        finally
                        {
                            mv.DrawingCacheEnabled = false;
                        }
                    });
                    editalert.Show();
                    return true;
            }

            return base.OnOptionsItemSelected(item);
        }

        public void onColorChanged(int color)
        {
            mPaint.Color = new Color(color);
        }
    }
}