﻿using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.OS;
using Android.Support.Design.Widget;
using Android.Support.V7.Widget;
using Android.Support.V7.Widget.Helper;
using Android.Widget;
using Newtonsoft.Json;
using OurPlace.Android.Adapters;
using OurPlace.Common.Models;
using System;

namespace OurPlace.Android.Activities.Create
{
    [Activity(Label = "Edit Collection", Theme = "@style/OurPlaceActionBar", ParentActivity = typeof(MainActivity), LaunchMode = LaunchMode.SingleTask)]
    public class CreateCollectionOverviewActivity : Activity
    {
        private ActivityCollection newCollection;
        private bool editingSubmitted;
        private ActivityCollectionAdapter adapter;
        private RecyclerView recyclerView;
        private RecyclerView.LayoutManager layoutManager;
        private TextView fabPrompt;
        private const int editCollectionIntent = 199;
        private const int addActivityIntent = 200;
        private const int viewLocIntent = 200;

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            SetContentView(Resource.Layout.CreateCollectionActivity);

            editingSubmitted = Intent.GetBooleanExtra("EDITING_SUBMITTED", false);
            string jsonData = Intent.GetStringExtra("JSON") ?? "";
            newCollection = JsonConvert.DeserializeObject<ActivityCollection>(jsonData, new JsonSerializerSettings { TypeNameHandling = TypeNameHandling.Auto });

            adapter = new ActivityCollectionAdapter(this, newCollection, SaveProgress);
            adapter.DeleteItemClick += Adapter_DeleteItemClick;
            adapter.EditCollectionClick += Adapter_EditCollectionClick;
            adapter.FinishClick += Adapter_FinishClick;
            adapter.OpenLocationClick += Adapter_OpenLocationClick;

            recyclerView = FindViewById<RecyclerView>(Resource.Id.recyclerView);
            recyclerView.SetAdapter(adapter);

            ItemTouchHelper.Callback callback = new DragHelper(adapter);
            ItemTouchHelper touchHelper = new ItemTouchHelper(callback);
            touchHelper.AttachToRecyclerView(recyclerView);

            layoutManager = new LinearLayoutManager(this);
            recyclerView.SetLayoutManager(layoutManager);

            fabPrompt = FindViewById<TextView>(Resource.Id.fabPrompt);

            FloatingActionButton fab = FindViewById<FloatingActionButton>(Resource.Id.addActivityFab);
            fab.Click += Fab_Click;
        }

        private void Fab_Click(object sender, EventArgs e)
        {
            Intent intent = new Intent(this, typeof(CreateChooseTaskTypeActivity));
            StartActivityForResult(intent, addActivityIntent);
        }

        private void Adapter_OpenLocationClick(object sender, int e)
        {
            throw new System.NotImplementedException();
        }

        private void Adapter_FinishClick(object sender, int e)
        {
            throw new System.NotImplementedException();
        }

        private void Adapter_EditCollectionClick(object sender, int e)
        {
            throw new System.NotImplementedException();
        }

        private void Adapter_DeleteItemClick(object sender, int e)
        {
            throw new System.NotImplementedException();
        }

        public async void SaveProgress()
        {
        }
    }
}