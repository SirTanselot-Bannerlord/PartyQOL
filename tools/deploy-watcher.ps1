# Copies the Release build of PartyQOL into the game module folder whenever Bannerlord
# is not running and a built artifact differs from the deployed one. Mirrors DeployToBannerlord
# in PartyQOL.csproj (DLL, PDB, SubModule.xml, GUI, UIExtenderEx Party* prefab cache clear).
# -Once: deploy now if possible, otherwise wait until the game closes; exit as soon as deployed.
# Only one copy runs at a time (named mutex); a second start prints SKIP and exits.
param([int]$IntervalSeconds = 10, [switch]$Once)

$owned = $false
$mutex = New-Object System.Threading.Mutex($false, 'Local\PartyQOL.DeployWatcher')
try {
    if (-not $mutex.WaitOne(0)) {
        Write-Output "$(Get-Date -Format HH:mm:ss) SKIP deploy-watcher already running"
        exit 0
    }
    $owned = $true

    $root = Split-Path -Parent $PSScriptRoot
    $modules = 'D:\SteamLibrary\steamapps\common\Mount & Blade II Bannerlord\Modules'
    $targets = @(
        @{
            Name     = 'PartyQOL'
            Src      = Join-Path $root 'bin\Release'
            Xml      = Join-Path $root 'SubModule.xml'
            Module   = Join-Path $modules 'PartyQOL'
            GuiSrc   = Join-Path $root 'GUI'
            ClearUiExtenderPartyCache = $true
        }
    )

    function GameRunning {
        @(Get-CimInstance Win32_Process | Where-Object { $_.ExecutablePath -like '*Bannerlord*' -or $_.Name -like '*Bannerlord*' }).Count -gt 0
    }

    function HashOf($path) {
        if (Test-Path $path) { (Get-FileHash $path).Hash } else { '' }
    }

    function Dll($t) { Join-Path $t.Src "$($t.Name).dll" }
    function DeployedDll($t) { Join-Path $t.Module "bin\Win64_Shipping_Client\$($t.Name).dll" }

    function GuiStale($t) {
        if (-not $t.GuiSrc -or -not (Test-Path $t.GuiSrc)) { return $false }
        $guiFiles = Get-ChildItem $t.GuiSrc -Recurse -File -ErrorAction SilentlyContinue
        foreach ($f in $guiFiles) {
            $rel = $f.FullName.Substring($t.GuiSrc.Length).TrimStart('\', '/')
            $dst = Join-Path (Join-Path $t.Module 'GUI') $rel
            if ((HashOf $f.FullName) -ne (HashOf $dst)) { return $true }
        }
        return $false
    }

    function Short($t) { $h = HashOf (Dll $t); if ($h) { $h.Substring(0, 8) } else { '--------' } }

    function ClearPartyPrefabCache {
        $cache = Join-Path $modules 'Bannerlord.UIExtenderEx\CompiledPrefabs'
        if (-not (Test-Path $cache)) { return }
        Get-ChildItem $cache -Recurse -File -ErrorAction SilentlyContinue |
            Where-Object { $_.Name -like '*PartyScreen*' -or $_.Name -like '*PartyTroopManagerPopUp*' -or $_.Name -like '*PartyTroopUpgradeItem*' } |
            Remove-Item -Force -ErrorAction SilentlyContinue
    }

    function DeployGui($t) {
        if (-not $t.GuiSrc -or -not (Test-Path $t.GuiSrc)) { return }
        $guiDst = Join-Path $t.Module 'GUI'
        if (Test-Path $guiDst) { Remove-Item $guiDst -Recurse -Force }
        Copy-Item $t.GuiSrc $guiDst -Recurse -Force
    }

    if (-not $Once) { Write-Output "WATCHING $(($targets | ForEach-Object { $_.Name }) -join ', ') every $IntervalSeconds s" }
    $waitingLogged = $false
    while ($true) {
        $stale = @($targets | Where-Object {
            $b = HashOf (Dll $_)
            ($b -ne '' -and $b -ne (HashOf (DeployedDll $_))) -or (GuiStale $_)
        })
        if ($Once -and $stale.Count -eq 0) {
            Write-Output "$(Get-Date -Format HH:mm:ss) UP TO DATE $(($targets | ForEach-Object { "$($_.Name)=$(Short $_)" }) -join ' ')"
            break
        }
        # Leave a few seconds after the last write so a build in progress is not copied half-done.
        $settled = @($targets | Where-Object { $d = Dll $_; (Test-Path $d) -and ((Get-Date) - (Get-Item $d).LastWriteTime).TotalSeconds -lt 5 }).Count -eq 0
        if ($stale.Count -gt 0 -and $settled) {
            $label = ($targets | ForEach-Object { "$($_.Name)=$(Short $_)" }) -join ' '
            if (GameRunning) {
                if (-not $waitingLogged) { Write-Output "$(Get-Date -Format HH:mm:ss) PENDING $label (game running)"; $waitingLogged = $true }
            }
            else {
                try {
                    $ok = $true
                    foreach ($t in $targets) {
                        if (-not (Test-Path (Dll $t))) { continue }
                        $dst = Join-Path $t.Module 'bin\Win64_Shipping_Client'
                        New-Item -ItemType Directory -Force $dst | Out-Null
                        Copy-Item (Dll $t) $dst -Force
                        $pdb = Join-Path $t.Src "$($t.Name).pdb"
                        if (Test-Path $pdb) { Copy-Item $pdb $dst -Force }
                        Copy-Item $t.Xml $t.Module -Force
                        DeployGui $t
                        if ($t.ClearUiExtenderPartyCache) { ClearPartyPrefabCache }
                        if ((HashOf (DeployedDll $t)) -ne (HashOf (Dll $t))) { $ok = $false }
                    }
                    Write-Output "$(Get-Date -Format HH:mm:ss) $(if ($ok) { 'DEPLOYED' } else { 'MISMATCH' }) $label"
                    if ($Once -and $ok) { break }
                }
                catch {
                    Write-Output "$(Get-Date -Format HH:mm:ss) FAILED $($_.Exception.Message)"
                }
                $waitingLogged = $false
            }
        }
        Start-Sleep -Seconds $IntervalSeconds
    }
}
finally {
    if ($owned) { $mutex.ReleaseMutex() }
    $mutex.Dispose()
}
