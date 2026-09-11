using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Lives in Bootstrap.unity, on the same object as the manager singletons
/// (InventoryManager/EquipmentManager/SoundFXManager/MusicManager/TutorialManager/QuestManager/
/// ShopManager/CraftingManager). Each of those calls DontDestroyOnLoad in its own Awake(), which
/// moves it into Unity's special DontDestroyOnLoad pseudo-scene -- meaning it no longer belongs
/// to the "Bootstrap" scene at all after the first frame. Simply unloading the Bootstrap scene
/// therefore does NOT destroy them. ReleaseForReset() pulls them back into this scene right
/// before SceneFlowService unloads it, so New Game/Continue/Return to Menu actually get fresh
/// manager instances instead of carrying the previous session's forward.
/// </summary>
public sealed class BootstrapManagerLifetime : MonoBehaviour
{
    [SerializeField] private GameObject[] _persistentManagerRoots;

    public void ReleaseForReset()
    {
        if (_persistentManagerRoots == null)
            return;

        Scene bootstrapScene = gameObject.scene;
        foreach (GameObject root in _persistentManagerRoots)
        {
            if (root != null && root.scene != bootstrapScene)
                SceneManager.MoveGameObjectToScene(root, bootstrapScene);
        }
    }
}
