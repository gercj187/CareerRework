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

            var harmony = new Harmony(modEntry.Info.Id);
            harmony.PatchAll();
            modEntry.Logger.Log("[CareerRework] Harmony patches applied.");
            return true;
        }

        static void OnGUI(UnityModManager.ModEntry modEntry)
        {
            if (settings == null) return;

            Label("Choose optional starter loco:");
            if (Toggle(settings.selectedStarterLoco == StarterLocoType.DM3, "DM3", ExpandWidth(false)))
                settings.selectedStarterLoco = StarterLocoType.DM3;
            if (Toggle(settings.selectedStarterLoco == StarterLocoType.S060, "S060", ExpandWidth(false)))
                settings.selectedStarterLoco = StarterLocoType.S060;

            Space(10);
            Label($"Starting Money: {settings.startingMoney:N0} $");
            settings.startingMoney = (int)HorizontalSlider(settings.startingMoney, 150000, 500000, ExpandWidth(true));

        }

        static void OnSaveGUI(UnityModManager.ModEntry modEntry)
        {
            settings?.Save(modEntry);
        }

        public static TrainCarType GetSelectedStarterLoco()
        {
            return settings?.selectedStarterLoco == StarterLocoType.S060
                ? TrainCarType.LocoS060
                : TrainCarType.LocoDM3;
        }
		
        [HarmonyPatch(typeof(ScanItemCashRegisterModule), "InitializeData")]
        public class Patch_ScanItemCashRegisterModule
        {
            static void Postfix(ScanItemCashRegisterModule __instance)
            {
                var spec = __instance.sellingItemSpec;
                if (spec == null) return;

                string id = spec.name;
                float original = __instance.Data.pricePerUnit;

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

                    default:
                        break;
                }
			}
        }
    }
}
