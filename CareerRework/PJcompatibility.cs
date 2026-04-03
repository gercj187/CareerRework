// Datei: PJcompatibility.cs
// Zweck: Kompatibilität zu "PassengerJobs" – aktiv nur, wenn das Drittmod aktiv ist.
//        - Findet Passenger-Lizenz über Prefabname "LicensePassengers1" (exakt) oder Namespace "PassengerJobs"
//        - Setzt Preis/Voraussetzungen und schaltet dynamisch auf Fragile um, sobald FreightHaul vorhanden
//        - Fügt (nur im Custom-Mode) einen editierbaren Preis-Eintrag ins Settings-GUI ein
//
// Hinweis: Alle Logik ausschließlich in dieser Datei. Keine Änderungen an Main.cs / CareerReworkSettings.cs nötig.

#nullable enable

using System;
using System.Linq;
using System.Reflection;
using System.Collections;
using System.Collections.Generic;
using HarmonyLib;
using DV.ThingTypes;      // JobLicenses, JobLicenseType_v2
using DV.Logic.Job;      // LicenseManager
using UnityEngine;
using UnityModManagerNet;

namespace CareerRework
{
    [HarmonyPatch] // Marker, damit PatchAll() diese Datei findet
    internal static class PJcompatibility
    {
        // --- Konfiguration / Caches ---
        private const string PASSENGER_PREFAB_NAME = "LicensePassengers1";
		private const string PASSENGER2_PREFAB_NAME = "LicensePassengers2";
        private static bool? _active;
        private static string? _passengerNsHint;
        private static JobLicenseType_v2? _passengerV2;
		private static JobLicenseType_v2? _passenger2V2;
        private static MethodInfo? _toV2JobMethod;
		private static CoroutineRunner? _runner;

        private const string PLAYERPREFS_KEY = "CR_PJ_PassengersPrice";
        private const int DEFAULT_PRICE = 500000;
		
		private const string PLAYERPREFS_KEY_P2 = "CR_PJ_Passengers2Price";
		private const int DEFAULT_PRICE_P2 = 1000000;
		
        // ---------------- Aktivierung erkennen ----------------
        internal static bool IsPassengerJobsActive()
        {
            if (_active.HasValue) return _active.Value;

            // 0) Schneller Pfad: Prefab bereits im Speicher?
            try
            {
                var objs = Resources.FindObjectsOfTypeAll(typeof(JobLicenseType_v2));
                if (objs != null)
                {
                    foreach (var o in objs)
                    {
                        var uo = o as UnityEngine.Object;
                        if (uo != null && string.Equals(uo.name, PASSENGER_PREFAB_NAME, StringComparison.OrdinalIgnoreCase))
                        {
                            Debug.Log("[CareerRework][PassengerJobs] Detected by prefab name: " + PASSENGER_PREFAB_NAME);
                            _active = true;
                            return true;
                        }
                    }
                }
            }
            catch
            {
                // ignore – sehr früher Init-Zeitpunkt
            }

            // 1) Namespace-Scan (exakt "PassengerJobs")
            try
            {
                var asms = AppDomain.CurrentDomain.GetAssemblies();
                if (asms != null)
                {
                    foreach (var asm in asms)
                    {
                        if (asm == null) continue;
                        Type[] types;
                        try { types = asm.GetTypes(); }
                        catch (ReflectionTypeLoadException ex)
                        {
                            types = ex.Types != null ? ex.Types.Where(t => t != null).ToArray()! : Array.Empty<Type>();
                        }

                        foreach (var t in types)
                        {
                            if (t == null) continue;
                            var ns = t.Namespace;
                            if (!string.IsNullOrEmpty(ns) && ns.StartsWith("PassengerJobs", StringComparison.Ordinal))
                            {
                                _passengerNsHint ??= ns;
                                _active = true;
                                Debug.Log("[CareerRework][PassengerJobs] Detected by namespace: " + ns);
                                return true;
                            }
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Debug.Log("[CareerRework][PassengerJobs] Detection failed: " + e);
            }

            _active = false;
            return false;
        }
		
		internal class CoroutineRunner : MonoBehaviour
		{
		}

        // ---------------- Helper: ToV2(JobLicenses)->JobLicenseType_v2 (Reflection) ----------------
        private static JobLicenseType_v2? GetV2(JobLicenses jl)
        {
            if (_toV2JobMethod == null)
            {
                try
                {
                    var asms = AppDomain.CurrentDomain.GetAssemblies();
                    if (asms != null)
                    {
                        foreach (var asm in asms)
                        {
                            if (asm == null) continue;
                            Type[] types;
                            try { types = asm.GetTypes(); }
                            catch (ReflectionTypeLoadException ex)
                            {
                                types = ex.Types != null ? ex.Types.Where(t => t != null).ToArray()! : Array.Empty<Type>();
                            }

                            foreach (var t in types)
                            {
                                if (t == null) continue;
                                var ms = t.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
                                foreach (var m in ms)
                                {
                                    if (m.Name != "ToV2") continue;
                                    var p = m.GetParameters();
                                    if (p.Length == 1 &&
                                        p[0].ParameterType == typeof(JobLicenses) &&
                                        typeof(JobLicenseType_v2).IsAssignableFrom(m.ReturnType))
                                    {
                                        _toV2JobMethod = m;
                                        Debug.Log("[CareerRework][PassengerJobs] Found ToV2(JobLicenses) at: " + t.FullName);
                                        break;
                                    }
                                }
                                if (_toV2JobMethod != null) break;
                            }
                            if (_toV2JobMethod != null) break;
                        }
                    }
                }
                catch (Exception e)
                {
                    Debug.Log("[CareerRework][PassengerJobs] Locate ToV2(JobLicenses) failed: " + e);
                }
            }

            if (_toV2JobMethod == null) return null;
            try
            {
                return (JobLicenseType_v2?)_toV2JobMethod.Invoke(null, new object[] { jl });
            }
            catch (Exception e)
            {
                Debug.Log("[CareerRework][PassengerJobs] Invoke ToV2(JobLicenses) failed: " + e);
                return null;
            }
        }
		
		internal static JobLicenseType_v2? GetPassenger2V2(bool forceRefresh = false)
		{
			if (!forceRefresh && _passenger2V2 != null) return _passenger2V2;
			if (!IsPassengerJobsActive()) return null;

			try
			{
				var asms = AppDomain.CurrentDomain.GetAssemblies();

				foreach (var asm in asms)
				{
					if (asm == null) continue;

					Type[] types;
					try { types = asm.GetTypes(); }
					catch (ReflectionTypeLoadException ex)
					{
						types = ex.Types != null ? ex.Types.Where(t => t != null).ToArray()! : Array.Empty<Type>();
					}

					foreach (var t in types)
					{
						if (t.FullName != "PassengerJobs.Injectors.LicenseInjector") continue;

						// 👉 PROPERTY statt FIELD
						var prop = t.GetProperty("License2", BindingFlags.Public | BindingFlags.Static);
						if (prop != null)
						{
							var val = prop.GetValue(null) as JobLicenseType_v2;
							if (val != null)
							{
								_passenger2V2 = val;
								Debug.Log("[CareerRework][PassengerJobs] Passenger2 FOUND via PROPERTY");
								return _passenger2V2;
							}
						}

						// fallback: backing fields
						var fields = t.GetFields(BindingFlags.NonPublic | BindingFlags.Static);
						foreach (var f in fields)
						{
							if (!typeof(JobLicenseType_v2).IsAssignableFrom(f.FieldType)) continue;

							var val = f.GetValue(null) as JobLicenseType_v2;
							if (val == null) continue;

							var name = (val as UnityEngine.Object)?.name ?? "";
							if (name.Contains("Passengers2"))
							{
								_passenger2V2 = val;
								Debug.Log("[CareerRework][PassengerJobs] Passenger2 FOUND via backing field");
								return _passenger2V2;
							}
						}
					}
				}
			}
			catch (Exception e)
			{
				Debug.Log("[CareerRework][PassengerJobs] GetPassenger2V2 FAILED: " + e);
			}

			return null;
		}

        // ---------------- Passenger-Lizenz finden (mehrstufig, mit Prefab-Exact-Match) ----------------
        internal static JobLicenseType_v2? GetPassengerV2(bool forceRefresh = false)
        {
            if (!forceRefresh && _passengerV2 != null) return _passengerV2;
            if (!IsPassengerJobsActive()) return null;

            JobLicenseType_v2? v2;

            if (TryFindPassenger_ByExactPrefabName(out v2)) { _passengerV2 = v2; return _passengerV2; }
            if (TryFindPassenger_StaticFieldOrProperty(out v2)) { _passengerV2 = v2; return _passengerV2; }
            if (TryFindPassenger_EnumToV2(out v2)) { _passengerV2 = v2; return _passengerV2; }
            if (TryFindPassenger_ByResourcesContains(out v2)) { _passengerV2 = v2; return _passengerV2; }
            if (TryFindPassenger_InLicenseManager(out v2)) { _passengerV2 = v2; return _passengerV2; }

            Debug.Log("[CareerRework][PassengerJobs] Passenger license not found after all strategies. Compatibility disabled.");
            return null;
        }

        private static bool TryFindPassenger_ByExactPrefabName(out JobLicenseType_v2? result)
        {
            result = null;
            try
            {
                var objs = Resources.FindObjectsOfTypeAll(typeof(JobLicenseType_v2));
                if (objs != null)
                {
                    foreach (var o in objs)
                    {
                        var uo = o as UnityEngine.Object;
                        if (uo != null && string.Equals(uo.name, PASSENGER_PREFAB_NAME, StringComparison.OrdinalIgnoreCase))
                        {
                            result = (JobLicenseType_v2)o;
                            Debug.Log("[CareerRework][PassengerJobs] Passenger via exact prefab name: " + uo.name);
                            return true;
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Debug.Log("[CareerRework][PassengerJobs] TryFindPassenger_ByExactPrefabName failed: " + e);
            }
            return false;
        }

        private static bool TryFindPassenger_ByResourcesContains(out JobLicenseType_v2? result)
        {
            result = null;
            try
            {
                var objs = Resources.FindObjectsOfTypeAll(typeof(JobLicenseType_v2));
                if (objs != null)
                {
                    foreach (var o in objs)
                    {
                        var uo = o as UnityEngine.Object;
                        var n = uo != null ? uo.name : string.Empty;
                        if (!string.IsNullOrEmpty(n) &&
                            n.IndexOf("Passenger", StringComparison.OrdinalIgnoreCase) >= 0)
                        {
                            result = (JobLicenseType_v2)o;
                            Debug.Log("[CareerRework][PassengerJobs] Passenger via Resources contains: " + n);
                            return true;
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Debug.Log("[CareerRework][PassengerJobs] TryFindPassenger_ByResourcesContains failed: " + e);
            }
            return false;
        }

        private static bool TryFindPassenger_StaticFieldOrProperty(out JobLicenseType_v2? result)
        {
            result = null;
            try
            {
                var asms = AppDomain.CurrentDomain.GetAssemblies();
                if (asms != null)
                {
                    foreach (var asm in asms)
                    {
                        if (asm == null) continue;
                        Type[] types;
                        try { types = asm.GetTypes(); }
                        catch (ReflectionTypeLoadException ex)
                        {
                            types = ex.Types != null ? ex.Types.Where(t => t != null).ToArray()! : Array.Empty<Type>();
                        }

                        foreach (var t in types)
                        {
                            if (t == null) continue;
                            var ns = t.Namespace;
                            if (string.IsNullOrEmpty(ns) || !ns.StartsWith("PassengerJobs", StringComparison.Ordinal)) continue;

                            foreach (var f in t.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static))
                            {
                                if (!typeof(JobLicenseType_v2).IsAssignableFrom(f.FieldType)) continue;
                                var val = f.GetValue(null) as JobLicenseType_v2;
                                if (val != null && IsPassengerCandidate(val))
                                {
                                    Debug.Log("[CareerRework][PassengerJobs] Passenger via static field: " + t.FullName + "." + f.Name);
                                    result = val;
                                    return true;
                                }
                            }

                            foreach (var p in t.GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static))
                            {
                                if (!typeof(JobLicenseType_v2).IsAssignableFrom(p.PropertyType)) continue;
                                var val = p.GetValue(null, null) as JobLicenseType_v2;
                                if (val != null && IsPassengerCandidate(val))
                                {
                                    Debug.Log("[CareerRework][PassengerJobs] Passenger via static property: " + t.FullName + "." + p.Name);
                                    result = val;
                                    return true;
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Debug.Log("[CareerRework][PassengerJobs] TryFindPassenger_StaticFieldOrProperty failed: " + e);
            }
            return false;
        }

        private static bool TryFindPassenger_EnumToV2(out JobLicenseType_v2? result)
        {
            result = null;
            try
            {
                var asms = AppDomain.CurrentDomain.GetAssemblies();
                if (asms != null)
                {
                    foreach (var asm in asms)
                    {
                        if (asm == null) continue;
                        Type[] types;
                        try { types = asm.GetTypes(); }
                        catch (ReflectionTypeLoadException ex)
                        {
                            types = ex.Types != null ? ex.Types.Where(t => t != null).ToArray()! : Array.Empty<Type>();
                        }

                        foreach (var enumType in types)
                        {
                            if (enumType == null || !enumType.IsEnum) continue;
                            var ns = enumType.Namespace ?? string.Empty;
                            if (!ns.StartsWith("PassengerJobs", StringComparison.Ordinal)) continue;
                            if (enumType.Name.IndexOf("JobLicenses", StringComparison.OrdinalIgnoreCase) < 0) continue;

                            var fields = enumType.GetFields(BindingFlags.Public | BindingFlags.Static);
                            foreach (var fi in fields)
                            {
                                var enumValue = fi.GetValue(null);
                                var toV2 = FindToV2ForEnum(enumType);
                                if (toV2 != null)
                                {
                                    var val = toV2.Invoke(null, new object?[] { enumValue }) as JobLicenseType_v2;
                                    if (val != null && IsPassengerCandidate(val))
                                    {
                                        Debug.Log("[CareerRework][PassengerJobs] Passenger via enum + ToV2: " + enumType.FullName + "." + fi.Name);
                                        result = val;
                                        return true;
                                    }
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Debug.Log("[CareerRework][PassengerJobs] TryFindPassenger_EnumToV2 failed: " + e);
            }
            return false;
        }

        private static MethodInfo? FindToV2ForEnum(Type enumType)
        {
            try
            {
                var asms = AppDomain.CurrentDomain.GetAssemblies();
                if (asms != null)
                {
                    foreach (var asm in asms)
                    {
                        if (asm == null) continue;
                        Type[] types;
                        try { types = asm.GetTypes(); }
                        catch (ReflectionTypeLoadException ex)
                        {
                            types = ex.Types != null ? ex.Types.Where(x => x != null).ToArray()! : Array.Empty<Type>();
                        }

                        foreach (var t in types)
                        {
                            if (t == null) continue;
                            var ms = t.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
                            foreach (var m in ms)
                            {
                                if (m.Name != "ToV2") continue;
                                var pars = m.GetParameters();
                                if (pars.Length == 1 && pars[0].ParameterType == enumType &&
                                    typeof(JobLicenseType_v2).IsAssignableFrom(m.ReturnType))
                                {
                                    Debug.Log("[CareerRework][PassengerJobs] Found ToV2(" + enumType.FullName + ") at " + t.FullName);
                                    return m;
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Debug.Log("[CareerRework][PassengerJobs] FindToV2ForEnum failed: " + e);
            }
            return null;
        }

        private static bool TryFindPassenger_InLicenseManager(out JobLicenseType_v2? result)
        {
            result = null;
            try
            {
                var lm = UnityEngine.Object.FindObjectOfType<LicenseManager>();
                if (lm == null) return false;

                var fields = lm.GetType().GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                foreach (var f in fields)
                {
                    if (typeof(JobLicenseType_v2).IsAssignableFrom(f.FieldType))
                    {
                        var val = f.GetValue(lm) as JobLicenseType_v2;
                        if (val != null && IsPassengerCandidate(val))
                        {
                            Debug.Log("[CareerRework][PassengerJobs] Passenger in LM field: " + f.Name);
                            result = val;
                            return true;
                        }
                    }
                    else if (typeof(IEnumerable).IsAssignableFrom(f.FieldType))
                    {
                        var e = f.GetValue(lm) as IEnumerable;
                        if (e == null) continue;
                        foreach (var it in e)
                        {
                            var jl = it as JobLicenseType_v2;
                            if (jl != null && IsPassengerCandidate(jl))
                            {
                                Debug.Log("[CareerRework][PassengerJobs] Passenger in LM collection: " + f.Name);
                                result = jl;
                                return true;
                            }
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Debug.Log("[CareerRework][PassengerJobs] TryFindPassenger_InLicenseManager failed: " + e);
            }
            return false;
        }

        // Kandidatentest: exakter Prefabname ODER Name enthält "Passenger"
        private static bool IsPassengerCandidate(JobLicenseType_v2 jl)
        {
            var uo = jl as UnityEngine.Object;
            var n = uo != null ? uo.name : string.Empty;
            if (string.Equals(n, PASSENGER_PREFAB_NAME, StringComparison.OrdinalIgnoreCase)) return true;
            if (!string.IsNullOrEmpty(n) && n.IndexOf("Passenger", StringComparison.OrdinalIgnoreCase) >= 0) return true;
            return false;
        }

        // ---------------- Preis-Handling ----------------
        internal static int GetConfiguredPassengerPrice()
        {
            try { return PlayerPrefs.GetInt(PLAYERPREFS_KEY, DEFAULT_PRICE); }
            catch { return DEFAULT_PRICE; }
        }

        internal static void SetConfiguredPassengerPrice(int value)
        {
            var v = Mathf.Max(0, value);
            try { PlayerPrefs.SetInt(PLAYERPREFS_KEY, v); PlayerPrefs.Save(); }
            catch { /* ignore */ }
        }
		
		internal static int GetConfiguredPassenger2Price()
		{
			try { return PlayerPrefs.GetInt(PLAYERPREFS_KEY_P2, DEFAULT_PRICE_P2); }
			catch { return DEFAULT_PRICE_P2; }
		}

		internal static void SetConfiguredPassenger2Price(int value)
		{
			var v = Mathf.Max(0, value);
			try { PlayerPrefs.SetInt(PLAYERPREFS_KEY_P2, v); PlayerPrefs.Save(); }
			catch { }
		}

        // ---------------- Basistuning ----------------
        internal static void ApplyPassengerBaseTuning()
        {
            if (!IsPassengerJobsActive()) return;

            var passenger = GetPassengerV2();
            if (passenger == null) return;

            try
            {
                var freightV2 = GetV2(JobLicenses.FreightHaul);
                if (freightV2 != null)
                {
                    passenger.requiredJobLicense = freightV2;
                }

                passenger.price = (Main.settings != null && Main.settings.startupMode == StartupMode.Custom)
                    ? GetConfiguredPassengerPrice()
                    : DEFAULT_PRICE;

                var req = passenger.requiredJobLicense as UnityEngine.Object;
                var reqName = req != null ? req.name : "null";

                Debug.Log("[CareerRework][PassengerJobs] Base tuning applied. price=" + passenger.price + ", req=" + reqName);
            }
            catch (Exception e)
            {
                Debug.Log("[CareerRework][PassengerJobs] ApplyPassengerBaseTuning error: " + e);
            }
        }
		
		internal static void ApplyPassenger2Pricing()
		{
			if (!IsPassengerJobsActive()) return;

			var passenger2 = GetPassenger2V2();
			if (passenger2 == null) return;

			try
			{
				passenger2.price = (Main.settings != null && Main.settings.startupMode == StartupMode.Custom)
					? GetConfiguredPassenger2Price()
					: DEFAULT_PRICE_P2;

				Debug.Log("[CareerRework][PassengerJobs] Passenger2 price set to " + passenger2.price);
			}
			catch (Exception e)
			{
				Debug.Log("[CareerRework][PassengerJobs] ApplyPassenger2Pricing error: " + e);
			}
		}

        internal static void ApplyPassengerSwitchedRequirementIfNeeded()
        {
            if (!IsPassengerJobsActive()) return;

            var passenger = GetPassengerV2();
            if (passenger == null) return;

            try
            {
                var mgr = UnityEngine.Object.FindObjectOfType<LicenseManager>();
                var freightV2 = GetV2(JobLicenses.FreightHaul);
                var fragileV2 = GetV2(JobLicenses.Fragile);

                if (mgr != null && freightV2 != null && fragileV2 != null)
                {
                    if (mgr.IsJobLicenseAcquired(freightV2))
                    {
                        passenger.requiredJobLicense = fragileV2;
                        Debug.Log("[CareerRework][PassengerJobs] Requirement switched to Fragile (FreightHaul owned).");
                    }
                }
            }
            catch (Exception e)
            {
                Debug.Log("[CareerRework][PassengerJobs] ApplyPassengerSwitchedRequirementIfNeeded error: " + e);
            }
        }
		
		private static void OnGameFullyLoaded()
		{
			Debug.Log("[CareerRework][PassengerJobs] GAME FULLY LOADED → applying Passenger2 pricing");

			if (_runner != null)
			{
				_runner.StartCoroutine(ApplyPassenger2WhenReady());
			}
			else
			{
				Debug.Log("[CareerRework][PassengerJobs] ERROR: CoroutineRunner is NULL!");
			}
		}
		
		private static IEnumerator ApplyPassenger2WhenReady()
		{
			float timeout = 10f;
			float elapsed = 0f;

			while (elapsed < timeout)
			{
				var p2 = GetPassenger2V2(forceRefresh: true);

				if (p2 != null)
				{
					Debug.Log("[CareerRework][PassengerJobs] Passenger2 FOUND in coroutine!");

					ApplyPassenger2Pricing();

					Debug.Log("[CareerRework][PassengerJobs] Passenger2 pricing SUCCESSFULLY applied.");
					yield break;
				}

				elapsed += Time.deltaTime;
				yield return null;
			}

			Debug.Log("[CareerRework][PassengerJobs] ERROR: Passenger2 not found after timeout!");
		}

        // ---------------- Harmony Hooks ----------------
        [HarmonyPostfix, HarmonyPatch(typeof(LicenseManager), "LoadData")]
        private static void LM_LoadData_Postfix()
        {
            if (!IsPassengerJobsActive()) return;

            // Prefab kann erst jetzt im Speicher sein – erneut suchen
            GetPassengerV2(forceRefresh: true);
			GetPassenger2V2(forceRefresh: true);
			
            ApplyPassengerBaseTuning();
			ApplyPassenger2Pricing();
			
            ApplyPassengerSwitchedRequirementIfNeeded();
        }
		
		[HarmonyPostfix]
		[HarmonyPatch(typeof(WorldStreamingInit), "Awake")]
		private static void WorldStreamingInit_Awake_Postfix(WorldStreamingInit __instance)
		{
			if (_runner == null)
			{
				var go = new GameObject("[CareerRework CoroutineRunner]");
				UnityEngine.Object.DontDestroyOnLoad(go);
				_runner = go.AddComponent<CoroutineRunner>();
			}
			try
			{
				WorldStreamingInit.LoadingFinished += OnGameFullyLoaded;
				Debug.Log("[CareerRework][PassengerJobs] Subscribed to LoadingFinished");
			}
			catch (Exception e)
			{
				Debug.Log("[CareerRework][PassengerJobs] Failed to subscribe: " + e);
			}
		}

        [HarmonyPostfix, HarmonyPatch(typeof(LicenseManager), nameof(LicenseManager.AcquireJobLicense), new Type[] { typeof(JobLicenseType_v2) })]
        private static void LM_AcquireJobLicense_Postfix(JobLicenseType_v2 newLicense)
        {
            if (!IsPassengerJobsActive()) return;

            try
            {
                var freightV2 = GetV2(JobLicenses.FreightHaul);
                if (freightV2 != null && ReferenceEquals(newLicense, freightV2))
                {
                    var passenger = GetPassengerV2();
                    var fragileV2 = GetV2(JobLicenses.Fragile);
                    if (passenger != null && fragileV2 != null)
                    {
                        passenger.requiredJobLicense = fragileV2;
                        Debug.Log("[CareerRework][PassengerJobs] On acquire FreightHaul -> switch Passenger requirement to Fragile.");
                    }
                }
            }
            catch (Exception e)
            {
                Debug.Log("[CareerRework][PassengerJobs] Acquire hook failed: " + e);
            }
        }

        // ---------------- Settings-GUI (nur Custom + Passenger aktiv) ----------------
        [HarmonyPostfix, HarmonyPatch(typeof(Main), "OnGUI")]
        private static void Main_OnGUI_Postfix(UnityModManager.ModEntry modEntry)
        {
            try
            {
                if (!IsPassengerJobsActive()) return;
                if (Main.settings == null) return;
                if (Main.settings.startupMode != StartupMode.Custom) return;

                GUILayout.Space(8);
                GUILayout.Label("PassengerJobs compatibility", GUILayout.ExpandWidth(false));

                GUILayout.BeginHorizontal();
                string txt = GUILayout.TextField(GetConfiguredPassengerPrice().ToString(), GUILayout.Width(100));
                GUILayout.Label("$  Passengers1 license price", GUILayout.ExpandWidth(false));
                GUILayout.EndHorizontal();

                int parsed;
                if (int.TryParse(txt, out parsed) && parsed != GetConfiguredPassengerPrice())
                {
                    SetConfiguredPassengerPrice(parsed);
                    ApplyPassengerBaseTuning();
                }
				GUILayout.BeginHorizontal();
				string txt2 = GUILayout.TextField(GetConfiguredPassenger2Price().ToString(), GUILayout.Width(100));
				GUILayout.Label("$  Passengers2 license price", GUILayout.ExpandWidth(false));
				GUILayout.EndHorizontal();

				int parsed2;
				if (int.TryParse(txt2, out parsed2) && parsed2 != GetConfiguredPassenger2Price())
				{
					SetConfiguredPassenger2Price(parsed2);
					ApplyPassenger2Pricing();
				}
            }
            catch (Exception e)
            {
                Debug.Log("[CareerRework][PassengerJobs] OnGUI postfix error: " + e);
            }
        }
    }
}
