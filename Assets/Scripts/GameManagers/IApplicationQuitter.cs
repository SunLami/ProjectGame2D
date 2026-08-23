/// <summary>Abstraction over actually closing the application, so Quit Desktop flows are testable
/// without ever calling Application.Quit() in an automated test.</summary>
public interface IApplicationQuitter
{
    void Quit();
}
