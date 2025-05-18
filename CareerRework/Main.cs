using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using DV.ThingTypes;
using UnityModManagerNet;
using UnityEngine;
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
        }

        static void OnSaveGUI(UnityModManager.ModEntry modEntry)
        {
            settings?.Save(modEntry);
        }

        public static TrainCarType GetSelectedStarterLoco()
        {
            if (settings?.selectedStarterLoco == StarterLocoType.S060)
                return TrainCarType.LocoS060;
            return TrainCarType.LocoDM3;
        }
    }
}