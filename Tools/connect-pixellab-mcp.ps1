<#
.SYNOPSIS
    Connect the Pixellab MCP server to Claude Code for this machine.

.DESCRIPTION
    Wraps `claude mcp add` with the Pixellab HTTP endpoint so you don't have to
    remember the exact flags next time. Your API token is never written to disk
    by this script and never leaves your machine except in the request Claude
    Code itself makes to Pixellab.

.PARAMETER Token
    Your personal Pixellab API token. Get it (or regenerate it) at
    https://www.pixellab.ai/pixellab-api while signed in — shown under "API Key".

.EXAMPLE
    ./Tools/connect-pixellab-mcp.ps1 -Token 7c92716b-xxxx-xxxx-xxxx-xxxxxxxxxxxx

.NOTES
    After running this once, start a NEW Claude Code session (resuming an
    existing session does not pick up newly added MCP servers).
    Re-run this script any time to rotate/replace the token; `claude mcp add`
    overwrites the existing "pixellab" entry.
#>
param(
    [Parameter(Mandatory = $true)]
    [string]$Token
)

claude mcp add pixellab https://api.pixellab.ai/mcp -t http -H "Authorization: Bearer $Token"

Write-Host ""
Write-Host "Pixellab MCP server registered. Start a NEW Claude Code session to use it." -ForegroundColor Green
