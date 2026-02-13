namespace HyPrism.Models;

/// <summary>
/// Information about a pending update.
/// </summary>
public class UpdateInfo
{
    public int OldVersion { get; set; }
    public int NewVersion { get; set; }
    public bool HasOldUserData { get; set; }
    public string Branch { get; set; } = "";
}
