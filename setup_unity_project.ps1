# setup_unity_project.ps1
# Runs after Unity 2022 is installed. Initialises the project and generates all assets.

$unity   = "C:\Program Files\Unity 2022.3.62f3\Editor\Unity.exe"
$project = "C:\Users\Milo\Cross Word Rougelike"
$logDir  = "$project\Logs"

if (-not (Test-Path $logDir)) { New-Item -ItemType Directory -Path $logDir | Out-Null }

function Run-Unity {
    param([string]$logName, [string[]]$args)
    $log = "$logDir\$logName.log"
    Write-Host "`n=== $logName ===" -ForegroundColor Cyan
    & $unity @args -logFile $log
    $code = $LASTEXITCODE
    if ($code -ne 0) {
        Write-Host "FAILED (exit $code). Last 30 lines of log:" -ForegroundColor Red
        Get-Content $log -Tail 30 | Write-Host
        exit $code
    }
    Write-Host "OK (exit $code)" -ForegroundColor Green
    # Show any [SceneBuilder] or [Create*] lines from the log
    Get-Content $log | Select-String "\[Create|SceneBuilder|ERROR|error CS" | Write-Host
}

# ── 1. Initialise project structure ──────────────────────────────────────────
Write-Host "Step 1: Creating project structure..." -ForegroundColor Yellow
Run-Unity "01_create_project" @(
    "-batchmode", "-nographics",
    "-createProject", $project,
    "-quit"
)

# ── 2. Letter data assets ─────────────────────────────────────────────────────
Write-Host "Step 2: Generating Letter assets..." -ForegroundColor Yellow
Run-Unity "02_letters" @(
    "-batchmode", "-nographics",
    "-projectPath", $project,
    "-executeMethod", "CreateLetterDataAssets.CreateAll",
    "-quit"
)

# ── 3. Lexicon data assets ────────────────────────────────────────────────────
Write-Host "Step 3: Generating Lexicon assets..." -ForegroundColor Yellow
Run-Unity "03_lexicon" @(
    "-batchmode", "-nographics",
    "-projectPath", $project,
    "-executeMethod", "CreateLetterDataAssets.CreateLexiconAssets",
    "-quit"
)

# ── 4. Blind data assets ──────────────────────────────────────────────────────
Write-Host "Step 4: Generating Blind assets..." -ForegroundColor Yellow
Run-Unity "04_blinds" @(
    "-batchmode", "-nographics",
    "-projectPath", $project,
    "-executeMethod", "CreateLetterDataAssets.CreateBlindAssetsAnte1",
    "-quit"
)

# ── 5. Build scene and prefabs ────────────────────────────────────────────────
Write-Host "Step 5: Building scene and prefabs..." -ForegroundColor Yellow
Run-Unity "05_scene" @(
    "-batchmode", "-nographics",
    "-projectPath", $project,
    "-executeMethod", "SceneBuilder.BuildAll",
    "-quit"
)

Write-Host "`n=== All done! Open Unity Hub, add the project, and press Play. ===" -ForegroundColor Green
