using System;
using System.Linq;
using System.Collections.Generic;
using SDL2;

namespace StyleStar
{
    public class MusicManager
    {
        private IntPtr streamHandle = IntPtr.Zero;
        private const int leftChannel = 1;
        private const int rightChannel = 2;

        public bool IsPlaying { get { return SDL_mixer.Mix_PlayingMusic() == 1; } }
        public bool IsFinished { get; private set; }
        public long SongLengthBytes { get; private set; }
        public double SongLengthSec { get; private set; }
        public long SongCurrentPos { get; private set; }

        private int frequency;
        private int channels;
        private ushort format;

        // Offset in milliseconds
        public double Offset { get; set; }

        public MusicManager()
        {
            int success = SDL_mixer.Mix_Init(SDL_mixer.MIX_InitFlags.MIX_INIT_MP3 | SDL_mixer.MIX_InitFlags.MIX_INIT_MID);
            if (success != -1)
                success = SDL_mixer.Mix_OpenAudio(SDL_mixer.MIX_DEFAULT_FREQUENCY, SDL_mixer.MIX_DEFAULT_FORMAT, 2, 1024);

            if (success == -1)
                throw new Exception("SDL_mixer failed to initialize.");
        }

        ~MusicManager()
        {
            SDL_mixer.Mix_CloseAudio();
            SDL_mixer.Mix_Quit();
        }

        public bool LoadSong(string filename, List<BpmChangeEvent> bpmChanges)
        {
            IsFinished = false;
            streamHandle = SDL_mixer.Mix_LoadMUS(filename);
            Globals.BpmEvents = bpmChanges;
            Globals.BpmEvents = Globals.BpmEvents.OrderBy(x => x.StartBeat).ToList();
            for (int i = 0; i < Globals.BpmEvents.Count; i++)
            {
                if (i > 0)
                {
                    Globals.BpmEvents[i].StartSeconds = (Globals.BpmEvents[i].StartBeat - Globals.BpmEvents[i - 1].StartBeat) / Globals.BpmEvents[i - 1].BPM * 60 + Globals.BpmEvents[i - 1].StartSeconds;
                }
            }

            //SongBpm = bpm;
            // Globals.CurrentBpm = Globals.BpmEvents[0].BPM;

            // create hooks here
            SDL_mixer.Mix_HookMusic(MixDelegate, IntPtr.Zero);
            SDL_mixer.Mix_HookMusicFinished(FinishedDelegate);

            // Use the left channel for grabbing this info for now.
            // Is this correct? I'm not sure.
            IntPtr leftChannelMixChunk = SDL_mixer.Mix_GetChunk(leftChannel);
            SDL_mixer.Mix_QuerySpec(out frequency, out format, out channels);

            unsafe
            {
                byte* pLeftChannel = (byte*)leftChannelMixChunk.ToPointer();
                if (pLeftChannel != null)
                {
                    /*
                        typedef struct Mix_Chunk {
                            int allocated;
                            Uint8 *abuf;
                            Uint32 alen;
                            Uint8 volume;     
                        } Mix_Chunk;
                     * get the value of alen within this struct
                     * refer to https://www.libsdl.org/projects/SDL_mixer/docs/SDL_mixer.html#SEC85 for more details
                     */
                    SongLengthBytes = *(pLeftChannel + sizeof(int) + sizeof(byte));
                    SongLengthSec = GetCurrentSec(SongLengthBytes);
                }
            }
            return streamHandle == IntPtr.Zero ? false : true;
        }

        public void Play()
        {
            SDL_mixer.Mix_PlayMusic(streamHandle, 1);
        }

        public void Pause()
        {
            SDL_mixer.Mix_PauseMusic();
        }

        public void Resume()
        {
            SDL_mixer.Mix_ResumeMusic();
        }

        // Refer to https://www.libsdl.org/projects/SDL_mixer/docs/SDL_mixer.html#SEC60 for more information on this delegate
        private void MixDelegate(IntPtr udata, IntPtr stream, int len)
        {
            unsafe
            {
                // get the current position in the song byte stream
                int* pCurrPos = (int*)udata.ToPointer();
                if (pCurrPos != null)
                    SongCurrentPos = *pCurrPos;
            }
        }

        private void FinishedDelegate() { IsFinished = true; }

        public double GetCurrentSec(long currPosBytes)
        {
            /* bytes / samplesize == sample points */
            long points = (currPosBytes / ((format & 0xFF) / 8));

            /* sample points / channels == sample frames */
            long frames = (points / channels);

            /* sample frames / frequency == play length in seconds */
            return frames / frequency;
        }

        public double GetCurrentBeat()
        {
            if (Globals.BpmEvents == null)
                return -1;

            double sec = GetCurrentSec(SongCurrentPos);
            var evt = Globals.BpmEvents.Where(x => sec >= x.StartSeconds).LastOrDefault();
            if (evt == null)
                evt = Globals.BpmEvents[0];
            return (evt.BPM * (sec - evt.StartSeconds) / 60) + evt.StartBeat;
        }
    }
}
