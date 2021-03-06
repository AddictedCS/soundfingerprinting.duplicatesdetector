﻿namespace SoundFingerprinting.DuplicatesDetector
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    using SoundFingerprinting.Audio;
    using SoundFingerprinting.Builder;
    using SoundFingerprinting.Configuration;
    using SoundFingerprinting.DAO.Data;
    using SoundFingerprinting.Data;
    using SoundFingerprinting.Strides;

    internal class DuplicatesDetectorService
    {
        private const double FalsePositivesThreshold = 0.4;

        private readonly IStride createStride = new IncrementalRandomStride(512, 1024);

        private readonly IAdvancedModelService modelService;

        private readonly IAudioService audioService;

        private readonly IFingerprintCommandBuilder fingerprintCommandBuilder;

        private readonly IQueryFingerprintService queryFingerprintService;


        public DuplicatesDetectorService(IAdvancedModelService modelService, IAudioService audioService, IFingerprintCommandBuilder fingerprintCommandBuilder, IQueryFingerprintService queryFingerprintService)
        {
            this.modelService = modelService;
            this.audioService = audioService;
            this.fingerprintCommandBuilder = fingerprintCommandBuilder;
            this.queryFingerprintService = queryFingerprintService;
        }

        /// <summary>
        ///   Create fingerprints out of down sampled samples
        /// </summary>
        /// <param name = "samples">Down sampled to 5512 samples</param>
        /// <param name = "track">Track</param>
        public void CreateInsertFingerprints(AudioSamples samples, TrackInfo track)
        {
            if (track == null)
            {
                return; /*track is not eligible*/
            }

            /*Create fingerprints that will be used as initial fingerprints to be queried*/
            var hashes = fingerprintCommandBuilder.BuildFingerprintCommand()
                                                       .From(samples)
                                                       .WithFingerprintConfig(config =>
                                                       {
                                                            config.Stride = createStride;
                                                            return config;
                                                       })
                                                       .UsingServices(audioService)
                                                       .Hash()
                                                       .Result;

            modelService.Insert(track, hashes);
        }

        /// <summary>
        ///   Find duplicates between existing tracks in the database
        /// </summary>
        /// <param name = "callback">Callback invoked at each processed track</param>
        /// <returns>Sets of duplicates</returns>
        public HashSet<TrackData>[] FindDuplicates(Action<TrackData, int, int> callback)
        {
            var tracks = modelService.ReadAllTracks().ToList();
            var duplicates = new List<HashSet<TrackData>>();
            int total = tracks.Count, current = 0;
            var queryConfiguration = new DefaultQueryConfiguration { MaxTracksToReturn = int.MaxValue, ThresholdVotes = 4 };
            foreach (var track in tracks)
            {
                var trackDuplicates = new HashSet<TrackData>();

                var hashedFingerprints = modelService.ReadHashedFingerprintsByTrack(track.TrackReference);
                var max = hashedFingerprints.Max(_ => _.StartsAt);
                var min = hashedFingerprints.Min(_ => _.StartsAt);
                var hashes = new Hashes(hashedFingerprints, GetLength(min, max, queryConfiguration.FingerprintConfiguration.FingerprintLengthInSeconds));
                var result = queryFingerprintService.Query(hashes, queryConfiguration, modelService);

                if (result.ContainsMatches)
                {
                    foreach (var resultEntry in result.ResultEntries)
                    {
                        if (resultEntry.Confidence < FalsePositivesThreshold || track.Equals(resultEntry.Track))
                        {
                            continue;
                        }

                        trackDuplicates.Add(resultEntry.Track);
                    }

                    if (trackDuplicates.Any())
                    {
                        HashSet<TrackData> duplicatePair = new HashSet<TrackData>(trackDuplicates) { track };
                        duplicates.Add(duplicatePair);
                    }
                }

                callback?.Invoke(track, total, ++current);
            }

            for (int i = 0; i < duplicates.Count - 1; i++)
            {
                HashSet<TrackData> set = duplicates[i];
                for (int j = i + 1; j < duplicates.Count; j++)
                {
                    IEnumerable<TrackData> result = set.Intersect(duplicates[j]);
                    if (result.Any())
                    {
                        foreach (var track in duplicates[j])
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

        public void ClearStorage()
        {
            var tracks = modelService.ReadAllTracks();
            foreach (var track in tracks)
            {
                modelService.DeleteTrack(track.Id);
            }
        }

        private static double GetLength(double min, double max, double fingerprintLengthInSeconds)
        {
            return max - min + fingerprintLengthInSeconds;
        }
    }
}