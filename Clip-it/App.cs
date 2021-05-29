using ImGuiNET;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Text;
using Veldrid;
using System.Numerics;

namespace Clip_it
{

    // アプリケーションイベントを外部へ通知するハンドラ
    interface IAppEventHandler
    {
        void OnPushHide();
    }

    // アプリケーション本体
    class App
    {
        // アプリ名
        public const string AppName = "Clip-it";

        FusenDB db = new FusenDB();
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
        public void Initialize(Vector2 windowSize, IAppEventHandler appEventHandler)
        {
            _windowSize = new Vector2(windowSize.X, 1.0f);
            _appEventHandler = appEventHandler;

            // アプリケーションフォルダの作成
            System.IO.Directory.CreateDirectory(DataDir);

            // DB開く
            db.Open(DbFilePath);
            db.CreateDB();

            // DBの付箋から読み込み
            foreach (var model in db.Load())
            {
                fusens.Add(new Fusen(model));
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
            db.Save(fusens.Select((fusen) => fusen.Model).ToList());
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

            //ImGui.Begin("Test");
            //ImGui.SetKeyboardFocusHere(2);
            //string hoge = "";
            //ImGui.InputText("hoge", ref hoge, 64);
            //ImGui.InputText("fuga", ref hoge, 64);
            //ImGui.InputText("piyo", ref hoge, 64);
            //ImGui.End();

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

            if (ImGui.IsKeyPressed((int)Key.Escape, false))
            {
                _appEventHandler?.OnPushHide();
            }

            // CTRL+Vで付箋作成
            if (io.KeyCtrl && !ImGui.IsAnyItemActive() && ImGui.IsKeyPressed((int)Key.V, false))
            {
                var clipText = ImGui.GetClipboardText();
                if (clipText != null)
                {
                    var model = new FusenModel();
                    model.Text = clipText;
                    fusens.Add(new Fusen(model));
                }
            }
            else if (io.KeyCtrl && ImGui.IsKeyPressed((int)Key.A, false))
            {
                _bAlighn = true;
            }
            else if (io.KeyCtrl && ImGui.IsKeyPressed((int)Key.N, false))
            {
                CreateNewFusen();
            }
            else if (io.KeyCtrl && ImGui.IsKeyPressed((int)Key.S, false))
            {
                LayoutSave();
            }
            else if (io.KeyCtrl && ImGui.IsKeyPressed((int)Key.L, false))
            {
                LayoutLoad();
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

            float padX = 10;
            float padY = 10;
            float maxH = 0;
            float x = padX;
            float y = 30.0f + padY;
            for(int i = 0; i < fusens.Count; ++i)
            {
                var fusen = fusens[i];
                var nextW = (i <= fusens.Count) ? fusens[i].LastSize.X : 0.0f;

                ImGui.SetNextWindowPos(new Vector2(x, y));

                // 削除済みの場合は無視
                if (!fusen.Update())
                {
                    continue;
                }

                x += fusen.LastSize.X + padX;
                maxH = (maxH < fusen.LastSize.Y) ? fusen.LastSize.Y : maxH;

                if (_windowSize.X < (x + nextW))
                {
                    x = padX;
                    y = y + maxH + padY;
                    maxH = 0.0f;
                }

            }
        }

        /// <summary>
        /// 新しい付箋を作成する
        /// </summary>
        void CreateNewFusen()
        {
            var fusen = new Fusen(new FusenModel());
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
                    fusens[i].Model.Deleted = true;
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
        /// ファイルがウィンドウにドロップされたときの挙動
        /// </summary>
        /// <param name="dropFile"></param>
        public void OnDropItem(string dropFile)
        {
            var model = new FusenModel();
            model.Text = dropFile;
            fusens.Add(new Fusen(model));
        }
    }
}
