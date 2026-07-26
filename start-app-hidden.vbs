Set fso = CreateObject("Scripting.FileSystemObject")
Set WshShell = CreateObject("WScript.Shell")
folder = fso.GetParentFolderName(WScript.ScriptFullName)

' Set working directory first so cmd.exe doesn't need the full path (avoids & parsing issue)
WshShell.CurrentDirectory = folder
WshShell.Run "cmd.exe /c start-app-minimized.bat", 0, False
