using RoR2;
using RoR2.UI;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR;

namespace VRMod
{
    /// <summary>
    /// An on-screen keyboard for VR. Text boxes in the menus (game browser search and filters,
    /// lobby password, capacities...) are unreachable otherwise: no PC OpenXR runtime offers a
    /// system keyboard to games, so the mod draws its own.
    ///
    /// Opens when A is pressed while a text box is the selected menu control. While open it owns
    /// the controllers completely (the game's menu input is muted): left stick moves over the
    /// keys, A types the key, X is backspace, Y is space, B closes and keeps the text, and the
    /// DONE key confirms the box the way Enter does. Text is written straight into the game's own
    /// input field, so filters and searches react exactly as with a real keyboard.
    /// </summary>
    internal static class VRKeyboard
    {
        internal static bool IsOpen => panel && panel.activeSelf;

        private static GameObject panel;
        private static Canvas canvas;
        private static TextMeshProUGUI preview;
        private static TextMeshProUGUI hint;
        private static readonly List<List<Key>> rows = new List<List<Key>>();
        private static FieldRef target;

        /// <summary>Wraps either a TextMeshPro or a legacy Unity input field.</summary>
        internal class FieldRef
        {
            public TMP_InputField tmp;
            public InputField legacy;
            public static FieldRef From(GameObject go)
            {
                if (!go) return null;
                TMP_InputField t = go.GetComponent<TMP_InputField>();
                if (t) return new FieldRef { tmp = t };
                InputField l = go.GetComponent<InputField>();
                if (l) return new FieldRef { legacy = l };
                // A settings row (host name, password, tags) is itself the selectable and holds
                // the box as a child.
                t = go.GetComponentInChildren<TMP_InputField>(true);
                if (t) return new FieldRef { tmp = t };
                l = go.GetComponentInChildren<InputField>(true);
                if (l) return new FieldRef { legacy = l };
                return null;
            }
            public bool Alive => tmp ? tmp.gameObject.activeInHierarchy : legacy && legacy.gameObject.activeInHierarchy;
            public GameObject gameObject => tmp ? tmp.gameObject : legacy ? legacy.gameObject : null;
            public string name => gameObject ? gameObject.name : "?";
            public string text
            {
                get { return tmp ? (tmp.text ?? "") : legacy ? (legacy.text ?? "") : ""; }
                set { if (tmp) { tmp.text = value; tmp.caretPosition = value.Length; } else if (legacy) { legacy.text = value; legacy.caretPosition = value.Length; } }
            }
            public int characterLimit => tmp ? tmp.characterLimit : legacy ? legacy.characterLimit : 0;
            public bool integerOnly => tmp ? tmp.contentType == TMP_InputField.ContentType.IntegerNumber : legacy && legacy.contentType == InputField.ContentType.IntegerNumber;
            public bool decimalOnly => tmp ? tmp.contentType == TMP_InputField.ContentType.DecimalNumber : legacy && legacy.contentType == InputField.ContentType.DecimalNumber;
            public bool isFocused => tmp ? tmp.isFocused : legacy && legacy.isFocused;
            public void Deactivate() { if (tmp) tmp.DeactivateInputField(); else if (legacy) legacy.DeactivateInputField(); }
            public void EndEdit() { try { if (tmp) tmp.onEndEdit.Invoke(tmp.text); else if (legacy) legacy.onEndEdit.Invoke(legacy.text); } catch { } }
            public void Submit() { try { if (tmp) tmp.onSubmit.Invoke(tmp.text); else if (legacy) legacy.onSubmit.Invoke(legacy.text); } catch { } }
        }

        /// <summary>
        /// Invisible button laid over a text box. Menu navigation lands on the proxy instead of the
        /// box itself, because selecting a real input field puts it into typing mode (and
        /// deactivating it again deselects it), which fought the stick navigation.
        /// </summary>
        internal class FieldProxy : MonoBehaviour
        {
            public GameObject field;
        }

        /// <summary>Makes sure the given text box has a navigable proxy; returns it.</summary>
        internal static Selectable EnsureProxy(Selectable fieldSelectable)
        {
            if (!fieldSelectable) return null;
            Transform existing = fieldSelectable.transform.Find("VRKeyboardProxy");
            if (existing) return existing.GetComponent<Selectable>();
            GameObject go = new GameObject("VRKeyboardProxy", typeof(RectTransform));
            go.transform.SetParent(fieldSelectable.transform, false);
            go.layer = fieldSelectable.gameObject.layer;
            RectTransform rt = (RectTransform)go.transform;
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one; rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
            Image img = go.AddComponent<Image>();
            img.color = new Color(0f, 0f, 0f, 0f);
            img.raycastTarget = false;
            go.AddComponent<LayoutElement>().ignoreLayout = true;
            // The game's controls only navigate to targets that carry an MPEventSystemLocator for
            // the same event system (MPControlHelper.FilterSelectable) - a plain Button is skipped.
            go.AddComponent<MPEventSystemLocator>();
            MPButton b = go.AddComponent<MPButton>();
            b.allowAllEventSystems = true;
            b.transition = Selectable.Transition.None;
            FieldProxy proxy = go.AddComponent<FieldProxy>();
            proxy.field = fieldSelectable.gameObject;
            b.onClick.AddListener(() => { if (ModConfig.VRKeyboardEnabled.Value) Open(FieldRef.From(proxy.field)); });
            return b;
        }
        private static int curRow, curCol;
        private static bool shift, symbols;
        private static TMP_FontAsset font;
        private static Material fontMat;

        private static bool aWas, bWas, xWas, yWas;
        private static float stickRepeatAt;
        private static Vector2 lastStickDir;
        private static bool ignoreFirstA;

        private class Key
        {
            public string lower, upper, sym;
            public string special; // "SHIFT" "SYM" "SPACE" "BACK" "DONE" "CLOSE"
            public float width = 1f;
            public Button button;
            public Image image;
            public TextMeshProUGUI label;
            public string Current => special != null ? special : symbols ? sym : shift ? upper : lower;
        }

        private static readonly string[][] layout =
        {
            new[] { "1", "2", "3", "4", "5", "6", "7", "8", "9", "0" },
            new[] { "q", "w", "e", "r", "t", "y", "u", "i", "o", "p" },
            new[] { "a", "s", "d", "f", "g", "h", "j", "k", "l", "-" },
            new[] { "z", "x", "c", "v", "b", "n", "m", ",", ".", "_" },
        };
        private static readonly string[][] symLayout =
        {
            new[] { "!", "@", "#", "$", "%", "^", "&", "*", "(", ")" },
            new[] { "+", "=", "/", "\\", ":", ";", "'", "\"", "?", "~" },
            new[] { "[", "]", "{", "}", "<", ">", "|", "`", ",", "." },
            new[] { "1", "2", "3", "4", "5", "6", "7", "8", "9", "0" },
        };

        internal static void Init()
        {
            RoR2Application.onUpdate += WatchForOpen;
            RoR2Application.onUpdate += WatchForChat;
        }

        /// <summary>Called every input frame from Controllers while the keyboard is open.</summary>
        internal static void Tick()
        {
            if (!IsOpen) return;
            if (target == null || !target.Alive) { Close(false); return; }

            InputDevice left = InputDevices.GetDeviceAtXRNode(XRNode.LeftHand);
            InputDevice right = InputDevices.GetDeviceAtXRNode(XRNode.RightHand);

            Vector2 stick = Vector2.zero;
            if (left.isValid) left.TryGetFeatureValue(CommonUsages.primary2DAxis, out stick);
            HandleStick(stick);

            bool a = Pressed(right, CommonUsages.primaryButton);
            bool b = Pressed(right, CommonUsages.secondaryButton);
            bool x = Pressed(left, CommonUsages.primaryButton);
            bool y = Pressed(left, CommonUsages.secondaryButton);

            if (a && !aWas)
            {
                if (ignoreFirstA) ignoreFirstA = false; // the press that opened us
                else Activate(rows[curRow][curCol]);
            }
            if (b && !bWas) Close(chatTarget && target != null && target.text.Length > 0);
            if (x && !xWas) Backspace();
            if (y && !yWas) Type(" ");

            aWas = a; bWas = b; xWas = x; yWas = y;
            if (!a) ignoreFirstA = false;
        }

        private static bool Pressed(InputDevice d, InputFeatureUsage<bool> usage)
        {
            bool v = false;
            return d.isValid && d.TryGetFeatureValue(usage, out v) && v;
        }

        private static void HandleStick(Vector2 stick)
        {
            Vector2 dir = Vector2.zero;
            if (stick.magnitude > 0.6f)
                dir = Mathf.Abs(stick.x) > Mathf.Abs(stick.y) ? new Vector2(Mathf.Sign(stick.x), 0) : new Vector2(0, Mathf.Sign(stick.y));

            if (dir == Vector2.zero) { lastStickDir = Vector2.zero; return; }
            bool newDir = dir != lastStickDir;
            if (!newDir && Time.unscaledTime < stickRepeatAt) return;
            stickRepeatAt = Time.unscaledTime + (newDir ? 0.35f : 0.12f);
            lastStickDir = dir;
            Move((int)dir.x, (int)-dir.y);
        }

        private static void Move(int dx, int dy)
        {
            if (dy != 0)
            {
                // Keep the horizontal position by key centre when rows have different widths.
                float xCentre = KeyCentre(curRow, curCol);
                curRow = Mathf.Clamp(curRow + dy, 0, rows.Count - 1);
                int best = 0; float bestD = float.MaxValue;
                for (int i = 0; i < rows[curRow].Count; i++)
                {
                    float d = Mathf.Abs(KeyCentre(curRow, i) - xCentre);
                    if (d < bestD) { bestD = d; best = i; }
                }
                curCol = best;
            }
            if (dx != 0) curCol = Mathf.Clamp(curCol + dx, 0, rows[curRow].Count - 1);
            RefreshHighlight();
        }

        private static float KeyCentre(int row, int col)
        {
            float x = 0f;
            for (int i = 0; i < col; i++) x += rows[row][i].width;
            return x + rows[row][col].width * 0.5f;
        }

        private static void Activate(Key key)
        {
            switch (key.special)
            {
                case "SHIFT": shift = !shift; RefreshLabels(); return;
                case "SYM": symbols = !symbols; RefreshLabels(); return;
                case "SPACE": Type(" "); return;
                case "BACK": Backspace(); return;
                case "DONE": Close(true); return;
                case "CLOSE": Close(false); return;
            }
            Type(key.Current);
            if (shift) { shift = false; RefreshLabels(); }
        }

        private static void Type(string s)
        {
            if (target == null) return;
            if (target.characterLimit > 0 && target.text.Length + s.Length > target.characterLimit) return;
            // Respect numeric boxes (Max Ping, capacities).
            if (target.integerOnly || target.decimalOnly)
            {
                foreach (char c in s)
                    if (!char.IsDigit(c) && c != '-' && !(c == '.' && target.decimalOnly)) return;
            }
            target.text = target.text + s;
            preview.text = target.text;
        }

        private static void Backspace()
        {
            if (target == null || target.text.Length == 0) return;
            target.text = target.text.Substring(0, target.text.Length - 1);
            preview.text = target.text;
        }

        // ------------------------------------------------------------------ open / close

        private static bool submitWas;
        private static ChatBox chatTarget;
        private static bool stickClickWas;

        /// <summary>
        /// Chat: click the LEFT stick to type a message - in the character select lobby, and
        /// during a run only while the game is paused (so it can never fire mid-fight). DONE sends.
        /// </summary>
        private static void WatchForChat()
        {
            if (IsOpen || !ModConfig.VRKeyboardEnabled.Value || !ModConfig.VRChatEnabled.Value) return;
            InputDevice left = InputDevices.GetDeviceAtXRNode(XRNode.LeftHand);
            bool click = Pressed(left, CommonUsages.primary2DAxisClick);
            bool edge = click && !stickClickWas;
            stickClickWas = click;
            if (!edge) return;

            bool allowed = LobbyNavigation.IsInLobby || (Run.instance && PauseManager.isPaused);
            if (!allowed) return;

            ChatBox box = ChatBox.instance;
            if (!box || !box.inputField || !box.gameObject.activeInHierarchy) return;
            chatTarget = box;
            Open(new FieldRef { tmp = box.inputField });
            if (IsOpen) hint.text = "CHAT   <color=#FFD24A>A</color> type   <color=#FFD24A>X</color> backspace   <color=#FFD24A>Y</color> space   <color=#FFD24A>B</color> / DONE = send (B with nothing typed = close)";
        }

        /// <summary>Opens the keyboard when A is pressed on a selected text box.</summary>
        private static void WatchForOpen()
        {
            if (IsOpen || !ModConfig.VRKeyboardEnabled.Value) return;
            MPEventSystem es = null;
            try { LocalUser lu = LocalUserManager.GetFirstLocalUser(); es = lu != null ? lu.eventSystem : null; } catch { }
            if (!es) return;

            GameObject sel = es.currentSelectedGameObject;
            FieldRef field = null;
            if (sel)
            {
                FieldProxy proxy = sel.GetComponent<FieldProxy>();
                field = proxy ? FieldRef.From(proxy.field) : FieldRef.From(sel);
            }
            if (field == null) { submitWas = false; return; }

            InputDevice right = InputDevices.GetDeviceAtXRNode(XRNode.RightHand);
            bool submit = Pressed(right, CommonUsages.primaryButton);
            bool edge = submit && !submitWas;
            submitWas = submit;
            if (!edge) return;

            Open(field);
        }

        private static GameObject selectionBeforeOpen;

        private static MPEventSystem CurrentEventSystem()
        {
            try { LocalUser lu = LocalUserManager.GetFirstLocalUser(); return lu != null ? lu.eventSystem : null; } catch { return null; }
        }

        internal static void Open(FieldRef field)
        {
            if (field == null || !field.Alive || IsOpen) return;
            if (!panel) Build();
            target = field;
            MPEventSystem esOpen = CurrentEventSystem();
            selectionBeforeOpen = esOpen ? esOpen.currentSelectedGameObject : null;
            shift = false; symbols = false;
            curRow = 1; curCol = 0;
            ignoreFirstA = true;
            aWas = bWas = xWas = yWas = false;

            // The field must not swallow input itself while we drive it.
            if (field.isFocused) field.Deactivate();

            Place();
            RefreshLabels();
            preview.text = field.text;
            hint.text = "<color=#FFD24A>A</color> type   <color=#FFD24A>X</color> backspace   <color=#FFD24A>Y</color> space   <color=#FFD24A>B</color> close   DONE = confirm";
            panel.SetActive(true);
            VRMod.StaticLogger.LogInfo($"[VR keyboard] Opened for '{field.name}'.");
        }

        internal static void Close(bool submit)
        {
            if (!panel) return;
            panel.SetActive(false);
            FieldRef f = target;
            ChatBox chat = chatTarget;
            target = null;
            chatTarget = null;
            if (chat)
            {
                // Chat: sends through the game's own path (which also clears the box); an empty
                // draft just closes. The lobby's chat box re-focuses its text box after a send
                // (deselectAfterSubmitChat is off there), which left the box selected and typing
                // so the next B went to the lobby instead. Force the "deselect after send" path,
                // then hide the input the same way Escape does.
                if (submit)
                {
                    bool deselect = chat.deselectAfterSubmitChat;
                    chat.deselectAfterSubmitChat = true;
                    try { chat.SubmitChat(); }
                    catch (System.Exception e) { VRMod.StaticLogger.LogWarning("[VR keyboard] chat send: " + e.Message); }
                    finally { chat.deselectAfterSubmitChat = deselect; }
                }
                else if (chat.inputField) chat.inputField.text = "";
                try { chat.SetShowInput(false); } catch { }
                try { chat.UnfocusInputField(); } catch { }
                if (chat.inputField) chat.inputField.DeactivateInputField(true);
            }
            else if (f != null && f.Alive)
            {
                f.EndEdit();
                if (submit) f.Submit();
            }

            // Put the menu highlight back where it was, so the stick and buttons carry on from
            // the same control instead of a dead selection (where B would back out of the screen).
            MPEventSystem es = CurrentEventSystem();
            if (es)
            {
                GameObject restore = selectionBeforeOpen && selectionBeforeOpen.activeInHierarchy ? selectionBeforeOpen : null;
                if (chat && (!restore || restore == chat.inputField.gameObject))
                {
                    Selectable fallback = LobbyNavigation.DefaultSelectable();
                    restore = fallback ? fallback.gameObject : null;
                }
                if (restore) es.SetSelectedGameObject(restore);
            }
            selectionBeforeOpen = null;
            // Swallow the closing press so the menu underneath does not react to it.
            Controllers.SuppressUntilReleased(Time.unscaledTime + 0.2f);
        }

        private static void Place()
        {
            CameraRigController rig = Utils.localCameraRig;
            Camera cam = rig ? rig.uiCam : Camera.main;
            if (!cam) return;
            canvas.worldCamera = cam;
            Vector3 fwd = cam.transform.forward; fwd.y = 0f; fwd.Normalize();
            Vector3 pos = cam.transform.position + fwd * 10f + Vector3.down * 3.2f;
            panel.transform.position = pos;
            panel.transform.rotation = Quaternion.LookRotation(fwd, Vector3.up) * Quaternion.Euler(20f, 0f, 0f);
        }

        // ------------------------------------------------------------------ building

        private static void Build()
        {
            panel = new GameObject("VRKeyboard", typeof(RectTransform));
            Object.DontDestroyOnLoad(panel);
            panel.layer = LayerIndex.ui.intVal;
            canvas = panel.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.overrideSorting = true;
            canvas.sortingOrder = 400;
            panel.AddComponent<CanvasScaler>().dynamicPixelsPerUnit = 2f;
            RectTransform rt = (RectTransform)panel.transform;
            rt.sizeDelta = new Vector2(1200f, 560f);
            rt.localScale = new Vector3(0.006f, 0.006f, 0.006f);

            BorrowFont();

            Image bg = MakeImage(panel.transform, "Background", new Color(0.06f, 0.07f, 0.09f, 0.94f));
            Stretch(bg.rectTransform, Vector2.zero, Vector2.zero);
            Outline ol = bg.gameObject.AddComponent<Outline>();
            ol.effectColor = new Color(1f, 0.82f, 0.2f, 0.9f);
            ol.effectDistance = new Vector2(3f, -3f);

            Image previewBg = MakeImage(panel.transform, "PreviewBg", new Color(0.12f, 0.13f, 0.16f, 1f));
            previewBg.rectTransform.anchorMin = new Vector2(0f, 1f);
            previewBg.rectTransform.anchorMax = new Vector2(1f, 1f);
            previewBg.rectTransform.pivot = new Vector2(0.5f, 1f);
            previewBg.rectTransform.anchoredPosition = new Vector2(0f, -16f);
            previewBg.rectTransform.sizeDelta = new Vector2(-40f, 64f);
            preview = MakeText(previewBg.transform, "Preview", "", 34, TextAlignmentOptions.MidlineLeft);
            Stretch(preview.rectTransform, new Vector2(16f, 4f), new Vector2(-16f, -4f));
            preview.enableWordWrapping = false;
            preview.overflowMode = TextOverflowModes.Ellipsis;

            hint = MakeText(panel.transform, "Hint", "", 22, TextAlignmentOptions.Center);
            hint.rectTransform.anchorMin = new Vector2(0f, 0f);
            hint.rectTransform.anchorMax = new Vector2(1f, 0f);
            hint.rectTransform.pivot = new Vector2(0.5f, 0f);
            hint.rectTransform.anchoredPosition = new Vector2(0f, 10f);
            hint.rectTransform.sizeDelta = new Vector2(-40f, 30f);
            hint.color = new Color(0.85f, 0.85f, 0.85f, 1f);

            // Key rows.
            rows.Clear();
            for (int r = 0; r < layout.Length; r++)
            {
                List<Key> row = new List<Key>();
                for (int c = 0; c < layout[r].Length; c++)
                    row.Add(new Key { lower = layout[r][c], upper = layout[r][c].ToUpperInvariant(), sym = symLayout[r][c] });
                rows.Add(row);
            }
            rows.Add(new List<Key>
            {
                new Key { special = "SHIFT", width = 1.5f },
                new Key { special = "SYM", width = 1.5f },
                new Key { special = "SPACE", width = 4f },
                new Key { special = "BACK", width = 1.5f },
                new Key { special = "DONE", width = 1.5f },
            });

            const float keyW = 104f, keyH = 78f, gap = 8f, top = 100f, leftPad = 40f;
            for (int r = 0; r < rows.Count; r++)
            {
                float x = leftPad;
                foreach (Key k in rows[r])
                {
                    float w = keyW * k.width + gap * (k.width - 1f);
                    GameObject go = new GameObject("Key", typeof(RectTransform));
                    go.transform.SetParent(panel.transform, false);
                    go.layer = panel.layer;
                    RectTransform krt = (RectTransform)go.transform;
                    krt.anchorMin = krt.anchorMax = new Vector2(0f, 1f);
                    krt.pivot = new Vector2(0f, 1f);
                    krt.anchoredPosition = new Vector2(x, -(top + r * (keyH + gap)));
                    krt.sizeDelta = new Vector2(w, keyH);
                    k.image = go.AddComponent<Image>();
                    k.image.color = new Color(0.2f, 0.22f, 0.27f, 1f);
                    k.image.raycastTarget = false;
                    k.label = MakeText(go.transform, "Label", "", 34, TextAlignmentOptions.Center);
                    Stretch(k.label.rectTransform, Vector2.zero, Vector2.zero);
                    x += w + gap;
                }
            }
            panel.SetActive(false);
        }

        private static void RefreshLabels()
        {
            foreach (List<Key> row in rows)
                foreach (Key k in row)
                {
                    string t = k.Current;
                    if (k.special == "SHIFT") t = shift ? "SHIFT*" : "SHIFT";
                    else if (k.special == "SYM") t = symbols ? "abc" : "#+=";
                    else if (k.special == "BACK") t = "<-";
                    else if (k.special == "SPACE") t = "space";
                    k.label.text = t;
                }
            RefreshHighlight();
        }

        private static void RefreshHighlight()
        {
            for (int r = 0; r < rows.Count; r++)
                for (int c = 0; c < rows[r].Count; c++)
                {
                    bool sel = r == curRow && c == curCol;
                    Key k = rows[r][c];
                    k.image.color = sel ? new Color(1f, 0.82f, 0.2f, 1f) : (k.special != null ? new Color(0.16f, 0.18f, 0.22f, 1f) : new Color(0.2f, 0.22f, 0.27f, 1f));
                    k.label.color = sel ? Color.black : Color.white;
                }
        }

        private static void BorrowFont()
        {
            TMP_Text donor = null;
            try
            {
                if (RoR2Application.instance && RoR2Application.instance.mainCanvas)
                    donor = RoR2Application.instance.mainCanvas.GetComponentsInChildren<TMP_Text>(true).FirstOrDefault(t => t.font);
                if (!donor) donor = Resources.FindObjectsOfTypeAll<TMP_Text>().FirstOrDefault(t => t.font && t.gameObject.scene.name != null);
            }
            catch { }
            if (donor) { font = donor.font; fontMat = donor.fontSharedMaterial; }
        }

        private static Image MakeImage(Transform parent, string name, Color color)
        {
            GameObject go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            go.layer = panel.layer;
            Image img = go.AddComponent<Image>();
            img.color = color;
            img.raycastTarget = false;
            return img;
        }

        private static TextMeshProUGUI MakeText(Transform parent, string name, string text, float size, TextAlignmentOptions align)
        {
            GameObject go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            go.layer = panel.layer;
            TextMeshProUGUI t = go.AddComponent<TextMeshProUGUI>();
            if (font) { t.font = font; if (fontMat) t.fontSharedMaterial = fontMat; }
            t.text = text;
            t.fontSize = size;
            t.alignment = align;
            t.color = Color.white;
            t.raycastTarget = false;
            return t;
        }

        private static void Stretch(RectTransform rt, Vector2 min, Vector2 max)
        {
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
            rt.offsetMin = min; rt.offsetMax = max;
        }
    }
}
