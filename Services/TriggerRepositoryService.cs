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
        private readonly IDatabaseService<TriggerSetting> _dbService;
        private List<TriggerSetting> _cachedCommands;

        public TriggerRepositoryService(IDatabaseService<TriggerSetting> databaseService)
        {
            _dbService = databaseService ?? throw new ArgumentNullException(nameof(databaseService));
            _dbService.SetDatabasePath(Constants.DATABASE_PATH);
        }

        public List<TriggerSetting> GetList()
        {
            if (_cachedCommands == null)
            {
                _cachedCommands = _dbService.Load();

                _cachedCommands.ForEach(triggerSetting =>
                {
                    // if null return default value
                    triggerSetting.IsActive ??= true;
                    triggerSetting.Volume ??= 50;
                });
            }

            return _cachedCommands.ToList();
        }

        public bool TriggerOfTypeExists(TriggerTypeEnum triggerTypeType)
        {
            var triggerList = GetList();
            foreach (var trigger in triggerList)
            {
                if (trigger.Trigger == triggerTypeType.ToString())
                    return true;
            }

            return false;
        }

        public TriggerSetting GetTriggerOfType(TriggerTypeEnum triggerTypeEnum)
        {
            var triggerList = GetList();
            {
                foreach (var trigger in triggerList)
                {
                    if (trigger.Trigger == triggerTypeEnum.ToString())
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

        public TriggerSetting GetByCommandName(string triggerCommand)
        {
            List<TriggerSetting> triggerList = GetList();

            foreach (TriggerSetting trigger in triggerList)
            {
                if (trigger.CMD == triggerCommand)
                {
                    return trigger;
                }
            }

            return null;
        }

        public void Update(TriggerSetting trigger)
        {
            _dbService.Update(trigger);

            var dbCommand = _cachedCommands.RemoveAll(dbCmd => dbCmd.ID == trigger.ID);
            _cachedCommands.Add(trigger);
        }

        public void Insert(TriggerSetting trigger)
        {
            _dbService.Save(trigger);

            _cachedCommands.Add(trigger);
        }

        public void Delete(TriggerSetting trigger)
        {
            _dbService.Delete(trigger);

            var dbCommand = _cachedCommands.RemoveAll(dbCmd => dbCmd.ID == trigger.ID);
        }

        public void ClearAll()
        {
            _dbService.ClearTable();

            _cachedCommands = new List<TriggerSetting>();
        }
    }
}