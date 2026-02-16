using System.Collections.Generic;
using System.Linq;
using FsCheck;
using NUnit.Framework;
using UnityEngine;

// Feature: light-puzzle-game, Property 15: Save/load round-trip

[TestFixture]
public class SaveDataTests
{
    /// <summary>
    /// Property 15: Save/load round-trip
    /// For any valid SaveData (current level index, completed levels list),
    /// serializing to JSON and deserializing back produces an equivalent object.
    /// Validates: Requirements 11.4
    /// </summary>
    [Test]
    public void SaveDataSerializeDeserializeRoundTrip_PreservesAllFields()
    {
        var gen = from levelIndex in Gen.Choose(0, 99)
                  from completedCount in Gen.Choose(0, levelIndex + 1)
                  from completedLevels in Gen.ListOf(completedCount, Gen.Choose(0, 99))
                  select new SaveData
                  {
                      currentLevelIndex = levelIndex,
                      completedLevels = completedLevels.Distinct().OrderBy(x => x).ToList()
                  };

        var arb = Arb.From(gen);

        Prop.ForAll(arb, original =>
        {
            string json = JsonUtility.ToJson(original);
            var deserialized = JsonUtility.FromJson<SaveData>(json);

            Assert.AreEqual(original.currentLevelIndex, deserialized.currentLevelIndex,
                "currentLevelIndex mismatch");
            Assert.AreEqual(original.completedLevels.Count, deserialized.completedLevels.Count,
                "completedLevels count mismatch");

            for (int i = 0; i < original.completedLevels.Count; i++)
            {
                Assert.AreEqual(original.completedLevels[i], deserialized.completedLevels[i],
                    $"completedLevels[{i}] mismatch");
            }
        }).QuickCheckThrowOnFailure();
    }
}
