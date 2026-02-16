using System;

/// <summary>
/// Tracks mirrors the player is carrying. Pure C# class (no MonoBehaviour)
/// so it can be tested in EditMode without a running scene.
/// </summary>
public class Inventory
{
    public int MirrorCount { get; private set; }

    public event Action<int> OnMirrorCountChanged;

    public void AddMirror()
    {
        MirrorCount++;
        OnMirrorCountChanged?.Invoke(MirrorCount);
    }

    /// <summary>
    /// Removes a mirror from inventory. Returns false if inventory is empty.
    /// </summary>
    public bool RemoveMirror()
    {
        if (MirrorCount <= 0)
            return false;

        MirrorCount--;
        OnMirrorCountChanged?.Invoke(MirrorCount);
        return true;
    }
}
