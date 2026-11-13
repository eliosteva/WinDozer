using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace WinDozer.UI
{
	internal class DialogManager
	{

		static class Msg
		{
			[DllImport("user32.dll", CharSet = CharSet.Unicode, EntryPoint = "MessageBoxW")]
			private static extern int MessageBox(IntPtr hWnd, string text, string caption, uint type);

			public static void Show(string text, string caption = "PPM") =>	MessageBox(IntPtr.Zero, text, caption, 0x40 /* MB_ICONINFORMATION */ | 0x10000 /* MB_SETFOREGROUND */);

			public static bool Confirm(string text, string caption = "PPM") => MessageBox(IntPtr.Zero, text, caption, 0x24 /* MB_YESNO | MB_ICONQUESTION */ | 0x10000) == 6; // IDYES
		}

		/*
		public int ShowYesNoDialog(string message)
		{
			// ...
			ContentDialog dialog = new ContentDialog
			{
				Title = "Confirmation",
				Content = message,
				PrimaryButtonText = "Yes",
				SecondaryButtonText = "No",
				XamlRoot = this.XamlRoot // Required for WinUI 3
			};

			ContentDialogResult result = await dialog.ShowAsync();

			if (result == ContentDialogResult.Primary)
			{
				// User clicked Yes
			}
			else
			{
				// User clicked No
			}
		}
		*/

	}
}
