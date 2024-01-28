using System.Collections.Generic;
using NanoTwitchLeafs.Enums;
using NanoTwitchLeafs.Objects;

namespace NanoTwitchLeafs.Interfaces;

public interface ITriggerRepositoryService
{
    List<TriggerSetting> GetList();
    bool TriggerOfTypeExists(TriggerTypeEnum triggerTypeType);
    TriggerSetting GetTriggerOfType(TriggerTypeEnum triggerTypeEnum);
    int GetCount();
    TriggerSetting GetByCommandName(string triggerCommand);
    void Update(TriggerSetting trigger);
    void Insert(TriggerSetting trigger);
    void Delete(TriggerSetting trigger);
    void ClearAll();

}