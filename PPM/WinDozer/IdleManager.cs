using WinDozer;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.AccessControl;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace WinDozer
{
	internal class IdleManager
	{

		// listLock: guards balancedProcessNames / performanceProcessNames (UI edits, loop reads)
		// stateLock: serializes Poll / ForceStatus / ResetForced (loop thread vs UI thread)
		readonly object listLock = new object();
		readonly object stateLock = new object();

		bool idleOnScreensaver = true;
		public bool IdleOnScreensaver
		{
			get
			{
				return idleOnScreensaver;
			}
			set
			{
				if (value != idleOnScreensaver)
				{
					idleOnScreensaver = value;
					pm.SetPref(nameof(idleOnScreensaver), value.ToString());
				}
			}
		}

		bool idleOnTimeout = true;
		public bool IdleOnTimeout
		{
			get
			{
				return idleOnTimeout;
			}
			set
			{
				if (value != idleOnTimeout)
				{
					idleOnTimeout = value;
					pm.SetPref(nameof(idleOnTimeout), value.ToString());
				}
			}
		}

		int inputTimeout = 20;
		public int InputTimeout
		{
			get
			{
				return inputTimeout;
			}
			set
			{
				if (value != inputTimeout)
				{
					inputTimeout = value;
					pm.SetPref(nameof(inputTimeout), value.ToString());
				}
			}
		}

		int pollingInterval = 2;
		public int PollingInterval
		{
			get
			{
				return pollingInterval;
			}
			set
			{
				if (value != pollingInterval)
				{
					pollingInterval = value;
					pm.SetPref(nameof(pollingInterval), value.ToString());
				}
			}
		}

		//bool manualHibernation = true;
		//public bool ManualHibernation
		//{
		//	get
		//	{
		//		return manualHibernation;
		//	}
		//	set
		//	{
		//		if (value != manualHibernation)
		//		{
		//			manualHibernation = value;
		//			dm.SetPref("manualHibernation", value.ToString());
		//		}
		//	}
		//}

		List<string> balancedProcessNames = new List<string>();
		List<string> performanceProcessNames = new List<string>();

		// snapshots: the UI never enumerates the live lists
		public IReadOnlyList<string> BalancedProcessNames { get { lock (listLock) return balancedProcessNames.ToArray(); } }
		public IReadOnlyList<string> PerformanceProcessNames { get { lock (listLock) return performanceProcessNames.ToArray(); } }

		public void AddIdleProcess(IEnumerable<string> names)
		{
			lock (listLock)
			{
				foreach (var v in names)
				{
					string name = v.Trim();
					if (string.IsNullOrEmpty(name)) continue;

					// remove process from balanced and perf list
					if (balancedProcessNames.Contains(name))
					{
						balancedProcessNames.Remove(name);
					}
					if (performanceProcessNames.Contains(name))
					{
						performanceProcessNames.Remove(name);
					}
				}
				SaveProcessNames();
			}
		}

		public void AddBalancedProcess(IEnumerable<string> names)
		{
			lock (listLock)
			{
				foreach (var v in names)
				{
					string name = v.Trim();
					if (string.IsNullOrEmpty(name)) continue;

					// add process to balanced
					if (!balancedProcessNames.Contains(name))
					{
						balancedProcessNames.Add(name);
					}
					if (performanceProcessNames.Contains(name))
					{
						performanceProcessNames.Remove(name);
					}
				}
				SaveProcessNames();
			}
		}

		public void AddPerformanceProcess(IEnumerable<string> names)
		{
			lock (listLock)
			{
				foreach (var v in names)
				{
					string name = v.Trim();
					if (string.IsNullOrEmpty(name)) continue;

					// add process to performance
					if (balancedProcessNames.Contains(name))
					{
						balancedProcessNames.Remove(name);
					}
					if (!performanceProcessNames.Contains(name))
					{
						performanceProcessNames.Add(name);
					}
				}
				SaveProcessNames();
			}
		}

		// must be called with listLock held
		void SaveProcessNames()
		{
			{
				string s = "";
				foreach (var v in balancedProcessNames)
				{
					if (!string.IsNullOrEmpty(v))
					{
						s += v + "|";
					}
				}
				pm.SetPref("balancedProcessNames", s);
			}
			{
				string s = "";
				foreach (var v in performanceProcessNames)
				{
					if (!string.IsNullOrEmpty(v))
					{
						s += v + "|";
					}
				}
				pm.SetPref("performanceProcessNames", s);
			}
		}



		public enum Status
		{
			Idle = 0,
			Balanced = 1,
			Performance = 2,
		}

		BackgroundWorker bw;
		PrefsManager pm;
		PowerPlanManager ppm;
		PowerModeManager pmm;

		internal IdleManager(PrefsManager pm, PowerPlanManager ppm, PowerModeManager pmm)
		{
			this.pm = pm;
			this.ppm = ppm;
			this.pmm = pmm;

			Debug.Log("initializing idle manager");

			// load prefs
			IdleOnScreensaver = pm.GetPref(nameof(idleOnScreensaver), idleOnScreensaver);
			IdleOnTimeout = pm.GetPref(nameof(idleOnTimeout), idleOnTimeout);
			InputTimeout = pm.GetPref(nameof(inputTimeout), inputTimeout);
			PollingInterval = pm.GetPref(nameof(pollingInterval), pollingInterval);
			//ManualHibernation = dm.GetPref("manualHibernation", manualHibernation);
			RebuildProcessList();

			// init background worker and start
			//bw = new BackgroundWorker();
			//bw.WorkerReportsProgress = false;
			//bw.WorkerSupportsCancellation = false;
			//bw.DoWork += new DoWorkEventHandler(Update);
			//bw.RunWorkerAsync();

			//// intercept exit event
			//AppDomain.CurrentDomain.ProcessExit += new EventHandler(OnProcessExit);

			lastHibernationTime = DateTime.Now;

			Poll();
		}

		bool close = false;

		void RebuildProcessList()
		{
			lock (listLock)
			{
				{
					string pref = pm.GetPref("balancedProcessNames", "");
					balancedProcessNames.Clear();
					if (!string.IsNullOrEmpty(pref))
					{
						string[] ss = pref.Split('\n', '|', ',');
						foreach (var s in ss)
						{
							if (!string.IsNullOrEmpty(s))
							{
								if (!balancedProcessNames.Contains(s))
								{
									balancedProcessNames.Add(s);
								}
							}
						}
					}
				}
				{
					string pref = pm.GetPref("performanceProcessNames", "");
					performanceProcessNames.Clear();
					if (!string.IsNullOrEmpty(pref))
					{
						string[] ss = pref.Split('\n', '|', ',');
						foreach (var s in ss)
						{
							if (!string.IsNullOrEmpty(s))
							{
								if (!performanceProcessNames.Contains(s))
								{
									performanceProcessNames.Add(s);
								}
							}
						}
					}
				}
			}
		}

		//void Update(object sender, DoWorkEventArgs e)
		//{
		//	while (!close)
		//	{
		//		Poll();
		//		Thread.Sleep(pollingInterval * 1000);
		//	}
		//}

		void OnProcessExit(object sender, EventArgs e)
		{
			Debug.Log("stopping background worker");
			close = true;
		}


		// raised on the polling thread (or on the UI thread for forced changes): subscribers must marshal to the UI themselves
		internal Action StatusChangedEvent;

		// stored as an int (-1 = no status applied yet) so UI-thread reads are atomic, unlike a Status? struct
		volatile int currentStatusValue = -1;

		internal Status CurrentStatus => currentStatusValue < 0 ? Status.Balanced : (Status)currentStatusValue;
		internal string CurrentStatusReason => currentStatusReason;
		internal bool IsForced => forced;

		bool IsCurrent(Status status) => currentStatusValue == (int)status;

		volatile string currentStatusReason = "";
		volatile bool forced = false;
		DateTime lastHibernationTime;

		public void Poll()
		{
			lock (stateLock)
			{
				try
				{
					// check forced
					if (forced)
					{
#if DEBUG
						Debug.Log("[DEBUG] polling update: disabled due to forced status");
#endif
						return;
					}

					// snapshot the rules (the UI may edit them at any time)
					string[] perf, bal;
					lock (listLock)
					{
						perf = performanceProcessNames.ToArray();
						bal = balancedProcessNames.ToArray();
					}

					// ONE process snapshot per poll, only if there is something to look for
					var running = (perf.Length > 0 || bal.Length > 0) ? GetRunningNames() : new HashSet<string>();

					// check performance process
					foreach (string name in perf)
					{
						if (running.Contains(name))
						{
#if DEBUG
							Debug.Log("[DEBUG] polling update: Performance due to running process " + name);
#endif
							ApplyStatus(Status.Performance, "\"" + name + "\" process running");
							return;
						}
					}

					// check balanced process
					foreach (string name in bal)
					{
						if (running.Contains(name))
						{
#if DEBUG
							Debug.Log("[DEBUG] polling update: Balanced due to running process " + name);
#endif
							ApplyStatus(Status.Balanced, "\"" + name + "\" process running");
							return;
						}
					}

					// check timeout
					if (idleOnTimeout)
					{
						var idleTime = GetInputIdleTime();
						if (idleTime.TotalSeconds >= inputTimeout)
						{
							// enter idle
							if (!IsCurrent(Status.Idle))
							{
#if DEBUG
								Debug.Log("[DEBUG] polling update: Idle due to user input timeout");
#endif
								ApplyStatus(Status.Idle, "user input timeout");
							}
							return;
						}
					}

					// check screensaver
					if (idleOnScreensaver)
					{
						if (GetScreenSaverRunning())
						{
							// enter idle
							if (!IsCurrent(Status.Idle))
							{
#if DEBUG
								Debug.Log("[DEBUG] polling update: Idle due to screen saver running");
#endif
								ApplyStatus(Status.Idle, "screen saver running");
							}
							return;
						}
					}

					// if currently idle
					//if (currentStatus == Status.idle)
					//{
					//	if (ManualHibernation)
					//	{
					//		var idleTime = GetInputIdleTime();
					//		if (idleTime.TotalMinutes >= ppm.HibernateTimeout)
					//		{
					//			// minimum time between hibernations
					//                        if ((DateTime.Now - lastHibernationTime).TotalMinutes > ppm.HibernateTimeout)
					//			{
					//				// manually hibernate
					//				Debug.Log("polling update: Hibernation time elapsed, triggering manual hibernation");
					//				PowerPlansApiWrapper.SetSuspendState(true, true, true);
					//				return;
					//			}
					//		}
					//	}
					//}

#if DEBUG
					Debug.Log("[DEBUG] polling update: Balanced due to user input detected");
#endif
					ApplyStatus(Status.Balanced, "user input detected");
				}
				catch (Exception ex)
				{
					Debug.LogError(ex.ToString());
				}
			}
		}

		// called with stateLock held
		void ApplyStatus(Status status, string reason)
		{
			currentStatusReason = reason;

			if (!IsCurrent(status))   // also true on the very first call (-1)
			{
				Debug.Log("applying status: " + status);
				currentStatusValue = (int)status;
				ppm.ApplyPowerPlanForStatus(status);
				StatusChangedEvent?.Invoke();
			}
		}

		// one snapshot of the running process names (case sensitive, same as the rules)
		static HashSet<string> GetRunningNames()
		{
			var names = new HashSet<string>();
			foreach (var p in Process.GetProcesses())
			{
				using (p) names.Add(p.ProcessName);
			}
			return names;
		}

		internal void ForceStatus(Status status)
		{
			lock (stateLock)
			{
				forced = true;
				ApplyStatus(status, "forced by user");
			}
		}

		internal void ResetForced()
		{
			lock (stateLock)
			{
				forced = false;
				Poll();   // re-entrant lock: fine
			}
		}

		#region idle timer

		internal TimeSpan GetInputIdleTime()
		{
			NativeMethods.LastInputInfo plii = new NativeMethods.LastInputInfo();
			plii.cbSize = (UInt32)Marshal.SizeOf(plii);

			if (NativeMethods.GetLastInputInfo(ref plii))
			{
				//return TimeSpan.FromMilliseconds(Environment.TickCount - plii.dwTime);
				uint idleMs = unchecked((uint)Environment.TickCount - plii.dwTime);
				return TimeSpan.FromMilliseconds(idleMs);
			}
			else
			{
				throw new Win32Exception(Marshal.GetLastWin32Error());
			}
		}

		DateTimeOffset GetLastInputTime()
		{
			return DateTimeOffset.Now.Subtract(GetInputIdleTime());
		}

		static class NativeMethods
		{
			public struct LastInputInfo
			{
				public UInt32 cbSize;
				public UInt32 dwTime;
			}

			[DllImport("user32.dll")]
			public static extern bool GetLastInputInfo(ref LastInputInfo plii);
		}

		#endregion

		#region screen saver

		const int SPI_GETSCREENSAVERRUNNING = 114;

		[DllImport("user32.dll", CharSet = CharSet.Auto)]
		static extern bool SystemParametersInfo(int uAction, int uParam, ref bool lpvParam, int flags);

		/// <summary>
		/// Returns TRUE if the screen saver is actually running
		/// </summary>
		/// <returns></returns>
		public bool GetScreenSaverRunning()
		{
			bool isRunning = false;
			SystemParametersInfo(SPI_GETSCREENSAVERRUNNING, 0, ref isRunning, 0);
			return isRunning;
		}

		#endregion

	}
}