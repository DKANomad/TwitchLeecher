using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data.SQLite;
using System.IO;
using System.Text;
using TwitchLeecher.Core.Data;
using TwitchLeecher.Core.Models;
using TwitchLeecher.Services.Interfaces;
using TwitchLeecher.Shared.IO;
using TwitchLeecher.Shared.Notification;

namespace TwitchLeecher.Services.Services
{
    public class PersistenceService : BindableBase, IPersistenceService
    {
        #region Constants
        const string DATABASE_NAME = "database.sqlite";
        const string DATA_DIR = "data";
        #endregion

        #region Fields
        private readonly IFolderService _folderService;
        private readonly string _dataDir;

        string _connectionString = "";

        private ObservableCollection<TwitchVideoDownload> _downloads;
        private ObservableCollection<TwitchVideoDownload> _failedDownloads;

        private delegate void TransactionDelegate(SQLiteCommand command);
        private delegate long TransactionNonQueryDelegate(SQLiteCommand command);
        #endregion

        #region Constructors

        public PersistenceService(
            IFolderService folderService)
        {
            _folderService = folderService;

            _downloads = new ObservableCollection<TwitchVideoDownload>();
            _failedDownloads = new ObservableCollection<TwitchVideoDownload>();

            _dataDir = Path.Combine(_folderService.GetAppDataFolder(), DATA_DIR);
        }

        #endregion

        #region Methods

        public void InitialiseTables()
        {
            FileSystem.CreateDirectory(_dataDir);
            var path = Path.Combine(_dataDir, DATABASE_NAME);

            if (!File.Exists(path))
            {
                var fs = File.Create(path);
                fs.Close();
            }

            _connectionString = $"Data Source={path}";

            var db = new SQLiteConnection(_connectionString);

            var downloadSql = "CREATE TABLE IF NOT EXISTS Downloads (" +
                                    "ID INTEGER PRIMARY KEY, " +
                                    "DateCreated TEXT NOT NULL, " +
                                    "DateUpdated TEXT NOT NULL, " +
                                    "DownloadId TEXT NOT NULL," +
                                    "Download TEXT NOT NULL)";

            var failedDownloadSql = "CREATE TABLE IF NOT EXISTS FailedDownloads (" +
                                        "ID INTEGER PRIMARY KEY, " +
                                        "DateCreated TEXT NOT NULL, " +
                                        "DateUpdated TEXT NOT NULL, " +
                                        "DownloadId TEXT NOT NULL," +
                                        "Download TEXT NOT NULL, " +
                                        "Retried TEXT NOT NULL)";

            try
            {
                Transact(downloadSql, (command) =>
                {
                    command.ExecuteNonQuery();
                });

                Transact(failedDownloadSql, (command) =>
                {
                    command.ExecuteNonQuery();
                });
            }
            catch(Exception ex)
            {
                throw new Exception($"Error creating the tables: {ex.Message}");
            }
        }

        public void GetDownloads()
        {
            var sql = "SELECT * FROM Downloads";
            Transact(sql, (command) =>
            {
                var records = new List<DownloadRecord>();
                var reader = command.ExecuteReader();

                while (reader.Read())
                {
                    records.Add(new DownloadRecord
                    {
                        DateCreated = DateTime.Parse(reader["DateCreated"].ToString()),
                        DateUpdated = DateTime.Parse(reader["DateUpdated"].ToString()),
                        DownloadString = (reader["Download"].ToString())
                    });
                }

                var list = new ObservableCollection<TwitchVideoDownload>();

                foreach (var itm in records)
                {
                    list.Add(JsonConvert.DeserializeObject<TwitchVideoDownload>(Encoding.UTF8.GetString(Convert.FromBase64String(itm.DownloadString))));
                }

                Downloads = list;
            });
        }

        public void GetFailedDownloads()
        {
            var sql = "SELECT * FROM FailedDownloads";
            Transact(sql, (command) =>
            {
                var reader = command.ExecuteReader();

                var records = new List<FailedRecord>();

                while (reader.Read())
                {
                    records.Add(new FailedRecord
                    {
                        DateCreated = DateTime.Parse(reader["DateCreated"].ToString()),
                        DateUpdated = DateTime.Parse(reader["DateUpdated"].ToString()),
                        DownloadString = reader["Download"].ToString(),
                        Retried = bool.Parse(reader["Retried"].ToString())
                    });
                }

                var list = new ObservableCollection<TwitchVideoDownload>();

                foreach (var itm in records)
                {
                    list.Add(JsonConvert.DeserializeObject<TwitchVideoDownload>(Encoding.UTF8.GetString(Convert.FromBase64String(itm.DownloadString))));
                }

                FailedDownloads = list;
            });
        }

        public void AddDownloadRecord(ref DownloadRecord record)
        {

            var sql = $"INSERT INTO Downloads(DateCreated, DateUpdated, DownloadId, Download) VALUES ('{DateTime.Now}', '{DateTime.Now}', '{record.Download.Id}', '{record.DownloadString}')";
            var id = TransactNonQuery(sql, (command) =>
            {
                command.ExecuteNonQuery();

                return command.Connection.LastInsertRowId;
            });

            record.ID = id;
            GetDownloads();
        }

        public void AddFailedRecord(ref FailedRecord record)
        {
            var sql = $"INSERT INTO FailedDownloads(DateCreated, DateUpdated, DownloadId, Download, Retried) VALUES ('{DateTime.Now}', '{DateTime.Now}', '{record.Download.Id}', '{record.DownloadString.Replace("\'", "\"")}', '{record.Retried}')";
            var id = TransactNonQuery(sql, (command) =>
            {
                command.ExecuteNonQuery();

                return command.Connection.LastInsertRowId;
            });
              
            record.ID = id;
            GetFailedDownloads();
        }

        public void DeleteDownloadRecord(string id)
        {
            var sql = $"DELETE FROM Downloads WHERE DownloadId='{id}'";
            Transact(sql, (command) =>
            {
                command.ExecuteNonQuery();
            });

            GetDownloads();
        }

        public void DeleteFailedRecord(string id)
        {
            var sql = $"DELETE FROM FailedDownloads WHERE DownloadId='{id}'";
            Transact(sql, (command) =>
            {
                command.ExecuteNonQuery();
            });

            GetFailedDownloads();
        }

        public void PurgeDownloads()
        {
            var sql = "DELETE FROM Downloads";
            Transact(sql, (command) =>
            {
                command.ExecuteNonQuery();
            });

            GetFailedDownloads();
        }

        public void PurgeFailedDownloads()
        {
            var sql = "DELETE FROM FailedDownloads";
            Transact(sql, (command) =>
            {
                command.ExecuteNonQuery();
            });

            GetFailedDownloads();
        }

        private void Transact(string sqlString, TransactionDelegate transactionDelegate)
        {
            using (var connection = new SQLiteConnection(_connectionString))
            {
                connection.Open();

                using (var transaction = connection.BeginTransaction())
                {
                    var command = new SQLiteCommand(connection);
                    command.CommandText = sqlString;

                    transactionDelegate(command);

                    transaction.Commit();
                }

                connection.Close();
            }
        }

        private long TransactNonQuery(string sqlString, TransactionNonQueryDelegate transactionDelegate)
        {
            using (var connection = new SQLiteConnection(_connectionString))
            {
                connection.Open();
                long result;

                using (var transaction = connection.BeginTransaction())
                {
                    var command = new SQLiteCommand(connection);
                    command.CommandText = sqlString;

                    result = transactionDelegate(command);

                    transaction.Commit();
                }

                connection.Close();
                return result;
            }
        }

        #endregion

        #region Properties

        public ObservableCollection<TwitchVideoDownload> Downloads
        {
            get
            {
                return _downloads;
            }
            private set
            {
                SetProperty(ref _downloads, value, nameof(Downloads));
            }
        }

        public ObservableCollection<TwitchVideoDownload> FailedDownloads
        {
            get
            {
                return _failedDownloads;
            }
            private set
            {
                SetProperty(ref _failedDownloads, value, nameof(FailedDownloads));
            }
        }

        #endregion
    }
}
