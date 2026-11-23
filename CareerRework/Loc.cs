//FILE:		Loc.cs

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using DV.Localization;
using Newtonsoft.Json.Linq;

namespace CareerRework
{
	// =========================
	// MEHRSPRACHIGKEIT (wie LongerLoadingDelay)
	// =========================
	// Sprache = Spielsprache (I2 über LocalizationAPI.CC). Alle 26 Spielsprachen sind eingebaut.
	// Eigene Texte: <Mod-Ordner>/lang/<code>.json  →  { "key": "Text", ... }  (UTF-8 ohne BOM)
	// Fehlende Keys fallen auf Englisch zurück.
	public static class Loc
	{
		private static readonly Dictionary<string, Dictionary<string, string>> langs = new(StringComparer.OrdinalIgnoreCase)
		{
			["en"] = new()
			{
				["tip.price.starter"] = "\n\n<b>Base license:</b> every career and every player starts with this license. The price only matters if it is ever bought again; copay and time bonus apply from the start.",
				["gui.comfort"] = "Comfort",
				["gui.hidden"] = "Hidden progression",
				["tip.hidden"] = "<b>Hidden progression</b>\nThe license list in the Career Manager only shows licenses you own or can buy right now. Licenses still blocked by a missing requirement are hidden until you meet it.\n\n• Not stored in the savegame – applies from loading a savegame until you return to the main menu.\n• Multiplayer: the host's setting applies to everyone.",
				["notice.newlic.title"] = "CareerRework: new licenses detected",
				["notice.newlic.text"] = "Other mods added licenses that CareerRework has not seen before. Set their price, copay, time bonus and requirement in the CareerRework settings NOW, before loading a savegame: the first time a savegame is loaded, they are stored in it with these values and cannot be changed afterwards.",
				["notice.newlic.open"] = "Open settings",
				["notice.newlic.ok"] = "OK",
				["gui.mode"] = "Game mode",
				["gui.mode.preset"] = "Play my CareerRework preset",
				["gui.mode.custom"] = "Customize your own CareerRework",
				["tip.mode.preset"] = "<b>CareerRework preset</b>\nFixed balancing by CJ187:\n• 150,000 $ starting money\n• DE2 + DM3 as starter locomotives\n• adjusted license prices\n• keys and gadgets about 10× more expensive\n• restoration parts ×10\n\nOnly the start options below can be changed.",
				["tip.mode.custom"] = "<b>Custom</b>\nSet starting money, starter locomotives, every license price and both price multipliers yourself.\n\n<b>Important:</b> when a career starts for the first time, all values are written into the savegame. From then on the savegame values are used – changes here only affect new careers.",
				["gui.locked"] = "Settings are stored in the current savegame",
				["gui.locked.text"] = "These values belong to the loaded savegame and cannot be changed while it is loaded. Changes in the main menu only affect new careers.",
				["gui.mp.client"] = "Multiplayer: settings are controlled by the host",
				["gui.mp.client.text"] = "Mode, starting money, license prices and multipliers come from the host so everything is exactly the same for all players.",
				["gui.start"] = "Start options for a new career",
				["gui.cj1"] = "Start with Concurrent Jobs 1",
				["gui.cj2"] = "Start with Concurrent Jobs 2",
				["tip.cj1"] = "<b>Concurrent Jobs 1 from the start</b>\nThe new career starts with this license already owned – you can take more than one job at the same time right away.",
				["tip.cj2"] = "<b>Concurrent Jobs 2 from the start</b>\nAdditionally owns Concurrent Jobs 2 at the start. Only possible together with Concurrent Jobs 1.",
				["gui.money"] = "Starting money",
				["tip.money"] = "<b>Starting money</b>\nMoney on your account when a new career starts.\nCurrently {0} $ (preset: 150,000 $).",
				["gui.starter"] = "Starter locomotives",
				["gui.starter.1"] = "1st locomotive (track T1-01-P)",
				["gui.starter.2"] = "2nd locomotive (track T1-02-P)",
				["tip.starter"] = "<b>Starter locomotives</b>\nPlaced once in the Steel Mill yard right after a new career starts.\n\n• Driving them needs the matching locomotive license.\n• If the S060 is the first locomotive license you buy, you get a shovel, oil can and lighter as a gift.",
				["gui.prices"] = "License prices",
				["tip.prices"] = "<b>License prices</b>\nPrice in $ per license. 0 = vanilla price.\n\nThe requirements are fixed by the mod.",
				["tip.price"] = "<b>{0}</b>\nPrice with this value: {1} $\nVanilla price: {2} $\n\n0 = vanilla price.",
				["tip.price.free"] = "\nThis license is free in vanilla – with 0 it costs 10,000 $.",
				["gui.pj"] = "PassengerJobs licenses",
				["tip.pj"] = "<b>PassengerJobs</b>\nOnly effective when the PassengerJobs mod is installed.\n\n• Passengers 1 requires Freight Haul and Fragile.",
				["gui.gadgets"] = "Price multiplier keys & gadgets",
				["tip.gadgets"] = "<b>Keys & gadgets</b>\nMultiplies the shop price of all keys and gadgets.\n\nx{0}: 10,000 $ → {1} $\nx{0}: 35,000 $ → {2} $",
				["gui.restoration"] = "Price multiplier restoration",
				["tip.restoration"] = "<b>Restoration of demonstrator locomotives</b>\nMultiplies the prices for transport and installation of the spare parts.\n\nx{0}:\n• Transport DE6 / S282: 30,000 $ → {1} $\n• Transport DH4: 20,000 $ → {2} $\n• Transport DE2 / DM3 / S060: 15,000 $ → {3} $\n• Installation: 10,000 $ → {4} $",
				["gui.debug"] = "Debug log",
				["gui.starttime"] = "Start time",
				["tip.starttime"] = "<b>Start time</b>\nTime of day when a new career begins – always on the full hour.\nCurrently {0} (default: {1}).",
				["gui.copay"] = "Copay",
				["tip.prices.copay"] = "\nBehind it: insurance copay in $ (empty or 0 = vanilla).",
				["tip.copay"] = "\n\nInsurance copay with this value: {0} $\nVanilla copay: {1} $\nEmpty or 0 = vanilla copay.",
				["tip.mode.preset.extra"] = "• insurance copay and time bonus deadline adjusted per license",
				["gui.extra"] = "Additional licenses (other mods)",
				["tip.extra"] = "<b>Licenses from other mods</b>\nEvery general or job license that CareerRework does not manage itself is detected automatically.\n\n• Price / copay / Time bonus: empty or 0 = unchanged\n• Requirement: real purchase condition. \"Unchanged\" keeps the mod's own requirement, \"None\" removes it.\n• Choices that would create a loop (A needs B, B needs A) are not offered.\n• Settings are kept even if the mod is removed.",
				["gui.extra.none"] = "No additional licenses found.",
				["gui.price"] = "Price",
				["gui.extra.req"] = "Requires",
				["gui.req.unchanged"] = "Unchanged",
				["gui.req.none"] = "None",
				["gui.extra.missing"] = "{0} saved entries belong to licenses that are not loaded right now (kept).",
				["tip.extra.row"] = "<b>{0}</b>\nID: {1} ({2})\nOriginal price: {3} $\nOriginal copay: {4} $\nOriginal requirement: {5}",
				["gui.bonus"] = "Time bonus",
				["tip.bonus"] = "\n\nTime bonus deadline with this value: {0}\nVanilla: {1}\n+ = more time, − = less time. Empty or 0 = vanilla.",
				["tip.prices.bonus"] = "\nThen: time bonus deadline in % as shown on the license (+5 = more time, empty or 0 = vanilla).",
			},
			["de"] = new()
			{
				["tip.price.starter"] = "\n\n<b>Grundlizenz:</b> Jede Karriere und jeder Spieler startet mit dieser Lizenz. Der Preis spielt nur eine Rolle, falls sie doch gekauft wird – Copay und Zeitbonus gelten ab dem Start.",
				["gui.comfort"] = "Komfort",
				["gui.hidden"] = "Versteckte Progression",
				["tip.hidden"] = "<b>Versteckte Progression</b>\nDie Lizenzliste im CareerManager zeigt nur Lizenzen, die du besitzt oder gerade kaufen kannst. Lizenzen, die noch durch eine fehlende Voraussetzung blockiert sind, bleiben ausgeblendet, bis du sie erfüllst.\n\n• Nicht im Savegame gespeichert – gilt ab dem Laden eines Spielstands bis zur Rückkehr ins Hauptmenü.\n• Multiplayer: die Einstellung des Hosts gilt für alle.",
				["notice.newlic.title"] = "CareerRework: neue Lizenzen erkannt",
				["notice.newlic.text"] = "Andere Mods haben Lizenzen hinzugefügt, die CareerRework noch nicht kennt. Stelle Preis, Copay, Zeitbonus und Voraussetzung JETZT in den CareerRework-Einstellungen ein, bevor du einen Spielstand lädst: Beim ersten Laden werden sie mit diesen Werten im Savegame festgeschrieben und lassen sich danach nicht mehr ändern.",
				["notice.newlic.open"] = "Einstellungen öffnen",
				["notice.newlic.ok"] = "OK",
				["gui.mode"] = "Spielmodus",
				["gui.mode.preset"] = "Mein CareerRework-Preset spielen",
				["gui.mode.custom"] = "Eigenes CareerRework einstellen",
				["tip.mode.preset"] = "<b>CareerRework-Preset</b>\nFestes Balancing von CJ187:\n• 150.000 $ Startgeld\n• DE2 + DM3 als Starter-Loks\n• angepasste Lizenzpreise\n• Schlüssel und Gadgets etwa 10× teurer\n• Restaurations-Teile ×10\n\nNur die Start-Optionen darunter sind einstellbar.",
				["tip.mode.custom"] = "<b>Eigene Einstellungen</b>\nStartgeld, Starter-Loks, jeden Lizenzpreis und beide Preis-Multiplikatoren selbst festlegen.\n\n<b>Wichtig:</b> Beim ersten Start einer Karriere werden alle Werte im Savegame gespeichert. Danach gelten die Werte aus dem Savegame – Änderungen hier wirken nur auf neue Karrieren.",
				["gui.locked"] = "Einstellungen sind im aktuellen Savegame gespeichert",
				["gui.locked.text"] = "Diese Werte gehören zum geladenen Spielstand und können nicht geändert werden, solange er geladen ist. Änderungen im Hauptmenü gelten nur für neue Karrieren.",
				["gui.mp.client"] = "Multiplayer: Einstellungen werden vom Host vorgegeben",
				["gui.mp.client.text"] = "Modus, Startgeld, Lizenzpreise und Multiplikatoren kommen vom Host, damit für alle Spieler alles exakt gleich ist.",
				["gui.start"] = "Start-Optionen für eine neue Karriere",
				["gui.cj1"] = "Mit Gleichzeitige Aufträge 1 starten",
				["gui.cj2"] = "Mit Gleichzeitige Aufträge 2 starten",
				["tip.cj1"] = "<b>Gleichzeitige Aufträge 1 von Anfang an</b>\nDie neue Karriere beginnt mit dieser Lizenz – man kann sofort mehr als einen Auftrag gleichzeitig annehmen.",
				["tip.cj2"] = "<b>Gleichzeitige Aufträge 2 von Anfang an</b>\nZusätzlich Gleichzeitige Aufträge 2 zum Start. Nur zusammen mit Gleichzeitige Aufträge 1 möglich.",
				["gui.money"] = "Startgeld",
				["tip.money"] = "<b>Startgeld</b>\nGeld auf dem Konto beim Start einer neuen Karriere.\nAktuell {0} $ (Preset: 150.000 $).",
				["gui.starter"] = "Starter-Loks",
				["gui.starter.1"] = "1. Lok (Gleis T1-01-P)",
				["gui.starter.2"] = "2. Lok (Gleis T1-02-P)",
				["tip.starter"] = "<b>Starter-Loks</b>\nStehen einmalig direkt nach dem Start einer neuen Karriere im Bahnhof Steel Mill.\n\n• Zum Fahren braucht man die passende Lok-Lizenz.\n• Ist die S060 die erste gekaufte Lok-Lizenz, gibt es Schaufel, Ölkanne und Feuerzeug geschenkt.",
				["gui.prices"] = "Lizenzpreise",
				["tip.prices"] = "<b>Lizenzpreise</b>\nPreis in $ pro Lizenz. 0 = Vanilla-Preis.\n\nDie Voraussetzungen legt die Mod fest.",
				["tip.price"] = "<b>{0}</b>\nPreis mit diesem Wert: {1} $\nVanilla-Preis: {2} $\n\n0 = Vanilla-Preis.",
				["tip.price.free"] = "\nDiese Lizenz ist im Vanilla-Spiel gratis – mit 0 kostet sie 10.000 $.",
				["gui.pj"] = "PassengerJobs-Lizenzen",
				["tip.pj"] = "<b>PassengerJobs</b>\nNur wirksam, wenn die Mod PassengerJobs installiert ist.\n\n• Passagiere 1 benötigt Güterverkehr und Zerbrechliche Fracht.",
				["gui.gadgets"] = "Preis-Multiplikator Schlüssel & Gadgets",
				["tip.gadgets"] = "<b>Schlüssel & Gadgets</b>\nMultipliziert den Ladenpreis aller Schlüssel und Gadgets.\n\nx{0}: 10.000 $ → {1} $\nx{0}: 35.000 $ → {2} $",
				["gui.restoration"] = "Preis-Multiplikator Restauration",
				["tip.restoration"] = "<b>Restauration der Vorführ-Loks</b>\nMultipliziert die Preise für Transport und Einbau der Ersatzteile.\n\nx{0}:\n• Transport DE6 / S282: 30.000 $ → {1} $\n• Transport DH4: 20.000 $ → {2} $\n• Transport DE2 / DM3 / S060: 15.000 $ → {3} $\n• Einbau: 10.000 $ → {4} $",
				["gui.debug"] = "Debug-Log",
				["gui.starttime"] = "Startuhrzeit",
				["tip.starttime"] = "<b>Startuhrzeit</b>\nUhrzeit, zu der eine neue Karriere beginnt – immer zur vollen Stunde.\nAktuell {0} (Standard: {1}).",
				["gui.copay"] = "Copay",
				["tip.prices.copay"] = "\nDahinter: Versicherungs-Selbstbeteiligung (Copay) in $ (leer oder 0 = Vanilla).",
				["tip.copay"] = "\n\nCopay mit diesem Wert: {0} $\nVanilla-Copay: {1} $\nLeer oder 0 = Vanilla-Copay.",
				["tip.mode.preset.extra"] = "• Copay und Zeitbonusfrist pro Lizenz angepasst",
				["gui.extra"] = "Weitere Lizenzen (andere Mods)",
				["tip.extra"] = "<b>Lizenzen aus anderen Mods</b>\nJede allgemeine und jede Job-Lizenz, die CareerRework nicht selbst verwaltet, wird automatisch erkannt.\n\n• Preis / Copay / Zeitbonus: leer oder 0 = unverändert\n• Voraussetzung: echte Kaufbedingung. „Unverändert“ behält die Voraussetzung der Mod, „Keine“ entfernt sie.\n• Auswahlen, die einen Kreis ergeben würden (A braucht B, B braucht A), werden nicht angeboten.\n• Die Einstellungen bleiben erhalten, auch wenn die Mod entfernt wird.",
				["gui.extra.none"] = "Keine weiteren Lizenzen gefunden.",
				["gui.price"] = "Preis",
				["gui.extra.req"] = "Benötigt",
				["gui.req.unchanged"] = "Unverändert",
				["gui.req.none"] = "Keine",
				["gui.extra.missing"] = "{0} gespeicherte Einträge gehören zu Lizenzen, die gerade nicht geladen sind (bleiben erhalten).",
				["tip.extra.row"] = "<b>{0}</b>\nID: {1} ({2})\nOriginalpreis: {3} $\nOriginal-Copay: {4} $\nOriginal-Voraussetzung: {5}",
				["gui.bonus"] = "Zeitbonus",
				["tip.bonus"] = "\n\nZeitbonusfrist mit diesem Wert: {0}\nVanilla: {1}\n+ = mehr Zeit, − = weniger Zeit. Leer oder 0 = Vanilla.",
				["tip.prices.bonus"] = "\nDanach: Zeitbonusfrist in % wie auf der Lizenz (+5 = mehr Zeit, leer oder 0 = Vanilla).",
			},
		};

		static Loc()
		{
			LocLanguages.AddTo(langs);
		}

		// Spielsprache → Schlüssel unserer Tabellen
		// Varianten: zh-CN / zh-TW, pt (Portugal) / pt-BR, Norwegisch nb/nn/no → "no"
		private static string ResolveCulture(CultureInfo cc)
		{
			string name = cc.Name ?? "";
			string two = cc.TwoLetterISOLanguageName ?? "en";

			switch (two)
			{
				case "zh":
					return name.Contains("TW") || name.Contains("HK") || name.Contains("MO") || name.Contains("Hant") ? "zh-TW" : "zh-CN";
				case "pt":
					return name.Contains("BR") ? "pt-BR" : "pt";
				case "nb":
				case "nn":
				case "no":
					return "no";
			}

			if (langs.ContainsKey(name))
				return name;

			return two;
		}

		public static void LoadExternal(string modPath)
		{
			try
			{
				string dir = Path.Combine(modPath, "lang");
				if (!Directory.Exists(dir))
					return;

				foreach (string file in Directory.GetFiles(dir, "*.json"))
				{
					string code = Path.GetFileNameWithoutExtension(file);
					string json = File.ReadAllText(file, new UTF8Encoding(false)).TrimStart('\uFEFF');

					if (!langs.TryGetValue(code, out var dict))
					{
						dict = new Dictionary<string, string>();
						langs[code] = dict;
					}

					foreach (var prop in JObject.Parse(json).Properties())
						dict[prop.Name] = prop.Value.ToString();

					Main.Log("Language file loaded: " + code);
				}
			}
			catch (Exception e)
			{
				UnityEngine.Debug.LogWarning("[CareerRework] Could not load language files: " + e.Message);
			}
		}

		// Spielsprache nur einmal pro GUI-Frame ermitteln (I2 ist nicht ganz billig)
		private static int cachedFrame = -1;
		private static string cachedCode = "en";

		public static string CurrentCode
		{
			get
			{
				int frame = UnityEngine.Time.frameCount;
				if (frame == cachedFrame)
					return cachedCode;
				cachedFrame = frame;

				try
				{
					var cc = LocalizationAPI.CC;
					cachedCode = cc != null ? ResolveCulture(cc) : "en";
				}
				catch
				{
					cachedCode = "en";		// I2 noch nicht bereit
				}

				return cachedCode;
			}
		}

		public static string T(string key)
		{
			if (langs.TryGetValue(CurrentCode, out var dict) && dict.TryGetValue(key, out var text))
				return text;

			if (langs["en"].TryGetValue(key, out var en))
				return en;

			return key;
		}

		public static string T(string key, params object[] args)
		{
			try
			{
				return string.Format(T(key), args);
			}
			catch (FormatException)
			{
				return T(key);
			}
		}

		/// <summary>Zahl mit Tausendertrennzeichen der Spielsprache (Fallback: 100,000).</summary>
		public static string Num(double value)
		{
			try
			{
				return value.ToString("N0", CultureInfo.GetCultureInfo(CurrentCode));
			}
			catch
			{
				return value.ToString("N0", CultureInfo.InvariantCulture);
			}
		}

		/// <summary>Volle Stunde (1–24) im Uhrzeitformat der Spielsprache: "06:00" bzw. "6:00 AM" (24 = Mitternacht).</summary>
		public static string Hour(int hour)
		{
			hour = Math.Max(1, Math.Min(24, hour));
			try
			{
				var ci = CultureInfo.CreateSpecificCulture(CurrentCode);
				string pattern = ci.DateTimeFormat.ShortTimePattern ?? "HH:mm";
				if (pattern.IndexOf('t') >= 0)		// 12-Stunden-Format mit AM/PM
					return new DateTime(2000, 1, 1, hour % 24, 0, 0).ToString(pattern, ci);
			}
			catch
			{
				// Kultur nicht verfügbar → 24 h
			}
			return hour.ToString("00") + ":00";
		}

		// Vanilla-Texte des Spiels (bereits in allen Spielsprachen vorhanden)
		public static string Game(string? key, string fallback)
		{
			if (string.IsNullOrEmpty(key))
				return fallback;
			try
			{
				string s = LocalizationAPI.L(key);
				return string.IsNullOrEmpty(s) ? fallback : s;
			}
			catch
			{
				return fallback;
			}
		}
	}
}