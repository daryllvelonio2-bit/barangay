[Setup]
AppName=Barangay Management System
AppVersion=1.0.0
AppPublisher=Barangay System
DefaultDirName={autopf}\BarangaySystem
DefaultGroupName=Barangay Management System
OutputDir=.\installer_output
OutputBaseFilename=BarangaySystem_Setup
Compression=lzma2/ultra64
SolidCompression=yes
SetupIconFile=app.ico
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
UninstallDisplayIcon={app}\baranggaysystem1.exe
WizardStyle=modern
PrivilegesRequired=lowest
; Require Windows 10 or later (needed for .NET 8)
MinVersion=10.0

[Files]
; Main executable (single-file self-contained publish)
Source: "publish_sf\baranggaysystem1.exe"; DestDir: "{app}"; Flags: ignoreversion
; App Icon
Source: "app.ico"; DestDir: "{app}"; Flags: ignoreversion
; SQL and database assets bundled alongside the exe
Source: "publish_sf\Database\*"; DestDir: "{app}\Database"; Flags: ignoreversion recursesubdirs createallsubdirs
; Font assets
Source: "publish_sf\LatoFont\*"; DestDir: "{app}\LatoFont"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{group}\Barangay Management System"; Filename: "{app}\baranggaysystem1.exe"; IconFilename: "{app}\app.ico"
Name: "{group}\Uninstall Barangay Management System"; Filename: "{uninstallexe}"
Name: "{autodesktop}\Barangay Management System"; Filename: "{app}\baranggaysystem1.exe"; IconFilename: "{app}\app.ico"; Tasks: desktopicon

[Tasks]
Name: "desktopicon"; Description: "Create a desktop shortcut"; GroupDescription: "Additional icons:"

[Run]
Filename: "{app}\baranggaysystem1.exe"; Description: "Launch Barangay Management System"; Flags: nowait postinstall skipifsilent
