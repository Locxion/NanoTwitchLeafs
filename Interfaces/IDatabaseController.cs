using System.Collections.Generic;

namespace NanoTwitchLeafs.Interfaces
{
    /// <summary>
    /// Generic database interface for CRUD operations.
    /// </summary>
    /// <typeparam name="T">Entity type.</typeparam>
    public interface IDatabaseController<T> where T : new()
    {
        void CreateTable();
        List<T> Load();
        void Save(T entity);
        void Update(T entity);
        void Delete(T entity);
        void ClearTable();
    }
}