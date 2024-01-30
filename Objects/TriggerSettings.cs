namespace NanoTwitchLeafs.Objects;

public class TriggerSettings : NotifyObject
{
    public TriggerSettings() : base()
    {
        ChangeBackOnCommand = true;
        ChangeBackOnKeyword = true;
    }
    public bool TriggerEnabled
    {
        get { return Get(() => TriggerEnabled); }
        set { Set(() => TriggerEnabled, value); }
    }

    public bool CooldownEnabled
    {
        get { return Get(() => CooldownEnabled); }
        set { Set(() => CooldownEnabled, value); }
    }

    public bool CooldownIgnore
    {
        get { return Get(() => CooldownIgnore); }
        set { Set(() => CooldownIgnore, value); }
    }

    public int Cooldown
    {
        get { return Get(() => Cooldown); }
        set { Set(() => Cooldown, value); }
    }

    public bool ChangeBackOnCommand
    {
        get { return Get(() => ChangeBackOnCommand); }
        set { Set(() => ChangeBackOnCommand, value); }
    }

    public bool ChangeBackOnKeyword
    {
        get { return Get(() => ChangeBackOnKeyword); }
        set { Set(() => ChangeBackOnKeyword, value); }
    }
}