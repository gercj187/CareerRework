using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using UnityModManagerNet;
using UnityEngine;

namespace CareerRework;

public static class Main
{
	public static bool Load(UnityModManager.ModEntry modEntry)
	{
		var harmony = new Harmony(modEntry.Info.Id);
		harmony.PatchAll();
		modEntry.Logger.Log("[CareerRework] Harmony patches applied.");
		return true;
	}
}
