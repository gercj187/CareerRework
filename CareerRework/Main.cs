using System;
using System.Reflection;
using HarmonyLib;
using UnityModManagerNet;

namespace CareerRework;

public static class Main
{
	public static bool Load(UnityModManager.ModEntry modEntry)
	{
		var harmony = new Harmony(modEntry.Info.Id);
		harmony.PatchAll();
		modEntry.Logger.Log("[LicenseManagerMod] Harmony patches applied.");
		return true;
	}
}
