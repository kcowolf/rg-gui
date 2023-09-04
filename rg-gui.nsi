############################################################################################
#      NSIS Installation Script created by NSIS Quick Setup Script Generator v1.09.18
#               Entirely Edited with NullSoft Scriptable Installation System                
#              by Vlasis K. Barkas aka Red Wine red_wine@freemail.gr Sep 2006               
############################################################################################

!define APP_NAME "rg-gui"
!define COMP_NAME "kcowolf"
!define VERSION "0.2.1.0"
!define COPYRIGHT "Benjamin Stauffer © 2023"
!define DESCRIPTION "Application"
!define INSTALLER_NAME "rg-gui-installer.exe"
!define MAIN_APP_EXE "rg-gui.exe"
!define INSTALL_TYPE "SetShellVarContext current"
!define REG_ROOT "HKCU"
!define REG_APP_PATH "Software\Microsoft\Windows\CurrentVersion\App Paths\${MAIN_APP_EXE}"
!define UNINSTALL_PATH "Software\Microsoft\Windows\CurrentVersion\Uninstall\${APP_NAME}"
!define LICENSE_TXT "LICENSE"

!define REG_START_MENU "rg-gui"

var SM_Folder

######################################################################

VIProductVersion  "${VERSION}"
VIAddVersionKey "ProductName"  "${APP_NAME}"
VIAddVersionKey "CompanyName"  "${COMP_NAME}"
VIAddVersionKey "LegalCopyright"  "${COPYRIGHT}"
VIAddVersionKey "FileDescription"  "${DESCRIPTION}"
VIAddVersionKey "FileVersion"  "${VERSION}"

######################################################################

SetCompressor ZLIB
Name "${APP_NAME}"
Caption "${APP_NAME}"
OutFile "${INSTALLER_NAME}"
BrandingText "${APP_NAME}"
XPStyle on
InstallDirRegKey "${REG_ROOT}" "${REG_APP_PATH}" ""
InstallDir "$PROGRAMFILES64\rg-gui"

######################################################################

!include "MUI.nsh"

!define MUI_ABORTWARNING
!define MUI_UNABORTWARNING

!insertmacro MUI_PAGE_WELCOME

!ifdef LICENSE_TXT
!insertmacro MUI_PAGE_LICENSE "${LICENSE_TXT}"
!endif

!insertmacro MUI_PAGE_DIRECTORY

!ifdef REG_START_MENU
!define MUI_STARTMENUPAGE_DEFAULTFOLDER "rg-gui"
!define MUI_STARTMENUPAGE_REGISTRY_ROOT "${REG_ROOT}"
!define MUI_STARTMENUPAGE_REGISTRY_KEY "${UNINSTALL_PATH}"
!define MUI_STARTMENUPAGE_REGISTRY_VALUENAME "${REG_START_MENU}"
!insertmacro MUI_PAGE_STARTMENU Application $SM_Folder
!endif

!insertmacro MUI_PAGE_INSTFILES

!define MUI_FINISHPAGE_RUN "$INSTDIR\${MAIN_APP_EXE}"
!insertmacro MUI_PAGE_FINISH

!insertmacro MUI_UNPAGE_CONFIRM

!insertmacro MUI_UNPAGE_INSTFILES

!insertmacro MUI_UNPAGE_FINISH

!insertmacro MUI_LANGUAGE "English"

######################################################################

Section -MainProgram
${INSTALL_TYPE}
SetOverwrite ifnewer
SetOutPath "$INSTDIR"
File "rg-gui\bin\Release\net6.0-windows\CliWrap.dll"
File "rg-gui\bin\Release\net6.0-windows\ConcurrentCollections.dll"
File "rg-gui\bin\Release\net6.0-windows\Microsoft.Win32.SystemEvents.dll"
File "rg-gui\bin\Release\net6.0-windows\Ookii.Dialogs.Wpf.dll"
File "rg-gui\bin\Release\net6.0-windows\rg-gui.deps.json"
File "rg-gui\bin\Release\net6.0-windows\rg-gui.dll"
File "rg-gui\bin\Release\net6.0-windows\rg-gui.dll.config"
File "rg-gui\bin\Release\net6.0-windows\rg-gui.exe"
File "rg-gui\bin\Release\net6.0-windows\rg-gui.pdb"
File "rg-gui\bin\Release\net6.0-windows\rg-gui.runtimeconfig.json"
File "rg-gui\bin\Release\net6.0-windows\System.Configuration.ConfigurationManager.dll"
File "rg-gui\bin\Release\net6.0-windows\System.Drawing.Common.dll"
File "rg-gui\bin\Release\net6.0-windows\System.Security.Cryptography.ProtectedData.dll"
File "rg-gui\bin\Release\net6.0-windows\System.Security.Permissions.dll"
File "rg-gui\bin\Release\net6.0-windows\System.Windows.Extensions.dll"
File "depends\ripgrep\rg.exe"
SetOutPath "$INSTDIR\runtimes\win\lib\net6.0"
File "rg-gui\bin\Release\net6.0-windows\runtimes\win\lib\net6.0\Microsoft.Win32.SystemEvents.dll"
File "rg-gui\bin\Release\net6.0-windows\runtimes\win\lib\net6.0\System.Drawing.Common.dll"
File "rg-gui\bin\Release\net6.0-windows\runtimes\win\lib\net6.0\System.Security.Cryptography.ProtectedData.dll"
File "rg-gui\bin\Release\net6.0-windows\runtimes\win\lib\net6.0\System.Windows.Extensions.dll"
SectionEnd

######################################################################

Section -Icons_Reg
SetOutPath "$INSTDIR"
WriteUninstaller "$INSTDIR\uninstall.exe"

!ifdef REG_START_MENU
!insertmacro MUI_STARTMENU_WRITE_BEGIN Application
CreateDirectory "$SMPROGRAMS\$SM_Folder"
CreateShortCut "$SMPROGRAMS\$SM_Folder\${APP_NAME}.lnk" "$INSTDIR\${MAIN_APP_EXE}"
CreateShortCut "$SMPROGRAMS\$SM_Folder\Uninstall ${APP_NAME}.lnk" "$INSTDIR\uninstall.exe"

!ifdef WEB_SITE
WriteIniStr "$INSTDIR\${APP_NAME} website.url" "InternetShortcut" "URL" "${WEB_SITE}"
CreateShortCut "$SMPROGRAMS\$SM_Folder\${APP_NAME} Website.lnk" "$INSTDIR\${APP_NAME} website.url"
!endif
!insertmacro MUI_STARTMENU_WRITE_END
!endif

!ifndef REG_START_MENU
CreateDirectory "$SMPROGRAMS\rg-gui"
CreateShortCut "$SMPROGRAMS\rg-gui\${APP_NAME}.lnk" "$INSTDIR\${MAIN_APP_EXE}"
CreateShortCut "$SMPROGRAMS\rg-gui\Uninstall ${APP_NAME}.lnk" "$INSTDIR\uninstall.exe"

!ifdef WEB_SITE
WriteIniStr "$INSTDIR\${APP_NAME} website.url" "InternetShortcut" "URL" "${WEB_SITE}"
CreateShortCut "$SMPROGRAMS\rg-gui\${APP_NAME} Website.lnk" "$INSTDIR\${APP_NAME} website.url"
!endif
!endif

WriteRegStr ${REG_ROOT} "${REG_APP_PATH}" "" "$INSTDIR\${MAIN_APP_EXE}"
WriteRegStr ${REG_ROOT} "${UNINSTALL_PATH}"  "DisplayName" "${APP_NAME}"
WriteRegStr ${REG_ROOT} "${UNINSTALL_PATH}"  "UninstallString" "$INSTDIR\uninstall.exe"
WriteRegStr ${REG_ROOT} "${UNINSTALL_PATH}"  "DisplayIcon" "$INSTDIR\${MAIN_APP_EXE}"
WriteRegStr ${REG_ROOT} "${UNINSTALL_PATH}"  "DisplayVersion" "${VERSION}"
WriteRegStr ${REG_ROOT} "${UNINSTALL_PATH}"  "Publisher" "${COMP_NAME}"

!ifdef WEB_SITE
WriteRegStr ${REG_ROOT} "${UNINSTALL_PATH}"  "URLInfoAbout" "${WEB_SITE}"
!endif
SectionEnd

######################################################################

Section Uninstall
${INSTALL_TYPE}
Delete "$INSTDIR\CliWrap.dll"
Delete "$INSTDIR\ConcurrentCollections.dll"
Delete "$INSTDIR\Microsoft.Win32.SystemEvents.dll"
Delete "$INSTDIR\Ookii.Dialogs.Wpf.dll"
Delete "$INSTDIR\rg-gui.deps.json"
Delete "$INSTDIR\rg-gui.dll"
Delete "$INSTDIR\rg-gui.dll.config"
Delete "$INSTDIR\rg-gui.exe"
Delete "$INSTDIR\rg-gui.pdb"
Delete "$INSTDIR\rg-gui.runtimeconfig.json"
Delete "$INSTDIR\rg.exe"
Delete "$INSTDIR\System.Configuration.ConfigurationManager.dll"
Delete "$INSTDIR\System.Drawing.Common.dll"
Delete "$INSTDIR\System.Security.Cryptography.ProtectedData.dll"
Delete "$INSTDIR\System.Security.Permissions.dll"
Delete "$INSTDIR\System.Windows.Extensions.dll"
Delete "$INSTDIR\runtimes\win\lib\net6.0\Microsoft.Win32.SystemEvents.dll"
Delete "$INSTDIR\runtimes\win\lib\net6.0\System.Drawing.Common.dll"
Delete "$INSTDIR\runtimes\win\lib\net6.0\System.Security.Cryptography.ProtectedData.dll"
Delete "$INSTDIR\runtimes\win\lib\net6.0\System.Windows.Extensions.dll"
 
RmDir "$INSTDIR\runtimes\win\lib\net6.0"
RmDir "$INSTDIR\runtimes\win\lib"
RmDir "$INSTDIR\runtimes\win"
RmDir "$INSTDIR\runtimes"

Delete "$INSTDIR\uninstall.exe"
!ifdef WEB_SITE
Delete "$INSTDIR\${APP_NAME} website.url"
!endif

RmDir "$INSTDIR"

!ifdef REG_START_MENU
!insertmacro MUI_STARTMENU_GETFOLDER "Application" $SM_Folder
Delete "$SMPROGRAMS\$SM_Folder\${APP_NAME}.lnk"
Delete "$SMPROGRAMS\$SM_Folder\Uninstall ${APP_NAME}.lnk"
!ifdef WEB_SITE
Delete "$SMPROGRAMS\$SM_Folder\${APP_NAME} Website.lnk"
!endif
RmDir "$SMPROGRAMS\$SM_Folder"
!endif

!ifndef REG_START_MENU
Delete "$SMPROGRAMS\rg-gui\${APP_NAME}.lnk"
Delete "$SMPROGRAMS\rg-gui\Uninstall ${APP_NAME}.lnk"
!ifdef WEB_SITE
Delete "$SMPROGRAMS\rg-gui\${APP_NAME} Website.lnk"
!endif
RmDir "$SMPROGRAMS\rg-gui"
!endif

DeleteRegKey ${REG_ROOT} "${REG_APP_PATH}"
DeleteRegKey ${REG_ROOT} "${UNINSTALL_PATH}"
SectionEnd

######################################################################

