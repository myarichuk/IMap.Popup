

&'C:\Windows\Microsoft.NET\Framework64\v4.0.30319\msbuild.exe' ..\IMAP.Popup.sln /p:OutputPath=..\Binaries /p:Configuration=Release
&'C:\Program Files (x86)\WiX Toolset v3.8\bin\heat.exe' dir "..\Binaries" -gg -sfrag -srd -cg AppFilesAndLibs -dr INSTALLFOLDER -var var.BinariesFolder -out project.wxs 

$lines = Get-Content "project.wxs" 
ForEach ($line in $lines)
{
	if($line -eq '<Wix xmlns="http://schemas.microsoft.com/wix/2006/wi">')	
	{
		Add-Content "temp.txt" '<?define var.BinariesFolder = "..\Binaries"?>'
		Add-Content "temp.txt" $line 
	}
	else
	{
		Add-Content "temp.txt" $line 
	}
}

Remove-Item "project.wxs" 
Rename-Item "temp.txt" "project.wxs"

&'C:\Windows\Microsoft.NET\Framework64\v4.0.30319\msbuild.exe' installer.wixproj