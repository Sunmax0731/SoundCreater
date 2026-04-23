using System;
using System.Collections.Generic;
using System.Linq;
using GameAudioTool.Editor.Commands;
using GameAudioTool.Editor.Domain;

namespace GameAudioTool.Editor.Application
{
    public static class GameAudioTimelineCommandFactory
    {
        public static IGameAudioCommand AddTimelineNote(GameAudioProject sourceProject, string trackId, float startBeat, float durationBeat)
        {
            return GameAudioProjectCommandFactory.AddNote(
                sourceProject,
                trackId,
                new GameAudioNote
                {
                    Id = $"note-{Guid.NewGuid():N}",
                    StartBeat = startBeat,
                    DurationBeat = durationBeat,
                    MidiNote = 60,
                    Velocity = 0.8f
                });
        }

        public static IGameAudioCommand DeleteNotes(GameAudioProject sourceProject, IReadOnlyCollection<string> noteIds)
        {
            HashSet<string> selectedIds = NormalizeIds(noteIds);

            return GameAudioProjectCommandFactory.CreateMutation(sourceProject, selectedIds.Count == 1 ? "Delete Note" : "Delete Notes", project =>
            {
                bool removedAny = false;
                foreach (GameAudioTrack track in project.Tracks)
                {
                    removedAny |= track.Notes.RemoveAll(note => selectedIds.Contains(note.Id)) > 0;
                }

                if (!removedAny)
                {
                    throw new InvalidOperationException("No notes were found for deletion.");
                }
            });
        }

        public static IGameAudioCommand MoveNotes(GameAudioProject sourceProject, IReadOnlyCollection<GameAudioTimelineNotePlacement> placements)
        {
            Dictionary<string, GameAudioTimelineNotePlacement> placementMap = NormalizePlacements(placements);

            return GameAudioProjectCommandFactory.CreateMutation(sourceProject, placementMap.Count == 1 ? "Move Note" : "Move Notes", project =>
            {
                var pendingByTrack = new Dictionary<string, List<GameAudioNote>>(StringComparer.Ordinal);

                foreach (GameAudioTimelineNotePlacement placement in placementMap.Values)
                {
                    NoteLocation currentLocation = RequireNote(project, placement.NoteId);
                    GameAudioTrack destinationTrack = RequireTrack(project, placement.TrackId);
                    GameAudioNote movedNote = GameAudioProjectCloner.CloneNote(currentLocation.Note);
                    movedNote.StartBeat = placement.StartBeat;
                    movedNote.DurationBeat = placement.DurationBeat;

                    currentLocation.Track.Notes.RemoveAt(currentLocation.NoteIndex);

                    if (!pendingByTrack.TryGetValue(destinationTrack.Id, out List<GameAudioNote> pendingNotes))
                    {
                        pendingNotes = new List<GameAudioNote>();
                        pendingByTrack[destinationTrack.Id] = pendingNotes;
                    }

                    pendingNotes.Add(movedNote);
                }

                foreach ((string trackId, List<GameAudioNote> pendingNotes) in pendingByTrack)
                {
                    GameAudioTrack track = RequireTrack(project, trackId);
                    track.Notes.AddRange(pendingNotes);
                }
            });
        }

        public static IGameAudioCommand ResizeTimelineNote(GameAudioProject sourceProject, string noteId, string trackId, float startBeat, float durationBeat)
        {
            return MoveNotes(sourceProject, new[]
            {
                new GameAudioTimelineNotePlacement(noteId, trackId, startBeat, durationBeat)
            });
        }

        public static IGameAudioCommand DuplicateNotes(GameAudioProject sourceProject, IReadOnlyCollection<string> noteIds, float beatOffset)
        {
            return GameAudioProjectCommandFactory.DuplicateNotes(sourceProject, noteIds, beatOffset);
        }

        private static HashSet<string> NormalizeIds(IReadOnlyCollection<string> noteIds)
        {
            if (noteIds == null)
            {
                throw new ArgumentNullException(nameof(noteIds));
            }

            var normalized = new HashSet<string>(noteIds.Where(id => !string.IsNullOrWhiteSpace(id)), StringComparer.Ordinal);
            if (normalized.Count == 0)
            {
                throw new ArgumentException("At least one note id is required.", nameof(noteIds));
            }

            return normalized;
        }

        private static Dictionary<string, GameAudioTimelineNotePlacement> NormalizePlacements(IReadOnlyCollection<GameAudioTimelineNotePlacement> placements)
        {
            if (placements == null)
            {
                throw new ArgumentNullException(nameof(placements));
            }

            var normalized = new Dictionary<string, GameAudioTimelineNotePlacement>(StringComparer.Ordinal);
            foreach (GameAudioTimelineNotePlacement placement in placements)
            {
                if (string.IsNullOrWhiteSpace(placement.NoteId))
                {
                    continue;
                }

                normalized[placement.NoteId] = placement;
            }

            if (normalized.Count == 0)
            {
                throw new ArgumentException("At least one note placement is required.", nameof(placements));
            }

            return normalized;
        }

        private static GameAudioTrack RequireTrack(GameAudioProject project, string trackId)
        {
            GameAudioTrack track = project.Tracks.FirstOrDefault(candidate => string.Equals(candidate.Id, trackId, StringComparison.Ordinal));
            if (track == null)
            {
                throw new InvalidOperationException($"Track '{trackId}' was not found.");
            }

            return track;
        }

        private static NoteLocation RequireNote(GameAudioProject project, string noteId)
        {
            foreach (GameAudioTrack track in project.Tracks)
            {
                for (int noteIndex = 0; noteIndex < track.Notes.Count; noteIndex++)
                {
                    GameAudioNote note = track.Notes[noteIndex];
                    if (string.Equals(note.Id, noteId, StringComparison.Ordinal))
                    {
                        return new NoteLocation(track, note, noteIndex);
                    }
                }
            }

            throw new InvalidOperationException($"Note '{noteId}' was not found.");
        }

        private readonly struct NoteLocation
        {
            public NoteLocation(GameAudioTrack track, GameAudioNote note, int noteIndex)
            {
                Track = track;
                Note = note;
                NoteIndex = noteIndex;
            }

            public GameAudioTrack Track { get; }

            public GameAudioNote Note { get; }

            public int NoteIndex { get; }
        }
    }

    public readonly struct GameAudioTimelineNotePlacement
    {
        public GameAudioTimelineNotePlacement(string noteId, string trackId, float startBeat, float durationBeat)
        {
            NoteId = noteId;
            TrackId = trackId;
            StartBeat = startBeat;
            DurationBeat = durationBeat;
        }

        public string NoteId { get; }

        public string TrackId { get; }

        public float StartBeat { get; }

        public float DurationBeat { get; }
    }
}
