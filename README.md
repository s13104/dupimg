# dupimg

### 機能
* 指定したフォルダ内にある画像ファイルを相互に比較して類似しているかどうか判定します。
* フォルダ内にあるサブフォルダも対象になります。
* 類似性は閾値にて指定します。
* 処理の結果、類似していると判定された画像を指定したフォルダへ移動させます。

### 書式
~~~
dotnet dupimg.dll <target_folder> [-th|--threshold <value>] [-m|--move <dest_folder>] [-cl|cachelist] [-cd|--cachedelete <target_folder>]
~~~
#### \<target_folder\>  
比較する画像が格納されているフォルダのパス
#### -th|--threshold \<value\>  
類似性の閾値1～100(%)を指定する。デフォルトは100（完全一致）
#### -m|--move \<dest_folder\>  
類似していると判定された画像ファイルの移動先フォルダのパス
#### -cl|--cachelist
キャッシュファイルの一覧表表示する
#### -cd|--cachedelete \<target_folder\>  
キャッシュから削除するフォルダのパス

### 依存関係
* CoenM.ImageSharp.ImageHash(1.0.0)
* Microsoft.NETCore.App(2.1.0)
* System.Text.Json(5.0.2)

### 開発環境
* Visual Studio 2019
* .NET Core 2.1

### プラットフォーム
* Windows 10
* Linux (要.NET)
