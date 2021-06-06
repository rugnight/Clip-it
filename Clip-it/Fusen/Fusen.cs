using System;
using System.Collections.Generic;
using System.Net.Http;
using AngleSharp.Html.Dom;
using AngleSharp.Html.Parser;
using System.Threading.Tasks;
using System.Numerics;
using Veldrid;
using System.Diagnostics;

namespace Clip_it
{
    /// <summary>
    /// 付箋に関するイベント通知
    /// </summary>
    interface IFusenEventHandler
    {
        public void OnFusenChangeAndEditEnd(Fusen fusen);
        public void OnFusenRequestWebInfo(string url, Action<LinkModel> callback);
        public void OnFusenRequestOpenUrl(Uri url);
        public void OnFusenRequestOpenPath(string path);
        public void OnFusenRequestCreateTexture(Uri uri, Action<Texture, IntPtr> onComplete);
        public void OnFusenRequestNotifyToggle(Fusen fusen, DateTime dateTime, bool bOn);
    }

    // テクスチャの情報
    struct TextureInfo
    {
        public Texture texture;
        public IntPtr texId;
        public TextureInfo(Texture texture, IntPtr texId)
        {
            this.texId = texId;
            this.texture = texture;
        }
    }

    /// <summary>
    /// 付箋
    /// </summary>
    class Fusen
    {
        FusenModel _model;
        FusenView _view;

        IFusenEventHandler _eventHandler;

        public FusenModel Model => _model;
        public Vector2 LastSize => _view.LastSize;
        public int LastSizeUnit => _view.LastSizeUnit;
        public Dictionary<string, LinkModel> Urls { get; private set; } = new Dictionary<string, LinkModel>();
        public Dictionary<string, string> Paths { get; private set; } = new Dictionary<string, string>();
        public Dictionary<string, bool> Dates { get; private set; } = new Dictionary<string, bool>();
        public Dictionary<DateTime, bool> DateTimes { get; private set; } = new Dictionary<DateTime, bool>();
        public Dictionary<string, TextureInfo> Images { get; private set; } = new Dictionary<string, TextureInfo>();

        /// <summary>
        /// コンストラクタ
        /// </summary>
        public Fusen(IFusenEventHandler eventHandler)
            : this(new FusenModel(), eventHandler)
        {
        }
        public Fusen(FusenModel model, IFusenEventHandler eventHandler) 
        {
            this._model = model;
            this._eventHandler = eventHandler;
            this._model.OnChangeText += (changedModel) =>
            {
                //this.UpdateMetaData();
            };
            //this.UpdateMetaData();
            // 初回の内容変更を通知
            _eventHandler?.OnFusenChangeAndEditEnd(this);

            _view = new FusenView();
            _view.OnChangeText += (changedText) =>
            {
                this._model.Text = changedText;
            };
            _view.OnSelectURL += (url) =>
            {
                _eventHandler?.OnFusenRequestOpenUrl(url);
            };
            _view.OnSelectPath += (path) =>
            {
                _eventHandler?.OnFusenRequestOpenPath(path);
            };
            _view.OnChangeAndEditEnd += () =>
            {
                // 内容変更があってテキスト入力からフォーカスが外れたらメタ情報更新
                //this.UpdateMetaData();
                _eventHandler?.OnFusenChangeAndEditEnd(this);
            };
            _view.OnToggleDateTime += (dateTime, bOn) =>
            {
                DateTimes[dateTime] = bOn;
                _eventHandler?.OnFusenRequestNotifyToggle(this, dateTime, bOn);
            };
            _view.OnClose += () =>
            {
                model.Delete();
            };
        }

        /// <summary>
        /// 更新
        /// </summary>
        public bool Update()
        {
            // 削除済みは処理しない
            if (this._model.Deleted)
            {
                return false;
            }
            // 表示
            _view.Disp(this);
            return true;
        }

        /// <summary>
        /// 入力欄にフォーカスを合わせる
        /// </summary>
        public void SetFocusInput()
        {
            _view.SetFocusInput();
        }

        /// <summary>
        /// アクティブ状態か？
        /// </summary>
        /// <returns></returns>
        public bool IsActive()
        {
            return _view.IsActive();
        }

        /// <summary>
        /// URLリストを更新
        /// </summary>
        public void UpdateMetaData()
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

            // 日時
            var dateTimes = this._model.GetDateTimes();
            foreach (var dateTime in dateTimes)
            {
                if (this.DateTimes.ContainsKey(dateTime))
                {
                    continue;
                }
                this.DateTimes[dateTime] = false;
            }

            // ファイルパス
            this.Paths.Clear();
            var paths = this.Model.GetPaths();
            foreach (var path in paths)
            {
                // 画像は独自にキャッシュしているので、ファイルが存在しなくてもある可能性がある
                LoadImage(path);

                var item = "";
                if (System.IO.File.Exists(path.LocalPath))
                {
                    item = System.IO.Path.GetFileName(path.LocalPath);
                }
                else if (System.IO.Directory.Exists(path.LocalPath))
                {
                    item = path.LocalPath;
                }
                else
                {
                    continue;
                }
                if (this.Paths.ContainsKey(path.LocalPath))
                {
                    continue;
                }
                this.Paths[path.AbsoluteUri] = item;
            }

            // URL
            this.Urls.Clear();
            var urls = this._model.GetURLs();
            foreach (var url in urls)
            {
                if (this.Urls.ContainsKey(url))
                {
                    continue;
                }
                this.Urls[url] = new LinkModel(url);

                // 非同期通信でタイトル取得
                this._eventHandler?.OnFusenRequestWebInfo(
                    url,
                    (webInfo) =>
                    {
                        this.Urls[url] = webInfo;
                        LoadImage(new UriBuilder(webInfo.OgImageUrl).Uri);
                    }
                );
                //LoadImage(url);
            }
        }


        /// <summary>
        /// 画像の読み込み
        /// </summary>
        /// <param name="path"></param>
        void LoadImage(Uri uri)
        {
            try
            {
                switch (System.IO.Path.GetExtension(uri.ToString()))
                {
                    case ".png":
                    case ".jpg":
                    case ".jpeg":
                    case ".bmp":
                    case ".gif":
                    case ".ico":
                        _eventHandler.OnFusenRequestCreateTexture(
                            uri,
                            (texture, texId) =>
                            {
                                this.Images[uri.ToString()] = new TextureInfo(texture, texId);
                            }
                            );
                        break;

                    default:
                        break;
                }
            }
            catch (System.Exception e)
            {
            }
        }
    }
}
