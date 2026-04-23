using GameAudioTool.Editor.Windows;
using NUnit.Framework;

namespace GameAudioTool.Editor.Tests
{
    public sealed class GameAudioTimelineGridUtilityTests
    {
        [TestCase(null, "1/16")]
        [TestCase("", "1/16")]
        [TestCase("1/4", "1/4")]
        [TestCase("1/64", "1/64")]
        [TestCase("bogus", "1/16")]
        public void NormalizeDivision_ReturnsSupportedValue(string input, string expected)
        {
            Assert.That(GameAudioTimelineGridUtility.NormalizeDivision(input), Is.EqualTo(expected));
        }

        [TestCase("1/4", 1.0f)]
        [TestCase("1/8", 0.5f)]
        [TestCase("1/16", 0.25f)]
        [TestCase("1/32", 0.125f)]
        [TestCase("1/64", 0.0625f)]
        public void GetBeatStep_ReturnsExpectedBeatLength(string division, float expected)
        {
            Assert.That(GameAudioTimelineGridUtility.GetBeatStep(division), Is.EqualTo(expected).Within(0.0001f));
        }

        [Test]
        public void SnapBeat_UsesConfiguredDivision()
        {
            Assert.That(GameAudioTimelineGridUtility.SnapBeat(1.14f, "1/16"), Is.EqualTo(1.25f).Within(0.0001f));
            Assert.That(GameAudioTimelineGridUtility.SnapBeat(1.14f, "1/8"), Is.EqualTo(1.0f).Within(0.0001f));
        }

        [Test]
        public void SnapDuration_EnforcesMinimumLength()
        {
            Assert.That(GameAudioTimelineGridUtility.SnapDuration(0.01f, "1/64"), Is.EqualTo(0.0625f).Within(0.0001f));
            Assert.That(GameAudioTimelineGridUtility.SnapDuration(0.37f, "1/16"), Is.EqualTo(0.25f).Within(0.0001f));
        }
    }
}
