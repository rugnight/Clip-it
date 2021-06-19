using ImGuiNET;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Text;
using Veldrid;
using System.Numerics;
using System.Threading.Tasks;
using AngleSharp.Html.Dom;
using System.Net.Http;
using AngleSharp.Html.Parser;
using System.Drawing.Imaging;
using System.Drawing;
using System.IO;
using Microsoft.Toolkit.Uwp.Notifications;
using System.Web;

namespace Clip_it
{

    // アプリケーションイベントを外部へ通知するハンドラ
    interface IAppEventHandler
    {
        void OnPushHide();
    }

    // アプリケーション本体
    class App : IFusenEventHandler
    {
        // アプリ名
        public const string AppName = "Clip-it";
        const string MAIN_WIN_NAME = "Clip-it";

        ClipItDB _db;
        FusenTable _fusenTable;
        LinkTable _linkTable;
        MediaTable _mediaTable;

        GraphicsDevice _gd;
        ImGuiController _controller;

        List<Fusen> fusens = new List<Fusen>();
        Vector2 _windowSize;
        IAppEventHandler _appEventHandler;

        bool _bAlighn = false;
        bool _bQuit= false;

        // レイアウト番号
        int _layoutNo = 1;

        // 全てのタグ
        Dictionary<string, bool> AllTags = new Dictionary<string, bool>();

        // データ保存先ディレクトリ
        string DataDir { get => System.IO.Path.Combine(System.Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), AppName); }

        // DB情報保存ファイル
        string DbFilePath { get => System.IO.Path.Combine(DataDir, "fusen.db"); }

        /// <summary>
        /// 初期化
        /// </summary>
        /// <param name="windowSize"></param>
        /// <param name="appEventHandler"></param>
        public void Initialize(GraphicsDevice gd, ImGuiController controller, Vector2 windowSize, IAppEventHandler appEventHandler)
        {
            _gd = gd;
            _controller = controller;
            _windowSize = new Vector2(windowSize.X, windowSize.Y);
            _appEventHandler = appEventHandler;

            // アプリケーションフォルダの作成
            System.IO.Directory.CreateDirectory(DataDir);

            _db = new ClipItDB(DbFilePath);
            _fusenTable = new FusenTable(_db);
            _fusenTable.CreateTable();
            _linkTable = new LinkTable(_db);
            _linkTable.CreateTable();
            _mediaTable = new MediaTable(_db);
            _mediaTable.CreateTable();

            // DBの付箋から読み込み
            foreach (var model in _fusenTable.Load())
            {
                fusens.Add(new Fusen(model, this));
            }
            foreach (var fusen in fusens)
            foreach( var tag in fusen.Model.Tags)
            {
                if (!AllTags.ContainsKey(tag))
                {
                    AllTags[tag] = false;
                }
            }

            // imgui.iniロード
            LayoutLoad(_layoutNo);
        }

        /// <summary>
        /// 破棄
        /// </summary>
        public void Terminate()
        {
            // DB保存
            _fusenTable.Save(fusens.Select((fusen) => fusen.Model).ToList());
            // imgui.ini保存
            LayoutSave(_layoutNo);
        }

        /// <summary>
        /// 更新
        /// </summary>
        /// <returns></returns>
        public bool Update()
        {
            _bAlighn = false;

            // メインウィンドウ
            UpdateMainWindow();

            // 付箋を描画
            if (_bAlighn)
            {
                FusenSortUpdate();
            }
            else
            {
                FusenUpdate();
            }

            ImGui.ResetMouseDragDelta();

            return !_bQuit;
        }

        /// <summary>
        /// メインウィンドウ表示
        /// </summary>
        void UpdateMainWindow()
        { 
            ImGui.Begin(MAIN_WIN_NAME, 
                ImGuiWindowFlags.NoMove 
                | ImGuiWindowFlags.NoBackground 
                | ImGuiWindowFlags.MenuBar
                | ImGuiWindowFlags.NoCollapse
                | ImGuiWindowFlags.NoTitleBar
                | ImGuiWindowFlags.NoResize 
                | ImGuiWindowFlags.NoBringToFrontOnFocus);
            ImGui.SetWindowPos(new Vector2(0, 0));
            ImGui.SetWindowSize(_windowSize);

            // ショートカットキーの処理
            UpdateMainShortcutKeys();

            // メニューバーの処理
            UpdateMainMenuBar();

            // コンテキストメニューの処理
            UpdateMainContext();

            // タグのメニュー処理
            UpdateMainTagMenu();

            ImGui.End();
        }

        /// <summary>
        /// タグの切り替えメニュー
        /// </summary>
        void UpdateMainTagMenu()
        {
            // ユーザー操作によるチェック状態の変化を監視
            int count = 0;
            if (ImGui.Begin("Tags"))
            {
                foreach (var key in AllTags.Keys)
                {
                    bool isOn = AllTags[key];
                    if (ImGui.Checkbox(key, ref isOn))
                    {
                        AllTags[key] = isOn;
                        break;
                    }

                    if (ImGui.BeginPopupContextItem())
                    {
                        if (ImGui.MenuItem("削除"))
                        {
                        }
                        ImGui.EndPopup();
                    }

                    if (count < AllTags.Count - 1)
                    {
                        ImGui.SameLine();
                    }
                    count++;
                }
            }
            ImGui.End();
        }

        /// <summary>
        /// メニューバーを処理
        /// </summary>
        void UpdateMainMenuBar()
        {
            if (ImGui.BeginMenuBar())
            {
                if (ImGui.MenuItem("▼"))
                {
                    _appEventHandler?.OnPushHide();
                }

                // レイアウト
                for (int i = 1; i < 4 + 1; ++i)
                {
                    if (ImGui.MenuItem($"{i}", $"CTRL+{i}"))
                    {
                        LayoutSave(_layoutNo);
                        _layoutNo = i;
                        LayoutLoad(_layoutNo);
                    }
                }


                if (ImGui.BeginMenu("レイアウト"))
                {
                    if (ImGui.MenuItem("整列", "CTRL+A"))
                    {
                        _bAlighn = true;
                    }
                    ImGui.EndMenu();
                }

                if (ImGui.MenuItem("空の付箋を削除"))
                {
                    DeleteEmptyFusenAll();
                }

                if (ImGui.MenuItem("終了"))
                {
                    _bQuit = true; ;
                }
                ImGui.EndMenuBar();
            }
        }

        /// <summary>
        /// コンテキストメニュー
        /// </summary>
        void UpdateMainContext()
        {
            if (!ImGui.BeginPopupContextWindow())
            {
                return;
            }

            if (ImGui.MenuItem("新規"))
            {
                CreateNewFusen();
            }
            if (ImGui.MenuItem("貼り付け"))
            {
                CreateNewFusen(ImGui.GetClipboardText());
            }

            ImGui.EndPopup();
        }

        /// <summary>
        /// ショートカットキーの処理
        /// </summary>
        void UpdateMainShortcutKeys()
        {
            var io = ImGui.GetIO();

            // ESC
            if (ImGui.IsKeyPressed(ImGui.GetKeyIndex(ImGuiKey.Escape), false))
            {
                if (!ImGui.IsAnyItemActive())
                {
                    // ツールを非表示に
                    _appEventHandler?.OnPushHide();
                }
                //else
                //{
                //    // メインウィンドウへフォーカスを移す
                //    ImGui.SetWindowFocus(MAIN_WIN_NAME);
                //}
            }

            if (io.KeyCtrl && ImGui.IsKeyPressed((int)Key.N, false))
            {
                CreateNewFusen();
            }

            // 整列
            if (io.KeyCtrl && ImGui.IsKeyPressed((int)Key.A, false) && !ImGui.IsPopupOpen(FusenView.EDIT_POPUP_NAME, ImGuiPopupFlags.AnyPopup))
            {
                _bAlighn = true;
            }

            // 内容のコピー
            if (io.KeyCtrl && ImGui.IsKeyPressed((int)Key.C, false) && ImGui.IsWindowFocused(ImGuiFocusedFlags.AnyWindow))
            {
                var activeFusen = fusens.Find(a => a.IsActive());
                if (activeFusen != null)
                {
                    ImGui.SetClipboardText(activeFusen.Model.Text);
                }
            }

            // 削除
            if (io.KeyCtrl && ImGui.IsKeyPressed((int)Key.W, false) && ImGui.IsWindowFocused(ImGuiFocusedFlags.AnyWindow))
            {
                var activeFusen = fusens.Find(a => a.IsActive());
                if (activeFusen != null)
                {
                    activeFusen.Model.Delete();
                }
            }

            // CTRL+Vで付箋作成
            if (io.KeyCtrl && ImGui.IsKeyPressed((int)Key.V, false) && ImGui.IsWindowFocused())
            {
                var clipText = ImGui.GetClipboardText();
                if (clipText != null)
                {
                    CreateNewFusen(clipText);
                }
            }

        }

        /// <summary>
        /// 付箋の更新・描画
        /// </summary>
        void FusenUpdate()
        {
            foreach (var fusen in fusens)
            {
                if (IsFusenFiltered(fusen, AllTags))
                {
                    fusen.Update();
                }
            }
        }

        /// <summary>
        /// 付箋を並び替え描画
        /// </summary>
        void FusenSortUpdate()
        {
            fusens.Sort((a, b) => (int)(a.LastSize.Y - b.LastSize.Y));

            int colNum = (int)_windowSize.X / 350;
            float padX = 10;
            float padY = 10;
            float x = padX;
            float y = 0.0f;
            // 現在の行で描いた横幅個数
            int rowUnit = 0;
            var yarray = Enumerable.Repeat(30, 64).ToList();
            for(int i = 0; i < fusens.Count; ++i)
            {
                var fusen = fusens[i];

                // フィルター済みならスキップ
                if (!IsFusenFiltered(fusen, AllTags))
                {
                    continue;
                }

                var nextW = (i <= fusens.Count) ? fusens[i].LastSize.X : 0.0f;

                y = yarray[rowUnit];
                ImGui.SetNextWindowPos(new Vector2(x, y));

                // 幅分すすめる
                for (int j = 0; j < fusens[i].LastSizeUnit; ++j)
                {
                    yarray[rowUnit] += (int)fusens[i].LastSize.Y + (int)padY;
                    rowUnit++;
                }

                // 削除済みの場合は無視
                if (!fusen.Update())
                {
                    continue;
                }

                x += fusen.LastSize.X + padX;

                if (_windowSize.X < (x + nextW))
                {
                    x = padX;
                    rowUnit = 0;
                }
            }
        }

        /// <summary>
        /// 新しい付箋を作成する
        /// </summary>
        void CreateNewFusen(string text = "")
        {
            var fusen = new Fusen(new FusenModel(text), this);
            fusen.SetFocusInput();
            fusens.Add(fusen);
        }

        /// <summary>
        /// 削除フラグのたった付箋を削除する
        /// </summary>
        public void DeleteClosedFusens()
        {
            for (int i = fusens.Count - 1; 0 <= i; --i)
            {
                if (fusens[i].Model.Deleted)
                {
                    fusens.RemoveAt(i);
                }
            }
        }

        /// <summary>
        /// 内容画空の付箋をすべて削除する
        /// </summary>
        public void DeleteEmptyFusenAll()
        {
            // 削除対象に削除フラグを建てる
            // 実際の削除はDB保存時に行われる
            for (int i = fusens.Count - 1; 0 <= i; --i)
            {
                if (string.IsNullOrEmpty(fusens[i].Model.Text))
                {
                    fusens[i].Model.Delete();
                }
            }
        }

        // imgui.ini保存
        void LayoutSave(int layoutNo)
        {
            ImGui.SaveIniSettingsToDisk(System.IO.Path.Combine(DataDir, $"imgui{layoutNo}.ini"));
        }

        // imgui.iniロード
        void LayoutLoad(int layoutNo)
        {
            ImGui.LoadIniSettingsFromDisk(System.IO.Path.Combine(DataDir, $"imgui{layoutNo}.ini"));
        }

        /// <summary>
        /// 指定URLのサイトのタイトルを取得する
        /// </summary>
        /// <param name="url"></param>
        /// <returns></returns>
        async Task<LinkModel> GetURLTitle(string url)
        {
            var info = new LinkModel(url);
            var uri = new Uri(url);

            // DBにあればそれを使う
            var titles = _linkTable.Load(new List<Uri>() { uri } );
            if (0 < titles.Count)
            {
                return titles[0];
            }

            // 指定したサイトのHTMLをストリームで取得する
            var doc = default(IHtmlDocument);
            try
            {
                using (var client = new HttpClient())
                using (var stream = await client.GetStreamAsync(uri))
                {
                    // AngleSharp.Html.Parser.HtmlParserオブジェクトにHTMLをパースさせる
                    var parser = new HtmlParser();
                    doc = await parser.ParseDocumentAsync(stream);
                }
            }
            catch
            {
            }
            finally
            {
                if (doc != null)
                {
                    // HTMLからtitleタグの値(サイトのタイトルとして表示される部分)を取得する
                    info.Title = doc.QuerySelector("meta[property='og:title']")?.GetAttribute("content") ?? doc.Title;

                    var ogImageUrl = doc.QuerySelector("meta[property='og:image']")?.GetAttribute("content");
                    if (ogImageUrl == null)
                    {
                        info.OgImageUrl = "";
                    }
                    else
                    {
                        var query = new Uri(ogImageUrl).Query;
                        if (string.IsNullOrEmpty(query))
                        {
                            info.OgImageUrl = ogImageUrl.ToString();
                        }
                        else
                        {
                            info.OgImageUrl = ogImageUrl.ToString().Replace(query, "");
                        }
                    }
                    // DBに登録
                    _linkTable.Save(new List<LinkModel>() { info });
                }
                else
                {
                    info.Title  = "";
                }
            }

            return info;
        }

        /// <summary>
        /// ファイルがウィンドウにドロップされたときの挙動
        /// </summary>
        /// <param name="dropFile"></param>
        public void OnDropItem(string dropFile)
        {
            CreateNewFusen(dropFile);
        }

        /// <summary>
        /// 付箋の内容に変更があった場合に呼ばれる
        /// </summary>
        public void OnFusenChangeAndEditEnd(Fusen fusen)
        {
            fusen.UpdateMetaData();
        }

        /// <summary>
        /// 付箋からURLのタイトル取得をリクエストされた場合に呼ばれる
        /// </summary>
        /// <param name="fusen"></param>
        /// <param name="callback"></param>
        public async void OnFusenRequestWebInfo(string url, Action<LinkModel> callback)
        {
            callback?.Invoke(await GetURLTitle(url));
        }

        /// <summary>
        /// URLを開くリクエストが来たときに呼ばれる
        /// </summary>
        /// <param name="url"></param>
        public void OnFusenRequestOpenUrl(Uri url)
        {
            var psi = new System.Diagnostics.ProcessStartInfo();
            psi.UseShellExecute = true;
            psi.FileName = url.AbsoluteUri;
            System.Diagnostics.Process.Start(psi);
        }

        /// <summary>
        /// パスを開くリクエストが来たときに呼ばれる
        /// </summary>
        /// <param name="path"></param>
        public void OnFusenRequestOpenPath(string path)
        {
            var psi = new System.Diagnostics.ProcessStartInfo();
            psi.UseShellExecute = true;
            psi.FileName = path;
            System.Diagnostics.Process.Start(psi);
        }

        /// <summary>
        /// テクスチャ作成依頼
        /// </summary>
        /// <param name="path"></param>
        /// <param name="texture"></param>
        /// <param name="texId"></param>
        public void OnFusenRequestCreateTexture(Uri uri, Action<Texture, IntPtr> onComplete)
        {
            // DBを確認
            var dbModels = _mediaTable.Load(new List<string>() { uri.ToString() });
            if (0 < dbModels.Count)
            {
                var model = dbModels[0];
                using (var ms = new MemoryStream(Convert.FromBase64String(model.Base64)))
                {
                    Texture texture;
                    IntPtr texId;
                    CreateTexture(model.Uri.AbsoluteUri, ms, out texture, out texId);
                    onComplete?.Invoke(texture, texId);
                }
                return;
            }

            // 作成
            DownloadAndCreateTexture(uri, (texture, texId) => onComplete?.Invoke(texture, texId));
        }

        /// <summary>
        /// テクスチャの作成
        /// </summary>
        /// <param name="url"></param>
        /// <param name="onComplete"></param>
        async void DownloadAndCreateTexture(Uri uri, Action<Texture, IntPtr>  onComplete)
        {
            if (uri.IsFile)
            {
                using (var client = new System.Net.WebClient())
                using (var stream = client.OpenRead(uri))
                {
                    IntPtr texId;
                    Texture texture;
                    CreateTexture(uri.ToString(), stream, out texture, out texId);
                    onComplete?.Invoke(texture, texId);
                }
            }
            else
            {
                using (var client = new HttpClient())
                {
                    var res = await client.GetAsync(uri, HttpCompletionOption.ResponseContentRead);
                    using (var httpStream = await res.Content.ReadAsStreamAsync())
                    {
                        IntPtr texId;
                        Texture texture;
                        CreateTexture(uri.ToString(), httpStream, out texture, out texId);
                        onComplete?.Invoke(texture, texId);
                    }
                }
            }

        }


        /// <summary>
        /// テクスチャを生成
        /// </summary>
        /// <param name="name"></param>
        /// <param name="stream"></param>
        /// <param name="texture"></param>
        /// <param name="texId"></param>
        void CreateTexture(string name, Stream stream, out Texture texture, out IntPtr texId)
        {
            // テクスチャ作成
            Bitmap bmp = new Bitmap(stream);
            var tex = _gd.ResourceFactory.CreateTexture(
                TextureDescription.Texture2D(
                (uint)bmp.Width,
                (uint)bmp.Height,
                1,
                1,
                Veldrid.PixelFormat.B8_G8_R8_A8_UNorm,
                TextureUsage.Sampled));
            tex.Name = name;

            BitmapData bmpData = bmp.LockBits(new System.Drawing.Rectangle(0, 0, bmp.Width, bmp.Height), ImageLockMode.ReadOnly, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
            _gd.UpdateTexture(
                tex,
                bmpData.Scan0,
                (uint)(bmpData.Width * bmpData.Height * 4),
                0,
                0,
                0,
                tex.Width,
                tex.Height,
                1,
                0,
                0);
            bmp.UnlockBits(bmpData);

            // DBに登録
            stream.Position = 0;
            using (var br = new BinaryReader(stream))
            {
                var bytes = br.ReadBytes((int)stream.Length);
                if (bytes != null && 0 < bytes.Length)
                {
                    string base64 = Convert.ToBase64String(bytes);
                    _mediaTable.Save(new List<MediaModel>() { new MediaModel(name, base64) });
                }
            }

            texture = tex;
            texId = _controller.GetOrCreateImGuiBinding(_gd.ResourceFactory, tex);
        }

        public void OnFusenRequestNotifyToggle(Fusen fusen, DateTime dateTime, bool bOn)
        {
            if (bOn)
            {
                new ToastContentBuilder().AddText("Clip-It").AddText(fusen.Model.Text).Schedule(new DateTimeOffset(dateTime));
            }
        }

        public void OnFusenAddTag(Fusen fusen, string tag)
        {
            // フィルタが掛かっていたら true 状態で初期設定する
            bool defaultValue = false;
            defaultValue = AllTags.Any((pair) => { return pair.Value; });

            if (!AllTags.ContainsKey(tag))
            {
                AllTags[tag] = defaultValue;
            }
        }

        public void OnFusenDelTag(Fusen fusen, string tag)
        {
            // 使用中のタグであれば辞書からは消さない
            if (fusens.Any((fusen) => fusen.Model.Tags.Exists((_tag) => _tag == tag)))
            {
                return;
            }
            AllTags.Remove(tag);
        }

        /// <summary>
        /// 付箋がタグの状態と照らし合わせて表示対象かどうかを判定
        /// </summary>
        /// <param name="fusen"></param>
        /// <param name="tags"></param>
        /// <returns></returns>
        static bool IsFusenFiltered(Fusen fusen, Dictionary<string, bool> tags)
        {
            if (tags.All((pair) => { return !pair.Value; }))
            {
                return true;
            }
            else if (tags.Any((pair) => { return pair.Value && fusen.Model.Tags.Contains(pair.Key); }))
            {
                return true;
            }
            return false;
        }

    }
}
