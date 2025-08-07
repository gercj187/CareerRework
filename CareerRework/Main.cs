// Datei: Main.cs
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using DV.ThingTypes;
using DV.Shops;
using UnityModManagerNet;
using UnityEngine;
using TMPro;
using static UnityEngine.GUILayout;

namespace CareerRework
{
    public static class Main
    {
        public static UnityModManager.ModEntry? Mod;
        public static CareerReworkSettings? settings;

        public static bool Load(UnityModManager.ModEntry modEntry)
        {
            Mod = modEntry;
            settings = UnityModManager.ModSettings.Load<CareerReworkSettings>(modEntry);

            modEntry.OnGUI = OnGUI;
            modEntry.OnSaveGUI = OnSaveGUI;
			
			var switcherGO = new GameObject("LicenseConditionSwitcher");
			switcherGO.AddComponent<CareerRework.LicenseConditionSwitcher>();
			UnityEngine.Object.DontDestroyOnLoad(switcherGO);
	
            var harmony = new Harmony(modEntry.Info.Id);
            harmony.PatchAll();
            modEntry.Logger.Log("[CareerRework] Harmony patches applied.");
            return true;
        }
		
		static Vector2 scrollPosition;
        static void OnGUI(UnityModManager.ModEntry modEntry)
        {
            if (settings == null) return;

            Label("Select Startup Mode:");
            if (Toggle(settings.startupMode == StartupMode.Preset, "Play my CareerRework-Preset", ExpandWidth(false)))
                settings.startupMode = StartupMode.Preset;
            if (Toggle(settings.startupMode == StartupMode.Custom, "Customize your own CareerRework", ExpandWidth(false)))
                settings.startupMode = StartupMode.Custom;

            Space(10);

            if (settings.startupMode == StartupMode.Custom)
            {			
                Label($"Starting Money: {settings.startingMoney:N0} $");
                settings.startingMoney = (int)HorizontalSlider(settings.startingMoney, 40000, 500000, GUILayout.Width(400));
				
                Space(10);
				
                Label("Choose optional starter loco:");
                if (Toggle(settings.selectedStarterLoco == StarterLocoType.DM3, "DM3", ExpandWidth(false)))
                    settings.selectedStarterLoco = StarterLocoType.DM3;
                if (Toggle(settings.selectedStarterLoco == StarterLocoType.S060, "S060", ExpandWidth(false)))
                    settings.selectedStarterLoco = StarterLocoType.S060;
				
                Space(10);
				
                Label("Customize license prices:  (0 = Vanilla Prices)");
				scrollPosition = BeginScrollView(scrollPosition, Height(250));
				
				settings.priceTrainDriver = DrawIntField("TrainDriver", settings.priceTrainDriver,0);
				settings.priceShunting = DrawIntField("Shunting", settings.priceShunting,0);
				settings.priceLogisticalHaul = DrawIntField("LogisticalHaul", settings.priceLogisticalHaul,0);
				settings.priceFreightHaul = DrawIntField("FreightHaul", settings.priceFreightHaul,0);
				settings.priceFragile = DrawIntField("Fragile", settings.priceFragile,0);
				settings.priceHazmat1 = DrawIntField("Hazmat1", settings.priceHazmat1,0);
				settings.priceHazmat2 = DrawIntField("Hazmat2", settings.priceHazmat2,0);
				settings.priceHazmat3 = DrawIntField("Hazmat3", settings.priceHazmat3,0);
				settings.priceMilitary1 = DrawIntField("Military1", settings.priceMilitary1,0);
				settings.priceMilitary2 = DrawIntField("Military2", settings.priceMilitary2,0);
				settings.priceMilitary3 = DrawIntField("Military3", settings.priceMilitary3,0);
				settings.priceConcurrentJobs1 = DrawIntField("ConcurrentJobs1", settings.priceConcurrentJobs1,0);
				settings.priceConcurrentJobs2 = DrawIntField("ConcurrentJobs2", settings.priceConcurrentJobs2,0);
				settings.priceTrainLength1 = DrawIntField("TrainLength1", settings.priceTrainLength1,0);
				settings.priceTrainLength2 = DrawIntField("TrainLength2", settings.priceTrainLength2,0);
				settings.priceMultipleUnit = DrawIntField("MultipleUnit", settings.priceMultipleUnit,0);
				settings.priceManualService = DrawIntField("ManualService", settings.priceManualService,0);
				settings.priceMuseum = DrawIntField("Museum", settings.priceMuseum,0);
				settings.priceDispatcher = DrawIntField("Dispatcher", settings.priceDispatcher,0);
				settings.priceDE2 = DrawIntField("DE2", settings.priceDE2,0);
				settings.priceDM3 = DrawIntField("DM3", settings.priceDM3,0);
				settings.priceS060 = DrawIntField("S060", settings.priceS060,0);
				settings.priceDH4 = DrawIntField("DH4", settings.priceDH4,0);
				settings.priceS282 = DrawIntField("S282", settings.priceS282,0);
				settings.priceDE6 = DrawIntField("DE6", settings.priceDE6,0);

				EndScrollView();
				
                Space(10);
				Label($"Gadgetprice-Multiplicator: {settings.priceMultiplierKeysGadgets}x");
				settings.priceMultiplierKeysGadgets = Mathf.RoundToInt(HorizontalSlider(settings.priceMultiplierKeysGadgets, 1, 10, GUILayout.Width(400)));
            }
        }
		
		static int DrawIntField(string label, int currentValue, int minValue = 0)
		{
			BeginHorizontal();

			string input = TextField(currentValue.ToString(), ExpandWidth(false), Width(75));
			Label("$ for " + label, ExpandWidth(false));

			EndHorizontal();

			if (int.TryParse(input, out int value))
				return Mathf.Max(value, minValue);

			return currentValue;
		}

        static void OnSaveGUI(UnityModManager.ModEntry modEntry)
        {
            settings?.Save(modEntry);
        }

        public static TrainCarType GetSelectedStarterLoco()
        {
            if (settings?.startupMode == StartupMode.Preset)
                return TrainCarType.LocoDM3;

            return settings?.selectedStarterLoco == StarterLocoType.S060
                ? TrainCarType.LocoS060
                : TrainCarType.LocoDM3;
        }

        public static float GetStartingMoney()
        {
            if (settings?.startupMode == StartupMode.Preset)
                return 200000;

            return settings?.startingMoney ?? 200000;
        }

        [HarmonyPatch(typeof(ScanItemCashRegisterModule), "InitializeData")]
        public class Patch_ScanItemCashRegisterModule
        {
	        static void Postfix(ScanItemCashRegisterModule __instance)
	        {
	        	var settings = Main.settings;
		        if (settings == null) return;

		        var spec = __instance.sellingItemSpec;
		        if (spec == null) return;

		        string id = spec.name;
		        if (string.IsNullOrEmpty(id)) return;

		        if (settings.startupMode == StartupMode.Preset)
		        {
			        switch (id)
			        {
			        	// Keys
				        case "Key":                  __instance.Data.pricePerUnit = 170000f; break;
				        case "KeyDM1U":              __instance.Data.pricePerUnit = 180000f; break;
				        case "KeyDE6Slug":           __instance.Data.pricePerUnit = 70000f; break;
				        case "KeyCaboose":           __instance.Data.pricePerUnit = 40000f; break;
				        // Gadgets
				        case "ProximityReader":          __instance.Data.pricePerUnit = 120000f; break;
				        case "OverheatingProtection":    __instance.Data.pricePerUnit = 200000f; break;
			        	case "UniversalControlStand":    __instance.Data.pricePerUnit = 300000f; break;
			        	case "ProximitySensor":          __instance.Data.pricePerUnit = 85000f; break;
			        	case "SwitchSetter":             __instance.Data.pricePerUnit = 35000f; break;
			        	case "RemoteSignalBooster":      __instance.Data.pricePerUnit = 180000f; break;
				        case "DefectDetector":           __instance.Data.pricePerUnit = 80000f; break;
				        case "AntiWheelslipComputer":    __instance.Data.pricePerUnit = 280000f; break;
			        	case "DistanceTracker":          __instance.Data.pricePerUnit = 120000f; break;
			        	case "AutomaticTrainStop":       __instance.Data.pricePerUnit = 40000f; break;
			        	case "RemoteController":         __instance.Data.pricePerUnit = 350000f; break;
			        	case "WirelessMUController":     __instance.Data.pricePerUnit = 240000f; break;
			        	case "BatteryCharger":           __instance.Data.pricePerUnit = 150000f; break;
			        	case "AmpLimiter":               __instance.Data.pricePerUnit = 180000f; break;
			        }
		        }
		        else if (settings.startupMode == StartupMode.Custom)
	        	{
		        	float multiplier = settings.priceMultiplierKeysGadgets;

			        switch (id)
			        {
			        	// Keys
			        	case "Key":
			        	case "KeyDM1U":
			        	case "KeyDE6Slug":
			        	case "KeyCaboose":
				        // Gadgets
			        	case "ProximityReader":
			        	case "OverheatingProtection":
			        	case "UniversalControlStand":
			        	case "ProximitySensor":
			        	case "SwitchSetter":
				        case "RemoteSignalBooster":
			        	case "DefectDetector":
			        	case "AntiWheelslipComputer":
			        	case "DistanceTracker":
			        	case "AutomaticTrainStop":
			        	case "RemoteController":
			        	case "WirelessMUController":
			        	case "BatteryCharger":
			        	case "AmpLimiter":
			        		__instance.Data.pricePerUnit *= multiplier;
				        	break;
			        }
	        	}
	        }
        }
    }
}
