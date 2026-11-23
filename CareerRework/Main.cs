//FILE:		Main.cs

using System;
using System.IO;
using System.Reflection;
using HarmonyLib;
using UnityEngine;
using UnityModManagerNet;

namespace CareerRework
{
	public static class Main
	{
		public static UnityModManager.ModEntry Mod = null!;

		/// <summary>Eigene Einstellungen (Settings.xml) – nur im Hauptmenü bearbeitbar.</summary>
		public static CareerReworkSettings LocalSettings = null!;

		/// <summary>Wirksame Einstellungen: Hauptmenü = LocalSettings, Spielstand = Savegame, MP-Client = Host.</summary>
		public static CareerReworkSettings Settings = null!;

		/// <summary>Wirksame Werte inkl. Preset (bei Preset feste Werte der Mod).</summary>
		public static CareerReworkSettings E => Settings.Effective();

		private static Harmony? harmony;

		// =========================
		// LOG (nur Wichtiges; Details nur mit Debug-Log)
		// =========================
		public static bool DebugOn => LocalSettings != null && LocalSettings.debugLog;

		public static void Log(string msg)
		{
			if (DebugOn)
				Debug.Log("[CareerRework] " + msg);
		}

		public static void Info(string msg) => Debug.Log("[CareerRework] " + msg);

		public static void Warn(string msg) => Debug.LogWarning("[CareerRework] " + msg);

		// =========================
		// LOAD
		// =========================
		public static bool Load(UnityModManager.ModEntry modEntry)
		{
			Mod = modEntry;
			LocalSettings = UnityModManager.ModSettings.Load<CareerReworkSettings>(modEntry) ?? new CareerReworkSettings();
			MigrateLocalSettings(modEntry);
			LocalSettings.Sanitize();
			Settings = LocalSettings;

			Loc.LoadExternal(modEntry.Path);

			modEntry.OnGUI = SettingsGui.Draw;
			modEntry.OnSaveGUI = OnSaveGUI;
			modEntry.OnLateUpdate = InitMultiplayer;		// erst wenn alle Mods geladen sind

			// genau EIN PatchAll pro Spielsitzung
			if (harmony == null)
			{
				harmony = new Harmony(modEntry.Info.Id);
				harmony.PatchAll(Assembly.GetExecutingAssembly());
			}

			return true;
		}

		private static void OnSaveGUI(UnityModManager.ModEntry modEntry)
		{
			LocalSettings.Sanitize();
			LocalSettings.Save(modEntry);		// nie Savegame- oder Host-Werte in die eigene Datei schreiben
		}

		private static void InitMultiplayer(UnityModManager.ModEntry modEntry, float dt)
		{
			modEntry.OnLateUpdate = null;
			Mp.Initialize(modEntry);
		}

		// =========================
		// ZUSTAND
		// =========================
		/// <summary>Karriere-Welt geladen (zwischen WorldStreamingInit und Hauptmenü).</summary>
		public static bool WorldActive { get; internal set; }

		/// <summary>Einstellungen sind gesperrt: Werte kommen aus dem Savegame bzw. vom Host.</summary>
		public static bool Locked => Mp.IsClient || (WorldActive && SaveState.Loaded);

		// ---------- Versteckte Progression (Komfort, nicht im Savegame) ----------
		private static bool sessionHiddenProgression;
		private static bool hostHiddenProgression;

		/// <summary>Im CareerManager nur besessene/kaufbare Lizenzen zeigen. Client: Wert des Hosts.</summary>
		public static bool HiddenProgression => Mp.IsClient ? hostHiddenProgression : sessionHiddenProgression;

		/// <summary>Beim Laden der Welt festlegen – danach bis zum Hauptmenü unveränderlich.</summary>
		internal static void FixSessionOptions() => sessionHiddenProgression = LocalSettings.hiddenProgression;

		// ---------- Start-Lizenzen (Gleichzeitige Aufträge) ----------
		/// <summary>
		/// Nur mit der Multiplayer-Mod und OHNE DVMultiplayerIP: dann teilen sich alle Spieler die Lizenzen der Karriere.
		/// Mit DVMultiplayerIP vergibt IP die Start-Lizenzen pro Spieler, im Einzelspieler gibt es die Option nicht.
		/// </summary>
		public static bool StartLicenseOptionsAvailable => ModActive("Multiplayer") && !IpActive();

		private static bool IpActive()
		{
			foreach (var m in UnityModManager.modEntries)
			{
				if (m == null || !m.Active)
					continue;
				string id = m.Info?.Id ?? "";
				string name = m.Info?.DisplayName ?? "";
				if (id.Equals("DVMultiplayerIP", StringComparison.OrdinalIgnoreCase)
					|| id.Equals("MultiplayerIP", StringComparison.OrdinalIgnoreCase)
					|| name.IndexOf("Individual Progression", StringComparison.OrdinalIgnoreCase) >= 0)
					return true;
			}
			return false;
		}

		private static bool ModActive(string id)
		{
			var m = UnityModManager.FindMod(id);
			return m != null && m.Active;
		}

		// Einmalige Anpassung alter Standardwerte in der lokalen Settings.xml
		private static void MigrateLocalSettings(UnityModManager.ModEntry modEntry)
		{
			if (LocalSettings.localVersion >= 1)
				return;
			// v1: Lokführer ist Startlizenz → Startgeld-Standard 200.000 $ → 150.000 $ (nur wenn unverändert)
			if (LocalSettings.startingMoney == 200000)
				LocalSettings.startingMoney = 150000;
			LocalSettings.localVersion = 1;
			try { LocalSettings.Save(modEntry); } catch { }
		}

		/// <summary>Wird von DVMultiplayerIP (Reflection) für das Startgeld neuer Spieler benutzt.</summary>
		public static float GetStartingMoney() => E.startingMoney;

		/// <summary>Werte aus dem Savegame übernehmen (eigene Settings.xml bleibt unangetastet).</summary>
		internal static void UseSaveSettings(Newtonsoft.Json.Linq.JObject saved)
		{
			var s = LocalSettings.Clone();
			s.ReadJson(saved);
			Settings = s;
		}

		/// <summary>MP-Client: spielrelevante Werte vom Host übernehmen.</summary>
		internal static void ApplyHostSettings(BinaryReader r)
		{
			var s = LocalSettings.Clone();
			s.ReadGameplay(r);
			hostHiddenProgression = r.ReadBoolean();
			Settings = s;
			ApplyAll();
		}

		/// <summary>Hauptmenü / Verbindung getrennt: wieder die eigenen Einstellungen.</summary>
		internal static void RestoreLocalSettings()
		{
			if (ReferenceEquals(Settings, LocalSettings))
				return;
			Settings = LocalSettings;
			ApplyAll();
		}

		/// <summary>Preise, Voraussetzungen und Ladenpreise nach den wirksamen Einstellungen setzen.</summary>
		internal static void ApplyAll()
		{
			try
			{
				LicenseRules.Apply(E);
				ShopPrices.ReapplyAll();

				// Copay/Zeitbonus der besessenen Lizenzen neu aufsummieren (nur in der Welt)
				if (WorldActive)
					LicenseRules.RecalculateTotals(LicenseManager.Instance);
			}
			catch (Exception e)
			{
				Warn("Applying settings failed: " + e);
			}
		}
	}
}