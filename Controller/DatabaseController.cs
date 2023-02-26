using log4net;
using NanoTwitchLeafs.Interfaces;
using SQLite;
using System.Collections.Generic;

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

        /// <summary>
        /// Creates a New Table if Table not exists
        /// </summary>
        public void CreateTable()
        {
            using (var connection = new SQLiteConnection(_databasePath))
            {
                connection.CreateTable<T>();
            }
        }

		/// <summary>
		/// Loads all Entries from the Table
		/// </summary>
		/// <returns>List<T></returns>
		public List<T> Load()
        {
            using (var connection = new SQLiteConnection(_databasePath))
            {
                return connection.Table<T>().ToList();
            }
        }

        /// <summary>
        /// Saves a new Entry
        /// </summary>
        /// <param name="entity"></param>
        public void Save(T entity)
        {
            using (var connection = new SQLiteConnection(_databasePath))
            {
                connection.Insert(entity);
            }
        }

        /// <summary>
        /// Updates existing Entry
        /// </summary>
        /// <param name="entity"></param>
        public void Update(T entity)
        {
            using (var connection = new SQLiteConnection(_databasePath))
            {
                connection.Update(entity);
            }
        }

        /// <summary>
        /// Deletes Entry
        /// </summary>
        /// <param name="entity"></param>
        public void Delete(T entity)
        {
            using (var connection = new SQLiteConnection(_databasePath))
            {
                connection.Delete(entity);
            }
        }

        /// <summary>
        /// Deletes whole Table and creates it New
        /// </summary>
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