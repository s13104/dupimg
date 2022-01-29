# dupimg

### 概要
* 指定したフォルダ内にある画像ファイルを相互に比較して類似しているかどうか判定します。
* フォルダ内にあるサブフォルダも対象になります。
* 類似性は閾値にて調整可能です。
* 類似していると判定された画像を指定したフォルダへ移動させることができます。

### 書式
~~~
dupimg <target_folder> [-th|--threshold <value>] [-m|--move <dest_folder>] [-cl|cachelist] [-cd|--cachedelete <target_folder>]
~~~
#### \<target_folder\>  
比較する画像が格納されているフォルダのパス
#### -th|--threshold \<value\>  
類似性の閾値1～100(%)を指定する。デフォルトは100（完全一致）
#### -m|--move \<dest_folder\>  
類似していると判定された画像ファイルの移動先フォルダのパス
#### -cl|--cachelist
キャッシュの一覧を表示する
#### -cd|--cachedelete \<target_folder\>  
キャッシュから削除するフォルダのパス

### 使い方
画像が格納されているフォルダを指定します。
~~~
> dupimg C:\Users\Hoge\Pictures
~~~
判定結果が画面に表示されます。--thresholdオプションを指定しないので完全（100%）一致した画像です。
~~~
> dupimg C:\Users\Hoge\Pictures
Proccessing...
（略）
Comparing...
C:\Users\Hoge\Pictures\foo.jpg
C:\Users\Hoge\Pictures\bar.png
C:\Users\Hoge\Pictures\LastYear\foo_copy(1).jpg
~~~
次は、--thresholdオプションを指定し、完全一致ではなく類似画像も探します。
~~~
> dupimg C:\Users\Hoge\Pictures --threshold 95
~~~
判定結果が画面に表示され、類似している画像（bar2.png）が加わりました。
~~~
> dupimg C:\Users\Hoge\Pictures --threshold 95
Proccessing...
（略）
Comparing...
C:\Users\Hoge\Pictures\foo.jpg
C:\Users\Hoge\Pictures\bar.png
C:\Users\Hoge\Pictures\bar2.png
C:\Users\Hoge\Pictures\LastYear\foo_copy(1).jpg
~~~
最後に、類似した画像も含め重複した画像なので、それらを別のフォルダへ移動させます。
~~~
> dupimg C:\Users\Hoge\Pictures --threshold 95 --move D:\tmp\Duplicate
~~~

### 機能詳細
* 画像ファイルの検索や比較はマルチスレッドで実行するため高速です。
* 但し、画像ファイルの総容量やCPU、ストレージ性能に依存します。
* 判定結果には、タイムスタンプが新しいファイル、同じタイムスタンプであれば後に見つかったファイルを表示します。
* 画像ファイルを比較する際にハッシュ化しますが、この処理が一番負荷が高いです。
* そのため、一度ハッシュ化した画像はキャッシュファイルへ保存します。
* 画像ファイルの追加や変更があれば、それに応じキャッシュを更新します。
* 2回目以降の比較はキャッシュを参照するので非常に高速です。
* 非常に高速なので、類似性の閾値を色々変更して試したい場合に便利です。
* --moveオプションを指定して画像を移動させた場合、移動先で移動元のフォルダパスを再現します。

### インストール
#### Windows
* dupimg_win_x64.zipファイルをダウンロードし、適当なフォルダに解凍してください。
#### Linux
* dupimg_linux_x64.zipファイルをダウンロードし、適当なフォルダに解凍してください。

### 依存関係
* CoenM.ImageSharp.ImageHash(1.0.0)
* Microsoft.NETCore.App
* System.Text.Json(5.0.2)

.NET6対応の際に、単一ファイルになりました。

### 開発環境
* Visual Studio 2022
* .NET6

### プラットフォーム
* Windows 10
* Linux (要.NET)
