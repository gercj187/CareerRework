//FILE:		StarterLocos.cs

using System.Collections;
using System.Collections.Generic;
using System.Linq;
using DV;
using DV.ThingTypes;
using DV.Utils;
using UnityEngine;

namespace CareerRework
{
	// =========================
	// STARTER-LOKS
	// =========================
	// Genau einmal pro Karriere: direkt nach dem Start einer neuen Karriere (Merker im Savegame).
	// Dabei wird auch die gewählte Startuhrzeit gesetzt.
	// Nur Host / Einzelspieler – im Multiplayer synchronisiert die MP-Mod die Loks zu den Clients.
	internal static class StarterLocos
	{
		private const string TrackPrimary = "[Y]_[SM]_[T1-01-P]";
		private const string TrackSecondary = "[Y]_[SM]_[T1-02-P]";

		public static IEnumerator SpawnAfterLoad()
		{
			while (CarSpawner.Instance == null || CarSpawner.Instance.PoolSetupInProgress || !AStartGameData.carsAndJobsLoadingFinished)
				yield return null;

			if (SaveState.StarterLocosSpawned || !SaveState.Loaded || !Mp.CanModifyWorld)
				yield break;

			var e = Main.E;
			SetStartTime(e.startHour);

			int spawned = 0;
			if (Spawn(TrackPrimary, e.starterLocoPrimary)) spawned++;
			if (Spawn(TrackSecondary, e.starterLocoSecondary)) spawned++;

			// auch wenn ein Gleis fehlt: nicht bei jedem Laden erneut versuchen
			SaveState.StarterLocosSpawned = true;
			Main.Info($"Starter locomotives spawned: {spawned} ({e.starterLocoPrimary}, {e.starterLocoSecondary}).");
		}

		// Die Uhrzeit im Spiel kommt aus dem Wettersystem, nicht aus "Starting_time_and_date" im Savegame
		// (das zeigt nur die Spielstand-Info an). Deshalb wird sie bei einer neuen Karriere direkt gesetzt.
		private static void SetStartTime(int hour)
		{
			try
			{
				var manager = SingletonBehaviour<DV.WeatherSystem.WeatherDriver>.Instance?.manager;
				if (manager == null)
				{
					Main.Warn("Weather system not found – start time not set.");
					return;
				}

				manager.SetTimeOfDay((hour % 24) / 24f);
				Main.Info($"Start time set to {hour % 24:00}:00.");
			}
			catch (System.Exception ex)
			{
				Main.Warn("Setting the start time failed: " + ex.Message);
			}
		}

		private static bool Spawn(string trackName, StarterLocoType type)
		{
			var track = SingletonBehaviour<RailTrackRegistryBase>.Instance.AllTracks.FirstOrDefault(t => t.name == trackName);
			if (track == null)
			{
				Main.Warn("Track not found: " + trackName);
				return false;
			}

			TrainCarType carType = type switch
			{
				StarterLocoType.DE2 => TrainCarType.LocoShunter,
				StarterLocoType.S060 => TrainCarType.LocoS060,
				_ => TrainCarType.LocoDM3,
			};

			var livery = Globals.G.Types.Liveries.FirstOrDefault(l => l.v1 == carType);
			if (livery == null)
			{
				Main.Warn("Livery not found for " + carType);
				return false;
			}

			var cars = CarSpawner.Instance.SpawnCarTypesOnTrack(
				new List<TrainCarLivery> { livery },
				new List<bool> { false },
				track,
				preventAutoCoupleOnLastCars: true,
				applyHandbrakeOnLastCars: true,
				playerSpawnedCars: false);

			return cars != null && cars.Count > 0;
		}
	}
}
