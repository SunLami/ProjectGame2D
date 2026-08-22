using System;

public enum GameState
{
    Booting,
    MainMenu,
    Loading,
    Playing,
    Paused,
    GameplayMenu,
    Dialogue,
    Cutscene,
    Saving,
    PlayerDead
}

public enum GameplayMenuPage
{
    None,
    Inventory,
    Equipment,
    Character,
    Crafting,
    Shop,
    QuestLog,
    Settings
}

public readonly struct GameStatePolicy
{
    public GameStatePolicy(bool pausesWorld, bool allowsGameplayInput, bool allowsUIInput, bool showsCursor)
    {
        PausesWorld = pausesWorld;
        AllowsGameplayInput = allowsGameplayInput;
        AllowsUIInput = allowsUIInput;
        ShowsCursor = showsCursor;
    }

    public bool PausesWorld { get; }
    public bool AllowsGameplayInput { get; }
    public bool AllowsUIInput { get; }
    public bool ShowsCursor { get; }
}

public readonly struct GameStateSnapshot : IEquatable<GameStateSnapshot>
{
    public GameStateSnapshot(GameState state, GameplayMenuPage menuPage = GameplayMenuPage.None)
    {
        State = state;
        MenuPage = state == GameState.GameplayMenu ? menuPage : GameplayMenuPage.None;
    }

    public GameState State { get; }
    public GameplayMenuPage MenuPage { get; }

    public bool Equals(GameStateSnapshot other) => State == other.State && MenuPage == other.MenuPage;
    public override bool Equals(object obj) => obj is GameStateSnapshot other && Equals(other);
    public override int GetHashCode() => HashCode.Combine((int)State, (int)MenuPage);
    public static bool operator ==(GameStateSnapshot left, GameStateSnapshot right) => left.Equals(right);
    public static bool operator !=(GameStateSnapshot left, GameStateSnapshot right) => !left.Equals(right);
}

public readonly struct GameStateChange
{
    public GameStateChange(GameStateSnapshot previous, GameStateSnapshot current)
    {
        Previous = previous;
        Current = current;
    }

    public GameStateSnapshot Previous { get; }
    public GameStateSnapshot Current { get; }
}
