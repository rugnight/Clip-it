using ImGuiNET;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Text;
using Veldrid;

namespace Clip_it
{
    class App
    {
        public const string AppName = "Clip-it";

        FusenDB db = new FusenDB();
        List<Fusen> fusens = new List<Fusen>();

        string DataDir { get => System.IO.Path.Combine(System.Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), AppName); }
        string DbFilePath { get => System.IO.Path.Combine(DataDir, "fusen5.db"); }

        public void Initialize()
        {
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
        }

        public void Terminate()
        {
            // DB保存
            db.Save(fusens.Select((fusen) => fusen.Model).ToList());
        }

        public void Update()
        {
            FusenUI();
        }

        void FusenUI()
        {
            ImGui.Begin("Fusen Manager");

            var io = ImGui.GetIO();
            if (io.KeyCtrl && ImGui.IsKeyPressed((int)Key.V, false))
            {
                // CTRL+Vで付箋作成
                if (ImGui.IsWindowFocused())
                {
                    var model = new FusenModel();
                    model.Text = ImGui.GetClipboardText();
                    fusens.Add(new Fusen(model));
                }
            }

            if (ImGui.Button("Add Fusen"))
            {
                fusens.Add(new Fusen());
            }

            ImGui.End();

            foreach (var fusen in fusens)
            {
                fusen.Update();
            }
        }
    }
}
