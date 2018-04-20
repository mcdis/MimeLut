cd MimeLut.Gen
call dotnet run ..\MimeLut\MimeLutTable.cs MimeLut.MimeLutTable"
cd ..
cd MimeLut
dotnet pack -c Release -o %~dp0
cd %~dp0