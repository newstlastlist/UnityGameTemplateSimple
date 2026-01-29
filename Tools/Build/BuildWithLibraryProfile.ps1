param(
    [Parameter(Mandatory = $true)]
    [string]$ProjectPath,

    [Parameter(Mandatory = $true)]
    [string]$UnityPath,

    [Parameter(Mandatory = $true)]
    [ValidateSet("Dev", "Release")]
    [string]$Profile,

    [Parameter(Mandatory = $true)]
    [ValidateSet("Apk", "Aab")]
    [string]$Artifact,

    [Parameter(Mandatory = $true)]
    [string]$OutputPath,

    [Parameter(Mandatory = $true)]
    [int]$EditorPid
,
    [Parameter(Mandatory = $false)]
    [switch]$AggressiveUnlock
)

$ErrorActionPreference = "Stop"

$logFilePath = Join-Path (Resolve-Path $ProjectPath).Path "UserSettings\BuildAutomationSwitch.log"

function Write-Log([string]$Message)
{
    $timestamp = (Get-Date).ToString("HH:mm:ss")
    $line = "[$timestamp] $Message"
    Write-Host $line
    try
    {
        $directoryPath = Split-Path -Parent $logFilePath
        if (-not (Test-Path $directoryPath))
        {
            New-Item -ItemType Directory -Path $directoryPath | Out-Null
        }
        Add-Content -Path $logFilePath -Value $line -Encoding UTF8
    }
    catch
    {
        # ignore log write errors
    }
}

function Start-UnityGuiBuild([string]$ProjectRoot, [string]$UnityEditorPath, [string]$BuildProfile, [string]$BuildArtifact, [string]$BuildOutputPath)
{
    $executeMethod = "Editor.BuildTools.BuildGuiEntryPoint.PerformGuiBuild"
    $arguments = @(
        "-projectPath", "`"$ProjectRoot`"",
        "-buildTarget", "Android",
        "-executeMethod", $executeMethod,
        "-profile=$BuildProfile",
        "-artifact=$BuildArtifact",
        "-outputPath=`"$BuildOutputPath`""
    )

    $argumentLine = $arguments -join " "
    Write-Log "Starting Unity GUI build..."
    Write-Log "Unity: $UnityEditorPath"
    Write-Log "Args:  $argumentLine"

    Start-Process -FilePath $UnityEditorPath -ArgumentList $argumentLine | Out-Null
}

function Get-ProfileMarkerPath([string]$ProjectRoot)
{
    return (Join-Path $ProjectRoot ".library_profile")
}

function Get-CurrentProfile([string]$ProjectRoot)
{
    $markerPath = Get-ProfileMarkerPath $ProjectRoot
    if (Test-Path $markerPath)
    {
        $value = (Get-Content $markerPath -Raw).Trim()
        if ($value -eq "Dev" -or $value -eq "Release")
        {
            return $value
        }
    }

    return ""
}

function Set-CurrentProfile([string]$ProjectRoot, [string]$ProfileValue)
{
    $markerPath = Get-ProfileMarkerPath $ProjectRoot
    Set-Content -Path $markerPath -Value $ProfileValue -Encoding UTF8
}

function Wait-UnityToExit([int]$UnityEditorProcessId)
{
    Write-Log "Waiting Unity Editor PID=$UnityEditorProcessId to exit..."
    while ($true)
    {
        $process = Get-Process -Id $UnityEditorProcessId -ErrorAction SilentlyContinue
        if ($null -eq $process)
        {
            break
        }

        Start-Sleep -Seconds 1
    }
    Write-Log "Unity Editor has exited."
}

function Stop-UnityRelatedProcesses([string]$ProjectRoot)
{
    # На Windows иногда остаются фоновые процессы, которые держат хэндлы на Library (ShaderCompiler/Bee/PackageManager).
    # В момент свитча Unity уже закрыта, поэтому можно быть заметно агрессивнее и прибивать хвост билд-инструментов.
    $projectRootLower = $ProjectRoot.ToLowerInvariant()

    $alwaysSafeNames = @(
        "UnityShaderCompiler",
        "bee_backend",
        "BeeBackend",
        "UnityPackageManager",
        "UnityPackageManagerServer",
        "UnityCrashHandler64",
        "UnityCrashHandler",
        "UnityLicensingClient",
        "VBCSCompiler",
        "msbuild",
        "dotnet",
        "il2cpp",
        "clang",
        "clang++",
        "lld",
        "adb",
        "gradle",
        "java",
        "javaw"
    )

    foreach ($name in $alwaysSafeNames)
    {
        try
        {
            $processes = Get-Process -Name $name -ErrorAction SilentlyContinue
            foreach ($process in $processes)
            {
                Write-Log "Stopping process: $($process.ProcessName) PID=$($process.Id)"
                Stop-Process -Id $process.Id -Force -ErrorAction SilentlyContinue
            }
        }
        catch
        {
        }
    }

    # Дополнительно пробуем прибить процессы по имени exe через CIM (на случай, если Get-Process не видит/имя другое).
    $conditionallySafeNames = @(
        "java.exe",
        "javaw.exe",
        "adb.exe",
        "gradle.exe",
        "dotnet.exe",
        "msbuild.exe",
        "VBCSCompiler.exe",
        "il2cpp.exe",
        "UnityCrashHandler64.exe",
        "UnityCrashHandler.exe",
        "bee_backend.exe",
        "BeeBackend.exe",
        "UnityShaderCompiler.exe",
        "UnityPackageManager.exe",
        "UnityPackageManagerServer.exe",
        "UnityLicensingClient.exe",
        "cmd.exe"
    )
    foreach ($exeName in $conditionallySafeNames)
    {
        try
        {
            $cimProcesses = Get-CimInstance Win32_Process -Filter ("Name='" + $exeName.Replace("'", "''") + "'") -ErrorAction SilentlyContinue
            foreach ($cimProcess in $cimProcesses)
            {
                $commandLine = [string]$cimProcess.CommandLine
                if ([string]::IsNullOrWhiteSpace($commandLine))
                {
                    continue
                }

                # Если это процесс из сборки/Unity - обычно в CommandLine есть путь к проекту.
                # Но иногда его нет (например adb). В этом случае для перечисленных выше exe мы всё равно прибиваем их,
                # потому что свитч делается после закрытия Unity.
                $isProjectRelated = $commandLine.ToLowerInvariant().Contains($projectRootLower)
                if ($isProjectRelated -or $exeName -ne "cmd.exe")
                {
                    Write-Log "Stopping process (CIM): $($cimProcess.Name) PID=$($cimProcess.ProcessId)"
                    Stop-Process -Id $cimProcess.ProcessId -Force -ErrorAction SilentlyContinue
                }
            }
        }
        catch
        {
        }
    }

    # Последний шаг: прибиваем оставшиеся Unity.exe, относящиеся к этому проекту (по CommandLine).
    # Иногда именно второй Unity.exe (не тот PID, который мы ждали) продолжает держать хэндлы на Library.
    try
    {
        $unityProcesses = Get-CimInstance Win32_Process -Filter "Name='Unity.exe'" -ErrorAction SilentlyContinue
        foreach ($unityProcess in $unityProcesses)
        {
            $commandLine = [string]$unityProcess.CommandLine
            if ([string]::IsNullOrWhiteSpace($commandLine))
            {
                continue
            }

            if ($commandLine.ToLowerInvariant().Contains($projectRootLower))
            {
                Write-Log "Stopping Unity.exe (project match): PID=$($unityProcess.ProcessId)"
                Stop-Process -Id $unityProcess.ProcessId -Force -ErrorAction SilentlyContinue
            }
        }
    }
    catch
    {
    }
}

function Invoke-AggressiveUnlock([string]$ProjectRoot)
{
    Write-Log "Aggressive unlock: enabled. Trying to release file locks..."

    # 1) Stop Unity-related processes again (best effort)
    Stop-UnityRelatedProcesses -ProjectRoot $ProjectRoot

    # 1.5) Try to find and kill processes holding file handles under Library (best-effort, no external tools).
    try
    {
        $libraryDirectoryPath = Join-Path $ProjectRoot "Library"
        $scanResult = Get-LockingPidsByHandleScan -PathPrefix $libraryDirectoryPath
        if ($null -eq $scanResult)
        {
            Write-Log "Aggressive unlock: handle scan failed (no result)."
        }
        else
        {
            $pids = $scanResult.Pids
            $timedOut = [bool]$scanResult.TimedOut
            $hadError = [bool]$scanResult.HadError
            $errorText = [string]$scanResult.Error

            if ($hadError -and -not [string]::IsNullOrWhiteSpace($errorText))
            {
                Write-Log "Aggressive unlock: handle scan error: $errorText"
            }

            if ($timedOut)
            {
                Write-Log "Aggressive unlock: handle scan timed out (results may be incomplete)."
            }

            if ($null -ne $pids -and $pids.Count -gt 0)
            {
                Write-Log "Aggressive unlock: handle scan found $($pids.Count) process(es) locking Library."
                foreach ($pid in $pids)
                {
                    try
                    {
                        $process = Get-Process -Id $pid -ErrorAction SilentlyContinue
                        $name = if ($null -ne $process) { $process.ProcessName } else { "unknown" }
                        Write-Log "Aggressive unlock: stopping locking process PID=$pid Name=$name"
                        Stop-Process -Id $pid -Force -ErrorAction SilentlyContinue
                    }
                    catch
                    {
                    }
                }
            }
            else
            {
                Write-Log "Aggressive unlock: handle scan found no locking process for Library."
            }
        }
    }
    catch
    {
        Write-Log "Aggressive unlock: handle scan failed (best effort)."
    }

    # 2) Temporarily stop Windows Search (often holds file handles). Best-effort.
    try
    {
        $service = Get-Service -Name "WSearch" -ErrorAction SilentlyContinue
        if ($null -ne $service -and $service.Status -eq "Running")
        {
            Write-Log "Aggressive unlock: stopping service WSearch..."
            Stop-Service -Name "WSearch" -Force -ErrorAction SilentlyContinue
        }
    }
    catch
    {
    }

    # 3) Restart Explorer shell (can hold directory handles via shell extensions). Best-effort.
    try
    {
        $explorer = Get-Process -Name "explorer" -ErrorAction SilentlyContinue
        if ($null -ne $explorer)
        {
            Write-Log "Aggressive unlock: restarting explorer.exe..."
            Stop-Process -Name "explorer" -Force -ErrorAction SilentlyContinue
            Start-Sleep -Seconds 1
            Start-Process -FilePath "explorer.exe" | Out-Null
        }
    }
    catch
    {
    }

    Start-Sleep -Seconds 2
}

function Get-LockingPidsByHandleScan([string]$PathPrefix)
{
    # Пытаемся найти процессы, которые держат открытые файловые хэндлы на пути (или внутри) PathPrefix.
    # Реализовано через WinAPI (NtQuerySystemInformation + DuplicateHandle + GetFinalPathNameByHandle).
    # В отличие от Restart Manager это помогает при "невидимых" локах.
    try
    {
        if (-not ("BuildAutomation.HandleScannerV2" -as [type]))
        {
            Add-Type -TypeDefinition @"
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Text;

namespace BuildAutomation
{
    public sealed class HandleScanResult
    {
        public bool TimedOut;
        public bool HadError;
        public string Error;
        public int[] Pids = Array.Empty<int>();
    }

    public static class HandleScannerV2
    {
        private const int SystemExtendedHandleInformation = 64;
        private const uint STATUS_INFO_LENGTH_MISMATCH = 0xC0000004;
        private const uint DUPLICATE_SAME_ACCESS = 0x00000002;
        private const uint FILE_NAME_NORMALIZED = 0x0;

        [StructLayout(LayoutKind.Sequential)]
        private struct SYSTEM_HANDLE_TABLE_ENTRY_INFO_EX
        {
            public IntPtr Object;
            public IntPtr UniqueProcessId;
            public IntPtr HandleValue;
            public uint GrantedAccess;
            public ushort CreatorBackTraceIndex;
            public ushort ObjectTypeIndex;
            public uint HandleAttributes;
            public uint Reserved;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct SYSTEM_HANDLE_INFORMATION_EX
        {
            public IntPtr NumberOfHandles;
            public IntPtr Reserved;
            // followed by SYSTEM_HANDLE_TABLE_ENTRY_INFO_EX array
        }

        [DllImport("ntdll.dll")]
        private static extern int NtQuerySystemInformation(
            int SystemInformationClass,
            IntPtr SystemInformation,
            int SystemInformationLength,
            out int ReturnLength);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern IntPtr OpenProcess(uint processAccess, bool bInheritHandle, int processId);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool CloseHandle(IntPtr hObject);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool DuplicateHandle(
            IntPtr hSourceProcessHandle,
            IntPtr hSourceHandle,
            IntPtr hTargetProcessHandle,
            out IntPtr lpTargetHandle,
            uint dwDesiredAccess,
            bool bInheritHandle,
            uint dwOptions);

        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        private static extern uint GetFinalPathNameByHandle(
            IntPtr hFile,
            [Out] StringBuilder lpszFilePath,
            uint cchFilePath,
            uint dwFlags);

        private const uint PROCESS_DUP_HANDLE = 0x0040;
        private static readonly IntPtr CurrentProcess = GetCurrentProcess();

        [DllImport("kernel32.dll")]
        private static extern IntPtr GetCurrentProcess();

        public static HandleScanResult FindPidsHoldingPathPrefix(string pathPrefix, int maxMilliseconds)
        {
            var result = new HandleScanResult();
            if (string.IsNullOrWhiteSpace(pathPrefix))
            {
                result.Pids = Array.Empty<int>();
                return result;
            }

            // Normalize prefix for comparisons
            string prefix = pathPrefix.TrimEnd('\\').ToLowerInvariant();

            int length = 0x10000;
            IntPtr buffer = IntPtr.Zero;
            try
            {
                long startTicks = Environment.TickCount64;
                while (true)
                {
                    if (Environment.TickCount64 - startTicks > maxMilliseconds)
                    {
                        result.TimedOut = true;
                        result.Pids = Array.Empty<int>();
                        return result;
                    }
                    buffer = Marshal.AllocHGlobal(length);
                    int returned;
                    int status = NtQuerySystemInformation(SystemExtendedHandleInformation, buffer, length, out returned);
                    if ((uint)status == STATUS_INFO_LENGTH_MISMATCH)
                    {
                        Marshal.FreeHGlobal(buffer);
                        buffer = IntPtr.Zero;
                        length = Math.Max(length * 2, returned);
                        continue;
                    }
                    if (status != 0)
                    {
                        result.HadError = true;
                        result.Error = "NtQuerySystemInformation failed with status=" + status;
                        result.Pids = Array.Empty<int>();
                        return result;
                    }
                    break;
                }

                long handleCount = buffer.ToInt64() == 0 ? 0 : Marshal.ReadIntPtr(buffer).ToInt64();
                IntPtr handleEntryPtr = IntPtr.Add(buffer, IntPtr.Size * 2);
                int entrySize = Marshal.SizeOf(typeof(SYSTEM_HANDLE_TABLE_ENTRY_INFO_EX));

                HashSet<int> pids = new HashSet<int>();
                StringBuilder sb = new StringBuilder(2048);

                for (long i = 0; i < handleCount; i++)
                {
                    if (Environment.TickCount64 - startTicks > maxMilliseconds)
                    {
                        result.TimedOut = true;
                        break;
                    }

                    SYSTEM_HANDLE_TABLE_ENTRY_INFO_EX entry = Marshal.PtrToStructure<SYSTEM_HANDLE_TABLE_ENTRY_INFO_EX>(handleEntryPtr);
                    handleEntryPtr = IntPtr.Add(handleEntryPtr, entrySize);

                    int pid = entry.UniqueProcessId.ToInt32();
                    if (pid <= 0 || pid == System.Diagnostics.Process.GetCurrentProcess().Id)
                    {
                        continue;
                    }

                    IntPtr processHandle = OpenProcess(PROCESS_DUP_HANDLE, false, pid);
                    if (processHandle == IntPtr.Zero)
                    {
                        continue;
                    }

                    IntPtr duplicated;
                    bool ok = DuplicateHandle(processHandle, entry.HandleValue, CurrentProcess, out duplicated, 0, false, DUPLICATE_SAME_ACCESS);
                    CloseHandle(processHandle);
                    if (!ok || duplicated == IntPtr.Zero)
                    {
                        continue;
                    }

                    try
                    {
                        sb.Clear();
                        uint pathLength = GetFinalPathNameByHandle(duplicated, sb, (uint)sb.Capacity, FILE_NAME_NORMALIZED);
                        if (pathLength == 0)
                        {
                            continue;
                        }

                        // If buffer was too small, allocate required size and retry once.
                        if (pathLength >= sb.Capacity)
                        {
                            sb = new StringBuilder((int)pathLength + 2);
                            pathLength = GetFinalPathNameByHandle(duplicated, sb, (uint)sb.Capacity, FILE_NAME_NORMALIZED);
                            if (pathLength == 0)
                            {
                                continue;
                            }
                        }

                        string finalPath = sb.ToString();
                        if (string.IsNullOrWhiteSpace(finalPath))
                        {
                            continue;
                        }

                        // Normalize \\?\ prefix and compare
                        string normalized = finalPath;
                        if (normalized.StartsWith(@"\\?\"))
                        {
                            normalized = normalized.Substring(4);
                        }
                        normalized = normalized.Replace('/', '\\').TrimEnd('\\').ToLowerInvariant();

                        if (normalized.StartsWith(prefix))
                        {
                            pids.Add(pid);
                        }
                    }
                    finally
                    {
                        CloseHandle(duplicated);
                    }
                }

                int[] resultPids = new int[pids.Count];
                pids.CopyTo(resultPids);
                result.Pids = resultPids;
                return result;
            }
            finally
            {
                if (buffer != IntPtr.Zero)
                {
                    Marshal.FreeHGlobal(buffer);
                }
            }
        }
    }
}
"@ -ErrorAction Stop | Out-Null
        }

        $scanResult = [BuildAutomation.HandleScannerV2]::FindPidsHoldingPathPrefix($PathPrefix, 5000)
        return $scanResult
    }
    catch
    {
        try
        {
            $exceptionType = if ($null -ne $_.Exception) { [string]$_.Exception.GetType().FullName } else { "" }
            $message = if ($null -ne $_.Exception) { [string]$_.Exception.Message } else { [string]$_ }
            Write-Log "Handle scan exception: Type=$exceptionType Message=$message"
        }
        catch
        {
        }

        return $null
    }
}

function Get-LockingProcesses([string]$PathToCheck)
{
    # Используем Windows Restart Manager API чтобы выяснить, кто держит лок на пути.
    # Это не требует внешних утилит (handle.exe) и хорошо работает на Win10+.
    try
    {
        if (-not ("BuildAutomation.RestartManager" -as [type]))
        {
            Add-Type -TypeDefinition @"
using System;
using System.Runtime.InteropServices;

namespace BuildAutomation
{
    public static class RestartManager
    {
        private const int RmRebootReasonNone = 0;
        private const int CchRmMaxAppName = 255;
        private const int CchRmMaxSvcName = 63;
        private const int ErrorMoreData = 234;

        [StructLayout(LayoutKind.Sequential)]
        private struct RM_UNIQUE_PROCESS
        {
            public int dwProcessId;
            public System.Runtime.InteropServices.ComTypes.FILETIME ProcessStartTime;
        }

        private enum RM_APP_TYPE
        {
            RmUnknownApp = 0,
            RmMainWindow = 1,
            RmOtherWindow = 2,
            RmService = 3,
            RmExplorer = 4,
            RmConsole = 5,
            RmCritical = 1000
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        private struct RM_PROCESS_INFO
        {
            public RM_UNIQUE_PROCESS Process;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = CchRmMaxAppName + 1)]
            public string strAppName;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = CchRmMaxSvcName + 1)]
            public string strServiceShortName;
            public RM_APP_TYPE ApplicationType;
            public uint AppStatus;
            public uint TSSessionId;
            [MarshalAs(UnmanagedType.Bool)]
            public bool bRestartable;
        }

        [DllImport("rstrtmgr.dll", CharSet = CharSet.Unicode)]
        private static extern int RmStartSession(out uint pSessionHandle, int dwSessionFlags, string strSessionKey);

        [DllImport("rstrtmgr.dll", CharSet = CharSet.Unicode)]
        private static extern int RmRegisterResources(uint pSessionHandle, uint nFiles, string[] rgsFilenames, uint nApplications, IntPtr rgApplications, uint nServices, string[] rgsServiceNames);

        [DllImport("rstrtmgr.dll")]
        private static extern int RmGetList(uint dwSessionHandle, out uint pnProcInfoNeeded, ref uint pnProcInfo, [In, Out] RM_PROCESS_INFO[] rgAffectedApps, ref uint lpdwRebootReasons);

        [DllImport("rstrtmgr.dll")]
        private static extern int RmEndSession(uint pSessionHandle);

        public static int[] GetLockingProcessIds(string path)
        {
            uint sessionHandle;
            string sessionKey = Guid.NewGuid().ToString();
            int result = RmStartSession(out sessionHandle, 0, sessionKey);
            if (result != 0)
            {
                return Array.Empty<int>();
            }

            try
            {
                string[] resources = new[] { path };
                result = RmRegisterResources(sessionHandle, (uint)resources.Length, resources, 0, IntPtr.Zero, 0, null);
                if (result != 0)
                {
                    return Array.Empty<int>();
                }

                uint procInfoNeeded = 0;
                uint procInfo = 0;
                uint rebootReasons = RmRebootReasonNone;
                result = RmGetList(sessionHandle, out procInfoNeeded, ref procInfo, null, ref rebootReasons);
                if (result == ErrorMoreData)
                {
                    procInfo = procInfoNeeded;
                    RM_PROCESS_INFO[] processInfo = new RM_PROCESS_INFO[procInfo];
                    result = RmGetList(sessionHandle, out procInfoNeeded, ref procInfo, processInfo, ref rebootReasons);
                    if (result == 0)
                    {
                        int[] pids = new int[procInfo];
                        for (int i = 0; i < procInfo; i++)
                        {
                            pids[i] = processInfo[i].Process.dwProcessId;
                        }
                        return pids;
                    }
                }

                return Array.Empty<int>();
            }
            finally
            {
                RmEndSession(sessionHandle);
            }
        }
    }
}
"@ -ErrorAction Stop | Out-Null
        }

        $pids = [BuildAutomation.RestartManager]::GetLockingProcessIds($PathToCheck)
        return $pids
    }
    catch
    {
        return @()
    }
}

function Write-LockingProcessesDiagnostics([string]$ProjectRoot, [string]$PathToCheck)
{
    try
    {
        $pids = Get-LockingProcesses -PathToCheck $PathToCheck
        if ($null -eq $pids -or $pids.Count -le 0)
        {
            Write-Log "Lock diagnostics: no locking processes reported by Restart Manager for: $PathToCheck"
            return
        }

        Write-Log "Lock diagnostics: $($pids.Count) process(es) are locking: $PathToCheck"
        foreach ($pid in $pids)
        {
            try
            {
                $process = Get-Process -Id $pid -ErrorAction SilentlyContinue
                $name = if ($null -ne $process) { $process.ProcessName } else { "unknown" }

                $cim = Get-CimInstance Win32_Process -Filter ("ProcessId=" + $pid) -ErrorAction SilentlyContinue
                $exe = if ($null -ne $cim) { [string]$cim.ExecutablePath } else { "" }
                $cmd = if ($null -ne $cim) { [string]$cim.CommandLine } else { "" }

                Write-Log "  - PID=$pid Name=$name"
                if (-not [string]::IsNullOrWhiteSpace($exe)) { Write-Log "    Exe: $exe" }
                if (-not [string]::IsNullOrWhiteSpace($cmd)) { Write-Log "    Cmd: $cmd" }
            }
            catch
            {
                Write-Log "  - PID=$pid (failed to query details)"
            }
        }

        $names = @()
        foreach ($pid in $pids)
        {
            $process = Get-Process -Id $pid -ErrorAction SilentlyContinue
            if ($null -ne $process)
            {
                $names += $process.ProcessName
            }
        }

        if ($names -contains "MsMpEng" -or $names -contains "SearchIndexer")
        {
            Write-Log "TIP: It looks like Windows Defender or Search Indexer may be locking Library. Add an exclusion for the project folder to avoid Access Denied locks."
        }

        # Доп. эвристика: некоторые локи не видны Restart Manager'у. Делаем best-effort handle scan (может таймаутиться).
        try
        {
            if (Test-Path $PathToCheck)
            {
                $scanResult = Get-LockingPidsByHandleScan -PathPrefix $PathToCheck
                if ($null -eq $scanResult)
                {
                    Write-Log "Lock diagnostics (handle scan): failed (no result) for: $PathToCheck"
                }
                else
                {
                    $scanPids = $scanResult.Pids
                    if ([bool]$scanResult.TimedOut)
                    {
                        Write-Log "Lock diagnostics (handle scan): timed out (results may be incomplete) for: $PathToCheck"
                    }
                    if ([bool]$scanResult.HadError -and -not [string]::IsNullOrWhiteSpace([string]$scanResult.Error))
                    {
                        Write-Log "Lock diagnostics (handle scan): error: $([string]$scanResult.Error)"
                    }

                    if ($null -ne $scanPids -and $scanPids.Count -gt 0)
                    {
                        Write-Log "Lock diagnostics (handle scan): $($scanPids.Count) PID(s) hold handles under: $PathToCheck"
                        foreach ($pid in $scanPids)
                        {
                            $process = Get-Process -Id $pid -ErrorAction SilentlyContinue
                            $name = if ($null -ne $process) { $process.ProcessName } else { "unknown" }
                            Write-Log "  - PID=$pid Name=$name"
                        }
                    }
                }
            }
        }
        catch
        {
        }
    }
    catch
    {
        # Best-effort.
    }
}

function Switch-Library([string]$ProjectRoot, [string]$TargetProfile)
{
    $libraryPath = Join-Path $ProjectRoot "Library"
    $targetLibraryPath = Join-Path $ProjectRoot ("Library_" + $TargetProfile)
    $libraryProfileFileName = "BuildAutomationProfile.txt"

    $currentProfile = Get-CurrentProfile $ProjectRoot

    function Invoke-WithRetries([scriptblock]$Action, [string]$ActionName, [int]$MaxRetries = 7, [int]$DelaySeconds = 1)
    {
        for ($attempt = 1; $attempt -le $MaxRetries; $attempt++)
        {
            try
            {
                & $Action
                return
            }
            catch
            {
                $message = [string]$_.Exception.Message
                Write-Log "$ActionName failed ($attempt/$MaxRetries): $message"
                try
                {
                    $fqid = [string]$_.FullyQualifiedErrorId
                    $category = if ($null -ne $_.CategoryInfo) { [string]$_.CategoryInfo.Category } else { "" }
                    $exceptionType = if ($null -ne $_.Exception) { [string]$_.Exception.GetType().FullName } else { "" }
                    if (-not [string]::IsNullOrWhiteSpace($fqid) -or -not [string]::IsNullOrWhiteSpace($category) -or -not [string]::IsNullOrWhiteSpace($exceptionType))
                    {
                        Write-Log "Error details: ExceptionType=$exceptionType Category=$category FullyQualifiedErrorId=$fqid"
                    }
                }
                catch
                {
                }

                # На каждой попытке дополнительно пытаемся прибить фоновые процессы Unity, чтобы не требовать ребута.
                Stop-UnityRelatedProcesses -ProjectRoot $ProjectRoot

                if ($AggressiveUnlock -and ($attempt -eq 3))
                {
                    Invoke-AggressiveUnlock -ProjectRoot $ProjectRoot
                }

                if ($attempt -eq 1 -or ($attempt % 5 -eq 0))
                {
                    Write-Log "Lock diagnostics: running (best effort)..."
                    $libraryDirectoryPath = Join-Path $ProjectRoot "Library"
                    Write-LockingProcessesDiagnostics -ProjectRoot $ProjectRoot -PathToCheck $libraryDirectoryPath

                    # Иногда Restart Manager не возвращает PID'ы для директории. Пробуем пару типичных подпутей.
                    $probePaths = @(
                        (Join-Path $libraryDirectoryPath "Bee"),
                        (Join-Path $libraryDirectoryPath "Artifacts"),
                        (Join-Path $libraryDirectoryPath "PackageCache"),
                        (Join-Path $libraryDirectoryPath "ScriptAssemblies")
                    )
                    foreach ($probePath in $probePaths)
                    {
                        if (Test-Path $probePath)
                        {
                            Write-LockingProcessesDiagnostics -ProjectRoot $ProjectRoot -PathToCheck $probePath
                        }
                    }
                }
                Start-Sleep -Seconds $DelaySeconds
            }
        }

        throw "$ActionName failed after $MaxRetries attempts."
    }

    function Is-Junction([string]$PathToCheck)
    {
        try
        {
            if (-not (Test-Path $PathToCheck))
            {
                return $false
            }

            $item = Get-Item -LiteralPath $PathToCheck -Force -ErrorAction Stop
            # Junctions are reparse points.
            return ($item.Attributes -band [System.IO.FileAttributes]::ReparsePoint) -ne 0
        }
        catch
        {
            return $false
        }
    }

    function Set-LibraryJunction([string]$ProjectRootPath, [string]$TargetLibraryFolderPath)
    {
        $libraryJunctionPath = Join-Path $ProjectRootPath "Library"
        if (Test-Path $libraryJunctionPath)
        {
            if (-not (Is-Junction -PathToCheck $libraryJunctionPath))
            {
                throw "Cannot set junction: 'Library' exists and is not a junction."
            }

            Write-Log "Removing Library junction..."
            & cmd.exe /c "rmdir `"$libraryJunctionPath`"" | Out-Null
        }

        Write-Log "Creating Library junction -> $TargetLibraryFolderPath"
        & cmd.exe /c "mklink /J `"$libraryJunctionPath`" `"$TargetLibraryFolderPath`"" | Out-Null
    }

    function Remove-DirectorySafe([string]$PathToRemove)
    {
        if (-not (Test-Path $PathToRemove))
        {
            return
        }

        Invoke-WithRetries -Action {
            Remove-Item -Path $PathToRemove -Recurse -Force
        } -ActionName "Remove directory '$PathToRemove'"
    }

    function Move-DirectorySafe([string]$SourcePath, [string]$DestinationPath)
    {
        if (-not (Test-Path $SourcePath))
        {
            return
        }

        if (Test-Path $DestinationPath)
        {
            Remove-DirectorySafe -PathToRemove $DestinationPath
        }

        Invoke-WithRetries -Action {
            Move-Item -Path $SourcePath -Destination $DestinationPath
        } -ActionName "Move directory '$SourcePath' -> '$DestinationPath'"
    }

    function Get-LibraryProfileFromFile([string]$LibraryDirectoryPath)
    {
        $filePath = Join-Path $LibraryDirectoryPath $libraryProfileFileName
        if (-not (Test-Path $filePath))
        {
            return ""
        }

        try
        {
            $value = (Get-Content $filePath -Raw).Trim()
            if ($value -eq "Dev" -or $value -eq "Release")
            {
                return $value
            }
        }
        catch
        {
        }

        return ""
    }

    function Set-LibraryProfileToFile([string]$LibraryDirectoryPath, [string]$ProfileValue)
    {
        if (-not (Test-Path $LibraryDirectoryPath))
        {
            return
        }

        $filePath = Join-Path $LibraryDirectoryPath $libraryProfileFileName
        Invoke-WithRetries -Action {
            Set-Content -Path $filePath -Value $ProfileValue -Encoding UTF8
        } -ActionName "Write library profile file '$filePath'"
    }

    function Ensure-JunctionMode([string]$ProjectRootPath, [string]$AssumedActiveProfile)
    {
        $libraryDirectoryPath = Join-Path $ProjectRootPath "Library"
        if (Is-Junction -PathToCheck $libraryDirectoryPath)
        {
            return
        }

        if (-not (Test-Path $libraryDirectoryPath))
        {
            # Если Library отсутствует (редко), просто создаём профайл-папки и junction.
            New-Item -ItemType Directory -Path (Join-Path $ProjectRootPath ("Library_" + $AssumedActiveProfile)) | Out-Null
            Set-LibraryJunction -ProjectRootPath $ProjectRootPath -TargetLibraryFolderPath (Join-Path $ProjectRootPath ("Library_" + $AssumedActiveProfile))
            return
        }

        $activeProfileFolderPath = Join-Path $ProjectRootPath ("Library_" + $AssumedActiveProfile)
        if (Test-Path $activeProfileFolderPath)
        {
            $backupPath = $activeProfileFolderPath + "_Backup_" + (Get-Date).ToString("yyyyMMdd_HHmmss")
            Write-Log "Junction mode: '$activeProfileFolderPath' already exists. Moving it to backup: $backupPath"
            Move-DirectorySafe -SourcePath $activeProfileFolderPath -DestinationPath $backupPath
        }

        Write-Log "Junction mode: converting. Moving '$libraryDirectoryPath' -> '$activeProfileFolderPath'..."
        Move-DirectorySafe -SourcePath $libraryDirectoryPath -DestinationPath $activeProfileFolderPath

        $otherProfile = if ($AssumedActiveProfile -eq "Dev") { "Release" } else { "Dev" }
        $otherProfileFolderPath = Join-Path $ProjectRootPath ("Library_" + $otherProfile)
        if (-not (Test-Path $otherProfileFolderPath))
        {
            New-Item -ItemType Directory -Path $otherProfileFolderPath | Out-Null
        }

        Set-LibraryJunction -ProjectRootPath $ProjectRootPath -TargetLibraryFolderPath $activeProfileFolderPath
        Write-Log "Junction mode: enabled. Library is now a junction."
    }

    if ([string]::IsNullOrWhiteSpace($currentProfile) -and (Test-Path $libraryPath))
    {
        $inferredProfile = Get-LibraryProfileFromFile -LibraryDirectoryPath $libraryPath
        if (-not [string]::IsNullOrWhiteSpace($inferredProfile))
        {
            $currentProfile = $inferredProfile
        }
    }

    if ([string]::IsNullOrWhiteSpace($currentProfile) -and (Test-Path $libraryPath) -and (-not (Test-Path $targetLibraryPath)))
    {
        Set-CurrentProfile $ProjectRoot $TargetProfile
        Set-LibraryProfileToFile -LibraryDirectoryPath $libraryPath -ProfileValue $TargetProfile
        return
    }

    # Junction-only стратегия:
    # - если Library ещё не junction: один раз конвертируем (переносим текущую Library в Library_<activeProfile> и делаем junction)
    # - дальше свитч = смена junction, без переносов/копирования
    $assumedActiveProfile = if (-not [string]::IsNullOrWhiteSpace($currentProfile)) { $currentProfile } else { $TargetProfile }
    Ensure-JunctionMode -ProjectRootPath $ProjectRoot -AssumedActiveProfile $assumedActiveProfile

    if (-not (Test-Path $targetLibraryPath))
    {
        New-Item -ItemType Directory -Path $targetLibraryPath | Out-Null
    }

    Set-LibraryJunction -ProjectRootPath $ProjectRoot -TargetLibraryFolderPath $targetLibraryPath

    Set-CurrentProfile $ProjectRoot $TargetProfile
    Set-LibraryProfileToFile -LibraryDirectoryPath $libraryPath -ProfileValue $TargetProfile
}

try
{
    # Очищаем лог на каждый запуск, чтобы не превращался в простыню.
    try
    {
        $directoryPath = Split-Path -Parent $logFilePath
        if (-not (Test-Path $directoryPath))
        {
            New-Item -ItemType Directory -Path $directoryPath | Out-Null
        }
        Set-Content -Path $logFilePath -Value "" -Encoding UTF8
    }
    catch
    {
        # ignore
    }

    Write-Log "===== BuildAutomation switch started ====="
    Write-Log "Log: $logFilePath"

    $projectRootFull = (Resolve-Path $ProjectPath).Path
    $unityEditorFull = (Resolve-Path $UnityPath).Path
    $outputFull = [System.IO.Path]::GetFullPath($OutputPath)

    Write-Log "Project: $projectRootFull"
    Write-Log "Profile: $Profile"
    Write-Log "Artifact: $Artifact"
    Write-Log "Output: $outputFull"

    Wait-UnityToExit -UnityEditorProcessId $EditorPid
    Write-Log "Stopping Unity-related background processes (best effort)..."
    Stop-UnityRelatedProcesses -ProjectRoot $projectRootFull
    Write-Log "Waiting a bit for file locks to release..."
    Start-Sleep -Seconds 5
    Switch-Library -ProjectRoot $projectRootFull -TargetProfile $Profile
    Start-UnityGuiBuild -ProjectRoot $projectRootFull -UnityEditorPath $unityEditorFull -BuildProfile $Profile -BuildArtifact $Artifact -BuildOutputPath $outputFull

    Write-Log "===== BuildAutomation switch finished (Unity start requested) ====="
}
catch
{
    Write-Host ""
    Write-Host "BuildAutomation switch FAILED: $($_.Exception.Message)" -ForegroundColor Red
    Write-Log "FAILED: $($_.Exception.ToString())"
    Write-Log "TIP: the full log is at: $logFilePath"

    try
    {
        Start-Process -FilePath "notepad.exe" -ArgumentList "`"$logFilePath`"" | Out-Null
    }
    catch
    {
        # ignore
    }

    throw
}


