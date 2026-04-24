using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace TorusEdison.Editor.Persistence
{
    internal static class GameAudioProjectSchemaValidator
    {
        public static void Validate(string json)
        {
            GameAudioJsonNode root = GameAudioSimpleJsonParser.Parse(json);
            RequireObject(root, "$");

            RequireString(root, "$", "formatVersion");
            OptionalString(root, "$", "toolVersion");

            GameAudioJsonNode project = RequireObjectProperty(root, "$", "project");
            ValidateProject(project, "$.project");
        }

        private static void ValidateProject(GameAudioJsonNode node, string path)
        {
            OptionalString(node, path, "id");
            RequireString(node, path, "name");
            OptionalNumber(node, path, "bpm");
            if (TryGetOptionalObject(node, path, "timeSignature", out GameAudioJsonNode timeSignature))
            {
                OptionalNumber(timeSignature, $"{path}.timeSignature", "numerator");
                OptionalNumber(timeSignature, $"{path}.timeSignature", "denominator");
            }

            OptionalNumber(node, path, "totalBars");
            OptionalNumber(node, path, "sampleRate");
            OptionalString(node, path, "channelMode");
            OptionalNumber(node, path, "masterGainDb");
            OptionalBoolean(node, path, "loopPlayback");
            if (TryGetOptionalObject(node, path, "exportSettings", out GameAudioJsonNode exportSettings))
            {
                ValidateExportSettings(exportSettings, $"{path}.exportSettings");
            }

            if (TryGetOptionalObject(node, path, "importedAudioConversion", out GameAudioJsonNode importedAudioConversion))
            {
                ValidateImportedAudioConversion(importedAudioConversion, $"{path}.importedAudioConversion");
            }

            if (TryGetOptionalArray(node, path, "tracks", out GameAudioJsonNode tracks))
            {
                for (int index = 0; index < tracks.Items.Count; index++)
                {
                    ValidateTrack(tracks.Items[index], $"{path}.tracks[{index}]");
                }
            }
        }

        private static void ValidateExportSettings(GameAudioJsonNode node, string path)
        {
            RequireObject(node, path);
            OptionalString(node, path, "durationMode");
            OptionalNumber(node, path, "durationSeconds");
            OptionalBoolean(node, path, "includeTail");
        }

        private static void ValidateImportedAudioConversion(GameAudioJsonNode node, string path)
        {
            RequireObject(node, path);
            OptionalString(node, path, "sourceClipName");
            OptionalString(node, path, "sourceAssetPath");
            OptionalNumber(node, path, "sourceSampleRate");
            OptionalNumber(node, path, "sourceChannelCount");
            OptionalNumber(node, path, "sourceDurationSeconds");
            OptionalNumber(node, path, "targetSampleRate");
            OptionalString(node, path, "targetChannelMode");
            OptionalNumber(node, path, "outputChannelCount");
            OptionalString(node, path, "outputWaveFileName");
        }

        private static void ValidateTrack(GameAudioJsonNode node, string path)
        {
            RequireObject(node, path);
            OptionalString(node, path, "id");
            OptionalString(node, path, "name");
            OptionalBoolean(node, path, "mute");
            OptionalBoolean(node, path, "solo");
            OptionalNumber(node, path, "volumeDb");
            OptionalNumber(node, path, "pan");
            if (TryGetOptionalObject(node, path, "defaultVoice", out GameAudioJsonNode defaultVoice))
            {
                ValidateVoice(defaultVoice, $"{path}.defaultVoice");
            }

            if (TryGetOptionalArray(node, path, "notes", out GameAudioJsonNode notes))
            {
                for (int index = 0; index < notes.Items.Count; index++)
                {
                    ValidateNote(notes.Items[index], $"{path}.notes[{index}]");
                }
            }
        }

        private static void ValidateNote(GameAudioJsonNode node, string path)
        {
            RequireObject(node, path);
            OptionalString(node, path, "id");
            OptionalNumber(node, path, "startBeat");
            OptionalNumber(node, path, "durationBeat");
            OptionalNumber(node, path, "midiNote");
            OptionalNumber(node, path, "velocity");

            if (TryGetOptionalObject(node, path, "voiceOverride", out GameAudioJsonNode voiceOverride))
            {
                ValidateVoice(voiceOverride, $"{path}.voiceOverride");
            }
        }

        private static void ValidateVoice(GameAudioJsonNode node, string path)
        {
            RequireObject(node, path);
            OptionalString(node, path, "waveform");
            OptionalNumber(node, path, "pulseWidth");
            OptionalBoolean(node, path, "noiseEnabled");
            OptionalString(node, path, "noiseType");
            OptionalNumber(node, path, "noiseMix");
            if (TryGetOptionalObject(node, path, "adsr", out GameAudioJsonNode adsr))
            {
                ValidateEnvelope(adsr, $"{path}.adsr");
            }

            if (TryGetOptionalObject(node, path, "effect", out GameAudioJsonNode effect))
            {
                ValidateEffect(effect, $"{path}.effect");
            }
        }

        private static void ValidateEnvelope(GameAudioJsonNode node, string path)
        {
            RequireObject(node, path);
            OptionalNumber(node, path, "attackMs");
            OptionalNumber(node, path, "decayMs");
            OptionalNumber(node, path, "sustain");
            OptionalNumber(node, path, "releaseMs");
        }

        private static void ValidateEffect(GameAudioJsonNode node, string path)
        {
            RequireObject(node, path);
            OptionalNumber(node, path, "volumeDb");
            OptionalNumber(node, path, "pan");
            OptionalNumber(node, path, "pitchSemitone");
            OptionalNumber(node, path, "fadeInMs");
            OptionalNumber(node, path, "fadeOutMs");
            if (TryGetOptionalObject(node, path, "delay", out GameAudioJsonNode delay))
            {
                ValidateDelay(delay, $"{path}.delay");
            }
        }

        private static void ValidateDelay(GameAudioJsonNode node, string path)
        {
            RequireObject(node, path);
            OptionalBoolean(node, path, "enabled");
            OptionalNumber(node, path, "timeMs");
            OptionalNumber(node, path, "feedback");
            OptionalNumber(node, path, "mix");
        }

        private static void RequireObject(GameAudioJsonNode node, string path)
        {
            if (node == null || node.Kind != GameAudioJsonKind.Object)
            {
                throw new GameAudioPersistenceException($"{path} must be an object.");
            }
        }

        private static void RequireString(GameAudioJsonNode node, string path, string propertyName)
        {
            RequirePropertyKind(node, path, propertyName, GameAudioJsonKind.String);
        }

        private static void RequireNumber(GameAudioJsonNode node, string path, string propertyName)
        {
            RequirePropertyKind(node, path, propertyName, GameAudioJsonKind.Number);
        }

        private static void RequireBoolean(GameAudioJsonNode node, string path, string propertyName)
        {
            RequirePropertyKind(node, path, propertyName, GameAudioJsonKind.Boolean);
        }

        private static GameAudioJsonNode RequireObjectProperty(GameAudioJsonNode node, string path, string propertyName)
        {
            return RequirePropertyKind(node, path, propertyName, GameAudioJsonKind.Object);
        }

        private static GameAudioJsonNode RequireArrayProperty(GameAudioJsonNode node, string path, string propertyName)
        {
            return RequirePropertyKind(node, path, propertyName, GameAudioJsonKind.Array);
        }

        private static void OptionalString(GameAudioJsonNode node, string path, string propertyName)
        {
            OptionalPropertyKind(node, path, propertyName, GameAudioJsonKind.String);
        }

        private static void OptionalNumber(GameAudioJsonNode node, string path, string propertyName)
        {
            OptionalPropertyKind(node, path, propertyName, GameAudioJsonKind.Number);
        }

        private static void OptionalBoolean(GameAudioJsonNode node, string path, string propertyName)
        {
            OptionalPropertyKind(node, path, propertyName, GameAudioJsonKind.Boolean);
        }

        private static bool TryGetOptionalObject(GameAudioJsonNode node, string path, string propertyName, out GameAudioJsonNode propertyValue)
        {
            return TryGetOptionalPropertyKind(node, path, propertyName, GameAudioJsonKind.Object, out propertyValue);
        }

        private static bool TryGetOptionalArray(GameAudioJsonNode node, string path, string propertyName, out GameAudioJsonNode propertyValue)
        {
            return TryGetOptionalPropertyKind(node, path, propertyName, GameAudioJsonKind.Array, out propertyValue);
        }

        private static GameAudioJsonNode RequirePropertyKind(GameAudioJsonNode node, string path, string propertyName, GameAudioJsonKind expectedKind)
        {
            if (!TryGetProperty(node, propertyName, out GameAudioJsonNode propertyValue))
            {
                throw new GameAudioPersistenceException($"{path}.{propertyName} is required.");
            }

            if (propertyValue.Kind != expectedKind)
            {
                throw new GameAudioPersistenceException($"{path}.{propertyName} must be a {expectedKind.ToString().ToLowerInvariant()}.");
            }

            return propertyValue;
        }

        private static void OptionalPropertyKind(GameAudioJsonNode node, string path, string propertyName, GameAudioJsonKind expectedKind)
        {
            TryGetOptionalPropertyKind(node, path, propertyName, expectedKind, out _);
        }

        private static bool TryGetOptionalPropertyKind(GameAudioJsonNode node, string path, string propertyName, GameAudioJsonKind expectedKind, out GameAudioJsonNode propertyValue)
        {
            propertyValue = null;
            if (!TryGetProperty(node, propertyName, out GameAudioJsonNode candidate) || candidate.Kind == GameAudioJsonKind.Null)
            {
                return false;
            }

            if (candidate.Kind != expectedKind)
            {
                throw new GameAudioPersistenceException($"{path}.{propertyName} must be a {expectedKind.ToString().ToLowerInvariant()}.");
            }

            propertyValue = candidate;
            return true;
        }

        private static bool TryGetProperty(GameAudioJsonNode node, string propertyName, out GameAudioJsonNode propertyValue)
        {
            propertyValue = null;
            return node != null
                && node.Kind == GameAudioJsonKind.Object
                && node.TryGetProperty(propertyName, out propertyValue);
        }
    }

    internal enum GameAudioJsonKind
    {
        Object,
        Array,
        String,
        Number,
        Boolean,
        Null
    }

    internal sealed class GameAudioJsonNode
    {
        private readonly Dictionary<string, GameAudioJsonNode> _properties;
        private readonly List<GameAudioJsonNode> _items;

        private GameAudioJsonNode(GameAudioJsonKind kind, Dictionary<string, GameAudioJsonNode> properties = null, List<GameAudioJsonNode> items = null)
        {
            Kind = kind;
            _properties = properties;
            _items = items;
        }

        public GameAudioJsonKind Kind { get; }

        public IReadOnlyList<GameAudioJsonNode> Items => _items ?? (IReadOnlyList<GameAudioJsonNode>)System.Array.Empty<GameAudioJsonNode>();

        public static GameAudioJsonNode Object(Dictionary<string, GameAudioJsonNode> properties)
        {
            return new GameAudioJsonNode(GameAudioJsonKind.Object, properties: properties);
        }

        public static GameAudioJsonNode Array(List<GameAudioJsonNode> items)
        {
            return new GameAudioJsonNode(GameAudioJsonKind.Array, items: items);
        }

        public static GameAudioJsonNode String()
        {
            return new GameAudioJsonNode(GameAudioJsonKind.String);
        }

        public static GameAudioJsonNode Number()
        {
            return new GameAudioJsonNode(GameAudioJsonKind.Number);
        }

        public static GameAudioJsonNode Boolean()
        {
            return new GameAudioJsonNode(GameAudioJsonKind.Boolean);
        }

        public static GameAudioJsonNode Null()
        {
            return new GameAudioJsonNode(GameAudioJsonKind.Null);
        }

        public bool TryGetProperty(string name, out GameAudioJsonNode value)
        {
            value = null;
            return _properties != null && _properties.TryGetValue(name, out value);
        }
    }

    internal sealed class GameAudioSimpleJsonParser
    {
        private readonly string _json;
        private int _index;

        private GameAudioSimpleJsonParser(string json)
        {
            _json = json ?? string.Empty;
        }

        public static GameAudioJsonNode Parse(string json)
        {
            if (string.IsNullOrWhiteSpace(json))
            {
                throw new GameAudioPersistenceException("Project JSON is empty.");
            }

            var parser = new GameAudioSimpleJsonParser(json);
            GameAudioJsonNode value = parser.ParseValue();
            parser.SkipWhitespace();
            if (!parser.IsEnd)
            {
                parser.Fail("Unexpected trailing characters.");
            }

            return value;
        }

        private bool IsEnd => _index >= _json.Length;

        private GameAudioJsonNode ParseValue()
        {
            SkipWhitespace();
            if (IsEnd)
            {
                Fail("Unexpected end of input.");
            }

            switch (_json[_index])
            {
                case '{':
                    return ParseObject();
                case '[':
                    return ParseArray();
                case '"':
                    ParseStringValue();
                    return GameAudioJsonNode.String();
                case 't':
                    ParseLiteral("true");
                    return GameAudioJsonNode.Boolean();
                case 'f':
                    ParseLiteral("false");
                    return GameAudioJsonNode.Boolean();
                case 'n':
                    ParseLiteral("null");
                    return GameAudioJsonNode.Null();
                default:
                    if (_json[_index] == '-' || char.IsDigit(_json[_index]))
                    {
                        ParseNumberValue();
                        return GameAudioJsonNode.Number();
                    }

                    Fail($"Unexpected character '{_json[_index]}'.");
                    return null;
            }
        }

        private GameAudioJsonNode ParseObject()
        {
            Expect('{');
            SkipWhitespace();

            var properties = new Dictionary<string, GameAudioJsonNode>(StringComparer.Ordinal);
            if (TryConsume('}'))
            {
                return GameAudioJsonNode.Object(properties);
            }

            while (true)
            {
                SkipWhitespace();
                if (Current != '"')
                {
                    Fail("Object keys must be strings.");
                }

                string propertyName = ParseStringValue();
                SkipWhitespace();
                Expect(':');
                GameAudioJsonNode propertyValue = ParseValue();
                properties[propertyName] = propertyValue;

                SkipWhitespace();
                if (TryConsume('}'))
                {
                    return GameAudioJsonNode.Object(properties);
                }

                Expect(',');
            }
        }

        private GameAudioJsonNode ParseArray()
        {
            Expect('[');
            SkipWhitespace();

            var items = new List<GameAudioJsonNode>();
            if (TryConsume(']'))
            {
                return GameAudioJsonNode.Array(items);
            }

            while (true)
            {
                items.Add(ParseValue());
                SkipWhitespace();
                if (TryConsume(']'))
                {
                    return GameAudioJsonNode.Array(items);
                }

                Expect(',');
            }
        }

        private string ParseStringValue()
        {
            Expect('"');

            var builder = new StringBuilder();
            while (!IsEnd)
            {
                char character = _json[_index++];
                if (character == '"')
                {
                    return builder.ToString();
                }

                if (character == '\\')
                {
                    if (IsEnd)
                    {
                        Fail("Unterminated escape sequence.");
                    }

                    builder.Append(ParseEscapedCharacter());
                    continue;
                }

                if (character < 0x20)
                {
                    Fail("Control characters must be escaped.");
                }

                builder.Append(character);
            }

            Fail("Unterminated string literal.");
            return string.Empty;
        }

        private char ParseEscapedCharacter()
        {
            char escape = _json[_index++];
            switch (escape)
            {
                case '"':
                case '\\':
                case '/':
                    return escape;
                case 'b':
                    return '\b';
                case 'f':
                    return '\f';
                case 'n':
                    return '\n';
                case 'r':
                    return '\r';
                case 't':
                    return '\t';
                case 'u':
                    return ParseUnicodeEscape();
                default:
                    Fail($"Unsupported escape sequence '\\{escape}'.");
                    return '\0';
            }
        }

        private char ParseUnicodeEscape()
        {
            if (_index + 4 > _json.Length)
            {
                Fail("Incomplete unicode escape.");
            }

            string hex = _json.Substring(_index, 4);
            if (!ushort.TryParse(hex, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out ushort codePoint))
            {
                Fail("Invalid unicode escape.");
            }

            _index += 4;
            return (char)codePoint;
        }

        private void ParseNumberValue()
        {
            int start = _index;

            if (Current == '-')
            {
                _index++;
            }

            ConsumeDigits(requireAtLeastOneDigit: true);

            if (!IsEnd && Current == '.')
            {
                _index++;
                ConsumeDigits(requireAtLeastOneDigit: true);
            }

            if (!IsEnd && (Current == 'e' || Current == 'E'))
            {
                _index++;
                if (!IsEnd && (Current == '+' || Current == '-'))
                {
                    _index++;
                }

                ConsumeDigits(requireAtLeastOneDigit: true);
            }

            string value = _json.Substring(start, _index - start);
            if (!double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out _))
            {
                Fail($"Invalid number '{value}'.");
            }
        }

        private void ConsumeDigits(bool requireAtLeastOneDigit)
        {
            int start = _index;
            while (!IsEnd && char.IsDigit(Current))
            {
                _index++;
            }

            if (requireAtLeastOneDigit && start == _index)
            {
                Fail("Expected a digit.");
            }
        }

        private void ParseLiteral(string literal)
        {
            for (int literalIndex = 0; literalIndex < literal.Length; literalIndex++)
            {
                if (IsEnd || _json[_index] != literal[literalIndex])
                {
                    Fail($"Expected '{literal}'.");
                }

                _index++;
            }
        }

        private void Expect(char expectedCharacter)
        {
            SkipWhitespace();
            if (IsEnd || _json[_index] != expectedCharacter)
            {
                Fail($"Expected '{expectedCharacter}'.");
            }

            _index++;
        }

        private bool TryConsume(char expectedCharacter)
        {
            SkipWhitespace();
            if (IsEnd || _json[_index] != expectedCharacter)
            {
                return false;
            }

            _index++;
            return true;
        }

        private char Current => _json[_index];

        private void SkipWhitespace()
        {
            while (!IsEnd && char.IsWhiteSpace(_json[_index]))
            {
                _index++;
            }
        }

        private void Fail(string message)
        {
            throw new GameAudioPersistenceException($"Failed to parse project JSON at index {_index}: {message}");
        }
    }
}
