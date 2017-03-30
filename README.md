# Tvmaid YUI バージョン 1.7a のmark10alsに依る改造版

## オリジナル
Tvmaidのサイト(http://nana2.sarashi.com/)で公開されていた"Tvmaid YUI 1.7a"です。  
残念ながらオリジナルは開発終了となり、現在は公開されていません。  
現在は、タッチデバイス用のUIとストリーミングに対応した Tvmaid MAYA が公開されています。  

## 改造版
Version は、 "Tvmaid YUI 1.7a mod by mark10als at リリース日付" としています。  
これはTvmaid YUI バージョン 1.7a に自分用に手を加えたものです。  

## 注意！
これは、C#をはじめて見た超初心者が、グーグル先生に聞いて
手探りで改造したものです。  
どんなことが起こっても責任は取りません。  絶対に！  
自己責任で人柱になっても良いという方のみお使いください。  

## 謝辞
Tvmaidについて語るスレ(http://echo.2ch.net/test/read.cgi/avi/1453201688/)  
スレの>>76 にて、maidの中の人 ◆dGJLJP3RhQ さんが改造版の公開を
許可してくださり、ありがとうございました。  
そして、>>346,>>403にてビルドできるソースを公開してくださった
有志の方にも、お礼を申し上げます。  
さらに、>>417,>>418にて説明とアドバイスを頂きありがとうございました。  

おかげさまで、以下の改造を行うことができました。  
１．以前搭載されていた「予備録画フォルダの設定」を復活させました。  
　　>>76 にて、maidの中の人は、否定的でしたが、「録り逃しを防ぐ機能」の  
　　一つの対策としては、私は有用であると思い復活させました。  
２．自動予約録画に最小時間と最大時間を設定して、条件に該当するものを  
　　無効として予約登録させました。  
３．ファイル名の拡張変更を追加した。  
　　バッチスクリプトでファイル名を操作するときに、不具合のある文字を  
　　幾つか変更するようにした。  
　　半角の%と&は、全角に変換  
　　半角と全角の空白は、半角の「_」（アンダーバー）に変換  

## 使用方法
１．オリジナルのTvmaid YUI 1.7aのフォルダーにビルドした  
　　中のファイルを上書きします。  
２．userフォルダーのmain.defファイルに以下の項目を追加してください。  
　　項目の値は各自の環境に合わせて調整してください。  
-----------  
record.folder.spare=I:\TV-Capcher-2nd\  
record.folder.spare.use=30  
record.margin.overlap=0  
record.minimal.minute=15  
record.maximum.minute=150  
extend.name.change=1  
-----------  
　　record.margin.overlapに「0」を設定すると録画マージンの重複を許可します。  
　　extend.name.changeに「0」を設定するとファイル名の拡張変更は、無効になります。  

## 参照
http://nana2.sarashi.com/tvmaid-yui/  
http://www1.axfc.net/u/3637277  

## テスト環境
* TvTest_0.7.23fix
* PT3
* PX-Q3PE
* PX-W3U3V2

## 開発環境
* Microsoft Windows 7 Professional 64bit
* Microsoft Visual Studio Express 2015 for Windows Desktop

## ライセンス
オリジナルに準じます。  

