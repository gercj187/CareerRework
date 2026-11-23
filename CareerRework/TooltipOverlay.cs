//FILE:		TooltipOverlay.cs

using UnityEngine;

namespace CareerRework
{
	// =========================
	// TOOLTIP ÜBER DEM MOD MANAGER
	// =========================
	// IMGUI zeichnet in Aufrufreihenfolge: Alles, was der Mod Manager NACH den Mod-Settings zeichnet (die Mod-Liste
	// darunter), liegt über einem Tooltip aus Draw. Deshalb zeichnet ein eigenes Fenster den Tooltip – es wird jeden
	// Frame nach vorn geholt und liegt damit über dem Mod-Manager-Fenster.
	//
	// Regeln (Vorgabe für alle eigenen Mods):
	//   • deckender Hintergrund, nichts scheint durch
	//   • Breite an der Breite des Settings-Bereichs orientiert
	//   • ragt nie aus dem Settings-Bereich bzw. Fenster heraus (links/rechts/oben/unten begrenzt)
	//   • neben der Maus, nie unter dem Mauszeiger (sonst flackert der Hover)
	internal sealed class TooltipOverlay : MonoBehaviour
	{
		private const int WindowId = 0x43525454;		// "CRTT"

		private static TooltipOverlay? instance;

		// von SettingsGui beim Repaint gesetzt (Bildschirmkoordinaten ohne Clipping, UMM-Skalierung in Matrix)
		private static string text = "";
		private static Vector2 mouse;
		private static Rect area;			// Settings-Bereich (Breite/Lage)
		private static Matrix4x4 matrix = Matrix4x4.identity;
		private static GUISkin? skin;
		private static int frame = -10;

		private static GUIStyle? style;
		private Rect rect;
		private GUIContent content = GUIContent.none;

		/// <summary>Beim Repaint von Draw aufrufen.</summary>
		public static void Show(string tip, Vector2 mouseScreen, Rect areaScreen, Matrix4x4 guiMatrix, GUISkin guiSkin)
		{
			text = tip;
			mouse = mouseScreen;
			area = areaScreen;
			matrix = guiMatrix;
			skin = guiSkin;
			frame = Time.frameCount;

			if (instance == null)
			{
				var go = new GameObject("CareerRework.Tooltip") { hideFlags = HideFlags.HideAndDontSave };
				DontDestroyOnLoad(go);
				instance = go.AddComponent<TooltipOverlay>();
			}
		}

		private void OnGUI()
		{
			// nur solange Draw den Tooltip in diesem/letzten Frame gemeldet hat
			if (string.IsNullOrEmpty(text) || Time.frameCount - frame > 1)
				return;

			Matrix4x4 oldMatrix = GUI.matrix;
			GUISkin oldSkin = GUI.skin;
			GUI.matrix = matrix;
			if (skin != null)
				GUI.skin = skin;

			style ??= new GUIStyle(GUI.skin.label)
			{
				wordWrap = true,
				richText = true,
				alignment = TextAnchor.UpperLeft,
				padding = new RectOffset(10, 10, 8, 8),
				normal = { textColor = new Color(0.95f, 0.95f, 0.95f) }
			};

			// Breite: an den Settings-Bereich angelehnt (knapp die Hälfte), mind. 320, nie breiter als der Bereich
			const float margin = 6f, gap = 16f;
			float maxWidth = Mathf.Max(200f, area.width - 2f * margin);
			float width = Mathf.Min(maxWidth, Mathf.Clamp(area.width * 0.45f, 320f, 560f));
			content = new GUIContent(text);
			float height = style.CalcHeight(content, width);

			// waagerecht: links der Maus, sonst rechts, sonst so weit wie möglich – immer im Bereich
			float left = area.xMin + margin, right = area.xMax - margin;
			float x;
			if (mouse.x - gap - width >= left) x = mouse.x - gap - width;
			else if (mouse.x + gap + width <= right) x = mouse.x + gap;
			else x = Mathf.Clamp(mouse.x - width / 2f, left, Mathf.Max(left, right - width));

			// senkrecht: unter der Maus, sonst darüber – immer im sichtbaren Bereich
			float scaleY = Mathf.Abs(matrix.m11) > 0.0001f ? matrix.m11 : 1f;
			float top = Mathf.Max(0f, area.yMin), bottom = Mathf.Min(Screen.height / scaleY, area.yMax);
			if (bottom - top < height + 2f * margin) { top = 0f; bottom = Screen.height / scaleY; }
			float y = mouse.y + gap;
			if (y + height > bottom - margin) y = mouse.y - gap - height;
			y = Mathf.Clamp(y, top + margin, Mathf.Max(top + margin, bottom - margin - height));

			rect = new Rect(x, y, width, height);
			GUI.Window(WindowId, rect, DrawWindow, GUIContent.none, GUIStyle.none);
			GUI.BringWindowToFront(WindowId);

			GUI.matrix = oldMatrix;
			GUI.skin = oldSkin;
		}

		private void DrawWindow(int id)
		{
			var r = new Rect(0f, 0f, rect.width, rect.height);
			if (Event.current.type == EventType.Repaint)
			{
				Color old = GUI.color;
				GUI.color = new Color(0.05f, 0.05f, 0.05f, 1f);		// deckend
				GUI.DrawTexture(r, Texture2D.whiteTexture);
				GUI.color = new Color(1f, 0.8f, 0.3f, 1f);
				GUI.DrawTexture(new Rect(0f, 0f, r.width, 2f), Texture2D.whiteTexture);
				GUI.color = old;
			}
			if (style != null)
				GUI.Label(r, content, style);
		}

		private void OnDestroy()
		{
			if (instance == this)
				instance = null;
		}
	}
}