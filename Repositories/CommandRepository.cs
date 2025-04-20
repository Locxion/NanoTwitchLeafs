using NanoTwitchLeafs.Controller;
using NanoTwitchLeafs.Enums;
using NanoTwitchLeafs.Interfaces;
using NanoTwitchLeafs.Objects;
using System;
using System.Collections.Generic;
using System.Linq;

namespace NanoTwitchLeafs.Repositories
{
    public class CommandRepository
    {
        private readonly IDatabaseController<TriggerSetting> _dbController;
        private List<TriggerSetting> _cachedCommands;

        public CommandRepository(IDatabaseController<TriggerSetting> databaseController)
        {
            _dbController = databaseController ?? throw new ArgumentNullException(nameof(databaseController));
        }

        public List<TriggerSetting> GetList()
        {
            if (_cachedCommands == null)
            {
                _cachedCommands = _dbController.Load();

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
                _cachedCommands = _dbController.Load();
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
            _dbController.Update(trigger);

            var dbCommand = _cachedCommands.RemoveAll(dbCmd => dbCmd.ID == trigger.ID);
            _cachedCommands.Add(trigger);
        }

        public void Insert(TriggerSetting trigger)
        {
            _dbController.Save(trigger);

            _cachedCommands.Add(trigger);
        }

        public void Delete(TriggerSetting trigger)
        {
            _dbController.Delete(trigger);

            var dbCommand = _cachedCommands.RemoveAll(dbCmd => dbCmd.ID == trigger.ID);
        }

        public void ClearAll()
        {
            _dbController.ClearTable();

            _cachedCommands = new List<TriggerSetting>();
        }
    }
}