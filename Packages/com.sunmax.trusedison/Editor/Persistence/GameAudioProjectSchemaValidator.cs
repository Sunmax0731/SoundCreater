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
            RequireString(root, "$", "toolVersion");

            GameAudioJsonNode project = RequireObjectProperty(root, "$", "project");
            ValidateProject(project, "$.project");
        }

        private static void ValidateProject(GameAudioJsonNode node, string path)
        {
            RequireString(node, path, "id");
            RequireString(node, path, "name");
            RequireNumber(node, path, "bpm");
            GameAudioJsonNode timeSignature = RequireObjectProperty(node, path, "timeSignature");
            RequireNumber(timeSignature, $"{path}.timeSignature", "numerator");
            RequireNumber(timeSignature, $"{path}.timeSignature", "denominator");
            RequireNumber(node, path, "totalBars");
            RequireNumber(node, path, "sampleRate");
            RequireString(node, path, "channelMode");
            RequireNumber(node, path, "masterGainDb");
            RequireBoolean(node, path, "loopPlayback");

            GameAudioJsonNode tracks = RequireArrayProperty(node, path, "tracks");
            for (int index = 0; index < tracks.Items.Count; index++)
            {
                ValidateTrack(tracks.Items[index], $"{path}.tracks[{index}]");
            }
        }

        private static void ValidateTrack(GameAudioJsonNode node, string path)
        {
            RequireObject(node, path);
            RequireString(node, path, "id");
            RequireString(node, path, "name");
            RequireBoolean(node, path, "mute");
            RequireBoolean(node, path, "solo");
            RequireNumber(node, path, "volumeDb");
            RequireNumber(node, path, "pan");
            ValidateVoice(RequireObjectProperty(node, path, "defaultVoice"), $"{path}.defaultVoice");

            GameAudioJsonNode notes = RequireArrayProperty(node, path, "notes");
            for (int index = 0; index < notes.Items.Count; index++)
            {
                ValidateNote(notes.Items[index], $"{path}.notes[{index}]");
            }
        }

        private static void ValidateNote(GameAudioJsonNode node, string path)
        {
            RequireObject(node, path);
            RequireString(node, path, "id");
            RequireNumber(node, path, "startBeat");
            RequireNumber(node, path, "durationBeat");
            RequireNumber(node, path, "midiNote");
            RequireNumber(node, path, "velocity");

            if (TryGetProperty(node, "voiceOverride", out GameAudioJsonNode voiceOverride) && voiceOverride.Kind != GameAudioJsonKind.Null)
            {
                ValidateVoice(voiceOverride, $"{path}.voiceOverride");
            }
        }

        private static void ValidateVoice(GameAudioJsonNode node, string path)
        {
            RequireObject(node, path);
            RequireString(node, path, "waveform");
            RequireNumber(node, path, "pulseWidth");
            RequireBoolean(node, path, "noiseEnabled");
            RequireString(node, path, "noiseType");
            RequireNumber(node, path, "noiseMix");
            ValidateEnvelope(RequireObjectProperty(node, path, "adsr"), $"{path}.adsr");
            ValidateEffect(RequireObjectProperty(node, path, "effect"), $"{path}.effect");
        }

        private static void ValidateEnvelope(GameAudioJsonNode node, string path)
        {
            RequireObject(node, path);
            RequireNumber(node, path, "attackMs");
            RequireNumber(node, path, "decayMs");
            RequireNumber(node, path, "sustain");
            RequireNumber(node, path, "releaseMs");
        }

        private static void ValidateEffect(GameAudioJsonNode node, string path)
        {
            RequireObject(node, path);
            RequireNumber(node, path, "volumeDb");
            RequireNumber(node, path, "pan");
            RequireNumber(node, path, "pitchSemitone");
            RequireNumber(node, path, "fadeInMs");
            RequireNumber(node, path, "fadeOutMs");
            ValidateDelay(RequireObjectProperty(node, path, "delay"), $"{path}.delay");
        }

        private static void ValidateDelay(GameAudioJsonNode node, string path)
        {
            RequireObject(node, path);
            RequireBoolean(node, path, "enabled");
            RequireNumber(node, path, "timeMs");
            RequireNumber(node, path, "feedback");
            RequireNumber(node, path, "mix");
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
