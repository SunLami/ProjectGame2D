using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;
using Debug = UnityEngine.Debug;

/// <summary>
/// Runs Tools/connect-pixellab-mcp.ps1 from inside the Editor so the token never has to be typed
/// into a terminal manually. The token is only ever passed as a process argument to PowerShell —
/// never logged, never written to disk by this window.
/// </summary>
public class PixellabMcpConnectorWindow : EditorWindow
{
    private string _token = string.Empty;
    private string _output = string.Empty;
    private bool _isRunning;

    [MenuItem("Tools/ProjectGame2D/Pixellab/Connect MCP...")]
    private static void Open()
    {
        var window = GetWindow<PixellabMcpConnectorWindow>(true, "Connect Pixellab MCP");
        window.minSize = new Vector2(420, 220);
    }

    private void OnGUI()
    {
        EditorGUILayout.HelpBox(
            "Lấy API token tại https://www.pixellab.ai/pixellab-api (đăng nhập > mục \"API Key\"). " +
            "Token chỉ được dùng để chạy claude mcp add, không lưu vào project.",
            MessageType.Info);

        EditorGUILayout.Space();
        GUI.SetNextControlName("token");
        _token = EditorGUILayout.PasswordField("Pixellab API Token", _token);

        EditorGUILayout.Space();
        using (new EditorGUI.DisabledScope(_isRunning || string.IsNullOrWhiteSpace(_token)))
        {
            if (GUILayout.Button("Connect", GUILayout.Height(28)))
                Connect();
        }

        if (!string.IsNullOrEmpty(_output))
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Output:", EditorStyles.boldLabel);
            EditorGUILayout.TextArea(_output, GUILayout.MinHeight(80));

            if (_output.Contains("registered"))
            {
                EditorGUILayout.HelpBox(
                    "Đã đăng ký MCP server. Mở một phiên Claude Code MỚI để dùng — resume phiên cũ " +
                    "sẽ không thấy tool Pixellab.",
                    MessageType.Warning);
            }
        }
    }

    private void Connect()
    {
        string projectRoot = Directory.GetParent(Application.dataPath)!.FullName;
        string scriptPath = Path.Combine(projectRoot, "Tools", "connect-pixellab-mcp.ps1");

        if (!File.Exists(scriptPath))
        {
            _output = $"Không tìm thấy script tại {scriptPath}";
            return;
        }

        _isRunning = true;
        _output = "Đang chạy claude mcp add...";

        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = "powershell.exe",
                Arguments = $"-NoProfile -ExecutionPolicy Bypass -File \"{scriptPath}\" -Token \"{_token}\"",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
            };

            using var process = Process.Start(psi);
            var stdout = new StringBuilder();
            var stderr = new StringBuilder();
            process!.OutputDataReceived += (_, e) => { if (e.Data != null) stdout.AppendLine(e.Data); };
            process.ErrorDataReceived += (_, e) => { if (e.Data != null) stderr.AppendLine(e.Data); };
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();
            process.WaitForExit(30000);

            _output = stdout.ToString();
            if (stderr.Length > 0)
                _output += "\n[stderr]\n" + stderr;

            // Never keep the token around after the call, and never let it linger in this window.
            _token = string.Empty;
        }
        catch (Exception ex)
        {
            _output = $"Lỗi khi chạy script: {ex.Message}";
            Debug.LogError($"[PixellabMcpConnector] {ex.Message}");
        }
        finally
        {
            _isRunning = false;
            Repaint();
        }
    }
}
