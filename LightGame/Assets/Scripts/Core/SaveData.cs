using System;
using System.Collections.Generic;

[Serializable]
public class SaveData
{
    public int currentLevelIndex;
    public List<int> completedLevels = new();
}
