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

        ClipItDB _db;
        FusenTable _fusenTable;
        LinkTable _linkTable;

        GraphicsDevice _gd;
        ImGuiController _controller;

        List<Fusen> fusens = new List<Fusen>();
        Vector2 _windowSize;
        IAppEventHandler _appEventHandler;

        bool _bAlighn = false;
        bool _bQuit= false;

        // データ保存先ディレクトリ
        string DataDir { get => System.IO.Path.Combine(System.Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), AppName); }

        // DB情報保存ファイル
        string DbFilePath { get => System.IO.Path.Combine(DataDir, "fusen.db"); }

        // IMGUIの保存情報ファイル
        string IniFilePath { get => System.IO.Path.Combine(DataDir, "imgui.ini"); }

        /// <summary>
        /// 初期化
        /// </summary>
        /// <param name="windowSize"></param>
        /// <param name="appEventHandler"></param>
        public void Initialize(GraphicsDevice gd, ImGuiController controller, Vector2 windowSize, IAppEventHandler appEventHandler)
        {
            _gd = gd;
            _controller = controller;
            _windowSize = new Vector2(windowSize.X, 1.0f);
            _appEventHandler = appEventHandler;

            // アプリケーションフォルダの作成
            System.IO.Directory.CreateDirectory(DataDir);

            _db = new ClipItDB(DbFilePath);
            _fusenTable = new FusenTable(_db);
            _fusenTable.CreateTable();
            _linkTable = new LinkTable(_db);
            _linkTable.CreateTable();

            // DBの付箋から読み込み
            foreach (var model in _fusenTable.Load())
            {
                fusens.Add(new Fusen(model, this));
            }
            // imgui.iniロード
            LayoutLoad();
        }

        /// <summary>
        /// 破棄
        /// </summary>
        public void Terminate()
        {
            // DB保存
            _fusenTable.Save(fusens.Select((fusen) => fusen.Model).ToList());
            // imgui.ini保存
            LayoutSave();
        }

        /// <summary>
        /// 更新
        /// </summary>
        /// <returns></returns>
        public bool Update()
        {
            _bAlighn = false;

            // メニューバーの処理
            UpdateMenuBar();

            // ショートカットキーの処理
            UpdateShortcutKeys();

            // 付箋を描画
            if (_bAlighn)
            {
                FusenSortUpdate();
            }
            else
            {
                FusenUpdate();
            }



            return !_bQuit;
        }

        /// <summary>
        /// メニューバーを処理
        /// </summary>
        void UpdateMenuBar()
        {
            if (ImGui.BeginMainMenuBar())
            {
                if (ImGui.MenuItem("▼"))
                {
                    _appEventHandler?.OnPushHide();
                }

                if (ImGui.MenuItem("New", "CTRL+N"))
                {
                    CreateNewFusen();
                }

                if (ImGui.BeginMenu("Layout"))
                {
                    if (ImGui.MenuItem("Alighn", "CTRL+A"))
                    {
                        _bAlighn = true;
                    }
                    if (ImGui.MenuItem("Save", "CTRL+S"))
                    {
                        LayoutSave();
                    }
                    if (ImGui.MenuItem("Load", "CTRL+L"))
                    {
                        LayoutLoad();
                    }
                    ImGui.EndMenu();
                }

                if (ImGui.MenuItem("Clean Empty"))
                {
                    DeleteEmptyFusenAll();
                }

                if (ImGui.MenuItem("Quit"))
                {
                    _bQuit = true; ;
                }
                ImGui.EndMainMenuBar();
            }
        }

        /// <summary>
        /// ショートカットキーの処理
        /// </summary>
        void UpdateShortcutKeys()
        {
            var io = ImGui.GetIO();

            // ESC
            if (ImGui.IsKeyPressed((int)Key.Escape, false))
            {
                if (ImGui.IsAnyItemActive() || ImGui.IsWindowFocused(ImGuiFocusedFlags.AnyWindow))
                {
                    // ウィンドウからフォーカスを外す
                    ImGui.SetWindowFocus(null);
                }
                else
                {
                    // ツールを非表示に
                    _appEventHandler?.OnPushHide();
                }
            }

            if (io.KeyCtrl && ImGui.IsKeyPressed((int)Key.N, false))
            {
                CreateNewFusen();
            }

            if (io.KeyCtrl && ImGui.IsKeyPressed((int)Key.S, false))
            {
                LayoutSave();
            }

            if (io.KeyCtrl && ImGui.IsKeyPressed((int)Key.L, false))
            {
                LayoutLoad();
            }

            // 整列
            if (io.KeyCtrl && ImGui.IsKeyPressed((int)Key.A, false) && !ImGui.IsWindowFocused(ImGuiFocusedFlags.AnyWindow))
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
            if (io.KeyCtrl && ImGui.IsKeyPressed((int)Key.V, false) && !ImGui.IsWindowFocused(ImGuiFocusedFlags.AnyWindow))
            {
                var clipText = ImGui.GetClipboardText();
                if (clipText != null)
                {
                    var model = new FusenModel();
                    model.Text = clipText;
                    fusens.Add(new Fusen(model, this));
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
                fusen.Update();
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
                //maxH = (maxH < fusen.LastSize.Y) ? fusen.LastSize.Y : maxH;

                if (_windowSize.X < (x + nextW))
                {
                    x = padX;
                    rowUnit = 0;
                    //maxH = 0.0f;
                }

            }
        }

        /// <summary>
        /// 新しい付箋を作成する
        /// </summary>
        void CreateNewFusen()
        {
            var fusen = new Fusen(new FusenModel(), this);
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
        void LayoutSave()
        {
            ImGui.SaveIniSettingsToDisk(IniFilePath);
        }

        // imgui.iniロード
        void LayoutLoad()
        {
            ImGui.LoadIniSettingsFromDisk(IniFilePath);
        }

        /// <summary>
        /// 指定URLのサイトのタイトルを取得する
        /// </summary>
        /// <param name="url"></param>
        /// <returns></returns>
        async Task<string> GetURLTitle(string url)
        {
            // DBにあればそれを使う
            var titles = _linkTable.Load(new List<string>() { url });
            if (0 < titles.Count)
            {
                return titles[0].Title;
            }

            var title = "";
            // 指定したサイトのHTMLをストリームで取得する
            var doc = default(IHtmlDocument);
            try
            {
                using (var client = new HttpClient())
                using (var stream = await client.GetStreamAsync(new Uri(url)))
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
                    title = doc.Title;
                    // DBに登録
                    _linkTable.Save(new List<LinkModel>() { new LinkModel(url, title) });
                }
                else
                {
                    title = "";
                }
            }

            return title;
        }

        /// <summary>
        /// ファイルがウィンドウにドロップされたときの挙動
        /// </summary>
        /// <param name="dropFile"></param>
        public void OnDropItem(string dropFile)
        {
            var model = new FusenModel();
            model.Text = dropFile;
            fusens.Add(new Fusen(model, this));
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
        public async void OnFusenRequestUrlTitle(string url, Action<string> callback)
        {
            callback?.Invoke(await GetURLTitle(url));
        }

        /// <summary>
        /// URLを開くリクエストが来たときに呼ばれる
        /// </summary>
        /// <param name="url"></param>
        public void OnFusenRequestOpenUrl(string url)
        {
            var psi = new System.Diagnostics.ProcessStartInfo();
            psi.UseShellExecute = true;
            psi.FileName = url;
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
        public void OnFusenRequestCreateTexture(string path, Action<Texture, IntPtr> onComplete)
        {
            if (Uri.IsWellFormedUriString(path, UriKind.Absolute))
            {
                CreateTextureFromWeb(path, (texture, texId) => onComplete?.Invoke(texture, texId));
            }
            else
            {
                Texture texture;
                IntPtr texId;
                CreateTextureFromFile(path, path, out texture, out texId);
                onComplete?.Invoke(texture, texId);
            }
        }

        /// <summary>
        /// ファイルからテクスチャを作成
        /// </summary>
        /// <param name="name"></param>
        /// <param name="filePath"></param>
        /// <param name="texture"></param>
        /// <param name="texId"></param>
        void CreateTextureFromFile(string name, string filePath, out Texture texture, out IntPtr texId)
        {
            using (var fs = new FileStream(filePath, FileMode.Open))
            {
                CreateTexture(name, fs, out texture, out texId);
            }
            
        }

        async void CreateTextureFromWeb(string url, Action<Texture, IntPtr>  onComplete)
        {
            var client = new HttpClient();
            HttpResponseMessage res = await client.GetAsync(url, HttpCompletionOption.ResponseContentRead);

            using (var httpStream = await res.Content.ReadAsStreamAsync())
            {
                IntPtr texId;
                Texture texture;
                CreateTexture(url, httpStream, out texture, out texId);
                onComplete?.Invoke(texture, texId);
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
    }
}
