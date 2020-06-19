namespace SoundFingerprinting.DuplicatesDetector.Infrastructure
{
    using System.Collections.Generic;
    using System.IO;

    using SoundFingerprinting.Audio;
    using SoundFingerprinting.DAO.Data;
    using SoundFingerprinting.Data;

    public class TrackHelper
    {
        private readonly IAudioService audioService;

        public TrackHelper(IAudioService audioService)
        {
            this.audioService = audioService;
        }

        public AudioSamples GetTrackSamples(TrackInfo track, int sampleRate, int secondsToRead, int startAtSecond)
        {
            string filePath = track.MetaFields["FilePath"];
            if (track == null || filePath == null)
            {
                return null;
            }

            return audioService.ReadMonoSamplesFromFile(filePath, sampleRate, secondsToRead, startAtSecond);
        }

        public TrackInfo GetTrack(int mintracklen, int maxtracklen, string filename)
        {
            string artist, title, isrc;
            /*The song does not contain any tags*/
            artist = "Unknown Artist";
            title = "Unknown Title";
            isrc = Path.GetFileNameWithoutExtension(filename);
            var meta = new Dictionary<string, string>();
            meta["FilePath"] = Path.GetFullPath(filename);

            double duration = audioService.GetLengthInSeconds(Path.GetFullPath(filename));
            /*check the duration of a music file*/
            if (duration < mintracklen || duration > maxtracklen)
            {
                return null;
            }

            return new TrackInfo(isrc, title, artist, meta, MediaType.Audio);
        }
    }
}