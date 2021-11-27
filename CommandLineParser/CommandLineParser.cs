namespace CommandLineParser;
    public class CommandLineParser
    {
        private IEnumerable<string> _args;
        private List<IArgument> _arguments = new List<IArgument>();
        private List<Command> _commands = new List<Command>();
        private Command _help = new Command("-?|-h|--help", "Show help information");
        private List<IOption> _options = new List<IOption>();
        public string[] Args => _args.ToArray();
        public bool HasHelp { get; protected set; } = false;
        public string Name { get; set; } = string.Empty;

        private void _showHelpParameter(string title, IEnumerable<ICommandLineParam> @params)
        {
            Console.WriteLine();
            Console.WriteLine(title);
            foreach (var obj in @params)
            {
                Console.WriteLine($"{obj.Name.Value}{(obj is IOption ? " <value>" : "")}\t{obj.Description}");
            }
        }

        public int GetParamsCount()
        {
            return _arguments.Count + _commands.Count + _options.Count;
        }

        protected virtual bool IsArgumentsComplete()
        {
            return _arguments.Where(x => x.Required && !x.HasValue).Count() > 0;
        }

        public virtual void Parse(string[] args)
        {
            //ヘルプ出力かどうか判定する
            if(args.Count(x => _help.Verify(x)) > 0)
            {
                HasHelp = _help.HasValue;
                return;
            }
            //Commandかどうか判定する
            _args = VerifyCommands(args);
            //Optionかどうか判定する
            _args = VerifyOptions(_args);
            //残りの引数がArgumentかどうか判定する
            VerifyArguments(_args);
        }

        public Argument RegistArgument(string desc, bool req)
        {
            return RegistArgument($"arg{_arguments.Count + 1}", desc, req);
        }
        public Argument RegistArgument(string name, string desc, bool req)
        {
            var obj = new Argument(name, desc, req);
            _arguments.Add(obj);
            return obj;
        }

        public Argument<T> RegistArgument<T>(string desc, bool req, IValueParser<T> parser)
        {
            return RegistArgument($"arg{_arguments.Count + 1}", desc, req, parser);
        }
        public Argument<T> RegistArgument<T>(string name, string desc, bool req, IValueParser<T> parser)
        {
            var obj = new Argument<T>(name, desc, req, parser);
            _arguments.Add(obj);
            return obj;
        }

        public Command RegistCommand(string name, string desc)
        {
            _commands.Add(new Command(name, desc));
            return _commands.Last();
        }

        public Option RegistOption(string name, string desc)
        {
            var obj = new Option(name, desc);
            _options.Add(obj);
            return obj;
        }

        public Option<T> RegistOption<T>(string name, string desc, T defVal, IValueParser<T> parser)
        {
            var obj = new Option<T>(name, desc, defVal, parser);
            _options.Add(obj);
            return obj;
        }

        public void ShowHelpText()
        {
            //Usage段落
            Console.WriteLine();
            var usage = $"Usage: {Name}";
            var required = _arguments.Count(x => x.Required) > 0 ? " " + string.Join(" ", _arguments.Where(x => x.Required).Select(x => x.Name.Value)) : "";
            var arguments = _arguments.Count(x => !x.Required) > 0 ? " [arguments]" : "";
            var commands = _commands.Count() > 0 ? " [commands]" : "";
            var options = _options.Count() > 0 ? " [options]" : "";
            Console.WriteLine($"{usage}{required}{arguments}{commands}{options}");
            //Arguments段落
            if(_arguments.Count() > 0)
            {
                _showHelpParameter("Arguments:", _arguments);
            }
            //Commands段落
            if(!string.IsNullOrEmpty(commands))
            {
                _showHelpParameter("Commands:", _commands.Select(x => x).Append(_help).OrderBy(x => x.Name.Value));
            }
            //Options段落
            if (!string.IsNullOrEmpty(options))
            {
                _showHelpParameter("Options:", _options);
            }
            Console.WriteLine();
        }

        protected void VerifyArguments(IEnumerable<string> args)
        {
            //シーケンスの要素数を取得
            var countArgs = args.Count();
            //規程数以上のArgumentが設定されていたら例外発生
            if (countArgs > _arguments.Count())
            {
                throw new ArgumentException("Too many parameters.");
            }
            //Argumentをargsの先頭から順に割り当てる
            //割当てに失敗したら例外発生
            if (countArgs > 0)
            {
                var i = -1;
                if (args.Where(arg => { ++i; return _arguments.ElementAt(i).Verify(arg); }).Count() == 0)
                {
                    throw new ArgumentException("Invalid arguments.", args.ElementAt(i));
                }
            }
            //必須Argumentが指定されているか？
            //指定されていなければ例外発生
            if (IsArgumentsComplete())
            {
                throw new ArgumentException("Required Argument is not set.", _arguments.Where(x => x.Required & !x.HasValue).First().Name.Value);
            }
        }

        protected IEnumerable<string> VerifyCommands(IEnumerable<string> args)
        {
            return args.Where(arg => _commands.Where(x => !x.HasValue && x.Verify(arg)).Count() == 0);
        }

        protected IEnumerable<string> VerifyOptions(IEnumerable<string> args)
        {
            var result = new List<string>();
            var name = string.Empty;
            foreach (var arg in args)
            {
                if (_options.Where(x => !x.IsContains && x.Verify(arg)).Count() > 0)
                {
                    name = arg;
                    continue;
                }
                else
                {
                    if (!string.IsNullOrEmpty(name))
                    {
                        if (_options.Where(x => !x.HasValue && x.Name.Contains(name)).First().Verify(arg))
                        {
                            name = string.Empty;
                            continue;
                        }
                    }
                }
                result.Add(arg);
            }
            return result;
        }
    }
