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
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using FFImageLoading;
using Foundation;
using MobileCoreServices;
using OurPlace.Common;
using OurPlace.Common.Models;
using UIKit;

namespace OurPlace.iOS
{
    public partial class Create_EditChoosePhotoController : Create_EditTaskController
    {
        private string previousImage;
        private string currentImagePath;
        private string folderPath;
        private UIImagePickerController imagePicker;
        private UIDocumentPickerViewController docPicker;
        private bool saved;
        private bool allowParentAsSource;
        private LearningTask parentTask;

        public Create_EditChoosePhotoController(IntPtr handle) : base(handle)
        {

        }

        public override void ViewDidLoad()
        {
            base.ViewDidLoad();

            ChooseImageButton.TouchUpInside += ChooseImageButton_TouchUpInside;

            if (childTaskIndex != null)
            {
                parentTask = thisActivity.LearningTasks.ToList()[parentTaskIndex];
                string[] supportedParents = { "TAKE_PHOTO", "MATCH_PHOTO", "DRAW", "DRAW_PHOTO" };

                if (supportedParents.Contains(parentTask.TaskType.IdName))
                {
                    allowParentAsSource = true;
                }
            }

            if (!string.IsNullOrWhiteSpace(thisTask?.JsonData))
            {
                previousImage = thisTask.JsonData;

                if (!IsParent(previousImage))
                {
                    if (previousImage.StartsWith("upload"))
                    {
                        ImageService.Instance.LoadUrl(ServerUtils.GetUploadUrl(previousImage))
                            .Into(ChosenImage);
                    }
                    else
                    {
                        ImageService.Instance.LoadFile(
                    AppUtils.GetPathForLocalFile(previousImage))
                            .Into(ChosenImage);
                    }
                }
                else
                {
                    ImageService.Instance.LoadUrl(parentTask.TaskType.IconUrl)
                                .Into(ChosenImage);
                }
            }

            folderPath = Common.LocalData.Storage.GetCacheFolder("created");
        }

        public override void ViewWillDisappear(bool animated)
        {
            base.ViewWillDisappear(animated);

            Console.WriteLine("ViewWillDisappear, IsMovingFromParentViewController: " + IsMovingFromParentViewController);

            if (IsMovingFromParentViewController)
            {
                // popped - if a file isn't being referenced by the saved data, delete it
                if (!saved && currentImagePath != null &&
                !IsParent(currentImagePath) &&
                    currentImagePath != previousImage &&
                !currentImagePath.StartsWith("upload"))
                {
                    string path = AppUtils.GetPathForLocalFile(currentImagePath);
                    File.Delete(path);
                    Console.WriteLine("Cleaned up file at " + path);
                }
            }
        }

        private void ChooseImageButton_TouchUpInside(object sender, EventArgs e)
        {
            UIAlertController imageSourceAlert = UIAlertController.Create("Choose Image Source", null, UIAlertControllerStyle.ActionSheet);
            imageSourceAlert.AddAction(UIAlertAction.Create("Camera", UIAlertActionStyle.Default, (a) => { OpenCamera(); }));
            imageSourceAlert.AddAction(UIAlertAction.Create("Gallery", UIAlertActionStyle.Default, (a) => { OpenImagePicker(); }));
            imageSourceAlert.AddAction(UIAlertAction.Create("Other Sources", UIAlertActionStyle.Default, (a) => { var suppress = OpenDocumentPicker(); }));

            if (allowParentAsSource)
            {
                imageSourceAlert.AddAction(UIAlertAction.Create(string.Format("Use the Result of Parent '{0}' Task", parentTask.TaskType.DisplayName), UIAlertActionStyle.Default, (a) => { UseParent(); }));
            }

            imageSourceAlert.AddAction(UIAlertAction.Create("Cancel", UIAlertActionStyle.Cancel, null));

            UIPopoverPresentationController popCont = imageSourceAlert.PopoverPresentationController;
            if (popCont != null)
            {
                popCont.SourceView = ChooseImageButton;
                popCont.SourceRect = ChooseImageButton.Bounds;
                popCont.PermittedArrowDirections = UIPopoverArrowDirection.Down;
            }

            PresentViewController(imageSourceAlert, true, () => { Console.WriteLine("alert presented"); });
        }

        private void UseParent()
        {
            if (currentImagePath != null &&
            currentImagePath != previousImage &&
            !currentImagePath.StartsWith("upload"))
            {
                File.Delete(AppUtils.GetPathForLocalFile(currentImagePath));
            }

            currentImagePath = "TASK::" + parentTask.Id.ToString();
            ImageService.Instance.LoadUrl(parentTask.TaskType.IconUrl).Into(ChosenImage);
        }

        private void OpenCamera()
        {
            imagePicker = new UIImagePickerController
            {
                SourceType = UIImagePickerControllerSourceType.Camera,
                MediaTypes = new string[] { UTType.Image }
            };
            imagePicker.FinishedPickingMedia += ImagePicker_FinishedPickingMedia;
            imagePicker.Canceled += (object sender, EventArgs e) => { imagePicker.DismissViewController(true, null); };
            PresentViewController(imagePicker, true, null);
        }

        // https://github.com/xamarin/recipes/tree/master/Recipes/ios/media/video_and_photos/choose_a_photo_from_the_gallery
        private void OpenImagePicker()
        {
            imagePicker = new UIImagePickerController
            {
                SourceType = UIImagePickerControllerSourceType.PhotoLibrary,
                MediaTypes = new string[] { UTType.Image }
            };
            imagePicker.FinishedPickingMedia += ImagePicker_FinishedPickingMedia;
            imagePicker.Canceled += (object sender, EventArgs e) => { imagePicker.DismissViewController(true, null); };
            PresentViewController(imagePicker, true, null);
        }

        private bool IsParent(string path)
        {
            return path.StartsWith("TASK::", StringComparison.InvariantCulture);
        }

        private void ImagePicker_FinishedPickingMedia(object sender, UIImagePickerMediaPickedEventArgs e)
        {
            bool isImage = false;
            switch (e.Info[UIImagePickerController.MediaType].ToString())
            {
                case "public.image":
                    Console.WriteLine("Image selected");
                    isImage = true;
                    break;
                case "public.video":
                    Console.WriteLine("Video selected");
                    break;
            }

            if (!isImage) return; //sanity check against video

            NSUrl referenceUrl = e.Info[new NSString("UIImagePickerControllerReferenceURL")] as NSUrl;
            UIImage passedImage = e.Info[UIImagePickerController.OriginalImage] as UIImage;

            ShrinkAndSaveNewImage(passedImage);

            imagePicker?.DismissViewController(true, null);
        }

        private void ShrinkAndSaveNewImage(UIImage passedImage)
        {
            if (passedImage == null) return;

            UIImage smallerImage = AppUtils.ScaleAndRotateImage(passedImage, 800);
            NSData imageData = smallerImage.AsJPEG(0.8f);

            if (currentImagePath == null)
            {
                currentImagePath = Path.Combine(folderPath, "task_" + DateTime.UtcNow.ToString("s") + ".jpg");
            }
            else if (!File.Exists(currentImagePath))
            {
                File.Create(currentImagePath);
            }

            // save the image data to a folder ready for uploading
            if (imageData.Save(currentImagePath, true))
            {
                Console.WriteLine("Saved photo to: " + currentImagePath);
                ImageService.Instance.InvalidateCacheEntryAsync(currentImagePath, FFImageLoading.Cache.CacheType.All, true);
                ImageService.Instance.LoadFile(currentImagePath).Into(ChosenImage);
            }
            else
            {
                Console.WriteLine("ERROR saving to " + currentImagePath);
            }
        }

        private async Task OpenDocumentPicker()
        {
            if (PresentedViewController != null)
            {
                await DismissViewControllerAsync(false);
            }

            // Allow the Document picker to select a range of document types
            var allowedUTIs = new string[] {
                UTType.PNG,
                UTType.JPEG,
                UTType.Image
            };

            // Display the picker
            docPicker = new UIDocumentPickerViewController(allowedUTIs, UIDocumentPickerMode.Import);
            docPicker.AllowsMultipleSelection = false;
            docPicker.DidPickDocumentAtUrls += DocPicker_DidPickDocumentAtUrls;

            await PresentViewControllerAsync(docPicker, false);
        }

        void DocPicker_DidPickDocumentAtUrls(object sender, UIDocumentPickedAtUrlsEventArgs e)
        {
            if (e.Urls == null || e.Urls.Length < 1) return;

            docPicker?.DismissViewControllerAsync(true);

            // IMPORTANT! You must lock the security scope before you can
            // access this file
            var securityEnabled = e.Urls[0].StartAccessingSecurityScopedResource();

            ThisApp.ClearDocumentHandler();
            ThisApp.DocumentLoaded += ThisApp_DocumentLoaded;
            ThisApp.OpenDocument(e.Urls[0]);

            // IMPORTANT! You must release the security lock established
            // above.
            e.Urls[0].StopAccessingSecurityScopedResource();
        }

        public void ThisApp_DocumentLoaded(Helpers.GenericTextDocument document)
        {
            if (currentImagePath != null &&
             !IsParent(currentImagePath) &&
               currentImagePath != previousImage &&
               !currentImagePath.StartsWith("upload"))
            {
                File.Delete(AppUtils.GetPathForLocalFile(currentImagePath));
            }

            string tempPath = document.FileUrl.Path;

            UIImage fullSize = UIImage.FromFile(tempPath);

            ShrinkAndSaveNewImage(fullSize);
        }

        protected override void FinishButton_TouchUpInside(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(previousImage) && string.IsNullOrWhiteSpace(currentImagePath))
            {
                AppUtils.ShowSimpleDialog(this, "Select an Image", "Please select or take a photo.", "Got it");
                return;
            }

            if (UpdateBasicTask())
            {
                if (string.IsNullOrWhiteSpace(currentImagePath))
                {
                    thisTask.JsonData = previousImage;
                }
                else
                {
                    if (!string.IsNullOrWhiteSpace(previousImage) &&
                    !IsParent(previousImage) &&
                        !previousImage.StartsWith("upload"))
                    {
                        // new image replaces old one, delete the previous image
                        File.Delete(AppUtils.GetPathForLocalFile(previousImage));
                    }

                    if (!IsParent(currentImagePath))
                    {
                        thisTask.JsonData = Path.Combine(Directory.GetParent(currentImagePath).Name, Path.GetFileName(currentImagePath));
                    }
                    else
                    {
                        thisTask.JsonData = currentImagePath;
                    }
                }

                UpdateActivity();
                saved = true;
                Unwind();
            }
        }
    }
}
