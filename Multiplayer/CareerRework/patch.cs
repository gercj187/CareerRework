using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using DV;
using DV.Common;
using DV.Booklets;
using DV.Booklets.Rendered;
using DV.RenderTextureSystem;
using DV.RenderTextureSystem.BookletRender;
using DV.Localization;
using DV.Localization.Debug;
using DV.Scenarios.Common;
using DV.JObjectExtstensions;
using DV.Items;
using DV.ThingTypes;
using DV.ThingTypes.TransitionHelpers;
using DV.UserManagement;
using DV.Utils;
using DV.Player;
using DV.Logic.Job;
using DV.ServicePenalty;
using DV.ServicePenalty.UI;
using HarmonyLib;
using UnityModManagerNet;
using UnityEngine;
using UnityEngine.SceneManagement;
using Newtonsoft.Json.Linq;
using TMPro;
using Debug = UnityEngine.Debug;
using Object = UnityEngine.Object;
using static DV.Items.StartingItems;

namespace CareerRework
{
	
    internal static class CareerReworkSaveState
    {
        private const string SAVE_KEY = "CareerRework_Settings";

        public static bool IsCareerConfigurationLoaded
        {
            get;
            private set;
        }

        public static void SaveTo(SaveGameData saveData)
        {
            if (saveData == null)
                return;

            CareerReworkSettings? settings = Main.settings;

            if (settings == null)
            {
                Debug.LogWarning(
                    "[CareerRework] Cannot save career settings: " +
                    "Main.settings is null.");
                return;
            }

            JObject settingsObject = new JObject();

            settingsObject["Gamemode"] = settings.startupMode.ToString();
            settingsObject["startWithConcurrentJobs1"] = settings.startWithConcurrentJobs1;
            settingsObject["startWithConcurrentJobs2"] =
                settings.startWithConcurrentJobs1 && settings.startWithConcurrentJobs2;

            // Preset intentionally stores only the selected mode.
            if (settings.startupMode == StartupMode.Custom)
            {
                settingsObject["starterLocoPrimary"] = settings.starterLocoPrimary.ToString();
                settingsObject["starterLocoSecondary"] = settings.starterLocoSecondary.ToString();
                settingsObject["startingMoney"] = settings.startingMoney;
                settingsObject["priceMultiplierKeysGadgets"] = settings.priceMultiplierKeysGadgets;
                settingsObject["priceTrainDriver"] = settings.priceTrainDriver;
                settingsObject["priceShunting"] = settings.priceShunting;
                settingsObject["priceLogisticalHaul"] = settings.priceLogisticalHaul;
                settingsObject["priceFreightHaul"] = settings.priceFreightHaul;
                settingsObject["priceFragile"] = settings.priceFragile;
                settingsObject["priceHazmat1"] = settings.priceHazmat1;
                settingsObject["priceHazmat2"] = settings.priceHazmat2;
                settingsObject["priceHazmat3"] = settings.priceHazmat3;
                settingsObject["priceMilitary1"] = settings.priceMilitary1;
                settingsObject["priceMilitary2"] = settings.priceMilitary2;
                settingsObject["priceMilitary3"] = settings.priceMilitary3;
                settingsObject["priceConcurrentJobs1"] = settings.priceConcurrentJobs1;
                settingsObject["priceConcurrentJobs2"] = settings.priceConcurrentJobs2;
                settingsObject["priceTrainLength1"] = settings.priceTrainLength1;
                settingsObject["priceTrainLength2"] = settings.priceTrainLength2;
                settingsObject["priceMultipleUnit"] = settings.priceMultipleUnit;
                settingsObject["priceManualService"] = settings.priceManualService;
                settingsObject["priceMuseum"] = settings.priceMuseum;
                settingsObject["priceDispatcher"] = settings.priceDispatcher;
                settingsObject["priceDE2"] = settings.priceDE2;
                settingsObject["priceDM3"] = settings.priceDM3;
                settingsObject["priceS060"] = settings.priceS060;
                settingsObject["priceDH4"] = settings.priceDH4;
                settingsObject["priceS282"] = settings.priceS282;
                settingsObject["priceDE6"] = settings.priceDE6;
            }

            saveData.SetJObjectArray(SAVE_KEY,new[]{settingsObject});

            IsCareerConfigurationLoaded = true;

            Debug.Log(
                $"[CareerRework] Career settings saved. " +
                $"Gamemode={settings.startupMode}");
        }

        public static bool LoadFrom(SaveGameData saveData)
        {
            if (saveData == null || Main.settings == null)
            {
                return false;
            }

            JObject[] savedSettings;

            try
            {
                savedSettings = saveData.GetJObjectArray(SAVE_KEY);
            }
            catch (Exception exception)
            {
                Debug.LogWarning("[CareerRework] Could not read saved settings: " + exception.Message);

                return false;
            }

            if (savedSettings == null ||
                savedSettings.Length == 0 ||
                savedSettings[0] == null)
            {
                Debug.Log(
                    "[CareerRework] No CareerRework_Settings found " +
                    "in this savegame.");

                return false;
            }

            JObject settingsObject = savedSettings[0];

            string gamemode = ReadString(settingsObject,"Gamemode",Main.settings.startupMode.ToString());

            if (!Enum.TryParse(gamemode,true,out StartupMode startupMode))
            {
                Debug.LogWarning(
                    $"[CareerRework] Unknown saved Gamemode " +
                    $"'{gamemode}'. Keeping current setting.");

                startupMode = Main.settings.startupMode;
            }

            Main.settings.startupMode = startupMode;
            Main.settings.startWithConcurrentJobs1 = ReadBool(
                settingsObject,
                "startWithConcurrentJobs1",
                Main.settings.startWithConcurrentJobs1);
            Main.settings.startWithConcurrentJobs2 =
                Main.settings.startWithConcurrentJobs1 && ReadBool(
                    settingsObject,
                    "startWithConcurrentJobs2",
                    Main.settings.startWithConcurrentJobs2);

            if (startupMode == StartupMode.Custom)
            {
                Main.settings.starterLocoPrimary = ReadEnum(settingsObject,"starterLocoPrimary",Main.settings.starterLocoPrimary);
                Main.settings.starterLocoSecondary = ReadEnum(settingsObject,"starterLocoSecondary",Main.settings.starterLocoSecondary);
                Main.settings.startingMoney = ReadInt(settingsObject,"startingMoney",Main.settings.startingMoney,0);
                Main.settings.priceMultiplierKeysGadgets = ReadInt(settingsObject,"priceMultiplierKeysGadgets",Main.settings.priceMultiplierKeysGadgets,1,10);
                Main.settings.priceTrainDriver = ReadPrice(settingsObject,"priceTrainDriver",Main.settings.priceTrainDriver);
                Main.settings.priceShunting = ReadPrice(settingsObject,"priceShunting",Main.settings.priceShunting);
                Main.settings.priceLogisticalHaul = ReadPrice(settingsObject,"priceLogisticalHaul",Main.settings.priceLogisticalHaul);
                Main.settings.priceFreightHaul = ReadPrice(settingsObject,"priceFreightHaul",Main.settings.priceFreightHaul);
                Main.settings.priceFragile = ReadPrice(settingsObject,"priceFragile",Main.settings.priceFragile);
                Main.settings.priceHazmat1 = ReadPrice(settingsObject,"priceHazmat1",Main.settings.priceHazmat1);
                Main.settings.priceHazmat2 = ReadPrice(settingsObject,"priceHazmat2",Main.settings.priceHazmat2);
                Main.settings.priceHazmat3 = ReadPrice(settingsObject,"priceHazmat3",Main.settings.priceHazmat3);
                Main.settings.priceMilitary1 = ReadPrice(settingsObject,"priceMilitary1",Main.settings.priceMilitary1);
                Main.settings.priceMilitary2 = ReadPrice(settingsObject,"priceMilitary2",Main.settings.priceMilitary2);
                Main.settings.priceMilitary3 = ReadPrice(settingsObject,"priceMilitary3",Main.settings.priceMilitary3);
                Main.settings.priceConcurrentJobs1 = ReadPrice(settingsObject,"priceConcurrentJobs1",Main.settings.priceConcurrentJobs1);
                Main.settings.priceConcurrentJobs2 = ReadPrice(settingsObject,"priceConcurrentJobs2",Main.settings.priceConcurrentJobs2);
                Main.settings.priceTrainLength1 = ReadPrice(settingsObject,"priceTrainLength1",Main.settings.priceTrainLength1);
                Main.settings.priceTrainLength2 = ReadPrice(settingsObject,"priceTrainLength2",Main.settings.priceTrainLength2);
                Main.settings.priceMultipleUnit = ReadPrice(settingsObject,"priceMultipleUnit",Main.settings.priceMultipleUnit);
                Main.settings.priceManualService = ReadPrice(settingsObject,"priceManualService",Main.settings.priceManualService);
                Main.settings.priceMuseum = ReadPrice(settingsObject,"priceMuseum",Main.settings.priceMuseum);
                Main.settings.priceDispatcher = ReadPrice(settingsObject,"priceDispatcher",Main.settings.priceDispatcher);
                Main.settings.priceDE2 = ReadPrice(settingsObject,"priceDE2",Main.settings.priceDE2);
                Main.settings.priceDM3 = ReadPrice(settingsObject,"priceDM3",Main.settings.priceDM3);
                Main.settings.priceS060 = ReadPrice(settingsObject,"priceS060",Main.settings.priceS060);
                Main.settings.priceDH4 = ReadPrice(settingsObject,"priceDH4",Main.settings.priceDH4);
                Main.settings.priceS282 = ReadPrice(settingsObject,"priceS282",Main.settings.priceS282);
                Main.settings.priceDE6 = ReadPrice(settingsObject,"priceDE6",Main.settings.priceDE6);
            }

            IsCareerConfigurationLoaded = true;

            Debug.Log(
                $"[CareerRework] Career settings loaded from savegame. " +
                $"Gamemode={Main.settings.startupMode}");

            return true;
        }

        private static string ReadString(JObject source, string key, string fallback)
		{
			JToken? token = source[key];

			if (token == null || token.Type == JTokenType.Null)
			{
				return fallback;
			}

			string? value = token.Value<string>();

			// Explicit null check required for older Unity/.NET nullable annotations.
			if (value == null)
				return fallback;

			if (value.Trim().Length == 0)
				return fallback;

			return value;
		}

        private static bool ReadBool(JObject source, string key, bool fallback)
        {
            JToken? token = source[key];

            if (token == null || token.Type == JTokenType.Null)
                return fallback;

            try
            {
                return token.Value<bool>();
            }
            catch
            {
                return fallback;
            }
        }

        public static void AddConfiguredStartingConcurrentLicensesToSave(SaveGameData saveData)
        {
            if (saveData == null || Main.settings == null)
                return;

            bool grantConcurrentJobs1 = Main.settings.startWithConcurrentJobs1;
            bool grantConcurrentJobs2 =
                grantConcurrentJobs1 && Main.settings.startWithConcurrentJobs2;

            if (grantConcurrentJobs1)
            {
                GeneralLicenseType_v2 license =
                    GeneralLicenseType.ConcurrentJobs1.ToV2();

                saveData.AddToStringArray(
                    "Licenses_General",
                    license.id,
                    enforceUnique: true);

                SingletonBehaviour<UnlockablesManager>.Instance?.UnlockThing(license);
            }

            if (grantConcurrentJobs2)
            {
                GeneralLicenseType_v2 license =
                    GeneralLicenseType.ConcurrentJobs2.ToV2();

                saveData.AddToStringArray(
                    "Licenses_General",
                    license.id,
                    enforceUnique: true);

                SingletonBehaviour<UnlockablesManager>.Instance?.UnlockThing(license);
            }

            Debug.Log(
                $"[CareerRework] Starting concurrent licenses written: " +
                $"CJ1={grantConcurrentJobs1}, CJ2={grantConcurrentJobs2}.");
        }

        public static void ApplyConfiguredStartingConcurrentLicenses(
            LicenseManager manager,
            string source)
        {
            if (manager == null || Main.settings == null)
                return;

            bool grantConcurrentJobs1 = Main.settings.startWithConcurrentJobs1;
            bool grantConcurrentJobs2 =
                grantConcurrentJobs1 && Main.settings.startWithConcurrentJobs2;

            if (grantConcurrentJobs1 &&
                !manager.IsGeneralLicenseAcquired(
                    GeneralLicenseType.ConcurrentJobs1.ToV2()))
            {
                manager.AcquireGeneralLicense(
                    GeneralLicenseType.ConcurrentJobs1.ToV2());
            }

            if (grantConcurrentJobs2 &&
                !manager.IsGeneralLicenseAcquired(
                    GeneralLicenseType.ConcurrentJobs2.ToV2()))
            {
                manager.AcquireGeneralLicense(
                    GeneralLicenseType.ConcurrentJobs2.ToV2());
            }

            Debug.Log(
                $"[CareerRework] Starting concurrent licenses applied from {source}: " +
                $"CJ1={grantConcurrentJobs1}, CJ2={grantConcurrentJobs2}.");
        }

        private static int ReadPrice(JObject source,string key,int fallback)
        {
            return ReadInt(source,key,fallback,0);
        }

        private static int ReadInt(JObject source,string key,int fallback,int minimum,int maximum = int.MaxValue)
		{
			JToken? token = source[key];

			if (token == null || token.Type == JTokenType.Null)
			{
				return fallback;
			}

			int value;

			try
			{
				value = token.Value<int>();
			}
			catch
			{
				return fallback;
			}

			return Mathf.Clamp(value,minimum,maximum);
		}

        private static TEnum ReadEnum<TEnum>(JObject source,string key,TEnum fallback) where TEnum : struct
        {
            string value = ReadString(source,key,fallback.ToString());

            if (!Enum.TryParse(value,true,out TEnum parsedValue))
            {
                return fallback;
            }

            return parsedValue;
        }
    }
	
    [HarmonyPatch(typeof(LicenseManager), "LoadData")]		
    public static class LicenseManager_LoadData_Patch
    {
        public static bool IsRestoringSave = false;
		
        static bool Prefix(SaveGameData data)
        {
            IsRestoringSave = true;
			
			if (data == null)
			{
				Debug.LogError(
					"[CareerRework] LicenseManager.LoadData received " +
					"null SaveGameData.");

				IsRestoringSave = false;

				return false;
			}

			bool loaded = CareerReworkSaveState.LoadFrom(data);

			if (!loaded)
			{
				CareerReworkSaveState.SaveTo(data);

				Debug.Log(
					"[CareerRework] Existing career migrated to " +
					"CareerRework_Settings save format.");
			}

			Debug.Log(
				"[CareerRework] Loading GeneralLicenses, " +
				"JobLicenses, and Garages...");

            try
            {
                ProcessListOfIDs(data.GetStringArray("Licenses_General"), Globals.G.Types.generalLicenses)
                    .ForEach(l => SingletonBehaviour<LicenseManager>.Instance.AcquireGeneralLicense(l));
                ProcessListOfIDs(data.GetStringArray("Licenses_Jobs"), Globals.G.Types.jobLicenses)
                    .ForEach(l => SingletonBehaviour<LicenseManager>.Instance.AcquireJobLicense(l));
                ProcessListOfIDs(data.GetStringArray("Garages"), Globals.G.Types.garages)
                    .ForEach(g => SingletonBehaviour<LicenseManager>.Instance.UnlockGarage(g));

                CareerReworkSaveState.ApplyConfiguredStartingConcurrentLicenses(
                    SingletonBehaviour<LicenseManager>.Instance,
                    "savegame settings");

                Debug.Log("[CareerRework] All data loaded successfully.");
            }
            finally
            {
                IsRestoringSave = false;
            }

            return false;
        }
		
        static void Postfix(LicenseManager __instance)
        {            
            if (Main.settings == null) return;

            var s = Main.settings;

            if (s.startupMode == StartupMode.Preset)
            {
				// Job License Prices
                JobLicenses.Shunting.ToV2().price = 50000f;
                JobLicenses.LogisticalHaul.ToV2().price = 100000f;
                JobLicenses.FreightHaul.ToV2().price = 200000f;
                JobLicenses.TrainLength1.ToV2().price = 100000f;
                JobLicenses.TrainLength2.ToV2().price = 300000f;
                JobLicenses.Fragile.ToV2().price = 100000f;
                JobLicenses.Hazmat1.ToV2().price = 250000f;
                JobLicenses.Hazmat2.ToV2().price = 500000f;
                JobLicenses.Hazmat3.ToV2().price = 8000000f;
                JobLicenses.Military1.ToV2().price = 1000000f;
                JobLicenses.Military2.ToV2().price = 2000000f;
                JobLicenses.Military3.ToV2().price = 4000000f;
                //JobLicenses.Passenger.ToV2().price = 500000f;
				// Career License Prices
                GeneralLicenseType.TrainDriver.ToV2().price = 50000f;
                GeneralLicenseType.ManualService.ToV2().price = 100000f;
                GeneralLicenseType.ConcurrentJobs1.ToV2().price = 100000f;
                GeneralLicenseType.ConcurrentJobs2.ToV2().price = 300000f;
                GeneralLicenseType.MultipleUnit.ToV2().price = 400000f;
                GeneralLicenseType.Dispatcher1.ToV2().price = 400000f;
                GeneralLicenseType.MuseumCitySouth.ToV2().price = 200000f;
				// Loco License Prices
                GeneralLicenseType.DE2.ToV2().price = 75000f;
                GeneralLicenseType.DM3.ToV2().price = 75000f;
                GeneralLicenseType.S060.ToV2().price = 75000f;
                GeneralLicenseType.DH4.ToV2().price = 500000f;
                GeneralLicenseType.SH282.ToV2().price = 750000f;
                GeneralLicenseType.DE6.ToV2().price = 1000000f;
            }
            else if (s.startupMode == StartupMode.Custom)
            {
				// Job License Prices
				if (s.priceShunting > 0) JobLicenses.Shunting.ToV2().price = s.priceShunting;
				if (s.priceLogisticalHaul > 0) JobLicenses.LogisticalHaul.ToV2().price = s.priceLogisticalHaul;
				if (s.priceFreightHaul > 0)	JobLicenses.FreightHaul.ToV2().price = s.priceFreightHaul; else JobLicenses.FreightHaul.ToV2().price = 10000f;
				if (s.priceTrainLength1 > 0) JobLicenses.TrainLength1.ToV2().price = s.priceTrainLength1;
				if (s.priceTrainLength2 > 0) JobLicenses.TrainLength2.ToV2().price = s.priceTrainLength2;
				if (s.priceFragile > 0) JobLicenses.Fragile.ToV2().price = s.priceFragile;
				if (s.priceHazmat1 > 0) JobLicenses.Hazmat1.ToV2().price = s.priceHazmat1;
				if (s.priceHazmat2 > 0) JobLicenses.Hazmat2.ToV2().price = s.priceHazmat2;
				if (s.priceHazmat3 > 0) JobLicenses.Hazmat3.ToV2().price = s.priceHazmat3;
				if (s.priceMilitary1 > 0) JobLicenses.Military1.ToV2().price = s.priceMilitary1;
				if (s.priceMilitary2 > 0) JobLicenses.Military2.ToV2().price = s.priceMilitary2;
				if (s.priceMilitary3 > 0) JobLicenses.Military3.ToV2().price = s.priceMilitary3;
				//if (s.pricePassenger > 0) JobLicenses.Passenger.ToV2().price = s.pricePassenger;
				// Career License Prices
				if (s.priceTrainDriver > 0) GeneralLicenseType.TrainDriver.ToV2().price = s.priceTrainDriver; else GeneralLicenseType.TrainDriver.ToV2().price = 10000f;
				if (s.priceManualService > 0) GeneralLicenseType.ManualService.ToV2().price = s.priceManualService;
				if (s.priceConcurrentJobs1 > 0) GeneralLicenseType.ConcurrentJobs1.ToV2().price = s.priceConcurrentJobs1;
				if (s.priceConcurrentJobs2 > 0) GeneralLicenseType.ConcurrentJobs2.ToV2().price = s.priceConcurrentJobs2;
				if (s.priceMultipleUnit > 0) GeneralLicenseType.MultipleUnit.ToV2().price = s.priceMultipleUnit;
				if (s.priceDispatcher > 0) GeneralLicenseType.Dispatcher1.ToV2().price = s.priceDispatcher;
				if (s.priceMuseum > 0) GeneralLicenseType.MuseumCitySouth.ToV2().price = s.priceMuseum;
				// Loco License Prices
				if (s.priceDE2 > 0) GeneralLicenseType.DE2.ToV2().price = s.priceDE2; else GeneralLicenseType.DE2.ToV2().price = 10000f;
				if (s.priceDM3 > 0) GeneralLicenseType.DM3.ToV2().price = s.priceDM3;
				if (s.priceS060 > 0) GeneralLicenseType.S060.ToV2().price = s.priceS060;
				if (s.priceDH4 > 0) GeneralLicenseType.DH4.ToV2().price = s.priceDH4;
				if (s.priceS282 > 0) GeneralLicenseType.SH282.ToV2().price = s.priceS282;
				if (s.priceDE6 > 0) GeneralLicenseType.DE6.ToV2().price = s.priceDE6;
            }
            // Job License Conditions
            JobLicenses.Shunting.ToV2().requiredGeneralLicense = GeneralLicenseType.TrainDriver.ToV2();
            JobLicenses.LogisticalHaul.ToV2().requiredGeneralLicense = GeneralLicenseType.NotSet.ToV2();
            JobLicenses.LogisticalHaul.ToV2().requiredJobLicense = JobLicenses.Shunting.ToV2();
            JobLicenses.FreightHaul.ToV2().requiredJobLicense = JobLicenses.LogisticalHaul.ToV2();
            JobLicenses.TrainLength1.ToV2().requiredJobLicense = JobLicenses.Shunting.ToV2();
            JobLicenses.TrainLength2.ToV2().requiredJobLicense = JobLicenses.TrainLength1.ToV2();
            JobLicenses.Fragile.ToV2().requiredJobLicense = JobLicenses.Shunting.ToV2();
            JobLicenses.Hazmat1.ToV2().requiredJobLicense = JobLicenses.Fragile.ToV2();
            JobLicenses.Hazmat2.ToV2().requiredJobLicense = JobLicenses.Hazmat1.ToV2();
            JobLicenses.Hazmat3.ToV2().requiredJobLicense = JobLicenses.Military3.ToV2();
            JobLicenses.Military1.ToV2().requiredJobLicense = JobLicenses.FreightHaul.ToV2();
            JobLicenses.Military2.ToV2().requiredJobLicense = JobLicenses.Military1.ToV2();
            JobLicenses.Military3.ToV2().requiredJobLicense = JobLicenses.Military2.ToV2();
            //JobLicenses.Passenger.ToV2().requiredJobLicense = JobLicenses.FreightHaul.ToV2();
            // Career License Conditions
            GeneralLicenseType.ManualService.ToV2().requiredJobLicense = JobLicenses.Shunting.ToV2();
            GeneralLicenseType.ManualService.ToV2().requiredGeneralLicense = GeneralLicenseType.NotSet.ToV2();
            GeneralLicenseType.ConcurrentJobs1.ToV2().requiredJobLicense = JobLicenses.Shunting.ToV2();
            GeneralLicenseType.ConcurrentJobs2.ToV2().requiredGeneralLicense = GeneralLicenseType.ConcurrentJobs1.ToV2();
            GeneralLicenseType.Dispatcher1.ToV2().requiredGeneralLicense = GeneralLicenseType.ConcurrentJobs2.ToV2();
            GeneralLicenseType.MultipleUnit.ToV2().requiredGeneralLicense = GeneralLicenseType.NotSet.ToV2();
            GeneralLicenseType.MultipleUnit.ToV2().requiredJobLicense = JobLicenses.TrainLength2.ToV2();
            GeneralLicenseType.MuseumCitySouth.ToV2().requiredGeneralLicense = GeneralLicenseType.ManualService.ToV2();
            // Loco License Conditions
            GeneralLicenseType.DE2.ToV2().requiredGeneralLicense = GeneralLicenseType.TrainDriver.ToV2();
            GeneralLicenseType.DM3.ToV2().requiredGeneralLicense = GeneralLicenseType.TrainDriver.ToV2();
            GeneralLicenseType.S060.ToV2().requiredGeneralLicense = GeneralLicenseType.TrainDriver.ToV2();
            GeneralLicenseType.DH4.ToV2().requiredJobLicense = JobLicenses.TrainLength1.ToV2();
            GeneralLicenseType.DE6.ToV2().requiredGeneralLicense = GeneralLicenseType.NotSet.ToV2();
            GeneralLicenseType.DE6.ToV2().requiredJobLicense = JobLicenses.TrainLength2.ToV2();
            GeneralLicenseType.SH282.ToV2().requiredGeneralLicense = GeneralLicenseType.NotSet.ToV2();
            GeneralLicenseType.SH282.ToV2().requiredJobLicense = JobLicenses.TrainLength2.ToV2();

            // ---------- Apply dynamic switches based on already-owned licenses ----------
            var mgr = SingletonBehaviour<LicenseManager>.Instance;
            var switcher = UnityEngine.Object.FindObjectOfType<LicenseConditionSwitcher>();

            if (switcher != null && mgr != null)
            {
                if (mgr.IsJobLicenseAcquired(JobLicenses.TrainLength1.ToV2()))
                    switcher.ForceSwitchTrainLength(); // TL2 requires FreightHaul

                if (mgr.IsGeneralLicenseAcquired(GeneralLicenseType.ConcurrentJobs1.ToV2()))
                    switcher.ForceSwitchConcurrent();   // CJ2 requires FreightHaul

                if (mgr.IsJobLicenseAcquired(JobLicenses.FreightHaul.ToV2()))
                    switcher.ForceSwitchMilitary();     // M1 requires Hazmat2
            }
            else
            {
                Debug.LogWarning("[CareerRework] LicenseConditionSwitcher not found during LoadData.Postfix.");
            }
        }

        internal static void ReapplyHostConfiguration()
        {
            var manager = SingletonBehaviour<LicenseManager>.Instance;
            if (manager == null)
            {
                Debug.LogWarning("[CareerRework][MP] LicenseManager not ready; host configuration will be applied on LoadData.");
                return;
            }

            Postfix(manager);
            Debug.Log("[CareerRework][MP] Host license prices and dependencies reapplied.");
        }

        private static List<T> ProcessListOfIDs<T>(IEnumerable<string>? idList,IEnumerable<T> sourceList) where T : Thing_v2
		{
			List<T> list = new List<T>();

			if (idList == null)
				return list;

			foreach (string id in idList)
			{
				T? value = sourceList.FirstOrDefault(thing => thing.id == id);

				if (value != null)
				{
					list.Add(value);
				}
				else
				{
					Debug.LogError(
						$"Unknown thing ({typeof(T).Name}) " +
						$"ID in save file: {id}");
				}
			}

			return list;
		}
    }
	
    [HarmonyPatch(typeof(LicenseManager), nameof(LicenseManager.IsJobLicenseObtainable))]
    public static class Patch_IsJobLicenseObtainable_TL2
    {
        static void Postfix(JobLicenseType_v2 license, ref bool __result)
        {
            if (!__result) return; // already blocked by other logic
            if (license != JobLicenses.TrainLength2.ToV2()) return;

            var mgr = SingletonBehaviour<LicenseManager>.Instance;
            if (!mgr.IsJobLicenseAcquired(JobLicenses.FreightHaul.ToV2()))
            {
                __result = false;
            }
        }
    }
	
    [HarmonyPatch(typeof(LicenseManager), nameof(LicenseManager.AcquireJobLicense), new[] { typeof(JobLicenseType_v2) })]
    public static class Patch_AcquireJobLicense_Guard
    {
        static bool Prefix(JobLicenseType_v2 newLicense)
        {
            // Allow during save restoration to not strip existing licenses from old saves.
            if (LicenseManager_LoadData_Patch.IsRestoringSave)
                return true;

            if (newLicense == JobLicenses.TrainLength2.ToV2())
            {
                var mgr = SingletonBehaviour<LicenseManager>.Instance;
                if (!mgr.IsJobLicenseAcquired(JobLicenses.FreightHaul.ToV2()))
                {
                    Debug.Log("[CareerRework] Prevented acquiring TrainLength2: FreightHaul missing.");
                    return false; // block acquisition
                }
            }

            return true; // proceed
        }
    }
	
	[HarmonyPatch(typeof(LicenseManager), nameof(LicenseManager.AcquireGeneralLicense))]
	public static class Patch_AcquireGeneralLicense
	{
		static void Postfix(GeneralLicenseType_v2 license) // ← korrekt!
		{
			if (license.v1 == GeneralLicenseType.ConcurrentJobs1)
			{
				var switcher = UnityEngine.Object.FindObjectOfType<LicenseConditionSwitcher>();
				switcher?.ForceSwitchConcurrent();
			}
		}
	}

	[HarmonyPatch(typeof(LicenseManager), nameof(LicenseManager.AcquireJobLicense), new[] { typeof(JobLicenseType_v2) })]
	public static class Patch_AcquireJobLicense
	{
		static void Postfix(JobLicenseType_v2 newLicense) // ← korrekt!
		{
			var switcher = UnityEngine.Object.FindObjectOfType<LicenseConditionSwitcher>();
			if (switcher == null) return;

			if (newLicense.v1 == JobLicenses.TrainLength1)
			{
				switcher.ForceSwitchTrainLength();
			}
			else if (newLicense.v1 == JobLicenses.FreightHaul)
			{
				switcher.ForceSwitchMilitary();
			}
		}
	}

	public class LicenseConditionSwitcher : MonoBehaviour
	{
		private bool concurrentSwitched = false;
		private bool trainLengthSwitched = false;
		private bool militarySwitched = false;
		//private bool passengerSwitched = false;

		void Start()
		{
			Debug.Log("[CareerRework] LicenseConditionSwitcher started.");
		}
		
		private static JobLicenseType_v2 JL(JobLicenses v1)
			=> Globals.G.Types.jobLicenses.FirstOrDefault(j => j.v1 == v1);

		private static GeneralLicenseType_v2 GL(GeneralLicenseType v1)
			=> Globals.G.Types.generalLicenses.FirstOrDefault(g => g.v1 == v1);
		
		public void ForceSwitchConcurrent()
		{
			if (!concurrentSwitched)
				SwitchConcurrentRequirement();
		}

		public void ForceSwitchTrainLength()
		{
			if (!trainLengthSwitched)
				SwitchTrainLengthRequirement();
		}

		public void ForceSwitchMilitary()
		{
			if (!militarySwitched)
				SwitchMilitaryRequirement();
		}

		/*
		public void ForceSwitchPassenger()
		{
			if (!passengerSwitched)
				SwitchPassengerRequirement();
		}*/		

		private void SwitchConcurrentRequirement()
		{
			if (concurrentSwitched) return;

			var cj2 = GL(GeneralLicenseType.ConcurrentJobs2);
			var fh  = JL(JobLicenses.FreightHaul);
			if (cj2 != null && fh != null)
			{
				// UI & logic now see the DB-backed dependency
				cj2.requiredGeneralLicense = GL(GeneralLicenseType.NotSet);
				cj2.requiredJobLicense     = fh;
				concurrentSwitched = true;
				Debug.Log("[CareerRework] ConcurrentJobs2 requirement changed : now requires FreightHaul");
			}
			else
			{
				Debug.LogWarning("[CareerRework] Could not resolve ConcurrentJobs2/FreightHaul.");
			}
		}

		private void SwitchTrainLengthRequirement()
		{
			if (trainLengthSwitched) return;

			var tl2 = JL(JobLicenses.TrainLength2);
			var fh  = JL(JobLicenses.FreightHaul);
			if (tl2 != null && fh != null)
			{
				// Critical change: write to DB object, not .ToV2() temporary
				tl2.requiredJobLicense = fh;
				trainLengthSwitched = true;
				Debug.Log("[CareerRework] TrainLength2 requirement changed : now requires FreightHaul");
			}
			else
			{
				Debug.LogWarning("[CareerRework] Could not resolve TrainLength2/FreightHaul.");
			}
		}

		private void SwitchMilitaryRequirement()
		{
			if (militarySwitched) return;

			var m1  = JL(JobLicenses.Military1);
			var hz2 = JL(JobLicenses.Hazmat2);
			if (m1 != null && hz2 != null)
			{
				m1.requiredJobLicense = hz2;
				militarySwitched = true;
				Debug.Log("[CareerRework] Military1 requirement changed : now requires Hazmat2");
			}
			else
			{
				Debug.LogWarning("[CareerRework] Could not resolve Military1/Hazmat2.");
			}
		}
	}
	
	[HarmonyPatch(typeof(LicenseManager), nameof(LicenseManager.IsJobLicenseObtainable))]
	public static class Patch_IsJobLicenseObtainable_TL2_RefreshRequirement
	{
		static void Prefix(JobLicenseType_v2 license)
		{
			// If TL1 has been acquired earlier, ensure DB-backed TL2 requires FreightHaul.
			if (license != null && license.v1 == JobLicenses.TrainLength2)
			{
				var mgr = SingletonBehaviour<LicenseManager>.Instance;
				if (mgr != null && mgr.IsJobLicenseAcquired(JobLicenses.TrainLength1.ToV2()))
				{
					var tl2 = Globals.G.Types.jobLicenses.FirstOrDefault(j => j.v1 == JobLicenses.TrainLength2);
					var fh  = Globals.G.Types.jobLicenses.FirstOrDefault(j => j.v1 == JobLicenses.FreightHaul);
					if (tl2 != null && fh != null && tl2.requiredJobLicense != fh)
					{
						tl2.requiredJobLicense = fh;
						Debug.Log("[CareerRework] (Refresh) Forced TL2 dependency to FreightHaul in DB before obtainability check.");
					}
				}
			}
		}
	}
	
	[HarmonyPatch(typeof(LicenseManager), nameof(LicenseManager.IsJobLicenseObtainable))]
	public static class Patch_IsJobLicenseObtainable_Shunting
	{
		static void Postfix(JobLicenseType_v2 license, ref bool __result)
		{
			if (license == JobLicenses.Shunting.ToV2())
			{
				var mgr = SingletonBehaviour<LicenseManager>.Instance;

				bool hasTrainDriver = mgr.IsGeneralLicenseAcquired(GeneralLicenseType.TrainDriver.ToV2());
				bool hasAnyLoco = 
					mgr.IsGeneralLicenseAcquired(GeneralLicenseType.DE2.ToV2()) ||
					mgr.IsGeneralLicenseAcquired(GeneralLicenseType.S060.ToV2()) ||
					mgr.IsGeneralLicenseAcquired(GeneralLicenseType.DM3.ToV2());

				if (!(hasTrainDriver && hasAnyLoco))
				{
					__result = false;
				}
			}
		}
	}
	
	// ---------- Show custom message BEFORE vanilla sets "TrainDriver" ----------
	[HarmonyPatch(typeof(CareerManagerLicensesScreen), nameof(CareerManagerLicensesScreen.HandleInputAction))]
	public static class Patch_CareerManagerLicensesScreen_CustomMissingText_Prefix
	{
		// Intercept before vanilla logic; if we handle it, skip original (return false)
		static bool Prefix(CareerManagerLicensesScreen __instance, InputAction input)
		{
			if (input != InputAction.Confirm) return true; // let vanilla handle others

			var mgr = SingletonBehaviour<LicenseManager>.Instance;
			if (mgr == null || __instance == null) return true;

			// Resolve private selector from base ScrollableDisplayScreen
			var selectorField = AccessTools.Field(typeof(ScrollableDisplayScreen), "selector");
			var selector = selectorField?.GetValue(__instance);
			if (selector == null) return true;

			var currentProp = selector.GetType().GetProperty("Current", BindingFlags.Instance | BindingFlags.Public);
			if (currentProp == null) return true;
			int currentIndex = (int)currentProp.GetValue(selector);

			// Resolve private entries list
			var entriesField = AccessTools.Field(typeof(CareerManagerLicensesScreen), "licenseEntries");
			var entriesList = entriesField?.GetValue(__instance) as System.Collections.IList;
			if (entriesList == null || currentIndex < 0 || currentIndex >= entriesList.Count) return true;

			var entry = entriesList[currentIndex];
			if (entry == null) return true;

			// Check: selected item is a Job license AND it's Shunting
			var isJobSetProp  = entry.GetType().GetProperty("IsJobLicenseSet", BindingFlags.Instance | BindingFlags.Public);
			if (isJobSetProp == null || !(bool)isJobSetProp.GetValue(entry)) return true;

			var jobLicenseProp = entry.GetType().GetProperty("JobLicense", BindingFlags.Instance | BindingFlags.Public);
			var jobLicense = jobLicenseProp?.GetValue(entry) as JobLicenseType_v2;
			if (jobLicense == null || jobLicense != JobLicenses.Shunting.ToV2()) return true;

			// Our custom availability condition
			bool hasTrainDriver = mgr.IsGeneralLicenseAcquired(GeneralLicenseType.TrainDriver.ToV2());
			bool hasAnyLoco =
				mgr.IsGeneralLicenseAcquired(GeneralLicenseType.DE2.ToV2()) ||
				mgr.IsGeneralLicenseAcquired(GeneralLicenseType.S060.ToV2()) ||
				mgr.IsGeneralLicenseAcquired(GeneralLicenseType.DM3.ToV2());

			// If player owns TrainDriver but no loco license → show our message and STOP vanilla
			if (hasTrainDriver && !hasAnyLoco)
			{
				string customMsg = "DE2, DM3, S060";
				__instance.infoScreen.SetInfoData(__instance, CareerManagerInfoScreen.Preset.MissingLicense, customMsg);
				__instance.screenSwitcher.SetActiveDisplay(__instance.infoScreen);
				return false; // ← skip original HandleInputAction so "TrainDriver" doesn't overwrite us
			}

			return true; // run vanilla for all other cases
		}
	}
	
    [HarmonyPatch(typeof(StartGameData_NewCareer))]
    public static class StartGameData_NewCareerPatch
    {
        [HarmonyPatch("PrepareNewSaveData")]
        [HarmonyPrefix]
        public static bool OverrideSkipTutorialLogic(ref SaveGameData saveGameData, out IDifficulty DifficultyToUse, IGameSession session, IDifficulty difficultyParams, bool skipTutorial)
        {
            if (saveGameData == null)
            {
                saveGameData = SaveGameManager.MakeEmptySave();
            }
            else
            {
                saveGameData.Clear();
            }

            saveGameData.SetString("Game_mode", session.GameMode);
            saveGameData.SetString("World", session.World);
            saveGameData.SetDouble("Starting_time_and_date", AStartGameData.BaseTimeAndDate.ToOADate());

            if (difficultyParams != null)
            {
                DifficultyToUse = difficultyParams;
                DifficultyParamsSetter.SetDifficultyParams(difficultyParams);
            }
            else
            {
                DifficultyToUse = DifficultyParamsSetter.Standard;
                Debug.LogError("Unexpected state: difficultyParams are null for new career session. Using default values in attempt to recover.");
            }

            session.PerformGameplayEntryDifficultyCheck(DifficultyToUse);

            saveGameData.SetBool("Tutorial_01_completed", true);
            saveGameData.SetBool("Tutorial_02_completed", true);
            saveGameData.SetBool("Tutorial_03_completed", true);
            session.GameData.SetBool("Difficulty_picked", skipTutorial);

            saveGameData.SetFloat("Player_money", Main.GetStartingMoney());

            GameParams.StartingItemsType startingItems = Globals.G.GameParams.StartingItems;
            switch (startingItems)
            {
                case GameParams.StartingItemsType.Basic:
                case GameParams.StartingItemsType.Auto:
                    saveGameData.SetInt("Starting_items", 0);
                    break;
                case GameParams.StartingItemsType.Expanded:
                    saveGameData.SetInt("Starting_items", 1);
                    break;
                case GameParams.StartingItemsType.Engineer:
                    saveGameData.SetInt("Starting_items", 2);
                    break;
                default:
					Debug.LogError(
						$"Unexpected state: Unhandled entry " +
						$"{startingItems}. Using basic starting items");

					saveGameData.SetInt("Starting_items",0);

					break;
				}
                CareerReworkSaveState.AddConfiguredStartingConcurrentLicensesToSave(
                    saveGameData);
					
				CareerReworkSaveState.SaveTo(saveGameData);
				Debug.Log(
					"[CareerRework] Initial CareerRework settings written " +
					"to the new career save.");

				return false;
        }
    }
	
	[HarmonyPatch(typeof(SaveGameManager), "Start")]
    public static class SaveGameManager_Start_CareerRework_Patch
    {
        static void Postfix(
            SaveGameManager __instance)
        {
            if (__instance == null)
                return;

            __instance.OnInternalDataUpdate -=
                OnInternalDataUpdate;

            __instance.OnInternalDataUpdate +=
                OnInternalDataUpdate;

            Debug.Log("[CareerRework] Savegame settings writer registered.");
        }

        private static void OnInternalDataUpdate(
            SaveGameData saveData)
        {
            if (saveData == null)
                return;

            if (CR_Multiplayer.IsClient)
                return;

            CareerReworkSaveState.SaveTo(
                saveData);
        }
    }

    public static class LicenseManagerMod
    {
        public static void Init()
        {
            var harmony = new Harmony("com.yourname.licensemanagerpatch");
            harmony.PatchAll();
            Debug.Log("[CareerRework] Harmony patches applied.");

            var field = typeof(LicenseManager).GetField("TutorialGeneralLicenses", BindingFlags.Static | BindingFlags.Public);
            if (field != null)
            {
                field.SetValue(null, new List<GeneralLicenseType_v2> { GeneralLicenseType.NotSet.ToV2() });
            }
        }

        public static bool CreateLocoZoneBlocker(TrainCar loco)
		{
			if (loco == null)
				return false;

			if (loco.interior == null)
				return false;

			string blockerName =
				$"LocoZoneBlocker_{loco.ID}";

			Transform? existingBlockerTransform =
				loco.interior.Find(
					blockerName);

			// The blocker already exists.
			// Only update the optional cab reference when it becomes available.
			if (existingBlockerTransform != null)
			{
				LocoZoneBlocker? existingBlocker =
					existingBlockerTransform.GetComponent<LocoZoneBlocker>();

				if (existingBlocker != null &&
					existingBlocker.cab == null)
				{
					CabTeleportDestination? existingCab =
						loco.GetComponentInChildren<CabTeleportDestination>(
							true);

					if (existingCab != null)
					{
						existingBlocker.cab =
							existingCab;

						Debug.Log(
							$"[CareerRework] Cab reference attached later to " +
							$"DE2 blocker for {loco.ID}.");
					}
				}

				return true;
			}

			// IMPORTANT:
			// Create the physical blocker immediately.
			// CabTeleportDestination is NOT required for the collider.
			GameObject blocker =
				GameObject.CreatePrimitive(
					PrimitiveType.Cube);

			blocker.name =
				blockerName;

			blocker.transform.SetParent(
				loco.interior,
				false);

			blocker.transform.localPosition =
				new Vector3(
					0f,
					2f,
					0f);

			blocker.transform.localRotation =
				Quaternion.identity;

			blocker.transform.localScale =
				new Vector3(
					3.5f,
					3.9f,
					7.8f);

			Renderer? renderer =
				blocker.GetComponent<Renderer>();

			if (renderer != null)
				renderer.enabled = false;

			Collider? collider =
				blocker.GetComponent<Collider>();

			if (collider != null)
				collider.isTrigger = true;

			LocoZoneBlocker locoZoneBlocker =
				blocker.AddComponent<LocoZoneBlocker>();

			locoZoneBlocker.blockerObjectsParent =
				blocker;

			// Optional:
			// The cab may not exist until the locomotive interior is streamed.
			CabTeleportDestination? cab =
				loco.GetComponentInChildren<CabTeleportDestination>(
					true);

			if (cab != null)
			{
				locoZoneBlocker.cab =
					cab;
			}

			Debug.Log(
				$"[CareerRework] Created local DE2 blocker for " +
				$"{loco.ID}. CabReady={cab != null}, " +
				$"MP client={CR_Multiplayer.IsClient}");

			return true;
		}

        public static IEnumerator BlockDE2Later()
		{
			// CHANGE:
			// Wait until the local CarSpawner exists.
			while (CarSpawner.Instance == null)
				yield return null;

			while (CarSpawner.Instance.PoolSetupInProgress)
				yield return null;

			while (!AStartGameData.carsAndJobsLoadingFinished)
				yield return null;

			// Multiplayer may instantiate synchronized cars and interiors
			// slightly after carsAndJobsLoadingFinished becomes true.
			yield return new WaitForSeconds(2f);

			List<TrainCar> cars =
				CarSpawner.Instance.AllCars;

			if (cars == null)
				yield break;

			for (int i = 0; i < cars.Count; i++)
			{
				TrainCar loco =
					cars[i];

				if (loco == null)
					continue;

				if (loco.carType != TrainCarType.LocoShunter)
					continue;

				bool created =
					CreateLocoZoneBlocker(
						loco);

				if (!created)
				{
					Debug.LogWarning(
						$"[CareerRework] DE2 blocker could not be created for " +
						$"{loco.ID}: locomotive interior is unavailable.");
				}
			}

			Debug.Log(
				$"[CareerRework] Initial DE2 blocker pass completed. " +
				$"MP client={CR_Multiplayer.IsClient}");
		}
    }
	
	public sealed class DE2BlockerWatcher : MonoBehaviour
    {
        private static DE2BlockerWatcher? instance;

        private const float CHECK_INTERVAL_SECONDS = 2f;

        public static void Initialize()
        {
            if (instance != null)
                return;

            GameObject runtimeObject =
                new GameObject("[CareerRework DE2 Blocker Watcher]");

            UnityEngine.Object.DontDestroyOnLoad(runtimeObject);

            instance =
                runtimeObject.AddComponent<DE2BlockerWatcher>();

            Debug.Log(
                "[CareerRework] DE2 blocker watcher initialized.");
        }

        // NEW:
        // Explicit refresh after a multiplayer client receives
        // the host settings snapshot.
        public static void RequestRefresh()
        {
            Initialize();

            if (instance == null)
                return;

            instance.StartCoroutine(
                instance.RefreshAfterMultiplayerJoin());
        }

        private IEnumerator Start()
        {
            WaitForSeconds wait =
                new WaitForSeconds(CHECK_INTERVAL_SECONDS);

            while (true)
            {
                ApplyMissingBlockers();

                yield return wait;
            }
        }

        private IEnumerator RefreshAfterMultiplayerJoin()
        {
            while (CarSpawner.Instance == null)
                yield return null;

            while (CarSpawner.Instance.PoolSetupInProgress)
                yield return null;

            while (!AStartGameData.carsAndJobsLoadingFinished)
                yield return null;

            // Multiplayer cars and interiors may appear shortly
            // after the local loading flag was set.
            yield return new WaitForSeconds(2f);

            ApplyMissingBlockers();

            Debug.Log(
                "[CareerRework][MP] DE2 blocker refresh completed " +
                "after client synchronization.");
        }

        public static void ApplyMissingBlockers()
        {
            CarSpawner spawner = CarSpawner.Instance;

            if (spawner == null)
                return;

            List<TrainCar> cars = spawner.AllCars;

            if (cars == null || cars.Count == 0)
                return;

            for (int i = 0; i < cars.Count; i++)
            {
                TrainCar loco = cars[i];

                if (loco == null)
                    continue;

                if (loco.carType != TrainCarType.LocoShunter)
                    continue;

                if (loco.interior == null)
                    continue;

                bool blockerAvailable =
					LicenseManagerMod.CreateLocoZoneBlocker(
						loco);

				if (!blockerAvailable)
				{
					Debug.LogWarning(
						$"[CareerRework] Could not create DE2 blocker for " +
						$"{loco.ID}: locomotive interior is unavailable.");
				}
            }
        }

        private void OnDestroy()
        {
            if (instance == this)
                instance = null;
        }
    }

    public static class LocoSpawner
    {
        public static IEnumerator SpawnStarterLocoAfterLoad()
		{
			while (CarSpawner.Instance == null || CarSpawner.Instance.PoolSetupInProgress)
				yield return null;

			SpawnStarterOnTrack("[Y]_[SM]_[T1-01-P]", Main.settings?.starterLocoPrimary);
			SpawnStarterOnTrack("[Y]_[SM]_[T1-02-P]", Main.settings?.starterLocoSecondary);
		}	

		private static void SpawnStarterOnTrack(string trackName, StarterLocoType? locoType)
		{
			if (locoType == null) return;

			var track = SingletonBehaviour<RailTrackRegistryBase>.Instance.AllTracks
				.FirstOrDefault(t => t.name == trackName);

			if (track == null)
			{
				Debug.LogError($"[CareerRework] Track not found: {trackName}");
				return;
			}

			TrainCarType carType = locoType switch
			{
				StarterLocoType.DE2  => TrainCarType.LocoShunter,
				StarterLocoType.DM3  => TrainCarType.LocoDM3,
				StarterLocoType.S060 => TrainCarType.LocoS060,
				_ => TrainCarType.LocoDM3
			};

			var livery = Globals.G.Types.Liveries.FirstOrDefault(l => l.v1 == carType);
			if (livery == null)
			{
				Debug.LogError($"[CareerRework] Livery not found for {carType}");
				return;
			}

			var spawned = CarSpawner.Instance.SpawnCarTypesOnTrack(
				new List<TrainCarLivery> { livery },
				new List<bool> { false },
				track,
				preventAutoCoupleOnLastCars: true,
				applyHandbrakeOnLastCars: true,
				playerSpawnedCars: false
			);

			if (spawned != null && spawned.Count > 0)
				Debug.Log($"[CareerRework] Spawned {carType} at {trackName}");
		}
	}

    [HarmonyPatch]
    public static class Patch_CarSpawner_SpawnCar_Logger
    {
        static MethodBase TargetMethod()
        {
            var railTrackType = AccessTools.TypeByName("RailTrack");
            return typeof(CarSpawner).GetMethod("SpawnCar", new[] {
                typeof(GameObject), railTrackType, typeof(Vector3), typeof(Vector3),
                typeof(bool), typeof(bool)
            });
        }

		static void Postfix(
			TrainCar __result,
			bool playerSpawnedCar)
		{
			if (__result == null)
				return;

			if (playerSpawnedCar)
				return;

			if (!__result.IsLoco)
				return;

			if (__result.carType != TrainCarType.LocoShunter)
				return;

			if (__result.interior == null)
				return;

			string blockerName =
				$"LocoZoneBlocker_{__result.ID}";

			// CHANGE:
			// The blocker is parented to interior, not to TrainCar.transform.
			if (__result.interior.Find(blockerName) != null)
				return;

			LicenseManagerMod.CreateLocoZoneBlocker(
				__result);
		}
    }

    [HarmonyPatch(typeof(StartingItemsController), nameof(StartingItemsController.AddStartingItems))]
    public static class ItemsPatch
    {
        private static readonly string[] includes = new string[] { "Oiler", "lighter", "shovel" };

        public static void Prefix()
        {
            if (Main.settings == null || Main.settings.startupMode != StartupMode.Custom)
				return;

			bool hasS060 =
				Main.settings.starterLocoPrimary == StarterLocoType.S060 ||
				Main.settings.starterLocoSecondary == StarterLocoType.S060;

			if (!hasS060)
				return;

            Debug.Log("[CareerRework] Patch get starting items");

            var allDefs = AccessTools.Field(typeof(ItemsConfig), "startingItems").GetValue(Globals.G.Items) as List<StartingItems>;
            if (allDefs == null) return;

            var basic = allDefs.FirstOrDefault(i => i.startingItemsType == GameParams.StartingItemsType.Basic);
            var expanded = allDefs.FirstOrDefault(i => i.startingItemsType == GameParams.StartingItemsType.Expanded);
            if (basic == null || expanded == null) return;

            var basicItems = basic.GetStartingItems();
            var expandedItems = expanded.GetStartingItems();

            var items = expandedItems.Where(i => includes.Contains(i.ItemPrefabName) && !basicItems.Contains(i)).ToList();

            basicItems.AddRange(items);
            expandedItems.RemoveAll(i => !items.Contains(i));

            Debug.Log($"[CareerRework] {items.Count} additional Items for S060 in your inventory.");
        }
    }
	
	[HarmonyPatch(typeof(DV.UI.MainMenu),"Start")]
	public static class Patch_MainMenu_Start_CareerSettings
	{
		static void Postfix()
		{
			Main.SetCareerWorldActive(false);

			Debug.Log(
				"[CareerRework] Main menu loaded. " +
				"Career settings unlocked.");
		}
	}

    [HarmonyPatch(typeof(WorldStreamingInit), "Awake")]
	public static class Patch_WorldStreamingInit
	{
		static void Postfix()
		{
			Main.SetCareerWorldActive(true);
			
			DE2BlockerWatcher.Initialize();
			
			CoroutineManager.Instance.Run(LicenseManagerMod.BlockDE2Later());

			if (CR_Multiplayer.CanModifyWorld)
			{
				CoroutineManager.Instance.Run(LocoSpawner.SpawnStarterLocoAfterLoad());
			}
			else
			{
				Debug.Log(
					"[CareerRework][MP] Starter locomotive spawn " +
					"skipped on client.");
			}
		}
	}
}
