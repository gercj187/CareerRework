//FILE:		SaveState.cs

using System;
using Newtonsoft.Json.Linq;

namespace CareerRework
{
	// =========================
	// EINSTELLUNGEN IM SAVEGAME
	// =========================
	// Key "CareerRework_Settings" (JObject-Array mit einem Eintrag – gleiches Format wie die bisherige MP-Version).
	//
	// • Neue Karriere: die Einstellungen aus dem Menü werden ins Savegame geschrieben.
	// • Laden: die Werte aus dem Savegame gelten, egal was im Menü steht.
	// • Älteres Savegame ohne Eintrag: die aktuellen Menü-Werte werden einmalig übernommen und ab dann gespeichert.
	// • "starterLocosSpawned": die Starter-Loks kommen genau einmal (direkt nach dem Start der Karriere).
	// • "knownLicenses": Zusatzlizenzen, die der Spielstand kennt. Neue (z. B. neu installierte Mod) übernehmen beim
	//   ersten Laden die Menü-Einstellungen und sind danach ebenfalls festgeschrieben.
	internal static class SaveState
	{
		public const string SaveKey = "CareerRework_Settings";
		private const string StarterKey = "starterLocosSpawned";

		/// <summary>Die Werte des geladenen Spielstands sind aktiv.</summary>
		public static bool Loaded { get; private set; }

		/// <summary>false = neue Karriere, Starter-Loks müssen noch gespawnt werden.</summary>
		public static bool StarterLocosSpawned { get; set; } = true;

		public static void Reset()
		{
			Loaded = false;
			StarterLocosSpawned = true;
		}

		/// <summary>Neue Karriere: Einstellungen + "Starter-Loks noch offen" schreiben.</summary>
		public static void WriteNewCareer(SaveGameData data)
		{
			StarterLocosSpawned = false;
			// alle jetzt erkannten Zusatzlizenzen sind ab sofort im Savegame festgeschrieben
			Main.Settings.knownLicenses = ExtraLicenses.DetectedKeys();
			Write(data);
		}

		/// <summary>Bei jedem Speichern (Host/Einzelspieler).</summary>
		public static void Write(SaveGameData data)
		{
			if (data == null)
				return;

			var o = Main.Settings.ToJson();
			o[StarterKey] = StarterLocosSpawned;
			data.SetJObjectArray(SaveKey, new[] { o });
		}

		/// <summary>Beim Laden des Spielstands (LicenseManager.LoadData).</summary>
		public static void Read(SaveGameData data)
		{
			// MP-Client mit Host-Werten: nichts überschreiben
			if (Mp.HasHostState)
			{
				Loaded = true;
				return;
			}

			JObject? saved = null;
			try
			{
				var arr = data.GetJObjectArray(SaveKey);
				if (arr != null && arr.Length > 0)
					saved = arr[0];
			}
			catch (Exception e)
			{
				Main.Warn("Could not read saved settings: " + e.Message);
			}

			if (saved != null)
			{
				Main.UseSaveSettings(saved);
				var t = saved[StarterKey];
				StarterLocosSpawned = t == null || t.Type != JTokenType.Boolean || t.Value<bool>();	// fehlt = alter Spielstand
				Main.Info($"Settings loaded from savegame ({Main.Settings.startupMode}).");

				// Neue Lizenzen (seit dem letzten Laden installierte Mods): Menü-Einstellungen einmalig übernehmen
				var adopted = ExtraLicenses.AdoptNew(Main.Settings, Main.LocalSettings);
				if (adopted.Count > 0)
				{
					Main.Info($"New licenses added to the savegame with the current menu settings: {string.Join(", ", adopted)}");
					if (!Mp.IsClient)
						Write(data);
				}
			}
			else
			{
				// Älterer Spielstand: aktuelle Werte übernehmen (werden beim nächsten Speichern geschrieben)
				Main.Settings = Main.LocalSettings.Clone();
				Main.Settings.knownLicenses = ExtraLicenses.DetectedKeys();
				StarterLocosSpawned = true;
				if (!Mp.IsClient)
					Write(data);
				Main.Info($"Savegame had no CareerRework settings → current settings adopted ({Main.Settings.startupMode}).");
			}

			Loaded = true;
		}
	}
}
