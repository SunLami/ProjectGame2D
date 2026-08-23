using UnityEngine;

/// <summary>Production IApplicationQuitter -- the only place allowed to call Application.Quit().</summary>
public sealed class UnityApplicationQuitter : IApplicationQuitter
{
    public void Quit() => Application.Quit();
}
