// WARNING
//
// This file has been generated automatically by Xamarin Studio to store outlets and
// actions made in the UI designer. If it is removed, they will be lost.
// Manual changes to this file may not be handled correctly.
//
using MonoTouch.Foundation;
using System.CodeDom.Compiler;

namespace CodeHub.iOS.Views.App
{
	[Register ("EnablePushNotificationsViewController")]
	partial class EnablePushNotificationsViewController
	{
		[Outlet]
		MonoTouch.UIKit.UIButton CancelButton { get; set; }

		[Outlet]
		MonoTouch.UIKit.UIButton EnableButton { get; set; }

		[Outlet]
		MonoTouch.UIKit.UIImageView ImageView { get; set; }
		
		void ReleaseDesignerOutlets ()
		{
			if (ImageView != null) {
				ImageView.Dispose ();
				ImageView = null;
			}

			if (EnableButton != null) {
				EnableButton.Dispose ();
				EnableButton = null;
			}

			if (CancelButton != null) {
				CancelButton.Dispose ();
				CancelButton = null;
			}
		}
	}
}
