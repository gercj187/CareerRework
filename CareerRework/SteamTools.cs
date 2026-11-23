//FILE:		SteamTools.cs

using System;
using System.Collections.Generic;
using DV;
using DV.CabControls;
using DV.CashRegister;
using DV.ServicePenalty.UI;
using DV.ThingTypes;
using HarmonyLib;
using UnityEngine;

namespace CareerRework
{
	// =========================
	// WERKZEUG FÜR DIE DAMPFLOK
	// =========================
	// Ist die S060 die ERSTE Fahrzeuglizenz, die man kauft, gibt es Schaufel, Ölkanne und Feuerzeug geschenkt
	// (ins Fundbüro). Bereits vorhandene Werkzeuge werden nicht doppelt vergeben.
	//
	// Wer bekommt es? Immer nur der Spieler, der die Lizenz selbst am Career Manager kauft:
	// Der Kauf läuft auf SEINEM Rechner (Bezahl-Bildschirm + Kasse). Das gilt im Einzelspieler, im
	// Multiplayer und mit DVMultiplayerIP (dort ist der LicenseManager des Clients mit SEINEN Lizenzen gefüllt,
	// der Host vergibt die Lizenz auf seinem Konto) – andere Spieler bekommen nichts.
	//
	// Ablauf: Bezahl-Bildschirm (Bestätigen) merkt sich "S060, noch keine Lok-Lizenz" → Kasse meldet
	// erfolgreiche Zahlung → sobald die Lizenz beim Spieler angekommen ist (im MP etwas später), Geschenk.
	public static class SteamTools
	{
		private static readonly string[] ItemPrefabs = { "shovel", "Oiler", "lighter" };
		private const float WaitForLicenseSeconds = 30f;

		private static bool candidate;			// läuft gerade ein Bestätigen mit S060 als erster Lok-Lizenz?
		private static float pendingUntil = -1f;
		private static Runner? runner;

		/// <summary>Wird nach dem Geschenk ausgelöst (z. B. für DVMultiplayerIP). Parameter: Prefab-Namen.</summary>
		public static event Action<IReadOnlyList<string>>? Given;

		public static void Reset()
		{
			candidate = false;
			pendingUntil = -1f;
		}

		/// <summary>Bezahl-Bildschirm: vor dem Kauf prüfen (danach besitzt man die Lizenz ja schon).</summary>
		internal static void BeforeConfirm(GeneralLicenseType_v2? toBuy)
		{
			candidate = false;
			if (toBuy == null)
				return;

			var s060 = LicenseRules.GL(GeneralLicenseType.S060);
			var lm = LicenseManager.Instance;
			if (s060 == null || lm == null || toBuy != s060)
				return;

			candidate = !LicenseRules.OwnsAnyLocoLicense(lm);
		}

		internal static void AfterConfirm() => candidate = false;

		/// <summary>Kasse des Career Managers hat erfolgreich kassiert.</summary>
		internal static void OnPaid()
		{
			if (!candidate)
				return;

			candidate = false;
			pendingUntil = Time.unscaledTime + WaitForLicenseSeconds;

			if (runner == null)
			{
				var go = new GameObject("[CareerRework] Steam Tools");
				UnityEngine.Object.DontDestroyOnLoad(go);
				runner = go.AddComponent<Runner>();
			}
			runner.enabled = true;
		}

		// Wartet, bis die S060-Lizenz beim Spieler angekommen ist (Einzelspieler: sofort; MP: Host bestätigt)
		private sealed class Runner : MonoBehaviour
		{
			private float next;

			private void Update()
			{
				if (pendingUntil < 0f)
				{
					enabled = false;
					return;
				}

				if (Time.unscaledTime < next)
					return;
				next = Time.unscaledTime + 0.5f;

				var lm = LicenseManager.Instance;
				var s060 = LicenseRules.GL(GeneralLicenseType.S060);
				if (lm != null && s060 != null && lm.IsGeneralLicenseAcquired(s060))
				{
					pendingUntil = -1f;
					Give();
				}
				else if (Time.unscaledTime > pendingUntil)
				{
					pendingUntil = -1f;
					Main.Warn("S060 license did not arrive – no steam tools given.");
				}
			}
		}

		private static void Give()
		{
			var sc = StorageController.Instance;
			var specs = Globals.G?.Items?.items;
			if (sc == null || specs == null)
				return;

			var owned = OwnedPrefabNames(sc);
			var given = new List<string>();

			foreach (string prefabName in ItemPrefabs)
			{
				if (owned.Contains(prefabName))
					continue;

				InventoryItemSpec? spec = null;
				foreach (var s in specs)
					if (s != null && string.Equals(s.ItemPrefabName, prefabName, StringComparison.OrdinalIgnoreCase))
					{
						spec = s;
						break;
					}

				if (spec == null)
				{
					Main.Warn("Item not found: " + prefabName);
					continue;
				}

				try
				{
					var go = UnityEngine.Object.Instantiate(spec.gameObject, new Vector3(0f, -500f - given.Count, 0f), Quaternion.identity);
					go.name = spec.gameObject.name;

					var item = go.GetComponent<ItemBase>();
					if (item == null)
					{
						UnityEngine.Object.Destroy(go);
						continue;
					}

					var itemSpec = go.GetComponent<InventoryItemSpec>();
					if (itemSpec != null)
						itemSpec.BelongsToPlayer = true;

					sc.AddItemToLostAndFound(item, true);
					given.Add(prefabName);
				}
				catch (Exception e)
				{
					Main.Warn($"Steam tool {prefabName} could not be created: {e.Message}");
				}
			}

			Main.Info(given.Count > 0
				? "First locomotive license is the S060 → steam tools given (lost and found): " + string.Join(", ", given)
				: "First locomotive license is the S060 → steam tools already owned.");

			if (given.Count > 0)
			{
				try { Given?.Invoke(given); }
				catch (Exception e) { Main.Warn("SteamTools.Given handler failed: " + e.Message); }
			}
		}

		private static HashSet<string> OwnedPrefabNames(StorageController sc)
		{
			var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

			void Add(StorageBase? storage, bool onlyPlayers)
			{
				if (storage == null)
					return;
				foreach (var item in storage.GetStorageItemList())
				{
					var spec = item != null ? item.InventorySpecs : null;
					if (spec == null || (onlyPlayers && !spec.BelongsToPlayer))
						continue;
					if (!string.IsNullOrEmpty(spec.ItemPrefabName))
						set.Add(spec.ItemPrefabName);
				}
			}

			Add(sc.StorageInventory, false);
			Add(sc.StorageLostAndFound, false);
			Add(sc.StorageItemContainers, false);
			Add(sc.StorageWorld, true);
			return set;
		}
	}

	// =========================
	// PATCHES
	// =========================
	[HarmonyPatch(typeof(CareerManagerLicensePayingScreen), nameof(CareerManagerLicensePayingScreen.HandleInputAction))]
	internal static class Patch_LicensePayingScreen_SteamTools
	{
		private static AccessTools.FieldRef<CareerManagerLicensePayingScreen, GeneralLicenseType_v2>? generalToBuy;

		// vor anderen Mods (DVMultiplayerIP kassiert in seinem eigenen Prefix und bricht dann ab)
		[HarmonyPriority(Priority.First + 1)]
		[HarmonyBefore("DVMultiplayerIP", "Multiplayer")]
		private static void Prefix(CareerManagerLicensePayingScreen __instance, InputAction input)
		{
			if (input != InputAction.Confirm || __instance == null)
				return;

			try
			{
				generalToBuy ??= AccessTools.FieldRefAccess<CareerManagerLicensePayingScreen, GeneralLicenseType_v2>("generalLicenseToBuy");
				SteamTools.BeforeConfirm(generalToBuy(__instance));
			}
			catch (Exception e)
			{
				Main.Log("Steam tools check failed: " + e.Message);
			}
		}

		private static void Finalizer() => SteamTools.AfterConfirm();
	}

	[HarmonyPatch(typeof(CashRegisterCareerManager), nameof(CashRegisterCareerManager.Buy))]
	internal static class Patch_CashRegisterCareerManager_Buy
	{
		private static void Postfix(bool __result)
		{
			if (__result)
				SteamTools.OnPaid();
		}
	}
}
