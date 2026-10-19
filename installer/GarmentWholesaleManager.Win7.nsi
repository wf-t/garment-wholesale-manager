Unicode true

!ifndef PROJECT_DIR
  !error "PROJECT_DIR is required"
!endif
!ifndef OUTPUT_FILE
  !error "OUTPUT_FILE is required"
!endif

!include "MUI2.nsh"
!include "LogicLib.nsh"
!include "WinVer.nsh"
!include "x64.nsh"

!define APP_NAME "服装进销存批发管理"
!define APP_EXE "GarmentWholesaleManager.exe"
!define APP_PUBLISHER "杉泉服饰"
!define APP_REG_KEY "Software\Shanquan\GarmentWholesaleManager"
!define UNINSTALL_REG_KEY "Software\Microsoft\Windows\CurrentVersion\Uninstall\GarmentWholesaleManager"

Name "${APP_NAME}"
OutFile "${OUTPUT_FILE}"
InstallDir "$PROGRAMFILES64\杉泉服饰\服装进销存批发管理"
InstallDirRegKey HKLM "${APP_REG_KEY}" "InstallLocation"
RequestExecutionLevel admin
SetCompressor /SOLID lzma
SetDatablockOptimize on
ShowInstDetails show
ShowUninstDetails show
BrandingText "杉泉服饰"
Icon "${PROJECT_DIR}/src/Shanquan.Inventory.Desktop/Assets/avalonia-logo.ico"
UninstallIcon "${PROJECT_DIR}/src/Shanquan.Inventory.Desktop/Assets/avalonia-logo.ico"

VIProductVersion "1.0.0.0"
VIAddVersionKey /LANG=2052 "ProductName" "${APP_NAME}"
VIAddVersionKey /LANG=2052 "CompanyName" "${APP_PUBLISHER}"
VIAddVersionKey /LANG=2052 "FileDescription" "${APP_NAME} Windows 7 安装程序"
VIAddVersionKey /LANG=2052 "FileVersion" "1.0.0"
VIAddVersionKey /LANG=2052 "ProductVersion" "1.0.0"
VIAddVersionKey /LANG=2052 "LegalCopyright" "Copyright (C) 2026 杉泉服饰"

!define MUI_ABORTWARNING
!define MUI_ICON "${PROJECT_DIR}/src/Shanquan.Inventory.Desktop/Assets/avalonia-logo.ico"
!define MUI_UNICON "${PROJECT_DIR}/src/Shanquan.Inventory.Desktop/Assets/avalonia-logo.ico"
!define MUI_FINISHPAGE_RUN "$INSTDIR\${APP_EXE}"
!define MUI_FINISHPAGE_RUN_TEXT "立即运行 ${APP_NAME}"

!insertmacro MUI_PAGE_WELCOME
!insertmacro MUI_PAGE_INSTFILES
!insertmacro MUI_PAGE_FINISH

!insertmacro MUI_UNPAGE_CONFIRM
!insertmacro MUI_UNPAGE_INSTFILES
!insertmacro MUI_UNPAGE_FINISH

!insertmacro MUI_LANGUAGE "SimpChinese"

Var PrerequisiteExitCode

Function .onInit
  ${IfNot} ${RunningX64}
    MessageBox MB_ICONSTOP|MB_OK "此安装包仅支持 64 位 Windows。"
    Abort
  ${EndIf}

  ${IfNot} ${AtLeastWin7}
    MessageBox MB_ICONSTOP|MB_OK "此程序要求 Windows 7 SP1 或更高版本。"
    Abort
  ${EndIf}

  ${IfNot} ${AtLeastWin8}
  ${AndIfNot} ${AtLeastServicePack} 1
    MessageBox MB_ICONSTOP|MB_OK "当前电脑尚未安装 Windows 7 Service Pack 1。$\r$\n请先将系统升级到 Windows 7 SP1，再重新运行本安装程序。"
    Abort
  ${EndIf}

  SetRegView 64
FunctionEnd

Section "安装 ${APP_NAME}" SEC_MAIN
  SectionIn RO

  SetShellVarContext all
  SetRegView 64
  SetOutPath "$PLUGINSDIR"

  ; .NET 6 在 Windows 7 上依赖 KB3063858。对 Win8 及更新系统无需安装。
  ${IfNot} ${AtLeastWin8}
    DetailPrint "正在准备 Windows 7 系统组件..."
    File /oname=KB3063858-x64.msu "${PROJECT_DIR}/installer/prerequisites/Windows6.1-KB3063858-x64.msu"

    IfFileExists "$WINDIR\Sysnative\wusa.exe" 0 use_native_wusa
      ExecWait '"$WINDIR\Sysnative\wusa.exe" "$PLUGINSDIR\KB3063858-x64.msu" /quiet /norestart' $PrerequisiteExitCode
      Goto kb_finished
    use_native_wusa:
      ExecWait '"$SYSDIR\wusa.exe" "$PLUGINSDIR\KB3063858-x64.msu" /quiet /norestart' $PrerequisiteExitCode
    kb_finished:
      ${If} $PrerequisiteExitCode == 3010
      ${OrIf} $PrerequisiteExitCode == 1641
        SetRebootFlag true
      ${EndIf}
  ${EndIf}

  ; 只在已安装 VC++ 14.29 或更新的 x64 运行库时跳过。
  ReadRegDWORD $0 HKLM "Software\Microsoft\VisualStudio\14.0\VC\Runtimes\x64" "Installed"
  ReadRegDWORD $1 HKLM "Software\Microsoft\VisualStudio\14.0\VC\Runtimes\x64" "Major"
  ReadRegDWORD $2 HKLM "Software\Microsoft\VisualStudio\14.0\VC\Runtimes\x64" "Minor"
  StrCpy $3 0
  ${If} $0 != 1
    StrCpy $3 1
  ${ElseIf} $1 < 14
    StrCpy $3 1
  ${ElseIf} $1 == 14
  ${AndIf} $2 < 29
    StrCpy $3 1
  ${EndIf}

  ${If} $3 == 1
    DetailPrint "正在安装程序运行组件..."
    File /oname=VC_redist.x64.exe "${PROJECT_DIR}/installer/prerequisites/VC_redist.x64.exe"
    ExecWait '"$PLUGINSDIR\VC_redist.x64.exe" /install /quiet /norestart' $PrerequisiteExitCode
    ${If} $PrerequisiteExitCode == 3010
    ${OrIf} $PrerequisiteExitCode == 1641
      SetRebootFlag true
    ${ElseIf} $PrerequisiteExitCode != 0
      MessageBox MB_ICONSTOP|MB_OK "系统运行组件安装失败（代码：$PrerequisiteExitCode）。请重启电脑后再次运行本安装程序。"
      Abort
    ${EndIf}
  ${EndIf}

  DetailPrint "正在安装 ${APP_NAME}..."
  SetOutPath "$INSTDIR"
  File /r "${PROJECT_DIR}/releases/win7-x64/*.*"

  WriteUninstaller "$INSTDIR\卸载.exe"
  WriteRegStr HKLM "${APP_REG_KEY}" "InstallLocation" "$INSTDIR"
  WriteRegStr HKLM "${UNINSTALL_REG_KEY}" "DisplayName" "${APP_NAME}"
  WriteRegStr HKLM "${UNINSTALL_REG_KEY}" "DisplayVersion" "1.0.0"
  WriteRegStr HKLM "${UNINSTALL_REG_KEY}" "Publisher" "${APP_PUBLISHER}"
  WriteRegStr HKLM "${UNINSTALL_REG_KEY}" "DisplayIcon" "$INSTDIR\${APP_EXE}"
  WriteRegStr HKLM "${UNINSTALL_REG_KEY}" "InstallLocation" "$INSTDIR"
  WriteRegStr HKLM "${UNINSTALL_REG_KEY}" "UninstallString" '"$INSTDIR\卸载.exe"'
  WriteRegDWORD HKLM "${UNINSTALL_REG_KEY}" "NoModify" 1
  WriteRegDWORD HKLM "${UNINSTALL_REG_KEY}" "NoRepair" 1

  CreateDirectory "$SMPROGRAMS\${APP_NAME}"
  CreateShortcut "$SMPROGRAMS\${APP_NAME}\${APP_NAME}.lnk" "$INSTDIR\${APP_EXE}" "" "$INSTDIR\${APP_EXE}" 0
  CreateShortcut "$SMPROGRAMS\${APP_NAME}\卸载.lnk" "$INSTDIR\卸载.exe"
  CreateShortcut "$DESKTOP\${APP_NAME}.lnk" "$INSTDIR\${APP_EXE}" "" "$INSTDIR\${APP_EXE}" 0
SectionEnd

Section "Uninstall"
  SetShellVarContext all
  SetRegView 64

  Delete "$DESKTOP\${APP_NAME}.lnk"
  RMDir /r "$SMPROGRAMS\${APP_NAME}"
  DeleteRegKey HKLM "${UNINSTALL_REG_KEY}"
  DeleteRegKey HKLM "${APP_REG_KEY}"
  RMDir /r "$INSTDIR"

  ; 业务数据库位于用户本地应用数据目录，卸载时刻意保留。
SectionEnd
