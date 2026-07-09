using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace ProjectHunt.Data
{
    // Minimal JSON parser adapted for lightweight runtime data loading.
    public static class MiniJson
    {
        public static object Deserialize(string json)
        {
            if (json == null)
            {
                return null;
            }

            return Parser.Parse(json);
        }

        private sealed class Parser : IDisposable
        {
            private const string WordBreak = "{}[],:\"";
            private readonly StringReader _json;

            private Parser(string jsonString)
            {
                _json = new StringReader(jsonString);
            }

            public static object Parse(string jsonString)
            {
                using (var parser = new Parser(jsonString))
                {
                    return parser.ParseValue();
                }
            }

            public void Dispose()
            {
                _json.Dispose();
            }

            private IDictionary<string, object> ParseObject()
            {
                var table = new Dictionary<string, object>();
                _json.Read();

                while (true)
                {
                    switch (NextToken)
                    {
                        case Token.None:
                            return null;
                        case Token.CurlyClose:
                            return table;
                        default:
                            var name = ParseString();
                            if (name == null)
                            {
                                return null;
                            }

                            if (NextToken != Token.Colon)
                            {
                                return null;
                            }

                            _json.Read();
                            table[name] = ParseValue();
                            break;
                    }
                }
            }

            private IList<object> ParseArray()
            {
                var array = new List<object>();
                _json.Read();

                var parsing = true;
                while (parsing)
                {
                    var token = NextToken;
                    switch (token)
                    {
                        case Token.None:
                            return null;
                        case Token.SquaredClose:
                            parsing = false;
                            break;
                        default:
                            array.Add(ParseByToken(token));
                            break;
                    }
                }

                return array;
            }

            private object ParseValue()
            {
                return ParseByToken(NextToken);
            }

            private object ParseByToken(Token token)
            {
                switch (token)
                {
                    case Token.String:
                        return ParseString();
                    case Token.Number:
                        return ParseNumber();
                    case Token.CurlyOpen:
                        return ParseObject();
                    case Token.SquaredOpen:
                        return ParseArray();
                    case Token.True:
                        return true;
                    case Token.False:
                        return false;
                    case Token.Null:
                        return null;
                    default:
                        return null;
                }
            }

            private string ParseString()
            {
                var stringBuilder = new StringBuilder();
                char c;

                _json.Read();

                var parsing = true;
                while (parsing)
                {
                    if (_json.Peek() == -1)
                    {
                        break;
                    }

                    c = NextChar;
                    switch (c)
                    {
                        case '"':
                            parsing = false;
                            break;
                        case '\\':
                            if (_json.Peek() == -1)
                            {
                                parsing = false;
                                break;
                            }

                            c = NextChar;
                            switch (c)
                            {
                                case '"':
                                case '\\':
                                case '/':
                                    stringBuilder.Append(c);
                                    break;
                                case 'b':
                                    stringBuilder.Append('\b');
                                    break;
                                case 'f':
                                    stringBuilder.Append('\f');
                                    break;
                                case 'n':
                                    stringBuilder.Append('\n');
                                    break;
                                case 'r':
                                    stringBuilder.Append('\r');
                                    break;
                                case 't':
                                    stringBuilder.Append('\t');
                                    break;
                                case 'u':
                                    var hex = new char[4];
                                    for (var i = 0; i < 4; i++)
                                    {
                                        hex[i] = NextChar;
                                    }

                                    stringBuilder.Append((char)Convert.ToInt32(new string(hex), 16));
                                    break;
                            }

                            break;
                        default:
                            stringBuilder.Append(c);
                            break;
                    }
                }

                return stringBuilder.ToString();
            }

            private object ParseNumber()
            {
                var number = NextWord;
                if (number.IndexOf('.') == -1)
                {
                    long.TryParse(number, out var parsedInt);
                    return parsedInt;
                }

                double.TryParse(number, out var parsedDouble);
                return parsedDouble;
            }

            private void EatWhitespace()
            {
                while (char.IsWhiteSpace(PeekChar))
                {
                    _json.Read();

                    if (_json.Peek() == -1)
                    {
                        break;
                    }
                }
            }

            private char PeekChar => Convert.ToChar(_json.Peek());

            private char NextChar => Convert.ToChar(_json.Read());

            private string NextWord
            {
                get
                {
                    var word = new StringBuilder();
                    while (!IsWordBreak(PeekChar))
                    {
                        word.Append(NextChar);

                        if (_json.Peek() == -1)
                        {
                            break;
                        }
                    }

                    return word.ToString();
                }
            }

            private Token NextToken
            {
                get
                {
                    EatWhitespace();

                    if (_json.Peek() == -1)
                    {
                        return Token.None;
                    }

                    switch (PeekChar)
                    {
                        case '{':
                            return Token.CurlyOpen;
                        case '}':
                            _json.Read();
                            return Token.CurlyClose;
                        case '[':
                            return Token.SquaredOpen;
                        case ']':
                            _json.Read();
                            return Token.SquaredClose;
                        case ',':
                            _json.Read();
                            return NextToken;
                        case '"':
                            return Token.String;
                        case ':':
                            return Token.Colon;
                        case '0':
                        case '1':
                        case '2':
                        case '3':
                        case '4':
                        case '5':
                        case '6':
                        case '7':
                        case '8':
                        case '9':
                        case '-':
                            return Token.Number;
                    }

                    switch (NextWord)
                    {
                        case "false":
                            return Token.False;
                        case "true":
                            return Token.True;
                        case "null":
                            return Token.Null;
                    }

                    return Token.None;
                }
            }

            private static bool IsWordBreak(char c)
            {
                return char.IsWhiteSpace(c) || WordBreak.IndexOf(c) != -1;
            }

            private enum Token
            {
                None,
                CurlyOpen,
                CurlyClose,
                SquaredOpen,
                SquaredClose,
                Colon,
                String,
                Number,
                True,
                False,
                Null,
            }
        }
    }
}
