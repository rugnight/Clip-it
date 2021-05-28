using System;
using System.Collections.Generic;
using System.Net.Http;
using AngleSharp.Html.Dom;
using AngleSharp.Html.Parser;
using System.Threading.Tasks;

namespace Clip_it
{
    /// <summary>
    /// 付箋
    /// </summary>
    class Fusen
    {
        FusenModel _model;
        FusenView _view;

        public FusenModel Model => _model;
        public Dictionary<string, string> Urls { get; private set; } = new Dictionary<string, string>();

        public Dictionary<string, string> Paths { get; private set; } = new Dictionary<string, string>();
        public Dictionary<string, bool> Dates { get; private set; } = new Dictionary<string, bool>();
        /// <summary>
        /// コンストラクタ
        /// </summary>
        public Fusen()
            : this(new FusenModel())
        {
        }
        public Fusen(FusenModel model) 
        {
            this._model = model;
            this._model.OnChangeText += (changedModel) =>
            {
                this.UpdateMetaData();
            };
            this.UpdateMetaData();

            _view = new FusenView();
            _view.OnChangeText += (changedText) =>
            {
                this._model.Text = changedText;
            };
            _view.OnSelectURL += (url) =>
            {
                OpenURL(url);
            };
            _view.OnSelectPath += (path) =>
            {
                OpenPath(path);
            };
            
            _view.OnClose += () =>
            {
                model.Deleted = true;
            };
        }

        /// <summary>
        /// 更新
        /// </summary>
        public void Update()
        {
            _view.Disp(this, _model);
        }

        /// <summary>
        /// URLリストを更新
        /// </summary>
        void UpdateMetaData()
        {
            // 時刻
            var dates = this._model.GetDates();
            foreach (var date in dates)
            {
                if (this.Dates.ContainsKey(date))
                {
                    continue;
                }
                this.Dates[date] = false;
            }

            // ファイルパス
            var paths = this.Model.GetPaths();
            foreach (var path in paths)
            {
                var item = "";
                if (System.IO.File.Exists(path))
                {
                    item = System.IO.Path.GetFileName(path);
                }
                else if (System.IO.Directory.Exists(path))
                {
                    item = System.IO.Path.GetDirectoryName(path);
                }
                else
                {
                    continue;
                }

                if (this.Paths.ContainsKey(path))
                {
                    continue;
                }
                this.Paths[path] = item;
            }

            // URL
            var urls = this._model.GetURLs();
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
        /// パスを開く
        /// </summary>
        /// <param name="path"></param>
        static void OpenPath(string path)
        {
            var psi = new System.Diagnostics.ProcessStartInfo();
            psi.UseShellExecute = true;
            psi.FileName = path;
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
}
