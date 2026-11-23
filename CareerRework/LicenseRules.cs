//FILE:		LicenseRules.cs

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using DV;
using DV.ThingTypes;
using DV.ThingTypes.TransitionHelpers;
using UnityEngine;

namespace CareerRework
{
	// =========================
	// LIZENZEN: PREISE + VORAUSSETZUNGEN
	// =========================
	// Zustandslos: alles wird aus den wirksamen Einstellungen bzw. den besessenen Lizenzen berechnet.
	// Keine Umschalt-Merker mehr → funktioniert nach Spielstandwechsel, im Multiplayer (gleiches Ergebnis
	// auf allen Rechnern) und mit DVMultiplayerIP (Lizenzen des jeweiligen Spielers).
	//
	// Lizenzen mit zwei Voraussetzungen (beide werden auf dem Lizenz-Muster angezeigt):
	//   Shunting          TrainDriver + eine Starter-Lok-Lizenz (DE2, DM3 oder S060)
	//   ConcurrentJobs2   ConcurrentJobs1 + FreightHaul
	//   TrainLength2      TrainLength1 + FreightHaul
	//   Military1         FreightHaul + Hazmat2
	//   Passengers1       FreightHaul + Fragile        (PassengerJobs)
	internal static class LicenseRules
	{
		// ---------- eine Voraussetzung ----------
		internal sealed class Req
		{
			public GeneralLicenseType_v2? General;
			public JobLicenseType_v2? Job;
			public GeneralLicenseType_v2[]? AnyOf;		// eine davon genügt (Starter-Loks)

			public bool IsMet(LicenseManager lm)
			{
				if (General != null)
					return lm.IsGeneralLicenseAcquired(General);
				if (Job != null)
					return lm.IsJobLicenseAcquired(Job);
				if (AnyOf != null)
				{
					for (int i = 0; i < AnyOf.Length; i++)
						if (lm.IsGeneralLicenseAcquired(AnyOf[i]))
							return true;
					return false;
				}
				return true;
			}

			/// <summary>Anzeige-Lizenz: bei "eine davon" die besessene bzw. die erste Starter-Lok.</summary>
			public ScriptableObject? Display(LicenseManager? lm)
			{
				if (General != null)
					return General;
				if (Job != null)
					return Job;
				if (AnyOf == null || AnyOf.Length == 0)
					return null;
				if (lm != null)
					foreach (var l in AnyOf)
						if (lm.IsGeneralLicenseAcquired(l))
							return l;
				var preferred = StarterLicense(Main.E.starterLocoPrimary);
				return preferred != null && AnyOf.Contains(preferred) ? preferred : AnyOf[0];
			}

			public string Name()
			{
				if (AnyOf != null)
					return string.Join(" / ", AnyOf.Select(LicenseName));
				return LicenseName(General ?? (ScriptableObject?)Job);
			}
		}

		private static readonly Dictionary<ScriptableObject, Req[]> table = new();
		// Originalwerte jeder Lizenz (Vanilla bzw. wie die andere Mod sie registriert hat), beim ersten Zugriff gemerkt
		internal readonly struct Original
		{
			public readonly float Price, Copay, Bonus;
			public readonly GeneralLicenseType_v2? ReqGeneral;
			public readonly JobLicenseType_v2? ReqJob;

			public Original(float price, float copay, float bonus, GeneralLicenseType_v2? g, JobLicenseType_v2? j)
			{
				Price = price; Copay = copay; Bonus = bonus; ReqGeneral = g; ReqJob = j;
			}
		}

		private static readonly Dictionary<ScriptableObject, Original> originals = new();
		private static readonly HashSet<GeneralLicenseType_v2> locoLicenses = new();
		private static readonly HashSet<ScriptableObject> managed = new();
		private static bool resolved;

		public static JobLicenseType_v2? Passengers1 { get; private set; }
		public static JobLicenseType_v2? Passengers2 { get; private set; }
		public static bool PassengerJobsPresent => Passengers1 != null || Passengers2 != null;

		// =========================
		// AUFLÖSEN
		// =========================
		public static GeneralLicenseType_v2? GL(GeneralLicenseType v1)
		{
			try { return v1.ToV2(); }
			catch { return null; }
		}

		public static JobLicenseType_v2? JL(JobLicenses v1)
		{
			try { return v1.ToV2(); }
			catch { return null; }
		}

		private static JobLicenseType_v2? JobById(string id)
		{
			var list = Globals.G?.Types?.jobLicenses;
			if (list == null)
				return null;
			for (int i = 0; i < list.Count; i++)
				if (list[i] != null && list[i].id == id)
					return list[i];
			return null;
		}

		public static GeneralLicenseType_v2? StarterLicense(StarterLocoType t) => t switch
		{
			StarterLocoType.DM3 => GL(GeneralLicenseType.DM3),
			StarterLocoType.S060 => GL(GeneralLicenseType.S060),
			_ => GL(GeneralLicenseType.DE2),
		};

		/// <summary>Lizenz zu einem Preis-Feld (null = gibt es nicht, z. B. ohne PassengerJobs).</summary>
		public static ScriptableObject? LicenseForPriceKey(string key) => key switch
		{
			"priceTrainDriver" => GL(GeneralLicenseType.TrainDriver),
			"priceShunting" => JL(JobLicenses.Shunting),
			"priceLogisticalHaul" => JL(JobLicenses.LogisticalHaul),
			"priceFreightHaul" => JL(JobLicenses.FreightHaul),
			"priceFragile" => JL(JobLicenses.Fragile),
			"priceHazmat1" => JL(JobLicenses.Hazmat1),
			"priceHazmat2" => JL(JobLicenses.Hazmat2),
			"priceHazmat3" => JL(JobLicenses.Hazmat3),
			"priceMilitary1" => JL(JobLicenses.Military1),
			"priceMilitary2" => JL(JobLicenses.Military2),
			"priceMilitary3" => JL(JobLicenses.Military3),
			"priceConcurrentJobs1" => GL(GeneralLicenseType.ConcurrentJobs1),
			"priceConcurrentJobs2" => GL(GeneralLicenseType.ConcurrentJobs2),
			"priceTrainLength1" => JL(JobLicenses.TrainLength1),
			"priceTrainLength2" => JL(JobLicenses.TrainLength2),
			"priceMultipleUnit" => GL(GeneralLicenseType.MultipleUnit),
			"priceManualService" => GL(GeneralLicenseType.ManualService),
			"priceMuseum" => GL(GeneralLicenseType.MuseumCitySouth),
			"priceDispatcher" => GL(GeneralLicenseType.Dispatcher1),
			"priceDE2" => GL(GeneralLicenseType.DE2),
			"priceDM3" => GL(GeneralLicenseType.DM3),
			"priceS060" => GL(GeneralLicenseType.S060),
			"priceDH4" => GL(GeneralLicenseType.DH4),
			"priceS282" => GL(GeneralLicenseType.SH282),
			"priceDE6" => GL(GeneralLicenseType.DE6),
			"pricePassengers1" => Passengers1 ?? JobById("Passengers1"),
			"pricePassengers2" => Passengers2 ?? JobById("Passengers2"),
			_ => null,
		};

		private static void Resolve()
		{
			// PassengerJobs registriert seine Lizenzen ggf. später → bei jedem Apply erneut prüfen (billig)
			Passengers1 = JobById("Passengers1");
			Passengers2 = JobById("Passengers2");

			if (resolved && (Passengers1 == null || table.ContainsKey(Passengers1)))
				return;

			table.Clear();

			var trainDriver = GL(GeneralLicenseType.TrainDriver);
			var de2 = GL(GeneralLicenseType.DE2);
			var dm3 = GL(GeneralLicenseType.DM3);
			var s060 = GL(GeneralLicenseType.S060);
			var cj1 = GL(GeneralLicenseType.ConcurrentJobs1);
			var cj2 = GL(GeneralLicenseType.ConcurrentJobs2);
			var manual = GL(GeneralLicenseType.ManualService);

			var shunting = JL(JobLicenses.Shunting);
			var logistic = JL(JobLicenses.LogisticalHaul);
			var freight = JL(JobLicenses.FreightHaul);
			var fragile = JL(JobLicenses.Fragile);
			var hz1 = JL(JobLicenses.Hazmat1);
			var hz2 = JL(JobLicenses.Hazmat2);
			var mil1 = JL(JobLicenses.Military1);
			var mil2 = JL(JobLicenses.Military2);
			var mil3 = JL(JobLicenses.Military3);
			var tl1 = JL(JobLicenses.TrainLength1);
			var tl2 = JL(JobLicenses.TrainLength2);

			Req G(GeneralLicenseType_v2? l) => new Req { General = l };
			Req J(JobLicenseType_v2? l) => new Req { Job = l };

			void Add(ScriptableObject? license, params Req[] reqs)
			{
				if (license == null)
					return;
				table[license] = reqs.Where(r => r.General != null || r.Job != null || (r.AnyOf != null && r.AnyOf.Length > 0)).ToArray();
			}

			var starters = new[] { de2, dm3, s060 }.Where(l => l != null).Cast<GeneralLicenseType_v2>().ToArray();

			// Jobs
			Add(shunting, G(trainDriver), new Req { AnyOf = starters });
			Add(logistic, J(shunting));
			Add(freight, J(logistic));
			Add(tl1, J(shunting));
			Add(tl2, J(tl1), J(freight));
			Add(fragile, J(shunting));
			Add(hz1, J(fragile));
			Add(hz2, J(hz1));
			Add(JL(JobLicenses.Hazmat3), J(mil3));
			Add(mil1, J(freight), J(hz2));
			Add(mil2, J(mil1));
			Add(mil3, J(mil2));
			Add(Passengers1, J(freight), J(fragile));

			// Karriere
			Add(manual, J(shunting));
			Add(cj1, J(shunting));
			Add(cj2, G(cj1), J(freight));
			Add(GL(GeneralLicenseType.Dispatcher1), G(cj2));
			Add(GL(GeneralLicenseType.MultipleUnit), J(tl2));
			Add(GL(GeneralLicenseType.MuseumCitySouth), G(manual));

			// Loks
			Add(de2, G(trainDriver));
			Add(dm3, G(trainDriver));
			Add(s060, G(trainDriver));
			Add(GL(GeneralLicenseType.DH4), J(tl1));
			Add(GL(GeneralLicenseType.DE6), J(tl2));
			Add(GL(GeneralLicenseType.SH282), J(tl2));

			// Alle Lok-Lizenzen (für "erste Fahrzeuglizenz")
			locoLicenses.Clear();
			try
			{
				foreach (var livery in Globals.G.Types.Liveries)
					if (livery != null && livery.requiredLicense != null)
						locoLicenses.Add(livery.requiredLicense);
			}
			catch { }
			foreach (var l in starters)
				locoLicenses.Add(l);

			// fest verwaltet = Regel-Tabelle + alle Lizenzen mit eigenem Preisfeld
			managed.Clear();
			foreach (var k in table.Keys)
				managed.Add(k);
			foreach (var f in CareerReworkSettings.PriceFields)
			{
				var l = LicenseForPriceKey(f.Key);
				if (l != null)
					managed.Add(l);
			}

			resolved = true;
			Main.Log($"License rules resolved: {table.Count} licenses, {locoLicenses.Count} locomotive licenses, PassengerJobs={PassengerJobsPresent}");
		}

		// =========================
		// ANWENDEN
		// =========================
		public static void Apply(CareerReworkSettings e)
		{
			if (Globals.G == null)
				return;

			Resolve();
			ApplyLicenseValues(e);
			ApplyRequirementFields();
			ExtraLicenses.Apply(e);
		}

		// Preis, Copay und Zeitbonusfrist der fest verwalteten Lizenzen
		//   Preset: Werte aus der Preset-Tabelle (Feld-Standardwerte)
		//   Custom: Preis/Copay/Zeitbonus aus den Einstellungen (0 bzw. leer = Vanilla)
		private static void ApplyLicenseValues(CareerReworkSettings e)
		{
			bool custom = e.startupMode == StartupMode.Custom;

			foreach (var f in CareerReworkSettings.PriceFields)
			{
				var license = LicenseForPriceKey(f.Key);
				if (license == null)
					continue;

				var o = GetOriginal(license);

				int value = f.Get(e);
				float price = custom && value <= 0
					? (o.Price > 0f ? o.Price : 10000f)		// 0 = Vanilla; im Vanilla gratis → 10.000 $
					: value;

				int copay = f.GetCopay(e);
				float copayValue = copay > 0 ? copay : o.Copay;

				// Anzeige "+5 %" (Frist länger) = Feldwert −0,05 (das Spiel speichert eine Verkürzung); 0 = Vanilla
				float shown = f.GetBonus(e);
				float bonus = Mathf.Abs(shown) > 0.001f ? -shown / 100f : o.Bonus;

				SetValues(license, price, copayValue, bonus);
			}
		}

		/// <summary>Originalwerte (beim ersten Zugriff gemerkt, bevor die Mod sie ändert).</summary>
		internal static Original GetOriginal(ScriptableObject license)
		{
			if (!originals.TryGetValue(license, out var o))
			{
				o = license switch
				{
					GeneralLicenseType_v2 g => new Original(g.price, g.insuranceFeeQuotaIncrease, g.bonusTimeDecreasePercentage, g.requiredGeneralLicense, g.requiredJobLicense),
					JobLicenseType_v2 j => new Original(j.price, j.insuranceFeeQuotaIncrease, j.bonusTimeDecreasePercentage, j.requiredGeneralLicense, j.requiredJobLicense),
					_ => default,
				};
				originals[license] = o;
			}
			return o;
		}

		public static float VanillaPrice(ScriptableObject license) => GetOriginal(license).Price;

		public static float GetPrice(ScriptableObject license) => license switch
		{
			GeneralLicenseType_v2 g => g.price,
			JobLicenseType_v2 j => j.price,
			_ => 0f,
		};

		public static float GetCopay(ScriptableObject license) => license switch
		{
			GeneralLicenseType_v2 g => g.insuranceFeeQuotaIncrease,
			JobLicenseType_v2 j => j.insuranceFeeQuotaIncrease,
			_ => 0f,
		};

		public static float GetBonus(ScriptableObject license) => license switch
		{
			GeneralLicenseType_v2 g => g.bonusTimeDecreasePercentage,
			JobLicenseType_v2 j => j.bonusTimeDecreasePercentage,
			_ => 0f,
		};

		/// <summary>
		/// Eigene Werte direkt vor der Verwendung erneut setzen (Lizenzliste, Bezahlen, Kauf, Lizenzmuster).
		/// Andere Mods setzen ihre Preise teils selbst beim Laden oder später – das hier gewinnt immer,
		/// egal in welcher Reihenfolge die Mods geladen werden. Kostet nur ein paar Feldzuweisungen.
		/// </summary>
		internal static void Enforce()
		{
			if (!Main.WorldActive || Globals.G == null)
				return;
			try
			{
				Apply(Main.E);
			}
			catch (Exception ex)
			{
				Main.Warn("Re-applying license values failed: " + ex.Message);
			}
		}

		internal static void SetValues(ScriptableObject license, float price, float copay, float bonus)
		{
			if (license is GeneralLicenseType_v2 g)
			{
				g.price = price;
				g.insuranceFeeQuotaIncrease = copay;
				g.bonusTimeDecreasePercentage = bonus;
			}
			else if (license is JobLicenseType_v2 j)
			{
				j.price = price;
				j.insuranceFeeQuotaIncrease = copay;
				j.bonusTimeDecreasePercentage = bonus;
			}
		}

		internal static void SetRequirement(ScriptableObject license, GeneralLicenseType_v2? g, JobLicenseType_v2? j)
		{
			if (license is GeneralLicenseType_v2 gl)
			{
				gl.requiredGeneralLicense = g!;
				gl.requiredJobLicense = j!;
			}
			else if (license is JobLicenseType_v2 jl)
			{
				jl.requiredGeneralLicense = g!;
				jl.requiredJobLicense = j!;
			}
		}

		/// <summary>Zeitbonusfrist wie auf der Lizenz angezeigt, z. B. "+5.00 %" (Feldwert hat umgekehrtes Vorzeichen).</summary>
		public static string FormatBonus(float fieldValue)
		{
			float shown = -fieldValue * 100f;
			if (Mathf.Abs(shown) < 0.001f)
				return "N/A";
			return (shown > 0 ? "+" : "−") + Mathf.Abs(shown).ToString("0.##") + " %";
		}

		// =========================
		// SUMMEN NEU BERECHNEN
		// =========================
		// Das Spiel addiert Copay und Zeitbonus beim ERWERB einer Lizenz auf (LicenseManager.InsuranceFeeQuota /
		// BonusTimeDecreasePercentage). Ändern sich die Werte nach dem Laden (z. B. MP-Client bekommt die Host-
		// Einstellungen), werden die Summen hier aus den besessenen Lizenzen neu berechnet.
		private static MethodInfo? setInsurance, setBonus;
		private static FieldInfo? insuranceUpdated;

		public static void RecalculateTotals(LicenseManager lm)
		{
			try
			{
				float insurance = 0f, bonus = 0f;
				foreach (var g in lm.GetGeneralAcquiredLicenses())
					if (g != null)
					{
						insurance += g.insuranceFeeQuotaIncrease;
						bonus = Mathf.Clamp(bonus + g.bonusTimeDecreasePercentage, -0.7f, 0.7f);
					}
				foreach (var j in lm.GetAcquiredJobLicenses())
					if (j != null)
					{
						insurance += j.insuranceFeeQuotaIncrease;
						bonus = Mathf.Clamp(bonus + j.bonusTimeDecreasePercentage, -0.7f, 0.7f);
					}

				bool changed = !Mathf.Approximately(lm.InsuranceFeeQuota, insurance) || !Mathf.Approximately(lm.BonusTimeDecreasePercentage, bonus);
				if (!changed)
					return;

				setInsurance ??= AccessTools.PropertySetter(typeof(LicenseManager), nameof(LicenseManager.InsuranceFeeQuota));
				setBonus ??= AccessTools.PropertySetter(typeof(LicenseManager), nameof(LicenseManager.BonusTimeDecreasePercentage));
				insuranceUpdated ??= AccessTools.Field(typeof(LicenseManager), "InsuranceFeeQuotaUpdated");

				setInsurance?.Invoke(lm, new object[] { insurance });
				setBonus?.Invoke(lm, new object[] { bonus });
				(insuranceUpdated?.GetValue(lm) as Action)?.Invoke();

				Main.Log($"License totals recalculated: copay {insurance:N0} $, time bonus {FormatBonus(bonus)}");
			}
			catch (Exception e)
			{
				Main.Warn("Recalculating license totals failed: " + e.Message);
			}
		}

		/// <summary>Fest verwaltete Lizenzen (erscheinen nicht bei den Zusatzlizenzen).</summary>
		public static bool IsManaged(ScriptableObject license)
		{
			if (!resolved)
				Resolve();
			return managed.Contains(license);
		}

		/// <summary>Effektive Voraussetzungen einer fest verwalteten Lizenz (für die Zyklusprüfung).</summary>
		internal static IEnumerable<ScriptableObject> ManagedRequirements(ScriptableObject license)
		{
			if (!table.TryGetValue(license, out var reqs))
				yield break;
			foreach (var r in reqs)
			{
				if (r.General != null) yield return r.General;
				if (r.Job != null) yield return r.Job;
				if (r.AnyOf != null)
					foreach (var a in r.AnyOf)
						yield return a;
			}
		}

		// Vanilla-Felder: erste allgemeine + erste Job-Voraussetzung (für Vanilla-Code und Anzeige).
		// Die vollständige Prüfung macht IsObtainable (Patch).
		private static void ApplyRequirementFields()
		{
			// Instance würde im Hauptmenü automatisch erzeugt → nur in der Welt abfragen
			LicenseManager? lm = Main.WorldActive ? LicenseManager.Instance : null;
			foreach (var kv in table)
			{
				GeneralLicenseType_v2? g = null;
				JobLicenseType_v2? j = null;
				foreach (var r in kv.Value)
				{
					if (g == null && r.General != null) g = r.General;
					if (g == null && r.AnyOf != null) g = r.Display(lm) as GeneralLicenseType_v2;
					if (j == null && r.Job != null) j = r.Job;
				}

				GetOriginal(kv.Key);		// Original merken, bevor es überschrieben wird
				SetRequirement(kv.Key, g, j);
			}
		}

		// =========================
		// ABFRAGEN (Patches)
		// =========================
		public static bool TryGet(ScriptableObject? license, out Req[] reqs)
		{
			if (license != null && table.TryGetValue(license, out var r))
			{
				reqs = r;
				return true;
			}
			reqs = Array.Empty<Req>();
			return false;
		}

		public static bool AllMet(LicenseManager lm, Req[] reqs)
		{
			for (int i = 0; i < reqs.Length; i++)
				if (!reqs[i].IsMet(lm))
					return false;
			return true;
		}

		public static bool IsLocoLicense(GeneralLicenseType_v2 license)
		{
			if (!resolved)
				Resolve();
			return locoLicenses.Contains(license);
		}

		public static bool OwnsAnyLocoLicense(LicenseManager lm)
		{
			if (!resolved)
				Resolve();
			foreach (var l in locoLicenses)
				if (lm.IsGeneralLicenseAcquired(l))
					return true;
			return false;
		}

		public static string LicenseName(ScriptableObject? license) => license switch
		{
			GeneralLicenseType_v2 g => Loc.Game(g.localizationKey, g.id),
			JobLicenseType_v2 j => Loc.Game(j.localizationKey, j.id),
			_ => "?",
		};

		public static Sprite? Icon(ScriptableObject? license) => license switch
		{
			GeneralLicenseType_v2 g => g.icon,
			JobLicenseType_v2 j => j.icon,
			_ => null,
		};
	}
}
