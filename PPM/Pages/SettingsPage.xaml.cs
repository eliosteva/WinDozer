using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Animation;

namespace WinDozer.Pages
{
	public sealed partial class SettingsPage : Page
	{

		bool loading;


		public SettingsPage()
		{
			InitializeComponent();

			IconIdle.Glyph = WinDozer.gliphIdle;
			IconBalanced.Glyph = WinDozer.gliphBalanced;
			IconPerformance.Glyph = WinDozer.gliphPerformance;

			LoadSettings();
		}

		void LoadSettings()
		{
			if (App.WinDozer.IdleManager is not { } idle) return;

			loading = true;
			{
				StartupToggle.IsOn = App.WinDozer.PrefsManager.IsAutoStartEnabled();
				ScreensaverToggle.IsOn = idle.IdleOnScreensaver;
				ActivityToggle.IsOn = idle.IdleOnTimeout;
				InactivityBox.Value = idle.InputTimeout;
				PollingBox.Value = idle.PollingInterval;
				

				//if (App.WinDozer.PowerPlanManager is not { } plans) return;
				//var names = plans.GetAllPowerPlansNames();
				//foreach (var (box, status) in new[]
				//{
				//	(IdlePlanCombo,     IdleManager.Status.Idle),
				//	(BalancedPlanCombo, IdleManager.Status.Balanced),
				//	(PerformancePlanCombo,    IdleManager.Status.Performance),
				//})
				//{
				//	box.ItemsSource = names;
				//	box.SelectedItem = plans.GetSelectedPowerPlanNameForStatus(status);
				//}
			}
			loading = false;
		}

		void StartupToggle_Toggled(object sender, RoutedEventArgs e)
		{
			if (loading || App.WinDozer.IdleManager is not { } idle) return;
			App.WinDozer.PrefsManager.SetAutoStart(StartupToggle.IsOn);
		}

		void ScreensaverToggle_Toggled(object sender, RoutedEventArgs e)
		{
			if (loading || App.WinDozer.IdleManager is not { } idle) return;
			idle.IdleOnScreensaver = ScreensaverToggle.IsOn;
		}
		
		void ActivityToggle_Toggled(object sender, RoutedEventArgs e)
		{
			if (loading || App.WinDozer.IdleManager is not { } idle) return;
			idle.IdleOnTimeout = ActivityToggle.IsOn;
		}

		void InactivityBox_ValueChanged(NumberBox sender, NumberBoxValueChangedEventArgs args)
		{
			if (loading || App.WinDozer.IdleManager is not { } idle) return;
			if (double.IsNaN(sender.Value)) return;     // cleared or invalid input

			idle.InputTimeout = (int)sender.Value;
		}

		void PollingBox_ValueChanged(NumberBox sender, NumberBoxValueChangedEventArgs args)
		{
			if (loading || App.WinDozer.IdleManager is not { } idle) return;
			if (double.IsNaN(sender.Value)) return;

			idle.PollingInterval = (int)sender.Value;
		}


		void IdleSettings_Click(object sender, RoutedEventArgs e)
		{
			Frame.Navigate(typeof(StatusSettingsPage), IdleManager.Status.Idle, new SlideNavigationTransitionInfo { Effect = SlideNavigationTransitionEffect.FromRight });
		}

		void BalancedSettings_Click(object sender, RoutedEventArgs e)
		{
			Frame.Navigate(typeof(StatusSettingsPage), IdleManager.Status.Balanced, new SlideNavigationTransitionInfo { Effect = SlideNavigationTransitionEffect.FromRight });
		}

		void PerformanceSettings_Click(object sender, RoutedEventArgs e)
		{
			Frame.Navigate(typeof(StatusSettingsPage), IdleManager.Status.Performance, new SlideNavigationTransitionInfo { Effect = SlideNavigationTransitionEffect.FromRight });
		}

	}
}
