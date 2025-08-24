@echo off
echo Running unit tests...
dotnet test --configuration Debug --verbosity normal
pause