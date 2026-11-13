using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using System.Xml.Serialization;
using System.Reflection;
using System.Runtime.InteropServices;
using System.ComponentModel;
using static WinDozer.IdleManager;
using System.IO.Pipes;


namespace WinDozer
{

	internal class PowerPlanManager
	{
		public enum ProcessorBoostModes
		{
			disabled = 0, // off (good for cool device)
			enabled = 1, // boost clock step by step (good for lowering temp spike frequency) 
			aggressive = 2, // boost clock to upper limitt directly (good for benchmarks/maximizing frames)
			efficient = 3, // lower clock(turns off boost) faster after usage
			efficientAggressive = 4, // Always select the highest possible target frequency above nominal frequency if hardware supports doing so efficiently.
			aggressiveAtGuaranteed = 5, // Always select the highest possible target frequency above guaranteed frequency.
			efficientAggressiveAtGuaranteed = 6, // Always select the highest possible target frequency above guaranteed frequency if hardware supports doing so efficiently.
		}

		public class PowerPlanWrapper
		{
			public Guid guid;
			public string name;
			public bool isManaged;

			public ProcessorBoostModes boostMode = ProcessorBoostModes.disabled;
			public uint displayTimeout = 0;
			public uint sleepTimeout = 0;
			public uint hibernateTimeout = 0;
			public uint procThrottleMin = 5;
			public uint procThrottleMax = 100;

			internal PowerPlanWrapper(Guid guid, string name)
			{
				this.guid = guid;
				this.name = name;
			}

			internal void Read()
			{
				boostMode = (ProcessorBoostModes)PowerPlansApiWrapper.GetPlanSetting(guid, SettingSubgroup.PROCESSOR_SETTINGS_SUBGROUP, Setting.PERFBOOSTMODE, PowerMode.AC);
				displayTimeout = PowerPlansApiWrapper.GetPlanSetting(guid, SettingSubgroup.VIDEO_SUBGROUP, Setting.VIDEOIDLE, PowerMode.AC) / 60;
				sleepTimeout = PowerPlansApiWrapper.GetPlanSetting(guid, SettingSubgroup.SLEEP_SUBGROUP, Setting.STANDBYIDLE, PowerMode.AC) / 60;
				hibernateTimeout = PowerPlansApiWrapper.GetPlanSetting(guid, SettingSubgroup.SLEEP_SUBGROUP, Setting.HIBERNATEIDLE, PowerMode.AC) /60;
				procThrottleMin = PowerPlansApiWrapper.GetPlanSetting(guid, SettingSubgroup.PROCESSOR_SETTINGS_SUBGROUP, Setting.PROCTHROTTLEMIN, PowerMode.AC);
				procThrottleMax = PowerPlansApiWrapper.GetPlanSetting(guid, SettingSubgroup.PROCESSOR_SETTINGS_SUBGROUP, Setting.PROCTHROTTLEMAX, PowerMode.AC);
			}

			internal void Write()
			{
				PowerPlansApiWrapper.SetPlanDescription(guid, "Custom PowerPlan managed by " + WinDozer.ProgramName);

				PowerPlansApiWrapper.SetPlanSetting(guid, SettingSubgroup.PROCESSOR_SETTINGS_SUBGROUP, Setting.PERFBOOSTMODE, PowerMode.DC, (uint)(int)ProcessorBoostModes.disabled);
				PowerPlansApiWrapper.SetPlanSetting(guid, SettingSubgroup.PROCESSOR_SETTINGS_SUBGROUP, Setting.PERFBOOSTMODE, PowerMode.AC, (uint)(int)boostMode);

				PowerPlansApiWrapper.SetPlanSetting(guid, SettingSubgroup.VIDEO_SUBGROUP, Setting.VIDEOIDLE, PowerMode.DC, displayTimeout * 60);
				PowerPlansApiWrapper.SetPlanSetting(guid, SettingSubgroup.VIDEO_SUBGROUP, Setting.VIDEOIDLE, PowerMode.AC, displayTimeout * 60);

				PowerPlansApiWrapper.SetPlanSetting(guid, SettingSubgroup.SLEEP_SUBGROUP, Setting.STANDBYIDLE, PowerMode.DC, sleepTimeout * 60);
				PowerPlansApiWrapper.SetPlanSetting(guid, SettingSubgroup.SLEEP_SUBGROUP, Setting.STANDBYIDLE, PowerMode.AC, sleepTimeout * 60);

				PowerPlansApiWrapper.SetPlanSetting(guid, SettingSubgroup.SLEEP_SUBGROUP, Setting.HIBERNATEIDLE, PowerMode.DC, hibernateTimeout * 60);
				PowerPlansApiWrapper.SetPlanSetting(guid, SettingSubgroup.SLEEP_SUBGROUP, Setting.HIBERNATEIDLE, PowerMode.AC, hibernateTimeout * 60);

				PowerPlansApiWrapper.SetPlanSetting(guid, SettingSubgroup.PROCESSOR_SETTINGS_SUBGROUP, Setting.PROCTHROTTLEMIN, PowerMode.DC, 5);
				PowerPlansApiWrapper.SetPlanSetting(guid, SettingSubgroup.PROCESSOR_SETTINGS_SUBGROUP, Setting.PROCTHROTTLEMIN, PowerMode.AC, procThrottleMin);

				PowerPlansApiWrapper.SetPlanSetting(guid, SettingSubgroup.PROCESSOR_SETTINGS_SUBGROUP, Setting.PROCTHROTTLEMAX, PowerMode.DC, 50);
				PowerPlansApiWrapper.SetPlanSetting(guid, SettingSubgroup.PROCESSOR_SETTINGS_SUBGROUP, Setting.PROCTHROTTLEMAX, PowerMode.AC, procThrottleMax);
			}

			public override string ToString()
			{
				return name + "(" + guid + ")";
			}
		}

		#region properties
		/*
		internal uint DisplayTimeoutIdle
		{
			get => displayTimeoutIdle;
			set
			{
				if (value != displayTimeoutIdle)
				{
					displayTimeoutIdle = value;
					prefsManager.SetPref(nameof(displayTimeoutIdle), value.ToString());
					ApplySettingsToManagedPowerPlans();
				}
			}
		}

		internal uint DisplayTimeoutBalanced
		{
			get => displayTimeoutBalanced;
			set
			{
				if (value != displayTimeoutBalanced)
				{
					displayTimeoutBalanced = value;
					prefsManager.SetPref(nameof(displayTimeoutBalanced), value.ToString());
					ApplySettingsToManagedPowerPlans();
				}
			}
		}

		internal uint DisplayTimeoutPerformance
		{
			get => displayTimeoutPerformance;
			set
			{
				if (value != displayTimeoutPerformance)
				{
					displayTimeoutPerformance = value;
					prefsManager.SetPref(nameof(displayTimeoutPerformance), value.ToString());
					ApplySettingsToManagedPowerPlans();
				}
			}
		}

		internal uint SleepTimeoutIdle
		{
			get => sleepTimeoutIdle;
			set
			{
				if (value != sleepTimeoutIdle)
				{
					sleepTimeoutIdle = value;
					prefsManager.SetPref(nameof(sleepTimeoutIdle), value.ToString());
					ApplySettingsToManagedPowerPlans();
				}
			}
		}

		internal uint SleepTimeoutBalanced
		{
			get => sleepTimeoutBalanced;
			set
			{
				if (value != sleepTimeoutBalanced)
				{
					sleepTimeoutBalanced = value;
					prefsManager.SetPref(nameof(sleepTimeoutBalanced), value.ToString());
					ApplySettingsToManagedPowerPlans();
				}
			}
		}

		internal uint SleepTimeoutPerformance
		{
			get => sleepTimeoutPerformance;
			set
			{
				if (value != sleepTimeoutPerformance)
				{
					sleepTimeoutPerformance = value;
					prefsManager.SetPref(nameof(sleepTimeoutPerformance), value.ToString());
					ApplySettingsToManagedPowerPlans();
				}
			}
		}

		//internal uint HibernateTimeoutIdle
		//{
		//	get => hibernateTimeoutIdle;
		//	set
		//	{
		//		if (value != hibernateTimeoutIdle)
		//		{
		//			hibernateTimeoutIdle = value;
		//			dm.SetPref(nameof(hibernateTimeoutIdle), value.ToString());
		//			ApplySettingsToManagedPowerPlans();
		//		}
		//	}
		//}

		//internal uint HibernateTimeoutBalanced
		//{
		//	get => hibernateTimeoutBalanced;
		//	set
		//	{
		//		if (value != hibernateTimeoutBalanced)
		//		{
		//			hibernateTimeoutBalanced = value;
		//			dm.SetPref(nameof(hibernateTimeoutBalanced), value.ToString());
		//			ApplySettingsToManagedPowerPlans();
		//		}
		//	}
		//}

		//internal uint HibernateTimeoutPerformance
		//{
		//	get => hibernateTimeoutPerformance;
		//	set
		//	{
		//		if (value != hibernateTimeoutPerformance)
		//		{
		//			hibernateTimeoutPerformance = value;
		//			dm.SetPref(nameof(hibernateTimeoutPerformance), value.ToString());
		//			ApplySettingsToManagedPowerPlans();
		//		}
		//	}
		//}
		*/
		#endregion

		#region fields

		//Balanced: 381b4222-f694-41f0-9685-ff5bb260df2e
		//High performance: 8c5e7fda-e8bf-4a96-9a85-a6e23a8c635c
		//Power saver: a1841308-3541-4fab-bc81-f71556f20b4a
		readonly Guid winPlanBalancedGuid = new Guid("381b4222-f694-41f0-9685-ff5bb260df2e");
		const string winPlanBalancedName = "Balanced";
		PowerPlanWrapper defaultPlan = null;

		const string managedPlanIdleName = WinDozer.ProgramName + "_Idle";
		const string managedPlanBalancedName = WinDozer.ProgramName + "_Balanced";
		const string managedPlanPerformanceName = WinDozer.ProgramName + "_Performance";

		PowerPlanWrapper managedIdle;
		PowerPlanWrapper managedBalanced;
		PowerPlanWrapper managedPerformance;
		Dictionary<Guid, PowerPlanWrapper> wrappers = new();

		PowerPlanWrapper selectedIdle;
		PowerPlanWrapper selectedBalanced;
		PowerPlanWrapper selectedPerformance;

		PrefsManager prefsManager;




		/*
		PowerPlanWrapper systemPlanBalanced = null;
		PowerPlanWrapper managedPlanIdle = null;
		PowerPlanWrapper managedPlanBalanced = null;
		PowerPlanWrapper managedPlanPerformance = null;
		Dictionary<string, PowerPlanWrapper> otherPlans = new Dictionary<string, PowerPlanWrapper>();

		PowerPlanWrapper selectedPlanIdle = null;
		PowerPlanWrapper selectedPlanBalanced = null;
		PowerPlanWrapper selectedPlanPerformance = null;
		*/

		//uint displayTimeoutIdle = 15;
		//uint displayTimeoutBalanced = 15;
		//uint displayTimeoutPerformance = 0;

		//uint sleepTimeoutIdle = 30;
		//uint sleepTimeoutBalanced = 0;
		//uint sleepTimeoutPerformance = 0;

		//uint hibernateTimeoutIdle = 60;
		//uint hibernateTimeoutBalanced = 0;
		//uint hibernateTimeoutPerformance = 0;

		#endregion

		internal PowerPlanManager(PrefsManager prefsManager)
		{
			this.prefsManager = prefsManager;
			Debug.Log("initializing power plan manager");

			ReadPlans();
		}

		void ReadPlans()
		{
			List<Guid> guids = PowerPlansApiWrapper.ListPlans();
			foreach (Guid planGuid in guids)
			{
				string planName = PowerPlansApiWrapper.GetPlanName(planGuid);

				// get or create wrapper
				PowerPlanWrapper wrapper = null;
				if (!wrappers.ContainsKey(planGuid))
				{
					wrapper = new PowerPlanWrapper(planGuid, planName);
					wrapper.isManaged = false;
					wrappers.Add(planGuid, wrapper);
				}
				else
				{
					wrapper = wrappers[planGuid];
				}

				// read plan and save in wrapper
				wrapper.Read();

				// in case status plans didn't exists, check if they've been created since last read
				if (managedIdle == null && planName.Equals(managedPlanIdleName)) managedIdle = wrapper;
				if (managedBalanced == null && planName.Equals(managedPlanBalancedName)) managedBalanced = wrapper;
				if (managedPerformance == null && planName.Equals(managedPlanPerformanceName)) managedPerformance = wrapper;
			}

			// remove deleted plans
			List<Guid> toRemove = new();
			foreach (var v in wrappers)
			{
				if (!guids.Contains(v.Key)) toRemove.Add(v.Key);
			}
			foreach (var v in toRemove)
			{
				wrappers.Remove(v);
			}

			// find selected plans from prefs
			selectedIdle = FindWrapperWithName(prefsManager.GetPref(nameof(selectedIdle), managedPlanIdleName));
			selectedBalanced = FindWrapperWithName(prefsManager.GetPref(nameof(selectedBalanced), managedPlanIdleName));
			selectedPerformance = FindWrapperWithName(prefsManager.GetPref(nameof(selectedPerformance), managedPlanIdleName));
		}

		PowerPlanWrapper FindWrapperWithName(string planName)
		{
			if (!string.IsNullOrEmpty(planName))
			{
				foreach (var v in wrappers)
				{
					if (v.Value.name.Equals(planName)) return v.Value;
				}
			}
			Debug.LogError($"no plan found with name: {planName}");
			return null;
		}

		public PowerPlanWrapper FindWrapperForStatus(IdleManager.Status status)
		{
			switch (status)
			{
				case Status.Idle: return selectedIdle; break;
				case Status.Balanced: return selectedBalanced; break;
				case Status.Performance: return selectedPerformance; break;
			}
			return null;
		}

		public bool HasManagedPlansInstalled()
		{
			return managedIdle != null && managedBalanced != null && managedPerformance != null;
		}

		public void InstallMissingManagedPlans()
		{
			ReadPlans();

			// find windows default plan
			PowerPlanWrapper template = null;

			// find by guid
			if (wrappers.ContainsKey(winPlanBalancedGuid))
			{
				template = wrappers[winPlanBalancedGuid];
			}

			// find by (english) name
			if (template == null)
			{
				template = FindWrapperWithName(winPlanBalancedName);
			}

			// else use first available plan
			if (template == null)
			{
				template = wrappers.First().Value;
			}

			// must have balanced power plan
			if (template == null)
			{
				string s = "Cannot install managed PowerPlans, no default found";
				Debug.LogError(s);
				return;
			}

			PowerPlanWrapper InstallManagedPowerPlan(string newName, uint displayTimeout, uint sleepTimeout, uint hibernateTimeout, ProcessorBoostModes boostMode, uint procThrottleMin, uint procThrottleMax)
			{
				Debug.Log("installing managed PowerPlan " + newName);
				Guid results = PowerPlansApiWrapper.DuplicatePlan(template.guid);
				List<Guid> plans = PowerPlansApiWrapper.ListPlans();
				if (plans.Contains(results))
				{
					// set new plan settings
					PowerPlansApiWrapper.SetPlanName(results, newName);
					PowerPlanWrapper wrapper = new(results, newName);
					wrapper.isManaged = true;
					wrapper.displayTimeout = displayTimeout;
					wrapper.sleepTimeout = sleepTimeout;
					wrapper.hibernateTimeout = hibernateTimeout;
					wrapper.boostMode = boostMode;
					wrapper.procThrottleMin = procThrottleMin;
					wrapper.procThrottleMax = procThrottleMax;
					wrapper.Write();
					wrappers.Add(results, wrapper);
					return wrapper;
				}
				else Debug.LogError("failed to duplicate template power plan");
				return null;
			}

			// install managed plans and save wrappers
			Debug.Log("installing missing managed PowerPlans");
			if (managedIdle == null) managedIdle = InstallManagedPowerPlan(managedPlanIdleName, 15, 30, 60, ProcessorBoostModes.disabled, 5, 40);
			if (managedBalanced == null) managedBalanced = InstallManagedPowerPlan(managedPlanBalancedName, 30, 0, 0, ProcessorBoostModes.disabled, 5, 100);
			if (managedPerformance == null) managedPerformance = InstallManagedPowerPlan(managedPlanPerformanceName, 0, 0, 0, ProcessorBoostModes.efficientAggressiveAtGuaranteed, 5, 100);

			// also save newly installed plans and wrappers as selected
			selectedIdle = managedIdle;
			selectedBalanced = managedBalanced;
			selectedPerformance = managedPerformance;
			Save();

			// read just in case
			ReadPlans();
		}

		public void Save()
		{
			prefsManager.SetPref(nameof(selectedIdle), selectedIdle?.name);
			prefsManager.SetPref(nameof(selectedBalanced), selectedBalanced?.name);
			prefsManager.SetPref(nameof(selectedPerformance), selectedPerformance?.name);
		}



		internal string GetActivePowerPlanName()
		{
			return PowerPlansApiWrapper.GetActivePlanName();
		}

		internal List<string> GetAllPowerPlansNames()
		{
			List<string> names = new List<string>();
			foreach(var v in wrappers)
			{
				names.Add(v.Value.name);
			}
			return names;
		}

		internal string GetSelectedPowerPlanNameForStatus(Status status)
		{
			switch (status)
			{
				case Status.Idle: return selectedIdle?.name;
				case Status.Balanced: return selectedBalanced?.name;
				case Status.Performance: return selectedPerformance?.name;
				default: throw new Exception();
			}
		}

		internal void SelectPowerPlanForStatus(string name, Status status)
		{
			Debug.Log("selecting PowerPlan with name " + name + " for status " + status);
			PowerPlanWrapper powerPlan = FindWrapperWithName(name);
			if (powerPlan == null)
			{
				Debug.LogError("cannot select PowerPlan with name " + name + " for status " + status);
				return;
			}

			switch (status)
			{
				case Status.Idle:
					selectedIdle = powerPlan;
					break;
				case Status.Balanced:
					selectedBalanced = powerPlan;
					break;
				case Status.Performance:
					selectedPerformance = powerPlan;
					break;
				default: throw new Exception();
			}

			// if the app is currently in that status, switch to the new plan right away
			if (App.WinDozer.PowerPlanManager is not { } ppm || App.WinDozer.IdleManager is not { } idleManager) return;
			if (idleManager.CurrentStatus == status) ppm.ApplyPowerPlanForStatus(status);

			Save();
		}

		internal void ApplyPowerPlanForStatus(Status status)
		{
			Debug.Log("applying PowerPlan for status " + status);
			switch (status)
			{
				case Status.Idle: ApplyPowerPlan(selectedIdle); break;
				case Status.Balanced: ApplyPowerPlan(selectedBalanced); break;
				case Status.Performance: ApplyPowerPlan(selectedPerformance); break;
			}
		}

		void ApplyPowerPlan(PowerPlanWrapper plan)
		{
			try
			{
				// must have plan
				if (plan == null)
				{
					Debug.LogError("cannot apply PowerPlan: target plan is null");
					return;
				}

				// must be different
				if (PowerPlansApiWrapper.GetActivePlan() == plan.guid)
				{
					//Debug.LogWarning("cannot apply PowerPlan: plan " + plan.name + " is already active");
					return;
				}

				// apply
				Debug.LogWarning($"applying PowerPlan: {plan.name}");
				PowerPlansApiWrapper.SetActivePlan(plan.guid);

				// check
				if (PowerPlansApiWrapper.GetActivePlan() != plan.guid)
				{
					Debug.LogError($"failed to apply PowerPlan {plan.guid}, current is {PowerPlansApiWrapper.GetActivePlan()}");
				}
			}
			catch (Exception ex)
			{
				Debug.LogError($"exception applying power plan: {plan}\n{ex}");
			}
		}

		void UninstallManagedPlans()
		{
			// TODO check if in use
			if (managedIdle != null) PowerPlansApiWrapper.DeletePlan(managedIdle.guid);
			if (managedBalanced != null) PowerPlansApiWrapper.DeletePlan(managedBalanced.guid);
			if (managedPerformance != null) PowerPlansApiWrapper.DeletePlan(managedPerformance.guid);
		}

	}
}
