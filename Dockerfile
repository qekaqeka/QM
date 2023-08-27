FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build

COPY . .

RUN dotnet restore -v diag

RUN dotnet publish -c Release -o out

FROM mcr.microsoft.com/dotnet/aspnet:6.0

COPY --from=build /out .

ENTRYPOINT ["dotnet", "QM.dll"]