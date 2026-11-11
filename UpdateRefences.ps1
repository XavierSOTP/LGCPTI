function Get-SSReferenceObjects {
    param (
        [Parameter(Mandatory=$true)]
        [string]$dllsPath
    )
$dirs = Get-ChildItem -Path  $dllsPath -Directory
$references = @()
foreach ($dir in $dirs) {
    $dllPath = "$($dir.FullName)\latest\$($dir.Name).dll"
    $dllExist = Test-Path $dllPath -PathType Leaf
    if($dllExist)
    {
        write-host "Dll path updated for $($dir.Name)"
        $references += [PSCustomObject]@{ 'ReferenceName' = $dir.Name
                         'DllPath' = $dllPath }
    }
    else
    {
        Write-Host "Dll not found $($dir.Name)"
    }
}
$references
}


function Get-ReferenceObjects {
    param (
        [Parameter(Mandatory=$true)]
        [string]$dllsPath,
        [Parameter(Mandatory=$true)]
        [string[]]$dlls
    )
    $references = @()
    foreach ($dll in $dlls) {
        $dllPath = Get-Item -Path  "$dllsPath\$($dll).dll"
        $dllExist = Test-Path $dllPath -PathType Leaf
        if($dllExist)
        {
            write-host "Dll path updated for $($dll)"
            $references += [PSCustomObject]@{ 'ReferenceName' = $dll
                            'DllPath' = $dllPath }
        }
        else
        {
            Write-Host "Dll not found $($dll)"
        }
    }
    
    $references
}


function Get-References{
param (
    [Parameter(Mandatory=$true)]
    [string]$solutionPath,
    [Parameter(Mandatory=$true)]
    [PSCustomObject[]]$referencesObject    
)

    $csprojFiles = Get-ChildItem -Path $solutionPath -Filter *.csproj -Recurse
    foreach ($csprojFile in $csprojFiles)
    {        
        $xml = [xml](Get-Content $csprojFile.FullName)
        $referenceNodes = $xml.Project.ItemGroup.Reference | Where-Object{$_ -ne $null -and $null -ne $_.HintPath} 
        foreach ($currentItemName in $referencesObject) {
            $reference = $referenceNodes|Where-Object{$_.Include.Split(",")[0] -eq $currentItemName.ReferenceName} | Select-Object -First 1
            if($null -ne $reference.HintPath){
                $reference.HintPath = $currentItemName.DllPath                         
            }            
        }        
        $xml.Save($csprojFile.FullName)            
    }
        
}


$solutionPath = "H:\SSBackUP\Git\Actives\LGC.PTI.RestApi"
$softsolutedlls = "H:\Softsolute\SoftSolutedlls"

$references =Get-SSReferenceObjects  -dllsPath $softsolutedlls
Get-References -solutionPath $solutionPath -referencesObject $references
