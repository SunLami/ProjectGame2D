using System;
using System.Collections.Generic;

[Serializable]
public sealed class QuestSaveData
{
    public List<QuestProgressSaveData> quests = new();
}
