using System.Collections.Generic;
using System.Diagnostics;

using Game.Code.Helpers;

using Godot;

namespace Game.Code.Managers
{
    public class SoundManager : Node2D
    {
        private AudioStreamPlayer _musicPlayer = new AudioStreamPlayer();
        private AudioStreamPlayer[] _soundPlayers = new AudioStreamPlayer[4];

        private AudioStreamPlayer _soundQueuePlayer = new AudioStreamPlayer();
        private Queue<AudioStream> _soundQueue = new Queue<AudioStream>();

        public SoundManager()
        {
            Debug.Assert(Instance == null);
            Instance = this;
        }

        // DOESN'T WORK ON HTML BECAUSE OGG ARE NOT PRESENTED AS SEPARATE FILES (PACKED INTO BINARY)
        // USE https://github.com/hhyyrylainen/GodotPckTool FOR INSPECTING PCK FILES

        public static Dictionary<string, AudioStreamOGGVorbis> MusicStreams { get; } = ResourceHelper.LoadResourcesDictionary<AudioStreamOGGVorbis>("res://Assets/Music");
        public static Dictionary<string, AudioStreamOGGVorbis> SoundStreams { get; } = ResourceHelper.LoadResourcesDictionary<AudioStreamOGGVorbis>(new string[] { "res://Assets/Sounds/Tabletop", "res://Assets/Sounds/Judo", "res://Assets/Sounds/UI" });
        public static SoundManager Instance { get; private set; }

        private static readonly string[] SoundPossibleSubPathNames = {"Judo", "Tabletop", "UI"};

        public static AudioStreamOGGVorbis GetThemeStreamByName(string themeName)
        {
            AudioStreamOGGVorbis stream = GD.Load<AudioStreamOGGVorbis>("res://Assets/Music/" + themeName + ".ogg");
            if (stream == null)
            {
                GD.PushError("Not found theme: " + themeName);
                return null;
            }
            return stream;
        }
        
        public static AudioStreamOGGVorbis GetSoundStreamByName(string soundName)
        {
            AudioStreamOGGVorbis stream = null;
            foreach (string possibleSubPathName in SoundPossibleSubPathNames)
            {
                stream = GD.Load<AudioStreamOGGVorbis>("res://Assets/Sounds/" + possibleSubPathName + "/" + soundName + ".ogg");
                if (stream != null)
                    break;
            }
            if (stream == null)
            {
                GD.PushError("Not found sound: " + soundName);
                return null;
            }

            return stream;
        }
        
        public override void _Ready()
        {
            PauseMode = PauseModeEnum.Process;
            AddChild(_musicPlayer);

            AddChild(_soundQueuePlayer);

            for (int i = 0; i < _soundPlayers.Length; i++)
            {
                _soundPlayers[i] = new AudioStreamPlayer();
                AddChild(_soundPlayers[i]);
            }

            _soundQueuePlayer.Connect("finished", this, nameof(OnSoundQueueFinished));
        }

        public void PlayClickButtonSound()
        {
            PlaySound("ClickButton", 2);
        }

        public void PlayClickStartButtonSound()
        {
            PlaySound("ClickStartButton", 2);
        }

        public void PlayMainMenuTheme()
        {
            PlayTheme("Roll_of_the_dice_theme_3_(MainMenu)");
        }

        public void PlayTabletopTheme()
        {
            PlayTheme("Roll_of_the_dice_theme_1_(Tabletop)");
        }

        public void PlayJudoTheme()
        {
            PlayTheme("Roll_of_the_dice_theme_2_(Battle)");
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
            PlaySound("VoiceLose", 1);
        }

        public void PlayJudoVoiceWinSound()
        {
            PlaySound("VoiceWin", 1);
        }

        public void StopSound(int index)
        {
            _soundPlayers[index].Stop();
        }

        public void StopAllSounds()
        {
            for (int i = 0; i < 4; i++)
                StopSound(i);
        }

        private void OnSoundQueueFinished()
        {
            if (_soundQueue.Count == 0)
                return;

            AudioStream stream = _soundQueue.Dequeue();
            _soundQueuePlayer.Stream = stream;
            _soundQueuePlayer.Play();
        }

        private void PlayTheme(string themeName)
        {
            AudioStreamOGGVorbis stream = GetThemeStreamByName(themeName);
            if (stream == null)
                return;

            // DOESN'T WORK ON HTML BECAUSE OGG ARE NOT PRESENTED AS SEPARATE FILES (PACKED INTO BINARY)
            // USE https://github.com/hhyyrylainen/GodotPckTool FOR INSPECTING PCK FILES
            // AudioStream stream = MusicStreams[themeName]; 

            if (stream == _musicPlayer.Stream)
            {
                return;
            }

            _musicPlayer.Stream = stream;
            _musicPlayer.Play();
        }

        private void PlaySound(string soundName, int index = 0)
        {
            AudioStreamOGGVorbis stream = GetSoundStreamByName(soundName);
            if (stream == null)
                return;

            // DOESN'T WORK ON HTML BECAUSE OGG ARE NOT PRESENTED AS SEPARATE FILES (PACKED INTO BINARY)
            // USE https://github.com/hhyyrylainen/GodotPckTool FOR INSPECTING PCK FILES
            // AudioStream stream = SoundStreams[soundName];

            _soundPlayers[index].Stream = stream;
            _soundPlayers[index].Play();
        }

        private void PlayQueuedSound(string soundName)
        {
            AudioStreamOGGVorbis stream = GetSoundStreamByName(soundName);
            if (stream == null)
                return;

            // DOESN'T WORK ON HTML BECAUSE OGG ARE NOT PRESENTED AS SEPARATE FILES (PACKED INTO BINARY)
            // USE https://github.com/hhyyrylainen/GodotPckTool FOR INSPECTING PCK FILES
            // AudioStream stream = SoundStreams[soundName];

            if (!_soundQueuePlayer.Playing)
            {
                _soundQueuePlayer.Stream = stream;
                _soundQueuePlayer.Play();
            }
            else
            {
                _soundQueue.Enqueue(stream);
            }
        }
    }
}