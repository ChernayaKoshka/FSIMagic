#Include <Windows Explorer>

activeFsiPid := 0
!`::
    if (activeFsiPid and WinExist("ahk_pid" . activeFsiPid))
    {
        WinActivate, % "ahk_pid" . activeFsiPid
    }
    else
    {
        toRun := COMSPEC . " /Q /K dotnet fsi --shadowcopyreferences+"

        useFiles := ""
        Loop, Files, % A_MyDocuments "\FSI Scripts\*.fsx", F
            useFiles .=  " --use:""" . A_LoopFileFullPath . """`n"
        Sort, useFiles

        Loop, Parse, useFiles, `n
            toRun .= A_LoopField

        Run, % toRun, % Explorer_GetPath(), , fsiPid
        activeFsiPid := fsiPid
    }
return

#IfWinActive, ahk_exe explorer.exe
F3::
selected := Explorer_GetSelected()
Run % COMSPEC . " /Q /C code """ . selected . """",, Hide
return
