using System;
using System.Collections.Generic;
using System.IO;
using CoenM.ImageHash;
using CoenM.ImageHash.HashAlgorithms;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using dupimg.CacheFile;

namespace SimilarImg
{
    /// <summary>
    /// ハッシュ化した画像の情報を扱うクラス
    /// </summary>
    /// <remarks>
    /// <para>ハッシュ値以外に次の情報も格納する。</para>
    /// <para>画像ファイルのフルパス</para>
    /// <para>画像ファイルのタイムスタンプ（チック値）</para>
    /// </remarks>
    class HashedImage : ICacheData<HashedImage> 
    {
        private const string DELIMITER = ";";
        public string FullName { get; protected set; } = string.Empty;
        public ulong HashValue { get; protected set; } = 0;
        public string ErrMessage { get; protected set; } = string.Empty;
        public long Timestamp { get; protected set; } = 0;

        public HashedImage()
        {
        }
        public HashedImage(FileInfo fi)
        {
            Init(fi);
        }

        public int CompareTo(HashedImage other)
        {
            return Timestamp.CompareTo(other.Timestamp);
        }

        public HashedImage Deserialize(string serial)
        {
            try
            {
                var splited = serial.Split(DELIMITER);
                FullName = splited[0];
                Timestamp = long.TryParse(splited[1], out var ts) ? ts : 0;
                HashValue = ulong.TryParse(splited[2], out var hv) ? hv : 0;
                ErrMessage = string.Empty;
            }
            catch (Exception e)
            {
                ErrMessage = e.Message;
            }
            return this;
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as HashedImage);
        }

        public bool Equals(HashedImage other)
        {
            return other != null &&
                   FullName == other.FullName;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(FullName);
        }

        public HashedImage Init(FileInfo fi)
        {
            FullName = fi?.FullName ?? string.Empty;
            Timestamp = fi?.CreationTime.ToUniversalTime().Ticks ?? 0;
            return this;
        }

        protected virtual Image<Rgba32> LoadImage(string path)
        {
            ErrMessage = string.Empty;
            try
            {                
                return Image.Load<Rgba32>(path);
            }
            catch (UnknownImageFormatException)
            {
                ErrMessage = $"Unknown format";
            }
            catch (InvalidImageContentException)
            {
                ErrMessage = $"Contains invalid content";
            }
            catch (Exception e)
            {
                ErrMessage = $"{e.Message}";
            }
            return null;
        }

        public HashedImage LoadTo(IImageHash argorithm)
        {
            using (var img = LoadImage(FullName))
            {
                //ハッシュ化できなかった場合は0を設定する
                HashValue = img != null ? argorithm.Hash(img) : 0;
            }
            return this;
        }

        public string Serialize()
        {
            return $"{FullName}{DELIMITER}{Timestamp}{DELIMITER}{HashValue}";
        }
    }

    abstract class HashedImageComparer<T> : EqualityComparer<T>
    {
        private const double maxThreshold = 100;
        private const double minThreshold = 0;
        public double Threshold { get; }

        public HashedImageComparer(double threshold)
        {
            Threshold = threshold >= maxThreshold ? maxThreshold :
                        threshold <= minThreshold ? minThreshold :
                        threshold;
        }
    }

    class HashedImageComparer : HashedImageComparer<HashedImage>
    {
        public HashedImageComparer(double threshold) : base(threshold)
        {
        }

        public override bool Equals(HashedImage x, HashedImage y)
        {
            var similar = CompareHash.Similarity(x.HashValue, y.HashValue);
            return similar >= Threshold;
        }

        public override int GetHashCode(HashedImage obj)
        {
            return obj.GetHashCode();
        }
    }

    //HashedImageの拡張クラス
    static class HashedImageEx
    {
        //ファイルを指定した先へ移動する
        public static void Move(this HashedImage obj, string dst)
        {
            if (File.Exists(obj.FullName))
            {
                var dirName = Path.GetDirectoryName(dst);
                Directory.CreateDirectory(dirName);
                //.NET Core 2.2以前ではMoveメソッドに上書きオプションがない。
                //そのため例外を発生させないよう事前に移動先のファイルを削除する。
                //Deleteメソッドは対象ファイルが存在しない場合でも例外をスローしない。
                File.Delete(dst);
                File.Move(obj.FullName, dst);
            }
        }
    }

    class SimilarImage<T>
    {
        public IImageHash Argorithm { get; set; } = new PerceptualHash();
        public HashedImageComparer<T> Comparer { get; }

        public SimilarImage(HashedImageComparer<T> comparer)
        {
            Comparer = comparer;
        }
        public SimilarImage(HashedImageComparer<T> comparer, IImageHash argo) : this(comparer)
        {
            Argorithm = argo;
        }
    }

    class SimilarImage : SimilarImage<HashedImage>
    {
        public SimilarImage(double threshold) : base(new HashedImageComparer(threshold))
        {
        }
    }
}
