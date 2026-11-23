//FILE:		De2Blocker.cs

using System.Collections.Generic;
using DV.ThingTypes;
using HarmonyLib;
using UnityEngine;

namespace CareerRework
{
	// =========================
	// DE2 OHNE LIZENZ SPERREN
	// =========================
	// Vanilla hat für die DE2 keinen Lok-Blocker (die Lizenz gibt es im Vanilla-Spiel gratis).
	// CareerRework baut deshalb an jede DE2 einen Blocker wie bei den anderen Loks – aber nur, solange die
	// DE2-Lizenz fehlt: Lok nicht betretbar, Hinweis "Lizenz fehlt" beim Draufzeigen, Führerstand gesperrt.
	//
	// Bisher: alle 2 s alle Wagen durchsucht und den Blocker neu erzeugt, sobald er fehlte. Der Vanilla-Blocker
	// zerstört sich aber selbst, wenn die Lizenz vorhanden ist → endlos neu erzeugt (über 600 Log-Zeilen in Minuten).
	// Jetzt: jede Lok wird genau einmal behandelt (CarSpawned-Ereignis + einmaliger Durchlauf nach dem Laden),
	// mit vorhandener Lizenz passiert gar nichts. Kein Dauer-Polling, keine Logs pro Lok.
	internal static class De2Blocker
	{
		private static readonly HashSet<TrainCar> handled = new();
		// gesperrte DE2 → ihr Blocker (Einstieg ins Führerhaus gesperrt, solange er existiert)
		private static readonly Dictionary<TrainCar, LocoZoneBlocker> zones = new();
		private static readonly List<TrainCar> pending = new();
		private static readonly List<(LocoZoneBlocker zone, TrainCar car)> waitingForCab = new();
		private static CarSpawner? subscribedTo;
		private static Runner? runner;

		private static bool LicenseOwned
		{
			get
			{
				var lm = LicenseManager.Instance;
				var de2 = LicenseRules.GL(GeneralLicenseType.DE2);
				return lm != null && de2 != null && lm.IsGeneralLicenseAcquired(de2);
			}
		}

		public static void Start()
		{
			Stop();

			var spawner = CarSpawner.Instance;
			if (spawner == null)
				return;

			spawner.CarSpawned += OnCarSpawned;
			subscribedTo = spawner;

			var cars = spawner.AllLocos;
			if (cars != null)
				for (int i = 0; i < cars.Count; i++)
					OnCarSpawned(cars[i]);
		}

		public static void Stop()
		{
			if (subscribedTo != null)
				subscribedTo.CarSpawned -= OnCarSpawned;
			subscribedTo = null;
			handled.Clear();
			zones.Clear();
			pending.Clear();
			waitingForCab.Clear();
		}

		private static void OnCarSpawned(TrainCar car)
		{
			if (car == null || car.carType != TrainCarType.LocoShunter || handled.Contains(car))
				return;
			if (LicenseOwned)
				return;

			if (!TryCreate(car))
			{
				pending.Add(car);		// Innenraum noch nicht da → kurz später erneut
				EnsureRunner();
			}
		}

		// ---------- Aufbau ----------
		// Wie bei den anderen Loks: ein fester Block, den der Spieler nicht betreten kann, plus Hover-Hinweis
		// "Lizenz fehlt" und gesperrter Einstieg ins Führerhaus.
		//
		// Physik – WICHTIG: Der Block darf nie zum Rigidbody der Lok gehören und nie auf einer Ebene liegen, die mit
		// Gleis/Boden/Wagen kollidiert, sonst wird die Lok weggedrückt ("fliegt weg"). Deshalb exakt wie das Spiel:
		// Block am Innenraum, Physik-Ebene von einem Original-Lok-Blocker übernommen, unten nie tiefer als das
		// Grundmodell. Mit Lizenz (oder sobald sie gekauft wird) entfernt das Spiel alles selbst.
		private static bool TryCreate(TrainCar car)
		{
			if (car.interior == null)
				return false;

			handled.Add(car);

			string name = "LocoZoneBlocker_" + car.ID;
			if (car.interior.Find(name) != null)
				return true;

			// Form (Lok-Koordinaten): Länge vom Box-Collider des Grundmodells, Breite und Höhe aus den Außenmaßen
			// ALLER festen Collider der Lok UND der Einstiegszonen des Führerhauses (Trigger der
			// CabTeleportDestination, etwas größer als die Kabine, damit man von weitem hineinzielen kann).
			// Der Blocker muss beides umschließen, sonst trifft der Teleport-Strahl zuerst die Einstiegszone.
			var source = FindBodyCollider(car);
			Vector3 center, size;
			if (source != null)
			{
				var t = source.transform;
				center = car.transform.InverseTransformPoint(t.TransformPoint(source.center));
				Vector3 ls = t.lossyScale, cs = car.transform.lossyScale;
				size = new Vector3(
					Mathf.Abs(source.size.x * ls.x / cs.x),
					Mathf.Abs(source.size.y * ls.y / cs.y),
					Mathf.Abs(source.size.z * ls.z / cs.z));
			}
			else
			{
				center = new Vector3(0f, 2f, 0f);			// Rückfall: bisherige Maße
				size = new Vector3(3.5f, 3.9f, 7.8f);
			}

			Bounds? cabZonesOnly = TryGetCabZoneBounds(car, out var cabZones) ? cabZones : (Bounds?)null;
			if (TryGetOuterBounds(car, out var outer))
			{
				const float margin = 0.1f;		// je Seite
				center.x = outer.center.x;
				size.x = Mathf.Max(size.x, outer.size.x) + 2f * margin;
				// Höhe: unten NIE tiefer als das Grundmodell (sonst ragt der Block bis an Räder/Gleis),
				// oben bis über das höchste Teil (Führerhaus/Einstiegszone)
				float yMin = center.y - size.y / 2f;
				float yMax = Mathf.Max(center.y + size.y / 2f, outer.max.y + margin);
				center.y = (yMin + yMax) / 2f;
				size.y = yMax - yMin;
				// Länge nur erweitern, wenn eine Einstiegszone übers Grundmodell hinausragt
				float zMin = Mathf.Min(center.z - size.z / 2f, outer.min.z - margin);
				float zMax = Mathf.Max(center.z + size.z / 2f, outer.max.z + margin);
				if (cabZonesOnly.HasValue)
				{
					zMin = Mathf.Min(center.z - size.z / 2f, cabZonesOnly.Value.min.z - margin);
					zMax = Mathf.Max(center.z + size.z / 2f, cabZonesOnly.Value.max.z + margin);
				}
				else
				{
					zMin = center.z - size.z / 2f;
					zMax = center.z + size.z / 2f;
				}
				center.z = (zMin + zMax) / 2f;
				size.z = zMax - zMin;
			}

			// Harte Grenze: nie mehr als 1,5 m breiter/höher/länger als das Grundmodell
			if (source != null)
			{
				var t = source.transform;
				Vector3 bc = car.transform.InverseTransformPoint(t.TransformPoint(source.center));
				Vector3 ls = t.lossyScale, cs = car.transform.lossyScale;
				Vector3 bs = new Vector3(Mathf.Abs(source.size.x * ls.x / cs.x), Mathf.Abs(source.size.y * ls.y / cs.y), Mathf.Abs(source.size.z * ls.z / cs.z));
				size = new Vector3(Mathf.Min(size.x, bs.x + 1.5f), Mathf.Min(size.y, bs.y + 1.5f), Mathf.Min(size.z, bs.z + 1.5f));
				center = new Vector3(
					Mathf.Clamp(center.x, bc.x - 0.75f, bc.x + 0.75f),
					Mathf.Clamp(center.y, bc.y - 0.75f, bc.y + 0.75f),
					Mathf.Clamp(center.z, bc.z - 0.75f, bc.z + 0.75f));
			}
			Main.Log($"DE2 blocker {car.ID}: center {center}, size {size}.");

			// 1) Hover-Hinweis "Lizenz fehlt" (Trigger, das Spiel hängt ihn an den Innenraum und hält ihn an der Lok)
			var hover = new GameObject(name);
			hover.transform.SetParent(car.interior, false);

			// 2) fester Block – GENAU wie die Blocker des Spiels bei den anderen Loks: am Innenraum (NICHT am
			//    Rigidbody der Lok) und auf derselben Physik-Ebene wie die Original-Blocker. Diese Ebene stößt nur
			//    den Spieler ab, nicht Lok, Gleis oder Boden. Ohne gefundene Original-Ebene gibt es keinen festen
			//    Block (nur Hinweis + gesperrter Einstieg) – lieber durchlässig als eine wegfliegende Lok.
			var solid = new GameObject("CareerRework_DE2Blocker");
			solid.transform.SetParent(hover.transform, false);
			int layer = VanillaBlockerLayer();
			if (layer >= 0)
			{
				solid.layer = layer;
				var box = solid.AddComponent<BoxCollider>();
				box.center = center;
				box.size = size;
				foreach (var c in car.GetComponentsInChildren<Collider>(true))		// zusätzliche Sicherheit
					if (c != null && c != box && c.enabled && c.gameObject.activeInHierarchy)
						Physics.IgnoreCollision(box, c, true);
			}

			var trigger = hover.AddComponent<BoxCollider>();
			trigger.isTrigger = true;
			trigger.center = center;
			trigger.size = size + new Vector3(0.1f, 0.1f, 0.1f);		// knapp größer → der Mauszeiger trifft zuerst ihn

			var zone = hover.AddComponent<LocoZoneBlocker>();
			zones[car] = zone;
			zone.blockerObjectsParent = solid;		// wird beim Freischalten vom Spiel entfernt
			zone.cab = CabOf(car);
			if (zone.cab == null)
			{
				waitingForCab.Add((zone, car));	// Führerstand wird ggf. erst beim Näherkommen geladen
				EnsureRunner();
			}

			return true;
		}

		/// <summary>Größter fester Box-Collider der Lok (ohne Innenraum) = Grundmodell.</summary>
		private static BoxCollider? FindBodyCollider(TrainCar car)
		{
			BoxCollider? best = null;
			float bestVolume = 0f;
			foreach (var c in car.GetComponentsInChildren<BoxCollider>(true))
			{
				if (c == null || c.isTrigger || c.gameObject.name.StartsWith("CareerRework_"))
					continue;
				if (car.interior != null && c.transform.IsChildOf(car.interior))
					continue;
				Vector3 s = Vector3.Scale(c.size, c.transform.lossyScale);
				float v = Mathf.Abs(s.x * s.y * s.z);
				if (v > bestVolume)
				{
					bestVolume = v;
					best = c;
				}
			}
			return best;
		}

		/// <summary>Außenmaße aller festen Collider der Lok (ohne Innenraum) in Lok-Koordinaten.</summary>
		private static bool TryGetOuterBounds(TrainCar car, out Bounds result)
		{
			result = default;
			bool any = false;
			var root = car.transform;
			var corners = new Vector3[8];

			foreach (var c in car.GetComponentsInChildren<Collider>(true))
			{
				if (c == null || !c.enabled || c.gameObject.name.StartsWith("CareerRework_"))
					continue;
				if (c.isTrigger && !IsCabZone(c.transform))
					continue;		// Trigger nur, wenn es eine Einstiegszone ist
				if (car.interior != null && c.transform.IsChildOf(car.interior))
					continue;

				if (!c.gameObject.activeInHierarchy || !AddCorners(root, c, corners) || !Plausible(corners))
					continue;
				for (int i = 0; i < 8; i++)
				{
					if (!any) { result = new Bounds(corners[i], Vector3.zero); any = true; }
					else result.Encapsulate(corners[i]);
				}
			}

			// Einstiegszonen, die am Innenraum hängen, ebenfalls einschließen
			if (TryGetCabZoneBounds(car, out var cz))
			{
				if (!any) { result = cz; any = true; }
				else { result.Encapsulate(cz.min); result.Encapsulate(cz.max); }
			}
			return any;
		}

		/// <summary>Außenmaße aller Einstiegszonen (CabTeleportDestination-Collider) in Lok-Koordinaten.</summary>
		private static bool TryGetCabZoneBounds(TrainCar car, out Bounds result)
		{
			result = default;
			bool any = false;
			var root = car.transform;
			var corners = new Vector3[8];
			var list = new List<CabTeleportDestination>(car.GetComponentsInChildren<CabTeleportDestination>(true));
			if (car.interior != null)
				list.AddRange(car.interior.GetComponentsInChildren<CabTeleportDestination>(true));

			foreach (var dest in list)
			{
				if (dest == null)
					continue;
				foreach (var c in dest.GetComponentsInChildren<Collider>(true))
				{
					if (c == null || !c.enabled || !c.gameObject.activeInHierarchy)
						continue;
					if (!AddCorners(root, c, corners) || !Plausible(corners))
						continue;
					for (int i = 0; i < 8; i++)
					{
						if (!any) { result = new Bounds(corners[i], Vector3.zero); any = true; }
						else result.Encapsulate(corners[i]);
					}
				}
			}
			return any;
		}

		// Gehört der Collider zu einer Einstiegszone? (auch inaktive Eltern – ältere Unity-Version ohne
		// GetComponentInParent(includeInactive))
		private static bool IsCabZone(Transform? t)
		{
			for (; t != null; t = t.parent)
				if (t.GetComponent<CabTeleportDestination>() != null)
					return true;
			return false;
		}

		// Nur Collider, die wirklich an der Lok sitzen (Lok-Koordinaten: Breite ±3 m, Höhe -1…6 m, Länge ±8 m).
		// Inaktive bzw. noch nicht platzierte Collider melden teils Positionen am Weltursprung – ohne diese Prüfung
		// wurde der Block kilometergroß und schief.
		private static bool Plausible(Vector3[] corners)
		{
			for (int i = 0; i < 8; i++)
			{
				var p = corners[i];
				if (Mathf.Abs(p.x) > 3f || p.y < -1f || p.y > 6f || Mathf.Abs(p.z) > 8f)
					return false;
			}
			return true;
		}

		/// <summary>8 Eckpunkte eines Colliders in Lok-Koordinaten (false = unbrauchbar, z. B. inaktiv).</summary>
		private static bool AddCorners(Transform root, Collider c, Vector3[] corners)
		{
			int k = 0;
			if (c is BoxCollider b)
			{
				Vector3 h = b.size * 0.5f;
				for (int x = -1; x <= 1; x += 2)
					for (int y = -1; y <= 1; y += 2)
						for (int z = -1; z <= 1; z += 2)
							corners[k++] = root.InverseTransformPoint(b.transform.TransformPoint(b.center + Vector3.Scale(h, new Vector3(x, y, z))));
				return true;
			}

			// andere Collider: Welt-Bounds (leicht großzügig); inaktive liefern 0
			var wb = c.bounds;
			if (wb.size == Vector3.zero)
				return false;
			for (int x = -1; x <= 1; x += 2)
				for (int y = -1; y <= 1; y += 2)
					for (int z = -1; z <= 1; z += 2)
						corners[k++] = root.InverseTransformPoint(wb.center + Vector3.Scale(wb.extents, new Vector3(x, y, z)));
			return true;
		}

		// Physik-Ebene für den festen Block – anhand der Kollisionsmatrix des Spiels geprüft:
		//   MUSS mit dem Spieler kollidieren (sonst läuft man durch),
		//   darf NICHT mit Default / Train_Big_Collider / Terrain kollidieren (sonst wird die Lok weggedrückt),
		//   möglichst auch nicht mit Train_Walkable.
		// Reihenfolge der Kandidaten: Ebene der Original-Lok-Blocker, Train_Walkable, alle übrigen benannten Ebenen.
		// Das Ergebnis steht immer im Log.
		private static int blockerLayer = -2;

		private static int VanillaBlockerLayer()
		{
			if (blockerLayer != -2)
				return blockerLayer;
			blockerLayer = -1;

			int player = LayerMask.NameToLayer("Player");
			if (player < 0)
			{
				Main.Warn("DE2 blocker: layer 'Player' not found – no solid blocker (hint and locked cab entry only).");
				return blockerLayer;
			}

			var hard = new List<int>();
			foreach (var n in new[] { "Default", "Train_Big_Collider", "Terrain" })
			{
				int l = LayerMask.NameToLayer(n);
				if (l >= 0) hard.Add(l);
			}
			int walkable = LayerMask.NameToLayer("Train_Walkable");

			var candidates = new List<int>();
			int vanilla = FindVanillaLayer();
			if (vanilla >= 0) candidates.Add(vanilla);
			if (walkable >= 0 && !candidates.Contains(walkable)) candidates.Add(walkable);
			for (int l = 0; l < 32; l++)
				if (!candidates.Contains(l) && !string.IsNullOrEmpty(LayerMask.LayerToName(l)))
					candidates.Add(l);

			bool Fits(int l, bool avoidWalkable)
			{
				if (Physics.GetIgnoreLayerCollision(l, player))
					return false;									// Spieler muss abprallen
				foreach (var h in hard)
					if (!Physics.GetIgnoreLayerCollision(l, h))
						return false;								// darf Lok/Gleis/Boden nicht berühren
				if (avoidWalkable && walkable >= 0 && l != walkable && !Physics.GetIgnoreLayerCollision(l, walkable))
					return false;
				return true;
			}

			foreach (bool strict in new[] { true, false })
				foreach (var l in candidates)
					if (Fits(l, strict))
					{
						blockerLayer = l;
						Main.Info($"DE2 blocker layer: {l} ({LayerMask.LayerToName(l)}){(l == vanilla ? " – same as vanilla loco blockers" : "")}.");
						return blockerLayer;
					}

			Main.Warn($"DE2 blocker: no safe physics layer found (vanilla layer {vanilla}) – no solid blocker (hint and locked cab entry only).");
			return blockerLayer;
		}

		// Ebene der Original-Lok-Blocker (z. B. am DM3-Prefab)
		private static int FindVanillaLayer()
		{
			try
			{
				foreach (var z in Resources.FindObjectsOfTypeAll<LocoZoneBlocker>())
				{
					if (z == null || z.blockerObjectsParent == null || z.gameObject.name.StartsWith("LocoZoneBlocker_"))
						continue;		// eigene überspringen
					foreach (var c in z.blockerObjectsParent.GetComponentsInChildren<Collider>(true))
						if (c != null && !c.isTrigger)
							return c.gameObject.layer;
				}
			}
			catch (System.Exception e)
			{
				Main.Log("Vanilla loco blocker layer: " + e.Message);
			}
			return -1;
		}

		// Einstieg ins Führerhaus: so wie das Spiel ihn selbst verwaltet (TrainCar.cabTeleportDestination)
		private static CabTeleportDestination? CabOf(TrainCar car)
		{
			var cab = car.cabTeleportDestination;
			if (cab == null)
				cab = car.GetComponentInChildren<CabTeleportDestination>(true);
			if (cab == null && car.interior != null)
				cab = car.interior.GetComponentInChildren<CabTeleportDestination>(true);
			return cab;
		}

		/// <summary>Ist diese Lok gerade durch unseren Blocker gesperrt? (Blocker verschwindet mit der Lizenz)</summary>
		internal static bool TryGetZone(TrainCar? car, out LocoZoneBlocker zone)
		{
			zone = null!;
			if (car == null || !zones.TryGetValue(car, out var z))
				return false;
			if (z == null)
			{
				zones.Remove(car);		// Lizenz gekauft → Blocker hat sich entfernt
				return false;
			}
			zone = z;
			return true;
		}

		private static void EnsureRunner()
		{
			if (runner != null)
			{
				runner.enabled = true;
				return;
			}

			var go = new GameObject("[CareerRework] DE2 Blocker");
			Object.DontDestroyOnLoad(go);
			runner = go.AddComponent<Runner>();
		}

		// Läuft nur, solange es offene Loks/Führerstände gibt (danach deaktiviert)
		private sealed class Runner : MonoBehaviour
		{
			private float next;

			private void Update()
			{
				if (pending.Count == 0 && waitingForCab.Count == 0)
				{
					enabled = false;
					return;
				}

				if (Time.unscaledTime < next)
					return;
				next = Time.unscaledTime + 2f;

				if (LicenseOwned)
				{
					pending.Clear();
					waitingForCab.Clear();
				}

				for (int i = pending.Count - 1; i >= 0; i--)
				{
					var car = pending[i];
					if (car == null || handled.Contains(car) || TryCreate(car))
						pending.RemoveAt(i);
				}

				for (int i = waitingForCab.Count - 1; i >= 0; i--)
				{
					var (zone, car) = waitingForCab[i];
					if (zone == null || car == null)
					{
						waitingForCab.RemoveAt(i);
						continue;
					}

					var cab = CabOf(car);
					if (cab != null)
					{
						zone.cab = cab;
						waitingForCab.RemoveAt(i);
					}
				}

				if (pending.Count == 0 && waitingForCab.Count == 0)
					enabled = false;
			}
		}
	}

	// =========================
	// EINSTIEG INS FÜHRERHAUS SPERREN (ohne Lizenz)
	// =========================
	// Der Blocker umschließt die Lok, aber durch Lehnen ragt die Kamera immer in ihn hinein – dann trifft der
	// Zeiger die Kabinenwand bzw. Einstiegszone von innen. Deshalb gilt zusätzlich: Solange unser Blocker an der
	// Lok existiert, ist jeder Einstieg ins Führerhaus verboten und zeigt den Hinweis des Blockers (Lizenz fehlt).
	// Nach dem Lizenzkauf entfernt das Spiel den Blocker → Einstieg wie gewohnt.
	[HarmonyPatch(typeof(CabTeleportDestination))]
	internal static class Patch_CabTeleportDestination_De2
	{
		private static readonly AccessTools.FieldRef<CabTeleportDestination, TrainCar>? carRef =
			AccessTools.FieldRefAccess<CabTeleportDestination, TrainCar>("car");

		[HarmonyPostfix]
		[HarmonyPatch(nameof(CabTeleportDestination.IsTeleportAllowed))]
		private static void IsTeleportAllowed(CabTeleportDestination __instance, ref bool __result)
		{
			if (__result && carRef != null && De2Blocker.TryGetZone(carRef(__instance), out _))
				__result = false;
		}

		// Text beim Draufzeigen ("Press F to enter") → Hinweis des Blockers
		[HarmonyPostfix]
		[HarmonyPatch(nameof(CabTeleportDestination.GetHoverText))]
		private static void GetHoverText(CabTeleportDestination __instance, ref string __result)
		{
			if (carRef != null && De2Blocker.TryGetZone(carRef(__instance), out var zone))
				__result = zone.GetHoverText();
		}
	}
}