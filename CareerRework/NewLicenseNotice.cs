//FILE:		NewLicenseNotice.cs

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using HarmonyLib;
using UnityEngine;
using UnityModManagerNet;

namespace CareerRework
{
	// =========================
	// NEUE LIZENZEN → HINWEIS IM HAUPTMENÜ
	// =========================
	// Im Mod-Ordner liegt "KnownLicenses.txt" mit allen Zusatzlizenzen (andere Mods) vom letzten Start.
	// Kommen im Hauptmenü Lizenzen dazu (neue Mod installiert), erscheint ein Hinweis: jetzt einstellen –
	// beim ersten Laden eines Spielstands werden sie mit den Menü-Werten ins Savegame übernommen und sind danach fest.
	// Die Datei wird beim Anzeigen aktualisiert, der Hinweis kommt also nur einmal pro neuer Lizenz.
	internal sealed class NewLicenseNotice : MonoBehaviour
	{
		private const string FileName = "KnownLicenses.txt";
		private const int WindowId = 0x43524E4C;		// "CRNL"

		private static NewLicenseNotice? instance;

		/// <summary>Seit dem letzten Start neu erkannte Lizenzen (in den Einstellungen rot markiert, bis zum Spielende).</summary>
		public static readonly HashSet<string> NewKeys = new(StringComparer.Ordinal);
		private readonly List<string> names = new();
		private Vector2 scroll;
		private GUIStyle? textStyle, titleStyle;

		/// <summary>Im Hauptmenü aufrufen.</summary>
		public static void Check()
		{
			try
			{
				string path = Path.Combine(Main.Mod.Path, FileName);
				var detected = ExtraLicenses.Detected;
				if (detected.Count == 0 && !File.Exists(path))
					return;

				var known = new HashSet<string>(StringComparer.Ordinal);
				if (File.Exists(path))
					foreach (var line in File.ReadAllLines(path, Encoding.UTF8))
					{
						string k = line.Trim().TrimStart('﻿');
						if (k.Length > 0 && !k.StartsWith("#"))
							known.Add(k);
					}

				var fresh = detected.Where(e => !known.Contains(e.Key)).ToList();
				var current = detected.Select(e => e.Key).OrderBy(k => k, StringComparer.Ordinal).ToList();
				if (fresh.Count == 0 && known.SetEquals(current))
					return;

				// Datei aktualisieren (UTF-8 ohne BOM)
				var sb = new StringBuilder();
				sb.Append("# CareerRework: licenses of other mods seen at the last start (G = general, J = job)\n");
				foreach (var k in current)
					sb.Append(k).Append('\n');
				File.WriteAllText(path, sb.ToString(), new UTF8Encoding(false));

				if (fresh.Count == 0)
					return;		// nur entfernt → kein Hinweis

				foreach (var e in fresh)
					NewKeys.Add(e.Key);
				Main.Info("New licenses detected since the last start: " + string.Join(", ", fresh.Select(e => e.Id)));
				Show(fresh.Select(e => e.Name + "  (" + e.Id + ")"));
			}
			catch (Exception e)
			{
				Main.Warn("Checking for new licenses failed: " + e.Message);
			}
		}

		private static void Show(IEnumerable<string> list)
		{
			if (instance == null)
			{
				var go = new GameObject("CareerRework.NewLicenseNotice") { hideFlags = HideFlags.HideAndDontSave };
				DontDestroyOnLoad(go);
				instance = go.AddComponent<NewLicenseNotice>();
			}
			instance.names.Clear();
			instance.names.AddRange(list);
			instance.scroll = Vector2.zero;
			instance.enabled = true;
		}

		/// <summary>Beim Laden der Welt schließen.</summary>
		public static void Hide()
		{
			if (instance != null)
				instance.enabled = false;
		}

		private void OnGUI()
		{
			if (names.Count == 0)
			{
				enabled = false;
				return;
			}

			// auf 1080p-Höhe skalieren, damit es in 4K nicht winzig ist
			float scale = Mathf.Max(1f, Screen.height / 1080f);
			Matrix4x4 old = GUI.matrix;
			GUI.matrix = Matrix4x4.Scale(new Vector3(scale, scale, 1f));
			GUI.depth = -10001;

			float w = 620f, h = 400f;
			float sw = Screen.width / scale, sh = Screen.height / scale;
			var rect = new Rect((sw - w) / 2f, (sh - h) / 2f, w, h);
			GUI.ModalWindow(WindowId, rect, DrawWindow, GUIContent.none, GUIStyle.none);

			GUI.matrix = old;
		}

		private void DrawWindow(int id)
		{
			var r = new Rect(0f, 0f, 620f, 400f);
			if (Event.current.type == EventType.Repaint)
			{
				Color c = GUI.color;
				GUI.color = new Color(0.08f, 0.08f, 0.08f, 1f);
				GUI.DrawTexture(r, Texture2D.whiteTexture);
				GUI.color = new Color(1f, 0.8f, 0.3f, 1f);
				GUI.DrawTexture(new Rect(0f, 0f, r.width, 3f), Texture2D.whiteTexture);
				GUI.DrawTexture(new Rect(0f, r.height - 1f, r.width, 1f), Texture2D.whiteTexture);
				GUI.DrawTexture(new Rect(0f, 0f, 1f, r.height), Texture2D.whiteTexture);
				GUI.DrawTexture(new Rect(r.width - 1f, 0f, 1f, r.height), Texture2D.whiteTexture);
				GUI.color = c;
			}

			titleStyle ??= new GUIStyle(GUI.skin.label)
			{
				fontSize = 18,
				fontStyle = FontStyle.Bold,
				wordWrap = true,
				normal = { textColor = new Color(1f, 0.8f, 0.3f) }
			};
			textStyle ??= new GUIStyle(GUI.skin.label)
			{
				fontSize = 14,
				wordWrap = true,
				richText = true,
				normal = { textColor = new Color(0.95f, 0.95f, 0.95f) }
			};

			GUILayout.BeginArea(new Rect(16f, 12f, r.width - 32f, r.height - 24f));
			GUILayout.Label(Loc.T("notice.newlic.title"), titleStyle);
			GUILayout.Space(6);
			GUILayout.Label(Loc.T("notice.newlic.text"), textStyle);
			GUILayout.Space(6);

			scroll = GUILayout.BeginScrollView(scroll, GUILayout.ExpandHeight(true));
			foreach (var n in names)
				GUILayout.Label("•  <color=#ff4d4d><b>" + n + "</b></color>", textStyle);
			GUILayout.EndScrollView();

			GUILayout.Space(8);
			GUILayout.BeginHorizontal();
			GUILayout.FlexibleSpace();
			if (GUILayout.Button(Loc.T("notice.newlic.open"), GUILayout.Width(220f), GUILayout.Height(30f)))
			{
				OpenSettings();
				names.Clear();
			}
			if (GUILayout.Button(Loc.T("notice.newlic.ok"), GUILayout.Width(120f), GUILayout.Height(30f)))
				names.Clear();
			GUILayout.EndHorizontal();
			GUILayout.EndArea();
		}

		// Mod-Manager-Fenster öffnen und gleich die CareerRework-Einstellungen aufklappen.
		// Per Reflection, damit es mit jeder UMM-Version kompiliert (interne Felder: Reiter "Mods" = 0,
		// aufgeklappte Mod = Index in modEntries). Fehlt ein Feld, öffnet sich wenigstens das Fenster.
		private static void OpenSettings()
		{
			try
			{
				var ui = AccessTools.Property(typeof(UnityModManager.UI), "Instance")?.GetValue(null);
				if (ui == null)
					return;
				var t = ui.GetType();

				int index = UnityModManager.modEntries.IndexOf(Main.Mod);
				if (index >= 0)
				{
					SetInt(ui, t, index, "mShowModSettings", "ShowModSettings", "showModSettings");
					SetInt(ui, t, 0, "mTabId", "tabId", "TabId");
				}

				var toggle = AccessTools.Method(t, "ToggleWindow", new[] { typeof(bool) });
				if (toggle != null)
					toggle.Invoke(ui, new object[] { true });
				else
					AccessTools.Method(t, "ToggleWindow", Type.EmptyTypes)?.Invoke(ui, null);
			}
			catch (Exception e)
			{
				Main.Log("Opening the mod settings failed: " + e.Message);
			}
		}

		private static void SetInt(object obj, Type type, int value, params string[] names)
		{
			foreach (var n in names)
			{
				var f = AccessTools.Field(type, n);
				if (f != null && f.FieldType == typeof(int))
				{
					f.SetValue(obj, value);
					return;
				}
				var p = AccessTools.Property(type, n);
				if (p != null && p.PropertyType == typeof(int) && p.CanWrite)
				{
					p.SetValue(obj, value, null);
					return;
				}
			}
		}

		private void OnDestroy()
		{
			if (instance == this)
				instance = null;
		}
	}
}
