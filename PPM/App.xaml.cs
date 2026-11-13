using H.NotifyIcon;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media.Imaging;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace WinDozer
{
	/// <summary>
	/// Provides application-specific behavior to supplement the default Application class.
	/// </summary>
	public partial class App : Application
	{

		public static WinDozer WinDozer { get; } = new();

		Window? window;
		TaskbarIcon? tray;
		bool isExiting;
		static DispatcherQueue? uiDispatcher;

		static BitmapImage Icon(string file) => new(new Uri($"ms-appx:///Assets/{file}"));
		readonly BitmapImage iconBalanced = Icon("balanced.ico");
		readonly BitmapImage iconIdle = Icon("idle.ico");
		readonly BitmapImage iconBoost = Icon("boost.ico");

		public App()
		{
			InitializeComponent();
			uiDispatcher = DispatcherQueue.GetForCurrentThread();

			UnhandledException += (_, e) => Debug.LogError("UI unhandled: " + e.Exception);
			AppDomain.CurrentDomain.UnhandledException += (_, e) => Debug.LogError("AppDomain unhandled: " + e.ExceptionObject);
			TaskScheduler.UnobservedTaskException += (_, e) => Debug.LogError("Unobserved task: " + e.Exception);
			AppDomain.CurrentDomain.ProcessExit += (_, _) => Debug.Log("process exiting");
		}

		protected override void OnLaunched(Microsoft.UI.Xaml.LaunchActivatedEventArgs args)
		{
			// start main program
			WinDozer.Start();

			// create persistent tray icon
			CreateTray();

			// on status change, update icon on ui thread
			WinDozer.StatusChangedEvent += () => uiDispatcher?.TryEnqueue(UpdateTrayIcon);

#if DEBUG
			ShowWindow();
#endif
		}

		public static void ShowExistingWindow() => uiDispatcher?.TryEnqueue(() => ((App)Current).ShowWindow());

		public void ShowWindow()
		{
			if (window == null)
			{
				var w = new MainWindow();

				w.AppWindow.Closing += (_, e) =>
				{
					// override window close and just hide it
					if (isExiting) return;
					Debug.Log("hiding window");
					e.Cancel = true;
					w.AppWindow.Hide();
				};
				window = w;
			}

			window.AppWindow.Show();
			window.Activate();
		}


		void CreateTray()
		{
			var showCmd = new XamlUICommand();
			showCmd.ExecuteRequested += (_, _) => ShowWindow();

			var exitCmd = new XamlUICommand();
			exitCmd.ExecuteRequested += (_, _) => ExitApp();

			var setAuto = new XamlUICommand();
			setAuto.ExecuteRequested += (_, _) => WinDozer.IdleManager.ResetForced();

			var setIdle = new XamlUICommand();
			setIdle.ExecuteRequested += (_, _) => WinDozer.IdleManager.ForceStatus(IdleManager.Status.Idle);

			var setBalanced = new XamlUICommand();
			setBalanced.ExecuteRequested += (_, _) => WinDozer.IdleManager.ForceStatus(IdleManager.Status.Balanced);

			var setPerf = new XamlUICommand();
			setPerf.ExecuteRequested += (_, _) => WinDozer.IdleManager.ForceStatus(IdleManager.Status.Performance);

			tray = new TaskbarIcon
			{
				ToolTipText = WinDozer.ProgramName,
				IconSource = iconBalanced,
				ContextMenuMode = ContextMenuMode.PopupMenu,
				NoLeftClickDelay = true,
				LeftClickCommand = showCmd,
				ContextFlyout = new MenuFlyout
				{
					Items =
				{
					new MenuFlyoutItem { Text = "Open", Command = showCmd },
					new MenuFlyoutSeparator(),
					new MenuFlyoutItem { Text = "Auto", Command = setAuto },
					new MenuFlyoutItem { Text = "Idle", Command = setIdle },
					new MenuFlyoutItem { Text = "Balanced", Command = setBalanced },
					new MenuFlyoutItem { Text = "Performance", Command = setPerf },
					new MenuFlyoutSeparator(),
					new MenuFlyoutItem { Text = "Exit", Command = exitCmd },
				}
				},
			};

			// needed so the icon exists without any window
			tray.ForceCreate();
		}

		void UpdateTrayIcon()
		{
			if (tray == null || WinDozer.IdleManager is not { } idle) return;

			tray.IconSource = idle.CurrentStatus switch
			{
				IdleManager.Status.Idle => iconIdle,
				IdleManager.Status.Performance => iconBoost,
				_ => iconBalanced,
			};
			tray.ToolTipText = $"{WinDozer.ProgramName}: {idle.CurrentStatus}" + (idle.IsForced ? " (forced)" : "");
		}

		void ExitApp()
		{
			Debug.Log("exit requested from tray menu");
			isExiting = true;
			tray?.Dispose();
			WinDozer.Dispose();
			window?.Close();
			Exit();
		}
	}
}
