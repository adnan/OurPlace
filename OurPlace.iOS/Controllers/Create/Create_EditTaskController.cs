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

// This file has been autogenerated from a class added in the UI designer.

using System;
using System.Collections.Generic;
using System.Linq;
using CoreGraphics;
using FFImageLoading;
using Foundation;
using OurPlace.iOS.Controllers.Create;
using OurPlace.iOS.Delegates;
using OurPlace.Common.Models;
using UIKit;

namespace OurPlace.iOS
{
    public partial class Create_EditTaskController : Create_BaseSegueController
    {
        public TaskType thisTaskType;
        public int parentTaskIndex;
        public int? childTaskIndex;
        protected LearningTask thisTask;
        protected string placeholderText = "Provide a short instruction for the task.";

        public Create_EditTaskController(IntPtr handle) : base(handle)
        {
        }

        /// <summary>
        /// Returns the delegate of the current running application
        /// </summary>
        /// <value>The this app.</value>
        protected AppDelegate ThisApp
        {
            get { return (AppDelegate)UIApplication.SharedApplication.Delegate; }
        }

        public override bool HandlesKeyboardNotifications()
        {
            return true;
        }

        public override void ViewDidLoad()
        {
            base.ViewDidLoad();

            InitKeyboardHandling();
            DismissKeyboardOnBackgroundTap();

            if (thisActivity == null)
            {
                NavigationController.PopViewController(true);
                return;
            }

            if (thisActivity.LearningTasks == null)
            {
                thisActivity.LearningTasks = new List<LearningTask>();
            }

            // check if we're editing an existing task
            if (thisActivity.LearningTasks.ToList().Count > parentTaskIndex)
            {
                if (childTaskIndex != null)
                {
                    var parent = thisActivity.LearningTasks.ToList()[parentTaskIndex];
                    if (parent.ChildTasks != null & parent.ChildTasks.Count() > childTaskIndex)
                    {
                        thisTask = parent.ChildTasks.ToList()[(int)childTaskIndex];
                        thisTaskType = thisTask.TaskType;
                    }
                }
                else
                {
                    thisTask = thisActivity.LearningTasks.ToList()[parentTaskIndex];
                    thisTaskType = thisTask.TaskType;
                }

                UIBarButtonItem customButton = new UIBarButtonItem(
                    UIImage.FromFile("ic_delete"),
                    UIBarButtonItemStyle.Plain,
                    (s, e) =>
                    {
                        ConfirmDelete();
                    }
                );
                NavigationItem.RightBarButtonItem = customButton;
            }

            if (thisTaskType == null)
            {
                NavigationController.PopViewController(true);
            }

            NavigationItem.Title = thisTaskType.DisplayName;

            ImageService.Instance.LoadUrl(thisTaskType.IconUrl).Into(TaskTypeIcon);

            FinishButton.TouchUpInside += FinishButton_TouchUpInside;

            // style the text view to look like a text field - why the hell are they different?
            TaskDescription.Layer.BorderColor = UIColor.Gray.ColorWithAlpha(0.5f).CGColor;
            TaskDescription.Layer.BorderWidth = 2;
            TaskDescription.Layer.CornerRadius = 5;
            TaskDescription.ClipsToBounds = true;

            if (thisTask != null)
            {
                TaskDescription.Text = thisTask.Description;
                FinishButton.SetTitle("Save Changes", UIControlState.Normal);
            }
            else
            {
                // Add placeholder text
                TaskDescription.Delegate = new TextViewPlaceholderDelegate(TaskDescription, placeholderText);
            }
        }

        private void ConfirmDelete()
        {
            var alertController = UIAlertController.Create("Confirm Delete", "Are you sure you want to delete this task?", UIAlertControllerStyle.Alert);
            alertController.AddAction(UIAlertAction.Create("Delete", UIAlertActionStyle.Destructive, (obj) =>
            {
                RemoveFromActivityAndReturn();
            }));
            alertController.AddAction(UIAlertAction.Create("Cancel", UIAlertActionStyle.Cancel, (obj) => { }));
            PresentViewController(alertController, true, null);
        }

        protected bool UpdateBasicTask()
        {
            if (string.IsNullOrWhiteSpace(TaskDescription.Text) || TaskDescription.Text == placeholderText)
            {
                AppUtils.ShowSimpleDialog(this, "Missing Instruction", "Please enter an instruction or description for your task.", "Got it");
                return false;
            }

            if (thisTask == null)
            {
                thisTask = new LearningTask
                {
                    Id = (new Random()).Next()
                };
            }

            thisTask.TaskType = thisTaskType;
            thisTask.Description = TaskDescription.Text;

            return true;
        }

        protected void UpdateActivity()
        {
            List<LearningTask> tasks = thisActivity.LearningTasks.ToList();

            if (childTaskIndex != null)
            {
                // we're adding/editing a child task
                List<LearningTask> children = tasks[parentTaskIndex].ChildTasks?.ToList();
                if (children == null) children = new List<LearningTask>();

                if (children.Count != 0 && children.Count() > childTaskIndex)
                {
                    // editing existing child task
                    children[(int)childTaskIndex] = thisTask;
                }
                else
                {
                    // adding new child task
                    children.Add(thisTask);
                }

                tasks[parentTaskIndex].ChildTasks = children;
            }
            else if (tasks.Count != 0 && tasks.Count > parentTaskIndex)
            {
                // we're editing an existing parent task
                thisTask.ChildTasks = tasks[parentTaskIndex].ChildTasks;
                tasks[parentTaskIndex] = thisTask;
            }
            else
            {
                // adding a new parent-level task
                tasks.Add(thisTask);
                thisActivity.LearningTasks = tasks;
            }

            thisActivity.LearningTasks = tasks;
        }

        protected void RemoveFromActivityAndReturn()
        {
            List<LearningTask> tasks = thisActivity.LearningTasks.ToList();

            for (int i = 0; i < tasks.Count; i++)
            {
                if (tasks[i].Id == thisTask.Id)
                {
                    tasks.RemoveAt(i);
                    goto FoundTask;
                }

                if (tasks[i].ChildTasks == null) continue;
                for (int j = 0; j < tasks[i].ChildTasks.Count(); j++)
                {
                    List<LearningTask> childTasks = tasks[i].ChildTasks.ToList();
                    if (childTasks[j].Id == thisTask.Id)
                    {
                        childTasks.RemoveAt(j);
                        tasks[i].ChildTasks = childTasks;
                        goto FoundTask;
                    }
                }
            }

        FoundTask:

            thisActivity.LearningTasks = tasks;
            Unwind();
        }

        protected void Unwind()
        {
            if (childTaskIndex != null)
            {
                PerformSegue("UnwindToChildrenOverview", this);
            }
            else
            {
                PerformSegue("UnwindToOverview", this);
            }
        }

        protected virtual void FinishButton_TouchUpInside(object sender, EventArgs e)
        {
            if (UpdateBasicTask())
            {
                UpdateActivity();
                Unwind();
            }
        }

    }
}
