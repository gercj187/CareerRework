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
using DV.Localization.Debug;
using DV.Scenarios.Common;
using DV.JObjectExtstensions;
using DV.Items;
using DV.ThingTypes;
using DV.ThingTypes.TransitionHelpers;
using DV.UserManagement;
using DV.Utils;
using DV.Player;
using HarmonyLib;
using UnityModManagerNet;
using UnityEngine;
using UnityEngine.SceneManagement;
using Debug = UnityEngine.Debug;
using Object = UnityEngine.Object;
using static DV.Items.StartingItems;


namespace CareerRework
{
	[HarmonyPatch(typeof(LicenseManager), "LoadData")]
	public static class LicenseManager_LoadData_Patch
	{
		static void Postfix(LicenseManager __instance)
		{
			// Job Licenses Prices									vanilla			modded
			JobLicenses.Shunting.ToV2().price = 25000f;         //	1000			25000
			JobLicenses.LogisticalHaul.ToV2().price = 100000f;   //	20000			100000
			JobLicenses.FreightHaul.ToV2().price = 200000f;     //	OWNED			200000
			JobLicenses.TrainLength1.ToV2().price = 200000f;     //	10000			200000
			JobLicenses.TrainLength2.ToV2().price = 300000f;    //	20000			300000
			JobLicenses.Fragile.ToV2().price = 300000f;         //	10000			300000
			JobLicenses.Hazmat1.ToV2().price = 400000f;         //	40000			400000
			JobLicenses.Hazmat2.ToV2().price = 500000f;         //	130000			500000
			JobLicenses.Hazmat3.ToV2().price = 8000000f;         //	290000			8000000
			JobLicenses.Military1.ToV2().price = 1000000f;       //	100000			1000000
			JobLicenses.Military2.ToV2().price = 2000000f;      //	30000			2000000
			JobLicenses.Military3.ToV2().price = 4000000f;      //	400000			4000000

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
			GeneralLicenseType.TrainDriver.ToV2().price = 50000f;      //	OWNED			50000
			GeneralLicenseType.ManualService.ToV2().price = 200000f;     //	20000			200000
			GeneralLicenseType.ConcurrentJobs1.ToV2().price = 200000f;   //	10000			200000
			GeneralLicenseType.ConcurrentJobs2.ToV2().price = 300000f;   //	20000			300000
			GeneralLicenseType.MultipleUnit.ToV2().price = 400000f;      //	30000			400000
			GeneralLicenseType.Dispatcher1.ToV2().price = 400000f;       //	10000			400000
			GeneralLicenseType.MuseumCitySouth.ToV2().price = 300000f;  //	15000			300000

			// Career Licenses Conditions
			GeneralLicenseType.ManualService.ToV2().requiredJobLicense = JobLicenses.Shunting.ToV2();
			GeneralLicenseType.ManualService.ToV2().requiredGeneralLicense = GeneralLicenseType.NotSet.ToV2();
			GeneralLicenseType.ConcurrentJobs1.ToV2().requiredJobLicense = JobLicenses.LogisticalHaul.ToV2();
			GeneralLicenseType.Dispatcher1.ToV2().requiredGeneralLicense = GeneralLicenseType.ConcurrentJobs1.ToV2();
			GeneralLicenseType.ConcurrentJobs2.ToV2().requiredGeneralLicense = GeneralLicenseType.Dispatcher1.ToV2();
			GeneralLicenseType.MultipleUnit.ToV2().requiredGeneralLicense = GeneralLicenseType.NotSet.ToV2();
			GeneralLicenseType.MultipleUnit.ToV2().requiredJobLicense = JobLicenses.TrainLength2.ToV2();
			GeneralLicenseType.MuseumCitySouth.ToV2().requiredGeneralLicense = GeneralLicenseType.ManualService.ToV2();

			// Loco Licenses Prices									vanilla			modded
			GeneralLicenseType.DE2.ToV2().price = 75000f;      //	OWNED			75000
			GeneralLicenseType.DM3.ToV2().price = 75000f;      //	30000			100000
			GeneralLicenseType.S060.ToV2().price = 75000f;     //	20000			100000
			GeneralLicenseType.DH4.ToV2().price = 400000f;      //	50000			400000
			GeneralLicenseType.SH282.ToV2().price = 750000f;    //	50000			750000
			GeneralLicenseType.DE6.ToV2().price = 1000000f;     //	200000			1000000

			// Loco Licenses Conditions
			GeneralLicenseType.DE2.ToV2().requiredJobLicense = JobLicenses.Shunting.ToV2();
			GeneralLicenseType.DE2.ToV2().requiredGeneralLicense = GeneralLicenseType.NotSet.ToV2();
			GeneralLicenseType.DM3.ToV2().requiredJobLicense = JobLicenses.Shunting.ToV2();
			GeneralLicenseType.DM3.ToV2().requiredGeneralLicense = GeneralLicenseType.NotSet.ToV2();
			GeneralLicenseType.S060.ToV2().requiredGeneralLicense = GeneralLicenseType.NotSet.ToV2();
			GeneralLicenseType.S060.ToV2().requiredJobLicense = JobLicenses.Shunting.ToV2();
			GeneralLicenseType.DH4.ToV2().requiredJobLicense = JobLicenses.TrainLength1.ToV2();
			GeneralLicenseType.DE6.ToV2().requiredGeneralLicense = GeneralLicenseType.NotSet.ToV2();
			GeneralLicenseType.DE6.ToV2().requiredJobLicense = JobLicenses.TrainLength2.ToV2();
			GeneralLicenseType.SH282.ToV2().requiredGeneralLicense = GeneralLicenseType.NotSet.ToV2();
			GeneralLicenseType.SH282.ToV2().requiredJobLicense = JobLicenses.TrainLength2.ToV2();
		}
		
		static bool Prefix(SaveGameData data)
		{
			Debug.Log("[CareerRework] Loading GeneralLicenses, JobLicenses, and Garages...");
			ProcessListOfIDs(data.GetStringArray("Licenses_General"), Globals.G.Types.generalLicenses).ForEach(l => SingletonBehaviour<LicenseManager>.Instance.AcquireGeneralLicense(l));
			ProcessListOfIDs(data.GetStringArray("Licenses_Jobs"), Globals.G.Types.jobLicenses).ForEach(l => SingletonBehaviour<LicenseManager>.Instance.AcquireJobLicense(l));
			ProcessListOfIDs(data.GetStringArray("Garages"), Globals.G.Types.garages).ForEach(g => SingletonBehaviour<LicenseManager>.Instance.UnlockGarage(g));
			Debug.Log("[CareerRework] All data loaded successfully.");
			return false;
		}

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
				//saveGameData.SetFloat("Player_money", 155000f);
				saveGameData.SetFloat("Player_money", Main.settings?.startingMoney ?? 155000f);
				saveGameData.SetBool("Tutorial_01_completed", true);
				saveGameData.SetBool("Tutorial_02_completed", true);
				saveGameData.SetBool("Tutorial_03_completed", true);
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
				saveGameData.SetBool("Tutorial_01_completed", true);
				saveGameData.SetBool("Tutorial_02_completed", true);
				saveGameData.SetBool("Tutorial_03_completed", true);
				session.GameData.SetBool("Difficulty_picked", value: false);
				//saveGameData.SetFloat("Player_money", 155000f);
				saveGameData.SetFloat("Player_money", Main.settings?.startingMoney ?? 155000f);
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

			var field = typeof(LicenseManager).GetField("TutorialGeneralLicenses", BindingFlags.Static | BindingFlags.Public);
			if (field != null)
			{
				field.SetValue(null, new List<GeneralLicenseType_v2> { GeneralLicenseType.NotSet.ToV2() });
			}
			else
			{
			}
		}

        public static void CreateLocoZoneBlocker(TrainCar loco)
		{
			if (loco == null || loco.interior == null) return;

			if (loco.interior.Find($"LocoZoneBlocker_{loco.ID}") != null) return;
						
			GameObject blockedDE2 = GameObject.CreatePrimitive(PrimitiveType.Cube);
			blockedDE2.name = $"LocoZoneBlocker_{loco.ID}";
			blockedDE2.transform.SetParent(loco.interior);
			blockedDE2.transform.localPosition = new Vector3(0f, 2.0f, 0f);
			blockedDE2.transform.localRotation = Quaternion.identity;
			blockedDE2.transform.localScale = new Vector3(3.5f, 3.9f, 7.8f);
			blockedDE2.GetComponent<Renderer>().enabled = false;
			
			Collider col = blockedDE2.GetComponent<Collider>();
			if (col != null)
			{
				col.isTrigger = true;
			}

			LocoZoneBlocker locoZoneBlocker = blockedDE2.AddComponent<LocoZoneBlocker>();
			locoZoneBlocker.blockerObjectsParent = blockedDE2;
			locoZoneBlocker.cab = loco.GetComponentInChildren<CabTeleportDestination>();
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
		}
    }
	
    public static class LocoSpawner
    {
        public static IEnumerator SpawnStarterLocoAfterLoad()
        {
            while (CarSpawner.Instance == null || CarSpawner.Instance.PoolSetupInProgress)
                yield return null;

            var trackName = "[Y]_[SM]_[T1-02-P]";
            var track = SingletonBehaviour<RailTrackRegistryBase>.Instance.AllTracks
                .FirstOrDefault(t => t.name == trackName);

            if (track == null)
            {
                Debug.LogError($"[CareerRework] Track '{trackName}' not found!");
                yield break;
            }

            var selectedType = Main.settings?.selectedStarterLoco == StarterLocoType.S060
                ? TrainCarType.LocoS060
                : TrainCarType.LocoDM3;

            var livery = Globals.G.Types.Liveries.Find(l => l.v1 == selectedType);
            if (livery == null)
            {
                Debug.LogError($"[CareerRework] Livery für {selectedType} not loaded!");
                yield break;
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
                Debug.Log($"[CareerRework] Spawned starter {selectedType} at SteelMill Yard Track 2.");
            else
                Debug.LogWarning($"[CareerRework] cant spawn {selectedType}.");
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

		static void Postfix(TrainCar __result, bool playerSpawnedCar)
		{
			if (__result == null || playerSpawnedCar || !__result.IsLoco) return;

			if (__result.carType == TrainCarType.LocoShunter && __result.transform.Find($"LocoZoneBlocker_{__result.ID}") == null)
			{
				LicenseManagerMod.CreateLocoZoneBlocker(__result);
			}
		}		
	}
	
	[HarmonyPatch]
	internal class ItemsPatch
	{
		private static readonly string[] includes = new string[] { "Oiler", "lighter", "shovel" };
		private static List<StartingItem> items = new List<StartingItem>();

		[HarmonyPatch(typeof(StartingItemsController), nameof(StartingItemsController.AddStartingItems))]
		[HarmonyPrefix]
		public static void PatchGetStartingItems()
		{
			Debug.Log("[CareerRework] Patch get starting items");
			items.Clear();

			if (Main.settings?.selectedStarterLoco != StarterLocoType.S060)
			{
				//Debug.Log("[CareerRework] Keine S060 in den Settings – keine Items hinzugefügt.");
				return;
			}

			var allDefs = AccessTools.Field(typeof(ItemsConfig), "startingItems")
				.GetValue(Globals.G.Items) as List<StartingItems>;

			if (allDefs == null)
			{
				//Debug.LogError("[CareerRework] startingItems konnte nicht geladen werden!");
				return;
			}

			var basic = allDefs.FirstOrDefault(i => i.startingItemsType == GameParams.StartingItemsType.Basic);
			var expanded = allDefs.FirstOrDefault(i => i.startingItemsType == GameParams.StartingItemsType.Expanded);

			if (basic == null || expanded == null)
			{
				//Debug.LogError("[CareerRework] Basic oder Expanded StartingItems nicht gefunden!");
				return;
			}

			var basicItems = basic.GetStartingItems();
			var expandedItems = expanded.GetStartingItems();

			Debug.Log($"{basicItems.Count} - {expandedItems.Count}");

			foreach (var i in expandedItems)
			{
				if (includes.Contains(i.ItemPrefabName) && !basicItems.Contains(i))
				{
					items.Add(i);
				}
			}

			basicItems.AddRange(items);
			expandedItems.RemoveAll(i => !items.Contains(i));

			Debug.Log($"[CareerRework] {items.Count} additional Items for S060 in your inventory.");
		}
	}

	[HarmonyPatch(typeof(WorldStreamingInit), "Awake")]
	public static class Patch_WorldStreamingInit
	{
		static void Postfix()
		{
			CoroutineManager.Instance.Run(LicenseManagerMod.BlockDE2Later());
			CoroutineManager.Instance.Run(LocoSpawner.SpawnStarterLocoAfterLoad());
		}
	}
}
