using System.Globalization;

namespace Axe2DEditor.Core.Rules;

public sealed class FormulaExpressionEvaluator
{
    private readonly string _expression;
    private readonly IReadOnlyDictionary<string, double> _variables;
    private int _position;

    private FormulaExpressionEvaluator(string expression, IReadOnlyDictionary<string, double> variables)
    {
        _expression = expression;
        _variables = variables;
    }

    public static double Evaluate(string expression, IReadOnlyDictionary<string, double> variables)
    {
        if (string.IsNullOrWhiteSpace(expression))
        {
            throw new FormatException("表达式不能为空。");
        }

        var evaluator = new FormulaExpressionEvaluator(expression, variables);
        var value = evaluator.ParseExpression();
        evaluator.SkipWhitespace();
        if (!evaluator.IsAtEnd)
        {
            throw new FormatException("表达式末尾存在无法识别的内容。");
        }

        return value;
    }

    private bool IsAtEnd => _position >= _expression.Length;

    private char Current => IsAtEnd ? '\0' : _expression[_position];

    private double ParseExpression()
    {
        var value = ParseTerm();
        while (true)
        {
            SkipWhitespace();
            if (TryConsume('+'))
            {
                value += ParseTerm();
            }
            else if (TryConsume('-'))
            {
                value -= ParseTerm();
            }
            else
            {
                return value;
            }
        }
    }

    private double ParseTerm()
    {
        var value = ParseFactor();
        while (true)
        {
            SkipWhitespace();
            if (TryConsume('*'))
            {
                value *= ParseFactor();
            }
            else if (TryConsume('/'))
            {
                var divisor = ParseFactor();
                if (Math.Abs(divisor) < double.Epsilon)
                {
                    throw new DivideByZeroException("除数不能为 0。");
                }

                value /= divisor;
            }
            else
            {
                return value;
            }
        }
    }

    private double ParseFactor()
    {
        SkipWhitespace();
        if (TryConsume('+'))
        {
            return ParseFactor();
        }

        if (TryConsume('-'))
        {
            return -ParseFactor();
        }

        return ParsePrimary();
    }

    private double ParsePrimary()
    {
        SkipWhitespace();
        if (TryConsume('('))
        {
            var value = ParseExpression();
            Expect(')');
            return value;
        }

        if (char.IsDigit(Current) || Current == '.')
        {
            return ParseNumber();
        }

        if (IsIdentifierStart(Current))
        {
            var identifier = ParseIdentifier();
            SkipWhitespace();
            if (TryConsume('('))
            {
                return ParseFunction(identifier);
            }

            if (_variables.TryGetValue(identifier, out var value))
            {
                return value;
            }

            throw new KeyNotFoundException($"变量 {identifier} 没有测试值。");
        }

        throw new FormatException("表达式中存在无法识别的字符。");
    }

    private double ParseFunction(string name)
    {
        var args = new List<double>();
        SkipWhitespace();
        if (!TryConsume(')'))
        {
            while (true)
            {
                args.Add(ParseExpression());
                SkipWhitespace();
                if (TryConsume(')'))
                {
                    break;
                }

                Expect(',');
            }
        }

        return name.ToLowerInvariant() switch
        {
            "max" when args.Count >= 2 => args.Max(),
            "min" when args.Count >= 2 => args.Min(),
            "clamp" when args.Count == 3 => Math.Min(Math.Max(args[0], args[1]), args[2]),
            "abs" when args.Count == 1 => Math.Abs(args[0]),
            "round" when args.Count == 1 => Math.Round(args[0]),
            "floor" when args.Count == 1 => Math.Floor(args[0]),
            "ceil" or "ceiling" when args.Count == 1 => Math.Ceiling(args[0]),
            "sqrt" when args.Count == 1 => Math.Sqrt(args[0]),
            "pow" when args.Count == 2 => Math.Pow(args[0], args[1]),
            _ => throw new FormatException($"函数 {name} 的参数数量不正确或暂不支持。")
        };
    }

    private double ParseNumber()
    {
        var start = _position;
        while (char.IsDigit(Current) || Current == '.')
        {
            _position++;
        }

        var text = _expression[start.._position];
        if (double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out var value))
        {
            return value;
        }

        if (double.TryParse(text, NumberStyles.Float, CultureInfo.CurrentCulture, out value))
        {
            return value;
        }

        throw new FormatException($"数字 {text} 无法解析。");
    }

    private string ParseIdentifier()
    {
        var start = _position;
        while (IsIdentifierPart(Current) || Current == '.')
        {
            _position++;
        }

        return _expression[start.._position];
    }

    private void Expect(char expected)
    {
        SkipWhitespace();
        if (!TryConsume(expected))
        {
            throw new FormatException($"缺少字符 {expected}。");
        }
    }

    private bool TryConsume(char value)
    {
        if (Current != value)
        {
            return false;
        }

        _position++;
        return true;
    }

    private void SkipWhitespace()
    {
        while (char.IsWhiteSpace(Current))
        {
            _position++;
        }
    }

    private static bool IsIdentifierStart(char value)
    {
        return char.IsLetter(value) || value == '_';
    }

    private static bool IsIdentifierPart(char value)
    {
        return char.IsLetterOrDigit(value) || value == '_';
    }
}
