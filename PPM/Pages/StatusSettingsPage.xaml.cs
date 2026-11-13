using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Security.Authentication.Web.Core;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace WinDozer.Pages
{
	/// <summary>
	/// An empty page that can be used on its own or navigated to within a Frame.
	/// </summary>
	public sealed partial class StatusSettingsPage : Page
	{
		public StatusSettingsPage()
		{
			InitializeComponent();	
		}

		IdleManager.Status currentStatus;
		PowerPlanManager.PowerPlanWrapper currentWrapper;
		bool ignoreEvents = false;

		protected override void OnNavigatedTo(NavigationEventArgs e)
		{
			base.OnNavigatedTo(e);

			currentStatus = (IdleManager.Status)e.Parameter;
			TitleText.Text = currentStatus.ToString();

			// get plan for current status
			if (App.WinDozer.PowerPlanManager is not { } ppm) return;
			currentWrapper = ppm.FindWrapperForStatus(currentStatus);
			if (currentWrapper == null)
			{
				Debug.LogError("no wrapper found for status: " + currentStatus);
				return;
			}

			ShowWrapperValues();
			
		}

		void ShowWrapperValues()
		{
			if (currentWrapper == null) return;
			if (ignoreEvents) return;
			ignoreEvents = true;
			try
			{
				MinProcStateBox.Value = currentWrapper.procThrottleMin;
				MaxProcStateBox.Value = currentWrapper.procThrottleMax;
				DisplayTimeoutBox.Value = currentWrapper.displayTimeout;
				SleepTimeoutBox.Value = currentWrapper.sleepTimeout;
				HibernationTimeoutBox.Value = currentWrapper.hibernateTimeout;

				// show selected plan for selectedStatus
				if (App.WinDozer.PowerPlanManager is not { } ppm) return;
				var names = ppm.GetAllPowerPlansNames();
				PlanCombo.ItemsSource = names;
				PlanCombo.SelectedItem = ppm.GetSelectedPowerPlanNameForStatus(currentStatus);

				// show selected boost mode
				var boostModes = Enum.GetNames(typeof(PowerPlanManager.ProcessorBoostModes));
				ProcPerfBoost.ItemsSource = boostModes;
				ProcPerfBoost.SelectedItem = currentWrapper.boostMode.ToString();
			}
			catch (Exception ex)
			{
				Debug.LogError(ex.ToString());
			}
			finally
			{
				ignoreEvents = false;
			}
		}

		void WriteWrapperValues()
		{
			if (App.WinDozer.PowerPlanManager is not { } plans || App.WinDozer.IdleManager is not { } idle) return;
			if (currentWrapper == null) return;
			if (ignoreEvents) return;

			bool apply = false;

			PowerPlanManager.ProcessorBoostModes boost = (PowerPlanManager.ProcessorBoostModes)Enum.Parse(typeof(PowerPlanManager.ProcessorBoostModes), (string)ProcPerfBoost.SelectedItem);
			if (currentWrapper.boostMode != boost)
			{
				currentWrapper.boostMode = boost;
				apply = true;
			}
			if (currentWrapper.procThrottleMin != (uint)MinProcStateBox.Value)
			{
				currentWrapper.procThrottleMin = (uint)MinProcStateBox.Value;
				apply = true;
			}
			if (currentWrapper.procThrottleMax != (uint)MaxProcStateBox.Value)
			{
				currentWrapper.procThrottleMax = (uint)MaxProcStateBox.Value;
				apply = true;
			}
			if (currentWrapper.displayTimeout != (uint)DisplayTimeoutBox.Value)
			{
				currentWrapper.displayTimeout = (uint)DisplayTimeoutBox.Value;
				apply = true;
			}
			if (currentWrapper.sleepTimeout != (uint)SleepTimeoutBox.Value)
			{
				currentWrapper.sleepTimeout = (uint)SleepTimeoutBox.Value;
				apply = true;
			}
			if (currentWrapper.hibernateTimeout != (uint)HibernationTimeoutBox.Value)
			{
				currentWrapper.hibernateTimeout = (uint)HibernationTimeoutBox.Value;
				apply = true;
			}

			if (apply) currentWrapper.Write();
			currentWrapper.Read();
			ShowWrapperValues();
		}

		void PlanCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			if (App.WinDozer.PowerPlanManager is not { } plans || App.WinDozer.IdleManager is not { } idle) return;
			if (sender is not ComboBox { SelectedItem: string name } box) return;
			if (ignoreEvents) return;

			// change wrapper for status
			plans.SelectPowerPlanForStatus(name, currentStatus);

			if (App.WinDozer.PowerPlanManager is not { } ppm) return;
			currentWrapper = ppm.FindWrapperForStatus(currentStatus);
			if (currentWrapper == null)
			{
				Debug.LogError("no wrapper found for status: " + currentStatus);
				return;
			}

			ShowWrapperValues();
		}

		void ProcStateBox_ValueChanged(NumberBox sender, NumberBoxValueChangedEventArgs args)
		{
			WriteWrapperValues();
		}

		void ProcPerfBoost_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			WriteWrapperValues();
		}

		void Timeouts_ValueChanged(NumberBox sender, NumberBoxValueChangedEventArgs args)
		{
			WriteWrapperValues();
		}

		

		
	}
}
