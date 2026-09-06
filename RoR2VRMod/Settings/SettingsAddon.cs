using BepInEx.Configuration;
using MonoMod.Cil;
using Rewired;
using RoR2.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;

namespace VRMod
{
    //Thank you KingEnderBrine. Your code from ExtraSkillSlots have been greatly helpful for this part.
    internal static class SettingsAddon
    {
        internal static void Init()
        {
            On.RoR2.UI.MainMenu.SubmenuMainMenuScreen.OnEnter += (orig, self, controller) =>
            {
                orig(self, controller);
                // The original mod gated on the prefab being named exactly "SettingsPanel". If the
                // game renames that prefab in a future build the whole VR tab silently disappears -
                // which is what happened here. Detect the settings screen by its STRUCTURE instead
                // (a header navigator + a SubPanelArea), so the tab keeps working across renames and
                // we skip the Profile/Multiplayer submenus (which lack that structure) without noise.
                if (LooksLikeSettingsPanel(self.submenuPanelInstance))
                {
                    SetupVRSettings(self.submenuPanelInstance);
                }
                else if (self.submenuPanelInstance)
                {
                    VRMod.StaticLogger.LogInfo($"[VR settings] Main-menu submenu '{self.submenuPanelInstance.name}' (prefab '{(self.submenuPanelPrefab ? self.submenuPanelPrefab.name : "null")}') is not the settings panel; VR tab not added here.");
                }
            };

            On.RoR2.UI.MainMenu.SubmenuMainMenuScreen.OnExit += (orig, self, controller) =>
            {
                orig(self, controller);
                SaveSettings();
            };

            On.RoR2.UI.PauseScreenController.OpenSettingsMenu += (orig, self) =>
            {
                orig(self);
                VRMod.StaticLogger.LogInfo("[VR settings] In-run pause settings opened; building VR tab.");
                SetupVRSettings(self.submenuObject);
            };

            IL.RoR2.UI.PauseScreenController.Update += SaveOnClose;

            On.RoR2.UI.BaseSettingsControl.GetCurrentValue += GetVRSettingValue;

            On.RoR2.UI.BaseSettingsControl.SubmitSettingInternal += SubmitConfig;

            On.RoR2.UI.BaseSettingsControl.Awake += StopError;

            On.RoR2.UI.HGButton.OnSelect += ScrollToButton;
        }

        private static void ScrollToButton(On.RoR2.UI.HGButton.orig_OnSelect orig, HGButton self, BaseEventData eventData)
        {
            orig(self, eventData);
            if (self.gameObject.name.Contains("VRModSetting"))
            {
                HGScrollRectHelper scrollHelper = self.GetComponentInParent<HGScrollRectHelper>();
                scrollHelper.ScrollToShowMe(self);
            }
        }

        private static void StopError(On.RoR2.UI.BaseSettingsControl.orig_Awake orig, BaseSettingsControl self)
        {
            if (self.gameObject.name.Contains("VRModSetting"))
            {
                self.eventSystemLocator = self.GetComponent<MPEventSystemLocator>();
                if (self.nameLabel && !string.IsNullOrEmpty(self.nameToken))
                {
                    self.nameLabel.token = self.nameToken;
                }
                return;
            }
            orig(self);
        }

        private static void SubmitConfig(On.RoR2.UI.BaseSettingsControl.orig_SubmitSettingInternal orig, BaseSettingsControl self, string newValue)
        {
            if (self.gameObject.name.Contains("VRModSetting"))
            {
                ModConfig.ConfigSetting setting;
                if (ModConfig.settings.TryGetValue(self.settingName, out setting))
                {
                    ConfigEntryBase entry = setting.entry;
                    if (entry.SettingType == typeof(bool))
                    {
                        (entry as ConfigEntry<bool>).Value = newValue == "1";
                    }
                    else if (entry.SettingType == typeof(int))
                    {
                        float parsedValue = float.Parse(newValue);
                        (entry as ConfigEntry<int>).Value = (int)parsedValue;
                    }
                    else if (entry.SettingType == typeof(float))
                    {
                        float parsedValue = float.Parse(newValue, System.Globalization.CultureInfo.InvariantCulture);
                        (entry as ConfigEntry<float>).Value = parsedValue;
                    }
                    else if (entry.SettingType == typeof(string))
                    {
                        (entry as ConfigEntry<string>).Value = newValue;
                    }
                }
                RoR2.RoR2Application.onNextUpdate += self.OnUpdateControls;
                return;
            }
            orig(self, newValue);
        }

        private static string GetVRSettingValue(On.RoR2.UI.BaseSettingsControl.orig_GetCurrentValue orig, BaseSettingsControl self)
        {
            if (self.gameObject.name.Contains("VRModSetting"))
            {
                ModConfig.ConfigSetting setting;
                if (ModConfig.settings.TryGetValue(self.settingName, out setting))
                {
                    ConfigEntryBase entry = setting.entry;
                    if (entry.SettingType == typeof(bool))
                    {
                        return (entry as ConfigEntry<bool>).Value ? "1" : "0";
                    }
                    else if (entry.SettingType == typeof(int))
                    {
                        return TextSerialization.ToStringInvariant((entry as ConfigEntry<int>).Value);
                    }
                    else if (entry.SettingType == typeof(float))
                    {
                        return TextSerialization.ToStringInvariant((entry as ConfigEntry<float>).Value);
                    }
                    else if (entry.SettingType == typeof(string))
                    {
                        string value = (entry as ConfigEntry<string>).Value;

                        if (Array.Exists((self as CarouselController).choices, x => x.convarValue == value))
                        {
                            return value;
                        }
                        else
                        {
                            if (self.settingName == "vr_ray_color")
                                return "#FFFFFF";
                            else if (self.settingName == "vr_haptics_suit")
                                return "None";
                        }
                    }
                }
                else
                {
                    return "0";
                }
            }
            return orig(self);
        }

        private static void SaveOnClose(ILContext il)
        {
            ILCursor c = new ILCursor(il);

            c.GotoNext(x => x.MatchCallvirt<GameObject>("SetActive"));

            c.Index++;
            c.EmitDelegate<Action>(() =>
            {
                SaveSettings();
            });
        }

        private static void SaveSettings()
        {
            ModConfig.Save();
        }

        /// <summary>
        /// True if this panel is the settings screen, detected by structure rather than by the
        /// prefab's name (which the game can rename between builds). The settings screen is the only
        /// submenu that carries an HGHeaderNavigationController together with a "SafeArea/SubPanelArea".
        /// </summary>
        private static bool LooksLikeSettingsPanel(GameObject panel)
        {
            if (!panel) return false;
            if (!panel.GetComponent<HGHeaderNavigationController>()) return false;
            return (bool)panel.transform.Find("SafeArea/SubPanelArea");
        }

        internal static void SetupVRSettings(GameObject panel)
        {
            try
            {
                SetupVRSettingsInternal(panel);
            }
            catch (System.Exception e)
            {
                VRMod.StaticLogger.LogError($"[VR settings] Could not build the VR settings tab - the settings panel hierarchy in this game build differs from what the mod expects. Details: {e}");
                if (panel) DumpSettingsHierarchy(panel);
            }
        }

        private static void SetupVRSettingsInternal(GameObject panel)
        {
            HGHeaderNavigationController controller = panel.GetComponent<HGHeaderNavigationController>();

            if (!controller)
            {
                VRMod.StaticLogger.LogWarning("[VR settings] No HGHeaderNavigationController on the settings panel; VR tab skipped.");
                return;
            }

            if (controller.headers != null && controller.headers.Exists(h => h.headerName == "VR"))
            {
                VRMod.StaticLogger.LogInfo("[VR settings] VR tab already present on this settings panel; nothing to do.");
                return;
            }

            Transform header = panel.transform.Find("SafeArea/HeaderContainer/Header (JUICED)");
            Transform subPanelArea = panel.transform.Find("SafeArea/SubPanelArea");

            if (!header || !subPanelArea)
            {
                VRMod.StaticLogger.LogWarning($"[VR settings] Settings paths not found (header={(bool)header}, subPanelArea={(bool)subPanelArea}); the panel hierarchy changed in this game build. VR tab skipped.");
                DumpSettingsHierarchy(panel);
                return;
            }

            GameObject subPanelInstance = SetupSubPanel(subPanelArea);

            GameObject headerInstance = SetupHeader(header);

            LanguageTextMeshController text = headerInstance.GetComponent<LanguageTextMeshController>();
            text.token = "VR";

            HGHeaderNavigationController.Header headerInfo = new HGHeaderNavigationController.Header();
            headerInfo.headerButton = headerInstance.GetComponent<HGButton>();
            headerInfo.headerName = "VR";
            headerInfo.tmpHeaderText = headerInstance.GetComponentInChildren<HGTextMeshProUGUI>();
            headerInfo.headerRoot = subPanelInstance;

            controller.headers.Add(headerInfo);

            string headerNames = string.Join(", ", controller.headers.ConvertAll(h => h.headerName).ToArray());
            VRMod.StaticLogger.LogInfo($"[VR settings] VR tab added ({controller.headers.Count} headers now: {headerNames}).");
        }

        /// <summary>
        /// Logs the first two levels of the settings panel's transform hierarchy. Only runs when the
        /// tab could not be built, so a single log tells us exactly which child names this game build
        /// uses and the paths can be corrected precisely.
        /// </summary>
        private static void DumpSettingsHierarchy(GameObject panel)
        {
            try
            {
                System.Text.StringBuilder sb = new System.Text.StringBuilder();
                sb.AppendLine($"[VR settings] Hierarchy of '{panel.name}':");
                foreach (Transform child in panel.transform)
                {
                    sb.AppendLine($"  {child.name}");
                    foreach (Transform grandchild in child)
                        sb.AppendLine($"    {grandchild.name}");
                }
                VRMod.StaticLogger.LogInfo(sb.ToString());
            }
            catch { }
        }

        private static GameObject SetupSubPanel(Transform parent)
        {
            GameObject subPanelToInstantiate = parent.Find("SettingsSubPanel, Controls (Gamepad)").gameObject;

            GameObject subPanelInstance = GameObject.Instantiate(subPanelToInstantiate, parent);

            Transform instanceLayout = subPanelInstance.transform.Find("Scroll View/Viewport/VerticalLayout");

            foreach (Transform child in instanceLayout)
            {
                GameObject.Destroy(child.gameObject);
            }

            GameObject sliderSetting = subPanelToInstantiate.transform.Find("Scroll View/Viewport/VerticalLayout/SettingsEntryButton, Slider (Look Scale X)").gameObject;
            GameObject boolSetting = subPanelToInstantiate.transform.Find("Scroll View/Viewport/VerticalLayout/SettingsEntryButton, Bool (Invert X)").gameObject;
            GameObject carouselSetting = parent.transform.Find("SettingsSubPanel, Video/Scroll View/Viewport/VerticalLayout/SettingsEntryButton, Carousel (Vsync)").gameObject;

            LanguageTextMeshController descriptionText = parent.transform.Find("GenericDescriptionPanel/ContentSizeFitter/DescriptionText").GetComponent<LanguageTextMeshController>();

            bool first = true;
            foreach (KeyValuePair<string, ModConfig.ConfigSetting> keyValuePair in ModConfig.settings)
            {
                ModConfig.ConfigSetting setting = keyValuePair.Value;

                BaseSettingsControl settingInstance;
                if (setting.entry.SettingType == typeof(float) || setting.entry.SettingType == typeof(int))
                {
                    settingInstance = GameObject.Instantiate(sliderSetting, instanceLayout).GetComponent<BaseSettingsControl>();

                    SettingsSlider slider = settingInstance as SettingsSlider;
                    slider.minValue = setting.minValue;
                    slider.maxValue = setting.maxValue;
                    slider.formatString = setting.entry.SettingType == typeof(float) ? "{0:0.00}" : "{0:N0}";
                }
                else if (setting.entry.SettingType == typeof(bool))
                {
                    settingInstance = GameObject.Instantiate(boolSetting, instanceLayout).GetComponent<BaseSettingsControl>();
                }
                else
                {
                    settingInstance = GameObject.Instantiate(carouselSetting, instanceLayout).GetComponent<BaseSettingsControl>();

                    CarouselController carousel = settingInstance as CarouselController;
                    List<CarouselController.Choice> choices = new List<CarouselController.Choice>();
                    if (keyValuePair.Key == "vr_ray_color")
                    {
                        string[] choiceStrings = new string[]
                        {
                            "White",
                            "Green",
                            "Red",
                            "Blue",
                            "Yellow",
                            "Magenta",
                            "Cyan",
                            "Lime",
                            "Black"
                        };

                        string[] hexStrings = new string[]
                        {
                            "#FFFFFF",
                            "#008000",
                            "#FF0000",
                            "#0000FF",
                            "#FFFF00",
                            "#FF00FF",
                            "#00FFFF",
                            "#00FF00",
                            "#000000"
                        };

                        for (int i = 0; i < choiceStrings.Length; i++)
                        {
                            CarouselController.Choice choice = new CarouselController.Choice();
                            choice.convarValue = hexStrings[i];
                            choice.suboptionDisplayToken = choiceStrings[i];
                            choices.Add(choice);
                        }
                    }
                    else if (keyValuePair.Key == "vr_haptics_suit")
                    {
                        string[] choiceStrings = new string[]
                        {
                            "None",
                            "Shockwave",
                            "Bhaptics"
                        };

                        for (int i = 0; i < choiceStrings.Length; i++)
                        {
                            CarouselController.Choice choice = new CarouselController.Choice();
                            choice.convarValue = choiceStrings[i];
                            choice.suboptionDisplayToken = choiceStrings[i];
                            choices.Add(choice);
                        }
                    }

                    carousel.choices = choices.ToArray();
                }

                settingInstance.settingSource = BaseSettingsControl.SettingSource.ConVar;
                settingInstance.nameToken = setting.entry.Definition.Key;
                settingInstance.settingName = keyValuePair.Key;
                settingInstance.gameObject.name = "VRModSetting, " + settingInstance.nameToken;

                HGButton button = settingInstance.GetComponent<HGButton>();
                if (button)
                {
                    string prefixString = "";

                    if (setting.settingUpdate == ModConfig.ConfigSetting.SettingUpdate.NextStage)
                        prefixString = "[WILL APPLY NEXT STAGE] ";
                    else if (setting.settingUpdate == ModConfig.ConfigSetting.SettingUpdate.AfterRestart)
                        prefixString = "[RESTART REQUIRED] ";

                    button.updateTextOnHover = true;
                    button.hoverToken = prefixString + setting.entry.Description.Description;
                    button.hoverLanguageTextMeshController = descriptionText;

                    button.defaultFallbackButton = first;
                    first = false;
                }
            }

            // The "Recenter HMD" keyboard/gamepad binding rows used to be added here. Recenter is
            // driven straight off the right stick click in VR, so the rows were only confusing and
            // are no longer shown (the action itself still exists for the input side).

            subPanelInstance.transform.Find("Scroll View").gameObject.AddComponent<ScrollToSelection>();

            return subPanelInstance;
        }

        private static GameObject SetupHeader(Transform parent)
        {
            GameObject categoryToInstantiate = parent.Find("GenericHeaderButton (Graphics)").gameObject;

            GameObject headerInstance = GameObject.Instantiate(categoryToInstantiate, parent);

            headerInstance.transform.SetSiblingIndex(parent.childCount - 2);
            headerInstance.name = "GenericHeaderButton (VR)";

            return headerInstance;
        }
    }
}
