using System.Collections.Generic;

namespace NanoTwitchLeafs.Interfaces
{
    public interface IDatabaseService<T> where T : new()
    {
        void SetDatabasePath(string path);
        void CreateTable();
        List<T> Load();
        void Save(T entity);
        void Update(T entity);
        void Delete(T entity);
        void ClearTable();
    }
}