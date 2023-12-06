REM dotnet test will not use the correct .NET Framework 4.7.2 overloads, VSTest.Console shipped with VS2019 is built using .NET Framework 4.7.2 and accurately reflects running under .NET Framework 4.7.2
dotnet build
SET VSTest2019CommunityPath="C:\Program Files (x86)\Microsoft Visual Studio\2019\Community\Common7\IDE\CommonExtensions\Microsoft\TestWindow\vstest.console.exe"
SET VSTest2019ProfessionalPath="C:\Program Files (x86)\Microsoft Visual Studio\2019\Professional\Common7\IDE\CommonExtensions\Microsoft\TestWindow\vstest.console.exe"
SET VSTest2019EnterprisePath="C:\Program Files (x86)\Microsoft Visual Studio\2019\Enterprise\Common7\IDE\CommonExtensions\Microsoft\TestWindow\vstest.console.exe"
SET VSTest2022CommunityPath="C:\Program Files\Microsoft Visual Studio\2022\Community\Common7\IDE\CommonExtensions\Microsoft\TestWindow\vstest.console.exe"
SET VSTest2022ProfessionalPath="C:\Program Files\Microsoft Visual Studio\2022\Professional\Common7\IDE\CommonExtensions\Microsoft\TestWindow\vstest.console.exe"
SET VSTest2022EnterprisePath="C:\Program Files\Microsoft Visual Studio\2022\Enterprise\Common7\IDE\CommonExtensions\Microsoft\TestWindow\vstest.console.exe"
if exist %VSTest2019CommunityPath% (SET VSTestPath=%VSTest2019CommunityPath%)
if exist %VSTest2019ProfessionalPath% (SET VSTestPath=%VSTest2019ProfessionalPath%)
if exist %VSTest2019EnterprisePath% (SET VSTestPath=%VSTest2019EnterprisePath%)
if exist %VSTest2022CommunityPath% (SET VSTestPath=%VSTest2022CommunityPath%)
if exist %VSTest2022ProfessionalPath% (SET VSTestPath=%VSTest2022ProfessionalPath%)
if exist %VSTest2022EnterprisePath% (SET VSTestPath=%VSTest2022EnterprisePath%)
if not defined VSTestPath echo VSTest.Console.exe was not found & exit /B 2
%VSTestPath% "StandardSocketsHttpHandler.FunctionalTests\bin\Debug\net472\StandardSocketsHttpHandler.FunctionalTests.dll" /TestCaseFilter:"category!=WindowsAuthentication&category!=nonnetfxtests&category!=activeissue&category!=failing"