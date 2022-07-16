using System.Collections.Generic;
using System.Diagnostics;

using Game.Code.Helpers;

using Godot;

namespace Game.Code.Managers
{
    public class SoundManager : Node2D
    {
        private AudioStreamPlayer _musicPlayer = new AudioStreamPlayer();

        public SoundManager()
        {
            Debug.Assert(Instance == null);
            Instance = this;
        }

        public static Dictionary<string, AudioStreamOGGVorbis> MusicStreams { get; } = ResourceHelper.LoadResourcesDictionary<AudioStreamOGGVorbis>("res://Assets/Music");
        public static SoundManager Instance { get; private set; }

        public override void _Ready()
        {
            PauseMode = PauseModeEnum.Process;
            AddChild(_musicPlayer);
        }

        public void PlayMainTheme()
        {
            var stream = MusicStreams["Roll_of_the_dice_theme_1"];

            if (stream == _musicPlayer.Stream)
            {
                return;
            }

            _musicPlayer.Stream = stream;
            _musicPlayer.Play();
        }

        public void PlayBattleTheme()
        {
            var stream = MusicStreams["Roll_of_the_dice_theme_2"];

            if (stream == _musicPlayer.Stream)
            {
                return;
            }

            _musicPlayer.Stream = stream;
            _musicPlayer.Play();
        }
    }
}