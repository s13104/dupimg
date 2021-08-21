using System;

namespace CmdLineParser
{
    public interface IValueParser<out T>
    {
        T Parse(string val);
    }

    class ValueParser : IValueParser<string>
    {
        public virtual string Parse(string val)
        {
            return val;
        }
    }

    public abstract class ValueParserConstraint<T> where T : struct, IComparable
    {
        public T MaxValue { get; private set; }
        public T MinValue { get; private set; }

        public ValueParserConstraint(T min, T max)
        {
            SetValue(min, max);
        }

        protected T CheckValue(T val)
        {
            return val.CompareTo(MinValue) == -1 ? MinValue
                : val.CompareTo(MaxValue) == 1 ? MaxValue
                : val;
        }

        protected void SetValue(T min, T max)
        {
            MinValue = min.CompareTo(max) == -1 ? min : max;
            MaxValue = max.CompareTo(min) == 1 ? max : min;
        }
    }

    public class ValueParserDouble : ValueParserConstraint<double>, IValueParser<double>
    {
        public ValueParserDouble(double min, double max) : base(min, max)
        {
        }

        public virtual double Parse(string val)
        {
            return CheckValue(double.TryParse(val, out var tmp) ? tmp : default(double));
        }
    }

    public class ValueParserInteger : ValueParserConstraint<int>, IValueParser<int>
    {
        public ValueParserInteger(int min, int max) : base(min, max)
        {
        }

        public virtual int Parse(string val)
        {
            return CheckValue(int.TryParse(val, out var tmp) ? tmp : default(int));
        }
    }
}
