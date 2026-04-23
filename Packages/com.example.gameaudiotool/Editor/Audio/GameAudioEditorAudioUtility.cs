using System;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace GameAudioTool.Editor.Audio
{
    internal static class GameAudioEditorAudioUtility
    {
        private static readonly Type AudioUtilType = typeof(AudioImporter).Assembly.GetType("UnityEditor.AudioUtil");
        private static readonly MethodInfo PlayPreviewClipMethod = ResolveMethod(
            "PlayPreviewClip",
            "PlayClip");
        private static readonly MethodInfo StopPreviewClipMethod = ResolveStopMethod(
            "StopPreviewClip",
            "StopClip");
        private static readonly MethodInfo StopAllPreviewClipsMethod = ResolveParameterlessMethod(
            "StopAllPreviewClips",
            "StopAllClips");

        public static bool IsAvailable => AudioUtilType != null
            && PlayPreviewClipMethod != null
            && (StopPreviewClipMethod != null || StopAllPreviewClipsMethod != null);

        public static bool SupportsNativeLooping => PlayPreviewClipMethod != null
            && PlayPreviewClipMethod.GetParameters().Any(parameter => parameter.ParameterType == typeof(bool));

        public static void PlayPreviewClip(AudioClip clip, bool loopPlayback, int startSample = 0)
        {
            if (clip == null)
            {
                throw new ArgumentNullException(nameof(clip));
            }

            if (!IsAvailable)
            {
                throw new InvalidOperationException("UnityEditor preview audio API is unavailable in this editor build.");
            }

            object[] arguments = BuildPlayArguments(PlayPreviewClipMethod, clip, loopPlayback, Math.Max(0, startSample));
            PlayPreviewClipMethod.Invoke(null, arguments);
        }

        public static void StopPreviewClip(AudioClip clip)
        {
            if (!IsAvailable)
            {
                return;
            }

            if (clip != null && StopPreviewClipMethod != null)
            {
                StopPreviewClipMethod.Invoke(null, new object[] { clip });
                return;
            }

            StopAllPreviewClips();
        }

        public static void StopAllPreviewClips()
        {
            if (!IsAvailable || StopAllPreviewClipsMethod == null)
            {
                return;
            }

            StopAllPreviewClipsMethod.Invoke(null, null);
        }

        private static MethodInfo ResolveMethod(params string[] names)
        {
            if (AudioUtilType == null)
            {
                return null;
            }

            foreach (string name in names)
            {
                MethodInfo match = AudioUtilType
                    .GetMethods(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)
                    .Where(method => method.Name == name)
                    .OrderByDescending(method => method.GetParameters().Length)
                    .FirstOrDefault(IsSupportedPlayMethod);
                if (match != null)
                {
                    return match;
                }
            }

            return null;
        }

        private static MethodInfo ResolveStopMethod(params string[] names)
        {
            if (AudioUtilType == null)
            {
                return null;
            }

            foreach (string name in names)
            {
                MethodInfo match = AudioUtilType
                    .GetMethods(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)
                    .FirstOrDefault(method =>
                    {
                        ParameterInfo[] parameters = method.GetParameters();
                        return method.Name == name
                            && parameters.Length == 1
                            && parameters[0].ParameterType == typeof(AudioClip);
                    });

                if (match != null)
                {
                    return match;
                }
            }

            return null;
        }

        private static MethodInfo ResolveParameterlessMethod(params string[] names)
        {
            if (AudioUtilType == null)
            {
                return null;
            }

            foreach (string name in names)
            {
                MethodInfo match = AudioUtilType
                    .GetMethods(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)
                    .FirstOrDefault(method => method.Name == name && method.GetParameters().Length == 0);
                if (match != null)
                {
                    return match;
                }
            }

            return null;
        }

        private static bool IsSupportedPlayMethod(MethodInfo method)
        {
            ParameterInfo[] parameters = method.GetParameters();
            if (parameters.Length == 0 || parameters[0].ParameterType != typeof(AudioClip))
            {
                return false;
            }

            for (int index = 1; index < parameters.Length; index++)
            {
                Type parameterType = parameters[index].ParameterType;
                if (parameterType != typeof(int) && parameterType != typeof(bool))
                {
                    return false;
                }
            }

            return true;
        }

        private static object[] BuildPlayArguments(MethodInfo method, AudioClip clip, bool loopPlayback, int startSample)
        {
            ParameterInfo[] parameters = method.GetParameters();
            object[] arguments = new object[parameters.Length];
            arguments[0] = clip;
            bool wroteStartSample = false;

            for (int index = 1; index < parameters.Length; index++)
            {
                Type parameterType = parameters[index].ParameterType;
                if (parameterType == typeof(bool))
                {
                    arguments[index] = loopPlayback;
                }
                else if (parameterType == typeof(int))
                {
                    arguments[index] = wroteStartSample ? 0 : startSample;
                    wroteStartSample = true;
                }
                else
                {
                    arguments[index] = parameterType.IsValueType
                        ? Activator.CreateInstance(parameterType)
                        : null;
                }
            }

            return arguments;
        }
    }
}
