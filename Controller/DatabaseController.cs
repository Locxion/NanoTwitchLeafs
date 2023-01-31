using System.Collections.Generic;
using log4net;
using SQLite;

namespace NanoTwitchLeafs.Controller
{
    public class DatabaseController<T> : IDatabaseController<T> where T : new()
    {
        private readonly string _databasePath;
        private readonly ILog _logger = LogManager.GetLogger(typeof(DatabaseController<T>));

        public DatabaseController(string databaseFileName)
        {
            _databasePath = databaseFileName;

            _logger.Info($"Initialize Database Controller for '{typeof(T).Name}'");
            CreateTable();
        }

        public void CreateTable()
        {
            using (var connection = new SQLiteConnection(_databasePath))
            {
                connection.CreateTable<T>();
            }
        }

        public List<T> Load()
        {
            using (var connection = new SQLiteConnection(_databasePath))
            {
                return connection.Table<T>().ToList();
            }
        }

        public void Save(T entity)
        {
            using (var connection = new SQLiteConnection(_databasePath))
            {
                connection.Insert(entity);
            }
        }

        public void Update(T entity)
        {
            using (var connection = new SQLiteConnection(_databasePath))
            {
                connection.Update(entity);
            }
        }

        public void Delete(T entity)
        {
            using (var connection = new SQLiteConnection(_databasePath))
            {
                connection.Delete(entity);
            }
        }

        public void ClearTable()
        {
            using (var connection = new SQLiteConnection(_databasePath))
            {
                connection.DropTable<T>();
                connection.CreateTable<T>();
            }
        }
    }
}