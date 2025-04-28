using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using DV;
using DV.Common;
using DV.Booklets;
using DV.Localization.Debug;
using DV.Scenarios.Common;
using DV.JObjectExtstensions;
using DV.ThingTypes;
using DV.ThingTypes.TransitionHelpers;
using DV.UserManagement;
using DV.Utils;
using HarmonyLib;
using UnityModManagerNet;
using Debug = UnityEngine.Debug; // Explicitly use UnityEngine.Debug


namespace CareerRework
{
	[HarmonyPatch(typeof(LicenseManager), "LoadData")]
	public static class LicenseManager_LoadData_Patch
	{
		// Run patch after super method
		static void Postfix(LicenseManager __instance)
		{
			// Job Licenses Prices
			JobLicenses.Shunting.ToV2().price = 15000f;         //	15000f;
			JobLicenses.LogisticalHaul.ToV2().price = 25000f;   //	25000f;
			JobLicenses.FreightHaul.ToV2().price = 50000f;      //	50000f;
			JobLicenses.TrainLength1.ToV2().price = 25000f;     //	25000f;
			JobLicenses.TrainLength2.ToV2().price = 50000f;     //	50000f;
			JobLicenses.Fragile.ToV2().price = 75000f;          //	75000f;
			JobLicenses.Hazmat1.ToV2().price = 100000f;         //	100000f;
			JobLicenses.Hazmat2.ToV2().price = 200000f;         //	200000f;
			JobLicenses.Hazmat3.ToV2().price = 400000f;         //	400000f;
			JobLicenses.Military1.ToV2().price = 250000f;       //	250000f;
			JobLicenses.Military2.ToV2().price = 500000f;       //	500000f;
			JobLicenses.Military3.ToV2().price = 1000000f;      //	1000000f;

			// Job Licenses Conditions
			JobLicenses.Shunting.ToV2().requiredGeneralLicense = GeneralLicenseType.TrainDriver.ToV2();
			JobLicenses.LogisticalHaul.ToV2().requiredGeneralLicense = GeneralLicenseType.NotSet.ToV2();
			JobLicenses.LogisticalHaul.ToV2().requiredJobLicense = JobLicenses.Shunting.ToV2();
			JobLicenses.FreightHaul.ToV2().requiredJobLicense = JobLicenses.LogisticalHaul.ToV2();
			JobLicenses.TrainLength1.ToV2().requiredJobLicense = JobLicenses.FreightHaul.ToV2();
			JobLicenses.TrainLength2.ToV2().requiredJobLicense = JobLicenses.TrainLength1.ToV2();
			JobLicenses.Fragile.ToV2().requiredJobLicense = JobLicenses.FreightHaul.ToV2();
			JobLicenses.Hazmat1.ToV2().requiredJobLicense = JobLicenses.Fragile.ToV2();
			JobLicenses.Hazmat2.ToV2().requiredJobLicense = JobLicenses.Hazmat1.ToV2();
			JobLicenses.Hazmat3.ToV2().requiredJobLicense = JobLicenses.Military3.ToV2();
			JobLicenses.Military1.ToV2().requiredJobLicense = JobLicenses.Hazmat2.ToV2();
			JobLicenses.Military2.ToV2().requiredJobLicense = JobLicenses.Military1.ToV2();
			JobLicenses.Military3.ToV2().requiredJobLicense = JobLicenses.Military2.ToV2();

			// Career Licenses Prices
			GeneralLicenseType.TrainDriver.ToV2().price = 150000f;      //	150000f;
			GeneralLicenseType.ManualService.ToV2().price = 25000f;     //	25000f;
			GeneralLicenseType.ConcurrentJobs1.ToV2().price = 25000f;   //	25000f;
			GeneralLicenseType.ConcurrentJobs2.ToV2().price = 50000f;   //	50000f;
			GeneralLicenseType.MultipleUnit.ToV2().price = 50000f;      //	50000f;
			GeneralLicenseType.Dispatcher1.ToV2().price = 50000f;       //	50000f;
			GeneralLicenseType.MuseumCitySouth.ToV2().price = 150000f;  //	150000f;

			// Career Licenses Conditions
			GeneralLicenseType.ManualService.ToV2().requiredJobLicense = JobLicenses.Basic.ToV2();
			GeneralLicenseType.ConcurrentJobs1.ToV2().requiredJobLicense = JobLicenses.LogisticalHaul.ToV2();
			GeneralLicenseType.ConcurrentJobs2.ToV2().requiredGeneralLicense = GeneralLicenseType.ConcurrentJobs1.ToV2();
			GeneralLicenseType.MultipleUnit.ToV2().requiredGeneralLicense = GeneralLicenseType.NotSet.ToV2();
			GeneralLicenseType.MultipleUnit.ToV2().requiredJobLicense = JobLicenses.TrainLength2.ToV2();
			GeneralLicenseType.Dispatcher1.ToV2().requiredGeneralLicense = GeneralLicenseType.ConcurrentJobs2.ToV2();
			GeneralLicenseType.MuseumCitySouth.ToV2().requiredGeneralLicense = GeneralLicenseType.ManualService.ToV2();

			// Loco Licenses Prices
			GeneralLicenseType.DE2.ToV2().price = 100000f;      //	150000f;
			GeneralLicenseType.DM3.ToV2().price = 100000f;      //	150000f;
			GeneralLicenseType.DH4.ToV2().price = 400000f;      //	400000f;
			GeneralLicenseType.DE6.ToV2().price = 1000000f;     //	1000000f;
			GeneralLicenseType.S060.ToV2().price = 250000f;     //	250000f;
			GeneralLicenseType.SH282.ToV2().price = 750000f;    //	750000f;

			// Loco Licenses Conditions
			GeneralLicenseType.DE2.ToV2().requiredJobLicense = JobLicenses.Basic.ToV2();
			GeneralLicenseType.DE2.ToV2().requiredGeneralLicense = GeneralLicenseType.TrainDriver.ToV2();
			GeneralLicenseType.DM3.ToV2().requiredJobLicense = JobLicenses.Basic.ToV2();
			GeneralLicenseType.DM3.ToV2().requiredGeneralLicense = GeneralLicenseType.TrainDriver.ToV2();
			GeneralLicenseType.DH4.ToV2().requiredJobLicense = JobLicenses.TrainLength1.ToV2();
			GeneralLicenseType.DE6.ToV2().requiredGeneralLicense = GeneralLicenseType.ConcurrentJobs2.ToV2();
			GeneralLicenseType.S060.ToV2().requiredGeneralLicense = GeneralLicenseType.NotSet.ToV2();
			GeneralLicenseType.S060.ToV2().requiredJobLicense = JobLicenses.LogisticalHaul.ToV2();
			GeneralLicenseType.SH282.ToV2().requiredGeneralLicense = GeneralLicenseType.NotSet.ToV2();
			GeneralLicenseType.SH282.ToV2().requiredJobLicense = JobLicenses.TrainLength2.ToV2();
		}

		// Prefix: Wird vor der Originalfunktion aufgerufen
		static bool Prefix(SaveGameData data)
		{
			Debug.Log("[LicenseManagerMod] Loading GeneralLicenses, JobLicenses, and Garages...");

			// Lade GeneralLicenses
			ProcessListOfIDs(data.GetStringArray("Licenses_General"), Globals.G.Types.generalLicenses).ForEach(delegate (GeneralLicenseType_v2 l)
			{
				SingletonBehaviour<LicenseManager>.Instance.AcquireGeneralLicense(l);
			});

			// Lade JobLicenses
			ProcessListOfIDs(data.GetStringArray("Licenses_Jobs"), Globals.G.Types.jobLicenses).ForEach(delegate (JobLicenseType_v2 l)
			{
				SingletonBehaviour<LicenseManager>.Instance.AcquireJobLicense(l);
			});

			// Lade Garages
			ProcessListOfIDs(data.GetStringArray("Garages"), Globals.G.Types.garages).ForEach(delegate (GarageType_v2 g)
			{
				SingletonBehaviour<LicenseManager>.Instance.UnlockGarage(g);
			});

			Debug.Log("[LicenseManagerMod] All data loaded successfully.");

			// Blockiere die Originalmethode vollständig
			return false;
		}

		// Hilfsmethode aus der Originalklasse extrahiert
		private static List<T> ProcessListOfIDs<T>(IEnumerable<string> idList, IEnumerable<T> sourceList) where T : Thing_v2
		{
			if (idList == null)
			{
				return new List<T>();
			}
			List<T> list = new List<T>();
			foreach (string id in idList)
			{
				T val = sourceList.FirstOrDefault(t => t.id == id);
				if (val != null)
				{
					list.Add(val);
				}
				else
				{
					Debug.LogError("Unknown thing (" + typeof(T).Name + ") ID in save file: " + id);
				}
			}
			return list;
		}
	}

	public static class LicenseManagerMod
	{
		public static void Init()
		{
			var harmony = new Harmony("com.yourname.licensemanagerpatch");
			harmony.PatchAll();
			Debug.Log("[LicenseManagerMod] Harmony patches applied.");
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
			if (skipTutorial)
			{	/*
				foreach (GeneralLicenseType_v2 tutorialGeneralLicense in LicenseManager.TutorialGeneralLicenses)
				{
					SingletonBehaviour<UnlockablesManager>.Instance.UnlockThing(tutorialGeneralLicense);
					saveGameData.AddToStringArray("Licenses_General", tutorialGeneralLicense.id, enforceUnique: true);
				}*/
				saveGameData.SetFloat("Player_money", 270000f);			// if the game will start complete without licenses
				//saveGameData.SetFloat("Player_money", 17250f);		// if the game will start with Tutorial licenses (Driver and DE2)
				saveGameData.SetBool("Tutorial_01_completed", value: true);
				saveGameData.SetBool("Tutorial_02_completed", value: true);
				saveGameData.SetBool("Tutorial_03_completed", value: true);
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
						Debug.LogError($"Unexpected state: Unhandled entry {startingItems}. Using basic starting items");
						saveGameData.SetInt("Starting_items", 0);
						break;
				}
				session.GameData.SetBool("Difficulty_picked", value: true);
			}
			else
			{
				saveGameData.SetBool("Tutorial_01_completed", value: false);
				saveGameData.SetBool("Tutorial_02_completed", value: false);
				saveGameData.SetBool("Tutorial_03_completed", value: false);
				session.GameData.SetBool("Difficulty_picked", value: false);
				saveGameData.SetFloat("Player_money", 0f);
			}
			return false; // Skip the original method
		}
	}
}
