param(
    [string] $PackageOutputPath = (Join-Path $PSScriptRoot "../artifacts/package")
)

$ErrorActionPreference = "Stop"
$PSNativeCommandUseErrorActionPreference = $false
$packages = @(Get-ChildItem -LiteralPath $PackageOutputPath -Filter "CommentSense.*.nupkg")
if ($packages.Count -ne 1) {
    throw "Expected exactly one CommentSense package in '$PackageOutputPath'; pack into a clean directory first."
}

$archive = [IO.Compression.ZipFile]::OpenRead($packages[0].FullName)
try {
    foreach ($entry in @(
        "analyzers/dotnet/cs/CommentSense.Analyzers.dll",
        "analyzers/dotnet/cs/CommentSense.Core.dll",
        "analyzers/dotnet/cs/codefixes/CommentSense.CodeFixes.dll",
        "build/CommentSense.targets"
    )) {
        if ($null -eq $archive.GetEntry($entry)) { throw "Package is missing '$entry'." }
    }
    $reader = [IO.StreamReader]::new($archive.GetEntry("CommentSense.nuspec").Open())
    try { $version = ([xml] $reader.ReadToEnd()).package.metadata.version }
    finally { $reader.Dispose() }
}
finally { $archive.Dispose() }

# Avoid inheriting the repository's MSBuild configuration.
$checkRoot = Join-Path ([IO.Path]::GetTempPath()) "commentsense-package-check-$([Guid]::NewGuid().ToString('N'))"
New-Item -ItemType Directory -Path $checkRoot | Out-Null
try {
    New-Item -ItemType Directory -Path (Join-Path $checkRoot "feed") | Out-Null
    Copy-Item -LiteralPath $packages[0].FullName -Destination (Join-Path $checkRoot "feed")
    Copy-Item -LiteralPath (Join-Path $PSScriptRoot "../global.json") -Destination $checkRoot
    Set-Content (Join-Path $checkRoot "NuGet.Config") @'
<configuration>
  <packageSources><clear /><add key="local" value="feed" /></packageSources>
</configuration>
'@
    Set-Content (Join-Path $checkRoot "Consumer.csproj") @"
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <GenerateDocumentationFile>true</GenerateDocumentationFile>
    <TreatWarningsAsErrors>true</TreatWarningsAsErrors>
    <NoWarn>CS1591</NoWarn>
  </PropertyGroup>
  <ItemGroup><PackageReference Include="CommentSense" Version="$version" /></ItemGroup>
</Project>
"@
    Push-Location $checkRoot
    try {
        dotnet restore Consumer.csproj --configfile NuGet.Config --packages .packages
        if ($LASTEXITCODE -ne 0) { throw "Consumer restore failed." }

        Set-Content Consumer.cs 'public class Undocumented { }'
        $output = (& dotnet build Consumer.csproj --no-restore --configuration Release --nologo 2>&1 | Out-String)
        $buildExitCode = $LASTEXITCODE
        if ($buildExitCode -eq 0 -or $output -notmatch 'error CSENSE001\b') {
            Write-Host $output
            throw "The packaged analyzer did not reject the undocumented API with CSENSE001."
        }
        if ($output -match '\b(?:warning|error) (?!CSENSE001\b)[A-Z]+\d+\b') {
            Write-Host $output
            throw "The undocumented consumer produced an unexpected compiler or analyzer diagnostic."
        }
        Write-Host "Undocumented API correctly rejected with CSENSE001."

        Set-Content Consumer.cs @'
/// <summary>A documented API.</summary>
public class Documented { }
'@
        $output = (& dotnet build Consumer.csproj --no-restore --configuration Release --nologo --target:Rebuild 2>&1 | Out-String)
        $buildExitCode = $LASTEXITCODE
        Write-Host $output
        if ($buildExitCode -ne 0 -or $output -match '\b(?:warning|error) [A-Z]+\d+\b') {
            throw "The documented consumer must build without compiler or analyzer warnings."
        }
        Write-Host "Package consumption check passed for CommentSense $version."
    }
    finally { Pop-Location }
}
finally {
    $resolvedCheckRoot = (Resolve-Path -LiteralPath $checkRoot).Path
    $tempRoot = [IO.Path]::GetFullPath([IO.Path]::GetTempPath())
    if (-not $resolvedCheckRoot.StartsWith($tempRoot, [StringComparison]::OrdinalIgnoreCase)) {
        throw "Refusing to remove a check directory outside '$tempRoot'."
    }
    Remove-Item -LiteralPath $resolvedCheckRoot -Recurse -Force
}
