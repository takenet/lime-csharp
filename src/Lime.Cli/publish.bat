dotnet publish --self-contained --runtime win-x64 -c Release
cd .\bin\Release\netcoreapp2.1\win-x64\publish\
choco pack
choco push --api-key [CHOCOLATEY-API-KEY]