using System.Collections.Generic;
using NanoTwitchLeafs.Enums;
using NanoTwitchLeafs.Objects;

namespace NanoTwitchLeafs.Interfaces;

public interface ITriggerRepositoryService
{
    List<Trigger> GetList();
    bool TriggerOfTypeExists(TriggerTypeEnum triggerTypeType);
    Trigger GetTriggerOfType(TriggerTypeEnum triggerTypeEnum);
    int GetCount();
    Trigger GetByCommandName(string triggerCommand);
    void Update(Trigger trigger);
    void Insert(Trigger trigger);
    void Delete(Trigger trigger);
    void ClearAll();

}