using System;
using System.Linq;
using System.Text.RegularExpressions;
using System.Numerics;
using System.Collections.Generic;
using Veldrid;
using Veldrid.Sdl2;
using Veldrid.StartupUtilities;
using AngleSharp;
using static ImGuiNET.ImGuiNative;
using System.Net.Http;
using AngleSharp.Html.Dom;
using AngleSharp.Html.Parser;
using System.Threading.Tasks;
using System.Runtime.InteropServices;

namespace ImGuiNET
{
    /// <summary>
    /// 付箋情報
    /// </summary>
    class FusenModel
    {
        // 識別ID
        Guid _id;
        public Guid Id 
        {
            get => this._id;
            set 
            {
                if (this._id == value) return;
                this._id = value;
            }
        }

        // 内容
        string _text;
        public string Text
        {
            get => this._text;
            set
            {
                if (this._text == value) return;
                this._text = value;
                this.OnChangeText?.Invoke(this);
            }
        }

        // 内容更新通知
        public event Action<FusenModel> OnChangeText;

        public FusenModel()
        {
            this._id = Guid.NewGuid();
            this._text = "";
            this.OnChangeText = null;
        }
        public FusenModel(string text)
            : this()
        {
            this._text = text;
        }

        public List<string> GetURLs()
        {
            return CollectURL(this.Text);
        }

        static List<string> CollectURL(string text)
        {
            var result = new List<string>();

            var reg = new Regex(@"\b(?:https?://|www\.)\S+\b", RegexOptions.Compiled | RegexOptions.IgnoreCase);
            foreach (Match match in reg.Matches(text))
            {
                result.Add(match.Value);
            }

            return result;
        }
    };

    /// <summary>
    /// 付箋表示
    /// </summary>
    class FusenView
    {
        enum State
        {
            Active,
            Inactive,
            Edit,
        };

        public bool visible = true;
        public bool Visible { get; set; } = true;

        public event Action<string> OnChangeText;
        public event Action<string> OnSelectURL;

        State state = State.Active;

        public void Disp(Fusen fusen, FusenModel model)
        {
            if (!visible)
            {
                return;
            }

            var text = model.Text;
            var windowsFlags = ImGuiWindowFlags.MenuBar;

            ImGui.Begin(model.Id.ToString(), ref visible, windowsFlags);

            if (state == State.Edit)
            {
                DispTextEditing(text);
            }
            else
            {
                DispText(text);
            }

            ImGui.Spacing();
            DispURLButtons(fusen);
            ImGui.Spacing();
            DispCloseButton();

            if (ImGui.IsMouseDoubleClicked(ImGuiMouseButton.Left))
            {
                state = State.Edit;
            }
            else if (ImGui.IsMouseClicked(ImGuiMouseButton.Left))
            {
                state = State.Active;
            }

            ImGui.End();
        }

        void DispText(string text)
        {
            ImGui.Text(text);
        }

        void DispTextEditing(string text)
        {
            var textSize = ImGui.CalcTextSize(text);
            if (ImGui.InputTextMultiline("", ref text, 1024, textSize * 2.0f))
            {
                this.OnChangeText?.Invoke(text);
            }
        }

        void DispURLButtons(Fusen fusen)
        {
            // URLボタン
            foreach (var pair in fusen.Urls)
            {
                // タイトル
                if (ImGui.Button(pair.Value))
                {
                    this.OnSelectURL?.Invoke(pair.Key);
                }
                // URL
                ImGui.LabelText("%s", pair.Key);
            }
        }
        void DispCloseButton()
        {
            if (ImGui.Button("Close"))
            {
                visible = false;
            }
        }
    };

    /// <summary>
    /// 付箋
    /// </summary>
    class Fusen
    {
        FusenModel model;
        FusenView view;

        public Dictionary<string, string> Urls { get; private set; } = new Dictionary<string, string>();

        public Fusen()
            : this(new FusenModel())
        {
        }
        public Fusen(FusenModel model) 
        {
            this.model = model;
            this.model.OnChangeText += (changedModel) =>
            {
                this.UpdateURLs();
            };
            this.UpdateURLs();

            view = new FusenView();
            view.OnChangeText += (changedText) =>
            {
                this.model.Text = changedText;
            };
            view.OnSelectURL += (url) =>
            {
                OpenURL(url);
            };
        }

        public void Update()
        {
            view.Disp(this, model);
        }

        void UpdateURLs()
        {
            var urls = this.model.GetURLs();
            foreach (var url in urls)
            {
                if (this.Urls.ContainsKey(url))
                {
                    continue;
                }
                this.Urls[url] = "";    // とりあえず空の結果だけ用意する

                // 非同期通信でタイトル取得
                this.Urls[url] = GetURLTitle(url).Result;
            }
        }

        /// <summary>
        /// URLを開く
        /// </summary>
        /// <param name="url"></param>
        static void OpenURL(string url)
        {
            var psi = new System.Diagnostics.ProcessStartInfo();
            psi.UseShellExecute = true;
            psi.FileName = url;
            System.Diagnostics.Process.Start(psi);
        }

        /// <summary>
        /// 指定URLのサイトのタイトルを取得する
        /// </summary>
        /// <param name="url"></param>
        /// <returns></returns>
        static async Task<string> GetURLTitle(string url)
        {
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
                }
                else
                {
                    title = "404";
                }
            }

            return title;
        }
    }



    class Program
    {
        static List<Fusen> fusens = new List<Fusen>();

        private static Sdl2Window _window;
        private static GraphicsDevice _gd;
        private static CommandList _cl;
        private static ImGuiController _controller;

        // UI state
        private static Vector3 _clearColor = new Vector3(0.0f, 0.0f, 0.0f);

        string DataDir { get; set; } = System.IO.Path.Combine(System.Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Fusen");

        static void Main(string[] args)
        {

            // Create window, GraphicsDevice, and all resources necessary for the demo.
            VeldridStartup.CreateWindowAndGraphicsDevice(
                new WindowCreateInfo(50, 50, 1280, 720, WindowState.Normal, "付箋"),
                new GraphicsDeviceOptions(false, null, true, ResourceBindingModel.Improved, true, true),
                out _window,
                out _gd);
            _window.KeyUp += (evt) =>
             {
                // タブで全体を半透明
                if (evt.Key == Key.Tab)
                {
                    _window.Opacity = 1.0f;
                }
             };
            _window.KeyDown += (evt) =>
            {
                // タブで全体を半透明
                if (evt.Key == Key.Tab)
                {
                    _window.Opacity = 0.5f;
                }

                // CTRL+Vで付箋作成
                if (ImGui.IsWindowFocused())
                {
                    if (_window.Focused && evt.Modifiers == ModifierKeys.Control && evt.Key == Key.V && !evt.Repeat)
                    {
                        var model = new FusenModel();
                        model.Text = ImGui.GetClipboardText();
                        fusens.Add(new Fusen(model));
                    }
                }
            };
            _window.BorderVisible = false;
            _window.DragDrop += (evt) =>
            {
                var model = new FusenModel();
                model.Text = evt.File;
                fusens.Add(new Fusen(model));

                System.Diagnostics.Debug.WriteLine(evt.File);
                System.Diagnostics.Debug.WriteLine(ImGui.GetClipboardText());
                var payload = ImGui.GetDragDropPayload();
                System.Diagnostics.Debug.WriteLine(payload.ToString());
            };
            _window.Resized += () =>
            {
                _gd.MainSwapchain.Resize((uint)_window.Width, (uint)_window.Height);
                _controller.WindowResized(_window.Width, _window.Height);
            };
            _cl = _gd.ResourceFactory.CreateCommandList();

            // IMGUI制御
            _controller = new ImGuiController(_gd, _gd.MainSwapchain.Framebuffer.OutputDescription, _window.Width, _window.Height);

            // Main application loop
            while (_window.Exists)
            {
                InputSnapshot snapshot = _window.PumpEvents();
                if (!_window.Exists) 
                {
                    break; 
                }
                _controller.Update(1f / 60f, snapshot); // Feed the input events to our ImGui controller, which passes them through to ImGui.

                FusenUI();

                _cl.Begin();
                _cl.SetFramebuffer(_gd.MainSwapchain.Framebuffer);
                _cl.ClearColorTarget(0, new RgbaFloat(_clearColor.X, _clearColor.Y, _clearColor.Z, 0.5f));
                _controller.Render(_gd, _cl);
                _cl.End();
                _gd.SubmitCommands(_cl);
                _gd.SwapBuffers(_gd.MainSwapchain);
            }

            // Clean up Veldrid resources
            _gd.WaitForIdle();
            _controller.Dispose();
            _cl.Dispose();
            _gd.Dispose();
        }

        static void FusenUI()
        {
            ImGui.Begin("Fusen Manager");
            ImGui.Text("Hello from another window!");
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
};
}
