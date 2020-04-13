REM dotnet test will not use the correct .NET Framework 4.7.2 overloads, VSTest.Console shipped with VS2019 is built using .NET Framework 4.7.2 and accurately reflects running under .NET Framework 4.7.2
dotnet build
SET VSTestCommunityPath="C:\Program Files (x86)\Microsoft Visual Studio\2019\Community\Common7\IDE\CommonExtensions\Microsoft\TestWindow\vstest.console.exe"
SET VSTestProfessionalPath="C:\Program Files (x86)\Microsoft Visual Studio\2019\Professional\Common7\IDE\CommonExtensions\Microsoft\TestWindow\vstest.console.exe"
SET VSTestEnterprisePath="C:\Program Files (x86)\Microsoft Visual Studio\2019\Enterprise\Common7\IDE\CommonExtensions\Microsoft\TestWindow\vstest.console.exe"
if exist %VSTestCommunityPath% (SET VSTestPath=%VSTestCommunityPath%)
if exist %VSTestProfessionalPath% (SET VSTestPath=%VSTestProfessionalPath%)
if exist %VSTestEnterprisePath% (SET VSTestPath=%VSTestEnterprisePath%)
if not defined VSTestPath echo VSTest.Console.exe was not found & exit /B 2
%VSTestPath% "StandardSocketsHttpHandler.FunctionalTests\bin\Debug\net472\StandardSocketsHttpHandler.FunctionalTests.dll" /TestCaseFilter:"category!=WindowsAuthentication&category!=nonnetfxtests&category!=activeissue&category!=failing"