using System.Collections.Generic;
using System.Diagnostics;

using Game.Code.Helpers;

using Godot;

namespace Game.Code.Managers
{
    public class SoundManager : Node2D
    {
        private AudioStreamPlayer _musicPlayer = new AudioStreamPlayer();
        private AudioStreamPlayer _soundPlayer = new AudioStreamPlayer();

        public SoundManager()
        {
            Debug.Assert(Instance == null);
            Instance = this;
        }

        public static Dictionary<string, AudioStreamOGGVorbis> MusicStreams { get; } = ResourceHelper.LoadResourcesDictionary<AudioStreamOGGVorbis>("res://Assets/Music");
        public static Dictionary<string, AudioStreamOGGVorbis> SoundStreams { get; } = ResourceHelper.LoadResourcesDictionary<AudioStreamOGGVorbis>(new string[] { "res://Assets/Sounds/Tabletop", "res://Assets/Sounds/Judo" });
        public static SoundManager Instance { get; private set; }

        public override void _Ready()
        {
            PauseMode = PauseModeEnum.Process;
            AddChild(_musicPlayer);
            AddChild(_soundPlayer);
        }

        public void PlayMainTheme()
        {
            PlayTheme("MainMenuTheme");
        }

        public void PlayTabletopTheme()
        {
            PlayTheme("TabletopTheme");
        }

        public void PlayJudoTheme()
        {
            PlayTheme("JudoTheme");
        }

        public void PlayTabletopVictorySound()
        {
            PlaySound("TabletopVictory");
        }

        public void PlayTabletopPawnPlacedSound()
        {
            PlaySound("PawnPlaced");
        }

        public void PlayJudoFightThrowSound()
        {
            PlaySound("FightHit");
        }

        public void PlayJudoFightFlightSound()
        {
            PlaySound("FightFlight");
        }

        public void PlayJudoLoseSound()
        {
            PlaySound("JudoLose");
        }

        public void PlayJudoWinSound()
        {
            PlaySound("JudoWin");
        }

        public void PlayJudoStartMatchSound()
        {
            PlaySound("StartMatch");
        }

        public void PlayJudoVoiceFightSound()
        {
            PlaySound("VoiceFight");
        }

        public void PlayJudoVoiceLoseSound()
        {
            PlaySound("VoiceLose");
        }

        public void PlayJudoVoiceWinSound()
        {
            PlaySound("VoiceWin");
        }

        private void PlayTheme(string themeName)
        {
            AudioStream stream = MusicStreams[themeName];

            if (stream == _musicPlayer.Stream)
            {
                return;
            }

            _musicPlayer.Stream = stream;
            _musicPlayer.Play();
        }

        private void PlaySound(string soundName)
        {
            AudioStream stream = SoundStreams[soundName];
            _soundPlayer.Stream = stream;
            _soundPlayer.Play();
        }
    }
}
