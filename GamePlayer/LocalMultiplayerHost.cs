using System.Diagnostics;
using GameServer;
using Microsoft.AspNetCore.Builder;

public static class LocalMultiplayerHost
{
	public static async Task RunAsync(string url, bool useListView = false)
	{
		var exeName = OperatingSystem.IsWindows() ? "GamePlayer.exe" : "GamePlayer";
		var exePath = Path.Combine(AppContext.BaseDirectory, exeName);

		if (!File.Exists(exePath))
		{
			Console.WriteLine($"Could not find '{exeName}' next to this executable ({AppContext.BaseDirectory}).");
			Console.WriteLine("Build the GamePlayer project first (e.g. `dotnet build GamePlayer`) and try again.");
			return;
		}

		WebApplication app;
		try
		{
			app = ServerHost.BuildAndListen(url);
			await app.StartAsync();
		}
		catch (Exception ex)
		{
			Console.WriteLine($"Failed to start the GameServer on {url}: {ex.Message}");
			Console.WriteLine("Is the port already in use by another GameServer instance?");
			return;
		}

		Console.WriteLine($"GameServer is running at {url}.");

		Process? player1 = null;
		Process? player2 = null;

		// Process.Start(UseShellExecute: true) spawns the player windows as independent OS processes
		// with no lifetime tie to this one - closing this host window (X button, Ctrl+C, or a crash)
		// otherwise leaves them running and locking GamePlayer.exe for the next build. Hook every exit
		// path so they always get cleaned up alongside the host.
		void KillChildren(object? sender, EventArgs e)
		{
			TryKill(player1);
			TryKill(player2);
		}
		AppDomain.CurrentDomain.ProcessExit += KillChildren;
		Console.CancelKeyPress += (_, _) => KillChildren(null, EventArgs.Empty);

		try
		{
			player1 = StartRemoteClient(exePath, url, "Player 1", useListView);
			player2 = StartRemoteClient(exePath, url, "Player 2", useListView);

			Console.WriteLine();
			Console.WriteLine("Two player windows have been opened.");
			Console.WriteLine("In EACH window, press Q to quick-match - the server will automatically pair them together.");
			Console.WriteLine("Waiting for both games to finish...");
			Console.WriteLine();

			await Task.WhenAll(player1.WaitForExitAsync(), player2.WaitForExitAsync());

			Console.WriteLine("Both player windows have closed.");
		}
		catch (Exception ex)
		{
			Console.WriteLine($"Failed to launch a player window: {ex.Message}");
			TryKill(player1);
			TryKill(player2);
		}
		finally
		{
			AppDomain.CurrentDomain.ProcessExit -= KillChildren;

			player1?.Dispose();
			player2?.Dispose();

			Console.WriteLine("Shutting down GameServer...");
			await app.StopAsync();
			await app.DisposeAsync();
		}
	}

	private static Process StartRemoteClient(string exePath, string url, string label, bool useListView)
	{
		var startInfo = new ProcessStartInfo
		{
			FileName = exePath,
			UseShellExecute = true,
			WorkingDirectory = AppContext.BaseDirectory,
		};
		startInfo.ArgumentList.Add("--remote");
		startInfo.ArgumentList.Add(url);
		if (useListView)
		{
			startInfo.ArgumentList.Add("--list");
		}

		var process = Process.Start(startInfo);
		if (process == null)
		{
			throw new InvalidOperationException($"{label} process did not start (Process.Start returned null).");
		}

		return process;
	}

	private static void TryKill(Process? process)
	{
		if (process == null || process.HasExited)
		{
			return;
		}

		try
		{
			process.Kill();
		}
		catch
		{
		}
	}
}
