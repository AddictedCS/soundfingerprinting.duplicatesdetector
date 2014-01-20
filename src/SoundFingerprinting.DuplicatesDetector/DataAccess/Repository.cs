﻿namespace SoundFingerprinting.DuplicatesDetector.DataAccess
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    using SoundFingerprinting.Builder;
    using SoundFingerprinting.Configuration;
    using SoundFingerprinting.Dao;
    using SoundFingerprinting.Data;
    using SoundFingerprinting.DuplicatesDetector.Model;
    using SoundFingerprinting.Strides;

    /// <summary>
    ///   Singleton class for repository container
    /// </summary>
    public class Repository
    {
        private readonly IModelService modelService;

        private readonly IFingerprintCommandBuilder fingerprintCommandBuilder;

        private readonly IQueryFingerprintService queryFingerprintService;

        public Repository(IModelService modelService, IFingerprintCommandBuilder fingerprintCommandBuilder, IQueryFingerprintService queryFingerprintService)
        {
            this.modelService = modelService;
            this.fingerprintCommandBuilder = fingerprintCommandBuilder;
            this.queryFingerprintService = queryFingerprintService;
        }

        /// <summary>
        ///   Create fingerprints out of down sampled samples
        /// </summary>
        /// <param name = "samples">Down sampled to 5512 samples</param>
        /// <param name = "track">Track</param>
        /// <param name = "stride">Stride</param>
        /// <param name = "hashTables">Number of hash tables</param>
        /// <param name = "hashKeys">Number of hash keys</param>
        public void CreateInsertFingerprints(float[] samples, Track track, IStride stride, int hashTables, int hashKeys)
        {
            if (track == null)
            {
                return; /*track is not eligible*/
            }

            var trackReference = modelService.InsertTrack(track);
           
            /*Create fingerprints that will be used as initial fingerprints to be queried*/
            var hashes = fingerprintCommandBuilder.BuildFingerprintCommand()
                                                       .From(samples)
                                                       .WithFingerprintConfig(config => config.Stride = stride)
                                                       .Hash()
                                                       .Result;
           
            modelService.InsertHashDataForTrack(hashes, trackReference);
        }

        /// <summary>
        ///   Find duplicates between existing tracks in the database
        /// </summary>
        /// <param name = "tracks">Tracks to be processed (this list should contain only tracks that have been inserted previously)</param>
        /// <param name = "threshold">Number of threshold tables</param>
        /// <param name = "numberOfFingerprintThreshold">Number of fingerprints threshold</param>
        /// <param name = "callback">Callback invoked at each processed track</param>
        /// <returns>Sets of duplicates</returns>
        public HashSet<Track>[] FindDuplicates(IList<TrackData> tracks, int threshold, int numberOfFingerprintThreshold, Action<Track, int, int> callback)
        {
            List<HashSet<Track>> duplicates = new List<HashSet<Track>>();
            int total = tracks.Count, current = 0;
            var queryConfiguration = new QueryConfiguration(threshold, -1);
            foreach (var track in tracks)
            {
                HashSet<Track> trackDuplicates = new HashSet<Track>();

                var hashes = modelService.ReadHashDataByTrack(track.TrackReference);
                var result = queryFingerprintService.Query(hashes, queryConfiguration);

                if (result.IsSuccessful)
                {
                    foreach (var resultEntry in result.ResultEntries)
                    {
                        if (track.Equals(resultEntry.Track))
                        {
                            continue;
                        }

                        if (numberOfFingerprintThreshold > resultEntry.Similarity)
                        {
                            continue;
                        }

                        trackDuplicates.Add((Track)resultEntry.Track);
                    }

                    if (trackDuplicates.Any())
                    {
                        HashSet<Track> duplicatePair = new HashSet<Track>(trackDuplicates) { (Track)track };
                        duplicates.Add(duplicatePair);
                    }
                }

                if (callback != null)
                {
                    callback.Invoke((Track)track, total, ++current);
                }
            }

            for (int i = 0; i < duplicates.Count - 1; i++)
            {
                HashSet<Track> set = duplicates[i];
                for (int j = i + 1; j < duplicates.Count; j++)
                {
                    IEnumerable<Track> result = set.Intersect(duplicates[j]);
                    if (result.Any())
                    {
                        foreach (Track track in duplicates[j])
                        {
                            // collapse all duplicates in one set
                            set.Add(track);
                        }

                        duplicates.RemoveAt(j); /*Remove the duplicate set*/
                        j--;
                    }
                }
            }

            return duplicates.ToArray();
        }

        /// <summary>
        ///   Clear current storage
        /// </summary>
        public void ClearStorage()
        {
             throw new NotImplementedException();
        }
    }
}