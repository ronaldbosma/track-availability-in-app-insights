<# 
  The Logic App has a custom .NET code project. We need to build the .NET project first
  in order for the assemblies to be included in the /lib/custom folder of the Logic App
#>

dotnet build ../Functions/TrackAvailabilityInAppInsights.LogicApp.Functions.csproj --configuration Release
