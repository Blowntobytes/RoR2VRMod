using RoR2;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.XR;

namespace VRMod
{
    /// <summary>
    /// EXPERIMENTAL (2.20.0): builds a hand model at runtime for characters that have no authored
    /// hand prefab in the asset bundle (e.g. the Seekers of the Storm DLC survivors).
    ///
    /// Instead of shipping extracted meshes, this reads the character's own SkinnedMeshRenderers,
    /// finds the right-hand bone chain by name, and bakes the triangles that are skinned to those
    /// bones into a rigid static mesh which is attached to the tracked controller. The left
    /// controller mirrors it exactly like the authored prefabs (the left HandController root has a
    /// negative X scale). The mesh keeps the character's real materials.
    ///
    /// Alignment: the hand bone's finger direction is estimated from its child bones and rotated
    /// onto the controller's forward axis. Per-character offsets can then be tuned LIVE from the
    /// config file (section "Character - <Body>", 2.23.35+): while RuntimeHandsDebug is on, the
    /// config file is re-read every few seconds, so offsets can be edited with the headset on and
    /// the hand moves as the file is saved. RGB axis lines are drawn at the hand origin
    /// (red=right, green=up, blue=forward/aim) and every local entity state is logged on enter so
    /// the exact states each DLC skill uses can be collected for proper per-skill support later.
    ///
    /// Characters with authored hand prefabs are completely unaffected: this code only runs on the
    /// fallback path of HandController.SetCurrentHand, which previously just warned and kept the
    /// default pointer.
    /// </summary>
    internal static class RuntimeHands
    {
        private static bool stateHookApplied;

        /// <summary>
        /// 2.23.36: rotation offsets are applied X first, then Y, then Z (each about the axes of the
        /// debug gizmo: X = red, Y = green, Z = blue). X always turns about the red line exactly;
        /// Y then turns about the green line as tilted by X; Z spins about the hand's own blue line.
        /// </summary>
        internal static Quaternion Compose(float x, float y, float z) =>
            Quaternion.AngleAxis(x, Vector3.right) * Quaternion.AngleAxis(y, Vector3.up) * Quaternion.AngleAxis(z, Vector3.forward);

        /// <summary>Converts an old Unity-Euler (Z, X, Y) triple to the equivalent Compose() angles.</summary>
        internal static Vector3 LegacyToXYZ(Vector3 e)
        {
            Matrix4x4 m = Matrix4x4.Rotate(Quaternion.Euler(e));
            float b = Mathf.Asin(Mathf.Clamp(m.m02, -1f, 1f)) * Mathf.Rad2Deg;
            float a, c;
            if (Mathf.Abs(m.m02) < 0.9999f)
            {
                a = Mathf.Atan2(-m.m12, m.m22) * Mathf.Rad2Deg;
                c = Mathf.Atan2(-m.m01, m.m00) * Mathf.Rad2Deg;
            }
            else
            {
                a = Mathf.Atan2(m.m21, m.m11) * Mathf.Rad2Deg;
                c = 0f;
            }
            return new Vector3(Mathf.Round(a * 10f) / 10f, Mathf.Round(b * 10f) / 10f, Mathf.Round(c * 10f) / 10f);
        }

        // ---- 2.23.38: "set current pose as zero" ------------------------------------------------
        // Every rotation offset is Base * Compose(x, y, z). Base is a stored quaternion
        // ("<stem>Base" in the character's section); SetCurrentPoseAsZero folds the current angles
        // into it and resets them to 0, so tuning always starts from 0/0/0, far from gimbal lock.

        internal sealed class RotEntry
        {
            internal BepInEx.Configuration.ConfigEntry<float> x, y, z;
            internal BepInEx.Configuration.ConfigEntry<string> b;
            private string parsedFrom; private Quaternion parsed = Quaternion.identity;
            internal Quaternion Base
            {
                get
                {
                    if (b == null) return Quaternion.identity;
                    if (b.Value != parsedFrom) { parsedFrom = b.Value; parsed = ParseQ(b.Value); }
                    return parsed;
                }
            }
            internal Quaternion Value => Base * Compose(x.Value, y.Value, z.Value);
            internal void Fold()
            {
                b.Value = FormatQ(Value);
                x.Value = 0f; y.Value = 0f; z.Value = 0f;
            }
        }

        private static readonly Dictionary<string, RotEntry> rotEntries = new Dictionary<string, RotEntry>();

        internal static string FormatQ(Quaternion q)
        {
            System.Globalization.CultureInfo c = System.Globalization.CultureInfo.InvariantCulture;
            return $"{q.x.ToString("0.######", c)}, {q.y.ToString("0.######", c)}, {q.z.ToString("0.######", c)}, {q.w.ToString("0.######", c)}";
        }

        internal static Quaternion ParseQ(string s)
        {
            try
            {
                string[] p = s.Split(',');
                if (p.Length != 4) return Quaternion.identity;
                System.Globalization.CultureInfo c = System.Globalization.CultureInfo.InvariantCulture;
                Quaternion q = new Quaternion(float.Parse(p[0].Trim(), c), float.Parse(p[1].Trim(), c), float.Parse(p[2].Trim(), c), float.Parse(p[3].Trim(), c));
                float n = Mathf.Sqrt(q.x * q.x + q.y * q.y + q.z * q.z + q.w * q.w);
                if (n < 1e-4f) return Quaternion.identity;
                return new Quaternion(q.x / n, q.y / n, q.z / n, q.w / n);
            }
            catch { return Quaternion.identity; }
        }

        /// <summary>Registers (binds) the zero-pose Base entry for a rotation triple.</summary>
        internal static RotEntry Rot(string bodyName, string stem,
            BepInEx.Configuration.ConfigEntry<float> x, BepInEx.Configuration.ConfigEntry<float> y, BepInEx.Configuration.ConfigEntry<float> z,
            Quaternion defaultBase)
        {
            string key = bodyName + "/" + stem;
            if (rotEntries.TryGetValue(key, out RotEntry e)) return e;
            e = new RotEntry
            {
                x = x, y = y, z = z,
                b = ModConfig.BindRuntimeHand<string>(bodyName, stem + "Base", FormatQ(defaultBase),
                    $"Internal: the pose that '{bodyName}_{stem}X/Y/Z' = 0/0/0 stands for (a quaternion x, y, z, w). Written by '{bodyName}_SetCurrentPoseAsZero'; delete this line to go back to the built-in pose.")
            };
            rotEntries[key] = e;
            return e;
        }

        private static readonly Dictionary<string, BepInEx.Configuration.ConfigEntry<bool>> zeroPoseEntries = new Dictionary<string, BepInEx.Configuration.ConfigEntry<bool>>();

        internal static BepInEx.Configuration.ConfigEntry<bool> ZeroPoseEntry(string bodyName)
        {
            string body = bodyName.IndexOf('_') > 0 ? bodyName.Substring(0, bodyName.IndexOf('_')) : bodyName;
            if (!zeroPoseEntries.TryGetValue(bodyName, out var e))
            {
                e = ModConfig.BindRuntimeHand<bool>(bodyName, "SetCurrentPoseAsZero", false,
                    "Set to true and save: every hand, aim and weapon rotation of this character (this form) keeps its current pose but its RotX/RotY/RotZ become 0, so you tune from zero again. Switches itself back to false.");
                zeroPoseEntries[bodyName] = e;
            }
            return e;
        }

        /// <summary>Folds every bound rotation of this body into its Base when the switch is on.</summary>
        internal static void ProcessZeroPose(string bodyName)
        {
            var sw = ZeroPoseEntry(bodyName);
            if (!sw.Value) return;
            int n = 0;
            string prefix = bodyName + "/";
            ModConfig.Batched(() =>
            {
                foreach (var kv in rotEntries)
                    if (kv.Key.StartsWith(prefix, StringComparison.Ordinal)) { kv.Value.Fold(); n++; }
                sw.Value = false;
            });
            ModConfig.FlushIfDirty();
            VRMod.StaticLogger.LogInfo($"[VR hands] '{bodyName}': current pose set as zero for {n} rotation set(s) - their RotX/Y/Z are now 0.");
        }

        /// <summary>The built-in (baked default) hand rotation, used as the held weapon's anchor.</summary>
        internal static Quaternion BakedHandRotation(string bodyName, bool leftHand)
        {
            Vector3[] defs = DefaultsFor(bodyName);
            Vector3 v = LegacyToXYZ(leftHand ? defs[0] : defs[1]);
            return Compose(v.x, v.y, v.z);
        }

        internal static Quaternion BakedAimRotation(string bodyName)
        {
            Vector3 v = LegacyToXYZ(DefaultsFor(bodyName)[2]);
            return Compose(v.x, v.y, v.z);
        }

        internal static Quaternion BakedWeaponRotation(string bodyName)
        {
            if (IsPerSideWeaponBody(bodyName) || !weaponDefaults.TryGetValue(bodyName, out float[] w)) return Quaternion.identity;
            Vector3 v = LegacyToXYZ(new Vector3(w[3], w[4], w[5]));
            return Compose(v.x, v.y, v.z);
        }

        internal const string AxisNote = " Axis colours on the debug gizmo (Debug mode): X = RED, Y = GREEN, Z = BLUE (blue points along the fingers / aim). Order: X first (always about the red line), then Y (green), then Z (blue, spins about the part's own length).";

        /// <summary>Starting angles per character. Compile-time only - no shared config entries.</summary>
        private static readonly Dictionary<string, Vector3[]> bodyDefaults = new Dictionary<string, Vector3[]>
        {
            // bodyName -> { left hand rot, right hand rot, shared aim rot }
            // Values tuned in-headset by Blowntobytes (hands 1.0.0, aim 1.0.4).
            { "SeekerBody",    new[] { new Vector3(120f, 195f, 150f), new Vector3(45f, 10f, 120f),   new Vector3(65f, -20f, 0f) } },
            { "ChefBody",      new[] { new Vector3(280f, 350f, 15f),  new Vector3(120f, 110f, 160f), new Vector3(80f, -60f, 0f) } },
            { "FalseSonBody",  new[] { new Vector3(110f, 200f, 140f), new Vector3(60f, -25f, 90f),   new Vector3(60f, -15f, 0f) } },
            // Alloyed Collective (DLC3): the Operator is "DroneTechBody" internally.
            { "DroneTechBody", new[] { new Vector3(220f, 10f, 210f),  new Vector3(60f, -10f, 100f),  new Vector3(65f, -20f, 0f) } },
            { "DrifterBody",   new[] { new Vector3(-100f, 20f, 0f),   new Vector3(60f, -40f, -30f),  new Vector3(65f, -20f, 0f) } },
        };

        /// <summary>The DLC2, DLC3 and DLC4 survivors, pre-registered so all their entries exist in the
        /// config file at startup instead of appearing only after each one is first played.
        /// (DroneTechBody = Operator; Enforcer2Body = Enforcer and AcolyteBody = Acolyte, both from
        /// Hallowed Concepts / DLC4 - untuned, they start from the generic defaults.)</summary>
        internal static readonly string[] KnownRuntimeHandBodies = { "SeekerBody", "ChefBody", "FalseSonBody", "DroneTechBody", "DrifterBody", "Enforcer2Body", "AcolyteBody" };

        private static readonly Dictionary<string, string> baseModelNames = new Dictionary<string, string>();

        /// <summary>Body name for the body's normal model; "Body_Form" (e.g. "AcolyteBody_Reaper")
        /// once the same body is showing a different model.</summary>
        // 2.23.29: Acolyte's Reaper form does NOT swap the model - it toggles two child objects
        // (acolyteModel / reaperModel) under the same model. The hidden one is ignored while baking.
        private static Transform hiddenFormRoot;

        internal static Transform HiddenFormRoot(CharacterBody body)
        {
            AcolyteBodySwapController swap = body ? body.GetComponent<AcolyteBodySwapController>() : null;
            if (!swap) return null;
            GameObject hidden = swap.isReaperForm ? swap.acolyteModel : swap.reaperModel;
            return hidden ? hidden.transform : null;
        }

        private static bool InHiddenForm(Transform t)
        {
            return hiddenFormRoot && t && t.IsChildOf(hiddenFormRoot);
        }

        internal static string FormConfigName(string bodyName, Transform model, CharacterBody body = null)
        {
            if (!model) return bodyName;
            AcolyteBodySwapController swapForm = body ? body.GetComponent<AcolyteBodySwapController>() : null;
            if (swapForm && swapForm.isReaperForm)
            {
                VRMod.StaticLogger.LogInfo($"[VR hands] '{bodyName}' is in its Reaper form - using form config '{bodyName}_Reaper'.");
                return bodyName + "_Reaper";
            }
            string modelName = model.name.Replace("(Clone)", "").Trim();
            string baseName;
            if (!baseModelNames.TryGetValue(bodyName, out baseName))
            {
                // The body PREFAB's model is the normal form, whatever is showing right now.
                try
                {
                    GameObject prefab = body ? BodyCatalog.GetBodyPrefab(body.bodyIndex) : null;
                    ModelLocator ml = prefab ? prefab.GetComponent<ModelLocator>() : null;
                    if (ml && ml.modelTransform) baseName = ml.modelTransform.name.Replace("(Clone)", "").Trim();
                }
                catch { }
                if (string.IsNullOrEmpty(baseName)) baseName = modelName;
                baseModelNames[bodyName] = baseName;
            }
            if (modelName == baseName) return bodyName;

            string form = modelName.StartsWith("mdl", StringComparison.OrdinalIgnoreCase) ? modelName.Substring(3) : modelName;
            form = new string(form.Where(char.IsLetterOrDigit).ToArray());
            if (form.Length == 0) form = "Form";
            string name = bodyName + "_" + form;
            VRMod.StaticLogger.LogInfo($"[VR hands] '{bodyName}' is showing model '{modelName}' (normal: '{baseName}') - using form config '{name}'.");
            return name;
        }

        private static Vector3[] DefaultsFor(string bodyName)
        {
            Vector3[] d;
            if (bodyDefaults.TryGetValue(bodyName, out d)) return d;
            return new[] { new Vector3(45f, 10f, 120f), new Vector3(45f, 10f, 120f), new Vector3(60f, -15f, 0f) };
        }

        /// <summary>
        /// Binds one hand's config entries. LEFT and RIGHT have completely independent mesh
        /// entries (nothing is mirrored between them); the aim/laser entries are shared by both
        /// hands of a character, as is the mesh scale.
        /// </summary>
        internal static void BindHandEntries(string bodyName, bool leftHand,
            out BepInEx.Configuration.ConfigEntry<float> rotX,
            out BepInEx.Configuration.ConfigEntry<float> rotY,
            out BepInEx.Configuration.ConfigEntry<float> rotZ,
            out BepInEx.Configuration.ConfigEntry<float> posX,
            out BepInEx.Configuration.ConfigEntry<float> posY,
            out BepInEx.Configuration.ConfigEntry<float> posZ,
            out BepInEx.Configuration.ConfigEntry<float> scale,
            out BepInEx.Configuration.ConfigEntry<float> aimRotX,
            out BepInEx.Configuration.ConfigEntry<float> aimRotY,
            out BepInEx.Configuration.ConfigEntry<float> aimRotZ)
        {
            Vector3[] defs = DefaultsFor(bodyName);
            // Baked defaults were tuned in the old Euler order - convert them to Compose() angles.
            // 2.23.38: angles start at 0; the built-in pose lives in the "...RotBase" entries.
            Vector3 rotDef = Vector3.zero;
            Vector3 aimDef = Vector3.zero;
            string side = leftHand ? "Left" : "Right";
            string sideWord = leftHand ? "LEFT" : "RIGHT";
            string note = $" Applies to the {sideWord} hand only - the two hands are tuned independently and neither is mirrored from the other.";

            rotX = ModConfig.BindRuntimeHand(bodyName, side + "RotX", rotDef.x, $"{sideWord} HAND MESH rotation offset (degrees) around X = the RED line.{note} Does not affect the aim/laser or the held weapon.{AxisNote}");
            rotY = ModConfig.BindRuntimeHand(bodyName, side + "RotY", rotDef.y, $"{sideWord} HAND MESH rotation offset (degrees) around Y = the GREEN line.{note} Does not affect the aim/laser or the held weapon.{AxisNote}");
            rotZ = ModConfig.BindRuntimeHand(bodyName, side + "RotZ", rotDef.z, $"{sideWord} HAND MESH rotation offset (degrees) around Z = the BLUE line.{note} Does not affect the aim/laser or the held weapon.{AxisNote}");
            posX = ModConfig.BindRuntimeHand(bodyName, side + "PosX", 0f, $"{sideWord} HAND MESH position offset (meters) along X = the RED line.{note}");
            posY = ModConfig.BindRuntimeHand(bodyName, side + "PosY", 0f, $"{sideWord} HAND MESH position offset (meters) along Y = the GREEN line.{note}");
            posZ = ModConfig.BindRuntimeHand(bodyName, side + "PosZ", 0f, $"{sideWord} HAND MESH position offset (meters) along Z = the BLUE line.{note}");

            scale = ModConfig.BindRuntimeHand(bodyName, "Scale", 1f, "Scale multiplier for this character's runtime hand meshes (both hands).");
            aimRotX = ModConfig.BindRuntimeHand(bodyName, "AimRotX", aimDef.x, "AIM/LASER rotation offset (degrees) around X = the RED line. Shared by BOTH hands; does not affect the hand meshes." + AxisNote);
            aimRotY = ModConfig.BindRuntimeHand(bodyName, "AimRotY", aimDef.y, "AIM/LASER rotation offset (degrees) around Y = the GREEN line. Shared by BOTH hands; does not affect the hand meshes." + AxisNote);
            aimRotZ = ModConfig.BindRuntimeHand(bodyName, "AimRotZ", aimDef.z, "AIM/LASER rotation offset (degrees) around Z = the BLUE line. Shared by BOTH hands; does not affect the hand meshes." + AxisNote);
            Rot(bodyName, side + "Rot", rotX, rotY, rotZ, Compose(LegacyToXYZ(leftHand ? defs[0] : defs[1]).x, LegacyToXYZ(leftHand ? defs[0] : defs[1]).y, LegacyToXYZ(leftHand ? defs[0] : defs[1]).z));
            Rot(bodyName, "AimRot", aimRotX, aimRotY, aimRotZ, BakedAimRotation(bodyName));
            ZeroPoseEntry(bodyName);
        }

        /// <summary>Creates every entry for the known DLC2 survivors up front.</summary>
        internal static void PreRegisterKnownBodies()
        {
            BepInEx.Configuration.ConfigEntry<float> a, b, c, d, e, f, g, h, i, j;

            foreach (string bodyName in KnownRuntimeHandBodies)
            {
                BindHandEntries(bodyName, true, out a, out b, out c, out d, out e, out f, out g, out h, out i, out j);
                BindHandEntries(bodyName, false, out a, out b, out c, out d, out e, out f, out g, out h, out i, out j);
            }
            // The Operator's gun hangs off a weapon bone under the left hand.
            BindWeaponEntries("DroneTechBody");
            BindWeaponEntries("FalseSonBody");
            // Hallowed Concepts (DLC4): Enforcer's riot shotgun / shield and Acolyte's blades are
            // held props, so give both the weapon offset entries for in-headset mapping.
            BindWeaponEntries("Enforcer2Body", true);
            BindWeaponEntries("Enforcer2Body", false);
            BindWeaponEntries("AcolyteBody", true);
            BindWeaponEntries("AcolyteBody", false);
            foreach (string bodyName in KnownRuntimeHandBodies) BodyIncludesForearm(bodyName);
        }

        private static readonly Dictionary<string, BepInEx.Configuration.ConfigEntry<bool>> forearmEntries = new Dictionary<string, BepInEx.Configuration.ConfigEntry<bool>>();

        /// <summary>Whether this character's forearm is baked into the runtime hand. One setting
        /// per character (the old global "Include forearm" switch is gone - it conflicted with
        /// these). False Son's forearms are huge in first person, so his default is off.</summary>
        internal static bool BodyIncludesForearm(string bodyName)
        {
            BepInEx.Configuration.ConfigEntry<bool> e;
            if (!forearmEntries.TryGetValue(bodyName, out e))
            {
                bool def = bodyName != "FalseSonBody";
                e = ModConfig.BindRuntimeHand<bool>(bodyName, "Forearm", def, "Bake this character's forearm into the runtime hand mesh (rigid - it will not bend at the elbow). Off = hand only.");
                forearmEntries[bodyName] = e;
            }
            return e.Value;
        }

        internal static void Init()
        {
            if (stateHookApplied) return;
            stateHookApplied = true;

            // Debug: log every entity state the LOCAL body enters, so we can map which states each
            // DLC skill uses. No-op unless the debug flag is on.
            On.EntityStates.EntityState.OnEnter += (orig, self) =>
            {
                orig(self);

                if (!ModConfig.RuntimeHandsDebug.Value) return;

                try
                {
                    if (MotionControls.currentBody && self.outer && self.outer.commonComponents.characterBody == MotionControls.currentBody)
                    {
                        VRMod.StaticLogger.LogInfo($"[VR debug] state enter: {self.GetType().FullName} (machine: {self.outer.customName})");
                    }
                }
                catch { }
            };
        }

        /// <summary>
        /// Builds a runtime hand for the given body, or returns null when it can't (no model, no
        /// recognizable hand bone, no skinned triangles). The returned object is INACTIVE so the
        /// Hand component's Awake (which requires muzzles) runs only after the caller activates it.
        /// </summary>
        internal static Hand TryBuildHand(HandController controller, string bodyName)
        {
            try
            {
                CharacterBody body = MotionControls.currentBody;
                if (!body || !body.modelLocator || !body.modelLocator.modelTransform)
                {
                    VRMod.StaticLogger.LogWarning("[VR hands] Runtime hand: no model transform available.");
                    return null;
                }

                Transform model = body.modelLocator.modelTransform;

                // 2.23.27: in-place transformations (Acolyte's Reaper form) keep the same body but
                // swap the visible model. Each form gets its own config section,
                // "<Body>_<Form>", so the transformed hands are tuned independently and the base
                // hands come back unchanged afterwards. Temporary bodies (Shrine of Compulsion's
                // PolymorphLemurianBody) are separate bodies and simply use their own name.
                bodyName = FormConfigName(bodyName, model, body);
                hiddenFormRoot = HiddenFormRoot(body);

                // Each controller builds from its OWN arm - the left hand is never mirrored from
                // the right. That is what makes characters holding a different weapon in each hand
                // correct (Chef: cleaver on Wrist_L, pizza cutter on Wrist_R), and each hand is
                // then tuned with its own independent config entries.
                bool controllerIsLeft = controller.xrNode == XRNode.LeftHand;
                bool useRightArm = !controllerIsLeft;

                // The left HandController root carries a negative X scale, which exists to mirror
                // the authored right-arm hand models. An arm baked from the left side is already
                // correct, so that mirror is cancelled on the mesh.
                bool cancelParentMirror = controllerIsLeft;

                // Only the character's OWN skeleton is a valid source for the hand bone. Item
                // displays parented into the model bring their own rigs along (a glove item has a
                // bone literally called "hand.l"), and picking one of those gives a hand made of
                // an accessory instead of the character's arm.
                HashSet<Transform> skeletonBones = CollectSkeletonBones(model);

                Transform handBone = FindHandBone(model, useRightArm, skeletonBones);
                if (!handBone)
                {
                    // Some rigs only name one side recognizably - fall back to the other arm
                    // (mirrored) rather than losing the hand entirely.
                    handBone = FindHandBone(model, !useRightArm, skeletonBones);
                    if (handBone)
                    {
                        cancelParentMirror = !cancelParentMirror;
                        VRMod.StaticLogger.LogWarning($"[VR hands] No {(useRightArm ? "right" : "left")}-side hand bone on '{bodyName}'; mirroring the other arm for this hand.");
                    }
                }

                if (!handBone)
                {
                    VRMod.StaticLogger.LogWarning($"[VR hands] Runtime hand: no {(useRightArm ? "right" : "left")}-hand bone found on '{bodyName}'. Keeping default pointer.");
                    if (ModConfig.RuntimeHandsDebug.Value) DumpBones(model);
                    return null;
                }

                HashSet<Transform> boneSet = new HashSet<Transform>(handBone.GetComponentsInChildren<Transform>(true));

                // 2.23.26 sanity check: a hand never owns most of the skeleton. When the "hand"
                // bone's subtree covers more than half the rig (IK bones parented high up), the
                // bake would turn the whole character into a giant hand - refuse instead.
                int skeletonCount = skeletonBones != null ? skeletonBones.Count : 0;
                int setInSkeleton = skeletonBones != null ? boneSet.Count(b => skeletonBones.Contains(b)) : boneSet.Count;
                if (skeletonCount > 0 && setInSkeleton > skeletonCount / 2)
                {
                    VRMod.StaticLogger.LogWarning($"[VR hands] Runtime hand: '{handBone.name}' on '{bodyName}' covers {setInSkeleton} of {skeletonCount} skeleton bones - that is the whole character, not a hand. Keeping default pointer.");
                    return null;
                }

                if (BodyIncludesForearm(bodyName) && handBone.parent)
                {
                    string p = handBone.parent.name.ToLowerInvariant();
                    if (p.Contains("arm") || p.Contains("elbow") || p.Contains("wrist"))
                        boneSet.Add(handBone.parent);
                }

                List<CharacterModel.RendererInfo> rendererInfos = new List<CharacterModel.RendererInfo>();
                int characterParts;
                GameObject container = BakeHandMesh(model, handBone, boneSet, bodyName, rendererInfos, out characterParts);
                if (!container)
                {
                    VRMod.StaticLogger.LogWarning($"[VR hands] Runtime hand: no triangles skinned to '{handBone.name}' on '{bodyName}'. Keeping default pointer.");
                    return null;
                }

                // Loading a save attaches the carried items long before the character's own skin
                // finishes applying, so a bake at that moment captures ONLY item displays - rings,
                // magazines - and no arm at all. Treat that as "not ready" rather than success, so
                // the retry keeps waiting for the real meshes instead of leaving a hand made of
                // jewellery.
                // Same story on a stage change for rigs that carry loose props under the hand
                // (Drifter's junk): a rigid prop is not proof the character is ready - if any
                // skinned renderer still has no mesh and we captured no skinned body part, wait.
                if (characterParts == 0 || (skinPending && skinnedCharacterParts == 0))
                {
                    UnityEngine.Object.Destroy(container);
                    VRMod.StaticLogger.LogInfo($"[VR hands] The character's own meshes are still null for '{bodyName}' (only item displays / loose props are loaded so far) - waiting for the skin before baking the hand.");
                    return null;
                }

                // Operator: the drone ball baked into the claw follows the ThrowDroneBall charge.
                WireStockDrivenParts(container, body);

                // Estimate the finger direction (hand-bone local space) to auto-aim the mesh forward.
                Vector3 fingerDir = EstimateFingerDirectionAtBind(model, handBone);
                Quaternion autoAlign = Quaternion.FromToRotation(fingerDir, Vector3.forward);

                // Root object that HandController owns; child holds the mesh + muzzle so live offset
                // tuning never fights the PoseCorrection HandController applies to the root.
                GameObject root = new GameObject($"RuntimeHand_{bodyName}");
                root.SetActive(false);

                // MESH root: carries the baked hand, tuned with Rot/Pos/Scale offsets.
                GameObject aligned = new GameObject("AlignedRoot");
                aligned.transform.SetParent(root.transform, false);
                container.transform.SetParent(aligned.transform, false);

                // AIM root: carries the muzzle, tuned INDEPENDENTLY with AimRot offsets, so the
                // laser/aim can be adjusted without dragging the hand mesh along (and vice versa).
                GameObject aimRoot = new GameObject("AimRoot");
                aimRoot.transform.SetParent(root.transform, false);

                GameObject muzzleGO = new GameObject("RuntimeMuzzle");
                muzzleGO.transform.SetParent(aimRoot.transform, false);
                muzzleGO.transform.localPosition = new Vector3(0f, 0f, 0.05f);
                Muzzle muzzle = muzzleGO.AddComponent<Muzzle>();
                muzzle.entriesToReplaceIfDominant = new string[0];
                muzzle.entriesToReplaceIfNonDominant = new string[0];

                RuntimeHandAligner alignerComp = aligned.AddComponent<RuntimeHandAligner>();
                alignerComp.autoAlign = autoAlign;
                alignerComp.bodyName = bodyName;
                alignerComp.aimRoot = aimRoot.transform;
                alignerComp.isLeftHand = controllerIsLeft;
                // Cancel the controller's mirror on the MESH only, so a natively-left arm renders
                // the right way round. The aim root stays in the mirrored space so the shared aim
                // values behave the same on both hands.
                alignerComp.mirrorMesh = cancelParentMirror;
                // Remember which model instance this mesh was baked from. A stage change builds a
                // brand new character model, and a hand baked from the old (destroyed) one has no
                // visible mesh left - the watchdog below rebuilds when it sees the model change.
                alignerComp.builtModelId = model.GetInstanceID();
                alignerComp.sourceHandBone = handBone;
                alignerComp.itemDisplaysAtBuild = CountItemDisplays(handBone);
                alignerComp.inventorySumAtBuild = InventorySum(body);
                alignerComp.BindConfig();



                if (ModConfig.RuntimeHandsDebug.Value)
                {
                    // 2.23.36: the gizmo is drawn in the frame the rotation offsets act in (it does NOT
                    // turn with the hand), so RotX visibly turns the hand about the red line, etc.
                    // A second gizmo sits on the held weapon's own space when there is one.
                    try
                    {
                        GameObject gizmo = new GameObject("AxisGizmo");
                        gizmo.transform.SetParent(root.transform, false);
                        AddAxisLines(gizmo.transform, controller);
                        alignerComp.axisGizmo = gizmo.transform;
                        if (alignerComp.WeaponSpace)
                        {
                            GameObject wg = new GameObject("WeaponGizmo");
                            wg.transform.SetParent(alignerComp.WeaponSpace, false);
                            AddAxisLines(wg.transform, controller);
                            alignerComp.weaponGizmo = wg.transform;
                        }
                    }
                    catch (Exception axisEx) { VRMod.StaticLogger.LogWarning($"[VR debug] Axis lines failed (hand mesh unaffected): {axisEx.Message}"); }
                }

                Hand hand = root.AddComponent<Hand>();
                hand.handType = HandType.Both;
                hand.bodyName = bodyName;
                hand.useRay = ModConfig.RuntimeHandsRay.Value;
                hand.muzzles = new Muzzle[] { muzzle };
                hand.rendererInfos = rendererInfos.ToArray();
                hand.copyMaterialsFromCharacterModelIfNoSkin = true;

                root.transform.SetParent(controller.transform, false);

                if (ModConfig.HandTuningEnabled || ModConfig.RuntimeHandsDebug.Value) VRMod.StaticLogger.LogInfo($"[VR hands] Runtime hand built for '{bodyName}' [{(controllerIsLeft ? "LEFT" : "RIGHT")} controller] from bone '{handBone.name}' ({boneSet.Count} bones, {rendererInfos.Count} renderer info(s) wired to the game's material pipeline){(cancelParentMirror ? ", un-mirrored" : "")}. Tune offsets in config section '{ModConfig.CharacterSection(bodyName)}'.");

                return hand;
            }
            catch (Exception e)
            {
                VRMod.StaticLogger.LogError($"[VR hands] Runtime hand build failed for '{bodyName}': {e}");
                return null;
            }
        }

        /// <summary>
        /// The bones actually driving the character's own skinned meshes. Item display models are
        /// excluded, so their rigs can never be mistaken for the character's arm.
        /// </summary>
        private static HashSet<Transform> CollectSkeletonBones(Transform model)
        {
            HashSet<Transform> bones = new HashSet<Transform>();

            foreach (SkinnedMeshRenderer smr in model.GetComponentsInChildren<SkinnedMeshRenderer>(true))
            {
                if (smr.GetComponentInParent<ItemDisplay>()) continue;
                if (InHiddenForm(smr.transform)) continue;
                if (smr.bones == null) continue;

                foreach (Transform b in smr.bones)
                {
                    if (b) bones.Add(b);
                }
            }

            return bones;
        }

        private static Transform FindHandBone(Transform model, bool right, HashSet<Transform> skeletonBones)
        {
            Transform[] all = model.GetComponentsInChildren<Transform>(true);

            // Keep only real skeleton bones when we managed to identify them.
            if (skeletonBones != null && skeletonBones.Count > 0)
                all = all.Where(t => skeletonBones.Contains(t)).ToArray();

            string[] exact = right
                ? new string[] { "handr", "hand.r", "hand_r", "righthand", "right_hand", "right hand", "r_hand", "r hand", "hand right" }
                : new string[] { "handl", "hand.l", "hand_l", "lefthand", "left_hand", "left hand", "l_hand", "l hand", "hand left" };

            foreach (Transform t in all)
            {
                string lower = t.name.ToLowerInvariant().Trim();
                if (exact.Contains(lower)) return t;
            }

            // Fuzzy pass: name contains "hand" (but not "handle") and ends with the side letter,
            // optionally separated (Hand.R / hand_r / HandR / mdlHandR).
            // Side marker: trailing (HandR, hand_r, Hand.R) or embedded (arm_robot_R_wrist).
            System.Text.RegularExpressions.Regex sideRegex = new System.Text.RegularExpressions.Regex(
                right ? @"((\.|_|\s)?r$|(^|[._\s])r[._\s])" : @"((\.|_|\s)?l$|(^|[._\s])l[._\s])");

            // 2.23.27: prefer a real hand bone over an IK helper (Lemurian: "hand_ik.r" sits high in
            // the chain and its subtree is most of the skeleton) - IK names only as a fallback.
            System.Text.RegularExpressions.Regex ikRegex = new System.Text.RegularExpressions.Regex(@"(^|[._\s])ik([._\s]|$)|_ik|ik_");
            // 2.23.29: IK helpers are only tried at the very END (after wrist and finger searches):
            // Lemurian has no hand bone at all - its hand is lower_arm.r with finger children -
            // and "hand_ik.r" carries no mesh, which baked the whole body.
            foreach (Transform t in all)
            {
                string lower = t.name.ToLowerInvariant().Trim();
                if (!lower.Contains("hand") || lower.Contains("handle")) continue;
                if (ikRegex.IsMatch(lower)) continue;
                if (lower.Contains(right ? "right" : "left")) return t;
                if (sideRegex.IsMatch(lower)) return t;
            }

            // 2.20.9: not every rig names the bone "hand". Chef's, for instance, goes
            // ... Elbow_R -> ElbowPart2_R -> Wrist_R -> MiddleFinger1_R / ThumbFinger1_R.
            // Try "wrist" on the correct side next.
            // 2.23.37: a rig can have several wrists per side (Acolyte's Reaper form: a small lower
            // arm 'arm_lower_L_wrist_jnt' with one finger, and the real hand 'arm_av_L_wrist_jnt'
            // with four). Take the first wrist as before, unless it is a stump (< 2 finger children)
            // and another wrist on that side has more fingers.
            Transform firstWrist = null, bestWrist = null;
            int firstWristFingers = 0, bestWristFingers = -1;
            foreach (Transform t in all)
            {
                string lower = t.name.ToLowerInvariant().Trim();
                if (!lower.Contains("wrist")) continue;
                if (!lower.Contains(right ? "right" : "left") && !sideRegex.IsMatch(lower)) continue;
                int f = 0;
                foreach (Transform child in t) if (IsFingerName(child.name)) f++;
                if (!firstWrist) { firstWrist = t; firstWristFingers = f; }
                if (f > bestWristFingers) { bestWrist = t; bestWristFingers = f; }
            }
            if (firstWrist)
            {
                // 2.23.39: take the side's wrist with the MOST fingers (Reaper right: arm_lower_R
                // has only thumb + pointer; the full hand has four).
                if (bestWrist != firstWrist && bestWristFingers > firstWristFingers)
                    return LogPickedBone(bestWrist, $"wrist name, {bestWristFingers} finger(s) (skipped '{firstWrist.name}' with {firstWristFingers})");
                return LogPickedBone(firstWrist, "wrist name");
            }

            // Last resort, and the most rig-agnostic: the hand is whatever bone has the most
            // finger-like children on the correct side. This works regardless of naming style.
            Transform bestBone = null;
            int bestFingers = 0;

            foreach (Transform t in all)
            {
                string lower = t.name.ToLowerInvariant().Trim();

                bool onSide = lower.Contains(right ? "right" : "left") || sideRegex.IsMatch(lower);
                if (!onSide) continue;

                int fingers = 0;
                foreach (Transform child in t)
                {
                    if (IsFingerName(child.name)) fingers++;
                }

                if (fingers > bestFingers)
                {
                    bestFingers = fingers;
                    bestBone = t;
                }
            }

            if (bestBone) return LogPickedBone(bestBone, $"{bestFingers} finger child bone(s)");

            foreach (Transform t in all)
            {
                string lower = t.name.ToLowerInvariant().Trim();
                if (!lower.Contains("hand") || lower.Contains("handle")) continue;
                if (lower.Contains(right ? "right" : "left") || sideRegex.IsMatch(lower)) return LogPickedBone(t, "IK helper name (last resort)");
            }

            return null;
        }

        private static bool IsFingerName(string name)
        {
            string n = name.ToLowerInvariant();
            return n.Contains("finger") || n.Contains("thumb") || n.Contains("index") ||
                   n.Contains("middle") || n.Contains("pinky") || n.Contains("ring") ||
                   n.Contains("digit");
        }

        private static Transform LogPickedBone(Transform bone, string how)
        {
            VRMod.StaticLogger.LogInfo($"[VR hands] Hand bone identified as '{bone.name}' by {how} (this rig does not use a 'hand' bone name).");
            return bone;
        }

        private struct BoneSnapshot
        {
            public Transform bone;
            public Vector3 localPosition;
            public Quaternion localRotation;
        }

        /// <summary>
        /// Puts every bone of the renderer's hand subtree into the mesh's BIND (rest) pose and
        /// returns a snapshot to restore afterwards. The hand is re-baked whenever the inventory
        /// changes, and baking from the live animation frame meant every pickup could capture a
        /// different finger curl / wrist angle - the hand visibly "rotated inwards" over a run.
        /// Baking from the bind pose makes every bake identical.
        /// The bind pose of bone i relative to its parent p is bindposes[p] * inverse(bindposes[i]).
        /// </summary>
        /// <summary>
        /// Parts copied from a renderer whose object path contains "droneball" are shown while the
        /// skill whose activation state is ThrowDroneBall has stock. Matched by type NAME so this
        /// compiles against game builds without the DLC3 states.
        /// </summary>
        private static void WireStockDrivenParts(GameObject container, CharacterBody body)
        {
            if (!container || !body) return;
            try
            {
                GenericSkill throwSkill = null;
                foreach (GenericSkill skill in body.GetComponents<GenericSkill>())
                {
                    Type st = skill && skill.skillDef && skill.skillDef.activationState.stateType != null ? skill.skillDef.activationState.stateType : null;
                    if (st != null && st.FullName == "EntityStates.DroneTech.Weapon.ThrowDroneBall") { throwSkill = skill; break; }
                }
                if (!throwSkill) return;

                int wired = 0;
                foreach (RuntimeHandPartMirror m in container.GetComponentsInChildren<RuntimeHandPartMirror>(true))
                {
                    if (!m.source) continue;
                    bool drone = false;
                    for (Transform t = m.source.transform; t && !drone; t = t.parent)
                        drone = t.name.ToLowerInvariant().Contains("droneball");
                    if (drone) { m.stockSkill = throwSkill; wired++; }
                }
                if (wired > 0) VRMod.StaticLogger.LogInfo($"[VR hands] {wired} drone-ball part(s) now follow the '{throwSkill.skillName}' charge (shown while stock > 0).");
            }
            catch (Exception e) { VRMod.StaticLogger.LogWarning($"[VR hands] Stock-driven part wiring failed: {e.Message}"); }
        }

        private static bool IsWeaponBone(Transform t)
        {
            string n = t.name.ToLowerInvariant();
            return n.StartsWith("weapon") || n.Contains("_weapon") || n.Contains("weapon_");
        }

        // 2.23.25: held props whose rig does not use the word "weapon" (Enforcer's shield and riot
        // shotgun, Acolyte's blades). Used ONLY to decide which parts follow the tunable weapon
        // offsets - the bind-pose pinning above still keys on IsWeaponBone, so existing hands
        // (Operator, False Son) bake exactly as before.
        private static readonly string[] heldPropWords = { "weapon", "gun", "shotgun", "rifle", "pistol", "shield", "blade", "sword", "knife", "javelin", "cleaver", "club", "hammer" };

        internal static bool IsHeldPropName(string name)
        {
            if (string.IsNullOrEmpty(name)) return false;
            string n = name.ToLowerInvariant();
            foreach (string w in heldPropWords) if (n.Contains(w)) return true;
            return false;
        }

        private static bool IsHeldPropBone(Transform t) => t && (IsWeaponBone(t) || IsHeldPropName(t.name));

        private static Transform GetWeaponRoot(GameObject container)
        {
            Transform weaponRoot = container.transform.Find("WeaponRoot");
            if (!weaponRoot)
            {
                weaponRoot = new GameObject("WeaponRoot").transform;
                weaponRoot.SetParent(container.transform, false);
            }
            return weaponRoot;
        }

        /// <summary>Bodies whose two hands hold DIFFERENT props (Enforcer: shield right, shotgun
        /// left), so each hand gets its own LeftWeapon* / RightWeapon* entries.</summary>
        internal static readonly HashSet<string> perSideWeaponBodies = new HashSet<string> { "Enforcer2Body", "AcolyteBody" };

        /// <summary>True for a per-side body and for its forms ("AcolyteBody_Reaper").</summary>
        internal static bool IsPerSideWeaponBody(string name)
        {
            foreach (string b in perSideWeaponBodies) if (name == b || name.StartsWith(b + "_")) return true;
            return false;
        }

        /// <summary>
        /// Per-character weapon offset (metres / degrees) applied to "weapon*" bones that hang off
        /// the hand bone but whose rest position is nowhere near the hand - the Operator's gun bone
        /// (weapon_hand_M) sits 1.4 units away from handL in the rig, so without pinning the gun
        /// floats out in space next to the VR hand.
        /// </summary>
        private static readonly Dictionary<string, BepInEx.Configuration.ConfigEntry<float>[]> weaponEntries = new Dictionary<string, BepInEx.Configuration.ConfigEntry<float>[]>();

        // Tuned held-weapon offsets (Operator 1.0.0, False Son 1.0.4); other bodies default to zero.
        private static readonly Dictionary<string, float[]> weaponDefaults = new Dictionary<string, float[]>
        {
            { "DroneTechBody", new[] { -0.07f, -0.03f, 0f, 15f, -90f, -10f } },
            { "FalseSonBody",  new[] { 0f, 0f, 0f, -5f, 10f, 20f } },
        };

        /// <summary>"Weapon" for bodies with one shared set, "LeftWeapon"/"RightWeapon" for perSideWeaponBodies.</summary>
        internal static string WeaponKeyPrefix(string bodyName, bool leftHand) =>
            IsPerSideWeaponBody(bodyName) ? (leftHand ? "LeftWeapon" : "RightWeapon") : "Weapon";

        internal static BepInEx.Configuration.ConfigEntry<float>[] BindWeaponEntries(string bodyName, bool leftHand)
        {
            if (!IsPerSideWeaponBody(bodyName)) return BindWeaponEntries(bodyName);
            string prefix = WeaponKeyPrefix(bodyName, leftHand);
            string key = bodyName + "/" + prefix;
            BepInEx.Configuration.ConfigEntry<float>[] e;
            if (weaponEntries.TryGetValue(key, out e)) return e;
            string sideWord = leftHand ? "LEFT" : "RIGHT";
            e = new[]
            {
                ModConfig.BindRuntimeHand(bodyName, prefix + "PosX", 0f, $"{sideWord}-hand held WEAPON/prop position offset (metres) along X (RED), relative to its default pose. Moves only the prop (e.g. Enforcer's shield or shotgun), not the hand."),
                ModConfig.BindRuntimeHand(bodyName, prefix + "PosY", 0f, $"{sideWord}-hand held WEAPON/prop position offset (metres) along Y (GREEN)."),
                ModConfig.BindRuntimeHand(bodyName, prefix + "PosZ", 0f, $"{sideWord}-hand held WEAPON/prop position offset (metres) along Z (BLUE)."),
                ModConfig.BindRuntimeHand(bodyName, prefix + "RotX", 0f, $"{sideWord}-hand held WEAPON/prop rotation offset (degrees) around X (RED), relative to the weapon's default pose. Independent of the hand offsets." + AxisNote),
                ModConfig.BindRuntimeHand(bodyName, prefix + "RotY", 0f, $"{sideWord}-hand held WEAPON/prop rotation offset (degrees) around Y (GREEN), relative to the weapon's default pose. Independent of the hand offsets." + AxisNote),
                ModConfig.BindRuntimeHand(bodyName, prefix + "RotZ", 0f, $"{sideWord}-hand held WEAPON/prop rotation offset (degrees) around Z (BLUE), relative to the weapon's default pose. Independent of the hand offsets." + AxisNote),
            };
            weaponEntries[key] = e;
            Rot(bodyName, prefix + "Rot", e[3], e[4], e[5], Quaternion.identity);
            return e;
        }

        internal static BepInEx.Configuration.ConfigEntry<float>[] BindWeaponEntries(string bodyName)
        {
            BepInEx.Configuration.ConfigEntry<float>[] e;
            if (weaponEntries.TryGetValue(bodyName, out e)) return e;
            float[] w;
            if (!weaponDefaults.TryGetValue(bodyName, out w)) w = new float[6];
            else w = new[] { w[0], w[1], w[2], 0f, 0f, 0f }; // 2.23.38: rotation lives in WeaponRotBase
            e = new[]
            {
                ModConfig.BindRuntimeHand(bodyName, "WeaponPosX", w[0], "Held WEAPON position offset (metres) along X (RED). Only used when the rig parents a 'weapon' bone to the hand (Operator's gun)."),
                ModConfig.BindRuntimeHand(bodyName, "WeaponPosY", w[1], "Held WEAPON position offset (metres) along Y (GREEN)."),
                ModConfig.BindRuntimeHand(bodyName, "WeaponPosZ", w[2], "Held WEAPON position offset (metres) along Z (BLUE)."),
                ModConfig.BindRuntimeHand(bodyName, "WeaponRotX", w[3], "Held WEAPON rotation offset (degrees) around X (RED), relative to the weapon's default pose. Independent of the hand offsets." + AxisNote),
                ModConfig.BindRuntimeHand(bodyName, "WeaponRotY", w[4], "Held WEAPON rotation offset (degrees) around Y (GREEN), relative to the weapon's default pose. Independent of the hand offsets." + AxisNote),
                ModConfig.BindRuntimeHand(bodyName, "WeaponRotZ", w[5], "Held WEAPON rotation offset (degrees) around Z (BLUE), relative to the weapon's default pose. Independent of the hand offsets." + AxisNote),
            };
            weaponEntries[bodyName] = e;
            Rot(bodyName, "WeaponRot", e[3], e[4], e[5], BakedWeaponRotation(bodyName));
            return e;
        }

        private static List<BoneSnapshot> PoseToBind(SkinnedMeshRenderer smr, Transform[] bones, bool[] inSet)
        {
            List<BoneSnapshot> snapshot = new List<BoneSnapshot>();
            if (!smr || !smr.sharedMesh) return snapshot;
            Matrix4x4[] bind = smr.sharedMesh.bindposes;
            if (bind == null || bind.Length != bones.Length) return snapshot;

            Dictionary<Transform, int> indexOf = new Dictionary<Transform, int>();
            for (int i = 0; i < bones.Length; i++) if (bones[i] && !indexOf.ContainsKey(bones[i])) indexOf[bones[i]] = i;

            for (int i = 0; i < bones.Length; i++)
            {
                if (!inSet[i] || !bones[i] || !bones[i].parent) continue;
                int pi;
                if (!indexOf.TryGetValue(bones[i].parent, out pi)) continue;

                Matrix4x4 local = bind[pi] * bind[i].inverse;
                Vector3 pos = local.GetColumn(3);
                Quaternion rot = Quaternion.LookRotation(local.GetColumn(2), local.GetColumn(1));

                snapshot.Add(new BoneSnapshot { bone = bones[i], localPosition = bones[i].localPosition, localRotation = bones[i].localRotation });

                // Weapon bone parented to a hand-chain bone: pin it to that bone's origin (plus the
                // per-character offset) instead of its far-away rest position.
                if (IsWeaponBone(bones[i]) && !IsWeaponBone(bones[i].parent))
                {
                    pos = Vector3.zero;
                }

                bones[i].localRotation = rot;
                bones[i].localPosition = pos;
            }
            return snapshot;
        }

        private static void RestorePose(List<BoneSnapshot> snapshot)
        {
            foreach (BoneSnapshot b in snapshot)
            {
                if (!b.bone) continue;
                b.bone.localPosition = b.localPosition;
                b.bone.localRotation = b.localRotation;
            }
        }

        /// <summary>
        /// Finger-direction estimate taken with the hand subtree in its bind pose, so it does
        /// not depend on the animation frame at bake time.
        /// </summary>
        /// <summary>
        /// Poses <paramref name="root"/> and its whole subtree into the bind pose using the first
        /// skinned renderer whose skeleton contains that exact bone instance. Returns the snapshot
        /// to restore with <see cref="RestorePose"/> (empty if no renderer matched).
        /// </summary>
        private static List<BoneSnapshot> PoseSubtreeToBind(Transform model, Transform root)
        {
            foreach (SkinnedMeshRenderer smr in model.GetComponentsInChildren<SkinnedMeshRenderer>(true))
            {
                if (!smr.sharedMesh) continue;
                Transform[] bones = smr.bones;
                bool found = false;
                bool[] inSet = new bool[bones.Length];
                for (int i = 0; i < bones.Length; i++)
                {
                    if (!bones[i]) continue;
                    if (bones[i] == root) found = true;
                    inSet[i] = bones[i] == root || bones[i].IsChildOf(root);
                }
                if (found) return PoseToBind(smr, bones, inSet);
            }
            return new List<BoneSnapshot>();
        }

        /// <summary>
        /// Finger-direction estimate taken with the hand subtree in its bind pose, so it does
        /// not depend on the animation frame at bake time.
        /// </summary>
        private static Vector3 EstimateFingerDirectionAtBind(Transform model, Transform handBone)
        {
            List<BoneSnapshot> snap = PoseSubtreeToBind(model, handBone);
            try { return EstimateFingerDirection(handBone); }
            finally { RestorePose(snap); }
        }

        private static Vector3 EstimateFingerDirection(Transform handBone)
        {
            Vector3 sum = Vector3.zero;
            int count = 0;

            foreach (Transform child in handBone)
            {
                // An item display hanging off the hand would drag this estimate around every time
                // a new item is picked up - only the character's own child bones count.
                if (child.GetComponent<ItemDisplay>() || child.GetComponentInChildren<ItemDisplay>(true)) continue;
                // A weapon bone is not a finger, and the Operator's sits 1.4 units away.
                if (IsWeaponBone(child)) continue;

                Vector3 local = handBone.InverseTransformPoint(child.position);
                if (local.sqrMagnitude > 1e-8f) { sum += local.normalized; count++; }
            }

            if (count > 0 && sum.sqrMagnitude > 1e-6f)
                return (sum / count).normalized;

            // No finger bones: fingers point away from the forearm.
            if (handBone.parent)
            {
                Vector3 toParent = handBone.InverseTransformPoint(handBone.parent.position);
                if (toParent.sqrMagnitude > 1e-8f) return (-toParent).normalized;
            }

            return Vector3.forward;
        }

        /// <summary>
        /// Bakes every triangle (from all of the model's SkinnedMeshRenderers) whose vertices are
        /// majority-weighted to the given bone set into rigid meshes, expressed in hand-bone local
        /// space, preserving source materials. Returns null when nothing qualified.
        /// </summary>
        private static GameObject BakeHandMesh(Transform model, Transform handBone, HashSet<Transform> boneSet, string bodyName,
            List<CharacterModel.RendererInfo> rendererInfos, out int characterParts)
        {
            characterParts = 0;
            skinnedCharacterParts = 0;
            skinPending = false;
            // The game's own material pipeline (CharacterModel.UpdateRendererMaterials, driven for
            // hands by MotionControls.UpdateHandMaterials) is what makes RoR2's hgstandard
            // materials render correctly. It only runs on renderers listed in Hand.rendererInfos,
            // and it needs the DEFAULT material asset from the model's baseRendererInfos - not the
            // runtime instance on the renderer. Look those up here.
            CharacterModel charModel = model.GetComponent<CharacterModel>();
            GameObject container = null;
            bool debug = ModConfig.RuntimeHandsDebug.Value;

            SkinnedMeshRenderer[] renderers = model.GetComponentsInChildren<SkinnedMeshRenderer>(true);

            if (debug)
            {
                if (renderers.Any(r => r.sharedMesh)) DumpSkeletonTree(model, bodyName);
                VRMod.StaticLogger.LogInfo($"[VR debug] model '{model.name}': {renderers.Length} skinned renderer(s), {model.GetComponentsInChildren<MeshFilter>(true).Length} mesh filter(s).");
            }

            foreach (SkinnedMeshRenderer smr in renderers)
            {
                if (ItemDisplayHider.ShouldHide(charModel, smr.transform)) continue;
                if (InHiddenForm(smr.transform)) continue;
                if (!smr.sharedMesh)
                {
                    // RoR2 applies skins asynchronously - at spawn time the mesh may not be
                    // assigned yet. The caller retries until it is.
                    if (debug) VRMod.StaticLogger.LogInfo($"[VR debug] smr '{smr.name}': sharedMesh is NULL (skin not applied yet?)");
                    skinPending = true;
                    continue;
                }

                Transform[] bones = smr.bones;

                // 2.20.1: a renderer can be skinned to a DIFFERENT skeleton instance than the one
                // the global name search found (ragdoll/IK duplicates carry the same bone names).
                // Match the hand bone inside THIS renderer's own bone array by name, and use that
                // instance for both the ancestry test and the mesh space, so bone references always
                // line up with the bone weights.
                Transform smrHand = null;
                for (int i = 0; i < bones.Length; i++)
                {
                    if (bones[i] && bones[i].name == handBone.name) { smrHand = bones[i]; break; }
                }

                bool[] boneInSet = new bool[bones.Length];
                bool any = false;
                for (int i = 0; i < bones.Length; i++)
                {
                    if (!bones[i]) continue;

                    if (smrHand)
                    {
                        // Forearm option includes the whole forearm subtree (twist/roll bones too).
                        boneInSet[i] = BodyIncludesForearm(bodyName) && smrHand.parent
                            ? bones[i].IsChildOf(smrHand.parent)
                            : bones[i].IsChildOf(smrHand);
                    }
                    else
                    {
                        // Fallback: reference match against the globally-found subtree, then name match.
                        boneInSet[i] = boneSet.Contains(bones[i]) || boneSet.Any(b => b && b.name == bones[i].name);
                    }
                    any |= boneInSet[i];
                }

                if (debug)
                {
                    int inSet = boneInSet.Count(x => x);
                    VRMod.StaticLogger.LogInfo($"[VR debug] smr '{smr.name}': mesh='{smr.sharedMesh.name}', bones={bones.Length}, handBoneInSkeleton={(smrHand ? smrHand.name : "NO")}, bonesInSet={inSet}");
                }

                if (!any) continue;

                if (debug) ReportBindDelta(smr, bones, boneInSet);

                Matrix4x4 worldToHand = (smrHand ? smrHand : handBone).worldToLocalMatrix;

                Mesh shared = smr.sharedMesh;
                BoneWeight[] weights = shared.boneWeights;
                if (weights == null || weights.Length == 0)
                {
                    if (debug) VRMod.StaticLogger.LogInfo($"[VR debug] smr '{smr.name}': mesh has NO bone weights, skipped.");
                    continue;
                }

                // Per-vertex weight on the hand bone set (and how much of it is on weapon bones).
                bool[] boneIsWeapon = new bool[bones.Length];
                for (int i = 0; i < bones.Length; i++) boneIsWeapon[i] = boneInSet[i] && bones[i] && IsHeldPropBone(bones[i]);

                float[] setWeight = new float[weights.Length];
                float weaponWeightTotal = 0f, setWeightTotal = 0f;
                for (int v = 0; v < weights.Length; v++)
                {
                    BoneWeight w = weights[v];
                    float s = 0f, wp = 0f;
                    if (w.boneIndex0 < boneInSet.Length && boneInSet[w.boneIndex0]) { s += w.weight0; if (boneIsWeapon[w.boneIndex0]) wp += w.weight0; }
                    if (w.boneIndex1 < boneInSet.Length && boneInSet[w.boneIndex1]) { s += w.weight1; if (boneIsWeapon[w.boneIndex1]) wp += w.weight1; }
                    if (w.boneIndex2 < boneInSet.Length && boneInSet[w.boneIndex2]) { s += w.weight2; if (boneIsWeapon[w.boneIndex2]) wp += w.weight2; }
                    if (w.boneIndex3 < boneInSet.Length && boneInSet[w.boneIndex3]) { s += w.weight3; if (boneIsWeapon[w.boneIndex3]) wp += w.weight3; }
                    setWeight[v] = s;
                    setWeightTotal += s;
                    weaponWeightTotal += wp;
                }
                // A renderer that is mostly skinned to weapon bones is the held weapon: it goes
                // under a separate WeaponRoot so its offsets can be tuned live and independently.
                bool isWeaponPart = setWeightTotal > 0f && (weaponWeightTotal / setWeightTotal > 0.5f || IsHeldPropName(smr.name));

                // Bake with the hand subtree in its bind pose so every bake (spawn, stage change,
                // item pickup) produces the same geometry, then put the live pose back.
                List<BoneSnapshot> poseSnapshot = PoseToBind(smr, bones, boneInSet);
                Mesh baked = new Mesh();
                Matrix4x4 toHand;
                try
                {
                    smr.BakeMesh(baked);
                    // Hand-bone space must be taken in the SAME pose as the bake.
                    worldToHand = (smrHand ? smrHand : handBone).worldToLocalMatrix;
                    // BakeMesh output is in the renderer's local space without its transform scale;
                    // include the renderer's lossy scale when mapping to world.
                    Matrix4x4 rendererToWorld = Matrix4x4.TRS(smr.transform.position, smr.transform.rotation, Vector3.one);
                    toHand = worldToHand * rendererToWorld;
                }
                finally { RestorePose(poseSnapshot); }
                Vector3[] bakedVerts = baked.vertices;
                Vector3[] bakedNormals = baked.normals;
                Vector2[] uv = shared.uv;

                Material[] mats = smr.sharedMaterials;
                List<Material> outMats = new List<Material>();
                List<int[]> outTris = new List<int[]>();
                Dictionary<int, int> remap = new Dictionary<int, int>();
                List<Vector3> outVerts = new List<Vector3>();
                List<Vector3> outNormals = new List<Vector3>();
                List<Vector2> outUV = new List<Vector2>();

                for (int sub = 0; sub < shared.subMeshCount; sub++)
                {
                    int[] tris = shared.GetTriangles(sub);
                    List<int> kept = new List<int>();

                    for (int t = 0; t < tris.Length; t += 3)
                    {
                        int a = tris[t], b = tris[t + 1], c = tris[t + 2];
                        if (a >= setWeight.Length || b >= setWeight.Length || c >= setWeight.Length) continue;

                        float avg = (setWeight[a] + setWeight[b] + setWeight[c]) / 3f;
                        if (avg < 0.3f) continue;

                        kept.Add(Remap(a, remap, outVerts, outNormals, outUV, bakedVerts, bakedNormals, uv, toHand));
                        kept.Add(Remap(b, remap, outVerts, outNormals, outUV, bakedVerts, bakedNormals, uv, toHand));
                        kept.Add(Remap(c, remap, outVerts, outNormals, outUV, bakedVerts, bakedNormals, uv, toHand));
                    }

                    if (kept.Count > 0)
                    {
                        outTris.Add(kept.ToArray());
                        outMats.Add(sub < mats.Length ? mats[sub] : null);
                    }
                }

                UnityEngine.Object.Destroy(baked);

                if (debug)
                {
                    float maxW = setWeight.Length > 0 ? setWeight.Max() : 0f;
                    VRMod.StaticLogger.LogInfo($"[VR debug] smr '{smr.name}': keptVerts={outVerts.Count}, maxHandWeight={maxW:F2}");
                }

                if (outVerts.Count == 0) continue;

                Mesh handMesh = new Mesh();
                handMesh.name = $"RuntimeHand_{bodyName}_{smr.name}";
                handMesh.SetVertices(outVerts);
                if (outNormals.Count == outVerts.Count) handMesh.SetNormals(outNormals);
                if (outUV.Count == outVerts.Count) handMesh.SetUVs(0, outUV);
                handMesh.subMeshCount = outTris.Count;
                for (int s = 0; s < outTris.Count; s++) handMesh.SetTriangles(outTris[s], s);
                handMesh.RecalculateBounds();
                if (outNormals.Count != outVerts.Count) handMesh.RecalculateNormals();

                if (!container) container = new GameObject("Mesh");

                Transform partParent = container.transform;
                if (isWeaponPart)
                {
                    partParent = GetWeaponRoot(container);
                    if (debug) VRMod.StaticLogger.LogInfo($"[VR debug] '{smr.name}' is a held WEAPON part ({weaponWeightTotal / setWeightTotal:P0} of its hand weight is on prop bones) - tune it with '{bodyName}_{(IsPerSideWeaponBody(bodyName) ? "<Left|Right>Weapon" : "Weapon")}*'.");
                }
                else if (debug) VRMod.StaticLogger.LogInfo($"[VR debug] '{smr.name}' baked as part of the HAND (not a weapon; {(setWeightTotal > 0f ? weaponWeightTotal / setWeightTotal : 0f):P0} of its weight on prop bones).");

                GameObject part = new GameObject(smr.name);
                part.transform.SetParent(partParent, false);
                MeshFilter mf = part.AddComponent<MeshFilter>();
                mf.sharedMesh = handMesh;
                MeshRenderer mr = part.AddComponent<MeshRenderer>();
                mr.sharedMaterials = outMats.ToArray();

                // Carry over per-renderer shader data (elite index, dither/fade, tints) that RoR2
                // drives through MaterialPropertyBlocks - without it the material renders wrong.
                CopyPropertyBlock(smr, mr);
                part.AddComponent<RuntimeHandPartMirror>().source = smr;
                RegisterRendererInfo(rendererInfos, charModel, smr, mr, outMats.Count > 0 ? outMats[0] : null);
                if (!TagIfItemDisplay(smr, part)) { characterParts++; skinnedCharacterParts++; }

                if (ModConfig.RuntimeHandsDebug.Value)
                {
                    VRMod.StaticLogger.LogInfo($"[VR debug] runtime hand part from '{smr.name}': {outVerts.Count} verts, {outTris.Count} submesh(es).");
                }
            }

            // 2.20.2: weapons and other held props are often RIGID meshes parented under a
            // hand/weapon bone rather than skin-weighted to it (the authored prefabs ship these as
            // part of the hand model - CommandoPistol, HuntressBow, etc.). Clone every non-skinned
            // MeshRenderer whose ancestor chain passes through the hand bone subtree, expressed in
            // the hand bone's space so it keeps its exact grip position.
            HashSet<string> subtreeNames = new HashSet<string>(boneSet.Where(b => b).Select(b => b.name));

            foreach (MeshFilter mf in model.GetComponentsInChildren<MeshFilter>(true))
            {
                if (InHiddenForm(mf.transform)) continue;
                if (!mf.sharedMesh) continue;

                MeshRenderer sourceMr = mf.GetComponent<MeshRenderer>();
                if (!sourceMr) continue;
                if (ItemDisplayHider.ShouldHide(charModel, mf.transform)) continue;

                // Walk up: is this mesh attached under the hand chain? Remember the hand bone
                // instance of THIS hierarchy for the space conversion.
                Transform t = mf.transform.parent;
                Transform chainHand = null;
                bool underHand = false;
                bool rigidIsProp = IsHeldPropName(mf.name);
                while (t && t != model.parent)
                {
                    if (subtreeNames.Contains(t.name)) underHand = true;
                    if (t.name == handBone.name) { chainHand = t; break; }
                    if (IsHeldPropBone(t)) rigidIsProp = true;
                    t = t.parent;
                }

                if (!underHand) continue;

                Transform space = chainHand ? chainHand : handBone;

                if (!container) container = new GameObject("Mesh");

                GameObject part = new GameObject(mf.name + "_rigid");
                // Held props go under WeaponRoot (pose captured relative to the hand space first,
                // WeaponRoot itself sits at the hand origin until the offsets move it).
                part.transform.SetParent(rigidIsProp ? GetWeaponRoot(container) : container.transform, false);
                // Grip position relative to the hand in the BIND pose (a prop hanging off a finger
                // bone would otherwise move with the finger curl of the frame we baked on).
                List<BoneSnapshot> rigidSnapshot = PoseSubtreeToBind(model, space);
                try
                {
                    part.transform.localPosition = space.InverseTransformPoint(mf.transform.position);
                    part.transform.localRotation = Quaternion.Inverse(space.rotation) * mf.transform.rotation;
                }
                finally { RestorePose(rigidSnapshot); }

                Vector3 spaceScale = space.lossyScale;
                Vector3 meshScale = mf.transform.lossyScale;
                part.transform.localScale = new Vector3(
                    spaceScale.x != 0f ? meshScale.x / spaceScale.x : 1f,
                    spaceScale.y != 0f ? meshScale.y / spaceScale.y : 1f,
                    spaceScale.z != 0f ? meshScale.z / spaceScale.z : 1f);

                MeshFilter newMf = part.AddComponent<MeshFilter>();
                newMf.sharedMesh = mf.sharedMesh;
                MeshRenderer newMr = part.AddComponent<MeshRenderer>();
                newMr.sharedMaterials = sourceMr.sharedMaterials;

                CopyPropertyBlock(sourceMr, newMr);
                part.AddComponent<RuntimeHandPartMirror>().source = sourceMr;
                RegisterRendererInfo(rendererInfos, charModel, sourceMr, newMr,
                    sourceMr.sharedMaterials != null && sourceMr.sharedMaterials.Length > 0 ? sourceMr.sharedMaterials[0] : null);
                if (!TagIfItemDisplay(sourceMr, part)) characterParts++;

                if (debug)
                {
                    VRMod.StaticLogger.LogInfo($"[VR debug] rigid mesh '{mf.name}' captured under hand chain (space: {space.name}) as {(rigidIsProp ? "a held WEAPON part" : "part of the HAND")}.");
                }
            }

            return container;
        }

        /// <summary>
        /// Item pickups attached to the hand (razor wire and friends) are baked in alongside the
        /// arm so they stay visible on the VR hand - but they are modelled for the third-person
        /// character and are enormous in first person, so the baked copy is marked and scaled.
        /// </summary>
        private static bool TagIfItemDisplay(Renderer source, GameObject part)
        {
            if (!source.GetComponentInParent<ItemDisplay>()) return false;

            part.AddComponent<RuntimeHandItemPart>();

            if (ModConfig.RuntimeHandsDebug.Value)
                VRMod.StaticLogger.LogInfo($"[VR debug] '{source.name}' is an item display - scaling it on the hand.");

            return true;
        }

        private static void CopyPropertyBlock(Renderer source, Renderer target)
        {
            try
            {
                MaterialPropertyBlock block = new MaterialPropertyBlock();
                source.GetPropertyBlock(block);
                if (!block.isEmpty) target.SetPropertyBlock(block);
            }
            catch { }
        }

        /// <summary>
        /// Adds a RendererInfo for the baked renderer so MotionControls.UpdateHandMaterials feeds
        /// it through the game's own CharacterModel.UpdateRendererMaterials, exactly like the
        /// authored hand prefabs. The default material is taken from the model's baseRendererInfos
        /// entry for the SOURCE renderer when available (that is the material asset the game
        /// expects), falling back to the material currently on the renderer.
        /// </summary>
        private static void RegisterRendererInfo(List<CharacterModel.RendererInfo> infos, CharacterModel charModel,
            Renderer source, Renderer target, Material fallback)
        {
            Material defaultMaterial = fallback;
            bool ignoreOverlays = false;

            if (charModel != null && charModel.baseRendererInfos != null)
            {
                foreach (CharacterModel.RendererInfo info in charModel.baseRendererInfos)
                {
                    if (info.renderer != source) continue;
                    if (info.defaultMaterial) defaultMaterial = info.defaultMaterial;
                    ignoreOverlays = info.ignoreOverlays;
                    break;
                }
            }

            if (!defaultMaterial) return;

            // Give the renderer the default material too, so it looks right even before the first
            // UpdateMaterials pass runs.
            target.sharedMaterial = defaultMaterial;

            infos.Add(new CharacterModel.RendererInfo
            {
                renderer = target,
                defaultMaterial = defaultMaterial,
                defaultShadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off,
                ignoreOverlays = ignoreOverlays
            });
        }

        private static int Remap(int src, Dictionary<int, int> remap, List<Vector3> verts, List<Vector3> normals, List<Vector2> uvs,
            Vector3[] bakedVerts, Vector3[] bakedNormals, Vector2[] uv, Matrix4x4 toHand)
        {
            int idx;
            if (remap.TryGetValue(src, out idx)) return idx;

            idx = verts.Count;
            remap[src] = idx;
            verts.Add(toHand.MultiplyPoint3x4(bakedVerts[src]));
            if (bakedNormals != null && src < bakedNormals.Length)
                normals.Add(toHand.MultiplyVector(bakedNormals[src]).normalized);
            if (uv != null && src < uv.Length)
                uvs.Add(uv[src]);
            return idx;
        }

        private static void AddAxisLines(Transform parent, HandController controller)
        {
            // Shader.Find is unreliable in RoR2 (catalog lookup that throws on unknown names).
            // Instead, borrow the material from the controller's own aim-ray LineRenderer - it is
            // always present and known to render correctly in this game.
            Material lineMat = null;
            LineRenderer sourceRay = controller ? controller.GetComponentInChildren<LineRenderer>(true) : null;
            if (sourceRay && sourceRay.sharedMaterial) lineMat = sourceRay.sharedMaterial;

            if (!lineMat)
            {
                Shader shader = TryFindShader("Sprites/Default", "UI/Default", "Hidden/Internal-Colored", "Standard");
                if (shader) lineMat = new Material(shader);
            }

            if (!lineMat)
            {
                VRMod.StaticLogger.LogWarning("[VR debug] No usable material for axis lines - skipping them (hand mesh unaffected).");
                return;
            }

            AddAxisLine(parent, Vector3.right, Color.red, lineMat);
            AddAxisLine(parent, Vector3.up, Color.green, lineMat);
            AddAxisLine(parent, Vector3.forward, Color.blue, lineMat);
        }

        private static Shader TryFindShader(params string[] names)
        {
            foreach (string name in names)
            {
                try
                {
                    Shader sh = Shader.Find(name);
                    if (sh) return sh;
                }
                catch { }
            }
            return null;
        }

        private static void AddAxisLine(Transform parent, Vector3 dir, Color color, Material baseMat)
        {
            GameObject go = new GameObject($"DebugAxis_{dir}");
            go.transform.SetParent(parent, false);
            LineRenderer lr = go.AddComponent<LineRenderer>();
            lr.useWorldSpace = false;
            lr.positionCount = 2;
            lr.SetPosition(0, Vector3.zero);
            lr.SetPosition(1, dir * 0.32f);   // 2.23.38: doubled again (2.23.33: 0.16)
            lr.startWidth = 0.016f;
            lr.endWidth = 0.004f;
            Material mat = new Material(baseMat);
            mat.color = color;
            lr.material = mat;
            lr.startColor = color;
            lr.endColor = color;
        }

        /// <summary>
        /// Schedules a delayed retry of the runtime hand build on this controller. RoR2 applies
        /// character skins asynchronously, so at hand-pair time the model's SkinnedMeshRenderers
        /// can still have null meshes - the first build then finds zero triangles. The retry polls
        /// until the meshes exist and reapplies the whole hand pair.
        /// </summary>
        internal static void ScheduleRetry(HandController controller, string bodyName)
        {
            if (!controller || controller.GetComponent<RuntimeHandRetry>()) return;

            RuntimeHandRetry retry = controller.gameObject.AddComponent<RuntimeHandRetry>();
            retry.controller = controller;
            retry.bodyName = bodyName;
        }

        /// <summary>
        /// Cheap signature of what the character is carrying. Watching the inventory directly is
        /// far more reliable than counting item displays on one bone: a pickup may attach its
        /// model to the forearm, the chest or nothing at all, and we want the hand re-baked the
        /// moment the inventory changes either way.
        /// </summary>
        internal static int InventorySum(CharacterBody body)
        {
            if (!body || !body.inventory) return 0;

            int sum = 0;

            try
            {
                // itemAcquisitionOrder is the public list of distinct items held, in pickup order.
                // Combining its count with each item's stack size changes on any acquisition.
                var order = body.inventory.itemAcquisitionOrder;
                if (order != null)
                {
                    for (int i = 0; i < order.Count; i++)
                    {
                        sum += (int)order[i] + body.inventory.GetItemCount(order[i]) * 31;
                    }
                }
            }
            catch { }

            return sum;
        }

        internal static int CountItemDisplays(Transform bone)
        {
            return bone ? bone.GetComponentsInChildren<ItemDisplay>(true).Length : 0;
        }

        /// <summary>Set by BakeHandMesh: skinned (not rigid prop) character parts captured, and
        /// whether any of the model's skinned renderers still had no mesh (skin not applied yet).</summary>
        private static int skinnedCharacterParts;
        private static bool skinPending;

        private static readonly HashSet<string> dumpedModels = new HashSet<string>();

        /// <summary>One-time indented skeleton dump for a body (debug mode), so unusual rigs can be
        /// mapped from the log alone.</summary>
        private static void DumpSkeletonTree(Transform model, string bodyName)
        {
            if (!dumpedModels.Add(bodyName)) return;
            try
            {
                Transform root = null;
                foreach (SkinnedMeshRenderer smr in model.GetComponentsInChildren<SkinnedMeshRenderer>(true))
                    if (smr.rootBone) { root = smr.rootBone; while (root.parent && root.parent != model) root = root.parent; break; }
                if (!root) root = model;
                System.Text.StringBuilder sb = new System.Text.StringBuilder();
                sb.AppendLine($"[VR debug] Skeleton tree for '{bodyName}' (root '{root.name}'):");
                AppendTree(sb, root, 0, 0);
                VRMod.StaticLogger.LogInfo(sb.ToString());
            }
            catch (Exception e) { VRMod.StaticLogger.LogWarning($"[VR debug] Skeleton dump failed: {e.Message}"); }
        }

        private static void AppendTree(System.Text.StringBuilder sb, Transform t, int depth, int count)
        {
            if (depth > 12 || sb.Length > 60000) return;
            sb.Append(' ', depth * 2).Append(t.name);
            if (t.GetComponent<ItemDisplay>()) sb.Append(" [item display]");
            if (t.GetComponent<Renderer>()) sb.Append(" [renderer]");
            sb.Append($" lp={t.localPosition} ls={t.localScale}");
            sb.AppendLine();
            foreach (Transform c in t) AppendTree(sb, c, depth + 1, count + 1);
        }

        /// <summary>Reports bones of the set whose live pose differs a lot from the bind pose -
        /// a big gap on a weapon bone means the animator (not the bind pose) is what holds the
        /// weapon in place.</summary>
        private static void ReportBindDelta(SkinnedMeshRenderer smr, Transform[] bones, bool[] inSet)
        {
            try
            {
                Matrix4x4[] bind = smr.sharedMesh.bindposes;
                if (bind == null || bind.Length != bones.Length) return;
                Dictionary<Transform, int> indexOf = new Dictionary<Transform, int>();
                for (int i = 0; i < bones.Length; i++) if (bones[i] && !indexOf.ContainsKey(bones[i])) indexOf[bones[i]] = i;
                System.Text.StringBuilder sb = new System.Text.StringBuilder($"[VR debug] bind-vs-live for '{smr.name}':");
                int n = 0;
                for (int i = 0; i < bones.Length; i++)
                {
                    if (!inSet[i] || !bones[i] || !bones[i].parent) continue;
                    int pi;
                    if (!indexOf.TryGetValue(bones[i].parent, out pi)) { sb.Append($" {bones[i].name}(parent '{bones[i].parent.name}' not in skeleton)"); continue; }
                    Matrix4x4 local = bind[pi] * bind[i].inverse;
                    Vector3 bp = local.GetColumn(3);
                    Quaternion br = Quaternion.LookRotation(local.GetColumn(2), local.GetColumn(1));
                    float dPos = Vector3.Distance(bp, bones[i].localPosition);
                    float dRot = Quaternion.Angle(br, bones[i].localRotation);
                    if (dPos > 0.02f || dRot > 15f) { sb.Append($" {bones[i].name}[dPos={dPos:F3} dRot={dRot:F0} bindLP={bp} liveLP={bones[i].localPosition}]"); n++; }
                }
                if (n == 0) sb.Append(" (all bones within 2 cm / 15 deg of bind pose)");
                VRMod.StaticLogger.LogInfo(sb.ToString());
            }
            catch (Exception e) { VRMod.StaticLogger.LogWarning($"[VR debug] bind delta failed: {e.Message}"); }
        }

        private static void DumpBones(Transform model)
        {
            System.Text.StringBuilder sb = new System.Text.StringBuilder("[VR debug] model bones: ");
            foreach (Transform t in model.GetComponentsInChildren<Transform>(true))
            {
                sb.Append(t.name).Append(", ");
            }
            VRMod.StaticLogger.LogInfo(sb.ToString());
        }
    }

    /// <summary>
    /// Keeps a baked hand part's visibility in step with the renderer it was copied from. The
    /// bake is a static snapshot, so anything the game shows/hides on the real model afterwards
    /// (the Operator's drone sitting in the claw until it is launched, then returning) would
    /// otherwise stay frozen in the VR hand.
    /// </summary>
    internal class RuntimeHandPartMirror : MonoBehaviour
    {
        internal Renderer source;
        internal bool sourceHidden;
        /// <summary>When set, the part is shown exactly while this skill has a charge (the
        /// Operator's drone ball: in the claw while ThrowDroneBall has stock, gone while it is
        /// out / recharging). The game never re-activates the docked ball object after the
        /// first throw, so its active flag cannot be used for the return.</summary>
        internal GenericSkill stockSkill;

        private Renderer own;
        private float nextCheck;
        private bool seenActive;

        private void Awake()
        {
            own = GetComponent<Renderer>();
        }

        private void Update()
        {
            if (Time.unscaledTime < nextCheck) return;
            nextCheck = Time.unscaledTime + 0.1f;

            if (!own || !source) return; // source destroyed: the watchdog re-bakes soon

            if (stockSkill)
            {
                bool inClaw = stockSkill.stock > 0;
                sourceHidden = !inClaw;
                if (own.enabled != inClaw) own.enabled = inClaw;
                return;
            }

            // NOT Renderer.enabled: in first person the mod disables every renderer on the
            // local character model each frame to hide the player's own body, so that flag is
            // always false for our sources. What the game toggles for things like the Operator's
            // docked drone is the object itself (SetActive) or, on some models, its scale.
            // The local character model is hidden in first person by deactivating its renderer
            // objects, so activeSelf is FALSE for every ordinary body part - mirroring that
            // blindly hides the whole hand. Things the game toggles at runtime (the Operator's
            // docked drone: active while docked, inactive after ThrowDroneBall) are the ones
            // that have been seen ACTIVE at some point; only those are mirrored.
            bool activeNow = source.gameObject.activeSelf;
            if (activeNow) seenActive = true;
            bool visible = !seenActive || activeNow;
            sourceHidden = !visible;
            if (own.enabled != visible) own.enabled = visible;

        }
    }

    /// <summary>
    /// Applies the auto-alignment plus the per-character config offsets every frame, and (while the
    /// debug flag is on) re-reads the config file every few seconds so the offsets can be tuned
    /// live with the headset on.
    /// </summary>
    internal class RuntimeHandAligner : MonoBehaviour
    {
        internal Quaternion autoAlign = Quaternion.identity;
        internal string bodyName;
        internal Transform aimRoot;
        internal bool isLeftHand;
        internal bool mirrorMesh;
        internal int builtModelId;
        internal Transform sourceHandBone;
        internal int itemDisplaysAtBuild;
        internal int inventorySumAtBuild;

        private BepInEx.Configuration.ConfigEntry<float> rotX, rotY, rotZ, posX, posY, posZ, scale;
        private BepInEx.Configuration.ConfigEntry<float> aimRotX, aimRotY, aimRotZ;
        private BepInEx.Configuration.ConfigEntry<float>[] weapon;
        private Transform weaponRoot;
        // 2.23.34: the held weapon/prop lives in its own space beside the hand mesh, posed like the
        // hand at its DEFAULT offsets, so tuning the hand's Rot/Pos/Scale no longer drags it along.
        private Transform weaponSpace;
        internal Transform WeaponSpace => weaponSpace;
        // 2.23.37: Enforcer's shield is only shown while it is in use (Protect and Serve, shield
        // dash, kinetic shield) - any held-prop renderer whose name contains "shield".
        private List<MeshRenderer> shieldRenderers;
        private static readonly List<MeshRenderer> allShieldRenderers = new List<MeshRenderer>();

        /// <summary>
        /// 2.23.42: "Enforcer: Shield opacity". 2.23.41 used the game shader's _Fade, which only
        /// dithers (solid or nearly gone, nothing in between). Below 100 % the shield now draws
        /// with a real alpha-blended material: a copy using the always-present "UI/Default"
        /// shader (unlit, depth-tested, both faces) carrying the shield's own texture and colour.
        /// Re-applied right after the character model writes the hand materials each frame.
        /// </summary>
        internal static void ApplyShieldOpacity()
        {
            if (allShieldRenderers.Count == 0) return;
            float a = Mathf.Clamp01(ModConfig.EnforcerShieldOpacity.Value / 100f);
            if (a >= 0.999f)
            {
                // 100 %: the game's own material, restored if our copy is still on.
                foreach (var kv in transparentCopies)
                    if (kv.Key && kv.Key.sharedMaterial == kv.Value && transparentSource[kv.Key]) kv.Key.sharedMaterial = transparentSource[kv.Key];
                return;
            }
            foreach (MeshRenderer r in allShieldRenderers)
            {
                if (!r) continue;
                Material src = r.sharedMaterial;
                if (!src) continue;
                transparentCopies.TryGetValue(r, out Material t);
                if (src != t)
                {
                    // The game re-assigned its own material this frame: (re)build our copy if the
                    // source changed, otherwise reuse it.
                    if (!t || transparentSource[r] != src)
                    {
                        // 2.23.43: Shader.Find("UI/Default") returned null in 2.23.42 (the slider then did
                        // nothing between 1 and 99 %). The UI's default canvas material always has it.
                        Shader ui = Canvas.GetDefaultCanvasMaterial() ? Canvas.GetDefaultCanvasMaterial().shader : null;
                        if (!ui) ui = Shader.Find("UI/Default");
                        if (!ui) { if (!shieldLogged) { shieldLogged = true; VRMod.StaticLogger.LogWarning("[VR shield] no alpha-blended shader available - opacity slider cannot fade the shield."); } return; }
                        if (t) UnityEngine.Object.Destroy(t);
                        t = new Material(ui) { name = src.name + " (VR transparent)" };
                        if (src.HasProperty("_MainTex")) t.mainTexture = src.mainTexture;
                        t.SetInt("unity_GUIZTestMode", (int)UnityEngine.Rendering.CompareFunction.LessEqual);
                        t.renderQueue = 3000;
                        transparentCopies[r] = t;
                        transparentSource[r] = src;
                    }
                }
                Color c = transparentSource[r] && transparentSource[r].HasProperty("_Color") ? transparentSource[r].color : Color.white;
                c.a = a;
                t.color = c;
                if (r.sharedMaterial != t) r.sharedMaterial = t;
            }
        }

        private static bool shieldLogged;
        private static readonly Dictionary<MeshRenderer, Material> transparentCopies = new Dictionary<MeshRenderer, Material>();
        private static readonly Dictionary<MeshRenderer, Material> transparentSource = new Dictionary<MeshRenderer, Material>();
        internal Transform axisGizmo;
        internal Transform weaponGizmo;
        private RuntimeHands.RotEntry handRot, aimRot, weaponRot;
        private Quaternion bakedHand;
        private float nextReload;
        private float nextWatchdog;
        // Drifter's scavenging changes item counts several times a second; one re-bake per
        // couple of seconds is plenty and keeps the log (and the GPU) quiet.
        private float inventoryRebakeNotBefore = Time.unscaledTime + 2f;
        private float nextRenderReport;
        private bool forearmAtBuild;
        private bool rebuildRequested;

        /// <summary>True while any of the body's state machines is in a shield state (Enforcer).</summary>
        private static bool IsShielding(CharacterBody body)
        {
            if (!body) return false;
            foreach (EntityStateMachine m in body.GetComponents<EntityStateMachine>())
            {
                if (m == null || m.state == null) continue;
                string n = m.state.GetType().Name;
                if (n.IndexOf("Shield", StringComparison.Ordinal) >= 0 && !n.StartsWith("NotIn") && !n.Contains("Unshield"))
                    return true;
            }
            return false;
        }

        /// <summary>Hand mesh rotation offset from its three config angles.</summary>
        internal static Quaternion HandRotation(float x, float y, float z) => RuntimeHands.Compose(x, y, z);

        internal void BindConfig()
        {
            RuntimeHands.BindHandEntries(bodyName, isLeftHand,
                out rotX, out rotY, out rotZ, out posX, out posY, out posZ, out scale,
                out aimRotX, out aimRotY, out aimRotZ);

            string side = isLeftHand ? "Left" : "Right";
            handRot = RuntimeHands.Rot(bodyName, side + "Rot", rotX, rotY, rotZ, RuntimeHands.BakedHandRotation(bodyName, isLeftHand));
            aimRot = RuntimeHands.Rot(bodyName, "AimRot", aimRotX, aimRotY, aimRotZ, RuntimeHands.BakedAimRotation(bodyName));
            bakedHand = RuntimeHands.BakedHandRotation(bodyName, isLeftHand);
            if (ModConfig.HandTuningEnabled || ModConfig.RuntimeHandsDebug.Value) VRMod.StaticLogger.LogInfo($"[VR hands] '{bodyName}' {side.ToUpperInvariant()} hand offsets: Rot({rotX.Value:F0}, {rotY.Value:F0}, {rotZ.Value:F0}) Pos({posX.Value:F2}, {posY.Value:F2}, {posZ.Value:F2}) Scale {scale.Value:F2} | Aim({aimRotX.Value:F0}, {aimRotY.Value:F0}, {aimRotZ.Value:F0}, shared by both hands). Edit '{bodyName}_{side}*' in the config to tune this hand.");

            forearmAtBuild = RuntimeHands.BodyIncludesForearm(bodyName);

            weaponRoot = transform.Find("Mesh/WeaponRoot");
            if (!weaponRoot)
                VRMod.StaticLogger.LogInfo($"[VR hands] '{bodyName}' {side.ToUpperInvariant()} hand has NO held weapon part - '{bodyName}_{RuntimeHands.WeaponKeyPrefix(bodyName, isLeftHand)}*' has nothing to move on this hand. Turn on RuntimeHandsDebug to see how each mesh was classified.");
            if (weaponRoot)
            {
                weaponSpace = new GameObject("WeaponSpace").transform;
                weaponSpace.SetParent(transform.parent, false);
                weaponRoot.SetParent(weaponSpace, false);
                weapon = RuntimeHands.BindWeaponEntries(bodyName, isLeftHand);
                weaponRot = RuntimeHands.Rot(bodyName, RuntimeHands.WeaponKeyPrefix(bodyName, isLeftHand) + "Rot", weapon[3], weapon[4], weapon[5], Quaternion.identity);
                if (bodyName == "Enforcer2Body")
                {
                    shieldRenderers = new List<MeshRenderer>();
                    allShieldRenderers.RemoveAll(r => !r);
                    var names = new System.Text.StringBuilder();
                    foreach (MeshRenderer r in weaponSpace.GetComponentsInChildren<MeshRenderer>(true))
                    {
                        names.Append(r.name).Append(' ');
                        if (r.name.ToLowerInvariant().Contains("shield")) { shieldRenderers.Add(r); allShieldRenderers.Add(r); }
                    }
                    VRMod.StaticLogger.LogInfo($"[VR hands] Enforcer {side.ToUpperInvariant()} held parts: {names}- {shieldRenderers.Count} shield part(s) shown only while shielding.");
                }
                if (ModConfig.HandTuningEnabled || ModConfig.RuntimeHandsDebug.Value) VRMod.StaticLogger.LogInfo($"[VR hands] '{bodyName}' {side.ToUpperInvariant()} hand carries a weapon part: offsets Pos({weapon[0].Value:F2}, {weapon[1].Value:F2}, {weapon[2].Value:F2}) Rot({weapon[3].Value:F0}, {weapon[4].Value:F0}, {weapon[5].Value:F0}). Edit '{bodyName}_{RuntimeHands.WeaponKeyPrefix(bodyName, isLeftHand)}*' in the config (edits apply within 5 s while in game).");
            }
        }

        /// <summary>
        /// Forces the baked renderers back into a drawable state and, in debug mode, reports what
        /// they looked like. Runtime hands have been seen invisible after loading a save even
        /// though the build succeeded, so this both repairs and diagnoses.
        /// </summary>
        private void EnsureVisible()
        {
            HandController owner = GetComponentInParent<HandController>();
            bool inUi = owner && owner.uiMode;

            MeshRenderer[] renderers = (transform.parent ? transform.parent : transform).GetComponentsInChildren<MeshRenderer>(true); // 2.23.34: includes WeaponSpace
            int repaired = 0;

            foreach (MeshRenderer r in renderers)
            {
                if (!r) continue;

                // A part whose SOURCE renderer the game has hidden (the Operator's drone once it
                // is launched from the claw) is meant to be hidden - leave it alone.
                RuntimeHandPartMirror mirror = r.GetComponent<RuntimeHandPartMirror>();
                if (mirror && mirror.sourceHidden) continue;
                if (shieldRenderers != null && shieldRenderers.Contains(r)) continue; // shown only while shielding

                if (!r.enabled) { r.enabled = true; repaired++; }
                if (!r.gameObject.activeSelf) { r.gameObject.SetActive(true); repaired++; }

                // Outside UI mode nothing of ours belongs on the no-draw layer.
                if (!inUi && r.gameObject.layer == LayerIndex.noDraw.intVal)
                {
                    r.gameObject.layer = 0;
                    repaired++;
                }
            }

            if (repaired > 0)
            {
                VRMod.StaticLogger.LogInfo($"[VR hands] Restored visibility on {repaired} renderer state(s) for the {(isLeftHand ? "left" : "right")} '{bodyName}' hand.");
            }

            if (ModConfig.RuntimeHandsDebug.Value && Time.unscaledTime >= nextRenderReport)
            {
                nextRenderReport = Time.unscaledTime + 5f;

                if (renderers.Length == 0)
                {
                    VRMod.StaticLogger.LogInfo($"[VR debug] {(isLeftHand ? "LEFT" : "RIGHT")} '{bodyName}' hand has NO renderers.");
                }
                else
                {
                    MeshRenderer r = renderers[0];
                    string mat = r.sharedMaterial ? r.sharedMaterial.name : "NULL";
                    VRMod.StaticLogger.LogInfo($"[VR debug] {(isLeftHand ? "LEFT" : "RIGHT")} '{bodyName}' hand: {renderers.Length} renderer(s), first enabled={r.enabled}, active={r.gameObject.activeInHierarchy}, layer={r.gameObject.layer}, uiMode={inUi}, material='{mat}', visible={r.isVisible}, worldScale={transform.lossyScale}.");
                }
            }
        }

        private void LateUpdate()
        {
            // 2.23.43: also apply here in case the character model's material pass doesn't run.
            if (shieldRenderers != null && shieldRenderers.Count > 0) ApplyShieldOpacity();
        }

        private void Update()
        {
            if (rotX == null) return;

            // The HandController applies its SteamVR-era PoseCorrection to our root, but this mesh
            // was baked from the game model, not authored for that pose - cancel it, then apply
            // this hand's own offsets. Left and right are tuned entirely independently; nothing is
            // mirrored from the other hand.
            Quaternion cancel = Quaternion.Inverse(HandController.PoseCorrection);

            transform.localRotation = cancel * handRot.Value * autoAlign;
            // 2.23.36: position offsets move along the gizmo's red/green/blue lines (cancel frame).
            transform.localPosition = cancel * new Vector3(posX.Value, posY.Value, posZ.Value);
            if (axisGizmo)
            {
                // Zero-pose frame: X turns about this red line from the stored pose.
                axisGizmo.localRotation = cancel * handRot.Base;
                axisGizmo.localPosition = transform.localPosition;
            }

            // The left HandController root carries a negative X scale (it exists to mirror the
            // authored right-arm hand models). A hand baked from the left arm is already correct,
            // so that mirror is cancelled here on the MESH only - the aim root is left inside the
            // mirrored space so the shared aim values behave identically on both hands.
            float s = Mathf.Clamp(scale.Value, 0.05f, 5f);
            transform.localScale = new Vector3(mirrorMesh ? -s : s, s, s);

            if (aimRoot)
            {
                aimRoot.localRotation = cancel * aimRot.Value;
            }

            if (shieldRenderers != null && shieldRenderers.Count > 0)
            {
                // 2.23.41: "Enforcer: Shield opacity" (VR settings) - 0 hides it completely.
                bool show = IsShielding(MotionControls.currentBody) && ModConfig.EnforcerShieldOpacity.Value > 0;
                foreach (MeshRenderer r in shieldRenderers) if (r && r.enabled != show) r.enabled = show;
            }

            if (weaponSpace)
            {
                float ds = Mathf.Clamp((float)scale.DefaultValue, 0.05f, 5f);
                weaponSpace.localRotation = cancel * bakedHand * autoAlign;
                weaponSpace.localPosition = cancel * new Vector3((float)posX.DefaultValue, (float)posY.DefaultValue, (float)posZ.DefaultValue);
                weaponSpace.localScale = new Vector3(mirrorMesh ? -ds : ds, ds, ds);
            }

            if (weaponRoot && weapon != null)
            {
                weaponRoot.localPosition = new Vector3(weapon[0].Value, weapon[1].Value, weapon[2].Value);
                weaponRoot.localRotation = weaponRot != null ? weaponRot.Value : RuntimeHands.Compose(weapon[3].Value, weapon[4].Value, weapon[5].Value);
                if (weaponGizmo)
                {
                    // 2.23.38: drawn AT the weapon's pivot (it was at the hand, up to a metre away).
                    weaponGizmo.localPosition = weaponRoot.localPosition;
                    weaponGizmo.localRotation = weaponRot != null ? weaponRot.Base : Quaternion.identity;
                }
            }

            // Live tuning: pick up edits to the config file every 5 s (2.23.33, was 3 s) (always, not just in debug
            // mode) so aim / hand / weapon offsets can be adjusted without restarting the game.
            if (Time.unscaledTime >= nextReload)
            {
                nextReload = Time.unscaledTime + 5f;
                if (ModConfig.ReloadRuntimeHandConfig())
                {
                    RuntimeHands.ProcessZeroPose(bodyName);
                    if (ModConfig.HandTuningEnabled || ModConfig.RuntimeHandsDebug.Value) VRMod.StaticLogger.LogInfo($"[VR hands] Config reloaded: '{bodyName}' {(isLeftHand ? "LEFT" : "RIGHT")} Rot({rotX.Value:F0}, {rotY.Value:F0}, {rotZ.Value:F0}) Pos({posX.Value:F2}, {posY.Value:F2}, {posZ.Value:F2}) Aim({aimRotX.Value:F0}, {aimRotY.Value:F0}, {aimRotZ.Value:F0}){(weapon != null ? $" Weapon Pos({weapon[0].Value:F2}, {weapon[1].Value:F2}, {weapon[2].Value:F2}) Rot({weapon[3].Value:F0}, {weapon[4].Value:F0}, {weapon[5].Value:F0})" : "")}.");
                }
            }

            // Watchdog: a stage change (e.g. entering the Bazaar) destroys the character model
            // this mesh was baked from, leaving an empty hand with item displays attached to it.
            // Detect that - by model identity or by the renderers having gone - and re-bake.
            if (!rebuildRequested && Time.unscaledTime >= nextWatchdog)
            {
                nextWatchdog = Time.unscaledTime + 1f;

                CharacterBody liveBody = MotionControls.currentBody;
                Transform liveModel = liveBody && liveBody.modelLocator ? liveBody.modelLocator.modelTransform : null;

                bool modelChanged = liveModel && liveModel.GetInstanceID() != builtModelId;
                bool meshGone = GetComponentsInChildren<MeshRenderer>(true).Length == 0;

                // Watch the INVENTORY rather than displays on one bone: an item may hang off the
                // forearm or chest, and we want the hand re-baked the moment anything is picked up.
                bool itemsChanged = liveBody && RuntimeHands.InventorySum(liveBody) != inventorySumAtBuild
                                    && Time.unscaledTime >= inventoryRebakeNotBefore;

                if ((modelChanged || meshGone || itemsChanged) && liveBody && liveBody.name.StartsWith(bodyName))
                {
                    rebuildRequested = true;
                    string why = modelChanged ? "character model was rebuilt"
                        : meshGone ? "mesh renderers are gone"
                        : "inventory changed";
                    VRMod.StaticLogger.LogInfo($"[VR hands] Runtime hand for '{bodyName}' needs re-baking ({why}).");
                    MotionControls.SetHandPair(liveBody);
                    return;
                }

                // The hand exists but may have been left hidden - most often parked on the noDraw
                // layer by a UI transition that never handed it back. Put it right, and in debug
                // mode report exactly what state the renderers are in.
                EnsureVisible();
            }

            // Mesh-affecting toggles need a re-bake: when "Include forearm" changes, rebuild the
            // hand pair once so the change is visible without a restart.
            if (!rebuildRequested && RuntimeHands.BodyIncludesForearm(bodyName) != forearmAtBuild)
            {
                CharacterBody body = MotionControls.currentBody;
                if (body && body.name.StartsWith(bodyName))
                {
                    rebuildRequested = true;
                    VRMod.StaticLogger.LogInfo("[VR hands] 'Include forearm' changed - rebaking runtime hands.");
                    MotionControls.SetHandPair(body);
                }
            }
        }
    }

    /// <summary>
    /// Marks a baked part that came from an item display, and keeps it scaled to the configured
    /// factor. The part's authored size is captured once, so the factor is never compounded.
    /// </summary>
    internal class RuntimeHandItemPart : MonoBehaviour
    {
        private Vector3 authoredScale;
        private bool captured;

        private void Update()
        {
            if (!captured)
            {
                authoredScale = transform.localScale;
                captured = true;
            }

            float factor = Mathf.Clamp(ModConfig.RuntimeHandsItemScale.Value, 0.05f, 3f);
            Vector3 wanted = authoredScale * factor;

            if ((transform.localScale - wanted).sqrMagnitude > 1e-10f)
                transform.localScale = wanted;
        }
    }

    /// <summary>
    /// Retries the runtime hand build until the model's meshes are loaded (skins apply async),
    /// then reapplies the hand pair so muzzles and aim origin are wired properly. Gives up after
    /// maxAttempts. Destroys itself as soon as the controller has a hand for this body.
    /// </summary>
    internal class RuntimeHandRetry : MonoBehaviour
    {
        internal HandController controller;
        internal string bodyName;

        private float nextTry;
        private int attempts;
        private const int maxAttempts = 60;

        private void Update()
        {
            if (!controller || string.IsNullOrEmpty(bodyName)) { Destroy(this); return; }

            // Another path (or the opposite hand's retry) already gave us a hand for this body.
            if (controller.currentHand && controller.currentHand.bodyName == bodyName) { Destroy(this); return; }

            // Body changed - this retry is stale.
            CharacterBody body = MotionControls.currentBody;
            if (!body || !body.name.StartsWith(bodyName)) { Destroy(this); return; }

            if (Time.unscaledTime < nextTry) return;
            nextTry = Time.unscaledTime + 0.5f;   // up to 30s - loading a save can be slow
            attempts++;

            Hand hand = RuntimeHands.TryBuildHand(controller, bodyName);

            if (hand)
            {
                // Destroy the probe hand and reapply the whole pair so both controllers rebuild
                // and the muzzles/aim origin are registered exactly like the normal path.
                Destroy(hand.gameObject);
                VRMod.StaticLogger.LogInfo($"[VR hands] Model meshes ready after {attempts} attempt(s) - reapplying hand pair for '{bodyName}'.");
                MotionControls.SetHandPair(body);
                Destroy(this);
                return;
            }

            if (attempts >= maxAttempts)
            {
                VRMod.StaticLogger.LogWarning($"[VR hands] Runtime hand retry gave up after {maxAttempts} attempts for '{bodyName}'.");
                Destroy(this);
            }
        }
    }
}
