//FILE:		SettingsGui.cs

using System;
using UnityEngine;
using UnityModManagerNet;

namespace CareerRework
{
	// =========================
	// EINSTELLUNGEN (UMM)
	// =========================
	// Tooltips wie in LongerLoadingDelay: gelbes "?" hinter jeder Zeile, keine dauerhaften Hinweistexte.
	// Hauptmenü: eigene Einstellungen bearbeiten. Spielstand geladen: Werte des Savegames (gesperrt).
	// MP-Client: nur Hinweis, alles kommt vom Host.
	internal static class SettingsGui
	{
		private static Vector2 priceScroll;
		private static readonly string[] locoOptions = { "DE2", "DM3", "S060" };

		public static void Draw(UnityModManager.ModEntry modEntry)
		{
			CaptureArea();
			bool changedBefore = GUI.changed;

			if (Mp.IsClient)
			{
				GUILayout.BeginVertical("box");
				GUILayout.Label("<b>" + Loc.T("gui.mp.client") + "</b>", Banner);
				GUILayout.Label(Loc.T("gui.mp.client.text"));
				GUILayout.EndVertical();
				DrawDebugToggle();
				DrawTooltip();
				return;
			}

			// Spielstand geladen: Werte stehen im Savegame → nur die Meldung, keine (gesperrten) Felder
			if (Main.Locked)
			{
				drop.Close();
				GUILayout.BeginVertical("box");
				GUILayout.Label("<b>" + Loc.T("gui.locked") + "</b>", Banner);
				GUILayout.Label(Loc.T("gui.locked.text"));
				GUILayout.EndVertical();
				DrawDebugToggle();
				DrawTooltip();
				return;
			}

			var s = Main.LocalSettings;
			bool enabledBefore = GUI.enabled;

			// --- Modus ---
			GUILayout.BeginVertical("box");
			GUILayout.Label("<b>" + Loc.T("gui.mode") + "</b>");
			if (Toggle(s.startupMode == StartupMode.Preset, " " + Loc.T("gui.mode.preset"), Loc.T("tip.mode.preset") + "\n" + Loc.T("tip.mode.preset.extra")))
				s.startupMode = StartupMode.Preset;
			if (Toggle(s.startupMode == StartupMode.Custom, " " + Loc.T("gui.mode.custom"), Loc.T("tip.mode.custom")))
				s.startupMode = StartupMode.Custom;
			GUILayout.EndVertical();
			GUILayout.Space(6);

			// --- Start-Optionen (beide Modi) ---
			GUILayout.BeginVertical("box");
			GUILayout.Label("<b>" + Loc.T("gui.start") + "</b>");
			// Start-Lizenzen nur im Multiplayer ohne DVMultiplayerIP (gemeinsame Lizenzen aller Spieler)
			if (Main.StartLicenseOptionsAvailable)
			{
				s.startWithConcurrentJobs1 = Toggle(s.startWithConcurrentJobs1, " " + Loc.T("gui.cj1"), Loc.T("tip.cj1"));
				if (s.startWithConcurrentJobs1)
					s.startWithConcurrentJobs2 = Toggle(s.startWithConcurrentJobs2, "    " + Loc.T("gui.cj2"), Loc.T("tip.cj2"));
				else
					s.startWithConcurrentJobs2 = false;
			}

			s.startHour = IntSlider(Loc.T("gui.starttime"), s.startHour, 1, 24, 1, Loc.Hour(s.startHour),
				Loc.T("tip.starttime", Loc.Hour(s.startHour), Loc.Hour(6)));
			GUILayout.EndVertical();
			GUILayout.Space(6);

			// --- Komfort (nicht im Savegame, gilt ab dem Laden eines Spielstands) ---
			GUILayout.BeginVertical("box");
			GUILayout.Label("<b>" + Loc.T("gui.comfort") + "</b>");
			s.hiddenProgression = Toggle(s.hiddenProgression, " " + Loc.T("gui.hidden"), Loc.T("tip.hidden"));
			GUILayout.EndVertical();

			if (s.startupMode == StartupMode.Custom)
			{
				GUILayout.Space(6);

				// --- Startgeld + Starter-Loks ---
				GUILayout.BeginVertical("box");
				s.startingMoney = IntSlider(Loc.T("gui.money"), s.startingMoney, 40000, 500000, 10000,
					Loc.Num(s.startingMoney) + " $", Loc.T("tip.money", Loc.Num(s.startingMoney)));

				GUILayout.Space(4);
				GUILayout.BeginHorizontal();
				GUILayout.Label("<b>" + Loc.T("gui.starter") + "</b>", GUILayout.ExpandWidth(false));
				Tip(Loc.T("tip.starter"));
				GUILayout.FlexibleSpace();
				GUILayout.EndHorizontal();
				s.starterLocoPrimary = LocoGrid(Loc.T("gui.starter.1"), s.starterLocoPrimary);
				s.starterLocoSecondary = LocoGrid(Loc.T("gui.starter.2"), s.starterLocoSecondary);
				GUILayout.EndVertical();
				GUILayout.Space(6);

				// --- Lizenzpreise ---
				GUILayout.BeginVertical("box");
				GUILayout.BeginHorizontal();
				GUILayout.Label("<b>" + Loc.T("gui.prices") + "</b>", GUILayout.ExpandWidth(false));
				Tip(Loc.T("tip.prices") + Loc.T("tip.prices.copay") + Loc.T("tip.prices.bonus"));
				GUILayout.FlexibleSpace();
				GUILayout.EndHorizontal();

				priceScroll = GUILayout.BeginScrollView(priceScroll, GUILayout.Height(260));
				foreach (var f in CareerReworkSettings.PriceFields)
				{
					if (f.Key == "pricePassengers1" || f.Key == "pricePassengers2")
						continue;
					PriceRow(s, f);
				}
				GUILayout.EndScrollView();

				// PassengerJobs nur, wenn die Mod da ist
				if (LicenseRules.LicenseForPriceKey("pricePassengers1") != null || LicenseRules.LicenseForPriceKey("pricePassengers2") != null)
				{
					GUILayout.Space(4);
					GUILayout.BeginHorizontal();
					GUILayout.Label("<b>" + Loc.T("gui.pj") + "</b>", GUILayout.ExpandWidth(false));
					Tip(Loc.T("tip.pj"));
					GUILayout.FlexibleSpace();
					GUILayout.EndHorizontal();
					foreach (var f in CareerReworkSettings.PriceFields)
						if (f.Key == "pricePassengers1" || f.Key == "pricePassengers2")
							PriceRow(s, f);
				}
				GUILayout.EndVertical();
				GUILayout.Space(6);

				// --- Multiplikatoren ---
				GUILayout.BeginVertical("box");
				int g = s.priceMultiplierKeysGadgets;
				s.priceMultiplierKeysGadgets = IntSlider(Loc.T("gui.gadgets"), g, 1, 10, 1, "x" + g,
					Loc.T("tip.gadgets", g, Loc.Num(10000 * g), Loc.Num(35000 * g)));

				int r = s.priceMultiplierRestoration;
				s.priceMultiplierRestoration = IntSlider(Loc.T("gui.restoration"), r, 1, 10, 1, "x" + r,
					Loc.T("tip.restoration", r, Loc.Num(30000 * r), Loc.Num(20000 * r), Loc.Num(15000 * r), Loc.Num(10000 * r)));
				GUILayout.EndVertical();
			}

			// --- Lizenzen anderer Mods (beide Modi) ---
			GUILayout.Space(6);
			DrawExtraLicenses(s);
			DropSync();		// schwebendes Dropdown (eigenes Fenster über dem Mod Manager)

			GUI.enabled = enabledBefore;

			GUILayout.Space(6);
			DrawDebugToggle();
			DrawTooltip();

			GUI.changed |= changedBefore;
		}

		private static void DrawDebugToggle()
		{
			GUILayout.BeginHorizontal();
			GUILayout.FlexibleSpace();
			Main.LocalSettings.debugLog = GUILayout.Toggle(Main.LocalSettings.debugLog, " " + Loc.T("gui.debug"), GUILayout.ExpandWidth(false));
			GUILayout.EndHorizontal();
		}

		// =========================
		// FELDER
		// =========================
		// Zeile: [Preis] $  Copay [Betrag] $  Lizenzname  ?
		private static void PriceRow(CareerReworkSettings s, CareerReworkSettings.PriceField f)
		{
			var license = LicenseRules.LicenseForPriceKey(f.Key);
			string name = license != null ? LicenseRules.LicenseName(license) : f.Name;
			var o = license != null ? LicenseRules.GetOriginal(license) : default;

			int price = f.Get(s);
			int copay = f.GetCopay(s);
			float effPrice = price > 0 ? price : (o.Price > 0f ? o.Price : 10000f);
			float effCopay = copay > 0 ? copay : o.Copay;
			float bonus = f.GetBonus(s);
			float effBonusField = Mathf.Abs(bonus) > 0.001f ? -bonus / 100f : o.Bonus;

			string tip = Loc.T("tip.price", name, Loc.Num(effPrice), Loc.Num(o.Price));
			if (o.Price <= 0f)
				tip += Loc.T("tip.price.free");
			tip += Loc.T("tip.copay", Loc.Num(effCopay), Loc.Num(o.Copay));
			tip += Loc.T("tip.bonus", LicenseRules.FormatBonus(effBonusField), LicenseRules.FormatBonus(o.Bonus));
			if (f.Key == "priceTrainDriver")
				tip += Loc.T("tip.price.starter");		// Grundlizenz: jeder startet damit, Copay/Zeitbonus gelten ab Start

			GUILayout.BeginHorizontal();
			f.Set(s, NumField(price, 90, false));
			GUILayout.Label(" $   " + Loc.T("gui.copay") + " ", GUILayout.ExpandWidth(false));
			f.SetCopay(s, NumField(copay, 70, true));
			GUILayout.Label(" $   " + Loc.T("gui.bonus") + " ", GUILayout.ExpandWidth(false));
			f.SetBonus(s, BonusField(bonus, 55));
			GUILayout.Label(" %   " + name, GUILayout.ExpandWidth(false));
			Tip(tip);
			GUILayout.FlexibleSpace();
			GUILayout.EndHorizontal();
		}

		/// <summary>Zahlenfeld; leer = 0 (unverändert/Vanilla). emptyForZero: 0 wird als leeres Feld gezeigt.</summary>
		private static int NumField(int value, float width, bool emptyForZero)
		{
			string shown = emptyForZero && value <= 0 ? "" : value.ToString();
			string input = GUILayout.TextField(shown, GUILayout.Width(width));
			if (input == shown)
				return value;
			if (string.IsNullOrWhiteSpace(input))
				return 0;
			return int.TryParse(input.Trim(), out int v) ? Mathf.Max(0, v) : value;
		}

		// Zeitbonus in %: leer/0 = Vanilla, "+5", "-2.5" und "-2,5" werden verstanden
		private static readonly System.Collections.Generic.Dictionary<int, string> bonusEdit = new();

		private static float BonusField(float value, float width)
		{
			int id = GUIUtility.GetControlID(FocusType.Passive);
			string shown = Mathf.Abs(value) < 0.001f ? "" : (value > 0 ? "+" : "") + value.ToString("0.##", System.Globalization.CultureInfo.InvariantCulture);
			// angefangene Eingaben wie "-" oder "2." nicht sofort wegwerfen
			if (bonusEdit.TryGetValue(id, out var pending))
				shown = pending;

			string input = GUILayout.TextField(shown, GUILayout.Width(width));
			if (input == shown)
				return value;

			string t = input.Trim().Replace(',', '.');
			if (t.Length == 0)
			{
				bonusEdit.Remove(id);
				return 0f;
			}
			if (float.TryParse(t, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out float v))
			{
				bool unfinished = t.EndsWith(".") || t == "+" || t == "-";
				if (unfinished) bonusEdit[id] = input; else bonusEdit.Remove(id);
				return Mathf.Clamp(v, -70f, 70f);
			}
			bonusEdit[id] = input;		// "-" oder "+" allein: merken, Wert bleibt
			return value;
		}

		// =========================
		// ZUSATZLIZENZEN
		// =========================
		private static Vector2 extraScroll;

		private static void DrawExtraLicenses(CareerReworkSettings s)
		{
			var detected = ExtraLicenses.Detected;
			int missing = ExtraLicenses.MissingCount(s);

			GUILayout.BeginVertical("box");
			GUILayout.BeginHorizontal();
			GUILayout.Label("<b>" + Loc.T("gui.extra") + "</b>", GUILayout.ExpandWidth(false));
			Tip(Loc.T("tip.extra"));
			GUILayout.FlexibleSpace();
			GUILayout.EndHorizontal();

			if (detected.Count == 0)
			{
				GUILayout.Label(Loc.T("gui.extra.none"));
			}
			else
			{
				bool scroll = detected.Count > 6;
				if (scroll)
					extraScroll = GUILayout.BeginScrollView(extraScroll, GUILayout.Height(260));

				foreach (var entry in detected)
					ExtraRow(s, entry);

				if (scroll)
					GUILayout.EndScrollView();
			}

			if (missing > 0)
				GUILayout.Label(Loc.T("gui.extra.missing", missing));

			GUILayout.EndVertical();
		}

		// Zeile 1: Name ?    Zeile 2: Preis [..] $  Copay [..] $  Zeitbonus [..] %  Benötigt [Auswahl ▼]
		private static void ExtraRow(CareerReworkSettings s, ExtraLicenses.Entry entry)
		{
			var o = LicenseRules.GetOriginal(entry.License);
			var x = s.FindExtra(entry.Id, entry.Job);
			int price = x?.price ?? 0;
			int copay = x?.copay ?? 0;
			float bonus = x?.bonus ?? 0f;
			string required = x?.required ?? ExtraLicense.ReqUnchanged;

			float effBonusField = Mathf.Abs(bonus) > 0.001f ? -bonus / 100f : o.Bonus;
			string tip = Loc.T("tip.extra.row", entry.Name, entry.Id, entry.Job ? "Job" : "General",
				Loc.Num(o.Price), Loc.Num(o.Copay), ExtraLicenses.OriginalRequirementName(entry.License))
				+ Loc.T("tip.bonus", LicenseRules.FormatBonus(effBonusField), LicenseRules.FormatBonus(o.Bonus));

			GUILayout.BeginHorizontal();
			// seit dem letzten Start neu (andere Mod frisch installiert) → rot
			string title = NewLicenseNotice.NewKeys.Contains(entry.Key)
				? "<color=#ff4d4d><b>" + entry.Name + "</b></color>"
				: "<b>" + entry.Name + "</b>";
			GUILayout.Label(title, GUILayout.ExpandWidth(false));
			Tip(tip);
			GUILayout.FlexibleSpace();
			GUILayout.EndHorizontal();

			GUILayout.BeginHorizontal();
			GUILayout.Label("   " + Loc.T("gui.price") + " ", GUILayout.ExpandWidth(false));
			int newPrice = NumField(price, 90, true);
			GUILayout.Label(" $   " + Loc.T("gui.copay") + " ", GUILayout.ExpandWidth(false));
			int newCopay = NumField(copay, 70, true);
			GUILayout.Label(" $   " + Loc.T("gui.bonus") + " ", GUILayout.ExpandWidth(false));
			float newBonus = BonusField(bonus, 55);
			GUILayout.Label(" %   " + Loc.T("gui.extra.req") + " ", GUILayout.ExpandWidth(false));

			bool open = drop.Key == entry.Key;
			if (GUILayout.Button(RequirementLabel(required) + (open ? "  ▲" : "  ▼"), GUILayout.Width(DropWidth)))
			{
				if (open)
					drop.Close();
				else if (GUI.enabled)
					drop.Open(entry, s);
			}
			// Position des Buttons merken (Bildschirmkoordinaten, damit sie außerhalb der Scroll-Liste gilt)
			if (Event.current.type == EventType.Repaint && drop.Key == entry.Key)
			{
				Rect r = GUILayoutUtility.GetLastRect();
				Vector2 tl = GUIUtility.GUIToScreenPoint(new Vector2(r.x, r.y));
				drop.ButtonScreen = new Rect(tl.x, tl.y, r.width, r.height);
			}
			GUILayout.FlexibleSpace();
			GUILayout.EndHorizontal();

			// Auswahl aus dem Popup (wird am Ende von Draw übernommen)
			string newRequired = required;
			if (drop.Picked != null && drop.PickedFor == entry.Key)
			{
				newRequired = drop.Picked;
				drop.Picked = null;
			}

			if (newPrice != price || newCopay != copay || Mathf.Abs(newBonus - bonus) > 0.0001f || newRequired != required)
			{
				var e = s.GetOrAddExtra(entry.Id, entry.Job);
				e.price = newPrice;
				e.copay = newCopay;
				e.bonus = newBonus;
				e.required = newRequired;
				if (e.IsDefault)
					s.extraLicenses.Remove(e);		// Standard = kein Eintrag nötig
				GUI.changed = true;
			}

			GUILayout.Space(4);
		}

		private static string RequirementLabel(string required)
		{
			if (string.IsNullOrEmpty(required))
				return Loc.T("gui.req.unchanged");
			if (required == ExtraLicense.ReqNone)
				return Loc.T("gui.req.none");
			var t = ExtraLicenses.Find(required);
			return t != null ? t.Name : required.Substring(Math.Min(2, required.Length)) + " (?)";
		}

		// =========================
		// DROPDOWN (schwebend, über dem Mod Manager)
		// =========================
		// Das Popup ist ein eigenes Modal-Fenster (DropdownOverlay): Es wird nach dem UMM-Fenster gezeichnet,
		// liegt damit über allem und wird weder von der Scroll-Liste noch vom UMM-Rand abgeschnitten.
		// Solange es offen ist, bekommt der Mod Manager keine Maus-Eingaben; ein Klick daneben schließt es.
		private const float DropWidth = 230f;

		internal sealed class DropState
		{
			public string? Key;						// offene Lizenz
			public ExtraLicenses.Entry? Entry;
			public CareerReworkSettings? Settings;
			public Rect ButtonScreen;				// Button in UMM-Koordinaten ohne Clipping (letzter Repaint)
			public Matrix4x4 Matrix = Matrix4x4.identity;	// GUI-Skalierung des Mod Managers
			public GUISkin? Skin;
			public Vector2 Scroll;
			public int LastDrawFrame;
			public string? Picked, PickedFor;		// gewählte Voraussetzung → nächster Durchlauf von ExtraRow

			public void Open(ExtraLicenses.Entry entry, CareerReworkSettings s)
			{
				Key = entry.Key; Entry = entry; Settings = s;
				Scroll = Vector2.zero;
				ButtonScreen = default;
				LastDrawFrame = Time.frameCount;
				DropdownOverlay.Ensure();
			}

			public void Close()
			{
				Key = null; Entry = null; Settings = null;
				ButtonScreen = default;
			}
		}

		internal static readonly DropState drop = new();

		private static bool IsDetected(ExtraLicenses.Entry entry)
		{
			foreach (var d in ExtraLicenses.Detected)
				if (ReferenceEquals(d, entry) || d.Key == entry.Key)
					return true;
			return false;
		}

		/// <summary>Am Ende von Draw: Zustand für das Overlay aktualisieren.</summary>
		private static void DropSync()
		{
			if (drop.Key == null || drop.Entry == null)
				return;

			if (!GUI.enabled || !IsDetected(drop.Entry))
			{
				drop.Close();
				return;
			}

			drop.LastDrawFrame = Time.frameCount;
			if (Event.current.type == EventType.Repaint)
			{
				drop.Matrix = GUI.matrix;
				drop.Skin = GUI.skin;
			}
		}

		/// <summary>Einträge: Unverändert, Keine, alle anderen Lizenzen ohne Selbstbezug/Zyklus.</summary>
		internal static void DropItems(System.Collections.Generic.List<string> keys, System.Collections.Generic.List<string> labels, out string current)
		{
			keys.Clear(); labels.Clear();
			current = ExtraLicense.ReqUnchanged;
			var entry = drop.Entry;
			var s = drop.Settings;
			if (entry == null || s == null)
				return;

			current = s.FindExtra(entry.Id, entry.Job)?.required ?? ExtraLicense.ReqUnchanged;
			keys.Add(ExtraLicense.ReqUnchanged); labels.Add(Loc.T("gui.req.unchanged"));
			keys.Add(ExtraLicense.ReqNone); labels.Add(Loc.T("gui.req.none"));
			foreach (var other in ExtraLicenses.All)
			{
				if (ReferenceEquals(other.License, entry.License) || ExtraLicenses.CreatesCycle(entry.License, other.License, s))
					continue;
				keys.Add(other.Key);
				labels.Add(other.Name);
			}
		}

		internal static void DropPick(string key)
		{
			drop.Picked = key;
			drop.PickedFor = drop.Key;
			drop.Close();
		}

		private static StarterLocoType LocoGrid(string label, StarterLocoType current)
		{
			GUILayout.BeginHorizontal();
			GUILayout.Label("   " + label, GUILayout.Width(380));
			int index = Mathf.Clamp((int)current, 0, locoOptions.Length - 1);
			int selected = GUILayout.SelectionGrid(index, locoOptions, locoOptions.Length, GUILayout.Width(220));
			GUILayout.FlexibleSpace();
			GUILayout.EndHorizontal();
			return (StarterLocoType)Mathf.Clamp(selected, 0, 2);
		}

		// =========================
		// GUI-HELFER (wie LongerLoadingDelay)
		// =========================
		private static GUIStyle? tipIconStyle, bannerStyle;

		private static GUIStyle Banner => bannerStyle ??= new GUIStyle(GUI.skin.label)
		{
			richText = true,
			wordWrap = true,
			normal = { textColor = new Color(1f, 0.8f, 0.3f) }
		};

		// "?" mit Tooltip
		private static void Tip(string tip)
		{
			if (string.IsNullOrEmpty(tip))
				return;

			tipIconStyle ??= new GUIStyle(GUI.skin.label)
			{
				fontStyle = FontStyle.Bold,
				normal = { textColor = new Color(1f, 0.8f, 0.3f) }
			};

			// "?" auch bei gesperrten Einstellungen lesbar halten
			bool enabledBefore = GUI.enabled;
			GUI.enabled = true;
			GUILayout.Label(new GUIContent(" ?", tip), tipIconStyle, GUILayout.ExpandWidth(false));
			GUI.enabled = enabledBefore;
		}

		private static bool Toggle(bool value, string label, string tip)
		{
			GUILayout.BeginHorizontal();
			value = GUILayout.Toggle(value, label, GUILayout.ExpandWidth(false));
			Tip(tip);
			GUILayout.FlexibleSpace();
			GUILayout.EndHorizontal();
			return value;
		}

		private static int IntSlider(string label, int value, int min, int max, int step, string valueText, string tip = "")
		{
			GUILayout.BeginHorizontal();
			// "?" direkt hinter der Bezeichnung, nicht hinter dem Slider
			GUILayout.BeginHorizontal(GUILayout.Width(380));
			GUILayout.Label(label, GUILayout.ExpandWidth(false));
			Tip(tip);
			GUILayout.FlexibleSpace();
			GUILayout.EndHorizontal();
			float raw = GUILayout.HorizontalSlider(value, min, max, GUILayout.Width(220));
			GUILayout.Label(" " + valueText, GUILayout.ExpandWidth(false));
			GUILayout.FlexibleSpace();
			GUILayout.EndHorizontal();

			int stepped = Mathf.RoundToInt(raw / step) * step;
			return Mathf.Clamp(stepped, min, max);
		}

		// =========================
		// TOOLTIP
		// =========================
		// Regeln: "?" direkt hinter der Einstellung (nicht hinter Slider/Wert), Tooltip deckend und NICHT
		// überlagert (eigenes Fenster über dem Mod Manager, siehe TooltipOverlay), in Spielsprache (Loc),
		// Breite am Settings-Bereich orientiert, ragt nie aus dem Fenster.
		private static Rect areaScreen;
		private static System.Reflection.FieldInfo? ummWindowRect;
		private static bool ummWindowRectSearched;

		/// <summary>Erster Aufruf in Draw: Breite/Lage des Settings-Bereichs merken.</summary>
		private static void CaptureArea()
		{
			Rect r = GUILayoutUtility.GetRect(0f, 0f, GUILayout.ExpandWidth(true));
			if (Event.current.type != EventType.Repaint)
				return;
			Vector2 tl = GUIUtility.GUIToScreenPoint(new Vector2(r.x, r.y));
			areaScreen = new Rect(tl.x, tl.y, r.width, 0f);
		}

		/// <summary>Fensterrechteck des Mod Managers (gleicher Koordinatenraum), sonst ganzer Bildschirm.</summary>
		private static Rect UmmWindow()
		{
			try
			{
				if (!ummWindowRectSearched)
				{
					ummWindowRectSearched = true;
					var t = typeof(UnityModManager.UI);
					ummWindowRect = HarmonyLib.AccessTools.Field(t, "mWindowRect") ?? HarmonyLib.AccessTools.Field(t, "windowRect");
				}
				var ui = HarmonyLib.AccessTools.Property(typeof(UnityModManager.UI), "Instance")?.GetValue(null);
				if (ummWindowRect != null && ui != null && ummWindowRect.GetValue(ui) is Rect w && w.width > 10f && w.height > 10f)
					return w;
			}
			catch { }
			float sy = Mathf.Abs(GUI.matrix.m11) > 0.0001f ? GUI.matrix.m11 : 1f;
			float sx = Mathf.Abs(GUI.matrix.m00) > 0.0001f ? GUI.matrix.m00 : 1f;
			return new Rect(0f, 0f, Screen.width / sx, Screen.height / sy);
		}

		// Letzter Aufruf in Draw: Tooltip ans Overlay geben und leeren (sonst zeigt UMM ihn zusätzlich)
		private static void DrawTooltip()
		{
			string tip = GUI.tooltip;
			if (Event.current.type != EventType.Repaint)
				return;
			GUI.tooltip = string.Empty;
			if (string.IsNullOrEmpty(tip))
				return;

			Vector2 mouse = GUIUtility.GUIToScreenPoint(Event.current.mousePosition);
			Rect win = UmmWindow();
			float xMin = Mathf.Max(areaScreen.xMin, win.xMin), xMax = Mathf.Min(areaScreen.xMax, win.xMax);
			if (areaScreen.width <= 10f || xMax - xMin < 200f) { xMin = win.xMin; xMax = win.xMax; }
			var area = new Rect(xMin, win.yMin, xMax - xMin, win.height);
			TooltipOverlay.Show(tip, mouse, area, GUI.matrix, GUI.skin);
		}
	}
}