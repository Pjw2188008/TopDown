param(
    [Parameter(Mandatory = $true)][string]$UnityEditorPath
)
$ErrorActionPreference = 'Stop'
$editorDirectory = Split-Path -Parent (Resolve-Path -LiteralPath $UnityEditorPath).Path
$runtime = Join-Path $editorDirectory 'Data\NetCoreRuntime\dotnet.exe'
$compiler = Join-Path $editorDirectory 'Data\DotNetSdkRoslyn\csc.dll'
$mono = Join-Path $editorDirectory 'Data\MonoBleedingEdge\bin\mono.exe'
$coreLibrary = Join-Path $editorDirectory 'Data\MonoBleedingEdge\lib\mono\4.5\mscorlib.dll'
$projectDirectory = (Resolve-Path -LiteralPath (Join-Path $PSScriptRoot '..\..')).Path
$scriptDirectory = Join-Path $projectDirectory 'Assets\Scripts'
# Staged refactor verification can supply the same Scripts tree directly.
if (!(Test-Path -LiteralPath $scriptDirectory)) { $scriptDirectory = Join-Path $projectDirectory 'Scripts' }
$testOutput = Join-Path ([IO.Path]::GetTempPath()) ('StoryScriptTests-' + [guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path $testOutput | Out-Null
$testExecutable = Join-Path $testOutput 'RegressionTests.exe'
$compilerArguments = @('/nologo', '/target:exe', '/nostdlib', ('/r:' + $coreLibrary), ('/out:' + $testExecutable), (Join-Path $PSScriptRoot 'RegressionTests.cs'))
foreach ($relativePath in @(
    'Attachable\GiantErrorEffect.cs', 'Attachable\AccelerationErrorEffect.cs', 'Attachable\ReflectionErrorEffect.cs',
    'Internal\ErrorDefinitions.cs', 'Internal\ErrorInventory.cs', 'Internal\ErrorEffectState.cs',
    'Internal\IErrorSource.cs', 'Internal\AttackMath.cs', 'Internal\EnvironmentPasteSession.cs'
)) {
    $compilerArguments += Join-Path $scriptDirectory $relativePath
}
& $runtime $compiler @compilerArguments
if ($LASTEXITCODE -ne 0) { throw 'Test compilation failed.' }
& $mono $testExecutable
if ($LASTEXITCODE -ne 0) { throw 'Regression tests failed.' }
Write-Output ('Test executable: ' + $testExecutable)
