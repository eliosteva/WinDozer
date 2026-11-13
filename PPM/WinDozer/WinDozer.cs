using System;
using System.Collections.Generic;
using System.Text;

namespace WinDozer
{
	public class WinDozer : IDisposable
	{

		// TODO autoinstall power plans if missing
		// TODO per status settings (sleep, screen, CPU boost, CPU frequency)

		// sustainable EC0A &#xEC0A;
		// SpeedOff &#xEC48;
		// SpeedMedium &#xEC49;
		// SpeedHigh &#xEC4A;
		// 	F42F	SpeedHigh2		Copy &#xF42F;

		public const string ProgramName = "WinDozer";
		public const string CompanyName = "ElioSoft";

		//public const string gliphIdle = "\uEC48";
		public const string gliphIdle = "\uE708";
		//public const string gliphBalanced = "\uEC49";
		public const string gliphBalanced = "\uE7E8";
		//public const string gliphBoost = "\uEC4A";
		public const string gliphPerformance = "\uE945";

		static string executablePath => Environment.ProcessPath;
		static string executableName => Path.GetFileName(executablePath);
		static string executableDirPath => Path.GetDirectoryName(executablePath);
		static string appdataDirPath => Path.Combine(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), WinDozer.CompanyName), WinDozer.ProgramName);

		public static string AppdataDirPath => appdataDirPath;
		public static string AppdataExePath => Path.Combine(appdataDirPath, executableName);
		public static string AppdataLogFilePath => Path.Combine(AppdataDirPath, ProgramName + ".log");


		public event Action? StatusChangedEvent;

		readonly CancellationTokenSource pollingCancellationToken = new();
		const int pollingInterval = 2;

		internal PrefsManager PrefsManager { get; private set; }
		internal PowerModeManager PowerModeManager { get; private set; }
		internal PowerPlanManager PowerPlanManager { get; private set; }
		internal IdleManager IdleManager { get; private set; }   // null until initialization finishes


		public void Start() => _ = Task.Run(RunAsync);

		async Task RunAsync()
		{
			// start dirs
			if (!Directory.Exists(WinDozer.AppdataDirPath)) Directory.CreateDirectory(WinDozer.AppdataDirPath);
			
			// start components
			Debug.Initialize(AppdataLogFilePath);
			Debug.Log(" ====================== started");
			try
			{
				PrefsManager = new PrefsManager();
				PowerModeManager = new PowerModeManager();
				PowerPlanManager = new PowerPlanManager(PrefsManager);
				IdleManager = new IdleManager(PrefsManager, PowerPlanManager, PowerModeManager);
				IdleManager.StatusChangedEvent += () => StatusChangedEvent?.Invoke();
				StatusChangedEvent?.Invoke();
			}
			catch (Exception ex)
			{
				Debug.LogError(ex.ToString());
				return;
			}

			// start timed polling
			using var timer = new PeriodicTimer(TimeSpan.FromSeconds(pollingInterval));
			try
			{
				while (await timer.WaitForNextTickAsync(pollingCancellationToken.Token))
				{
					IdleManager.Poll();
				}
			}
			catch (OperationCanceledException) { }
		}

		public void Dispose() => pollingCancellationToken.Cancel();
	}
}
