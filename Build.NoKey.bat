@echo off

set msbuild="%ProgramFiles(x86)%\MSBuild\14.0\Bin\msbuild.exe"

%msbuild% WebSocket4Net.Net.NoKey.build /t:Build

pause