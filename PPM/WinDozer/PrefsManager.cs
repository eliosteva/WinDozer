using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Text;

namespace WinDozer
{
	internal class PrefsManager
	{

		const string dataFileName = WinDozer.ProgramName + ".ini";
		string dataFilePath => Path.Combine(WinDozer.AppdataDirPath, dataFileName);

		Dictionary<string, string> prefs = new Dictionary<string, string>();


		internal PrefsManager()
		{
			Debug.Log("initializing prefs manager");

			// load prefs
			LoadPrefs();
		}

		void LoadPrefs()
		{
			prefs.Clear();

			if (File.Exists(dataFilePath))
			{
				try
				{
					var lines = File.ReadAllLines(dataFilePath);
					foreach (var line in lines)
					{
						string[] split = line.Split('=', 2);
						if (split.Length == 2)
						{
							string key = split[0];
							string value = split[1];
							prefs[key] = value;
						}
						else Debug.LogError("failed to read prefs line: " + line);
					}
				}
				catch (Exception ex)
				{
					Debug.LogError("failed to read prefs: " + ex.ToString());
					prefs.Clear();
				}
			}
			else Debug.LogWarning("pref file not found: " + dataFilePath);
		}

		public void SetPref(string key, string value)
		{
			if (!prefs.ContainsKey(key)) prefs.Add(key, value);
			else prefs[key] = value;

			SavePrefs();
		}

		public string GetPref(string key)
		{
			return prefs.ContainsKey(key) ? prefs[key] : "";
		}

		public T GetPref<T>(string key, T def)
		{
			return prefs.ContainsKey(key) ? (T)Convert.ChangeType(prefs[key], typeof(T)) : def;
		}

		void SavePrefs()
		{
			try
			{
				string s = "";
				foreach (var v in prefs)
				{
					s += v.Key + "=" + v.Value + "\n";
				}
				s = s.Trim();

				if (!Directory.Exists(WinDozer.AppdataDirPath)) Directory.CreateDirectory(WinDozer.AppdataDirPath);
				File.WriteAllText(dataFilePath, s);
			}
			catch (Exception ex)
			{
				Debug.LogError("failed to save prefs: " + ex.ToString());
			}
		}


		internal bool IsAutoStartEnabled()
		{
			var rk = Registry.CurrentUser.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", true);
			if (rk != null)
			{
				var value = rk.GetValue(WinDozer.ProgramName);
				if (value != null && value is string s && !string.IsNullOrEmpty(s))
				{
					if (s.Equals(WinDozer.AppdataExePath)) return true;
				}
			}
			return false;
		}

		internal void SetAutoStart(bool value)
		{
			Debug.Log("setting autostart registry key to " + value);
			using var rk = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Run", true);
			if (rk == null) return;

			if (value) rk.SetValue(WinDozer.ProgramName, WinDozer.AppdataExePath);
			else rk.DeleteValue(WinDozer.ProgramName, false);
		}
	}
}
