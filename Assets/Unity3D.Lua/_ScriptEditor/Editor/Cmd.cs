using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Threading;
using System.Diagnostics;
using System.Text;

public class Cmd {

	public static void Execute(string cmdLine, System.Action complete)
	{
		var thread = new Thread(delegate ()
		{
			Command(cmdLine);
			if (complete != null)
				complete();
		});
		thread.Start();
		
	}

	static void Command(string cmdLine)
	{
		var sb = new StringBuilder();
		sb.AppendFormat("executing {0}", cmdLine);
		sb.AppendLine();

		var processInfo = new ProcessStartInfo("cmd.exe", "/c " + cmdLine);
		processInfo.CreateNoWindow = true;
		processInfo.UseShellExecute = false;
		processInfo.RedirectStandardOutput = true;
		processInfo.RedirectStandardError = true;
		var process = Process.Start(processInfo);
		process.OutputDataReceived += (sender, e) => {
			sb.AppendLine(e.Data);
		};
		process.ErrorDataReceived += (sender, e) => {
			sb.AppendLine(e.Data);
		};
		process.WaitForExit();
		process.Close();
		sb.AppendLine("end.");
		UnityEngine.Debug.Log(sb.ToString());
	}
	
}
