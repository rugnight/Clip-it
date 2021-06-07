using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.Data.SQLite;
using ImGuiNET;

namespace Clip_it
{
    class LinkTable
    {
        const string TB_NAME = "t_link";

        ClipItDB _db = null;
        SQLiteConnection connection => _db.Connection;

        public LinkTable(ClipItDB db)
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
                    command.CommandText = $"create table {TB_NAME}(link TEXT PRIMARY KEY, title TEXT, og_image_url TEXT)";
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
        public void Save(List<LinkModel> models)
        {
            Debug.Assert(connection != null);
            if (connection == null) return;

            foreach (var model in models)
            {
                using (SQLiteTransaction trans = connection.BeginTransaction())
                {
                    try
                    {
                        using (SQLiteCommand cmd = connection.CreateCommand())
                        {
                            // 削除済み
                            if (model.Deleted)
                            {
                                // デリート
                                cmd.CommandText = $"DELETE FROM {TB_NAME} WHERE link = '{model.Uri.AbsoluteUri}'";
                            }
                            else
                            {
                                // インサート
                                cmd.CommandText = $"REPLACE INTO {TB_NAME} (link, title, og_image_url) VALUES (@Link, @Title, @Og_Image_Url)";
                                // パラメータセット
                                cmd.Parameters.Add("Link", System.Data.DbType.String);
                                cmd.Parameters.Add("Title", System.Data.DbType.String);
                                cmd.Parameters.Add("Og_Image_Url", System.Data.DbType.String);
                                // データBegin追
                                cmd.Parameters["Link"].Value = model.Uri.AbsoluteUri;
                                cmd.Parameters["Title"].Value = model.Title;
                                cmd.Parameters["Og_Image_Url"].Value = model.OgImageUrl;
                            }
                            cmd.ExecuteNonQuery();
                        }
                        // コミット
                        trans.Commit();
                    }
                    catch (System.Data.SQLite.SQLiteException e)
                    {
                        // コミット
                        //trans.Rollback();
                        Console.WriteLine("LinkModelのSave時にDB例外が発生\n%s", e.ToString());
                    }
                    catch (System.Exception e)
                    {
                        // コミット
                        trans.Rollback();
                        Console.WriteLine("LinkModelのSave時にDB例外が発生\n%s", e.ToString());
                    }
                }
            }
        }

        /// <summary>
        /// 付箋を読み込み
        /// </summary>
        /// <returns></returns>
        public List<LinkModel> Load(List<Uri> urls)
        {
            var result = new List<LinkModel>();

            Debug.Assert(connection != null);
            if (connection == null) return result;

            SQLiteCommand cmd = connection.CreateCommand();

            foreach (var url in urls)
            {
                cmd.CommandText = $"SELECT * FROM {TB_NAME} WHERE link = '{url.ToString()}'";

                using (SQLiteDataReader reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        var model = new LinkModel(
                            reader["Link"].ToString(),
                            reader["Title"].ToString(),
                            reader["Og_Image_Url"].ToString()
                            );
                        result.Add(model);
                    }
                }
            }
            return result;
        }


    }
}
