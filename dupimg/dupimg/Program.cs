using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using dupimg.MyArgs;
using dupimg.CacheFile;
using SimilarImg;
using SimilarImg.Cache;

namespace dupimg
{
    class Program
    {
        static void Main(string[] args) => MainAsync(args).Wait();
        static async Task MainAsync(string[] args)
        {
            //コマンドライン引数を解析
            var myArgs = new Arguments();
            try
            {
                myArgs.Analyze(args);
            }
            catch(ArgumentException e)
            {
                Console.WriteLine(e.Message);
                return;
            }

            //キャッシュ設定ファイルの読込み
            var cacheSettings = new CacheManager<SimilarImageCache, HashedImage>("dupimg.cache.json");

            //CacheListスイッチが指定されている場合の処理
            if(myArgs.IsCacheList)
            {
                //キャッシュ設定ファイルの中身を表示する
                foreach(var txt in CacheList(cacheSettings.EnumerateSettings())) Console.WriteLine(txt);
                return;
            }

            //CacheDeleteオプションが指定されている場合の処理
            if (myArgs.IsCacheDelete)
            {
                //キャッシュファイルを削除
                if (cacheSettings.Delete(myArgs.CacheName)) Console.WriteLine("Cache deleted.");
                return;
            }

            var similar = new SimilarImage(myArgs.Threshold);
            //指定フォルダ内の画像ファイルをハッシュに変換し、キャッシュする。
            var cache = await ProcessLoadToCacheAsync(myArgs, similar, cacheSettings);
            //ハッシュ変換時に発生したエラーを通知（コンソールへ出力）
            NoticeError(cache);
            //ハッシュ情報を元に画像を総当たり比較する
            ProcessCompare(myArgs, similar, cache);
            //比較結果をキャッシュに反映
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
                Console.WriteLine($"{cnt} file(s) has error. See '{path}'");
            }
        }

        /// <summary>
        /// ハッシュ化した画像を比較して類似画像を移動する。
        /// </summary>
        /// <param name="arg">コマンドライン引数</param>
        /// <param name="similar">類似画像クラス</param>
        /// <param name="cache">ハッシュ化した画像を格納したキャッシュ</param>
        static void ProcessCompare(Arguments arg, SimilarImage similar, SimilarImageCache cache)
        {
            Console.WriteLine("Comparing...");
            //キャッシュ内で類似画像の比較を行う
            //総当たりで比較する関係上、重複が発生するのでDistinctで除外
            var compared = cache.Compare(similar).Distinct();
            //Moveオプションの有無で処理を分ける
            var messages = arg.IsMove ?
                //コマンドライン引数にMoveオプションが指定されている場合は実際にファイルを移動する
                compared.Select(obj =>
                {
                    //移動先にも同じフォルダ構成とするため、パス文字列を置換する
                    var dstFullName = obj.FullName.Replace(arg.SrcPath, arg.DstPath);
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
                //コマンドライン引数にMoveスイッチが指定されていない場合は、移動せず対象となるファイル名を表示する
                compared.Select(obj => obj.FullName);
            //比較結果をコンソールへ出力
            foreach (var msg in messages) Console.WriteLine(msg);
        }

        /// <summary>
        /// 画像ファイルをハッシュ化してキャッシュに格納する。
        /// </summary>
        /// <param name="arg">コマンドライン引数</param>
        /// <param name="similar">類似画像クラス</param>
        /// <param name="cacheManager">キャッシュ設定</param>
        /// <returns>ハッシュ化した画像を格納したキャッシュ</returns>
        static async Task<SimilarImageCache> ProcessLoadToCacheAsync(Arguments arg, SimilarImage similar, CacheManager<SimilarImageCache, HashedImage> cacheManager)
        {
            Console.WriteLine($"Processing...");
            //キャッシュファイルを読込む
            //新規の場合、比較元フォルダ単位でキャッシュファイルを作成する
            var cache = cacheManager.Create(arg.SrcPath);
            //キャッシュファイルの内容とフォルダを同期しながら画像ファイルをハッシュ化
            //ハッシュ化処理が終了した画像のファイル名をコンソールへ表示する
            await cache.SyncFolderAsync(arg.SrcPath, similar, async x => Console.WriteLine((await x).FullName));
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

    class Arguments : ArgsManager
    {
        private const string nameCacheDelete = "/cachedelete";
        private const string nameCacheList = "/cachelist";
        private const string nameMove = "/move";
        private const string nameSrcPath = "SrcPath";
        private const string nameThreshold = "/th";
        public string CacheName => GetOptionValue(nameCacheDelete);
        public string DstPath => GetOptionValue(nameMove);
        public bool IsCacheDelete => !string.IsNullOrEmpty(CacheName);
        public bool IsCacheList => GetSwitchValue(nameCacheList);
        public bool IsMove => !string.IsNullOrEmpty(DstPath);
        public string SrcPath => GetParamValue(nameSrcPath);
        public double Threshold => double.TryParse(GetOptionValue(nameThreshold), out var ret) ? ret : 100;

        public Arguments()
        {
            //コマンドライン引数を定義する
            RegisterParams(nameSrcPath);
            RegisterSwitches(nameCacheList);
            RegisterOptions(nameThreshold, nameMove, nameCacheDelete);
        }

        public override bool Analyze(string[] args)
        {
            base.Analyze(args);
            if (!IsCacheList && !IsCacheDelete)
            {
                if (!Directory.Exists(SrcPath)) throw new DirectoryNotFoundException($"{SrcPath} not found.");
                if (IsMove)
                {
                    //Moveオプションが指定されている場合は、移動先のフォルダをチェックする
                    if (!Directory.Exists(DstPath)) throw new DirectoryNotFoundException($"{DstPath} not found.");
                }
            }
            return true;
        }

        protected override bool IsParamComplete()
        {
            //CacheListスイッチあるいはCacheDeleteオプションが指定されているときは必須パラメータは無視する
            if(IsCacheList || IsCacheDelete)
            {
                return true;
            }
            return base.IsParamComplete();
        }
    }
}
