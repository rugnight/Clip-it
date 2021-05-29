using System;
using System.Diagnostics;
using System.Data.SQLite;

namespace Clip_it
{
    /// <summary>
    /// 本アプリケーション用のDB
    /// </summary>
    class ClipItDB
    {
        // DB
        SQLiteConnection _connection = null;
        public SQLiteConnection Connection => _connection;

        public ClipItDB(string dbFilePath)
        {
            Open(dbFilePath);
        }

        ~ClipItDB()
        {
            Close();
        }

        /// <summary>
        /// コネクションを開く
        /// </summary>
        /// <param name="dbFilePath"></param>
        /// <returns></returns>
        bool Open(string dbFilePath)
        {
            // すでに開いている
            if (_connection != null) return true;

            var sqlConnectionSb = new SQLiteConnectionStringBuilder { DataSource = dbFilePath };
            _connection = new SQLiteConnection(sqlConnectionSb.ToString());
            _connection.Open();

            return true;
        }

        /// <summary>
        /// コネクションを閉じる
        /// </summary>
        void Close()
        {
            if (_connection == null) return;

            _connection.Close();
            _connection = null;
        }

        /// <summary>
        /// SQLITEのバージョンを表示
        /// </summary>
        public void VersionDB()
        {
            Debug.Assert(_connection != null);
            if (_connection == null) return;

            using (var cmd = new SQLiteCommand(_connection))
            {
                cmd.CommandText = "select sqlite_version()";
                Console.WriteLine(cmd.ExecuteScalar());
            }
        }

    };
}
