using NUnit.Framework;
using UnityEngine.TestTools;

public sealed class GameSessionManagerPlayModeTests
{
    [Test]
    public void DefaultSaveRepository_IsConstructedWithoutError()
    {
        // Regression: SaveRepository used to be a field initializer that called
        // Application.persistentDataPath outside Awake, which Unity forbids and throws for.
        LogAssert.NoUnexpectedReceived();
        Assert.IsNotNull(GameSessionManager.Instance.SaveRepository);
        Assert.IsInstanceOf<FileSaveSlotRepository>(GameSessionManager.Instance.SaveRepository);
    }
}
