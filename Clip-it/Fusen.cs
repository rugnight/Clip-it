using System;
using System.Diagnostics;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using ImGuiNET;
using System.Net.Http;
using AngleSharp.Html.Dom;
using AngleSharp.Html.Parser;
using System.Threading.Tasks;
using System.Data.SQLite;
using System.Linq;
using System.Linq.Expressions;

namespace Clip_it
{
    class FusenDB
    {
        SQLiteConnection connection = null;

        public FusenDB()
        {
        }
        ~FusenDB()
        {
            Close();
        }


        public bool Open(string dbFilePath)
        {
            if (connection != null)
            {
                return true;
            }


            var sqlConnectionSb = new SQLiteConnectionStringBuilder { DataSource = dbFilePath };
            connection = new SQLiteConnection(sqlConnectionSb.ToString());
            connection.Open();

            return true;
        }

        public void Close()
        {
            if (connection == null) return;

            connection.Close();
            connection = null;
        }

        public void CreateDB()
        {
            Debug.Assert(connection != null);
            if (connection == null) return;

            var bExistsTable = false;

            using (SQLiteCommand command = connection.CreateCommand())
            {
                command.CommandText = "SELECT COUNT(*) FROM sqlite_master WHERE TYPE = 'table' AND name = 't_fusen'";
                var reader = command.ExecuteReader();
                bExistsTable = (0 < reader.FieldCount);
            }
            if (bExistsTable)
            {
                return;
            }

            using (SQLiteCommand command = connection.CreateCommand())
            {
                //command.CommandText = "create table t_product(id INTEGER  PRIMARY KEY AUTOINCREMENT, text TEXT)";
                command.CommandText = "create table t_fusen(id TEXT PRIMARY KEY, text TEXT, price INTEGER)";
                command.ExecuteNonQuery();
            }
        }

        public void Save(List<FusenModel> models)
        {
            Debug.Assert(connection != null);
            if (connection == null) return;

            using (SQLiteTransaction trans = connection.BeginTransaction())
            {
                SQLiteCommand cmd = connection.CreateCommand();
                foreach (var model in models)
                {
                    // 削除済み
                    if (model.Deleted)
                    {
                        // デリート
                        cmd.CommandText = $"DELETE FROM t_fusen WHERE id = '{model.Id.ToString()}'";
                    }
                    else
                    { 
                        // インサート
                        cmd.CommandText = "REPLACE INTO t_fusen (id, text) VALUES (@Id, @Text)";
                        // パラメータセット
                        cmd.Parameters.Add("Id", System.Data.DbType.String);
                        cmd.Parameters.Add("Text", System.Data.DbType.String);
                        // データ追加
                        cmd.Parameters["Id"].Value = model.Id.ToString();
                        cmd.Parameters["Text"].Value = model.Text;
                    }
                    cmd.ExecuteNonQuery();

                }
                // コミット
                trans.Commit();
            }
        }

        public List<FusenModel> Load()
        {
            var result = new List<FusenModel>();

            Debug.Assert(connection != null);
            if (connection == null) return result;

            SQLiteCommand cmd = connection.CreateCommand();
            cmd.CommandText = "SELECT * FROM t_fusen";

            using (SQLiteDataReader reader = cmd.ExecuteReader())
            {
                while (reader.Read())
                {
                    var model  = new FusenModel(
                        new Guid(reader["Id"].ToString()),
                        reader["Text"].ToString()
                        );
                    result.Add(model);
                }
            }
            return result;
        }

        public void VersionDB()
        {
            Debug.Assert(connection != null);
            if (connection == null) return;

            using (var cmd = new SQLiteCommand(connection))
            {
                cmd.CommandText = "select sqlite_version()";
                Console.WriteLine(cmd.ExecuteScalar());
            }
        }

    };

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
        string _text = "";
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

        public bool Deleted { get; set; } = false;

        // 内容更新通知
        public event Action<FusenModel> OnChangeText;

        public FusenModel() 
            : this(Guid.NewGuid(), "")
        {
        }
        public FusenModel(string text)
            : this(Guid.NewGuid(), text)
        {
        }
        public FusenModel(Guid id, string text)
        {
            this._id = id;
            this._text = (text != null) ? text : "";
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
            NonEdit,
            Edit,
        };

        public bool visible = true;
        public bool Visible { get; set; } = true;

        public event Action<string> OnChangeText;
        public event Action<string> OnSelectURL;
        public event Action OnClose;

        State state = State.NonEdit;

        public void Disp(Fusen fusen, FusenModel model)
        {
            if (!visible)
            {
                return;
            }

            var text = model.Text;
            var windowsFlags = ImGuiWindowFlags.MenuBar | ImGuiWindowFlags.NoTitleBar | ImGuiWindowFlags.AlwaysAutoResize;
            ImGui.Begin(model.Id.ToString(), ref visible, windowsFlags);

            DispText(text);

            ImGui.Spacing();
            DispURLButtons(fusen);

            ImGui.Spacing();
            DispCloseButton(fusen);

            //if (!ImGui.IsItemFocused())
            //{
            //    state = State.NonEdit;
            //}

            ImGui.End();

        }

        void DispText(string text)
        {
            var style = ImGui.GetStyle();
            var innerSpace = style.ItemInnerSpacing;
            var itemSpace = style.ItemSpacing;
            var w = (ImGui.CalcTextSize(text).X * 1.0f) + (innerSpace.X * 2.0f) + (itemSpace.X * 2.0f);
            var h = (ImGui.CalcTextSize(text).Y * 1.0f) + (innerSpace.Y * 2.0f) + (itemSpace.Y * 2.0f) + 20.0f;

            //if (state == State.Edit)
            {
                if (ImGui.InputTextMultiline("", ref text, 1024, new System.Numerics.Vector2(w, h), ImGuiInputTextFlags.None))
                {
                    this.OnChangeText?.Invoke(text);
                }
            }
            //else
            //{
            //    ImGui.Text(text);//, ref text, 1024, new System.Numerics.Vector2(w, h), ImGuiInputTextFlags.ReadOnly))
            //    if (ImGui.InvisibleButton(text, ImGui.GetItemRectSize()))
            //    {
            //        state = State.Edit;
            //    }
            //}
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
                ImGui.SameLine();
                // URL
                ImGui.LabelText("", pair.Key);
            }
        }
        void DispCloseButton(Fusen fusen)
        {
            if (ImGui.Button("X"))
            {
                visible = false;
                OnClose?.Invoke();
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

        public FusenModel Model => model;
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
            view.OnClose += () =>
            {
                model.Deleted = true;
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



}
