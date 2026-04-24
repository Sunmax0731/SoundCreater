using NUnit.Framework;
using TorusEdison.Editor.Windows;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace TorusEdison.Editor.Tests
{
    public sealed class GameAudioToolWindowShortcutTests
    {
        [Test]
        public void ShouldIgnoreShortcutForFocusedElement_IgnoresTextAndNumericFields()
        {
            Assert.That(GameAudioToolWindow.ShouldIgnoreShortcutForFocusedElement(new TextField()), Is.True);
            Assert.That(GameAudioToolWindow.ShouldIgnoreShortcutForFocusedElement(new IntegerField()), Is.True);
            Assert.That(GameAudioToolWindow.ShouldIgnoreShortcutForFocusedElement(new FloatField()), Is.True);
        }

        [Test]
        public void ShouldIgnoreShortcutForFocusedElement_IgnoresFieldChildren()
        {
            var textField = new TextField();
            var child = new VisualElement();
            textField.Add(child);

            Assert.That(GameAudioToolWindow.ShouldIgnoreShortcutForFocusedElement(child), Is.True);
        }

        [Test]
        public void ShouldIgnoreShortcutForFocusedElement_AllowsPlainTimelineElement()
        {
            Assert.That(GameAudioToolWindow.ShouldIgnoreShortcutForFocusedElement(new VisualElement()), Is.False);
        }
    }
}
