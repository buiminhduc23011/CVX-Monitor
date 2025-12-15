[Setup]
AppName=CVX-QLSX.App
AppVersion=1.0.0
DefaultDirName={pf}\CVX-QLSX.App
DefaultGroupName=CVX-QLSX.App
OutputBaseFilename=CVX-QLSX.App_Setup
SetupIconFile=C:\Users\ducbu\Documents\GitHub\CVX-Monitor\AppRelease\Release\Logo.ico
Compression=lzma
SolidCompression=yes

[Files]
Source: "C:\Users\ducbu\Documents\GitHub\CVX-Monitor\AppRelease\Release\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{group}\CVX-QLSX.App"; Filename: "{app}\CVX-QLSX.App.exe"; IconFilename: "{app}\Logo.ico"
Name: "{commondesktop}\CVX-QLSX.App"; Filename: "{app}\CVX-QLSX.App.exe"; Tasks: desktopicon; IconFilename: "{app}\Logo.ico"

[Tasks]
Name: "desktopicon"; Description: "Create a shortcut on desktop"; GroupDescription: "Additional options:"

[Run]
Filename: "https://dotnet.microsoft.com/en-us/download/dotnet/thank-you/runtime-desktop-8.0.5-windows-x64-installer"; \
    Description: ".NET 8 Desktop Runtime (required if not already installed)"; \
    StatusMsg: "Installing .NET 8 Desktop Runtime if your system does not have it."; \
    Flags: nowait postinstall shellexec skipifdoesntexist
