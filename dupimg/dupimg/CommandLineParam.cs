using System;
using System.Linq;

namespace CmdLineParser
{
    interface IArgument : ICommandLineParam
    {
        bool Required { get; set; }
    }

    interface ICommandLineParam
    {
        string Description { get; }
        bool HasValue { get; }
        CommandLineName Name { get; }

        bool Verify(string param);
    }

    interface IOption : ICommandLineParam
    {
        bool IsContains { get; }
    }

    interface IParamValue<out T>
    {
        T Value { get; }
    }

    public class CommandLineName
    {
        public char Separator { get; set; } = '|';
        public string[] Tokens { get; private set; }
        public string Value { get; private set; } = string.Empty;

        public CommandLineName(string name)
        {
            SetName(name);
        }

        public bool Contains(string val)
        {
            return Tokens.Contains(val);
        }

        public void SetName(string name)
        {
            Value = name;
            Tokens = name.Split(Separator);
        }
    }

    public abstract class CommandLineParam : ICommandLineParam
    {
        public string Description { get; } = string.Empty;
        public bool HasValue { get; protected set; } = false;
        public CommandLineName Name { get; }

        public CommandLineParam(string name, string desc)
        {
            Name = new CommandLineName(name);
            Description = desc;
        }

        public abstract bool Verify(string param);
    }

    public class Argument : Argument<string>
    {
        public Argument(string name, string desc, bool req) : base(name, desc, req, new ValueParser())
        {
            Value = string.Empty;
        }
    }

    public class Argument<T> : CommandLineParam, IArgument, IParamValue<T>
    {
        private IValueParser<T> _parser;
        public T Value { get; protected set; }
        public bool Required { get; set; } = false;

        public Argument(string name, string desc, bool req, IValueParser<T> parser) : base(name, desc)
        {
            Required = req;
            _parser = parser;
        }

        public override bool Verify(string param)
        {
            if (!HasValue)
            {
                HasValue = true;
                Value = _parser.Parse(param);
            }
            return HasValue;
        }
    }

    public class Command : CommandLineParam, IParamValue<bool>
    {
        public bool Value { get; private set; } = false;

        public Command(string name, string desc) : base(name, desc)
        {
        }

        public override bool Verify(string param)
        {
            if (Name.Contains(param) && !HasValue)
            {
                HasValue = true;
                Value = true;
            }
            return HasValue;
        }
    }

    public class Option : Option<string>
    {
        public Option(string name, string desc, string def = "") : base(name, desc, def, new ValueParser())
        {
        }
    }

    public class Option<T> : CommandLineParam, IOption, IParamValue<T>
    {
        private IValueParser<T> _parser;
        public bool IsContains { get; protected set; } = false;
        public T Value { get; protected set; }

        public Option(string name, string desc, T defVal, IValueParser<T> parser) : base(name, desc)
        {
            _parser = parser;
            Value = defVal;
        }

        public override bool Verify(string param)
        {
            if (Name.Contains(param) && !IsContains)
            {
                IsContains = true;
                return true;
            }
            else
            {
                if (IsContains && !HasValue)
                {
                    HasValue = true;
                    Value = _parser.Parse(param);
                }
            }
            return HasValue;
        }
    }
}
