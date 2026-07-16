using System;
using System.Collections.Generic;
using ProjectHunt.Data;
using UnityEngine;

namespace ProjectHunt.Battle
{
    public static class BattleSfx
    {
        private enum Sound
        {
            Slash,
            Bow,
            Throw,
            HammerSwing,
            Magic,
            EnemyAttack,
            Hit,
            HeavyHit,
            Explosion,
            Beam,
            Block,
            Death,
            DropLand,
            Claim,
            Merge,
            Reveal,
        }

        private static readonly Dictionary<Sound, AudioClip> Clips = new Dictionary<Sound, AudioClip>();
        private static readonly Dictionary<Sound, string> PackagedClipNames = new Dictionary<Sound, string>
        {
            { Sound.Slash, "snd_unit_attack_blade_big_1" },
            { Sound.Bow, "snd_unit_attack_arrow_1" },
            { Sound.Throw, "snd_unit_attack_throw_1" },
            { Sound.HammerSwing, "snd_unit_goblin_boss_round_hit" },
            { Sound.Magic, "snd_unit_attack_magic_light_1" },
            { Sound.EnemyAttack, "snd_unit_attack_blade_big_1" },
            { Sound.Hit, "snd_unit_attack_throw_1_impact" },
            { Sound.HeavyHit, "snd_unit_goblin_boss_ground_hit" },
            { Sound.Explosion, "snd_unit_goblin_boss_ground_hit" },
            { Sound.Beam, "snd_unit_attack_magic_light_1" },
            { Sound.Death, "snd_unit_death_goblin_1" },
            { Sound.DropLand, "snd_gaze_drop" },
            { Sound.Claim, "snd_rewards_take_artifact" },
            { Sound.Merge, "snd_artifacts_open" },
            { Sound.Reveal, "snd_artifacts_open" },
        };
        private const string ResourceRoot = "Audio/ProjectHunt/";
        private static AudioSource _source;

        public static void PlayAttack(CombatUnitController attacker, int impactIndex = 0)
        {
            if (attacker == null)
            {
                return;
            }

            if (attacker.team != CombatUnitController.TeamType.Player || attacker.characterConfig == null)
            {
                var enemyResourceId = attacker.bossConfig != null ? attacker.bossConfig.resourceId : string.Empty;
                if (enemyResourceId.Contains("goblin_boss_wife"))
                {
                    PlayNamed("snd_unit_goblin_boss_ground_hit", 0.72f, 1f);
                }
                else if (enemyResourceId.Contains("lich"))
                {
                    PlayNamed("snd_unit_attack_magic_light_1", 0.68f, 0.95f);
                }
                else
                {
                    Play(Sound.EnemyAttack, 0.6f, 0.94f);
                }
                return;
            }

            var config = attacker.characterConfig;
            if (config.resourceId == "catapult")
            {
                PlayNamed("snd_unit_ballista_shot_1", 0.68f, 1f);
            }
            else if (config.id == "mage_none")
            {
                Play(Sound.Magic, 0.75f, 1f);
            }
            else if (config.roleType == RoleType.Archer)
            {
                Play(config.id.EndsWith("_hammer") ? Sound.Throw : Sound.Bow, 0.72f, 1f);
            }
            else if (config.roleType == RoleType.Assassin)
            {
                Play(config.id.EndsWith("_hammer") && impactIndex > 0 ? Sound.HammerSwing : Sound.Throw, 0.65f, 1.05f);
            }
            else if (config.id.EndsWith("_hammer"))
            {
                Play(Sound.HammerSwing, 0.76f, 0.93f);
            }
            else
            {
                Play(Sound.Slash, 0.68f, 1f);
            }
        }

        public static void PlayProjectileLaunch(bool isHeavy)
        {
            Play(isHeavy ? Sound.HammerSwing : Sound.Throw, isHeavy ? 0.48f : 0.34f, 1.08f);
        }

        public static void PlayImpact(bool isArea = false, bool isHeavy = false, float volumeScale = 1f)
        {
            var volume = (isArea ? 0.85f : 0.65f) * Mathf.Clamp01(volumeScale);
            Play(isArea ? Sound.Explosion : isHeavy ? Sound.HeavyHit : Sound.Hit, volume, 1f);
        }

        public static void PlayBeam()
        {
            Play(Sound.Beam, 0.78f, 1f);
        }

        public static void PlayBlock()
        {
            Play(Sound.Block, 0.75f, 1.05f);
        }

        public static void PlayDeath(bool isBoss)
        {
            if (isBoss)
            {
                PlayNamed("snd_boss_death", 0.9f, 1f);
                return;
            }

            Play(Sound.Death, 0.55f, 1.05f);
        }

        public static void PlayDropLand()
        {
            Play(Sound.DropLand, 0.8f, 0.9f);
        }

        public static void PlayClaim()
        {
            Play(Sound.Claim, 0.82f, 1f);
        }

        public static void PlayMerge()
        {
            Play(Sound.Merge, 0.7f, 1f);
        }

        public static void PlayReveal()
        {
            Play(Sound.Reveal, 0.8f, 1f);
        }

        public static void PlayUiClick(bool important = false)
        {
            PlayNamed(important ? "snd_encounter_option_click" : "snd_button_click", important ? 0.42f : 0.5f, 1f);
        }

        public static void PlayBossIntro(CombatUnitController boss)
        {
            var resourceId = boss != null && boss.bossConfig != null
                ? boss.bossConfig.resourceId
                : string.Empty;
            if (resourceId.Contains("goblin_boss_wife"))
            {
                PlayNamed("snd_boss_intro_goblin_wife", 0.75f, 1f);
            }
            else if (resourceId.Contains("lich"))
            {
                PlayNamed("snd_boss_intro_lich", 0.75f, 1f);
            }
        }

        private static void Play(Sound sound, float volume, float pitch)
        {
            EnsureSource();
            if (_source == null)
            {
                return;
            }

            if (!Clips.TryGetValue(sound, out var clip))
            {
                clip = LoadPackagedClip(sound);
                if (clip == null)
                {
                    clip = CreateClip(sound);
                }
                Clips[sound] = clip;
            }

            _source.pitch = pitch;
            _source.PlayOneShot(clip, Mathf.Clamp01(volume));
        }

        private static void PlayNamed(string clipName, float volume, float pitch)
        {
            EnsureSource();
            if (_source == null)
            {
                return;
            }

            var clip = Resources.Load<AudioClip>(ResourceRoot + clipName);
            if (clip == null)
            {
                return;
            }

            _source.pitch = pitch;
            _source.PlayOneShot(clip, Mathf.Clamp01(volume));
        }

        private static AudioClip LoadPackagedClip(Sound sound)
        {
            return PackagedClipNames.TryGetValue(sound, out var clipName)
                ? Resources.Load<AudioClip>(ResourceRoot + clipName)
                : null;
        }

        private static void EnsureSource()
        {
            if (_source != null)
            {
                return;
            }

            var host = new GameObject("BattleSfx");
            UnityEngine.Object.DontDestroyOnLoad(host);
            _source = host.AddComponent<AudioSource>();
            _source.playOnAwake = false;
            _source.loop = false;
            _source.spatialBlend = 0f;
            _source.volume = 0.36f;
        }

        private static AudioClip CreateClip(Sound sound)
        {
            const int sampleRate = 22050;
            var duration = GetDuration(sound);
            var sampleCount = Mathf.CeilToInt(sampleRate * duration);
            var samples = new float[sampleCount];
            var random = new System.Random(1337 + (int)sound * 97);

            for (var i = 0; i < sampleCount; i++)
            {
                var time = i / (float)sampleRate;
                var t = i / (float)Mathf.Max(1, sampleCount - 1);
                var envelope = Mathf.Pow(1f - t, GetDecay(sound));
                var noise = (float)(random.NextDouble() * 2.0 - 1.0);
                samples[i] = GenerateSample(sound, time, t, noise) * envelope * 0.55f;
            }

            var clip = AudioClip.Create("Sfx_" + sound, sampleCount, 1, sampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        private static float GenerateSample(Sound sound, float time, float t, float noise)
        {
            var twoPi = Mathf.PI * 2f;
            switch (sound)
            {
                case Sound.Slash:
                    return noise * 0.5f + Mathf.Sin(twoPi * Mathf.Lerp(900f, 180f, t) * time) * 0.35f;
                case Sound.Bow:
                    return Mathf.Sin(twoPi * Mathf.Lerp(620f, 170f, t) * time) * 0.8f + noise * 0.18f;
                case Sound.Throw:
                    return Mathf.Sin(twoPi * Mathf.Lerp(520f, 120f, t) * time) * 0.52f + noise * 0.32f;
                case Sound.HammerSwing:
                    return Mathf.Sin(twoPi * Mathf.Lerp(180f, 55f, t) * time) * 0.78f + noise * 0.22f;
                case Sound.Magic:
                    return Mathf.Sin(twoPi * (330f + 520f * t) * time) * 0.55f +
                           Mathf.Sin(twoPi * (660f + 260f * t) * time) * 0.25f;
                case Sound.EnemyAttack:
                    return Mathf.Sin(twoPi * Mathf.Lerp(150f, 70f, t) * time) * 0.65f + noise * 0.25f;
                case Sound.Hit:
                    return noise * 0.62f + Mathf.Sin(twoPi * 120f * time) * 0.38f;
                case Sound.HeavyHit:
                    return noise * 0.45f + Mathf.Sin(twoPi * Mathf.Lerp(95f, 42f, t) * time) * 0.75f;
                case Sound.Explosion:
                    return noise * 0.75f + Mathf.Sin(twoPi * Mathf.Lerp(130f, 38f, t) * time) * 0.55f;
                case Sound.Beam:
                    return Mathf.Sin(twoPi * Mathf.Lerp(1100f, 420f, t) * time) * 0.5f +
                           Mathf.Sin(twoPi * 80f * time) * 0.22f + noise * 0.12f;
                case Sound.Block:
                    return Mathf.Sin(twoPi * 1450f * time) * 0.65f + Mathf.Sin(twoPi * 760f * time) * 0.35f;
                case Sound.Death:
                    return Mathf.Sin(twoPi * Mathf.Lerp(210f, 45f, t) * time) * 0.72f + noise * 0.2f;
                case Sound.DropLand:
                    return Mathf.Sin(twoPi * Mathf.Lerp(110f, 38f, t) * time) * 0.82f + noise * 0.28f;
                case Sound.Claim:
                    return Mathf.Sin(twoPi * (420f + 620f * t) * time) * 0.62f +
                           Mathf.Sin(twoPi * (630f + 820f * t) * time) * 0.25f;
                case Sound.Merge:
                    return Mathf.Sin(twoPi * (240f + 780f * t) * time) * 0.6f + noise * 0.12f;
                case Sound.Reveal:
                    return Mathf.Sin(twoPi * 523.25f * time) * 0.42f +
                           Mathf.Sin(twoPi * 659.25f * time) * 0.34f +
                           Mathf.Sin(twoPi * 783.99f * time) * 0.24f;
                default:
                    return 0f;
            }
        }

        private static float GetDuration(Sound sound)
        {
            switch (sound)
            {
                case Sound.Explosion:
                case Sound.Death:
                    return 0.42f;
                case Sound.Claim:
                case Sound.Merge:
                case Sound.Reveal:
                    return 0.5f;
                case Sound.Beam:
                    return 0.28f;
                default:
                    return 0.18f;
            }
        }

        private static float GetDecay(Sound sound)
        {
            return sound == Sound.Reveal || sound == Sound.Claim ? 0.7f : 1.8f;
        }
    }
}
