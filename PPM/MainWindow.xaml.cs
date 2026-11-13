using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Navigation;
using WinDozer.Pages;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace WinDozer
{
	public sealed partial class MainWindow : Window
	{

		static readonly Dictionary<Type, string> TagForPage = new()
		{
			[typeof(StatusPage)] = "status",
			[typeof(ProcessesPage)] = "processes",
			[typeof(SettingsPage)] = "settings",
			[typeof(StatusSettingsPage)] = "settingsIdle",
			[typeof(AboutPage)] = "about",
		};


		public MainWindow()
		{
			InitializeComponent();

			ExtendsContentIntoTitleBar = true;
			SetTitleBar(AppTitleBar);
			
			AppWindow.TitleBar.PreferredHeightOption = TitleBarHeightOption.Standard;
			AppWindow.SetIcon("Assets/AppIcon.ico");
			AppWindow.Resize(new Windows.Graphics.SizeInt32(1500, 800));

			ContentFrame.Navigated += ContentFrame_Navigated;
			ContentFrame.Navigate(typeof(StatusPage));

			RootGrid.AddHandler(UIElement.PointerPressedEvent, new PointerEventHandler(Root_PointerPressed), true);
		}

		private void TitleBar_PaneToggleRequested(TitleBar sender, object args)
		{
			NavView.IsPaneOpen = !NavView.IsPaneOpen;
		}

		private void TitleBar_BackRequested(TitleBar sender, object args)
		{
			if (ContentFrame.CanGoBack) ContentFrame.GoBack();
		}

		void ContentFrame_Navigated(object sender, NavigationEventArgs e)
		{
			if (TagForPage.TryGetValue(e.SourcePageType, out var tag))
			{
				NavView.SelectedItem = NavView.MenuItems.Concat(NavView.FooterMenuItems)
					.OfType<NavigationViewItem>()
					.FirstOrDefault(i => (string)i.Tag == tag);
			}
		}

		void NavView_ItemInvoked(NavigationView sender, NavigationViewItemInvokedEventArgs args)
		{
			Type? page = (args.InvokedItemContainer?.Tag as string) switch
			{
				"status" => typeof(StatusPage),
				"processes" => typeof(ProcessesPage),
				"settings" => typeof(SettingsPage),
				"about" => typeof(AboutPage),
				_ => null,
			};

			if (page == null || ContentFrame.CurrentSourcePageType == page) return;

			ContentFrame.BackStack.Clear();      // top-level items start fresh: back only goes subpage → parent
			ContentFrame.Navigate(page);
		}

		void Root_PointerPressed(object sender, PointerRoutedEventArgs e)
		{
			var props = e.GetCurrentPoint(RootGrid).Properties;

			if (props.IsXButton1Pressed)                       // mouse "back"
			{
				if (ContentFrame.CanGoBack) ContentFrame.GoBack();
				e.Handled = true;
			}
			else if (props.IsXButton2Pressed)                  // mouse "forward"
			{
				if (ContentFrame.CanGoForward) ContentFrame.GoForward();
				e.Handled = true;
			}
		}

		void BackAccelerator_Invoked(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs args)
		{
			if (ContentFrame.CanGoBack) ContentFrame.GoBack();
			args.Handled = true;
		}
	}
}
