using System;
using System.Collections.Generic;
using System.Text;

namespace WinDozer
{
	class Debug
	{

		const int MaxMessages = 500;          // messages kept in memory
		const long MaxLogSizeBytes = 10 * 1024 * 1024;  // 10 MB

		static readonly string logDirPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), WinDozer.CompanyName);
		static readonly string logFilePath = Path.Combine(logDirPath, WinDozer.ProgramName, WinDozer.ProgramName + ".log");
		static string Now => DateTime.Now.ToString("yyyy/MM/dd - HH:mm");

		public delegate void StringEventHandler(string message);
		public static StringEventHandler? LogEvent;

		// capped in-memory buffer
		static readonly Queue<string> messages = new Queue<string>();
		public static string[] GetMessages() { lock (_lockObject) return messages.ToArray(); }

		private readonly static object _lockObject = new object();

		public static void Initialize()
		{
			// start dirs
			if (!Directory.Exists(logDirPath)) Directory.CreateDirectory(logDirPath);

			RotateIfNeeded();
		}

		public static void Log(string message) => WriteLog("INFO", message);
		public static void LogError(string message) => WriteLog("ERROR", message);
		public static void LogWarning(string message) => WriteLog("WARNING", message);

		static void WriteLog(string level, string message)
		{
			lock (_lockObject)
			{
				message = Now + $" [{level}] - " + message;
				Console.WriteLine(message);
				System.Diagnostics.Debug.WriteLine(message);

				messages.Enqueue(message);
				while (messages.Count > MaxMessages)
					messages.Dequeue();

				LogEvent?.Invoke(message);

				RotateIfNeeded();
				Write(message);
			}
		}

		// starts a new log when the current one is over the size limit, keeping the old one as .old
		static void RotateIfNeeded()
		{
			try
			{
				var fi = new FileInfo(logFilePath);
				if (fi.Exists && fi.Length > MaxLogSizeBytes)
				{
					string backupPath = logFilePath + ".old";
					if (File.Exists(backupPath))
						File.Delete(backupPath);
					File.Move(logFilePath, backupPath);
				}
			}
			catch (Exception ex)
			{
				Console.WriteLine("failed to rotate log\n" + ex);
			}
		}

		static void Write(string line)
		{
			try
			{
				File.AppendAllText(logFilePath, line + Environment.NewLine);
			}
			catch (Exception ex)
			{
				Console.WriteLine("failed to save log\n" + ex);
			}
		}

		public static void ShowLog()
		{
			if (File.Exists(logFilePath))
			{
				System.Diagnostics.Process.Start(logFilePath);
			}
		}

	}
}
