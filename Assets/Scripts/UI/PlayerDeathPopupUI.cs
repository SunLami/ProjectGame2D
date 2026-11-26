using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public sealed class PlayerDeathPopupUI : MonoBehaviour
{
    private const string SpritePath = "UI/SessionUX/DarkInventoryStyle/player_death_";
    private const float ReturnDelaySeconds = 30f;

    private Player _player;
    private TMP_Text _countdown;
    private TMP_Text _status;
    private float _deadline;
    private bool _resolved;
    private bool _automaticReturnAttempted;

    public static void Show(Player player)
    {
        GameObject root = new(nameof(PlayerDeathPopupUI), typeof(RectTransform), typeof(Canvas),
            typeof(CanvasScaler), typeof(GraphicRaycaster), typeof(PlayerDeathPopupUI));
        Canvas canvas = root.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 1100;
        PopupViewportFit.ConfigureCanvas(root.GetComponent<CanvasScaler>());
        PlayerDeathPopupUI popup = root.GetComponent<PlayerDeathPopupUI>();
        popup._player = player;
        popup._deadline = Time.unscaledTime + ReturnDelaySeconds;
        popup.BuildPresentation();
        popup.Refresh();
    }

    private void BuildPresentation()
    {
        Image dim = WorldMessagePopupUI.CreateImage("Dim", transform, new Color(0f, 0f, 0f, 0.38f));
        WorldMessagePopupUI.Stretch(dim.rectTransform);

        Sprite boardSprite = Resources.Load<Sprite>(SpritePath + "board");
        RectTransform board = WorldMessagePopupUI.CreateBoard(transform, new Vector2(570f, 255f), boardSprite);
        if (boardSprite != null)
            board.GetComponent<Image>().color = Color.white;

        const string returnLabel = "Return to Village";
        AddText("Title", board, returnLabel, 33f, 68f, new Color(1f, 0.83f, 0.34f));
        _countdown = AddText("Countdown", board, string.Empty, 24f, 15f, Color.white);
        _status = AddText("Status", board, string.Empty, 16f, -18f, new Color(1f, 0.90f, 0.75f));

        Image image = WorldMessagePopupUI.CreateImage("ReturnButton", board, new Color(0.08f, 0.21f, 0.37f));
        WorldMessagePopupUI.SetRect(image.rectTransform, new Vector2(0f, -65f), new Vector2(264f, 49f));
        Sprite sprite = Resources.Load<Sprite>(SpritePath + "return_button");
        if (sprite != null)
        {
            image.sprite = sprite;
            image.color = Color.white;
            image.preserveAspect = true;
        }

        Button button = image.gameObject.AddComponent<Button>();
        button.targetGraphic = image;
        button.onClick.AddListener(ReturnToVillage);
        TMP_Text label = WorldMessagePopupUI.CreateText("Label", image.transform, returnLabel, 21f, Color.white);
        WorldMessagePopupUI.Stretch(label.rectTransform);
        label.raycastTarget = false;
    }

    private static TMP_Text AddText(string name, Transform parent, string value, float size, float y, Color color)
    {
        TMP_Text text = WorldMessagePopupUI.CreateText(name, parent, value, size, color);
        WorldMessagePopupUI.SetRect(text.rectTransform, new Vector2(0f, y), new Vector2(490f, 36f));
        text.raycastTarget = false;
        return text;
    }

    private void Update()
    {
        if (_player == null || GameStateManager.Instance?.CurrentState != GameState.PlayerDead)
        {
            Destroy(gameObject);
            return;
        }

        Refresh();
        if (Time.unscaledTime >= _deadline && !_automaticReturnAttempted)
        {
            _automaticReturnAttempted = true;
            ReturnToVillage();
        }
    }

    private void Refresh()
    {
        int seconds = Mathf.Max(0, Mathf.CeilToInt(_deadline - Time.unscaledTime));
        _countdown.text = seconds > 0 ? $"Automatic return in {seconds}s" : string.Empty;
    }

    private void ReturnToVillage()
    {
        if (_resolved || _player == null || !_player.IsDead)
            return;

        _resolved = true;
        Scene scene = SceneManager.GetActiveScene();
        if (scene.name == "MapNhat")
        {
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                SpawnRegistry registry = root.GetComponent<SpawnRegistry>();
                if (registry == null || !registry.TryGetSpawn(SceneTravel.VillageSpawnId, out Vector3 position))
                    continue;

                _player.WarpTo(new Vector3(position.x, position.y, _player.transform.position.z));
                _player.Revive();
                Destroy(gameObject);
                return;
            }
        }
        else if (SceneTravel.TryRespawnInVillage(scene, out _))
        {
            Destroy(gameObject);
            return;
        }

        _resolved = false;
        _automaticReturnAttempted |= Time.unscaledTime >= _deadline;
        _status.text = "Could not return to the village. Try again.";
    }
}
