//FILE:		ExtraLicenses.cs

using System;
using System.Collections.Generic;
using DV;
using DV.ThingTypes;
using UnityEngine;

namespace CareerRework
{
	// =========================
	// ZUSATZLIZENZEN (aus anderen Mods)
	// =========================
	// Jede allgemeine und jede Job-Lizenz im Spiel, die CareerRework NICHT selbst verwaltet, wird automatisch
	// erkannt (keine festen Namenslisten einzelner Mods). Pro Lizenz einstellbar: Preis, Copay, Zeitbonus, Voraussetzung.
	//
	// • Preis/Copay/Zeitbonus 0 bzw. leer = unverändert (Wert der anderen Mod wird nicht angefasst)
	// • Eingestellte Werte werden vor jeder Verwendung erneut gesetzt (Lizenzliste, Kauf, Lizenzmuster), damit
	//   Mods, die ihre Preise selbst zur Laufzeit setzen, sie nicht überschreiben – unabhängig von der Ladereihenfolge
	// • Voraussetzung "Unverändert" = die der anderen Mod, "Keine" = entfernen, sonst jede andere Lizenz
	// • Voraussetzung ist eine echte Kaufbedingung (Vanilla-Felder requiredGeneralLicense/requiredJobLicense)
	// • Selbstbezug und Zyklen (A → B → A, auch über längere Ketten) werden nicht angeboten und nie angewendet
	// • Einstellungen werden nach stabiler ID gespeichert; fehlt die Mod, bleiben sie erhalten
	//
	// Erkennung: bei jeder Abfrage wird nur verglichen, ob sich die Anzahl der registrierten Lizenzen geändert hat
	// (Mods registrieren teils spät) → kein Dauer-Scan.
	internal static class ExtraLicenses
	{
		internal sealed class Entry
		{
			public ScriptableObject License = null!;
			public string Id = "";
			public bool Job;
			public string Key => (Job ? "J:" : "G:") + Id;
			public string Name => LicenseRules.LicenseName(License);
		}

		private static readonly List<Entry> extras = new();
		private static readonly List<Entry> all = new();
		private static readonly Dictionary<string, Entry> byKey = new();
		private static readonly Dictionary<ScriptableObject, Entry> byLicense = new();
		private static readonly HashSet<string> warned = new();
		private static readonly HashSet<string> announced = new();
		private static int lastGeneral = -1, lastJob = -1;

		/// <summary>Erkannte Zusatzlizenzen (sortiert nach Namen).</summary>
		public static IReadOnlyList<Entry> Detected
		{
			get
			{
				Refresh();
				return extras;
			}
		}

		/// <summary>Alle Lizenzen des Spiels (für die Auswahl der Voraussetzung).</summary>
		public static IReadOnlyList<Entry> All
		{
			get
			{
				Refresh();
				return all;
			}
		}

		private static void Refresh()
		{
			var types = Globals.G?.Types;
			if (types == null || types.generalLicenses == null || types.jobLicenses == null)
				return;

			if (types.generalLicenses.Count == lastGeneral && types.jobLicenses.Count == lastJob)
				return;
			lastGeneral = types.generalLicenses.Count;
			lastJob = types.jobLicenses.Count;

			extras.Clear();
			all.Clear();
			byKey.Clear();
			byLicense.Clear();

			var basic = LicenseRules.JL(JobLicenses.Basic);	// Grundlizenz, besitzt jeder

			foreach (var g in types.generalLicenses)
				Add(g, g?.id, false);
			foreach (var j in types.jobLicenses)
				if (j != basic)
					Add(j, j?.id, true);

			extras.Sort((a, b) => string.Compare(a.Name, b.Name, StringComparison.CurrentCultureIgnoreCase));
			all.Sort((a, b) => string.Compare(a.Name, b.Name, StringComparison.CurrentCultureIgnoreCase));

			var fresh = new List<string>();
			foreach (var e in extras)
				if (announced.Add(e.Key))
					fresh.Add(e.Id);
			if (fresh.Count > 0)
				Main.Info("Additional licenses detected: " + string.Join(", ", fresh));
		}

		private static void Add(ScriptableObject? license, string? id, bool job)
		{
			if (license == null || string.IsNullOrEmpty(id))
				return;

			var entry = new Entry { License = license, Id = id!, Job = job };
			if (byKey.ContainsKey(entry.Key))
				return;

			byKey[entry.Key] = entry;
			all.Add(entry);
			if (!LicenseRules.IsManaged(license))
			{
				extras.Add(entry);
				byLicense[license] = entry;
			}
		}

		public static Entry? Find(string? key)
		{
			if (string.IsNullOrEmpty(key))
				return null;
			Refresh();
			return byKey.TryGetValue(key!, out var e) ? e : null;
		}

		public static string KeyOf(ScriptableObject license) => license switch
		{
			JobLicenseType_v2 j => "J:" + j.id,
			GeneralLicenseType_v2 g => "G:" + g.id,
			_ => "",
		};

		/// <summary>Gespeicherte Einträge, deren Lizenz gerade nicht geladen ist (Mod fehlt).</summary>
		public static int MissingCount(CareerReworkSettings s)
		{
			Refresh();
			int n = 0;
			foreach (var x in s.extraLicenses)
				if (!x.IsDefault && !byKey.ContainsKey(x.Key))
					n++;
			return n;
		}

		// =========================
		// ANWENDEN
		// =========================
		// Welche Felder CareerRework bei einer Lizenz gerade selbst gesetzt hat. Felder, die auf "unverändert" stehen,
		// werden NICHT angefasst – so bleibt die eigene Preislogik der anderen Mod (z. B. Preis aus deren Settings)
		// unangetastet. Nur wenn ein Feld vorher von uns gesetzt war und jetzt wieder "unverändert" ist, wird der
		// gemerkte Ursprungswert einmal zurückgeschrieben.
		[Flags]
		private enum Touched : byte { None = 0, Price = 1, Copay = 2, Bonus = 4, Requirement = 8 }
		private static readonly Dictionary<ScriptableObject, Touched> touched = new();

		public static void Apply(CareerReworkSettings e)
		{
			Refresh();
			foreach (var entry in extras)
				ApplyEntry(entry, e);
		}

		/// <summary>Nur eine Lizenz neu setzen (direkt bevor das Spiel ihre Werte liest).</summary>
		public static void ApplyOne(ScriptableObject? license, CareerReworkSettings e)
		{
			if (license == null)
				return;
			Refresh();
			if (byLicense.TryGetValue(license, out var entry))
				ApplyEntry(entry, e);
		}

		/// <summary>Setzt CareerRework gerade den Preis dieser Zusatzlizenz?</summary>
		public static bool ControlsPrice(ScriptableObject license) =>
			touched.TryGetValue(license, out var t) && (t & Touched.Price) != 0;

		private static void ApplyEntry(Entry entry, CareerReworkSettings e)
		{
			var license = entry.License;
			var x = e.FindExtra(entry.Id, entry.Job);
			touched.TryGetValue(license, out var t);
			if (x == null && t == Touched.None)
				return;		// nichts eingestellt, nie angefasst → Lizenz gehört komplett der anderen Mod

			var o = LicenseRules.GetOriginal(license);
			float price = LicenseRules.GetPrice(license);
			float copay = LicenseRules.GetCopay(license);
			float bonus = LicenseRules.GetBonus(license);

			if (x != null && x.price > 0) { price = x.price; t |= Touched.Price; }
			else if ((t & Touched.Price) != 0) { price = o.Price; t &= ~Touched.Price; }

			if (x != null && x.copay > 0) { copay = x.copay; t |= Touched.Copay; }
			else if ((t & Touched.Copay) != 0) { copay = o.Copay; t &= ~Touched.Copay; }

			if (x != null && Mathf.Abs(x.bonus) > 0.001f) { bonus = -x.bonus / 100f; t |= Touched.Bonus; }
			else if ((t & Touched.Bonus) != 0) { bonus = o.Bonus; t &= ~Touched.Bonus; }

			LicenseRules.SetValues(license, price, copay, bonus);

			var req = ResolveRequirement(entry, x, e, out bool useOriginal);
			if (!useOriginal)
			{
				LicenseRules.SetRequirement(license, req as GeneralLicenseType_v2, req as JobLicenseType_v2);
				t |= Touched.Requirement;
			}
			else if ((t & Touched.Requirement) != 0)
			{
				LicenseRules.SetRequirement(license, o.ReqGeneral, o.ReqJob);
				t &= ~Touched.Requirement;
			}

			if (t == Touched.None) touched.Remove(license); else touched[license] = t;
		}

		// =========================
		// NEUE LIZENZEN IM SAVEGAME
		// =========================
		/// <summary>Schlüssel aller gerade erkannten Zusatzlizenzen.</summary>
		public static List<string> DetectedKeys()
		{
			Refresh();
			var keys = new List<string>(extras.Count);
			foreach (var x in extras)
				keys.Add(x.Key);
			return keys;
		}

		/// <summary>
		/// Lizenzen, die der Spielstand noch nicht kennt (z. B. neue Mod installiert), übernehmen einmalig die
		/// Einstellungen aus dem Menü und gelten danach als bekannt – ab dann sind sie im Savegame festgeschrieben.
		/// </summary>
		public static List<string> AdoptNew(CareerReworkSettings save, CareerReworkSettings local)
		{
			Refresh();
			save.knownLicenses ??= new List<string>();
			var known = new HashSet<string>(save.knownLicenses);
			var adopted = new List<string>();

			foreach (var entry in extras)
			{
				if (known.Contains(entry.Key))
					continue;

				save.extraLicenses.RemoveAll(x => x != null && x.Key == entry.Key);
				var lx = local.FindExtra(entry.Id, entry.Job);
				if (lx != null && !lx.IsDefault)
					save.extraLicenses.Add(lx.Clone());

				save.knownLicenses.Add(entry.Key);
				adopted.Add(entry.Name + " (" + entry.Id + ")");
			}
			return adopted;
		}

		private static ScriptableObject? ResolveRequirement(Entry entry, ExtraLicense? x, CareerReworkSettings e, out bool useOriginal)
		{
			useOriginal = true;
			if (x == null || string.IsNullOrEmpty(x.required))
				return null;

			if (x.required == ExtraLicense.ReqNone)
			{
				useOriginal = false;
				return null;
			}

			var target = Find(x.required);
			if (target == null)
			{
				WarnOnce(entry.Key + ">" + x.required, $"Requirement {x.required} for {entry.Id} is not loaded → keeping the original requirement.");
				return null;
			}

			if (CreatesCycle(entry.License, target.License, e))
			{
				WarnOnce(entry.Key + ">" + x.required, $"Requirement {x.required} for {entry.Id} would create a loop → keeping the original requirement.");
				return null;
			}

			useOriginal = false;
			return target.License;
		}

		private static void WarnOnce(string key, string msg)
		{
			if (warned.Add(key))
				Main.Warn(msg);
		}

		// =========================
		// ZYKLUSPRÜFUNG
		// =========================
		/// <summary>Würde "license benötigt candidate" einen Kreis ergeben (inkl. Selbstbezug)?</summary>
		public static bool CreatesCycle(ScriptableObject license, ScriptableObject candidate, CareerReworkSettings s)
		{
			if (ReferenceEquals(license, candidate))
				return true;

			var visited = new HashSet<ScriptableObject>();
			var stack = new Stack<ScriptableObject>();
			stack.Push(candidate);

			while (stack.Count > 0)
			{
				var cur = stack.Pop();
				if (!visited.Add(cur))
					continue;
				if (ReferenceEquals(cur, license))
					return true;
				foreach (var next in Requirements(cur, s))
					if (next != null && !visited.Contains(next))
						stack.Push(next);
			}
			return false;
		}

		/// <summary>Effektive Voraussetzungen einer Lizenz mit den Einstellungen s.</summary>
		private static IEnumerable<ScriptableObject> Requirements(ScriptableObject license, CareerReworkSettings s)
		{
			if (LicenseRules.IsManaged(license))
			{
				foreach (var r in LicenseRules.ManagedRequirements(license))
					yield return r;
				yield break;
			}

			string key = KeyOf(license);
			ExtraLicense? x = null;
			foreach (var e in s.extraLicenses)
				if (e.Key == key)
				{
					x = e;
					break;
				}

			if (x != null && x.required == ExtraLicense.ReqNone)
				yield break;

			if (x != null && !string.IsNullOrEmpty(x.required))
			{
				var t = Find(x.required);
				if (t != null)
				{
					yield return t.License;
					yield break;
				}
			}

			var o = LicenseRules.GetOriginal(license);
			if (o.ReqGeneral != null) yield return o.ReqGeneral;
			if (o.ReqJob != null) yield return o.ReqJob;
		}

		/// <summary>Name der Originalvoraussetzung (für den Tooltip).</summary>
		public static string OriginalRequirementName(ScriptableObject license)
		{
			var o = LicenseRules.GetOriginal(license);
			var names = new List<string>();
			if (o.ReqGeneral != null) names.Add(LicenseRules.LicenseName(o.ReqGeneral));
			if (o.ReqJob != null) names.Add(LicenseRules.LicenseName(o.ReqJob));
			return names.Count > 0 ? string.Join(", ", names) : "–";
		}
	}
}
