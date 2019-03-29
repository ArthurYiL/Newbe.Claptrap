properties {
    $rootNow = Resolve-Path .
    $deployMode = "Release"
    $releaseDir = "$rootNow/build/"
    $nugetexe = "$rootNow/buildTools/nuget.exe"
}

# default task
Task Default -depends Build

Task Clean -Description "clean last build result" {
    Remove-Item $releaseDir -Force -Recurse -ErrorAction SilentlyContinue
}

Task Init -depends Clean -Description "init some data" {
    New-Item $releaseDir -ItemType Directory
}

Task Nuget -depends Init -Description "nuget restore" {
    Exec {
        dotnet restore
    }
}

Task Build -depends Nuget -Description "build sln" {
    Exec {
        dotnet build -c $deployMode
    }   
}

Task Test -depends Build -Description "run tests"{
    Exec {
        dotnet test -c $deployMode Newbe.Claptrap.sln
    }  
}

Task Pack -depends Test -Description "pack" {
    Exec {
        dotnet pack Newbe.Claptrap.sln -o $releaseDir
    }
}

Task PackTemplate -depends Init -Description "pack template package" {
    Exec {
        Get-ChildItem "Newbe.Claptrap.Template" bin -Force -Recurse | ForEach-Object {Remove-Item $_.FullName -Recurse -Force }
        Get-ChildItem "Newbe.Claptrap.Template" obj -Force -Recurse | ForEach-Object {Remove-Item $_.FullName -Recurse -Force }
        Get-ChildItem "Newbe.Claptrap.Template" ".vs" -Force -Recurse | ForEach-Object {Remove-Item $_.FullName -Recurse -Force }
        Get-ChildItem "Newbe.Claptrap.Template" ".idea" -Force -Recurse | ForEach-Object {Remove-Item $_.FullName -Recurse -Force }
        Get-ChildItem "Newbe.Claptrap.Template" "*.user" -Force -Recurse | ForEach-Object {Remove-Item $_.FullName -Recurse -Force }
        . $nugetexe pack "Newbe.Claptrap.Template\Newbe.Claptrap.Template.nuspec" -OutputDirectory $releaseDir
    }
}

Task NugetPushNuget -depends Pack,PackTemplate -Description "push package to nuget" {
    Get-ChildItem $releaseDir *.nupkg | ForEach-Object {
        Exec {
            dotnet nuget push "$releaseDir$_" -s https://www.nuget.org/
        }
    }
    Write-Output "build completed, now is $( Get-Date )"
}