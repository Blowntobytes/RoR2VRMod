using RoR2;
using RoR2.UI;
using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace VRMod
{
    /// <summary>
    /// Shows the full item description (the detailed one from the logbook) in pickup notifications
    /// and item tooltips instead of the short pickup flavour text. Activated from the VR settings
    /// menu. Inspired by ItemStats (Moffein) but implemented natively.
    /// </summary>
    internal static class ItemHints
    {
        private static bool hooked;

        /// <summary>Extra height (in pixels) added to the notification description area.</summary>
        private const float ExtraDescriptionHeight = 120f;

        internal static void Init()
        {
            if (hooked) return;
            hooked = true;

            // Show the full description in the pickup notification popup.
            On.RoR2.UI.GenericNotification.SetItem += OnSetItem;
            On.RoR2.UI.GenericNotification.SetEquipment += OnSetEquipment;

            // Show the full description in item icon tooltips (inventory bar, scoreboard, etc.).
            On.RoR2.UI.TooltipProvider.SetContent += OnTooltipSetContent;

            // Ping an item on the ground → show its description in the chat/notification.
            On.RoR2.PingerController.SetCurrentPing += OnPing;

            // Show item description in the Command artifact picker panel.
            On.RoR2.UI.PickupPickerPanel.SetPickupOptions += OnSetPickupOptions;
        }

        private static void OnSetItem(On.RoR2.UI.GenericNotification.orig_SetItem orig, GenericNotification self, ItemDef itemDef)
        {
            orig(self, itemDef);

            if (!ModConfig.ItemHints.Value || !itemDef) return;

            string desc = Language.GetString(itemDef.descriptionToken);
            if (!string.IsNullOrEmpty(desc) && !Language.IsTokenInvalid(itemDef.descriptionToken))
            {
                if (self.descriptionText)
                    self.descriptionText.token = itemDef.descriptionToken;
            }

            ExpandNotification(self);
        }

        private static void OnSetEquipment(On.RoR2.UI.GenericNotification.orig_SetEquipment orig, GenericNotification self, EquipmentDef equipmentDef)
        {
            orig(self, equipmentDef);

            if (!ModConfig.ItemHints.Value || !equipmentDef) return;

            string desc = Language.GetString(equipmentDef.descriptionToken);
            if (!string.IsNullOrEmpty(desc) && !Language.IsTokenInvalid(equipmentDef.descriptionToken))
            {
                if (self.descriptionText)
                    self.descriptionText.token = equipmentDef.descriptionToken;
            }

            ExpandNotification(self);
        }

        /// <summary>
        /// Increases the height of the notification panel so the full item description text
        /// is readable without being clipped.
        /// </summary>
        private static void ExpandNotification(GenericNotification notification)
        {
            if (!notification || !notification.descriptionText) return;

            // Grow the description text area.
            RectTransform descRect = notification.descriptionText.transform as RectTransform;
            if (descRect)
            {
                descRect.sizeDelta = new Vector2(descRect.sizeDelta.x, descRect.sizeDelta.y + ExtraDescriptionHeight);
            }

            // Also grow the overall notification panel so the expanded text area fits.
            RectTransform notifRect = notification.transform as RectTransform;
            if (notifRect)
            {
                notifRect.sizeDelta = new Vector2(notifRect.sizeDelta.x, notifRect.sizeDelta.y + ExtraDescriptionHeight);
            }
        }

        private static void OnTooltipSetContent(On.RoR2.UI.TooltipProvider.orig_SetContent orig, TooltipProvider self, TooltipContent content)
        {
            if (ModConfig.ItemHints.Value)
            {
                // Item tooltips use bodyToken for the short pickup text. If there is a matching
                // descriptionToken that is more detailed, swap it in.
                // The TooltipContent struct is passed by value so we can modify it freely.
                TryExpandDescription(ref content);
            }

            orig(self, content);
        }

        private static void TryExpandDescription(ref TooltipContent content)
        {
            // The tooltip's body might be set from a token or from override text.
            // We look for items/equipment whose pickupToken matches the current body token
            // and swap in the descriptionToken.
            if (string.IsNullOrEmpty(content.bodyToken) || Language.IsTokenInvalid(content.bodyToken))
                return;

            // Search items
            for (int i = 0; i < ItemCatalog.itemCount; i++)
            {
                ItemDef def = ItemCatalog.GetItemDef((ItemIndex)i);
                if (def != null && def.pickupToken == content.bodyToken)
                {
                    if (!Language.IsTokenInvalid(def.descriptionToken))
                    {
                        content.bodyToken = def.descriptionToken;
                    }
                    return;
                }
            }

            // Search equipment
            for (int i = 0; i < EquipmentCatalog.equipmentCount; i++)
            {
                EquipmentDef def = EquipmentCatalog.GetEquipmentDef((EquipmentIndex)i);
                if (def != null && def.pickupToken == content.bodyToken)
                {
                    if (!Language.IsTokenInvalid(def.descriptionToken))
                    {
                        content.bodyToken = def.descriptionToken;
                    }
                    return;
                }
            }
        }

        private static void OnPing(On.RoR2.PingerController.orig_SetCurrentPing orig, PingerController self, PingerController.PingInfo newPingInfo)
        {
            orig(self, newPingInfo);

            if (!ModConfig.ItemHints.Value) return;

            // If the pinged object is a pickup, log its full description in the chat so
            // the player can read it. RoR2 already shows the name; we add the stats.
            if (!newPingInfo.targetNetworkIdentity) return;

            GenericPickupController pickup = newPingInfo.targetNetworkIdentity.GetComponent<GenericPickupController>();
            if (!pickup) return;

            PickupDef pickupDef = PickupCatalog.GetPickupDef(pickup.pickupIndex);
            if (pickupDef == null) return;

            string desc = null;
            if (pickupDef.itemIndex != ItemIndex.None)
            {
                ItemDef item = ItemCatalog.GetItemDef(pickupDef.itemIndex);
                if (item != null && !Language.IsTokenInvalid(item.descriptionToken))
                    desc = Language.GetString(item.descriptionToken);
            }
            else if (pickupDef.equipmentIndex != EquipmentIndex.None)
            {
                EquipmentDef equip = EquipmentCatalog.GetEquipmentDef(pickupDef.equipmentIndex);
                if (equip != null && !Language.IsTokenInvalid(equip.descriptionToken))
                    desc = Language.GetString(equip.descriptionToken);
            }

            if (!string.IsNullOrEmpty(desc))
            {
                Chat.AddMessage($"<style=cStack>{desc}</style>");
            }
        }

        // ── Command picker description ──────────────────────────────────────

        /// <summary>
        /// After the Command picker panel populates its item buttons, creates a description
        /// text area below the grid and wires each button's onSelect to show that item's
        /// full description.
        /// </summary>
        private static void OnSetPickupOptions(On.RoR2.UI.PickupPickerPanel.orig_SetPickupOptions orig, PickupPickerPanel self, PickupPickerController.Option[] options)
        {
            if (ModConfig.ItemHints.Value)
            {
                // Widen the grid from 5 to 6 columns so there is more vertical room for the
                // description text below the icons.
                self.maxColumnCount = 6;
                if (self.gridlayoutGroup)
                    self.gridlayoutGroup.constraintCount = 6;
            }

            orig(self, options);

            if (!ModConfig.ItemHints.Value) return;
            if (options == null || options.Length == 0) return;

            try
            {
                // Parent to the button container's parent — this is the visible picker
                // dialog, not the full-screen overlay (self.transform).
                Transform panelParent = self.buttonContainer.parent;

                // Create a description text element inside the picker dialog,
                // centered at the bottom below the item grid.
                GameObject textObj = new GameObject("ItemHintDescription");
                textObj.transform.SetParent(panelParent, false);

                TextMeshProUGUI tmp = textObj.AddComponent<TextMeshProUGUI>();
                tmp.fontSize = 16;
                tmp.alignment = TextAlignmentOptions.Top;
                tmp.enableWordWrapping = true;
                tmp.overflowMode = TextOverflowModes.Truncate;
                tmp.color = new Color(0.9f, 0.85f, 0.7f, 1f);
                tmp.margin = new Vector4(8, 4, 8, 4);

                // Position inside the panel at the bottom. Pivot at bottom so the box
                // grows upward; positive Y keeps it inside the parent bounds.
                RectTransform textRect = textObj.GetComponent<RectTransform>();
                textRect.anchorMin = new Vector2(0f, 0f);
                textRect.anchorMax = new Vector2(1f, 0f);
                textRect.pivot = new Vector2(0.5f, 0f);
                textRect.anchoredPosition = new Vector2(0f, 8f);
                textRect.sizeDelta = new Vector2(-16f, 120f);

                tmp.text = "";

                // Wire each button's onSelect event to update the description text.
                var buttons = self.buttonAllocator.elements;
                for (int i = 0; i < buttons.Count && i < options.Length; i++)
                {
                    int index = i; // capture for closure
                    MPButton button = buttons[index];
                    PickupIndex pickupIndex = options[index].pickup.pickupIndex;

                    button.onSelect.AddListener(() =>
                    {
                        UpdatePickerDescription(tmp, pickupIndex);
                    });
                }

                // Show the first item's description immediately if there are items.
                if (options.Length > 0)
                {
                    UpdatePickerDescription(tmp, options[0].pickup.pickupIndex);
                }
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogWarning("[VRMod] ItemHints: Failed to add picker description: " + ex.Message);
            }
        }

        /// <summary>
        /// Looks up the item/equipment name and full description for a pickup index and
        /// sets the text on the given TextMeshProUGUI.
        /// </summary>
        private static void UpdatePickerDescription(TextMeshProUGUI tmp, PickupIndex pickupIndex)
        {
            if (!tmp) return;

            PickupDef pickupDef = PickupCatalog.GetPickupDef(pickupIndex);
            if (pickupDef == null) { tmp.text = ""; return; }

            string name = null;
            string desc = null;

            if (pickupDef.itemIndex != ItemIndex.None)
            {
                ItemDef item = ItemCatalog.GetItemDef(pickupDef.itemIndex);
                if (item != null)
                {
                    name = Language.GetString(item.nameToken);
                    if (!Language.IsTokenInvalid(item.descriptionToken))
                        desc = Language.GetString(item.descriptionToken);
                }
            }
            else if (pickupDef.equipmentIndex != EquipmentIndex.None)
            {
                EquipmentDef equip = EquipmentCatalog.GetEquipmentDef(pickupDef.equipmentIndex);
                if (equip != null)
                {
                    name = Language.GetString(equip.nameToken);
                    if (!Language.IsTokenInvalid(equip.descriptionToken))
                        desc = Language.GetString(equip.descriptionToken);
                }
            }

            if (!string.IsNullOrEmpty(name) && !string.IsNullOrEmpty(desc))
                tmp.text = $"<b>{name}</b>\n{desc}";
            else if (!string.IsNullOrEmpty(name))
                tmp.text = $"<b>{name}</b>";
            else
                tmp.text = "";
        }
    }
}
