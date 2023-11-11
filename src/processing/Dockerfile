FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /app
COPY /src/processing/PricingTf.Processing.fsproj .
RUN dotnet restore
COPY /src/processing/ .
RUN dotnet publish -c Release -o out

# Build runtime image
FROM mcr.microsoft.com/dotnet/runtime:8.0
WORKDIR /app
COPY --from=build /app/out .
ENTRYPOINT ["dotnet", "PricingTf.Processing.dll"]
