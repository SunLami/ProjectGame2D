using System.Reflection;
using NUnit.Framework;

public sealed class PauseMenuUISaveSlotPlayModeTests
{
    private const BindingFlags PrivateStatic = BindingFlags.NonPublic | BindingFlags.Static;

    [TestCase(SaveSlotStatus.Valid, "OVERWRITE THE SAVE IN SLOT 2?")]
    [TestCase(SaveSlotStatus.Corrupted, "SLOT 2 IS CORRUPTED.\nDELETE IT AND SAVE HERE?")]
    [TestCase(SaveSlotStatus.IncompatibleVersion, "SLOT 2 IS INCOMPATIBLE.\nDELETE IT AND SAVE HERE?")]
    public void OverwriteConfirmation_UsesStatusSpecificText(SaveSlotStatus status, string expected)
    {
        MethodInfo method = typeof(PauseMenuUI).GetMethod("GetOverwriteConfirmationText", PrivateStatic);

        Assert.NotNull(method);
        Assert.AreEqual(expected, method.Invoke(null, new object[] { 2, status }));
    }

    [Test]
    public void DeleteConfirmation_IdentifiesSlotAndIrreversibleAction()
    {
        MethodInfo method = typeof(PauseMenuUI).GetMethod("GetDeleteConfirmationText", PrivateStatic);

        Assert.NotNull(method);
        Assert.AreEqual("DELETE SAVE IN SLOT 3?\nTHIS CANNOT BE UNDONE.",
            method.Invoke(null, new object[] { 3 }));
    }
}
