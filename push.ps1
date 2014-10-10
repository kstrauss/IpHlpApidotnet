$nugetServer = "http://rdev-tfs:8001/"
$nugetProgram = "~\Downloads\NuGet.exe"
$pkgs = dir *.nupkg

$pkgs | %{
        & $nugetProgram push $($_.Name) -s $nugetServer
}