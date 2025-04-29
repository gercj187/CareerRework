using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
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
using UnityEngine;
using UnityEngine.SceneManagement;
using Debug = UnityEngine.Debug;


namespace CareerRework
{
	[HarmonyPatch(typeof(LicenseManager), "LoadData")]
	public static class LicenseManager_LoadData_Patch
	{
		// Run patch after super method
		static void Postfix(LicenseManager __instance)
		{
			// Job Licenses Prices									vanilla			modded
			JobLicenses.Shunting.ToV2().price = 15000f;         //	1000			15000
			JobLicenses.LogisticalHaul.ToV2().price = 25000f;   //	20000			25000
			JobLicenses.FreightHaul.ToV2().price = 50000f;      //	OWNED			50000
			JobLicenses.TrainLength1.ToV2().price = 25000f;     //	10000			25000
			JobLicenses.TrainLength2.ToV2().price = 50000f;     //	20000			50000
			JobLicenses.Fragile.ToV2().price = 75000f;          //	10000			75000
			JobLicenses.Hazmat1.ToV2().price = 100000f;         //	40000			100000
			JobLicenses.Hazmat2.ToV2().price = 200000f;         //	130000			200000
			JobLicenses.Hazmat3.ToV2().price = 400000f;         //	290000			400000
			JobLicenses.Military1.ToV2().price = 250000f;       //	100000			250000
			JobLicenses.Military2.ToV2().price = 500000f;       //	30000			500000
			JobLicenses.Military3.ToV2().price = 1000000f;      //	400000			1000000

			// Job Licenses Conditions
			JobLicenses.Shunting.ToV2().requiredGeneralLicense = GeneralLicenseType.TrainDriver.ToV2();
			JobLicenses.LogisticalHaul.ToV2().requiredGeneralLicense = GeneralLicenseType.NotSet.ToV2();
			JobLicenses.LogisticalHaul.ToV2().requiredJobLicense = JobLicenses.Shunting.ToV2();
			JobLicenses.FreightHaul.ToV2().requiredJobLicense = JobLicenses.LogisticalHaul.ToV2();
			JobLicenses.TrainLength1.ToV2().requiredJobLicense = JobLicenses.LogisticalHaul.ToV2();
			JobLicenses.TrainLength2.ToV2().requiredJobLicense = JobLicenses.TrainLength1.ToV2();
			JobLicenses.Fragile.ToV2().requiredJobLicense = JobLicenses.FreightHaul.ToV2();
			JobLicenses.Hazmat1.ToV2().requiredJobLicense = JobLicenses.Fragile.ToV2();
			JobLicenses.Hazmat2.ToV2().requiredJobLicense = JobLicenses.Hazmat1.ToV2();
			JobLicenses.Hazmat3.ToV2().requiredJobLicense = JobLicenses.Military3.ToV2();
			JobLicenses.Military1.ToV2().requiredJobLicense = JobLicenses.Hazmat2.ToV2();
			JobLicenses.Military2.ToV2().requiredJobLicense = JobLicenses.Military1.ToV2();
			JobLicenses.Military3.ToV2().requiredJobLicense = JobLicenses.Military2.ToV2();

			// Career Licenses Prices										vanilla			modded
			GeneralLicenseType.TrainDriver.ToV2().price = 150000f;      //	OWNED			150000
			GeneralLicenseType.ManualService.ToV2().price = 25000f;     //	20000			25000
			GeneralLicenseType.ConcurrentJobs1.ToV2().price = 25000f;   //	10000			25000
			GeneralLicenseType.ConcurrentJobs2.ToV2().price = 50000f;   //	20000			50000
			GeneralLicenseType.MultipleUnit.ToV2().price = 50000f;      //	30000			50000
			GeneralLicenseType.Dispatcher1.ToV2().price = 50000f;       //	10000			50000
			GeneralLicenseType.MuseumCitySouth.ToV2().price = 150000f;  //	15000			150000

			// Career Licenses Conditions
			GeneralLicenseType.ManualService.ToV2().requiredJobLicense = JobLicenses.Basic.ToV2();
			GeneralLicenseType.ManualService.ToV2().requiredGeneralLicense = GeneralLicenseType.Dispatcher1.ToV2();
			GeneralLicenseType.ConcurrentJobs1.ToV2().requiredJobLicense = JobLicenses.LogisticalHaul.ToV2();
			GeneralLicenseType.ConcurrentJobs2.ToV2().requiredGeneralLicense = GeneralLicenseType.ConcurrentJobs1.ToV2();
			GeneralLicenseType.MultipleUnit.ToV2().requiredGeneralLicense = GeneralLicenseType.NotSet.ToV2();
			GeneralLicenseType.MultipleUnit.ToV2().requiredJobLicense = JobLicenses.TrainLength2.ToV2();
			GeneralLicenseType.Dispatcher1.ToV2().requiredGeneralLicense = GeneralLicenseType.ConcurrentJobs1.ToV2();
			GeneralLicenseType.MuseumCitySouth.ToV2().requiredGeneralLicense = GeneralLicenseType.ManualService.ToV2();

			// Loco Licenses Prices									vanilla			modded
			GeneralLicenseType.DE2.ToV2().price = 100000f;      //	OWNED			150000
			GeneralLicenseType.DM3.ToV2().price = 100000f;      //	30000			150000
			GeneralLicenseType.S060.ToV2().price = 250000f;     //	20000			250000
			GeneralLicenseType.DH4.ToV2().price = 400000f;      //	50000			400000
			GeneralLicenseType.SH282.ToV2().price = 750000f;    //	50000			750000
			GeneralLicenseType.DE6.ToV2().price = 1000000f;     //	200000			1000000

			// Loco Licenses Conditions
			GeneralLicenseType.DE2.ToV2().requiredJobLicense = JobLicenses.Shunting.ToV2();
			GeneralLicenseType.DE2.ToV2().requiredGeneralLicense = GeneralLicenseType.NotSet.ToV2();
			GeneralLicenseType.DM3.ToV2().requiredJobLicense = JobLicenses.Shunting.ToV2();
			GeneralLicenseType.DM3.ToV2().requiredGeneralLicense = GeneralLicenseType.NotSet.ToV2();
			GeneralLicenseType.DH4.ToV2().requiredJobLicense = JobLicenses.TrainLength1.ToV2();
			GeneralLicenseType.DE6.ToV2().requiredGeneralLicense = GeneralLicenseType.NotSet.ToV2();
			GeneralLicenseType.DE6.ToV2().requiredJobLicense = JobLicenses.TrainLength2.ToV2();
			GeneralLicenseType.S060.ToV2().requiredGeneralLicense = GeneralLicenseType.NotSet.ToV2();
			GeneralLicenseType.S060.ToV2().requiredJobLicense = JobLicenses.LogisticalHaul.ToV2();
			GeneralLicenseType.SH282.ToV2().requiredGeneralLicense = GeneralLicenseType.NotSet.ToV2();
			GeneralLicenseType.SH282.ToV2().requiredJobLicense = JobLicenses.TrainLength2.ToV2();
		}

		// Prefix: Wird vor der Originalfunktion aufgerufen
		

		static bool Prefix(SaveGameData data)
		{
			Debug.Log("[CareerRework] Loading GeneralLicenses, JobLicenses, and Garages...");
			ProcessListOfIDs(data.GetStringArray("Licenses_General"), Globals.G.Types.generalLicenses).ForEach(l => SingletonBehaviour<LicenseManager>.Instance.AcquireGeneralLicense(l));
			ProcessListOfIDs(data.GetStringArray("Licenses_Jobs"), Globals.G.Types.jobLicenses).ForEach(l => SingletonBehaviour<LicenseManager>.Instance.AcquireJobLicense(l));
			ProcessListOfIDs(data.GetStringArray("Garages"), Globals.G.Types.garages).ForEach(g => SingletonBehaviour<LicenseManager>.Instance.UnlockGarage(g));
			Debug.Log("[CareerRework] All data loaded successfully.");
			return false;
		}

		// Hilfsmethode aus der Originalklasse extrahiert
		private static List<T> ProcessListOfIDs<T>(IEnumerable<string> idList, IEnumerable<T> sourceList) where T : Thing_v2
		{
			if (idList == null) return new List<T>();
			List<T> list = new List<T>();
			foreach (string id in idList)
			{
				T val = sourceList.FirstOrDefault(t => t.id == id);
				if (val != null) list.Add(val);
				else Debug.LogError("Unknown thing (" + typeof(T).Name + ") ID in save file: " + id);
			}
			return list;
		}
	}
	
	// PATCH: StartGameData_NewCareer.PrepareNewSaveData
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
			{
				saveGameData.SetFloat("Player_money", 270000f);
				saveGameData.SetBool("Tutorial_01_completed", true);
				saveGameData.SetBool("Tutorial_02_completed", true);
				saveGameData.SetBool("Tutorial_03_completed", true);
				saveGameData.SetInt("Starting_items", 0);
				session.GameData.SetBool("Difficulty_picked", true);
			}
			else
			{
				saveGameData.SetFloat("Player_money", 270000f);
				saveGameData.SetBool("Tutorial_01_completed", true);
				saveGameData.SetBool("Tutorial_02_completed", true);
				saveGameData.SetBool("Tutorial_03_completed", true);
				saveGameData.SetInt("Starting_items", 0);
				session.GameData.SetBool("Difficulty_picked", true);
			}

			return false;
		}
	}

	public static class LicenseManagerMod
	{
		public static void Init()
		{
			var harmony = new Harmony("com.yourname.licensemanagerpatch");
			harmony.PatchAll();
			Debug.Log("[CareerRework] Harmony patches applied.");

			// entfernt – ersetzt durch Patch_WorldStreamingInit

			var field = typeof(LicenseManager).GetField("TutorialGeneralLicenses", BindingFlags.Static | BindingFlags.Public);
			if (field != null)
			{
				field.SetValue(null, new List<GeneralLicenseType_v2> { GeneralLicenseType.NotSet.ToV2() });
				Debug.Log("[CareerRework] TutorialGeneralLicenses erfolgreich auf NotSet umgestellt.");
			}
			else
			{
				Debug.LogError("[CareerRework] Fehler: Feld TutorialGeneralLicenses nicht gefunden.");
			}
		}

		public static IEnumerator BlockDE2Later()
		{
			yield return new WaitForSeconds(5f);

			foreach (var loco in Object.FindObjectsOfType<TrainCar>())
            {
                 if (loco.carType == TrainCarType.LocoShunter)
				{
					CreateLocoZoneBlocker(loco);
				}
            }
            Debug.Log("[CareerRework] Alle DE2-Loks mit ZoneBlocker versehen.");
		}

        public static void CreateLocoZoneBlocker(TrainCar loco)
		{
			GameObject blockerObject = new GameObject($"LocoZoneBlocker_{loco.ID}");
			blockerObject.transform.SetParent(loco.interior);
			blockerObject.transform.localPosition = Vector3.zero;
			blockerObject.transform.localRotation = Quaternion.identity;
						
			GameObject blockedDE2 = GameObject.CreatePrimitive(PrimitiveType.Cube);
			blockedDE2.transform.SetParent(blockerObject.transform);
			//blockedDE2.transform.localPosition = new Vector3(0f, 2f, 0f);
			blockedDE2.transform.localPosition = new Vector3(0f, 1.5f, 0f);
			blockedDE2.transform.localRotation = Quaternion.identity;
			//blockedDE2.transform.localScale = new Vector3(3.2f, 4f, 7.6f);
			blockedDE2.transform.localScale = new Vector3(3.5f, 5f, 7.8f);
			blockedDE2.GetComponent<Renderer>().enabled = false;
			
			Collider col = blockedDE2.GetComponent<Collider>();
			if (col != null)
			{
				col.isTrigger = true;
			}

			LocoZoneBlocker locoZoneBlocker = blockerObject.AddComponent<LocoZoneBlocker>();
			locoZoneBlocker.blockerObjectsParent = blockerObject;
			locoZoneBlocker.cab = loco.GetComponentInChildren<CabTeleportDestination>();
			
			Debug.Log($"[CareerRework] LocoZoneBlocker auf DE2 '{loco.ID}' erstellt.");
		}
	
		public static IEnumerator MonitorAndBlockDE2s()
		{
			while (true)
			{
				foreach (var loco in Object.FindObjectsOfType<TrainCar>())
				{
					if (loco.carType == TrainCarType.LocoShunter)
					{
						// Falls kein Blocker dran ist
						if (loco.transform.Find($"LocoZoneBlocker_{loco.ID}") == null)
						{
							CreateLocoZoneBlocker(loco);
						}
					}
				}
				yield return new WaitForSeconds(10f); // alle 10 Sekunden neu scannen
			}
		}
    }

	[HarmonyPatch(typeof(WorldStreamingInit), "Awake")]
	public static class Patch_WorldStreamingInit
	{
		static void Postfix()
		{
			Debug.Log("[CareerRework] WorldStreamingInit gestartet – Starte BlockDE2Later()");
			CoroutineManager.Instance.Run(LicenseManagerMod.BlockDE2Later());
			CoroutineManager.Instance.Run(LicenseManagerMod.MonitorAndBlockDE2s());
		}
	}
}
