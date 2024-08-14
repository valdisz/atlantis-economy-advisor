namespace advisor.TurnProcessing;

using System.Collections.Generic;

public record FactionRecord(int? Number = null) {
    public string Name {
        get => GetStr("Name");
        set => SetStr("Name", value);
    }

    public List<int> RewardTimes => GetAllInt("RewardTimes");
    public void AddRewardTime(int value) => SetInt("RewardTimes", value, true);

    public string Email {
        get => GetStr("Email");
        set => SetStr("Email", value);
    }

    public string Password {
        get {
            var value = GetStr("Password");
            return value == "none"
                ? null
                : value;
        }
        set => SetStr("Password", value ?? "none");
    }

    public string Battle {
        get => GetStr("Battle");
        set => SetStr("Battle", value);
    }

    public string Template {
        get => GetStr("Template");
        set => SetStr("Template", value);
    }

    public List<string> Rewards => GetAllStr("Reward");
    public void AddReward(string value) => SetStr("Reward", value, true);

    public bool SendTimes {
        get => HasProp("SendTimes");
        set => TogglFlag("SendTimes", value);
    }

    public int? LastOrders {
        get => GetInt("LastOrders");
        set => SetInt("LastOrders", value);
    }

    public int? FirstTurn {
        get => GetInt("FirstTurn");
        set => SetInt("FirstTurn", value);
    }

    // ToDo: It is possible by GM to alter palyer factions with new units, skills and items.
    //       Each directive can appear multiple times in the players file.
    // public string Loc {
    //     get => GetStr("Loc");
    //     set => SetStr("Loc", value);
    // }

    // public string NewUnit {
    //     get => GetStr("NewUnit");
    //     set => SetStr("NewUnit", value);
    // }

    // public string Item {
    //     get => GetStr("Item");
    //     set => SetStr("Item", value);
    // }

    // Skill
    // Order


    public bool IsNew => Number == null;

    public List<(string, string)> Props { get; init; } = [];

    public void Add(string name, string value) => Props.Add((name, value));

    public bool HasProp(string name) {
        for (var i = 0; i < Props.Count; i++) {
            if (Props[i].Item1 == name) {
                return true;
            }
        }

        return false;
    }

    public void SetFlag(string name) {
        if (HasProp(name)) {
            return;
        }

        Add(name, null);
    }

    public void ClearFlag(string name) {
        for (var i = 0; i < Props.Count; i++) {
            if (Props[i].Item1 == name) {
                Props.RemoveAt(i);
                return;
            }
        }
    }

    public void TogglFlag(string name, bool set) {
        if (set) {
            SetFlag(name);
        }
        else {
            ClearFlag(name);
        }
    }

    public string GetStr(string name) {
        for (var i = 0; i < Props.Count; i++) {
            if (Props[i].Item1 == name) {
                return Props[i].Item2;
            }
        }

        return null;
    }

    public List<string> GetAllStr(string name) {
        var result = new List<string>();
        for (var i = 0; i < Props.Count; i++) {
            if (Props[i].Item1 == name) {
                var value = Props[i].Item2;
                if (string.IsNullOrWhiteSpace(value)) {
                    continue;
                }

                result.Add(value);
            }
        }

        return result;
    }

    public void SetStr(string name, string value, bool multiple = false) {
        if (multiple) {
            Add(name, value);
            return;
        }

        for (var i = 0; i < Props.Count; i++) {
            if (Props[i].Item1 == name) {
                Props[i] = (name, value);
                return;
            }
        }

        Props.Add((name, value));
    }

    public int? GetInt(string name) {
        var value = GetStr(name);
        return value == null
            ? null
            : int.Parse(value);
    }

    public List<int> GetAllInt(string name) => GetAllStr(name).ConvertAll(int.Parse);

    public void SetInt(string name, int? value, bool multiple = false) => SetStr(name, value?.ToString(), multiple);
}
