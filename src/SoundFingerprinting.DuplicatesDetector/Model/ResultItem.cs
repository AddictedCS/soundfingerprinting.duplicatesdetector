namespace SoundFingerprinting.DuplicatesDetector.Model
{
    using SoundFingerprinting.DAO.Data;

    public class ResultItem
    {
        private readonly TrackData track;

        public ResultItem(int setId, TrackData track)
        {
            SetId = setId;
            this.track = track;
        }

        public int SetId { get; private set; }

        public string Title
        {
            get { return track.Title; }
        }

        public string Artist
        {
            get { return track.Artist; }
        }

        public string FileName
        {
            get { return System.IO.Path.GetFileName(track.MetaFields["FilePath"]); }
        }

        public string Path
        {
            get { return track.MetaFields["FilePath"]; }
        }
        
        public double TrackLength
        {
            get { return track.Length; }
        }
    }
}