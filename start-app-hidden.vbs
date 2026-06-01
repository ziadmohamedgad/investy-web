Set fso = CreateObject("Scripting.FileSystemObject")
Set WshShell = CreateObject("WScript.Shell")
folder = fso.GetParentFolderName(WScript.ScriptFullName)
bat = folder & "\start-app-minimized.bat"

'relaunch minimized batch hidden
WshShell.Run "cmd.exe /c """ & bat & """", 0, False
