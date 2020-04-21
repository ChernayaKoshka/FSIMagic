#Include <Windows Explorer>

activeFsiPid := 0
!`::
    if ((!activeFsiPid or !WinExist("ahk_pid" . activeFsiPid)) and WinExist("ahk_exe fsi.exe"))
        WinGet, activeFsiPid, PID, % "ahk_exe fsi.exe"

    if (activeFsiPid and WinExist("ahk_pid" . activeFsiPid))
    {
        path := Explorer_GetPath()
        WinActivate, % "ahk_pid" . activeFsiPid
        WinWaitActive, % "ahk_pid" . activeFsiPid
        if (path != "ERROR")
            SendInput, % "cd @""" . path . """;;{ENTER}"
    }
    else
    {
        toRun := COMSPEC . " /Q /K fsi --shadowcopyreferences+"

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
