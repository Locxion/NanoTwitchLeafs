using System;
using System.Collections.Generic;
using System.Linq;
using NanoTwitchLeafs.Enums;
using NanoTwitchLeafs.Interfaces;
using NanoTwitchLeafs.Objects;

namespace NanoTwitchLeafs.Services
{
    public class TriggerRepositoryService : ITriggerRepositoryService
    {
        private readonly IDatabaseService<Trigger> _dbService;
        private List<Trigger> _cachedCommands;

        public TriggerRepositoryService(IDatabaseService<Trigger> databaseService)
        {
            _dbService = databaseService ?? throw new ArgumentNullException(nameof(databaseService));
            _dbService.SetDatabasePath(Constants.DATABASE_PATH);
        }

        public List<Trigger> GetList()
        {
            if (_cachedCommands == null)
            {
                _cachedCommands = _dbService.Load();

                _cachedCommands.ForEach(trigger =>
                {
                    // if null return default value
                    trigger.IsActive ??= true;
                    trigger.Volume ??= 50;
                });
            }

            return _cachedCommands.ToList();
        }

        public bool TriggerOfTypeExists(TriggerTypeEnum triggerTypeType)
        {
            var triggerList = GetList();
            foreach (var trigger in triggerList)
            {
                if (trigger.Type == triggerTypeType.ToString())
                    return true;
            }

            return false;
        }

        public Trigger GetTriggerOfType(TriggerTypeEnum triggerTypeEnum)
        {
            var triggerList = GetList();
            {
                foreach (var trigger in triggerList)
                {
                    if (trigger.Type == triggerTypeEnum.ToString())
                        return trigger;
                }
            }
            return null;
        }

        public int GetCount()
        {
            if (_cachedCommands == null)
            {
                _cachedCommands = _dbService.Load();
            }

            return _cachedCommands.Count;
        }

        public Trigger GetByCommandName(string triggerCommand)
        {
            List<Trigger> triggerList = GetList();

            foreach (Trigger trigger in triggerList)
            {
                if (trigger.ChatCommand == triggerCommand)
                {
                    return trigger;
                }
            }

            return null;
        }

        public void Update(Trigger trigger)
        {
            _dbService.Update(trigger);

            var dbCommand = _cachedCommands.RemoveAll(dbCmd => dbCmd.Id == trigger.Id);
            _cachedCommands.Add(trigger);
        }

        public void Insert(Trigger trigger)
        {
            _dbService.Save(trigger);

            _cachedCommands.Add(trigger);
        }

        public void Delete(Trigger trigger)
        {
            _dbService.Delete(trigger);

            var dbCommand = _cachedCommands.RemoveAll(dbCmd => dbCmd.Id == trigger.Id);
        }

        public void ClearAll()
        {
            _dbService.ClearTable();

            _cachedCommands = new List<Trigger>();
        }
    }
}