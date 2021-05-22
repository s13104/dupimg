using System;
using System.Linq;
using System.Collections.Generic;

namespace dupimg.MyArgs
{
    //コマンドライン引数の解析クラス
    //引数の形式は以下の通り
    //必須引数　　　・・・必須の値 例）ファイル名やパスなど
    //オプション引数・・・何かの閾値や検索の値などを<name> <value>の組み合わせで指定する　例）/t 30
    //スイッチ引数　・・・<name>だけのオンオフで表す　例）--help
    public abstract class ArgsManager
    {
        protected Dictionary<string, string> _options = new Dictionary<string, string>();
        protected Dictionary<string, string> _params = new Dictionary<string, string>();
        protected Dictionary<string, bool> _switches = new Dictionary<string, bool>();

        public virtual bool Analyze(string[] args)
        {
            var name = string.Empty;
            foreach(var arg in args)
            {
                //引数はオプション名か？
                if (IsOption(arg))
                {
                    name = arg;
                }
                else
                {
                    //引数はスイッチか？
                    if (IsSwitch(arg))
                    {
                        SetSwitchValue(arg, true);
                    }
                    else
                    {
                        //オプション名が設定されているか？
                        if (name != "")
                        {
                            SetOptionValue(name, arg);
                        }
                        else //オプションでもなくスイッチでもなければ必須パラメータとみなす
                        {
                            SetParamValue(arg);
                        }
                    }
                    name = "";
                }
            }

            //必須パラメータは全て指定されているか？
            if (!IsParamComplete())
            {
                throw new ArgumentException("引数が指定されていません。");
            }
            return true;
        }

        protected string GetOptionValue(string name)
        {
            return _options[name];
        }

        protected string GetParamValue(string name)
        {
            return _params[name];
        }

        protected bool GetSwitchValue(string name)
        {
            return _switches[name];
        }

        protected bool IsOption(string arg)
        {
            return _options.ContainsKey(arg);
        }

        protected virtual bool IsParamComplete()
        {
            return _params.Where(x => x.Value == "").Count() == 0;
        }

        protected bool IsSwitch(string arg)
        {
            return _switches.ContainsKey(arg);
        }

        //オプション引数を登録する
        protected void RegisterOptions(params string[] names)
        {
            _options.AddKeys(names);
        }

        //必須引数を登録する
        protected void RegisterParams(params string[] names)
        {
            _params.AddKeys(names);
        }

        //スイッチ引数を登録する
        protected void RegisterSwitches(params string[] names)
        {
            _switches.AddKeys(names);
        }

        protected void SetOptionValue(string name, string val)
        {
            _options[name] = val;
        }

        protected void SetParamValue(string val) //未設定から順番にセットする
        {
            var key = _params.First(x => x.Value == "").Key;
            _params[key] = val;
        }
        protected void SetParamValue(string name, string val)
        {
            _params[name] = val;
        }

        protected void SetSwitchValue(string name, bool val)
        {
            _switches[name] = val;
        }
    }

    //Dictionary<TKey, TValue>の拡張メソッド
    static class DictionaryEx
    {
        public static void AddKeys(this Dictionary<string, string> pairs, params string[] keys)
        {
            foreach (var val in keys)
            {
                pairs.Add(val, "");
            }
        }
        public static void AddKeys(this Dictionary<string, bool> pairs, params string[] keys)
        {
            foreach (var val in keys)
            {
                pairs.Add(val, false);
            }
        }
    }
}
