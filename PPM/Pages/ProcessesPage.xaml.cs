using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using System.Collections.ObjectModel;
using System.Diagnostics;
using Windows.ApplicationModel.DataTransfer;
using Windows.System;

namespace WinDozer.Pages
{
	/// <summary>
	/// An empty page that can be used on its own or navigated to within a Frame.
	/// </summary>
	public sealed partial class ProcessesPage : Page
	{

		readonly ObservableCollection<string> idleItems = new(), balancedItems = new(), performanceItems = new();
		readonly DispatcherTimer refreshTimer = new() { Interval = TimeSpan.FromSeconds(3) };
		string? dragSourceTag;


		public ProcessesPage()
		{
			InitializeComponent();

			IconIdle.Glyph = WinDozer.gliphIdle;
			IconBalanced.Glyph = WinDozer.gliphBalanced;
			IconPerformance.Glyph = WinDozer.gliphPerformance;

			

			IdleList.ItemsSource = idleItems; BalancedList.ItemsSource = balancedItems; PerformanceList.ItemsSource = performanceItems;
			refreshTimer.Tick += (_, _) => Reload();
			Loaded += (_, _) => { Reload(); refreshTimer.Start(); };
			Unloaded += (_, _) => refreshTimer.Stop();
		}

		void Reload()
		{
			if (dragSourceTag != null) return;                 // don't touch the lists mid-drag
			if (App.WinDozer.IdleManager is not { } idle) return;

			var balanced = idle.BalancedProcessNames.ToList();
			var performance = idle.PerformanceProcessNames.ToList();

			var running = new HashSet<string>();   // default comparer = ordinal, same as IdleManager
			foreach (var p in Process.GetProcesses()) { using (p) running.Add(p.ProcessName); }
			running.ExceptWith(balanced);
			running.ExceptWith(performance);

			var filter = IdleFilterBox.Text.Trim();
			if (filter.Length > 0)
				running.RemoveWhere(n => !n.Contains(filter, StringComparison.OrdinalIgnoreCase));

			Sync(idleItems, running);
			Sync(balancedItems, balanced);
			Sync(performanceItems, performance);
		}

		// updates the collection in place (sorted), so scroll position and selection survive the refresh
		static void Sync(ObservableCollection<string> col, IEnumerable<string> source)
		{
			var sorted = source
				.OrderBy(s => s, StringComparer.OrdinalIgnoreCase)
				.ThenBy(s => s, StringComparer.Ordinal)      // keeps "Chrome" / "chrome" in a stable order
				.ToList();
			var set = new HashSet<string>(sorted);

			for (int i = col.Count - 1; i >= 0; i--)
				if (!set.Contains(col[i])) col.RemoveAt(i);

			for (int i = 0; i < sorted.Count; i++)
				if (i >= col.Count || col[i] != sorted[i]) col.Insert(i, sorted[i]);
		}

		void List_DragItemsStarting(object sender, DragItemsStartingEventArgs e)
		{
			dragSourceTag = (string)((ListView)sender).Tag;
			e.Data.SetText(string.Join("\n", e.Items.Cast<string>()));   // works with multi-selection
			e.Data.RequestedOperation = DataPackageOperation.Move;
		}

		void List_DragItemsCompleted(ListViewBase sender, DragItemsCompletedEventArgs args) => dragSourceTag = null;

		void List_DragOver(object sender, DragEventArgs e)
		{
			var target = (string)((ListView)sender).Tag;
			e.AcceptedOperation = (target != dragSourceTag && e.DataView.Contains(StandardDataFormats.Text))
				? DataPackageOperation.Move : DataPackageOperation.None;
			e.DragUIOverride.Caption = "Move here";
		}

		async void List_Drop(object sender, DragEventArgs e)
		{
			var target = (string)((ListView)sender).Tag;
			if (target == dragSourceTag || App.WinDozer.IdleManager is not { } idle) return;

			var deferral = e.GetDeferral();
			try
			{
				var text = await e.DataView.GetTextAsync();
				var names = text.Split('\n', StringSplitOptions.RemoveEmptyEntries);

				switch (target)
				{
					case "idle": idle.AddIdleProcess(names); break;
					case "balanced": idle.AddBalancedProcess(names); break;
					case "performance": idle.AddPerformanceProcess(names); break;
				}
			}
			finally { deferral.Complete(); }

			dragSourceTag = null;   // DragItemsCompleted can arrive after Drop, and Reload() skips while this is set
			Reload();
		}

		void IdleFilter_TextChanged(object sender, TextChangedEventArgs e) => Reload();

		void AddBox_KeyDown(object sender, KeyRoutedEventArgs e)
		{
			if (e.Key == VirtualKey.Enter) AddByName((string)((TextBox)sender).Tag);
		}

		void AddProcess_Click(object sender, RoutedEventArgs e) => AddByName((string)((Button)sender).Tag);

		void AddByName(string tag)
		{
			if (App.WinDozer.IdleManager is not { } idle) return;
			var box = tag == "balanced" ? BalancedAddBox : PerformanceAddBox;

			var names = box.Text
				.Split(new[] { ',', ';', '\n' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
				.Select(n => n.EndsWith(".exe", StringComparison.OrdinalIgnoreCase) ? n[..^4] : n)
				.ToList();
			if (names.Count == 0) return;

			if (tag == "balanced") idle.AddBalancedProcess(names);
			else idle.AddPerformanceProcess(names);

			box.Text = "";
			Reload();
		}

	}
}
