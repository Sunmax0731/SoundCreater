using NUnit.Framework;
using GameAudioTool.Editor.Application;
using GameAudioTool.Editor.Audio;

namespace GameAudioTool.Editor.Tests
{
    public sealed class GameAudioPreviewCursorCalculatorTests
    {
        [Test]
        public void Calculate_StartsAtBarOneBeatOne()
        {
            var project = GameAudioProjectFactory.CreateDefaultProject();
            project.TotalBars = 4;
            project.Bpm = 120;

            var previewState = new GameAudioPreviewState(
                true,
                false,
                false,
                false,
                0.0d,
                new GameAudioRenderResult(new float[0], 48000, 2, 48000 * 9, 48000 * 8, 0.0f),
                "Preview ready.",
                string.Empty);

            GameAudioPreviewCursorState cursor = GameAudioPreviewCursorCalculator.Calculate(project, previewState);

            Assert.That(cursor.CurrentBar, Is.EqualTo(1));
            Assert.That(cursor.BeatInBar, Is.EqualTo(1.0d));
            Assert.That(cursor.MusicalProgress, Is.EqualTo(0.0f));
            Assert.That(cursor.IsInTail, Is.False);
        }

        [Test]
        public void Calculate_ClampsTailToProjectEnd()
        {
            var project = GameAudioProjectFactory.CreateDefaultProject();
            project.TotalBars = 4;
            project.Bpm = 120;

            var previewState = new GameAudioPreviewState(
                true,
                false,
                false,
                false,
                8.5d,
                new GameAudioRenderResult(new float[0], 48000, 2, 48000 * 9, 48000 * 8, 0.0f),
                "Preview complete.",
                string.Empty);

            GameAudioPreviewCursorState cursor = GameAudioPreviewCursorCalculator.Calculate(project, previewState);

            Assert.That(cursor.CurrentBar, Is.EqualTo(4));
            Assert.That(cursor.BeatInBar, Is.EqualTo(4.0d));
            Assert.That(cursor.MusicalProgress, Is.EqualTo(1.0f));
            Assert.That(cursor.IsInTail, Is.True);
            Assert.That(cursor.TailSeconds, Is.EqualTo(0.5d).Within(0.0001d));
        }

        [Test]
        public void Calculate_MapsTimeIntoBarAndBeat()
        {
            var project = GameAudioProjectFactory.CreateDefaultProject();
            project.TotalBars = 4;
            project.Bpm = 120;

            var previewState = new GameAudioPreviewState(
                true,
                true,
                false,
                false,
                2.25d,
                new GameAudioRenderResult(new float[0], 48000, 2, 48000 * 9, 48000 * 8, 0.0f),
                "Preview playing.",
                string.Empty);

            GameAudioPreviewCursorState cursor = GameAudioPreviewCursorCalculator.Calculate(project, previewState);

            Assert.That(cursor.CurrentBar, Is.EqualTo(2));
            Assert.That(cursor.BeatInBar, Is.EqualTo(1.5d).Within(0.0001d));
            Assert.That(cursor.MusicalSeconds, Is.EqualTo(2.25d).Within(0.0001d));
        }

        [Test]
        public void Calculate_UsesPausedPlaybackPosition()
        {
            var project = GameAudioProjectFactory.CreateDefaultProject();
            project.TotalBars = 4;
            project.Bpm = 120;

            var previewState = new GameAudioPreviewState(
                true,
                false,
                true,
                false,
                2.25d,
                new GameAudioRenderResult(new float[0], 48000, 2, 48000 * 9, 48000 * 8, 0.0f),
                "Preview paused.",
                string.Empty);

            GameAudioPreviewCursorState cursor = GameAudioPreviewCursorCalculator.Calculate(project, previewState);

            Assert.That(cursor.CurrentBar, Is.EqualTo(2));
            Assert.That(cursor.BeatInBar, Is.EqualTo(1.5d).Within(0.0001d));
            Assert.That(cursor.MusicalSeconds, Is.EqualTo(2.25d).Within(0.0001d));
        }
    }
}
