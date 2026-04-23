using System;
using System.Collections.Generic;
using System.Linq;
using TorusEdison.Editor.Commands;
using TorusEdison.Editor.Domain;
using TorusEdison.Editor.Utilities;

namespace TorusEdison.Editor.Application
{
    public static class GameAudioProjectCommandFactory
    {
        public static IGameAudioCommand CreateMutation(GameAudioProject sourceProject, string displayName, Action<GameAudioProject> applyChange)
        {
            if (sourceProject == null)
            {
                throw new ArgumentNullException(nameof(sourceProject));
            }

            if (string.IsNullOrWhiteSpace(displayName))
            {
                throw new ArgumentException("Display name is required.", nameof(displayName));
            }

            if (applyChange == null)
            {
                throw new ArgumentNullException(nameof(applyChange));
            }

            GameAudioProject afterProject = GameAudioProjectCloner.CloneProject(sourceProject);
            applyChange(afterProject);
            NormalizeProject(afterProject);
            return new GameAudioProjectSnapshotCommand(displayName, sourceProject, afterProject);
        }

        public static IGameAudioCommand AddNote(GameAudioProject sourceProject, string trackId, GameAudioNote note, int? insertIndex = null)
        {
            return CreateMutation(sourceProject, "Add Note", project =>
            {
                GameAudioTrack track = RequireTrack(project, trackId);
                GameAudioNote noteClone = GameAudioProjectCloner.CloneNote(note ?? throw new ArgumentNullException(nameof(note)));
                noteClone.Id = EnsureId(noteClone.Id, "note");
                NormalizeNote(noteClone);

                if (insertIndex.HasValue)
                {
                    int clampedIndex = Math.Max(0, Math.Min(insertIndex.Value, track.Notes.Count));
                    track.Notes.Insert(clampedIndex, noteClone);
                }
                else
                {
                    track.Notes.Add(noteClone);
                    SortNotes(track.Notes);
                }
            });
        }

        public static IGameAudioCommand RemoveNote(GameAudioProject sourceProject, string trackId, string noteId)
        {
            return CreateMutation(sourceProject, "Remove Note", project =>
            {
                GameAudioTrack track = RequireTrack(project, trackId);
                int noteIndex = track.Notes.FindIndex(note => string.Equals(note.Id, noteId, StringComparison.Ordinal));
                if (noteIndex < 0)
                {
                    throw new InvalidOperationException($"Note '{noteId}' was not found in track '{trackId}'.");
                }

                track.Notes.RemoveAt(noteIndex);
            });
        }

        public static IGameAudioCommand MoveNote(GameAudioProject sourceProject, string noteId, string destinationTrackId, float startBeat)
        {
            return CreateMutation(sourceProject, "Move Note", project =>
            {
                NoteLocation location = RequireNote(project, noteId);
                GameAudioTrack destinationTrack = RequireTrack(project, destinationTrackId);
                GameAudioNote note = location.Note;

                location.Track.Notes.RemoveAt(location.NoteIndex);
                note.StartBeat = startBeat;
                NormalizeNote(note);
                destinationTrack.Notes.Add(note);
                SortNotes(destinationTrack.Notes);
                if (!ReferenceEquals(location.Track, destinationTrack))
                {
                    SortNotes(location.Track.Notes);
                }
            });
        }

        public static IGameAudioCommand ResizeNote(GameAudioProject sourceProject, string noteId, float durationBeat)
        {
            return ChangeNotes(sourceProject, new[] { noteId }, "Resize Note", note =>
            {
                note.DurationBeat = durationBeat;
            });
        }

        public static IGameAudioCommand DuplicateNotes(GameAudioProject sourceProject, IReadOnlyCollection<string> noteIds, float startBeatOffset)
        {
            HashSet<string> ids = NormalizeIds(noteIds, nameof(noteIds));

            return CreateMutation(sourceProject, "Duplicate Notes", project =>
            {
                bool anyNoteMatched = false;
                foreach (GameAudioTrack track in project.Tracks)
                {
                    var duplicates = new List<GameAudioNote>();
                    foreach (GameAudioNote note in track.Notes)
                    {
                        if (!ids.Contains(note.Id))
                        {
                            continue;
                        }

                        anyNoteMatched = true;
                        GameAudioNote clone = GameAudioProjectCloner.CloneNote(note);
                        clone.Id = EnsureId(null, "note");
                        clone.StartBeat += startBeatOffset;
                        NormalizeNote(clone);
                        duplicates.Add(clone);
                    }

                    if (duplicates.Count > 0)
                    {
                        track.Notes.AddRange(duplicates);
                        SortNotes(track.Notes);
                    }
                }

                if (!anyNoteMatched)
                {
                    throw new InvalidOperationException("No notes were found for duplication.");
                }
            });
        }

        public static IGameAudioCommand ChangeNotes(GameAudioProject sourceProject, IReadOnlyCollection<string> noteIds, string displayName, Action<GameAudioNote> applyChange)
        {
            HashSet<string> ids = NormalizeIds(noteIds, nameof(noteIds));

            return CreateMutation(sourceProject, displayName, project =>
            {
                bool anyNoteMatched = false;
                foreach (GameAudioTrack track in project.Tracks)
                {
                    bool trackChanged = false;
                    foreach (GameAudioNote note in track.Notes)
                    {
                        if (!ids.Contains(note.Id))
                        {
                            continue;
                        }

                        applyChange(note);
                        NormalizeNote(note);
                        anyNoteMatched = true;
                        trackChanged = true;
                    }

                    if (trackChanged)
                    {
                        SortNotes(track.Notes);
                    }
                }

                if (!anyNoteMatched)
                {
                    throw new InvalidOperationException("No notes were found for the requested change.");
                }
            });
        }

        public static IGameAudioCommand AddTrack(GameAudioProject sourceProject, GameAudioTrack track, int? insertIndex = null)
        {
            return CreateMutation(sourceProject, "Add Track", project =>
            {
                if (project.Tracks.Count >= GameAudioToolInfo.MaxTrackCount)
                {
                    throw new InvalidOperationException($"Track count cannot exceed {GameAudioToolInfo.MaxTrackCount}.");
                }

                GameAudioTrack trackClone = GameAudioProjectCloner.CloneTrack(track ?? throw new ArgumentNullException(nameof(track)));
                trackClone.Id = EnsureId(trackClone.Id, "track");
                NormalizeTrack(trackClone, project.Tracks.Count + 1);

                if (insertIndex.HasValue)
                {
                    int clampedIndex = Math.Max(0, Math.Min(insertIndex.Value, project.Tracks.Count));
                    project.Tracks.Insert(clampedIndex, trackClone);
                }
                else
                {
                    project.Tracks.Add(trackClone);
                }
            });
        }

        public static IGameAudioCommand RemoveTrack(GameAudioProject sourceProject, string trackId)
        {
            return CreateMutation(sourceProject, "Remove Track", project =>
            {
                int trackIndex = project.Tracks.FindIndex(track => string.Equals(track.Id, trackId, StringComparison.Ordinal));
                if (trackIndex < 0)
                {
                    throw new InvalidOperationException($"Track '{trackId}' was not found.");
                }

                project.Tracks.RemoveAt(trackIndex);
            });
        }

        public static IGameAudioCommand ChangeTracks(GameAudioProject sourceProject, IReadOnlyCollection<string> trackIds, string displayName, Action<GameAudioTrack> applyChange)
        {
            HashSet<string> ids = NormalizeIds(trackIds, nameof(trackIds));

            return CreateMutation(sourceProject, displayName, project =>
            {
                bool anyTrackMatched = false;
                for (int index = 0; index < project.Tracks.Count; index++)
                {
                    GameAudioTrack track = project.Tracks[index];
                    if (!ids.Contains(track.Id))
                    {
                        continue;
                    }

                    applyChange(track);
                    NormalizeTrack(track, index + 1);
                    anyTrackMatched = true;
                }

                if (!anyTrackMatched)
                {
                    throw new InvalidOperationException("No tracks were found for the requested change.");
                }
            });
        }

        public static IGameAudioCommand ChangeProject(GameAudioProject sourceProject, string displayName, Action<GameAudioProject> applyChange)
        {
            return CreateMutation(sourceProject, displayName, applyChange);
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

        private static HashSet<string> NormalizeIds(IReadOnlyCollection<string> ids, string paramName)
        {
            if (ids == null)
            {
                throw new ArgumentNullException(paramName);
            }

            var normalized = new HashSet<string>(ids.Where(id => !string.IsNullOrWhiteSpace(id)), StringComparer.Ordinal);
            if (normalized.Count == 0)
            {
                throw new ArgumentException("At least one id is required.", paramName);
            }

            return normalized;
        }

        private static void NormalizeProject(GameAudioProject project)
        {
            project.Id = EnsureId(project.Id, "proj");
            project.Name = string.IsNullOrWhiteSpace(project.Name) ? "New Audio Project" : project.Name.Trim();
            project.Bpm = Math.Max(1, project.Bpm);
            project.TotalBars = Math.Max(1, project.TotalBars);
            project.SampleRate = GameAudioValidationUtility.IsSupportedSampleRate(project.SampleRate)
                ? project.SampleRate
                : GameAudioToolInfo.DefaultSampleRate;
            project.ChannelMode = project.ChannelMode == GameAudioChannelMode.Mono
                ? GameAudioChannelMode.Mono
                : GameAudioChannelMode.Stereo;
            project.MasterGainDb = GameAudioValidationUtility.ClampFloat(project.MasterGainDb, -24.0f, 6.0f);
            project.TimeSignature = NormalizeTimeSignature(project.TimeSignature);
            project.Tracks = project.Tracks ?? new List<GameAudioTrack>();

            for (int index = 0; index < project.Tracks.Count; index++)
            {
                NormalizeTrack(project.Tracks[index], index + 1);
            }
        }

        private static void NormalizeTrack(GameAudioTrack track, int trackIndex)
        {
            track.Id = EnsureId(track.Id, "track");
            track.Name = string.IsNullOrWhiteSpace(track.Name) ? $"Track {trackIndex:00}" : track.Name.Trim();
            track.VolumeDb = GameAudioValidationUtility.ClampFloat(track.VolumeDb, -48.0f, 6.0f);
            track.Pan = GameAudioValidationUtility.ClampFloat(track.Pan, -1.0f, 1.0f);
            track.DefaultVoice = NormalizeVoice(track.DefaultVoice);
            track.Notes = track.Notes ?? new List<GameAudioNote>();

            foreach (GameAudioNote note in track.Notes)
            {
                NormalizeNote(note);
            }

            SortNotes(track.Notes);
        }

        private static void NormalizeNote(GameAudioNote note)
        {
            note.Id = EnsureId(note.Id, "note");
            note.StartBeat = GameAudioValidationUtility.ClampFloat(note.StartBeat, 0.0f, 100000.0f);
            note.DurationBeat = GameAudioValidationUtility.ClampFloat(note.DurationBeat, GameAudioToolInfo.MinNoteDurationBeat, 100000.0f);
            note.MidiNote = GameAudioValidationUtility.ClampInt(note.MidiNote, 0, 127);
            note.Velocity = GameAudioValidationUtility.ClampFloat(note.Velocity, 0.0f, 1.0f);
            note.VoiceOverride = note.VoiceOverride == null ? null : NormalizeVoice(note.VoiceOverride);
        }

        private static GameAudioVoiceSettings NormalizeVoice(GameAudioVoiceSettings voice)
        {
            GameAudioVoiceSettings normalized = voice ?? GameAudioProjectFactory.CreateDefaultVoice();
            normalized.PulseWidth = GameAudioValidationUtility.ClampFloat(normalized.PulseWidth, 0.10f, 0.90f);
            normalized.NoiseMix = GameAudioValidationUtility.ClampFloat(normalized.NoiseMix, 0.0f, 1.0f);
            normalized.Adsr = NormalizeEnvelope(normalized.Adsr);
            normalized.Effect = NormalizeEffect(normalized.Effect);
            return normalized;
        }

        private static GameAudioEnvelopeSettings NormalizeEnvelope(GameAudioEnvelopeSettings envelope)
        {
            GameAudioEnvelopeSettings normalized = envelope ?? GameAudioProjectFactory.CreateDefaultEnvelope();
            normalized.AttackMs = GameAudioValidationUtility.ClampInt(normalized.AttackMs, 0, 5000);
            normalized.DecayMs = GameAudioValidationUtility.ClampInt(normalized.DecayMs, 0, 5000);
            normalized.Sustain = GameAudioValidationUtility.ClampFloat(normalized.Sustain, 0.0f, 1.0f);
            normalized.ReleaseMs = GameAudioValidationUtility.ClampInt(normalized.ReleaseMs, 0, 5000);
            return normalized;
        }

        private static GameAudioEffectSettings NormalizeEffect(GameAudioEffectSettings effect)
        {
            GameAudioEffectSettings normalized = effect ?? GameAudioProjectFactory.CreateDefaultEffect();
            normalized.VolumeDb = GameAudioValidationUtility.ClampFloat(normalized.VolumeDb, -48.0f, 6.0f);
            normalized.Pan = GameAudioValidationUtility.ClampFloat(normalized.Pan, -1.0f, 1.0f);
            normalized.PitchSemitone = GameAudioValidationUtility.ClampFloat(normalized.PitchSemitone, -24.0f, 24.0f);
            normalized.FadeInMs = GameAudioValidationUtility.ClampInt(normalized.FadeInMs, 0, 3000);
            normalized.FadeOutMs = GameAudioValidationUtility.ClampInt(normalized.FadeOutMs, 0, 3000);
            normalized.Delay = NormalizeDelay(normalized.Delay);
            return normalized;
        }

        private static GameAudioDelaySettings NormalizeDelay(GameAudioDelaySettings delay)
        {
            GameAudioDelaySettings normalized = delay ?? GameAudioProjectFactory.CreateDefaultDelay();
            normalized.TimeMs = GameAudioValidationUtility.ClampInt(normalized.TimeMs, 20, 1000);
            normalized.Feedback = GameAudioValidationUtility.ClampFloat(normalized.Feedback, 0.0f, 0.70f);
            normalized.Mix = GameAudioValidationUtility.ClampFloat(normalized.Mix, 0.0f, 1.0f);
            return normalized;
        }

        private static GameAudioTimeSignature NormalizeTimeSignature(GameAudioTimeSignature timeSignature)
        {
            if (timeSignature == null)
            {
                return new GameAudioTimeSignature { Numerator = 4, Denominator = 4 };
            }

            bool isSupported = (timeSignature.Numerator == 4 && timeSignature.Denominator == 4)
                || (timeSignature.Numerator == 3 && timeSignature.Denominator == 4)
                || (timeSignature.Numerator == 6 && timeSignature.Denominator == 8);

            return isSupported
                ? timeSignature
                : new GameAudioTimeSignature { Numerator = 4, Denominator = 4 };
        }

        private static string EnsureId(string currentId, string prefix)
        {
            return string.IsNullOrWhiteSpace(currentId)
                ? $"{prefix}-{Guid.NewGuid():N}"
                : currentId;
        }

        private static void SortNotes(List<GameAudioNote> notes)
        {
            notes.Sort((left, right) =>
            {
                int startComparison = left.StartBeat.CompareTo(right.StartBeat);
                return startComparison != 0
                    ? startComparison
                    : string.Compare(left.Id, right.Id, StringComparison.Ordinal);
            });
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
}
