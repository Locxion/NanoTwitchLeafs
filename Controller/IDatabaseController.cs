using System.Collections.Generic;

namespace NanoTwitchLeafs.Controller
{
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