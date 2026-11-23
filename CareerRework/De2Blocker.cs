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
		// "Lizenz fehlt". Form und Lage kommen vom Box-Collider des Lok-Grundmodells (größter fester Box-Collider
		// der Lok), damit der Block exakt die Lok umschließt.
		//
		// Früher ist die Lok explodiert: ein eigener fester Collider ohne Bezug zur Lok lag IN den Collidern der Lok,
		// die Physik hat beide auseinandergedrückt. Jetzt:
		//   • Der feste Block hängt unter dem Collider des Grundmodells und gehört damit zum Rigidbody der Lok.
		//     Collider desselben Rigidbody stoßen sich nie ab → kein Kampf um die Position.
		//   • Er liegt auf der Ebene Train_Walkable (wie die begehbaren Flächen): blockiert den Spieler,
		//     berührt aber keine anderen Wagen – genau dieselbe Form wie das Grundmodell, also nicht größer.
		//   • Schwerpunkt und Trägheit der Lok werden vorher gemerkt und danach wiederhergestellt,
		//     damit sich das Fahrverhalten nicht ändert.
		//   • Der Hover-Hinweis ist ein Trigger (keine Physik) minimal größer als der Block.
		// Mit Lizenz (oder sobald sie gekauft wird) entfernt das Spiel beides selbst (LocoZoneBlocker.UnblockLoco).
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
				center.y = outer.center.y;
				size.x = Mathf.Max(size.x, outer.size.x) + 2f * margin;
				size.y = Mathf.Max(size.y, outer.size.y) + margin;
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

			// 1) fester Block (Kind der Lok → gehört zu ihrem Rigidbody)
			var solid = new GameObject("CareerRework_DE2Blocker");
			solid.transform.SetParent(car.transform, false);
			var box = solid.AddComponent<BoxCollider>();
			box.center = center;
			box.size = size;
			AddSolid(car, solid, box);

			// 2) Hover-Hinweis "Lizenz fehlt" (Trigger, das Spiel hängt ihn an den Innenraum und hält ihn an der Lok)
			var hover = new GameObject(name);
			hover.transform.SetParent(car.interior, false);
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

				if (!AddCorners(root, c, corners))
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
					if (c == null)
						continue;
					if (!AddCorners(root, c, corners))
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

		private static int walkableLayer = -2;

		private static void AddSolid(TrainCar car, GameObject solid, BoxCollider box)
		{
			if (walkableLayer == -2)
				walkableLayer = LayerMask.NameToLayer("Train_Walkable");
			if (walkableLayer >= 0)
				solid.layer = walkableLayer;

			// Schwerpunkt/Trägheit nicht durch den zusätzlichen Collider verändern
			var rb = car.rb;
			if (rb != null)
			{
				Vector3 com = rb.centerOfMass;
				Vector3 inertia = rb.inertiaTensor;
				Quaternion inertiaRot = rb.inertiaTensorRotation;
				box.enabled = true;
				rb.centerOfMass = com;
				rb.inertiaTensor = inertia;
				rb.inertiaTensorRotation = inertiaRot;
			}

			// zur Sicherheit: nie mit den eigenen Collidern der Lok kollidieren
			foreach (var c in car.GetComponentsInChildren<Collider>(true))
				if (c != null && c != box && c.enabled && c.gameObject.activeInHierarchy)
					Physics.IgnoreCollision(box, c, true);
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