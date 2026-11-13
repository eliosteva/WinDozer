using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace WinDozer.Pages
{
	public sealed partial class StatusPage : Page
	{

		readonly DispatcherTimer timer = new() { Interval = TimeSpan.FromSeconds(1) };


		public StatusPage()
		{
			InitializeComponent();

			timer.Tick += (_, _) => Refresh();
			Loaded += (_, _) => { Refresh(); timer.Start(); };
			Unloaded += (_, _) => timer.Stop();

			Refresh();
		}

		void Refresh()
		{
			var p = App.WinDozer;
			if (p.IdleManager is not { } idle)
			{
				StatusText.Text = "starting…"; 
				return;
			}

			StatusText.Text = idle.CurrentStatus + (idle.IsForced ? " (forced)" : "");
			switch(idle.CurrentStatus)
			{
				case IdleManager.Status.Idle: StatusIcon.Glyph = WinDozer.gliphIdle; break;
				case IdleManager.Status.Balanced: StatusIcon.Glyph = WinDozer.gliphBalanced; break;
				case IdleManager.Status.Performance: StatusIcon.Glyph = WinDozer.gliphPerformance; break;
			}

			ReasonText.Text = idle.CurrentStatusReason;
			try
			{
				PlanText.Text = p.PowerPlanManager.GetActivePowerPlanName();
				ModeText.Text = p.PowerModeManager.GetCurrentPowerModeName();
				IdleTime.Text = p.IdleManager.GetInputIdleTime().ToString();
			}
			catch (Exception ex) { PlanText.Text = ex.Message; }

			UpdateWarnings();
			if (!ModeCombo.IsDropDownOpen) SyncModeCombo();
		}

		#region warnings

		void ResetPowerMode_Click(object sender, RoutedEventArgs e)
		{
			App.WinDozer.PowerModeManager?.ApplyBalancedPowerMode();
			UpdateWarnings();
		}

		void InstallPowerPlans_Click(object sender, RoutedEventArgs e)
		{
			if (App.WinDozer.PowerPlanManager is not { } plans) return;
			plans.InstallMissingManagedPlans();
			UpdateWarnings();
		}

		void UpdateWarnings()
		{
			if (App.WinDozer.PowerModeManager is not { } modes) return;
			ModeWarning.IsOpen = !modes.IsCurrentModeBalanced;

			if (App.WinDozer.PowerPlanManager is not { } plans) return;
			MissingPlansWarning.IsOpen = !plans.HasManagedPlansInstalled();
		}

		#endregion

		#region mode combobox

		bool syncing; // prevents SelectionChanged from re-applying while we set the value from code

		void ModeCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			if (syncing || App.WinDozer.IdleManager is not { } idle) return;

			switch (ModeCombo.SelectedIndex)
			{
				case 0: idle.ResetForced(); break;                          // back to automatic
				case 1: idle.ForceStatus(IdleManager.Status.Idle); break;
				case 2: idle.ForceStatus(IdleManager.Status.Balanced); break;
				case 3: idle.ForceStatus(IdleManager.Status.Performance); break;
			}
		}

		// call from Loaded (and from Refresh() if you want it to follow changes made elsewhere)
		void SyncModeCombo()
		{
			if (App.WinDozer.IdleManager is not { } idle) return;
			if (ModeCombo.IsDropDownOpen) return;

			syncing = true;
			ModeCombo.SelectedIndex = !idle.IsForced ? 0 : idle.CurrentStatus switch
			{
				IdleManager.Status.Idle => 1,
				IdleManager.Status.Balanced => 2,
				_ => 3,
			};
			syncing = false;
		}

		#endregion
	}
}
