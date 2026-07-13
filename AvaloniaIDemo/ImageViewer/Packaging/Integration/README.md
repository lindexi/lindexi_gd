# 鍥剧墖鏌ョ湅鍣ㄧ郴缁熼泦鎴愭柟妗?

鏈洰褰曟彁渚?Windows 涓?Linux 妗岄潰绯荤粺闆嗘垚鐨勫彲瀹￠槄鏂规鍜岃剼鏈ā鏉裤€傝剼鏈笉浼氳鏋勫缓鑷姩璋冪敤锛涢粯璁ゅ彧杈撳嚭璁″垝锛屽繀椤绘樉寮忎紶鍏?`-Apply` 鎴?`--apply` 鎵嶄細淇敼鎸佷箙鐢ㄦ埛绾х郴缁熻缃€?

## 涓庡簲鐢ㄥ惎鍔ㄦ満鍒剁殑琛旀帴

褰撳墠搴旂敤宸茬粡鍏峰鏂囦欢鍏宠仈鎵€闇€鐨勫惎鍔ㄨ涓猴細

- `Code/Program.cs` 灏嗗師濮?`args` 浼犵粰 Avalonia 妗岄潰鐢熷懡鍛ㄦ湡銆?
- `Code/MainWindow.axaml.cs` 鍦ㄧ獥鍙ｆ瀯閫犳椂璇诲彇棣栦釜闈炵┖鍚姩鍙傛暟骞惰皟鐢ㄥ浘鐗囧姞杞介€昏緫銆?
- `Code/SingleInstanceCoordinator.cs` 鍦ㄤ簩娆″惎鍔ㄦ椂鎶婇涓潪绌哄弬鏁拌浆鍙戠粰宸叉湁瀹炰緥锛屽凡鏈夌獥鍙ｅ啀璋冪敤 `OpenFromExternalInstance` 鎵撳紑璇ヨ矾寰勩€?
- `Code/Services/ImageDirectoryService.cs` 鏀寔 `.jpg`銆乣.jpeg`銆乣.png`銆乣.bmp`銆乣.gif`銆乣.webp`銆乣.tif`銆乣.tiff`銆?

鍥犳锛屾枃浠跺叧鑱斿懡浠ゅ繀椤婚噰鐢ㄢ€滃彲鎵ц鏂囦欢 + 鍥剧墖璺緞鈥濈殑褰㈠紡锛?

- Windows: `"ImageViewer.exe" "%1"`
- Linux: `Exec="/path/to/ImageViewer" %U`

## Windows 鏂规

鑴氭湰锛歚Windows/Register-ImageViewerFileAssociations.ps1`

### 鐩爣

- 娉ㄥ唽 per-user ProgID锛歚ImageViewer.ImageFile`銆?
- 灏嗗簲鐢ㄥ姞鍏ュ父瑙佸浘鐗囨墿灞曞悕鐨?`OpenWithProgids` / `OpenWithList`銆?
- 涓哄父瑙佸浘鐗囨墿灞曞悕娣诲姞鍙抽敭鑿滃崟鍔ㄨ瘝锛歚浣跨敤鍥剧墖鏌ョ湅鍣ㄦ墦寮€`銆?
- 娉ㄥ唽鎵撳紑鍛戒护锛歚"<ImageViewer.exe>" "%1"`銆?
- 鏀寔鍗歌浇/鍥炴粴 ImageViewer 鍐欏叆鐨?HKCU 椤广€?

### 鏀寔鎵╁睍鍚?

`.jpg`銆乣.jpeg`銆乣.png`銆乣.bmp`銆乣.gif`銆乣.webp`銆乣.tif`銆乣.tiff`

### 浣跨敤鏂瑰紡

棰勮灏嗚鍐欏叆鐨勬敞鍐岃〃椤癸紝涓嶄慨鏀圭郴缁燂細

```powershell
pwsh ./Windows/Register-ImageViewerFileAssociations.ps1 -ExecutablePath "C:\Users\me\AppData\Local\ImageViewer\ImageViewer.exe"
```

搴旂敤鍒板綋鍓嶇敤鎴凤細

```powershell
pwsh ./Windows/Register-ImageViewerFileAssociations.ps1 -ExecutablePath "C:\Users\me\AppData\Local\ImageViewer\ImageViewer.exe" -Apply
```

鍗歌浇/鍥炴粴褰撳墠鐢ㄦ埛涓嬬敱鑴氭湰鍒涘缓鐨勫叧鑱旓細

```powershell
pwsh ./Windows/Register-ImageViewerFileAssociations.ps1 -Uninstall -Apply
```

### 榛樿鍥剧墖鏌ョ湅鍣ㄨ鏄?

Windows 10/11 瀵归粯璁ゅ簲鐢ㄩ€夋嫨鏈夊搱甯屼繚鎶わ紝搴旂敤鎴栬剼鏈笉搴旈潤榛樺己鍒剁鏀圭敤鎴烽粯璁ゅ簲鐢ㄣ€傛帹鑽愭祦绋嬶細

1. 瀹夎鎴栧彂甯冩祦绋嬭皟鐢ㄨ剼鏈敞鍐屽簲鐢ㄨ兘鍔涘拰 Open With 鍊欓€夐」銆?
2. 鐢ㄦ埛鍦ㄢ€滆缃?-> 搴旂敤 -> 榛樿搴旂敤鈥濅腑閫夋嫨鍥剧墖鏌ョ湅鍣ㄤ綔涓洪粯璁ゅ簲鐢紝鎴栧湪璧勬簮绠＄悊鍣ㄤ腑浣跨敤鈥滄墦寮€鏂瑰紡 -> 鍥剧墖鏌ョ湅鍣ㄢ€濄€?
3. 濡傞渶浼佷笟鎵归噺閮ㄧ讲锛屽彲鐢辩鐞嗗憳浣跨敤 Windows 鏀寔鐨勯粯璁ゅ簲鐢?XML/缁勭瓥鐣ユ祦绋嬶紝鑰屼笉鏄敱鏈剼鏈己鍒朵慨鏀?`UserChoice`銆?

## Linux 鏂规

鏂囦欢锛?

- `Linux/imageviewer.desktop.template`
- `Linux/install-imageviewer-integration.sh`

### 鐩爣

- 鐢熸垚鐢ㄦ埛绾?`.desktop` 鏂囦欢銆?
- 澹版槑 `MimeType=image/jpeg;image/png;image/bmp;image/gif;image/webp;image/tiff;`銆?
- 閫氳繃 `desktop-file-install` 瀹夎鍒?`$XDG_DATA_HOME/applications` 鎴?`~/.local/share/applications`銆?
- 鍙€夎繍琛?`update-desktop-database` 鏇存柊妗岄潰鏁版嵁搴撱€?
- 閫氳繃 `xdg-mime default imageviewer.desktop <mime>` 璁剧疆榛樿鎵撳紑鏂瑰紡銆?
- 鍗歌浇鏃跺垹闄ょ敓鎴愮殑 desktop 鏂囦欢锛涘鏈簲鐢ㄤ粛鏄煇 MIME 绫诲瀷榛樿椤癸紝闇€鐢辩敤鎴烽€夋嫨鏂扮殑榛樿搴旂敤銆?

### 浣跨敤鏂瑰紡

棰勮灏嗚鎵ц鐨勬搷浣滐紝涓嶄慨鏀圭郴缁燂細

```sh
./Linux/install-imageviewer-integration.sh --executable "$HOME/.local/bin/ImageViewer"
```

搴旂敤鍒板綋鍓嶇敤鎴凤細

```sh
./Linux/install-imageviewer-integration.sh --executable "$HOME/.local/bin/ImageViewer" --apply

# 如需同时设为默认图片处理程序，显式增加 --set-defaults
./Linux/install-imageviewer-integration.sh --executable "$HOME/.local/bin/ImageViewer" --set-defaults --apply
```

鎸囧畾鍥炬爣鍚嶆垨鍥炬爣璺緞锛?

```sh
./Linux/install-imageviewer-integration.sh --executable "$HOME/.local/bin/ImageViewer" --icon imageviewer --apply
```

鍗歌浇/鍥炴粴褰撳墠鐢ㄦ埛绾ч泦鎴愶細

```sh
./Linux/install-imageviewer-integration.sh --uninstall --apply
```

### 鍙抽敭鑿滃崟/鎵撳紑鏂瑰紡

澶氭暟 Linux 妗岄潰鐜浼氬熀浜?`.desktop` 鏂囦欢鐨?`MimeType` 瀛楁鍦ㄦ枃浠剁鐞嗗櫒涓樉绀衡€滄墦寮€鏂瑰紡鈥濆€欓€夐」銆傛槸鍚︾洿鎺ュ嚭鐜板湪涓€绾у彸閿彍鍗曠敱妗岄潰鐜鍐冲畾銆傝缃粯璁ゅ簲鐢ㄥ悗锛屽弻鍑诲搴斿浘鐗囨枃浠堕€氬父浼氬惎鍔細

```sh
/path/to/ImageViewer /path/to/image.png
```

濡傛灉宸叉湁瀹炰緥姝ｅ湪杩愯锛屽綋鍓嶅簲鐢ㄧ殑鍗曞疄渚嬫満鍒朵細灏嗚矾寰勮浆鍙戝埌宸叉湁绐楀彛銆?

## 楠岃瘉寤鸿

### 涓嶄慨鏀圭郴缁熺殑楠岃瘉

- Windows锛氳繍琛?PowerShell 鑴氭湰浣嗕笉浼?`-Apply`锛岀‘璁よ緭鍑虹殑娉ㄥ唽琛ㄨ鍒掑寘鍚纭彲鎵ц璺緞鍜?`"%1"`銆?
- Linux锛氳繍琛?shell 鑴氭湰浣嗕笉浼?`--apply`锛岀‘璁よ緭鍑虹殑 desktop 瀹夎璺緞銆丮IME 绫诲瀷鍜?`xdg-mime` 璁″垝銆?
- 妫€鏌?`.desktop` 妯℃澘锛歚desktop-file-validate Linux/imageviewer.desktop.template` 鍙兘鍥犳ā鏉垮崰浣嶇澶辫触锛涘簲楠岃瘉鑴氭湰鐢熸垚鍚庣殑涓存椂鎴栧畨瑁呮枃浠躲€?

### 搴旂敤鍚庣殑楠岃瘉

Windows锛?

1. 妫€鏌?`HKCU:\Software\Classes\ImageViewer.ImageFile\shell\open\command`銆?
2. 鍙抽敭 `.png` / `.jpg` 鏂囦欢锛岀‘璁ゅ彲鐪嬪埌鈥滀娇鐢ㄥ浘鐗囨煡鐪嬪櫒鎵撳紑鈥濇垨鍑虹幇鍦ㄢ€滄墦寮€鏂瑰紡鈥濆垪琛ㄣ€?
3. 鎵撳紑涓€寮犲浘鐗囷紝鍐嶆墦寮€鍙︿竴寮犲浘鐗囷紝纭绗簩娆″惎鍔ㄨ浆鍙戝埌宸叉湁绐楀彛銆?
4. 鎵ц鍗歌浇鑴氭湰锛岀‘璁ゅ彸閿彍鍗曞拰 Open With 鍊欓€夐」琚Щ闄ゃ€?

Linux锛?

1. 妫€鏌?`~/.local/share/applications/imageviewer.desktop`銆?
2. 鎵ц `xdg-mime query default image/png`锛岀‘璁よ繑鍥?`imageviewer.desktop`銆?
3. 鍦ㄦ枃浠剁鐞嗗櫒涓 `.png` / `.jpg` 浣跨敤鈥滄墦寮€鏂瑰紡鈥濓紝纭鍥剧墖璺緞浼犲叆搴旂敤銆?
4. 宸叉湁瀹炰緥杩愯鏃跺啀娆′粠鏂囦欢绠＄悊鍣ㄦ墦寮€鍥剧墖锛岀‘璁よ矾寰勮浆鍙戝埌宸叉湁绐楀彛銆?
5. 鎵ц鍗歌浇鑴氭湰锛岀‘璁?desktop 鏂囦欢琚垹闄ゃ€?

## 鏈畬鎴愰」鍜岄闄?

- 鏈湪褰撳墠寮€鍙戞満鎵ц鐪熷疄娉ㄥ唽琛ㄣ€丮IME 鏁版嵁搴撴垨鐢ㄦ埛璁剧疆淇敼銆?
- Windows 榛樿搴旂敤涓嶈兘鐢辨櫘閫氳剼鏈畨鍏ㄩ潤榛樺己鍒惰缃紝鍙兘娉ㄥ唽鍊欓€夐」骞跺紩瀵肩敤鎴烽€夋嫨銆?
- Linux 涓嶅悓妗岄潰鐜瀵?`xdg-mime`銆佸彸閿彍鍗曞拰 `update-desktop-database` 鐨勮涓哄瓨鍦ㄥ樊寮傘€?
- 褰撳墠鍗曞疄渚嬪疄鐜颁娇鐢ㄥ浐瀹氭湰鏈?TCP 绔彛 `38947`锛屽彲鑳戒笌鍏朵粬杩涚▼鍐茬獊锛涘悗缁彲鑰冭檻鍛藉悕绠￠亾銆乁nix domain socket 鎴栧寘鍚敤鎴?搴旂敤璺緞鍝堝笇鐨勭鐐广€?
- 褰撳墠鏈彁渚涘簲鐢ㄥ浘鏍囪祫浜э紱Linux 妯℃澘浣跨敤 `imageviewer` 鍥炬爣鍚嶏紝Windows 浣跨敤 exe 鍐呭浘鏍囩储寮?`0`銆?
- 鍙戝竷娴佺▼灏氭湭鎺ュ叆杩欎簺鑴氭湰锛涘悗缁彲鍦ㄥ畨瑁呭寘銆佸帇缂╁寘鍙戝竷璇存槑鎴?CI artifact 涓檮甯﹀苟鎻愮ず鐢ㄦ埛鎵ц銆?

