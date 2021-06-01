using ImGuiNET;
using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Diagnostics;
using System.Text;

namespace Clip_it
{
    class MediaModel
    {
        public string UriRaw { get; private set; } = "";

        public Uri Uri => new Uri(UriRaw);

        public string Base64 { get; set; } = "";

        public bool Deleted { get; set; } = false;


        public MediaModel(string uri, string base64)
        {
            this.UriRaw = uri;
            this.Base64 = base64;
        }
    };

    class MediaTable
    {
        const string TB_NAME = "t_media";

        ClipItDB _db = null;
        SQLiteConnection connection => _db.Connection;

        public MediaTable(ClipItDB db)
        {
            _db = db;
        }

        /// <summary>
        /// テーブルを生成
        /// </summary>
        public void CreateTable()
        {
            Debug.Assert(connection != null);
            if (connection == null) return;

            try
            {
                using (SQLiteCommand command = connection.CreateCommand())
                {
                    command.CommandText = $"create table {TB_NAME}(uri TEXT PRIMARY KEY, base64 TEXT)";
                    command.ExecuteNonQuery();
                }
            }
            catch(Exception e)
            {
                ImGui.LogText(e.Message);
            }
        }



        /// <summary>
        /// 付箋を保存
        /// </summary>
        /// <param name="models"></param>
        public void Save(List<MediaModel> models)
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
                        cmd.CommandText = $"DELETE FROM {TB_NAME} WHERE link = '{model.Uri.ToString()}'";
                    }
                    else
                    { 
                        // インサート
                        cmd.CommandText = $"REPLACE INTO {TB_NAME} (uri, base64) VALUES (@Uri, @Base64)";
                        // パラメータセット
                        cmd.Parameters.Add("Uri", System.Data.DbType.String);
                        cmd.Parameters.Add("Base64", System.Data.DbType.String);
                        // データ追加
                        cmd.Parameters["Uri"].Value = model.UriRaw;
                        cmd.Parameters["Base64"].Value = model.Base64;
                    }
                    cmd.ExecuteNonQuery();

                }
                // コミット
                trans.Commit();
            }
        }

        /// <summary>
        /// 付箋を読み込み
        /// </summary>
        /// <returns></returns>
        public List<MediaModel> Load(List<string> uris)
        {
            var result = new List<MediaModel>();

            Debug.Assert(connection != null);
            if (connection == null) return result;

            SQLiteCommand cmd = connection.CreateCommand();

            foreach (var uri in uris)
            {
                cmd.CommandText = $"SELECT * FROM {TB_NAME} WHERE uri = '{uri}'";

                using (SQLiteDataReader reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        var model = new MediaModel(
                            reader["Uri"].ToString(),
                            reader["Base64"].ToString()
                            );
                        result.Add(model);
                    }
                }
            }
            return result;
        }


    }
}
