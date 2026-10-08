using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace MiniMayhem
{
    /// <summary>Palette for the chunky, bright UI.</summary>
    public static class UiColors
    {
        public static readonly Color Bg = new(0.13f, 0.1f, 0.2f, 0.94f);
        public static readonly Color Panel = new(0.22f, 0.18f, 0.33f, 0.97f);
        public static readonly Color PanelLight = new(0.31f, 0.26f, 0.45f, 1f);
        public static readonly Color Button = new(0.36f, 0.3f, 0.55f, 1f);
        public static readonly Color ButtonFocus = new(1f, 0.78f, 0.25f, 1f);
        public static readonly Color Text = new(1f, 0.97f, 0.92f);
        public static readonly Color TextDim = new(0.72f, 0.68f, 0.82f);
        public static readonly Color Gold = new(1f, 0.83f, 0.25f);
        public static readonly Color Good = new(0.45f, 0.95f, 0.5f);
        public static readonly Color Bad = new(1f, 0.4f, 0.4f);
        public static readonly Color Locked = new(0.25f, 0.23f, 0.3f, 1f);
        public static readonly Color Ink = new(0.12f, 0.09f, 0.17f);
    }

    /// <summary>
    /// Makes the focused selectable obvious (gold outline + slight pop) and selects on mouse hover, so mouse and
    /// controller always agree on what is focused.
    /// </summary>
    public class FocusMarker : MonoBehaviour, IPointerEnterHandler
    {
        public Image outline;
        public float popScale = 1.06f;
        Selectable sel;
        Vector3 baseScale = Vector3.one;

        void Awake()
        {
            sel = GetComponent<Selectable>();
            baseScale = transform.localScale;
        }

        public bool Focused => EventSystem.current != null && EventSystem.current.currentSelectedGameObject == gameObject;

        void Update()
        {
            bool f = Focused;
            if (outline != null) outline.enabled = f;
            float target = f ? popScale : 1f;
            float s = Mathf.Lerp(transform.localScale.x / Mathf.Max(0.001f, baseScale.x), target, 1f - Mathf.Exp(-18f * Time.unscaledDeltaTime));
            transform.localScale = baseScale * s;
        }

        public void OnPointerEnter(PointerEventData e)
        {
            if (sel != null && sel.interactable && EventSystem.current != null) EventSystem.current.SetSelectedGameObject(gameObject);
        }
    }

    /// <summary>Code-built uGUI helpers.</summary>
    public static class Ui
    {
        static TMP_FontAsset font;

        public static TMP_FontAsset Font
        {
            get
            {
                if (font == null) font = TMP_Settings.defaultFontAsset != null ? TMP_Settings.defaultFontAsset : Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");
                return font;
            }
        }

        public static Sprite PanelSprite => Art.Get(ArtId.UiCard);

        public static RectTransform Node(Transform parent, string name)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return (RectTransform)go.transform;
        }

        public static RectTransform Stretch(this RectTransform rt, float left = 0, float bottom = 0, float right = 0, float top = 0)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(left, bottom);
            rt.offsetMax = new Vector2(-right, -top);
            return rt;
        }

        /// <summary>Anchor at a normalised point with a fixed size.</summary>
        public static RectTransform Place(this RectTransform rt, Vector2 anchor, Vector2 pos, Vector2 size, Vector2? pivot = null)
        {
            rt.anchorMin = rt.anchorMax = anchor;
            rt.pivot = pivot ?? anchor;
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;
            return rt;
        }

        public static Image Panel(Transform parent, string name, Color color, bool rounded = true)
        {
            var rt = Node(parent, name);
            var img = rt.gameObject.AddComponent<Image>();
            img.sprite = rounded ? PanelSprite : Art.Get(ArtId.Pixel);
            img.type = rounded ? Image.Type.Sliced : Image.Type.Simple;
            img.pixelsPerUnitMultiplier = rounded ? 0.7f : 1f;
            img.color = color;
            return img;
        }

        public static Image Icon(Transform parent, ArtId art, float size, string name = "Icon")
        {
            var rt = Node(parent, name);
            rt.sizeDelta = new Vector2(size, size);
            var img = rt.gameObject.AddComponent<Image>();
            img.sprite = Art.Get(art);
            img.preserveAspect = true;
            img.raycastTarget = false;
            return img;
        }

        public static TextMeshProUGUI Text(Transform parent, string text, float size, Color color, TextAlignmentOptions align = TextAlignmentOptions.Center, string name = "Text")
        {
            var rt = Node(parent, name);
            var t = rt.gameObject.AddComponent<TextMeshProUGUI>();
            t.font = Font;
            t.text = text;
            t.fontSize = size;
            t.color = color;
            t.alignment = align;
            t.raycastTarget = false;
            t.textWrappingMode = TextWrappingModes.Normal;
            t.overflowMode = TextOverflowModes.Truncate;
            return t;
        }

        /// <summary>Text with a dark outline for over-the-world HUD readability.</summary>
        public static TextMeshProUGUI OutlinedText(Transform parent, string text, float size, Color color, TextAlignmentOptions align = TextAlignmentOptions.Center)
        {
            var t = Text(parent, text, size, color, align);
            t.fontStyle = FontStyles.Bold;
            t.outlineWidth = 0.22f;
            t.outlineColor = new Color32(30, 20, 45, 255);
            return t;
        }

        /// <summary>
        /// Chunky button: the root image is the gold focus frame (only shown while focused), the child "Bg" is the
        /// tinted button face (targetGraphic), and the label sits on top.
        /// </summary>
        public static Button Button(Transform parent, string label, Action onClick, Vector2 size, float fontSize = 30, string name = null)
        {
            var frame = Panel(parent, name ?? ("Btn_" + label), UiColors.ButtonFocus);
            frame.rectTransform.sizeDelta = size;
            frame.raycastTarget = false;
            frame.enabled = false;
            var bg = Panel(frame.transform, "Bg", Color.white);
            bg.rectTransform.Stretch(6, 6, 6, 6);
            var b = frame.gameObject.AddComponent<Button>();
            b.targetGraphic = bg;
            var cb = b.colors;
            cb.normalColor = UiColors.Button;
            cb.highlightedColor = UiColors.Button;
            cb.selectedColor = new Color(0.47f, 0.4f, 0.72f);
            cb.pressedColor = UiColors.ButtonFocus;
            cb.disabledColor = UiColors.Locked;
            cb.colorMultiplier = 1f;
            cb.fadeDuration = 0.06f;
            b.colors = cb;
            if (onClick != null) b.onClick.AddListener(() => { Sfx.Play(SfxId.Click, 0.5f); onClick(); });
            var fm = frame.gameObject.AddComponent<FocusMarker>();
            fm.outline = frame;
            if (!string.IsNullOrEmpty(label))
            {
                var t = Text(frame.transform, label, fontSize, UiColors.Text);
                t.fontStyle = FontStyles.Bold;
                t.rectTransform.Stretch(14, 8, 14, 8);
            }
            return b;
        }

        /// <summary>The tinted face of a button made by <see cref="Button"/>.</summary>
        public static Image Bg(this Button b) => (Image)b.targetGraphic;

        public static TextMeshProUGUI Label(this Button b) => b.GetComponentInChildren<TextMeshProUGUI>();

        /// <summary>Horizontal bar: returns the fill image (use fillAmount).</summary>
        public static Image Bar(Transform parent, string name, Color back, Color fill, out Image backImg)
        {
            backImg = Panel(parent, name, back, false);
            var f = Panel(backImg.transform, "Fill", fill, false);
            f.rectTransform.Stretch(3, 3, 3, 3);
            f.type = Image.Type.Filled;
            f.fillMethod = Image.FillMethod.Horizontal;
            f.fillAmount = 1f;
            return f;
        }

        public static VerticalLayoutGroup VList(GameObject go, float spacing, TextAnchor align = TextAnchor.UpperCenter, int pad = 0)
        {
            var v = go.AddComponent<VerticalLayoutGroup>();
            v.spacing = spacing;
            v.childAlignment = align;
            v.childControlHeight = false;
            v.childControlWidth = false;
            v.childForceExpandHeight = false;
            v.childForceExpandWidth = false;
            v.padding = new RectOffset(pad, pad, pad, pad);
            return v;
        }

        public static HorizontalLayoutGroup HList(GameObject go, float spacing, TextAnchor align = TextAnchor.MiddleCenter, int pad = 0)
        {
            var h = go.AddComponent<HorizontalLayoutGroup>();
            h.spacing = spacing;
            h.childAlignment = align;
            h.childControlHeight = false;
            h.childControlWidth = false;
            h.childForceExpandHeight = false;
            h.childForceExpandWidth = false;
            h.padding = new RectOffset(pad, pad, pad, pad);
            return h;
        }

        public static void Select(GameObject go)
        {
            if (EventSystem.current == null || go == null) return;
            EventSystem.current.SetSelectedGameObject(null);
            EventSystem.current.SetSelectedGameObject(go);
        }

        public static GameObject Selected => EventSystem.current != null ? EventSystem.current.currentSelectedGameObject : null;

        public static string Time(float seconds)
        {
            int s = Mathf.Max(0, Mathf.FloorToInt(seconds));
            return $"{s / 60}:{s % 60:00}";
        }

        public static string Hex(Color c) => ColorUtility.ToHtmlStringRGB(c);
    }
}
