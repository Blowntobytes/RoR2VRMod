using System.Collections.Generic;
using RoR2;
using RoR2.Audio;
using UnityEngine;

namespace VRMod
{
    /// <summary>
    /// VR option "Reduce sounds under heavy load" (2.26.0, off by default).
    ///
    /// Big fights start the same sound event many times in the same instant (every proc,
    /// impact and hit plays its own copy), which stacks into a wall of noise. Every
    /// one-shot sound the game starts goes through one of:
    ///  * RoR2.Util.PlaySound(string, GameObject) - the other PlaySound / PlayAttackSpeedSound
    ///    overloads call this one;
    ///  * RoR2.Audio.PointSoundManager.EmitSoundLocal(AkEventIdArg / string, Vector3) - sounds
    ///    played at a point, including effect spawn/impact sounds and networked sound events.
    /// Only active while at least ActivationRate sound starts were requested in the last second (2.26.2).
    /// With the option on, the same event may start at most MaxPerWindow times per Window
    /// seconds and all limited sounds together at most MaxTotalPerWindow per TotalWindow; extra copies are not started (returns 0 = no playing ID). For Util.PlaySound only
    /// events named "Play..." are limited, so Stop/Set events are never dropped and looping
    /// sounds always get stopped. Local only - other players hear their own game as normal.
    /// </summary>
    internal static class SoundBudget
    {
        // 2.26.1 (user: still too many overlapping sounds in very heavy fights):
        // was 2 per 0.1 s per event with no overall cap.
        private const int MaxPerWindow = 1;
        private const float Window = 0.15f;
        private const int MaxTotalPerWindow = 10;   // all limited sounds together
        private const float TotalWindow = 0.1f;
        private static readonly Queue<float> allStarts = new Queue<float>(MaxTotalPerWindow + 1);
        // 2.26.2 (user: should only kick in when things get insane, not on early stages):
        // limits apply only while at least ActivationRate sound starts were requested in the
        // last second. 2.26.1 log: stage 1 asked for ~10-30 starts/s (and still lost 30-50% of
        // them to the per-sound limit), very heavy fights 300-3700/s.
        private const int ActivationRate = 100;
        private static readonly Queue<float> requests = new Queue<float>(256);
        private static readonly Dictionary<string, int> skippedNames = new Dictionary<string, int>();
        // 2.26.6 (user: hit/crit clicks should stay unless they reach an extreme rate):
        // these skip the normal limits and are only capped at HitSoundMaxPerSecond each.
        private static readonly HashSet<string> HitFeedbackSounds = new HashSet<string>(System.StringComparer.OrdinalIgnoreCase)
        {
            "Play_UI_hit", "Play_UI_crit",
            "Play_UI_coin" // 2.26.11: gold pickup (user: "being destroyed" by the limiter)
        };
        private const int HitSoundMaxPerSecond = 20;
        private const float ReportInterval = 10f;

        private static readonly Dictionary<string, Queue<float>> byName = new Dictionary<string, Queue<float>>();
        private static readonly Dictionary<uint, Queue<float>> byId = new Dictionary<uint, Queue<float>>();
        private static int skipped, played;
        private static float nextReport;

        internal static void Init()
        {
            On.RoR2.Util.PlaySound_string_GameObject += (orig, soundString, gameObject) =>
            {
                if (ModConfig.ReduceSoundsUnderLoad.Value && !string.IsNullOrEmpty(soundString)
                    && soundString.StartsWith("Play", System.StringComparison.OrdinalIgnoreCase)
                    && !Allow(byName, soundString)) return 0u;
                return orig(soundString, gameObject);
            };
            On.RoR2.Audio.PointSoundManager.EmitSoundLocal_string_Vector3 += (orig, soundString, position) =>
            {
                if (ModConfig.ReduceSoundsUnderLoad.Value && !string.IsNullOrEmpty(soundString)
                    && !Allow(byName, soundString)) return 0u;
                return orig(soundString, position);
            };
            On.RoR2.Audio.PointSoundManager.EmitSoundLocal_AkEventIdArg_Vector3 += (orig, akEventId, position) =>
            {
                if (ModConfig.ReduceSoundsUnderLoad.Value && akEventId.id != 0
                    && !Allow(byId, akEventId.id)) return 0u;
                return orig(akEventId, position);
            };
        }

        private static bool Allow<T>(Dictionary<T, Queue<float>> table, T key)
        {
            float now = Time.unscaledTime;
            if (key is string hitName && HitFeedbackSounds.Contains(hitName))
                return AllowHitSound(table, key, hitName, now);
            requests.Enqueue(now);
            while (requests.Count > 0 && now - requests.Peek() >= 1f) requests.Dequeue();
            if (requests.Count < ActivationRate)
            {
                played++;
                Report(now);
                return true;
            }
            if (!table.TryGetValue(key, out Queue<float> starts))
            {
                starts = new Queue<float>(MaxPerWindow + 1);
                table[key] = starts;
            }
            while (starts.Count > 0 && now - starts.Peek() >= Window) starts.Dequeue();
            while (allStarts.Count > 0 && now - allStarts.Peek() >= TotalWindow) allStarts.Dequeue();
            bool allow = starts.Count < MaxPerWindow && allStarts.Count < MaxTotalPerWindow;
            if (allow) { starts.Enqueue(now); allStarts.Enqueue(now); played++; }
            else
            {
                skipped++;
                string name = key as string ?? ("id " + key);
                skippedNames.TryGetValue(name, out int n);
                skippedNames[name] = n + 1;
            }
            Report(now);
            return allow;
        }

        private static bool AllowHitSound<T>(Dictionary<T, Queue<float>> table, T key, string name, float now)
        {
            if (!table.TryGetValue(key, out Queue<float> starts))
            {
                starts = new Queue<float>(HitSoundMaxPerSecond + 1);
                table[key] = starts;
            }
            while (starts.Count > 0 && now - starts.Peek() >= 1f) starts.Dequeue();
            bool allow = starts.Count < HitSoundMaxPerSecond;
            if (allow) { starts.Enqueue(now); played++; }
            else
            {
                skipped++;
                skippedNames.TryGetValue(name, out int n);
                skippedNames[name] = n + 1;
            }
            Report(now);
            return allow;
        }

        // Sounds started by numeric Wwise ID (networked sound events sent by the host, and
        // effect sounds) log as "id N"; show the event name when the game's network sound
        // catalog knows it.
        private static string DisplayName(string key)
        {
            if (!key.StartsWith("id ")) return key;
            try
            {
                if (uint.TryParse(key.Substring(3), out uint id))
                {
                    string n = RoR2.Audio.NetworkSoundEventCatalog.GetEventNameFromId(id);
                    if (!string.IsNullOrEmpty(n)) return n + " (networked)";
                }
            }
            catch { }
            return key;
        }

        private static void Report(float now)
        {
            if (now < nextReport) return;
            if (skipped > 0)
            {
                var top = new List<KeyValuePair<string, int>>(skippedNames);
                top.Sort((a, b) => b.Value.CompareTo(a.Value));
                var sb = new System.Text.StringBuilder();
                for (int i = 0; i < top.Count && i < 5; i++) sb.Append(i == 0 ? "" : ", ").Append(DisplayName(top[i].Key)).Append(" x").Append(top[i].Value);
                VRMod.StaticLogger.LogInfo($"[VR sound] last {ReportInterval:0}s: {skipped} sound(s) skipped by 'Reduce sounds under heavy load', {played} played; most skipped: {sb}.");
            }
            skippedNames.Clear();
            if (byName.Count > 2000) byName.Clear();
            if (byId.Count > 2000) byId.Clear();
            skipped = played = 0;
            nextReport = now + ReportInterval;
        }
    }
}
