//FILE:		DropdownOverlay.cs

using System.Collections.Generic;
using UnityEngine;

namespace CareerRework
{
	// =========================
	// DROPDOWN-OVERLAY
	// =========================
	// Zeichnet die Auswahlliste der Zusatzlizenzen als Modal-Fenster über dem Mod Manager.
	// Existiert nur, nachdem einmal ein Dropdown geöffnet wurde; OnGUI kehrt sofort zurück, solange keins offen ist.
	internal sealed class DropdownOverlay : MonoBehaviour
	{
		private const int WindowId = 0x43524450;		// "CRDP"
		private const float ItemHeight = 24f;
		private const int MaxVisible = 8;

		private static DropdownOverlay? instance;

		private readonly List<string> keys = new();
		private readonly List<string> labels = new();
		private string current = "";
		private Rect popup;
		private bool needsScroll;
		private GUIStyle? windowStyle, itemStyle, activeStyle;

		public static void Ensure()
		{
			if (instance != null)
				return;
			var go = new GameObject("CareerRework.Dropdown") { hideFlags = HideFlags.HideAndDontSave };
			DontDestroyOnLoad(go);
			instance = go.AddComponent<DropdownOverlay>();
		}

		private void OnGUI()
		{
			var drop = SettingsGui.drop;
			if (drop.Key == null)
				return;

			// Mod-Manager-Fenster geschlossen (Draw läuft nicht mehr) → Dropdown schließen
			if (Time.frameCount - drop.LastDrawFrame > 2)
			{
				drop.Close();
				return;
			}
			if (drop.ButtonScreen.width <= 0f)
				return;		// Button-Position kommt mit dem nächsten Repaint

			GUI.depth = -10000;
			Matrix4x4 oldMatrix = GUI.matrix;
			GUISkin oldSkin = GUI.skin;
			GUI.matrix = drop.Matrix;
			if (drop.Skin != null)
				GUI.skin = drop.Skin;

			if (Event.current.type == EventType.Layout || Event.current.type == EventType.Repaint || keys.Count == 0)
				SettingsGui.DropItems(keys, labels, out current);

			// exakt unter dem Button, gleiche Breite; reicht der Platz nach unten nicht, nach oben aufklappen
			int visible = Mathf.Min(keys.Count, MaxVisible);
			needsScroll = keys.Count > MaxVisible;
			float height = visible * ItemHeight + 4f;
			Rect b = drop.ButtonScreen;
			float scaleY = Mathf.Abs(drop.Matrix.m11) > 0.0001f ? drop.Matrix.m11 : 1f;
			float screenH = Screen.height / scaleY;
			float y = b.yMax + height > screenH && b.y - height >= 0f ? b.y - height : b.yMax;
			popup = new Rect(b.x, y, b.width, height);

			// Klick neben das Popup → schließen (der Mod Manager bekommt den Klick nicht)
			var e = Event.current;
			if (e.type == EventType.MouseDown && !popup.Contains(e.mousePosition))
			{
				drop.Close();
				e.Use();
			}
			else
			{
				windowStyle ??= new GUIStyle();		// kein Fensterrahmen, Hintergrund zeichnen wir selbst
				GUI.ModalWindow(WindowId, popup, DrawWindow, GUIContent.none, windowStyle);
			}

			GUI.matrix = oldMatrix;
			GUI.skin = oldSkin;
		}

		private void DrawWindow(int id)
		{
			var r = new Rect(0f, 0f, popup.width, popup.height);

			if (Event.current.type == EventType.Repaint)
			{
				// deckender Hintergrund + gelber Rahmen
				Color old = GUI.color;
				GUI.color = new Color(0.08f, 0.08f, 0.08f, 1f);
				GUI.DrawTexture(r, Texture2D.whiteTexture);
				GUI.color = new Color(1f, 0.8f, 0.3f, 1f);
				GUI.DrawTexture(new Rect(0f, 0f, r.width, 1f), Texture2D.whiteTexture);
				GUI.DrawTexture(new Rect(0f, r.height - 1f, r.width, 1f), Texture2D.whiteTexture);
				GUI.DrawTexture(new Rect(0f, 0f, 1f, r.height), Texture2D.whiteTexture);
				GUI.DrawTexture(new Rect(r.width - 1f, 0f, 1f, r.height), Texture2D.whiteTexture);
				GUI.color = old;
			}

			itemStyle ??= new GUIStyle(GUI.skin.button)
			{
				alignment = TextAnchor.MiddleLeft,
				padding = new RectOffset(8, 6, 2, 2),
				margin = new RectOffset(0, 0, 0, 0)
			};
			activeStyle ??= new GUIStyle(itemStyle)
			{
				fontStyle = FontStyle.Bold,
				normal = { textColor = new Color(1f, 0.8f, 0.3f) },
				hover = { textColor = new Color(1f, 0.85f, 0.4f) }
			};

			var drop = SettingsGui.drop;
			var inner = new Rect(2f, 2f, r.width - 4f, r.height - 4f);
			float itemWidth = inner.width - (needsScroll ? 16f : 0f);
			var view = new Rect(0f, 0f, itemWidth, keys.Count * ItemHeight);

			string? picked = null;
			drop.Scroll = GUI.BeginScrollView(inner, drop.Scroll, view, false, needsScroll);
			for (int i = 0; i < keys.Count; i++)
			{
				var item = new Rect(0f, i * ItemHeight, itemWidth, ItemHeight - 2f);
				if (GUI.Button(item, labels[i], keys[i] == current ? activeStyle : itemStyle))
					picked = keys[i];
			}
			GUI.EndScrollView(true);

			if (picked != null)
				SettingsGui.DropPick(picked);
		}

		private void OnDestroy()
		{
			if (instance == this)
				instance = null;
		}
	}
}
