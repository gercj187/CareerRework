//FILE:		LicenseList.cs

using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using DV.ServicePenalty.UI;
using DV.ThingTypes;
using HarmonyLib;
using UnityEngine;

namespace CareerRework
{
	// =========================
	// LIZENZLISTE IM CAREERMANAGER
	// =========================
	// 1) Neue Reihenfolge (immer):
	//      Lokführer, Rangieren, Leerzug, Güterzug, Zuglänge 1/2, Gleichzeitige Aufträge 1/2,
	//      Mehrfachtraktion, Manuelle Wartung, Museum, Fahrdienstleiter
	//      Mod-General-Lizenzen
	//      Fragil, Gefahrgut 1–3, Militär 1–3
	//      Mod-Job-Lizenzen (z. B. PassengerJobs)
	//      Original-Loks   S060, DM3, DE2, DH4, S282, DE6
	//      Mod-Loks
	//    Innerhalb einer Gruppe bleibt die bisherige Reihenfolge (auch die anderer Mods) erhalten.
	// 2) Versteckte Progression (Komfort-Option): nur besessene und gerade kaufbare Lizenzen.
	//
	// Das Spiel liest die Liste aus dem Feld licensesDisplayOrder (andere Mods hängen ihre Lizenzen dort an)
	// und die Zeilenzahl aus LicenseManager.AllLicensesCount. Beides wird beim Öffnen der Liste ersetzt –
	// nur dann, kein Overhead im Betrieb.
	[HarmonyPatch]
	internal static class LicenseList
	{
		private static readonly FieldInfo? orderField = AccessTools.Field(typeof(CareerManagerLicensesScreen), "licensesDisplayOrder");
		private static readonly FieldInfo? mainScreenField = AccessTools.Field(typeof(CareerManagerLicensesScreen), "mainScreen");
		private static readonly FieldInfo? selectorField = AccessTools.Field(typeof(ScrollableDisplayScreen), "selector");
		private static readonly FieldInfo? slotCountField = AccessTools.Field(typeof(ScrollableDisplayScreen), "activeSlotCount");
		private static readonly FieldInfo? entriesField = AccessTools.Field(typeof(CareerManagerLicensesScreen), "licenseEntries");
		private static FieldInfo? generalField, jobField, entryNameField, entryStatusField;

		private sealed class State
		{
			public readonly List<object> Master = new();			// alle Einträge in Ursprungsreihenfolge
			public readonly HashSet<object> Seen = new();
			public List<object>? Sorted;
			public int ShownCount = -1;
		}

		// eine Liste pro CareerManager (jede Station hat einen)
		private static readonly Dictionary<CareerManagerLicensesScreen, State> states = new();
		private static Dictionary<ScriptableObject, float>? ranks;

		// =========================
		// ÖFFNEN: Liste neu aufbauen
		// =========================
		[HarmonyPatch(typeof(CareerManagerLicensesScreen), nameof(CareerManagerLicensesScreen.Activate))]
		[HarmonyPrefix]
		private static void ActivatePrefix(CareerManagerLicensesScreen __instance, ref IDisplayScreen __0)
		{
			try
			{
				if (orderField == null || !(orderField.GetValue(__instance) is IList current))
					return;

				if (!states.TryGetValue(__instance, out var st))
				{
					CleanUp();
					st = new State();
					states[__instance] = st;
				}

				// neue Einträge (z. B. von anderen Mods nachträglich angehängt) übernehmen
				bool grown = false;
				foreach (var entry in current)
					if (entry != null && st.Seen.Add(entry))
					{
						st.Master.Add(entry);
						grown = true;
					}
				if (grown || st.Sorted == null)
					st.Sorted = Sort(st.Master);

				// Liste zusammenstellen (gleicher Listentyp wie im Spiel)
				var shown = (IList)Activator.CreateInstance(current.GetType());
				var lm = LicenseManager.Instance;
				bool hidden = Main.HiddenProgression && lm != null;
				foreach (var entry in st.Sorted)
					if (!hidden || Visible(entry, lm!))
						shown.Add(entry);
				orderField.SetValue(__instance, shown);

				// Auswahlbalken nur über belegte Zeilen (sonst läuft er in leere Zeilen weiter)
				int slots = slotCountField?.GetValue(__instance) is int n ? n : 0;
				if (slots > 0 && selectorField?.GetValue(__instance) is IntIterator selector)
					selector.UpdateLength(Mathf.Max(1, Mathf.Min(slots, shown.Count)));

				// Zeilenzahl geändert (z. B. nach einem Kauf neue Lizenzen sichtbar) → Auswahl wie beim Öffnen zurücksetzen
				if (st.ShownCount != -1 && st.ShownCount != shown.Count && mainScreenField?.GetValue(__instance) is IDisplayScreen main)
					__0 = main;
				st.ShownCount = shown.Count;
			}
			catch (Exception e)
			{
				Main.Warn("License list could not be rebuilt: " + e.Message);
			}
		}

		// Leere Zeilen (weniger Lizenzen als Bildschirmzeilen) leeren – das Spiel überschreibt nur belegte Zeilen
		[HarmonyPatch(typeof(CareerManagerLicensesScreen), nameof(CareerManagerLicensesScreen.PopulateTextsFromIndex))]
		[HarmonyPostfix]
		private static void PopulatePostfix(CareerManagerLicensesScreen __instance, int startingIndex)
		{
			try
			{
				if (!states.ContainsKey(__instance) || !(orderField?.GetValue(__instance) is IList list) || !(entriesField?.GetValue(__instance) is IList entries))
					return;
				int slots = slotCountField?.GetValue(__instance) is int n ? n : entries.Count;
				int filled = Mathf.Max(0, list.Count - startingIndex);
				for (int i = filled; i < slots && i < entries.Count; i++)
				{
					var e = entries[i];
					if (e == null)
						continue;
					if (entryNameField == null || entryStatusField == null)
					{
						entryNameField = AccessTools.Field(e.GetType(), "name");
						entryStatusField = AccessTools.Field(e.GetType(), "status");
					}
					if (entryNameField?.GetValue(e) is TMPro.TMP_Text name) name.text = string.Empty;
					if (entryStatusField?.GetValue(e) is TMPro.TMP_Text status) status.text = string.Empty;
				}
			}
			catch (Exception e)
			{
				Main.Log("License list empty rows: " + e.Message);
			}
		}

		// Zeilenzahl = unsere Liste (Vanilla: alle Lizenzen)
		[HarmonyPatch(typeof(CareerManagerLicensesScreen), "get_TotalSlotCount")]
		[HarmonyPostfix]
		private static void TotalSlotCountPostfix(CareerManagerLicensesScreen __instance, ref int __result)
		{
			if (states.ContainsKey(__instance) && orderField?.GetValue(__instance) is IList list)
				__result = list.Count;
		}

		// =========================
		// SICHTBAR (versteckte Progression)
		// =========================
		private static bool Visible(object entry, LicenseManager lm)
		{
			Resolve(entry, out var g, out var j);
			if (g != null)
				return lm.IsGeneralLicenseAcquired(g) || lm.IsGeneralLicenseObtainable(g);
			if (j != null)
				return lm.IsJobLicenseAcquired(j) || lm.IsJobLicenseObtainable(j);
			return true;
		}

		// =========================
		// SORTIEREN
		// =========================
		private const float ModGeneral = 211.5f;		// direkt nach Fahrdienstleiter
		private const float ModJob = 300f;
		private const float Loco = 400f;				// Loks am Schluss
		private const float ModLoco = 500f;

		private static List<object> Sort(List<object> master)
		{
			var keyed = new List<(float rank, int index, object entry)>(master.Count);
			for (int i = 0; i < master.Count; i++)
				keyed.Add((Rank(master[i]), i, master[i]));
			keyed.Sort((a, b) => a.rank != b.rank ? a.rank.CompareTo(b.rank) : a.index.CompareTo(b.index));

			var result = new List<object>(keyed.Count);
			foreach (var k in keyed)
				result.Add(k.entry);
			return result;
		}

		private static float Rank(object entry)
		{
			Resolve(entry, out var g, out var j);
			var map = Ranks();
			if (g != null)
			{
				if (map.TryGetValue(g, out float r)) return r;
				return LicenseRules.IsLocoLicense(g) ? ModLoco : ModGeneral;
			}
			if (j != null)
			{
				if (map.TryGetValue(j, out float r)) return r;
				return ModJob;
			}
			return ModLoco + 1f;
		}

		private static Dictionary<ScriptableObject, float> Ranks()
		{
			if (ranks != null)
				return ranks;

			var map = new Dictionary<ScriptableObject, float>();
			ranks = map;
			void Add(ScriptableObject? l, float r)
			{
				if (l != null && !map.ContainsKey(l))
					map[l] = r;
			}

			// Original-Loks
			Add(LicenseRules.GL(GeneralLicenseType.S060), Loco + 0);
			Add(LicenseRules.GL(GeneralLicenseType.DM3), Loco + 1);
			Add(LicenseRules.GL(GeneralLicenseType.DE2), Loco + 2);
			Add(LicenseRules.GL(GeneralLicenseType.DH4), Loco + 3);
			Add(LicenseRules.GL(GeneralLicenseType.SH282), Loco + 4);
			Add(LicenseRules.GL(GeneralLicenseType.DE6), Loco + 5);

			// Original-Lizenzen
			int i = 200;
			Add(LicenseRules.GL(GeneralLicenseType.TrainDriver), i++);
			Add(LicenseRules.JL(JobLicenses.Shunting), i++);
			Add(LicenseRules.JL(JobLicenses.LogisticalHaul), i++);
			Add(LicenseRules.JL(JobLicenses.FreightHaul), i++);
			Add(LicenseRules.JL(JobLicenses.TrainLength1), i++);
			Add(LicenseRules.JL(JobLicenses.TrainLength2), i++);
			Add(LicenseRules.GL(GeneralLicenseType.ConcurrentJobs1), i++);
			Add(LicenseRules.GL(GeneralLicenseType.ConcurrentJobs2), i++);
			Add(LicenseRules.GL(GeneralLicenseType.MultipleUnit), i++);
			Add(LicenseRules.GL(GeneralLicenseType.ManualService), i++);
			Add(LicenseRules.GL(GeneralLicenseType.MuseumCitySouth), i++);
			Add(LicenseRules.GL(GeneralLicenseType.Dispatcher1), i++);		// 211 → danach Mod-General (211.5)
			i = 212;
			Add(LicenseRules.JL(JobLicenses.Fragile), i++);
			Add(LicenseRules.JL(JobLicenses.Hazmat1), i++);
			Add(LicenseRules.JL(JobLicenses.Hazmat2), i++);
			Add(LicenseRules.JL(JobLicenses.Hazmat3), i++);
			Add(LicenseRules.JL(JobLicenses.Military1), i++);
			Add(LicenseRules.JL(JobLicenses.Military2), i++);
			Add(LicenseRules.JL(JobLicenses.Military3), i++);
			return map;
		}

		// =========================
		// HILFEN
		// =========================
		private static void Resolve(object entry, out GeneralLicenseType_v2? g, out JobLicenseType_v2? j)
		{
			if (generalField == null || jobField == null)
			{
				var t = entry.GetType();
				generalField = AccessTools.Field(t, "generalLicense");
				jobField = AccessTools.Field(t, "jobLicense");
			}
			g = generalField?.GetValue(entry) as GeneralLicenseType_v2;
			j = jobField?.GetValue(entry) as JobLicenseType_v2;
			if (g == null) g = null;		// Unity-"null" (zerstörtes Objekt) normalisieren
			if (j == null) j = null;
		}

		private static void CleanUp()
		{
			// CareerManager aus entladenen Szenen vergessen
			List<CareerManagerLicensesScreen>? dead = null;
			foreach (var k in states.Keys)
				if (k == null)
					(dead ??= new()).Add(k!);		// zerstörtes Unity-Objekt (== null), der Schlüssel selbst existiert
			if (dead != null)
				foreach (var k in dead)
					states.Remove(k);
		}

		/// <summary>Hauptmenü: alles vergessen.</summary>
		internal static void Reset() => states.Clear();
	}
}