using System.Runtime.InteropServices;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.Windows.AppLifecycle;

namespace WinDozer
{
	public static class Startup
	{
		const string InstanceKey = "PowerPlanManager-single-instance";

		[STAThread]
		static int Main(string[] args)
		{
			WinRT.ComWrappersSupport.InitializeComWrappers();

			var activationArgs = AppInstance.GetCurrent().GetActivatedEventArgs();
			var instance = AppInstance.FindOrRegisterForKey(InstanceKey);

			if (!instance.IsCurrent)
			{
				// another instance is already running: hand over and exit
				RedirectActivationTo(activationArgs, instance);
				return 0;
			}

			// we are the first instance: react to later launches
			instance.Activated += (_, _) => App.ShowExistingWindow();

			Application.Start(_ =>
			{
				var context = new DispatcherQueueSynchronizationContext(DispatcherQueue.GetForCurrentThread());
				SynchronizationContext.SetSynchronizationContext(context);
				new App();
			});

			return 0;
		}

		// RedirectActivationToAsync must not block the STA thread directly, so run it
		// on a worker thread and pump messages here until it signals completion.
		static void RedirectActivationTo(AppActivationArguments args, AppInstance target)
		{
			var done = CreateEvent(IntPtr.Zero, true, false, null);

			Task.Run(() =>
			{
				target.RedirectActivationToAsync(args).AsTask().Wait();
				SetEvent(done);
			});

			CoWaitForMultipleObjects(0, 0xFFFFFFFF, 1, new[] { done }, out _);
		}

		[DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
		static extern IntPtr CreateEvent(IntPtr attrs, bool manualReset, bool initialState, string? name);

		[DllImport("kernel32.dll")]
		static extern bool SetEvent(IntPtr handle);

		[DllImport("ole32.dll")]
		static extern uint CoWaitForMultipleObjects(uint flags, uint timeout, ulong handleCount, IntPtr[] handles, out uint index);
	}
}