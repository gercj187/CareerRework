//FILE:		Mp.cs

using System;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityModManagerNet;

namespace CareerRework
{
	// =========================
	// MULTIPLAYER (optional, "Shim with Integration" wie LongerLoadingDelay)
	// =========================
	// Die Hauptmod referenziert MultiplayerAPI NICHT. Nur wenn die Multiplayer-Mod aktiv ist, wird
	// CareerRework.Multiplayer.dll (liegt im Mod-Ordner) geladen; sie kapselt alles, was die API braucht.
	//
	// Modell: host-autoritativ
	//   Host / Einzelspieler → Einstellungen aus dem eigenen Savegame, spawnt die Starter-Loks
	//   Client               → übernimmt Modus, Preise und Multiplikatoren vom Host (eigene Datei bleibt)
	// Ohne Multiplayer-Mod verhält sich alles exakt wie im Einzelspieler.
	public static class Mp
	{
		private const string MultiplayerModId = "Multiplayer";
		private const string ApiAssemblyName = "MultiplayerAPI";
		private const string IntegrationDll = "CareerRework.Multiplayer.dll";
		private const string BootstrapTypeName = "CareerRework.Multiplayer.Bootstrap";

		private const byte StateVersion = 5;

		private static Func<bool>? isClient, isHost;
		private static Action<byte[]>? broadcast;

		public static bool Available { get; private set; }

		/// <summary>Client hat die Einstellungen des Hosts erhalten.</summary>
		public static bool HasHostState { get; private set; }

		/// <summary>Mit einem fremden Host verbunden.</summary>
		public static bool IsClient
		{
			get
			{
				try { return isClient != null && isClient(); }
				catch { return false; }
			}
		}

		/// <summary>Eigene Multiplayer-Sitzung läuft (Host).</summary>
		public static bool IsHost
		{
			get
			{
				try { return isHost != null && isHost(); }
				catch { return false; }
			}
		}

		/// <summary>Darf die Welt verändern (Loks spawnen): Einzelspieler oder Host.</summary>
		public static bool CanModifyWorld => !IsClient;

		// =========================
		// LADEN DER INTEGRATION
		// =========================
		public static void Initialize(UnityModManager.ModEntry modEntry)
		{
			try
			{
				var multiplayer = UnityModManager.FindMod(MultiplayerModId);
				bool apiLoaded = AppDomain.CurrentDomain.GetAssemblies().Any(a => a.GetName().Name == ApiAssemblyName);

				if (multiplayer == null || !multiplayer.Enabled || !apiLoaded)
				{
					Main.Log("Multiplayer not active → single player mode");
					return;
				}

				string path = Path.Combine(modEntry.Path, IntegrationDll);
				if (!File.Exists(path))
				{
					Main.Warn(IntegrationDll + " not found – multiplayer sync disabled.");
					return;
				}

				var asm = Assembly.LoadFile(path);
				var bootstrap = asm.GetType(BootstrapTypeName);
				if (bootstrap == null)
				{
					Main.Warn(BootstrapTypeName + " not found – multiplayer sync disabled.");
					return;
				}

				var init = bootstrap.GetMethod("Initialize", BindingFlags.Public | BindingFlags.Static);
				bool ok = init != null && (bool)init.Invoke(null, new object[]
				{
					modEntry.Info.Id,
					(Action<byte[]>)OnStateReceived,
					(Func<byte[]>)BuildState,
					(Action)OnClientStopped,
					(Action<string>)(msg => Main.Log("MP: " + msg))
				});

				if (!ok)
				{
					Main.Log("Multiplayer API not ready → single player mode");
					return;
				}

				isClient = (Func<bool>)Delegate.CreateDelegate(typeof(Func<bool>), bootstrap.GetMethod("IsClient", BindingFlags.Public | BindingFlags.Static)!);
				isHost = (Func<bool>)Delegate.CreateDelegate(typeof(Func<bool>), bootstrap.GetMethod("IsHost", BindingFlags.Public | BindingFlags.Static)!);
				broadcast = (Action<byte[]>)Delegate.CreateDelegate(typeof(Action<byte[]>), bootstrap.GetMethod("Broadcast", BindingFlags.Public | BindingFlags.Static)!);

				Available = true;
				Main.Info("Multiplayer sync active.");
			}
			catch (Exception e)
			{
				Main.Warn("Multiplayer integration failed: " + e.Message);
				isClient = isHost = null;
				broadcast = null;
				Available = false;
			}
		}

		// =========================
		// HOST: Einstellungen senden
		// =========================
		/// <summary>Host: Einstellungen sofort an alle Clients (z. B. nachdem das Savegame geladen wurde).</summary>
		public static void BroadcastSettings()
		{
			if (!Available || broadcast == null || !IsHost)
				return;

			try
			{
				broadcast(BuildState());
			}
			catch (Exception e)
			{
				Main.Warn("MP broadcast failed: " + e.Message);
			}
		}

		private static byte[] BuildState()
		{
			using var ms = new MemoryStream();
			using var w = new BinaryWriter(ms);

			w.Write(StateVersion);
			Main.Settings.WriteGameplay(w);
			w.Write(Main.HiddenProgression);		// Komfort-Option des Hosts (nicht im Savegame)

			w.Flush();
			return ms.ToArray();
		}

		// =========================
		// CLIENT: Einstellungen übernehmen
		// =========================
		private static void OnStateReceived(byte[] data)
		{
			if (data == null || data.Length == 0 || !IsClient)
				return;

			try
			{
				using var ms = new MemoryStream(data);
				using var r = new BinaryReader(ms);

				byte version = r.ReadByte();
				if (version != StateVersion)
				{
					Main.Warn($"MP: unknown settings version {version} (expected {StateVersion}) – host uses another mod version");
					return;
				}

				Main.ApplyHostSettings(r);

				if (!HasHostState)
				{
					HasHostState = true;
					Main.Info("MP: settings received from host.");
				}
			}
			catch (Exception e)
			{
				Main.Warn("MP: settings could not be read: " + e.Message);
			}
		}

		private static void OnClientStopped()
		{
			if (!HasHostState)
				return;

			HasHostState = false;
			Main.RestoreLocalSettings();
			Main.Log("MP: disconnected → local settings restored");
		}
	}
}
