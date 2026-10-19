<#
.SYNOPSIS
  Sends REAL keyboard input (Windows SendInput, hardware scan codes) to the Cities: Skylines II window for test
  sessions, to verify the actual key bindings end to end.
.DESCRIPTION
  Safety rules:
    * only acts when the test-session record exists (a session started by tools\test-session.ps1);
    * brings the Cities2 window to the foreground and verifies GetForegroundWindow() is that window before EVERY step;
      if any other window is in front, it stops and sends nothing;
    * releases every key it pressed, even on error.
  Steps (executed in order):  "tap KEY", "hold KEY MS", "down KEY", "up KEY", "sleep MS", "focus"
  Keys: W A S D E V F8 SPACE LSHIFT ESC BACKSPACE TAB UP DOWN LEFT RIGHT H L
.EXAMPLE
  tools\realinput.ps1 -Steps 'focus','hold W 2000','tap E'
#>
param([Parameter(Mandatory = $true)][string[]]$Steps)

. (Join-Path $PSScriptRoot 'env.ps1')
$Steps = @($Steps | ForEach-Object { $_ -split ';' } | ForEach-Object { $_.Trim() } | Where-Object { $_ })
if (-not (Test-Path (Join-Path $DtcTestRoot 'session.json'))) { Write-Host 'REALINPUT: no active test session; refusing.'; exit 2 }
$proc = Get-Process -Name 'Cities2' -ErrorAction SilentlyContinue | Select-Object -First 1
if (-not $proc -or $proc.MainWindowHandle -eq [IntPtr]::Zero) { Write-Host 'REALINPUT: game window not found.'; exit 2 }

Add-Type -TypeDefinition @'
using System;
using System.Runtime.InteropServices;
public static class DtcInput {
    [StructLayout(LayoutKind.Sequential)] public struct KEYBDINPUT { public ushort wVk; public ushort wScan; public uint dwFlags; public uint time; public IntPtr dwExtraInfo; }
    [StructLayout(LayoutKind.Sequential)] public struct MOUSEINPUT { public int dx; public int dy; public uint mouseData; public uint dwFlags; public uint time; public IntPtr dwExtraInfo; }
    [StructLayout(LayoutKind.Explicit)] public struct INPUTUNION { [FieldOffset(0)] public MOUSEINPUT mi; [FieldOffset(0)] public KEYBDINPUT ki; }
    [StructLayout(LayoutKind.Sequential)] public struct INPUT { public uint type; public INPUTUNION u; }
    [DllImport("user32.dll", SetLastError = true)] public static extern uint SendInput(uint n, INPUT[] inputs, int size);
    [DllImport("user32.dll")] public static extern IntPtr GetForegroundWindow();
    [DllImport("user32.dll")] public static extern bool SetForegroundWindow(IntPtr h);
    [DllImport("user32.dll")] public static extern bool ShowWindow(IntPtr h, int cmd);
    [DllImport("user32.dll")] public static extern uint GetWindowThreadProcessId(IntPtr h, out uint pid);
    [DllImport("kernel32.dll")] public static extern uint GetCurrentThreadId();
    [DllImport("user32.dll")] public static extern bool AttachThreadInput(uint a, uint b, bool attach);
    public static void Key(ushort scan, bool extended, bool up) {
        INPUT[] i = new INPUT[1];
        i[0].type = 1;
        i[0].u.ki.wScan = scan;
        i[0].u.ki.dwFlags = 0x0008u | (up ? 0x0002u : 0u) | (extended ? 0x0001u : 0u);
        SendInput(1, i, Marshal.SizeOf(typeof(INPUT)));
    }
}
'@ -ErrorAction SilentlyContinue

$scan = @{
    'W' = @(0x11, $false); 'A' = @(0x1E, $false); 'S' = @(0x1F, $false); 'D' = @(0x20, $false); 'E' = @(0x12, $false)
    'V' = @(0x2F, $false); 'H' = @(0x23, $false); 'L' = @(0x26, $false); 'F8' = @(0x42, $false); 'SPACE' = @(0x39, $false)
    'LSHIFT' = @(0x2A, $false); 'ESC' = @(0x01, $false); 'BACKSPACE' = @(0x0E, $false); 'TAB' = @(0x0F, $false)
    'UP' = @(0x48, $true); 'DOWN' = @(0x50, $true); 'LEFT' = @(0x4B, $true); 'RIGHT' = @(0x4D, $true)
}
$hwnd = $proc.MainWindowHandle
$pressed = New-Object System.Collections.Generic.List[string]

function Ensure-Focus {
    if ([DtcInput]::GetForegroundWindow() -eq $hwnd) { return $true }
    $fg = [DtcInput]::GetForegroundWindow()
    $fgPid = 0
    $fgThread = [DtcInput]::GetWindowThreadProcessId($fg, [ref]$fgPid)
    $me = [DtcInput]::GetCurrentThreadId()
    [void][DtcInput]::AttachThreadInput($me, $fgThread, $true)
    [void][DtcInput]::ShowWindow($hwnd, 9)
    [void][DtcInput]::SetForegroundWindow($hwnd)
    [void][DtcInput]::AttachThreadInput($me, $fgThread, $false)
    Start-Sleep -Milliseconds 300
    return ([DtcInput]::GetForegroundWindow() -eq $hwnd)
}

function Press($k, $up) {
    $v = $scan[$k.ToUpperInvariant()]
    if (-not $v) { throw "unknown key $k" }
    if (-not (Ensure-Focus)) { throw 'game window is not in the foreground; stopping' }
    [DtcInput]::Key([uint16]$v[0], [bool]$v[1], $up)
    if ($up) { [void]$pressed.Remove($k) } else { $pressed.Add($k) }
}

$ok = $true
try {
    foreach ($s in $Steps) {
        $p = $s -split '\s+'
        switch ($p[0].ToLowerInvariant()) {
            'focus' { if (-not (Ensure-Focus)) { throw 'could not focus the game window' } }
            'tap' { Press $p[1] $false; Start-Sleep -Milliseconds 80; Press $p[1] $true }
            'hold' { Press $p[1] $false; Start-Sleep -Milliseconds ([int]$p[2]); Press $p[1] $true }
            'down' { Press $p[1] $false }
            'up' { Press $p[1] $true }
            'sleep' { Start-Sleep -Milliseconds ([int]$p[1]) }
            default { throw "unknown step $s" }
        }
        Write-Host "REALINPUT step ok: $s"
    }
} catch {
    $ok = $false
    Write-Host "REALINPUT FAILED: $($_.Exception.Message)"
} finally {
    foreach ($k in @($pressed)) { $v = $scan[$k.ToUpperInvariant()]; [DtcInput]::Key([uint16]$v[0], [bool]$v[1], $true) }
}
if ($ok) { exit 0 } else { exit 1 }
