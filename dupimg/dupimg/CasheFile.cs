using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Unicode;
using System.Threading.Tasks;

namespace dupimg.CacheFile
{
    interface ICacheData<T> : ICacheSerializable<T>, IEquatable<T>, IComparable<T>
    {
    }

    interface ICacheSerializable<T>
    {
        string Serialize();
        T Deserialize(string serial);
    }

    class CacheManager<TCache, T>
        where TCache : CacheFile<T>, new()
        where T : ICacheData<T>, new()
    {
        private Dictionary<string, string> _settings = new Dictionary<string, string>();
        public string FileName { get; } = string.Empty; //キャッシュ設定ファイル名

        public CacheManager(string filename)
        {
            FileName = filename;
            Load();
        }

        public virtual TCache Create(string key)
        {
            CreateCacheFile(key, out var path);
            var obj = new TCache() { FileName = path };
            obj.Load();
            return obj;
        }

        protected bool CreateCacheFile(string key, out string path)
        {
            //キャッシュ設定ファイルに存在しなければ新規追加
            if (!_settings.TryGetValue(key, out path))
            {
                //キャッシュファイル名は重複しないようにGUIDを使用
                path = $"{Guid.NewGuid():N}.txt";
                _settings.Add(key, path);
                Save();
                return true;
            }
            return false;
        }

        public virtual bool Delete(string key)
        {
            //キャッシュ設定から削除、成功したらキャッシュファイルも削除して設定ファイルを保存
            if (_settings.Remove(key, out var path))
            {
                File.Delete(path);
                Save();
                return true;
            }
            return false;
        }

        public virtual void Load()
        {
            if (File.Exists(FileName))
            {
                var jsonStr = File.ReadAllText(FileName);
                _settings = JsonSerializer.Deserialize<Dictionary<string, string>>(jsonStr);
            }
        }

        public virtual void Save()
        {
            var jsonOption = new JsonSerializerOptions()
            {
                //JSON保存時に日本語の無用なエンコードを避ける
                Encoder = JavaScriptEncoder.Create(UnicodeRanges.All),
                //JSONへの出力を整形
                WriteIndented = true,
            };
            var jsonStr = JsonSerializer.Serialize(_settings, jsonOption);
            File.WriteAllText(FileName, jsonStr);
        }
    }

    class CacheFile<T>
        where T : ICacheData<T>, new()
    {
        private ConcurrentDictionary<T, T> _cache = new ConcurrentDictionary<T, T>();
        public int Count => _cache.Count;
        public string FileName { get; set; } = string.Empty;
        public ICollection<T> Values => _cache.Values;

        public CacheFile()
        {
        }
        public CacheFile(string filename)
        {
            FileName = filename;
        }

        public async Task AddOrUpdateAsync(IEnumerable<T> sequence, Func<T, T> addFactory, Action<Task<T>> progress = null)
        {
            progress = progress ?? (x => { });
            //キャッシュの洗い替えをタスク化
            var tasks = sequence.AsParallel()
                .Select(x =>
                    Task.Run(() =>
                    {
                        return _cache.AddOrUpdate(
                            x,
                            //キャッシュに新規追加の場合、addFactory()を使用してオブジェクトを生成
                            (key) => addFactory(x),
                            //既存の場合、CompareTo()の結果によりaddFactory()で生成したオブジェクトに更新
                            (key, val) => val.CompareTo(x) == 0 ? val : addFactory(x));
                    })
                    //それぞれのタスク完了後、非同期に実行する処理（進捗表示に関する処理を想定）
                    .ContinueWith(task => progress(task), TaskContinuationOptions.RunContinuationsAsynchronously)
                );
            //タスクがすべて完了するまで待機
            await Task.WhenAll(tasks);
        }

        public virtual void Load()
        {
            if (!File.Exists(FileName)) return;
            using (var sr = new StreamReader(FileName))
            {
                var row = string.Empty;
                while ((row = sr.ReadLine()) != null)
                {
                    var obj = new T();
                    _cache.TryAdd(obj, obj.Deserialize(row));
                }
            }
        }

        public virtual void Save()
        {
            var contents = Values.AsParallel().Select(x => $"{x.Serialize()}");
            Save(contents.AsSequential());
        }
        public void Save(IEnumerable<string> sequence)
        {
            File.WriteAllLines(FileName, sequence);
        }
    }
}
