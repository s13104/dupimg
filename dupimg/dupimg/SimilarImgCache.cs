using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using dupimg.CacheFile;

namespace SimilarImg.Cache
{
    class SimilarImageCache : CacheFile<HashedImage>
    {
        public string Pattern { get; set; } = "*.*";

        public SimilarImageCache() : base() { }

        public IEnumerable<HashedImage> Compare(SimilarImage similar)
        {
            var skip = 0;
            var cache = Values.Where(x => x.HashValue > 0);
            foreach (var anchor in cache)
            {
                //比較回数を減らすために検索済みは先頭から順にスキップしていく
                ++skip;
                //類似画像を抽出
                foreach (var obj in cache.Skip(skip).AsParallel().Where(x => similar.Comparer.Equals(anchor, x)))
                {
                    //どちらが除外対象か比較（新しい画像が除外対象）
                    yield return anchor.CompareTo(obj) <= 0 ? obj : anchor;
                }
            }
        }

        public IEnumerable<FileInfo> EnumerateFiles(string path)
        {
            if (!Directory.Exists(path))
            {
                throw new DirectoryNotFoundException();
            }
            var dirInfo = new DirectoryInfo(path);
            var options = new EnumerationOptions() { RecurseSubdirectories = true, };
            return dirInfo.EnumerateFiles(Pattern, options);
        }

        public override void Save()
        {
            //キャッシュの内容を画像ファイルが存在しているものだけにする
            var contents = Values.AsParallel()
                .Where(x => File.Exists(x.FullName) && x.HashValue > 0)
                .Select(x => x.Serialize());
            Save(contents.AsSequential());
        }

        public async Task SyncFolderAsync(string path, SimilarImage similar, Action<Task<HashedImage>> progress = null)
        {
            //画像ファイルからインスタンス生成する処理を並列に実行する
            var objects = EnumerateFiles(path).AsParallel().Select(x => new HashedImage(x));
            //非同期にハッシュ化処理を実行する
            await AddOrUpdateAsync(objects, x => x.LoadTo(similar.Argorithm), progress);
        }
    }
}
