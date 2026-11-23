//FILE:		ShopPrices.cs

using System;
using System.Collections.Generic;
using System.Reflection;
using DV.Shops;
using HarmonyLib;
using UnityEngine;

namespace CareerRework
{
	// =========================
	// LADENPREISE
	// =========================
	// • Schlüssel & Gadgets (ScanItemCashRegisterModule): Preset = feste Preise, Custom = Vanilla × Multiplikator
	// • Restauration (GenericThingCashRegisterModule "restoration/…" und "cargo/tp_…"): Vanilla × Multiplikator
	//
	// Module werden beim InitializeData gemerkt → beim Wechsel der Einstellungen (Savegame geladen, Host-Werte
	// angekommen) ohne FindObjectsOfType neu berechnet.
	internal static class ShopPrices
	{
		private static readonly HashSet<ScanItemCashRegisterModule> scanModules = new();
		private static readonly HashSet<GenericThingCashRegisterModule> genericModules = new();

		// Preset: feste Preise für Schlüssel & Gadgets
		private static readonly Dictionary<string, float> presetPrices = new(StringComparer.Ordinal)
		{
			// Schlüssel
			["Key"] = 170000f,
			["KeyDM1U"] = 180000f,
			["KeyDE6Slug"] = 70000f,
			["KeyCaboose"] = 40000f,
			// Gadgets
			["ProximityReader"] = 120000f,
			["OverheatingProtection"] = 200000f,
			["UniversalControlStand"] = 300000f,
			["ProximitySensor"] = 85000f,
			["SwitchSetter"] = 35000f,
			["RemoteSignalBooster"] = 180000f,
			["DefectDetector"] = 80000f,
			["AntiWheelslipComputer"] = 280000f,
			["DistanceTracker"] = 120000f,
			["AutomaticTrainStop"] = 40000f,
			["RemoteController"] = 350000f,
			["WirelessMUController"] = 240000f,
			["BatteryCharger"] = 150000f,
			["AmpLimiter"] = 180000f,
		};

		public static bool IsKeyOrGadget(string id) => presetPrices.ContainsKey(id);

		public static bool IsRestoration(string? key) =>
			!string.IsNullOrEmpty(key) &&
			(key!.StartsWith("restoration", StringComparison.OrdinalIgnoreCase) ||
			 key.StartsWith("cargo/tp_", StringComparison.OrdinalIgnoreCase));

		// =========================
		// SCHLÜSSEL & GADGETS
		// =========================
		/// <summary>Aufruf direkt nach dem Vanilla-InitializeData (Data.pricePerUnit = Vanilla-Preis).</summary>
		public static void ApplyScan(ScanItemCashRegisterModule module)
		{
			var spec = module.sellingItemSpec;
			var data = module.Data;
			if (spec == null || data == null)
				return;

			string id = spec.name;
			if (!presetPrices.TryGetValue(id, out float presetPrice))
				return;

			scanModules.Add(module);

			var e = Main.E;
			data.pricePerUnit = e.startupMode == StartupMode.Preset
				? presetPrice
				: data.pricePerUnit * e.priceMultiplierKeysGadgets;
		}

		// =========================
		// RESTAURATION
		// =========================
		/// <summary>module.price ist der unveränderte Vanilla-Preis (wird vom Spiel nur im Prefab gesetzt).</summary>
		public static void ApplyGeneric(GenericThingCashRegisterModule module)
		{
			if (!IsRestoration(module.localizationKey))
				return;

			genericModules.Add(module);

			var data = module.Data;
			if (data != null)
				data.pricePerUnit = module.price * Main.E.priceMultiplierRestoration;
		}

		// =========================
		// NEU BERECHNEN (Einstellungen haben sich geändert)
		// =========================
		private static MethodInfo? onShopDataChanged;

		public static void ReapplyAll()
		{
			if (scanModules.Count > 0)
			{
				onShopDataChanged ??= AccessTools.Method(typeof(ScanItemCashRegisterModule), "OnGlobalShopDataChanged");
				scanModules.RemoveWhere(m => m == null);
				foreach (var m in new List<ScanItemCashRegisterModule>(scanModules))
				{
					try
					{
						// liest den Vanilla-Preis neu ein (→ InitializeData → unser Postfix) und aktualisiert die Texte
						onShopDataChanged?.Invoke(m, null);
					}
					catch (Exception e)
					{
						Main.Log("Shop price refresh failed: " + e.Message);
					}
				}
			}

			if (genericModules.Count > 0)
			{
				genericModules.RemoveWhere(m => m == null);
				foreach (var m in new List<GenericThingCashRegisterModule>(genericModules))
					ApplyGeneric(m);
			}
		}

		public static void Clear()
		{
			scanModules.Clear();
			genericModules.Clear();
		}
	}

	[HarmonyPatch(typeof(ScanItemCashRegisterModule), "InitializeData")]
	internal static class Patch_ScanItemCashRegisterModule_InitializeData
	{
		private static void Postfix(ScanItemCashRegisterModule __instance)
		{
			ShopPrices.ApplyScan(__instance);
		}
	}

	[HarmonyPatch(typeof(GenericThingCashRegisterModule), "InitializeData")]
	internal static class Patch_GenericThingCashRegisterModule_InitializeData
	{
		private static void Postfix(GenericThingCashRegisterModule __instance)
		{
			ShopPrices.ApplyGeneric(__instance);
		}
	}
}
