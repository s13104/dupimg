# dupimg

### 機能
* 指定したフォルダ内にある画像ファイルを相互に比較して類似しているかどうか判定します。
* フォルダ内にあるサブフォルダも対象になります。
* 類似性は閾値にて指定します。
* 処理の結果、類似していると判定された画像を指定したフォルダへ移動させます。

### 書式
~~~
dotnet dupimg.dll <target_folder> [/t <threshorld>] [/move <dest_folder>] [/delete <target_folder>]
~~~
#### \<target_folder\>  
比較する画像が格納されているフォルダのパス
#### /t \<threshold\>  
類似性の閾値1～100(%)を指定する。デフォルトは100（完全一致）
#### /move \<dest_folder\>  
類似していると判定された画像ファイルの移動先フォルダのパス
#### /delete \<target_folder\>  
キャッシュから削除するフォルダのパス

