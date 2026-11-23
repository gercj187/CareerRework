//FILE:		Settings.cs

using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json.Linq;
using UnityEngine;
using UnityModManagerNet;

namespace CareerRework
{
	public enum StartupMode
	{
		Preset,
		Custom
	}

	public enum StarterLocoType
	{
		DE2,
		DM3,
		S060
	}

	// =========================
	// EINSTELLUNGEN
	// =========================
	// Feldnamen bleiben wie bisher → alte Settings.xml und alte Savegames werden weiter gelesen.
	//
	// Wirksam ist immer Main.Settings:
	//   Hauptmenü          → die eigenen Einstellungen (Settings.xml)
	//   Spielstand geladen → die im Savegame gespeicherten Werte (nicht mehr änderbar)
	//   MP-Client          → die Werte des Hosts
	public class CareerReworkSettings : UnityModManager.ModSettings
	{
		public StartupMode startupMode = StartupMode.Preset;

		public StarterLocoType starterLocoPrimary = StarterLocoType.DE2;		// Steel Mill, Gleis T1-01-P
		public StarterLocoType starterLocoSecondary = StarterLocoType.DM3;		// Steel Mill, Gleis T1-02-P
		public int startingMoney = 150000;		// Lokführer-Lizenz ist jetzt Startlizenz

		public bool startWithConcurrentJobs1 = false;
		public bool startWithConcurrentJobs2 = false;

		public int priceMultiplierKeysGadgets = 10;
		public int priceMultiplierRestoration = 10;

		// Lizenzpreise in $ (0 = Vanilla). Standardwerte = CareerRework-Preset.
		public int priceTrainDriver = 50000;
		public int priceShunting = 50000;
		public int priceLogisticalHaul = 100000;
		public int priceFreightHaul = 200000;
		public int priceFragile = 100000;
		public int priceHazmat1 = 250000;
		public int priceHazmat2 = 500000;
		public int priceHazmat3 = 8000000;
		public int priceMilitary1 = 1000000;
		public int priceMilitary2 = 2000000;
		public int priceMilitary3 = 4000000;
		public int priceConcurrentJobs1 = 100000;
		public int priceConcurrentJobs2 = 300000;
		public int priceTrainLength1 = 100000;
		public int priceTrainLength2 = 300000;
		public int priceMultipleUnit = 400000;
		public int priceManualService = 100000;
		public int priceMuseum = 300000;
		public int priceDispatcher = 400000;
		public int priceDE2 = 75000;
		public int priceDM3 = 60000;
		public int priceS060 = 50000;
		public int priceDH4 = 500000;
		public int priceS282 = 750000;
		public int priceDE6 = 1000000;
		public int pricePassengers1 = 500000;
		public int pricePassengers2 = 1000000;

		// Versicherungs-Selbstbeteiligung (Copay) in $ pro Lizenz (0 = Vanilla)
		public int copayTrainDriver = 1000;
		public int copayShunting = 2500;
		public int copayLogisticalHaul = 5000;
		public int copayFreightHaul = 10000;
		public int copayFragile = 5000;
		public int copayHazmat1 = 12500;
		public int copayHazmat2 = 25000;
		public int copayHazmat3 = 400000;
		public int copayMilitary1 = 50000;
		public int copayMilitary2 = 100000;
		public int copayMilitary3 = 200000;
		public int copayConcurrentJobs1 = 5000;
		public int copayConcurrentJobs2 = 15000;
		public int copayTrainLength1 = 5000;
		public int copayTrainLength2 = 15000;
		public int copayMultipleUnit = 20000;
		public int copayManualService = 5000;
		public int copayMuseum = 15000;
		public int copayDispatcher = 20000;
		public int copayDE2 = 1500;
		public int copayDM3 = 1000;
		public int copayS060 = 500;
		public int copayDH4 = 25000;
		public int copayS282 = 35000;
		public int copayDE6 = 50000;
		public int copayPassengers1 = 0;
		public int copayPassengers2 = 0;

		// Zeitbonusfrist in % wie auf der Lizenz angezeigt (+5 = mehr Zeit, −5 = weniger Zeit; 0 = Vanilla)
		public float bonusTrainDriver = 0f;
		public float bonusShunting = 5.0f;
		public float bonusLogisticalHaul = 0f;
		public float bonusFreightHaul = 5.0f;
		public float bonusFragile = -2.5f;
		public float bonusHazmat1 = -2.5f;
		public float bonusHazmat2 = -5.0f;
		public float bonusHazmat3 = -7.5f;
		public float bonusMilitary1 = -2.5f;
		public float bonusMilitary2 = -5.0f;
		public float bonusMilitary3 = -7.5f;
		public float bonusConcurrentJobs1 = 0f;
		public float bonusConcurrentJobs2 = 0f;
		public float bonusTrainLength1 = 0f;
		public float bonusTrainLength2 = -5.0f;
		public float bonusMultipleUnit = 0f;
		public float bonusManualService = 0f;
		public float bonusMuseum = 0f;
		public float bonusDispatcher = 0f;
		public float bonusDE2 = -5.0f;
		public float bonusDM3 = 0f;
		public float bonusS060 = 5.0f;
		public float bonusDH4 = 0f;
		public float bonusS282 = -2.0f;
		public float bonusDE6 = -5.0f;
		public float bonusPassengers1 = 0f;
		public float bonusPassengers2 = 0f;

		// Startuhrzeit einer neuen Karriere (volle Stunde, 1–24; 24 = Mitternacht)
		public int startHour = 6;

		// Lizenzen aus anderen Mods (bzw. alle, die CareerRework nicht selbst verwaltet)
		public List<ExtraLicense> extraLicenses = new List<ExtraLicense>();

		// Nur im Savegame: Zusatzlizenzen, die dieser Spielstand schon kennt (Schlüssel "G:id"/"J:id").
		// Lizenzen, die hier fehlen (neue Mod installiert), übernehmen beim ersten Laden die Menü-Einstellungen.
		[System.Xml.Serialization.XmlIgnore]
		public List<string> knownLicenses = new List<string>();

		// nur lokal (nicht im Savegame, nicht synchronisiert)
		public bool debugLog = false;

		// Version der lokalen Settings.xml (für einmalige Anpassungen alter Standardwerte)
		public int localVersion = 0;

		// Komfort: im CareerManager nur Lizenzen zeigen, die man besitzt oder gerade kaufen kann.
		// Nicht im Savegame – gilt pro Sitzung (beim Laden der Welt festgelegt), im Multiplayer vom Host vorgegeben.
		public bool hiddenProgression = false;

		public override void Save(UnityModManager.ModEntry modEntry)
		{
			Save(this, modEntry);
		}

		// =========================
		// PRESET (fest, eine Quelle für alle Werte)
		// =========================
		public const int PresetStartingMoney = 150000;
		public const int PresetGadgetMultiplier = 10;		// Schlüssel/Gadgets haben im Preset feste Preise (≈ x10)
		public const int PresetRestorationMultiplier = 10;

		private static readonly CareerReworkSettings presetValues = new CareerReworkSettings();	// Feld-Standardwerte = Preset

		/// <summary>Preset: feste Werte; Start-Optionen und Lizenzen anderer Mods kommen aus diesen Einstellungen.</summary>
		public CareerReworkSettings Effective()
		{
			if (startupMode == StartupMode.Custom)
				return this;

			var p = presetValues;
			p.startupMode = StartupMode.Preset;
			p.startWithConcurrentJobs1 = startWithConcurrentJobs1;
			p.startWithConcurrentJobs2 = startWithConcurrentJobs1 && startWithConcurrentJobs2;
			p.starterLocoPrimary = StarterLocoType.DE2;
			p.starterLocoSecondary = StarterLocoType.DM3;
			p.startingMoney = PresetStartingMoney;
			p.priceMultiplierKeysGadgets = PresetGadgetMultiplier;
			p.priceMultiplierRestoration = PresetRestorationMultiplier;
			p.startHour = startHour;
			p.extraLicenses = extraLicenses;		// Lizenzen anderer Mods kennt das Preset nicht → immer frei einstellbar
			return p;
		}

		// =========================
		// LIZENZPREIS-TABELLE
		// =========================
		public sealed class PriceField
		{
			public readonly string Name;				// z. B. "Shunting"
			public readonly string Key;					// "priceShunting" (Savegame/GUI)
			public readonly string CopayKey;			// "copayShunting"
			public readonly Func<CareerReworkSettings, int> Get;
			public readonly Action<CareerReworkSettings, int> Set;
			public readonly Func<CareerReworkSettings, int> GetCopay;
			public readonly Action<CareerReworkSettings, int> SetCopay;
			public readonly string BonusKey;			// "bonusShunting"
			/// <summary>Zeitbonusfrist in % wie auf der Lizenz angezeigt (+5 = Frist länger); 0 = Vanilla.</summary>
			public readonly Func<CareerReworkSettings, float> GetBonus;
			public readonly Action<CareerReworkSettings, float> SetBonus;

			public PriceField(string name, Func<CareerReworkSettings, int> get, Action<CareerReworkSettings, int> set,
				Func<CareerReworkSettings, int> getCopay, Action<CareerReworkSettings, int> setCopay,
				Func<CareerReworkSettings, float> getBonus, Action<CareerReworkSettings, float> setBonus)
			{
				Name = name;
				Key = "price" + name;
				CopayKey = "copay" + name;
				Get = get;
				Set = set;
				GetCopay = getCopay;
				SetCopay = setCopay;
				BonusKey = "bonus" + name;
				GetBonus = getBonus;
				SetBonus = setBonus;
			}
		}

		public static readonly PriceField[] PriceFields =
		{
			new("TrainDriver", s => s.priceTrainDriver, (s, v) => s.priceTrainDriver = v, s => s.copayTrainDriver, (s, v) => s.copayTrainDriver = v, s => s.bonusTrainDriver, (s, v) => s.bonusTrainDriver = v),
			new("Shunting", s => s.priceShunting, (s, v) => s.priceShunting = v, s => s.copayShunting, (s, v) => s.copayShunting = v, s => s.bonusShunting, (s, v) => s.bonusShunting = v),
			new("LogisticalHaul", s => s.priceLogisticalHaul, (s, v) => s.priceLogisticalHaul = v, s => s.copayLogisticalHaul, (s, v) => s.copayLogisticalHaul = v, s => s.bonusLogisticalHaul, (s, v) => s.bonusLogisticalHaul = v),
			new("FreightHaul", s => s.priceFreightHaul, (s, v) => s.priceFreightHaul = v, s => s.copayFreightHaul, (s, v) => s.copayFreightHaul = v, s => s.bonusFreightHaul, (s, v) => s.bonusFreightHaul = v),
			new("Fragile", s => s.priceFragile, (s, v) => s.priceFragile = v, s => s.copayFragile, (s, v) => s.copayFragile = v, s => s.bonusFragile, (s, v) => s.bonusFragile = v),
			new("Hazmat1", s => s.priceHazmat1, (s, v) => s.priceHazmat1 = v, s => s.copayHazmat1, (s, v) => s.copayHazmat1 = v, s => s.bonusHazmat1, (s, v) => s.bonusHazmat1 = v),
			new("Hazmat2", s => s.priceHazmat2, (s, v) => s.priceHazmat2 = v, s => s.copayHazmat2, (s, v) => s.copayHazmat2 = v, s => s.bonusHazmat2, (s, v) => s.bonusHazmat2 = v),
			new("Hazmat3", s => s.priceHazmat3, (s, v) => s.priceHazmat3 = v, s => s.copayHazmat3, (s, v) => s.copayHazmat3 = v, s => s.bonusHazmat3, (s, v) => s.bonusHazmat3 = v),
			new("Military1", s => s.priceMilitary1, (s, v) => s.priceMilitary1 = v, s => s.copayMilitary1, (s, v) => s.copayMilitary1 = v, s => s.bonusMilitary1, (s, v) => s.bonusMilitary1 = v),
			new("Military2", s => s.priceMilitary2, (s, v) => s.priceMilitary2 = v, s => s.copayMilitary2, (s, v) => s.copayMilitary2 = v, s => s.bonusMilitary2, (s, v) => s.bonusMilitary2 = v),
			new("Military3", s => s.priceMilitary3, (s, v) => s.priceMilitary3 = v, s => s.copayMilitary3, (s, v) => s.copayMilitary3 = v, s => s.bonusMilitary3, (s, v) => s.bonusMilitary3 = v),
			new("ConcurrentJobs1", s => s.priceConcurrentJobs1, (s, v) => s.priceConcurrentJobs1 = v, s => s.copayConcurrentJobs1, (s, v) => s.copayConcurrentJobs1 = v, s => s.bonusConcurrentJobs1, (s, v) => s.bonusConcurrentJobs1 = v),
			new("ConcurrentJobs2", s => s.priceConcurrentJobs2, (s, v) => s.priceConcurrentJobs2 = v, s => s.copayConcurrentJobs2, (s, v) => s.copayConcurrentJobs2 = v, s => s.bonusConcurrentJobs2, (s, v) => s.bonusConcurrentJobs2 = v),
			new("TrainLength1", s => s.priceTrainLength1, (s, v) => s.priceTrainLength1 = v, s => s.copayTrainLength1, (s, v) => s.copayTrainLength1 = v, s => s.bonusTrainLength1, (s, v) => s.bonusTrainLength1 = v),
			new("TrainLength2", s => s.priceTrainLength2, (s, v) => s.priceTrainLength2 = v, s => s.copayTrainLength2, (s, v) => s.copayTrainLength2 = v, s => s.bonusTrainLength2, (s, v) => s.bonusTrainLength2 = v),
			new("MultipleUnit", s => s.priceMultipleUnit, (s, v) => s.priceMultipleUnit = v, s => s.copayMultipleUnit, (s, v) => s.copayMultipleUnit = v, s => s.bonusMultipleUnit, (s, v) => s.bonusMultipleUnit = v),
			new("ManualService", s => s.priceManualService, (s, v) => s.priceManualService = v, s => s.copayManualService, (s, v) => s.copayManualService = v, s => s.bonusManualService, (s, v) => s.bonusManualService = v),
			new("Museum", s => s.priceMuseum, (s, v) => s.priceMuseum = v, s => s.copayMuseum, (s, v) => s.copayMuseum = v, s => s.bonusMuseum, (s, v) => s.bonusMuseum = v),
			new("Dispatcher", s => s.priceDispatcher, (s, v) => s.priceDispatcher = v, s => s.copayDispatcher, (s, v) => s.copayDispatcher = v, s => s.bonusDispatcher, (s, v) => s.bonusDispatcher = v),
			new("DE2", s => s.priceDE2, (s, v) => s.priceDE2 = v, s => s.copayDE2, (s, v) => s.copayDE2 = v, s => s.bonusDE2, (s, v) => s.bonusDE2 = v),
			new("DM3", s => s.priceDM3, (s, v) => s.priceDM3 = v, s => s.copayDM3, (s, v) => s.copayDM3 = v, s => s.bonusDM3, (s, v) => s.bonusDM3 = v),
			new("S060", s => s.priceS060, (s, v) => s.priceS060 = v, s => s.copayS060, (s, v) => s.copayS060 = v, s => s.bonusS060, (s, v) => s.bonusS060 = v),
			new("DH4", s => s.priceDH4, (s, v) => s.priceDH4 = v, s => s.copayDH4, (s, v) => s.copayDH4 = v, s => s.bonusDH4, (s, v) => s.bonusDH4 = v),
			new("S282", s => s.priceS282, (s, v) => s.priceS282 = v, s => s.copayS282, (s, v) => s.copayS282 = v, s => s.bonusS282, (s, v) => s.bonusS282 = v),
			new("DE6", s => s.priceDE6, (s, v) => s.priceDE6 = v, s => s.copayDE6, (s, v) => s.copayDE6 = v, s => s.bonusDE6, (s, v) => s.bonusDE6 = v),
			new("Passengers1", s => s.pricePassengers1, (s, v) => s.pricePassengers1 = v, s => s.copayPassengers1, (s, v) => s.copayPassengers1 = v, s => s.bonusPassengers1, (s, v) => s.bonusPassengers1 = v),
			new("Passengers2", s => s.pricePassengers2, (s, v) => s.pricePassengers2 = v, s => s.copayPassengers2, (s, v) => s.copayPassengers2 = v, s => s.bonusPassengers2, (s, v) => s.bonusPassengers2 = v),
		};

		// =========================
		// KOPIEREN / PRÜFEN
		// =========================
		public CareerReworkSettings Clone()
		{
			var c = new CareerReworkSettings();
			c.CopyGameplayFrom(this);
			c.debugLog = debugLog;
			return c;
		}

		public void CopyGameplayFrom(CareerReworkSettings s)
		{
			startupMode = s.startupMode;
			starterLocoPrimary = s.starterLocoPrimary;
			starterLocoSecondary = s.starterLocoSecondary;
			startingMoney = s.startingMoney;
			startWithConcurrentJobs1 = s.startWithConcurrentJobs1;
			startWithConcurrentJobs2 = s.startWithConcurrentJobs1 && s.startWithConcurrentJobs2;
			priceMultiplierKeysGadgets = s.priceMultiplierKeysGadgets;
			priceMultiplierRestoration = s.priceMultiplierRestoration;
			startHour = s.startHour;
			foreach (var f in PriceFields)
			{
				f.Set(this, f.Get(s));
				f.SetCopay(this, f.GetCopay(s));
				f.SetBonus(this, f.GetBonus(s));
			}
			extraLicenses = new List<ExtraLicense>();
			foreach (var x in s.extraLicenses)
				if (x != null)
					extraLicenses.Add(x.Clone());
			knownLicenses = new List<string>(s.knownLicenses ?? new List<string>());
		}

		public void Sanitize()
		{
			startingMoney = Mathf.Clamp(startingMoney, 0, 100000000);
			priceMultiplierKeysGadgets = Mathf.Clamp(priceMultiplierKeysGadgets, 1, 10);
			priceMultiplierRestoration = Mathf.Clamp(priceMultiplierRestoration, 1, 10);
			if (!startWithConcurrentJobs1)
				startWithConcurrentJobs2 = false;
			startHour = Mathf.Clamp(startHour, 1, 24);
			foreach (var f in PriceFields)
			{
				f.Set(this, Math.Max(0, f.Get(this)));
				f.SetCopay(this, Math.Max(0, f.GetCopay(this)));
				float b = f.GetBonus(this);
				f.SetBonus(this, float.IsNaN(b) ? 0f : Mathf.Clamp(Mathf.Round(b * 100f) / 100f, -70f, 70f));
			}

			extraLicenses ??= new List<ExtraLicense>();
			extraLicenses.RemoveAll(x => x == null || string.IsNullOrEmpty(x.id));
			// pro Lizenz nur ein Eintrag (Idempotenz)
			var seen = new HashSet<string>();
			extraLicenses.RemoveAll(x => !seen.Add(x.Key));
			foreach (var x in extraLicenses)
			{
				x.price = Math.Max(0, x.price);
				x.copay = Math.Max(0, x.copay);
				x.bonus = float.IsNaN(x.bonus) ? 0f : Mathf.Clamp(x.bonus, -70f, 70f);
				x.required ??= "";
			}
		}

		/// <summary>Eintrag für eine Zusatzlizenz (wird bei Bedarf angelegt).</summary>
		public ExtraLicense GetOrAddExtra(string id, bool job)
		{
			foreach (var x in extraLicenses)
				if (x.id == id && x.job == job)
					return x;
			var e = new ExtraLicense { id = id, job = job };
			extraLicenses.Add(e);
			return e;
		}

		public ExtraLicense? FindExtra(string id, bool job)
		{
			foreach (var x in extraLicenses)
				if (x.id == id && x.job == job)
					return x;
			return null;
		}

		// =========================
		// SAVEGAME (JSON)
		// =========================
		// Preset: nur Modus + Start-Optionen (Preset-Werte kommen immer aus der Mod).
		// Custom: alle Werte – danach gelten die Werte aus dem Savegame, egal was im Menü eingestellt ist.
		public JObject ToJson()
		{
			var o = new JObject
			{
				["Gamemode"] = startupMode.ToString(),
				["startWithConcurrentJobs1"] = startWithConcurrentJobs1,
				["startWithConcurrentJobs2"] = startWithConcurrentJobs1 && startWithConcurrentJobs2,
				["startHour"] = startHour,
			};

			// Lizenzen anderer Mods (beide Modi)
			var extras = new JArray();
			foreach (var x in extraLicenses)
				if (!x.IsDefault)
					extras.Add(x.ToJson());
			o["extraLicenses"] = extras;
			o["knownLicenses"] = new JArray(knownLicenses ?? new List<string>());

			if (startupMode == StartupMode.Custom)
			{
				o["starterLocoPrimary"] = starterLocoPrimary.ToString();
				o["starterLocoSecondary"] = starterLocoSecondary.ToString();
				o["startingMoney"] = startingMoney;
				o["priceMultiplierKeysGadgets"] = priceMultiplierKeysGadgets;
				o["priceMultiplierRestoration"] = priceMultiplierRestoration;
				foreach (var f in PriceFields)
				{
					o[f.Key] = f.Get(this);
					o[f.CopayKey] = f.GetCopay(this);
					o[f.BonusKey] = f.GetBonus(this);
				}
			}

			return o;
		}

		/// <summary>Werte aus dem Savegame übernehmen. Fehlende Einträge (ältere Savegames) behalten ihren Wert.</summary>
		public void ReadJson(JObject o)
		{
			startupMode = ReadEnum(o, "Gamemode", startupMode);
			startWithConcurrentJobs1 = ReadBool(o, "startWithConcurrentJobs1", startWithConcurrentJobs1);
			startWithConcurrentJobs2 = startWithConcurrentJobs1 && ReadBool(o, "startWithConcurrentJobs2", startWithConcurrentJobs2);
			startHour = ReadInt(o, "startHour", startHour);

			// Savegames vor 1.07: keine Zusatzlizenzen → nichts verändern
			extraLicenses = new List<ExtraLicense>();
			if (o["extraLicenses"] is JArray extras)
				foreach (var t in extras)
					if (t is JObject jo)
						extraLicenses.Add(ExtraLicense.FromJson(jo));

			// Bekannte Zusatzlizenzen. Ältere Savegames ohne Liste: nur die mit gespeicherten Werten gelten als
			// bekannt – alle anderen übernehmen beim Laden einmalig die Menü-Einstellungen.
			knownLicenses = new List<string>();
			if (o["knownLicenses"] is JArray known)
			{
				foreach (var t in known)
					if (t.Type == JTokenType.String && !string.IsNullOrEmpty(t.Value<string>()))
						knownLicenses.Add(t.Value<string>()!);
			}
			else
			{
				foreach (var x in extraLicenses)
					knownLicenses.Add(x.Key);
			}

			if (startupMode == StartupMode.Custom)
			{
				starterLocoPrimary = ReadEnum(o, "starterLocoPrimary", starterLocoPrimary);
				starterLocoSecondary = ReadEnum(o, "starterLocoSecondary", starterLocoSecondary);
				startingMoney = ReadInt(o, "startingMoney", startingMoney);
				priceMultiplierKeysGadgets = ReadInt(o, "priceMultiplierKeysGadgets", priceMultiplierKeysGadgets);
				// Savegames vor 1.07: Restauration war nie aktiv → x1
				priceMultiplierRestoration = ReadInt(o, "priceMultiplierRestoration", 1);
				foreach (var f in PriceFields)
				{
					f.Set(this, ReadInt(o, f.Key, f.Get(this)));
					// Savegames vor 1.07: Copay war Vanilla → 0
					f.SetCopay(this, ReadInt(o, f.CopayKey, 0));
					// Savegames vor 1.07: Zeitbonus war Vanilla → 0
					f.SetBonus(this, ReadFloat(o, f.BonusKey, 0f));
				}
			}

			Sanitize();
		}

		private static int ReadInt(JObject o, string key, int fallback)
		{
			var t = o[key];
			if (t == null || t.Type == JTokenType.Null)
				return fallback;
			try { return t.Value<int>(); }
			catch { return fallback; }
		}

		private static float ReadFloat(JObject o, string key, float fallback)
		{
			var t = o[key];
			if (t == null || t.Type == JTokenType.Null)
				return fallback;
			try { return t.Value<float>(); }
			catch { return fallback; }
		}

		private static bool ReadBool(JObject o, string key, bool fallback)
		{
			var t = o[key];
			if (t == null || t.Type == JTokenType.Null)
				return fallback;
			try { return t.Value<bool>(); }
			catch { return fallback; }
		}

		private static TEnum ReadEnum<TEnum>(JObject o, string key, TEnum fallback) where TEnum : struct
		{
			var t = o[key];
			if (t == null || t.Type == JTokenType.Null)
				return fallback;
			string? s = t.Value<string>();
			return s != null && Enum.TryParse(s, true, out TEnum v) ? v : fallback;
		}

		// =========================
		// MULTIPLAYER (binär, Host → Client)
		// =========================
		public void WriteGameplay(BinaryWriter w)
		{
			w.Write((byte)startupMode);
			w.Write((byte)starterLocoPrimary);
			w.Write((byte)starterLocoSecondary);
			w.Write(startingMoney);
			w.Write(startWithConcurrentJobs1);
			w.Write(startWithConcurrentJobs2);
			w.Write(priceMultiplierKeysGadgets);
			w.Write(priceMultiplierRestoration);
			w.Write(startHour);
			w.Write(PriceFields.Length);
			foreach (var f in PriceFields)
			{
				w.Write(f.Get(this));
				w.Write(f.GetCopay(this));
				w.Write(f.GetBonus(this));
			}

			w.Write(extraLicenses.Count);
			foreach (var x in extraLicenses)
			{
				w.Write(x.id);
				w.Write(x.job);
				w.Write(x.price);
				w.Write(x.copay);
				w.Write(x.bonus);
				w.Write(x.required ?? "");
			}
		}

		public void ReadGameplay(BinaryReader r)
		{
			startupMode = (StartupMode)Mathf.Clamp(r.ReadByte(), 0, 1);
			starterLocoPrimary = (StarterLocoType)Mathf.Clamp(r.ReadByte(), 0, 2);
			starterLocoSecondary = (StarterLocoType)Mathf.Clamp(r.ReadByte(), 0, 2);
			startingMoney = r.ReadInt32();
			startWithConcurrentJobs1 = r.ReadBoolean();
			startWithConcurrentJobs2 = r.ReadBoolean();
			priceMultiplierKeysGadgets = r.ReadInt32();
			priceMultiplierRestoration = r.ReadInt32();
			startHour = r.ReadInt32();
			int n = r.ReadInt32();
			for (int i = 0; i < n; i++)
			{
				int v = r.ReadInt32();
				int c = r.ReadInt32();
				float b = r.ReadSingle();
				if (i < PriceFields.Length)
				{
					PriceFields[i].Set(this, v);
					PriceFields[i].SetCopay(this, c);
					PriceFields[i].SetBonus(this, b);
				}
			}

			int m = r.ReadInt32();
			extraLicenses = new List<ExtraLicense>(Math.Max(0, m));
			for (int i = 0; i < m; i++)
			{
				extraLicenses.Add(new ExtraLicense
				{
					id = r.ReadString(),
					job = r.ReadBoolean(),
					price = r.ReadInt32(),
					copay = r.ReadInt32(),
					bonus = r.ReadSingle(),
					required = r.ReadString(),
				});
			}
			Sanitize();
		}
	}

	// =========================
	// ZUSATZLIZENZ (aus anderen Mods)
	// =========================
	// Gespeichert nach stabiler Lizenz-ID (nicht nach Anzeigename). Einträge für gerade nicht geladene Mods
	// bleiben erhalten und werden nur nicht angewendet.
	public class ExtraLicense
	{
		public const string ReqUnchanged = "";		// Voraussetzung der Mod beibehalten
		public const string ReqNone = "-";			// keine Voraussetzung
		// sonst "G:<id>" (allgemeine Lizenz) oder "J:<id>" (Job-Lizenz)

		public string id = "";
		public bool job;			// true = Job-Lizenz, false = allgemeine Lizenz
		public int price;			// 0 = unverändert
		public int copay;			// 0 = unverändert
		public float bonus;			// Zeitbonus in % wie auf der Lizenz angezeigt (+ = mehr Zeit); 0 = unverändert
		public string required = ReqUnchanged;

		public string Key => (job ? "J:" : "G:") + id;
		public bool IsDefault => price <= 0 && copay <= 0 && Mathf.Abs(bonus) < 0.001f && string.IsNullOrEmpty(required);

		public ExtraLicense Clone() => new ExtraLicense { id = id, job = job, price = price, copay = copay, bonus = bonus, required = required };

		public JObject ToJson() => new JObject
		{
			["id"] = id,
			["job"] = job,
			["price"] = price,
			["copay"] = copay,
			["bonus"] = bonus,
			["required"] = required ?? "",
		};

		public static ExtraLicense FromJson(JObject o) => new ExtraLicense
		{
			id = o["id"]?.Value<string>() ?? "",
			job = o["job"]?.Value<bool>() ?? false,
			price = o["price"]?.Value<int>() ?? 0,
			copay = o["copay"]?.Value<int>() ?? 0,
			bonus = o["bonus"]?.Value<float>() ?? 0f,		// ältere Spielstände: unverändert
			required = o["required"]?.Value<string>() ?? "",
		};
	}
}