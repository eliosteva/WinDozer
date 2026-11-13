using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Text;

namespace WinDozer
{


	public enum ErrorCode : uint
	{
		SUCCESS = 0x000,
		FILE_NOT_FOUND = 0x002,
		ERROR_INVALID_PARAMETER = 0x057,
		ERROR_ALREADY_EXISTS = 0x0B7,
		MORE_DATA = 0x0EA,
		NO_MORE_ITEMS = 0x103
	}

	public enum AccessFlags : uint
	{
		ACCESS_SCHEME = 16,
		ACCESS_SUBGROUP = 17,
		ACCESS_INDIVIDUAL_SETTING = 18
	}

	public enum SettingSubgroup
	{
		NO_SUBGROUP,
		DISK_SUBGROUP,
		SYSTEM_BUTTON_SUBGROUP,
		PROCESSOR_SETTINGS_SUBGROUP,
		VIDEO_SUBGROUP,
		BATTERY_SUBGROUP,
		SLEEP_SUBGROUP,
		PCIEXPRESS_SETTINGS_SUBGROUP
	}

	public enum Setting
	{
		BATACTIONCRIT,
		BATACTIONLOW,
		BATFLAGSLOW,
		BATLEVELCRIT,
		BATLEVELLOW,
		LIDACTION,
		PBUTTONACTION,
		SBUTTONACTION,
		UIBUTTON_ACTION,
		DISKIDLE,
		ASPM,
		PROCFREQMAX,
		PROCTHROTTLEMAX,
		PROCTHROTTLEMIN,
		PERFBOOSTMODE,
		SYSCOOLPOL,
		HIBERNATEIDLE,
		HYBRIDSLEEP,
		RTCWAKE,
		STANDBYIDLE,
		ADAPTBRIGHT,
		VIDEOIDLE
	}

	public static class SettingIdLookup
	{
		public static Dictionary<SettingSubgroup, Guid> SettingSubgroupGuids = new Dictionary<SettingSubgroup, Guid>
		{
			{ SettingSubgroup.NO_SUBGROUP,                  new Guid("fea3413e-7e05-4911-9a71-700331f1c294") },
			{ SettingSubgroup.DISK_SUBGROUP,                new Guid("0012ee47-9041-4b5d-9b77-535fba8b1442") },
			{ SettingSubgroup.SYSTEM_BUTTON_SUBGROUP,       new Guid("4f971e89-eebd-4455-a8de-9e59040e7347") },
			{ SettingSubgroup.PROCESSOR_SETTINGS_SUBGROUP,  new Guid("54533251-82be-4824-96c1-47b60b740d00") },
			{ SettingSubgroup.VIDEO_SUBGROUP,               new Guid("7516b95f-f776-4464-8c53-06167f40cc99") },
			{ SettingSubgroup.BATTERY_SUBGROUP,             new Guid("e73a048d-bf27-4f12-9731-8b2076e8891f") },
			{ SettingSubgroup.SLEEP_SUBGROUP,               new Guid("238C9FA8-0AAD-41ED-83F4-97BE242C8F20") },
			{ SettingSubgroup.PCIEXPRESS_SETTINGS_SUBGROUP, new Guid("501a4d13-42af-4429-9fd1-a8218c268e20") }
		};

		public static Dictionary<Setting, Guid> SettingGuids = new Dictionary<Setting, Guid>
		{
			{ Setting.BATACTIONCRIT,    new Guid("637ea02f-bbcb-4015-8e2c-a1c7b9c0b546") },
			{ Setting.BATACTIONLOW,     new Guid("d8742dcb-3e6a-4b3c-b3fe-374623cdcf06") },
			{ Setting.BATFLAGSLOW,      new Guid("bcded951-187b-4d05-bccc-f7e51960c258") },
			{ Setting.BATLEVELCRIT,     new Guid("9a66d8d7-4ff7-4ef9-b5a2-5a326ca2a469") },
			{ Setting.BATLEVELLOW,      new Guid("8183ba9a-e910-48da-8769-14ae6dc1170a") },
			{ Setting.LIDACTION,        new Guid("5ca83367-6e45-459f-a27b-476b1d01c936") },
			{ Setting.PBUTTONACTION,    new Guid("7648efa3-dd9c-4e3e-b566-50f929386280") },
			{ Setting.SBUTTONACTION,    new Guid("96996bc0-ad50-47ec-923b-6f41874dd9eb") },
			{ Setting.UIBUTTON_ACTION,  new Guid("a7066653-8d6c-40a8-910e-a1f54b84c7e5") },
			{ Setting.DISKIDLE,         new Guid("6738e2c4-e8a5-4a42-b16a-e040e769756e") },
			{ Setting.ASPM,             new Guid("ee12f906-d277-404b-b6da-e5fa1a576df5") },
			{ Setting.PROCFREQMAX,      new Guid("75b0ae3f-bce0-45a7-8c89-c9611c25e100") },
			{ Setting.PROCTHROTTLEMAX,  new Guid("bc5038f7-23e0-4960-96da-33abaf5935ec") },
			{ Setting.PROCTHROTTLEMIN,  new Guid("893dee8e-2bef-41e0-89c6-b55d0929964c") },
			{ Setting.PERFBOOSTMODE,    new Guid("be337238-0d82-4146-a960-4f3749d470c7") },
			{ Setting.SYSCOOLPOL,       new Guid("94d3a615-a899-4ac5-ae2b-e4d8f634367f") },
			{ Setting.HIBERNATEIDLE,    new Guid("9d7815a6-7ee4-497e-8888-515a05f02364") },
			{ Setting.HYBRIDSLEEP,      new Guid("94ac6d29-73ce-41a6-809f-6363ba21b47e") },
			{ Setting.RTCWAKE,          new Guid("bd3b718a-0680-4d9d-8ab2-e1d2b4ac806d") },
			{ Setting.STANDBYIDLE,      new Guid("29f6c1db-86da-48c5-9fdb-f2b67b1f44da") },
			{ Setting.ADAPTBRIGHT,      new Guid("fbd9aa66-9553-4097-ba44-ed6e9d65eab8") },
			{ Setting.VIDEOIDLE,        new Guid("3c0bc021-c8a8-4e07-a973-6b14cbcb2b7e") }
		};
	}


	[Flags]
	internal enum PowerMode
	{
		AC = 1,
		DC = 2,
	}

	internal static class PowerPlansApiWrapper
	{

		#region DLL Imports

		[DllImport("PowrProf.dll", CharSet = CharSet.Auto, ExactSpelling = true)]
		public static extern bool SetSuspendState(bool hiberate, bool forceCritical, bool disableWakeEvent);

		[DllImport("powrprof.dll")]
		private static extern uint PowerEnumerate(
			[In, Optional] IntPtr RootPowerKey,
			[In, Optional] IntPtr SchemeGuid,
			[In, Optional] IntPtr SubGroupOfPowerSettingsGuid,
			[In] AccessFlags AccessFlags,
			[In] uint Index,
			[Out, Optional] IntPtr Buffer,
			[In, Out] ref uint BufferSize
		);

		[DllImport("powrprof.dll")]
		private static extern uint PowerGetActiveScheme(
			[In, Optional] IntPtr UserPowerKey,
			[Out] out IntPtr ActivePolicyGuid
		);

		[DllImport("powrprof.dll")]
		private static extern uint PowerSetActiveScheme(
			[In, Optional] IntPtr UserPowerKey,
			[In] ref Guid ActivePolicyGuid
		);

		[DllImport("powrprof.dll")]
		private static extern uint PowerDuplicateScheme(
			[In, Optional] IntPtr RootPowerKey,
			[In] ref Guid SourceSchemeGuid,
			[In] ref IntPtr DestinationSchemeGuid
		);

		[DllImport("powrprof.dll")]
		private static extern uint PowerDeleteScheme(
			[In, Optional] IntPtr RootPowerKey,
			[In] ref Guid SchemeGuid
		);

		[DllImport("powrprof.dll")]
		private static extern uint PowerReadFriendlyName(
			[In, Optional] IntPtr RootPowerKey,
			[In, Optional] ref Guid SchemeGuid,
			[In, Optional] IntPtr SubGroupOfPowerSettingsGuid,
			[In, Optional] IntPtr PowerSettingGuid,
			[Out, Optional] IntPtr Buffer,
			[In, Out] ref uint BufferSize
		);

		[DllImport("powrprof.dll", CharSet = CharSet.Unicode)]
		private static extern uint PowerWriteFriendlyName(
			[In, Optional] IntPtr RootPowerKey,
			[In] ref Guid SchemeGuid,
			[In, Optional] IntPtr SubGroupOfPowerSettingsGuid,
			[In, Optional] IntPtr PowerSettingGuid,
			[In] string Buffer,
			[In] UInt32 BufferSize
		);

		[DllImport("powrprof.dll")]
		private static extern uint PowerReadDescription(
			[In, Optional] IntPtr RootPowerKey,
			[In, Optional] ref Guid SchemeGuid,
			[In, Optional] IntPtr SubGroupOfPowerSettingsGuid,
			[In, Optional] IntPtr PowerSettingGuid,
			[Out, Optional] IntPtr Buffer,
			[In, Out] ref uint BufferSize
		);

		[DllImport("powrprof.dll", CharSet = CharSet.Unicode)]
		private static extern uint PowerWriteDescription(
			[In, Optional] IntPtr RootPowerKey,
			[In] ref Guid SchemeGuid,
			[In, Optional] IntPtr SubGroupOfPowerSettingsGuid,
			[In, Optional] IntPtr PowerSettingGuid,
			[In] string Buffer,
			[In] UInt32 BufferSize
		);

		[DllImport("powrprof.dll")]
		private static extern uint PowerReadACValueIndex(
			[In, Optional] IntPtr RootPowerKey,
			[In, Optional] ref Guid SchemeGuid,
			[In, Optional] ref Guid SubGroupOfPowerSettingsGuid,
			[In, Optional] ref Guid PowerSettingGuid,
			[Out] out uint AcValueIndex
		);

		[DllImport("powrprof.dll")]
		private static extern uint PowerWriteACValueIndex(
			[In, Optional] IntPtr RootPowerKey,
			[In] ref Guid SchemeGuid,
			[In, Optional] ref Guid SubGroupOfPowerSettingsGuid,
			[In, Optional] ref Guid PowerSettingGuid,
			[In] uint AcValueIndex
		);

		[DllImport("powrprof.dll")]
		private static extern uint PowerReadDCValueIndex(
			[In, Optional] IntPtr RootPowerKey,
			[In, Optional] ref Guid SchemeGuid,
			[In, Optional] ref Guid SubGroupOfPowerSettingsGuid,
			[In, Optional] ref Guid PowerSettingGuid,
			[Out] out uint DcValueIndex
		);

		[DllImport("powrprof.dll")]
		private static extern uint PowerWriteDCValueIndex(
			[In, Optional] IntPtr RootPowerKey,
			[In] ref Guid SchemeGuid,
			[In, Optional] ref Guid SubGroupOfPowerSettingsGuid,
			[In, Optional] ref Guid PowerSettingGuid,
			[In] uint DcValueIndex
		);


		[DllImport("kernel32.dll")]
		private static extern IntPtr LocalFree(
			[In] IntPtr hMem
		);
		#endregion

		internal static string GetActivePlanName()
		{
			var guid = GetActivePlan();
			return GetPlanName(guid) + " (" + guid.ToString() + ")"; ;
		}

		/// <summary>
		/// Gets the currently active power plan
		/// </summary>
		/// <returns>Guid of the currently acvtive plan</returns>
		internal static Guid GetActivePlan()
		{
			IntPtr activePolicyGuidPtr = IntPtr.Zero;
			var res = PowerGetActiveScheme(IntPtr.Zero, out activePolicyGuidPtr);

			if (res != (uint)ErrorCode.SUCCESS)
				throw new Win32Exception((int)res);

			var guid = Marshal.PtrToStructure<Guid>(activePolicyGuidPtr);

			LocalFree(activePolicyGuidPtr);

			return guid;
		}

		/// <summary>
		/// Sets the active power plan
		/// </summary>
		/// <param name="planId">The plan that should be set active.</param>
		internal static void SetActivePlan(Guid planId)
		{
			var res = PowerSetActiveScheme(IntPtr.Zero, ref planId);

			if (res != (uint)ErrorCode.SUCCESS)
				throw new Win32Exception((int)res);
		}

		/// <summary>
		/// Gets the friendly name of a power plan
		/// </summary>
		/// <param name="planId">The Guid of the power plan</param>
		/// <returns>Plan name</returns>
		internal static string GetPlanName(Guid planId)
		{
			uint bufferSize = 255;
			IntPtr buffer = Marshal.AllocHGlobal((int)bufferSize);

			try
			{
				var res = PowerReadFriendlyName(IntPtr.Zero, ref planId, IntPtr.Zero, IntPtr.Zero, buffer, ref bufferSize);

				if (res == (uint)ErrorCode.MORE_DATA)
				{
					// The buffer was too small. The API function has already updated the value that bufferSize points to 
					// to be the needed size, so all we need is to create a buffer of that size and run the API call again.
					Marshal.FreeHGlobal(buffer);
					buffer = Marshal.AllocHGlobal((int)bufferSize);
					res = PowerReadFriendlyName(IntPtr.Zero, ref planId, IntPtr.Zero, IntPtr.Zero, buffer, ref bufferSize);
				}

				if (res != (uint)ErrorCode.SUCCESS)
					throw new Win32Exception((int)res);

				return Marshal.PtrToStringUni(buffer);
			}
			finally
			{
				Marshal.FreeHGlobal(buffer);
			}
		}

		/// <summary>
		/// Sets the friendly name of a power plan
		/// </summary>
		/// <param name="planId">The Guid of the power plan</param>
		/// <param name="name">The new name</param>
		internal static void SetPlanName(Guid planId, string name)
		{
			name += char.MinValue; // Null-terminate the name string.
			uint bufferSize = (uint)Encoding.Unicode.GetByteCount(name);

			var res = PowerWriteFriendlyName(IntPtr.Zero, ref planId, IntPtr.Zero, IntPtr.Zero, name, bufferSize);

			if (res != (uint)ErrorCode.SUCCESS)
				throw new Win32Exception((int)res);
		}

		/// <summary>
		/// Returns the description of a power plan.
		/// </summary>
		/// <param name="planId">Guid for the plan</param>
		/// <returns>Description</returns>
		internal static string GetPlanDescription(Guid planId)
		{
			uint bufferSize = 255;
			IntPtr buffer = Marshal.AllocHGlobal((int)bufferSize);

			try
			{
				var res = PowerReadDescription(IntPtr.Zero, ref planId, IntPtr.Zero, IntPtr.Zero, buffer, ref bufferSize);

				if (res == (uint)ErrorCode.MORE_DATA)
				{
					// The buffer was too small. The API function has already updated the value that bufferSize points to 
					// to be the needed size, so all we need is to create a buffer of that size and run the API call again.
					Marshal.FreeHGlobal(buffer);
					buffer = Marshal.AllocHGlobal((int)bufferSize);
					res = PowerReadDescription(IntPtr.Zero, ref planId, IntPtr.Zero, IntPtr.Zero, buffer, ref bufferSize);
				}

				if (res != (uint)ErrorCode.SUCCESS)
					throw new Win32Exception((int)res);

				return Marshal.PtrToStringUni(buffer);
			}
			finally
			{
				Marshal.FreeHGlobal(buffer);
			}
		}

		/// <summary>
		/// Sets the description of a power plan
		/// </summary>
		/// <param name="planId">The Guid of the power plan</param>
		/// <param name="description">The new description</param>
		internal static void SetPlanDescription(Guid planId, string description)
		{
			description += char.MinValue; // Null-terminate the description string.
			uint bufferSize = (uint)Encoding.Unicode.GetByteCount(description);

			var res = PowerWriteDescription(IntPtr.Zero, ref planId, IntPtr.Zero, IntPtr.Zero, description, bufferSize);

			if (res != (uint)ErrorCode.SUCCESS)
				throw new Win32Exception((int)res);
		}

		/// <summary>
		/// Creates a new power plan based on the provided source plan
		/// </summary>
		/// <param name="sourcePlanId">The Guid for the source plan.</param>
		/// <param name="targetPlanId">The Guid for the new plan to be created. If no guid is supplied one will be created.</param>
		/// <returns></returns>
		internal static Guid DuplicatePlan(Guid sourcePlanId, Guid targetPlanId = new Guid())
		{
			if (targetPlanId == Guid.Empty)
				targetPlanId = Guid.NewGuid();

			var targetPlanPtr = Marshal.AllocHGlobal(Marshal.SizeOf(targetPlanId));
			uint res;

			try
			{
				Marshal.StructureToPtr(targetPlanId, targetPlanPtr, false);
				res = PowerDuplicateScheme(IntPtr.Zero, ref sourcePlanId, ref targetPlanPtr);
			}
			finally
			{
				Marshal.FreeHGlobal(targetPlanPtr);
			}

			if (res != (uint)ErrorCode.SUCCESS)
				throw new Win32Exception((int)res);

			return targetPlanId;
		}

		/// <summary>
		/// Deletes the specified power plan
		/// </summary>
		/// <param name="planId">Guid for the power plan to be deleted</param>
		internal static void DeletePlan(Guid planId)
		{
			var res = PowerDeleteScheme(IntPtr.Zero, ref planId);

			if (res != (uint)ErrorCode.SUCCESS)
				throw new Win32Exception((int)res);
		}

		/// <summary>
		/// Deletes the specified power plan if it exists. If it does not, function returns without throwing an error.
		/// </summary>
		/// <param name="planId">Guid for the power plan to be deleted</param>
		internal static void DeletePlanIfExists(Guid planId)
		{
			if (!PlanExists(planId))
				return;

			DeletePlan(planId);
		}

		/// <summary>
		/// Gets the value for the specified power plan, power mode and setting
		/// </summary>
		/// <param name="plan">Guid of the power plan</param>
		/// <param name="subgroup">The subgroup to look in</param>
		/// <param name="setting">The settign to look up</param>
		/// <param name="powerMode">Power mode. AC or DC, but not both.</param>
		/// <returns>The active index value for the specified setting</returns>
		internal static uint GetPlanSetting(Guid plan, SettingSubgroup subgroup, Setting setting, PowerMode powerMode)
		{
			if (powerMode == (PowerMode.AC | PowerMode.DC))
				throw new ArgumentException("Can't get both AC and DC values at the same time, because they may be different.");

			Guid subgroupId = SettingIdLookup.SettingSubgroupGuids[subgroup];
			Guid settingId = SettingIdLookup.SettingGuids[setting];

			uint value = 0;
			uint res = 0;

			if (powerMode.HasFlag(PowerMode.AC))
			{
				res = PowerReadACValueIndex(IntPtr.Zero, ref plan, ref subgroupId, ref settingId, out value);
			}
			else if (powerMode.HasFlag(PowerMode.DC))
			{
				res = PowerReadDCValueIndex(IntPtr.Zero, ref plan, ref subgroupId, ref settingId, out value);
			}

			if (res != (uint)ErrorCode.SUCCESS)
				throw new Win32Exception((int)res);

			return value;
		}

		/// <summary>
		/// Alters a setting on a power plan.
		/// </summary>
		/// <param name="plan">The Guid for the plan you are changing</param>
		/// <param name="subgroup">The Guid for the subgroup the setting belongs to</param>
		/// <param name="setting">The Guid for the setting you are changing</param>
		/// <param name="powerMode">You can chose to alter the AC value, the DC value or both using the bitwise OR operator (|) to join the flags.</param>
		/// <param name="value">The new value for the setting. Run <code>powercfg -q</code> from the command line to list possible values</param>
		internal static void SetPlanSetting(Guid plan, SettingSubgroup subgroup, Setting setting, PowerMode powerMode, uint value)
		{
			Guid subgroupId = SettingIdLookup.SettingSubgroupGuids[subgroup];
			Guid settingId = SettingIdLookup.SettingGuids[setting];

			if (powerMode.HasFlag(PowerMode.AC))
			{
				var res = PowerWriteACValueIndex(IntPtr.Zero, ref plan, ref subgroupId, ref settingId, value);
				if (res != (uint)ErrorCode.SUCCESS)
					throw new Win32Exception((int)res);
			}
			if (powerMode.HasFlag(PowerMode.DC))
			{
				var res = PowerWriteDCValueIndex(IntPtr.Zero, ref plan, ref subgroupId, ref settingId, value);
				if (res != (uint)ErrorCode.SUCCESS)
					throw new Win32Exception((int)res);
			}
		}

		/// <summary>
		/// Creates a list of all the power plan Guids on this PC. The Guids can be used to look up more information (name, settings, etc.) about each plan.
		/// </summary>
		/// <returns>List of power plan Guids</returns>
		internal static List<Guid> ListPlans()
		{
			var powerPlans = new List<Guid>();

			IntPtr buffer;
			uint bufferSize = 16;

			uint index = 0;
			uint ret = 0;

			while (ret == 0)
			{
				buffer = Marshal.AllocHGlobal((int)bufferSize);

				try
				{
					ret = PowerEnumerate(IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, AccessFlags.ACCESS_SCHEME, index, buffer, ref bufferSize);

					if (ret == (uint)ErrorCode.NO_MORE_ITEMS) break;
					if (ret != (uint)ErrorCode.SUCCESS)
						throw new Win32Exception((int)ret);

					Guid guid = Marshal.PtrToStructure<Guid>(buffer);
					powerPlans.Add(guid);
				}
				finally
				{
					Marshal.FreeHGlobal(buffer);
				}

				index++;
			}

			return powerPlans;
		}

		/// <summary>
		/// Checks if a power plan identified by the given Guid exists
		/// </summary>
		/// <param name="planId">The Guid to check</param>
		/// <returns>True if the Guid matches a power plan. False if not.</returns>
		internal static bool PlanExists(Guid planId)
		{
			return ListPlans().Exists(p => p == planId);
		}

	}
}
