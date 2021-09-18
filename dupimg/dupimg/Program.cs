using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using dupimg.CacheFile;
using SimilarImg;
using SimilarImg.Cache;
using CmdLineParser;

namespace dupimg
{
    class Program
    {
        static void Main(string[] args) => MainAsync(args).Wait();
        static async Task MainAsync(string[] args)
        {
            //コマンドライン引数を解析
            var myArgs = new MyCmdLnParser();
            try
            {
                myArgs.Parse(args);
            }
            catch (Exception e) when (e is DirectoryNotFoundException || e is ArgumentException)
            {
                Console.Error.WriteLine(e.Message);
                return;
            }
            //ヘルプ表示は優先する
            if (myArgs.HasHelp)
            {
                myArgs.ShowHelpText();
                return;
            }

            //キャッシュ設定ファイルの読込み
            var cacheSettings = new CacheManager<SimilarImageCache, HashedImage>("dupimg.cache.json");

            //CacheListスイッチが指定されている場合の処理
            if(myArgs.CacheList.HasValue)
            {
                //キャッシュ設定ファイルの中身を表示する
                foreach (var txt in CacheList(cacheSettings.EnumerateSettings()))
                {
                    Console.WriteLine(txt);
                }
                return;
            }

            //CacheDeleteオプションが指定されている場合の処理
            if (myArgs.CacheDelete.HasValue)
            {
                //キャッシュファイルを削除
                if (cacheSettings.Delete(myArgs.CacheDelete.Value))
                {
                    Console.WriteLine("Cache deleted.");
                }
                return;
            }

            var similar = new SimilarImage(myArgs.Threshold.Value);
            //指定フォルダ内の画像ファイルをハッシュに変換し、キャッシュする。
            Console.WriteLine($"Processing...");
            var cache = await ProcessLoadToCacheAsync(myArgs, similar, cacheSettings);
            //ハッシュ変換時に発生したエラーをコンソールへ通知（内容はエラーファイルへ出力）
            NoticeError(cache);
            //ハッシュ情報を元に画像を総当たり比較する
            Console.WriteLine("Comparing...");
            foreach (var msg in ProcessCompare(myArgs, similar, cache))
            {
                //比較結果をコンソールへ出力
                Console.WriteLine(msg);
            }
            //比較後のキャッシュを保存
            cache.Save();
        }

        /// <summary>
        /// ハッシュ化時にエラーが発生したことを通知する。
        /// </summary>
        /// <param name="similar">類似画像クラス</param>
        static void NoticeError(SimilarImageCache cache)
        {
            //ハッシュ値が0のものはエラーとみなす
            var errors = cache.Values.Where(x => x.HashValue == 0);
            var cnt = errors.Count();
            if (cnt > 0)
            {
                //エラーファイルへ出力する
                var path = "errors.txt";
                File.WriteAllLines(path, errors.Select(x => $"{x.ErrMessage};{x.FullName}"));
                //エラーが発生したことをコンソールへ出力
                Console.Error.WriteLine($"{cnt} file(s) has error. See '{path}'");
            }
        }

        /// <summary>
        /// ハッシュ化した画像を比較して類似画像を移動する。
        /// </summary>
        /// <param name="arg">コマンドライン引数</param>
        /// <param name="similar">類似画像クラス</param>
        /// <param name="cache">ハッシュ化した画像を格納したキャッシュ</param>
        static IEnumerable<string> ProcessCompare(MyCmdLnParser arg, SimilarImage similar, SimilarImageCache cache)
        {
            //キャッシュ内で類似画像の比較を行う
            //総当たりで比較する関係上、重複が発生するのでDistinctで除外
            var compared = cache.Compare(similar).Distinct();
            //Moveオプションの有無で処理を分ける
            return arg.Move.HasValue ?
                //コマンドライン引数にMoveオプションが指定されている場合は実際にファイルを移動する
                compared.Select(obj =>
                {
                    //移動先にも同じフォルダ構成とするため、パス文字列を置換する
                    var dstFullName = obj.FullName.Replace(arg.SrcPath.Value, arg.Move.Value);
                    try
                    {
                        //ファイルを移動する
                        obj.Move(dstFullName);
                        //移動したファイル名を表示する
                        return dstFullName;
                    }
                    catch (DirectoryNotFoundException e)
                    {
                        return e.Message;
                    }
                }) :
                //コマンドライン引数にMoveオプションが指定されていない場合は、移動せず対象となるファイル名を表示する
                compared.Select(obj => obj.FullName);
        }

        /// <summary>
        /// 画像ファイルをハッシュ化してキャッシュに格納する。
        /// </summary>
        /// <param name="arg">コマンドライン引数</param>
        /// <param name="similar">類似画像クラス</param>
        /// <param name="cacheManager">キャッシュ設定</param>
        /// <returns>ハッシュ化した画像を格納したキャッシュ</returns>
        static async Task<SimilarImageCache> ProcessLoadToCacheAsync(MyCmdLnParser arg, SimilarImage similar, CacheManager<SimilarImageCache, HashedImage> cacheManager)
        {
            //キャッシュファイルを読込む
            //新規の場合、比較元フォルダ単位でキャッシュファイルを作成する
            var cache = cacheManager.Create(arg.SrcPath.Value);
            //キャッシュファイルの内容とフォルダを同期しながら画像ファイルをハッシュ化
            //ハッシュ化処理が終了した画像のファイル名をコンソールへ表示する
            await cache.SyncFolderAsync(arg.SrcPath.Value, similar, async x => Console.WriteLine((await x).FullName));
            return cache;
        }

        static IEnumerable<string> CacheList(IEnumerable<KeyValuePair<string, string>> sequence)
        {
            const char separator = '-';
            var name = (Key: "CacheName", Value: "CacheFile");
            //KeyとValueそれぞれで格納されている文字列の最大長を求める
            var length = sequence.Count() > 0
                ? (Key: sequence.Max(x => x.Key.Length), Value: sequence.Max(x => x.Value.Length))
                : (Key: name.Key.Length, Value: name.Value.Length);
            //ヘッダ（カラム名とセパレータ）の生成
            var header = new Dictionary<string, string>()
            {
                {name.Key, name.Value},
                {new string(separator, length.Key), new string(separator, length.Value)}
            };
            //生成したヘッダに引数のシーケンスを結合し
            //桁を揃えるため、求めた最大長の長さになるまで空白を埋めて出力
            return header.Concat(sequence).Select(x => $"{x.Key.PadRight(length.Key)} {x.Value.PadRight(length.Value)}");
        }
    }

    class MyCmdLnParser : CommandLineParser
    {
        public Option CacheDelete { get; }
        public Command CacheList { get; }
        public Option Move { get; }
        public Argument SrcPath { get; }
        public Option<int> Threshold { get; }

        public MyCmdLnParser()
        {
            Name = "dotnet dupimg.dll";
            SrcPath = RegistArgument("SrcPath", "画像ファイルが格納されているフォルダのパス", false);
            CacheList = RegistCommand("-cl|--cachelist", "キャッシュファイルの一覧を表示する");
            Threshold = RegistOption("-th|--threshold", "類似比較の閾値を0～100の間で指定する。省略した場合は100", 100, new ValueParserInteger(0, 100));
            Move = RegistOption("-m|--move", "類似画像の移動先フォルダのパス");
            CacheDelete = RegistOption("-cd|--cachedelete", "指定したキャッシュファイルを削除する");
        }

        public override void Parse(string[] args)
        {
            base.Parse(args);
            if(SrcPath.HasValue)
            {
                //SrcPathが指定されている場合は、フォルダをチェックする
                if (!Directory.Exists(SrcPath.Value))
                {
                    throw new DirectoryNotFoundException($"{SrcPath.Value} not found.");
                }
            }
            if (Move.HasValue)
            {
                //Moveオプションが指定されている場合は、移動先のフォルダをチェックする
                if (!Directory.Exists(Move.Value))
                {
                    throw new DirectoryNotFoundException($"{Move.Value} not found.");
                }
            }
            if (GetParamsCount() == 0)
            {
                //引数が一つも設定されていなければヘルプを表示する
                HasHelp = true;
            }
        }
    }
}
