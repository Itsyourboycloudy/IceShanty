param([string]$UnityEditor = 'C:/Program Files/Unity/Hub/Editor/6000.0.62f1/Editor')
$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path $PSScriptRoot -Parent
Push-Location $projectRoot
try {
    # Verify the actual launch scene, not only the standalone controller code.
    $sceneText = Get-Content Assets/Scenes/Game.unity -Raw
    $scriptGuid = [regex]::Match((Get-Content Assets/IceShanty/Horror/BlackwaterRoom.cs.meta -Raw),'guid: ([a-f0-9]+)').Groups[1].Value
    if (!$scriptGuid -or $sceneText -notmatch $scriptGuid) { throw 'Game scene is not wired to the circular room controller.' }
    $buildText = Get-Content ProjectSettings/EditorBuildSettings.asset -Raw
    if ([regex]::Match($buildText,'path: (.+)').Groups[1].Value.Trim() -ne 'Assets/Scenes/Game.unity') { throw 'Game must be the first build scene.' }
    $sceneRoots = [regex]::Match($sceneText,'(?s)--- !u!1660057539.*$').Value
    $activeRoots = 0
    foreach ($match in [regex]::Matches($sceneRoots,'fileID: (\d+)')) {
        $transformId = $match.Groups[1].Value
        $transformBlock = [regex]::Match($sceneText,'(?ms)^--- !u!(?:4|224) &' + $transformId + '\r?\n.*?(?=^---|\z)').Value
        $objectId = [regex]::Match($transformBlock,'m_GameObject: \{fileID: (\d+)\}').Groups[1].Value
        $objectBlock = [regex]::Match($sceneText,'(?ms)^--- !u!1 &' + $objectId + '\r?\n.*?(?=^---|\z)').Value
        if ($objectBlock -match 'm_IsActive: 1') { $activeRoots++ }
    }
    if ($activeRoots -ne 1) { throw 'Expected exactly one active gameplay root; legacy game systems must stay inactive.' }
    $runtimeRsp = Get-ChildItem Library/Bee -Filter Assembly-CSharp.rsp -Recurse | Select-Object -First 1
    $editorRsp = Get-ChildItem Library/Bee -Filter Assembly-CSharp-Editor.rsp -Recurse | Select-Object -First 1
    if (!$runtimeRsp -or !$editorRsp) { throw 'Import this project in Unity first to generate its compiler references.' }
    New-Item -ItemType Directory -Force Artifacts | Out-Null
    $dotnetPath = Join-Path $UnityEditor 'Data/NetCoreRuntime/dotnet.exe'
    $compilerPath = Join-Path $UnityEditor 'Data/DotNetSdkRoslyn/csc.dll'
    $runtimeArgs = @(Get-Content $runtimeRsp.FullName | Where-Object { $_ -notmatch '^(-out:|-refout:|/analyzer:|-analyzer:|/additionalfile:)' })
    foreach ($source in @(Get-ChildItem Assets/IceShanty/Horror -Filter '*.cs' | ForEach-Object { 'Assets/IceShanty/Horror/' + $_.Name })) {
        $quoted = '"' + $source + '"'
        if ($runtimeArgs -notcontains $quoted) { $runtimeArgs += $quoted }
    }
    $runtimeArgs += '-out:"Artifacts/Blackwater-Compile.dll"'
    Set-Content Artifacts/Blackwater-Compile.rsp $runtimeArgs
    & $dotnetPath $compilerPath '@Artifacts/Blackwater-Compile.rsp'
    if ($LASTEXITCODE) { throw 'Gameplay compilation failed.' }
    $editorArgs = @(Get-Content $editorRsp.FullName | Where-Object { $_ -notmatch '^(-out:|-refout:|/analyzer:|-analyzer:|/additionalfile:)' -and $_ -notmatch '^-r:.*Assembly-CSharp.ref.dll' })
    $editorArgs += '-r:"Artifacts/Blackwater-Compile.dll"','-out:"Artifacts/Blackwater-Editor-Compile.dll"'
    $builderSource = '"Assets/IceShanty/Horror/Editor/BlackwaterBuilder.cs"'
    if ($editorArgs -notcontains $builderSource) { $editorArgs += $builderSource }
    Set-Content Artifacts/Blackwater-Editor-Compile.rsp $editorArgs
    & $dotnetPath $compilerPath '@Artifacts/Blackwater-Editor-Compile.rsp'
    if ($LASTEXITCODE) { throw 'Editor compilation failed.' }
    # Execute the actual run model and the editor's exact assertions against Unity's managed math.
    $builderText = Get-Content Assets/IceShanty/Horror/Editor/BlackwaterBuilder.cs -Raw
    $checks = $builderText.Substring($builderText.IndexOf('        static int checks;'))
    $checks = $checks.Replace('[MenuItem("Ice Shanty/Game/Validate Circular Room Rules")]','').Replace('Debug.Log(', 'Console.WriteLine(')
    Set-Content Artifacts/RuleHarness.cs ("using System; using IceShanty.Horror; namespace Validation { public static class Harness { public static void Main() { ValidateRules(); }`n" + $checks)
    $ruleArgs = @($runtimeArgs | Where-Object { $_ -match '^-r:' -or $_ -match '^-nostdlib' })
    $ruleArgs += '-target:exe','-out:"Artifacts/Blackwater-Rules.dll"','"Assets/IceShanty/Horror/HorrorRun.cs"','"Artifacts/RuleHarness.cs"'
    Set-Content Artifacts/Blackwater-Rules.rsp $ruleArgs
    & $dotnetPath $compilerPath '@Artifacts/Blackwater-Rules.rsp'
    if ($LASTEXITCODE) { throw 'Rule harness compilation failed.' }
    Copy-Item (Join-Path $UnityEditor 'Data/Managed/UnityEngine/UnityEngine.CoreModule.dll') Artifacts/UnityEngine.CoreModule.dll -Force
    Set-Content Artifacts/Blackwater-Rules.runtimeconfig.json '{"runtimeOptions":{"tfm":"net6.0","framework":{"name":"Microsoft.NETCore.App","version":"6.0.21"}}}'
    & $dotnetPath Artifacts/Blackwater-Rules.dll
    if ($LASTEXITCODE) { throw 'Rule assertions failed.' }
    'Game scene wiring and build order: PASS. Gameplay compilation: PASS. Editor compilation: PASS. Circular room and item rules: PASS. Rendering: not tested by this script.' | Set-Content Artifacts/validation.txt
} finally { Pop-Location }
