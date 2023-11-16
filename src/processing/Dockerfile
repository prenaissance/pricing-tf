FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build

COPY /src/processing/PricingTf.Processing.fsproj /src/processing/
COPY /src/common/PricingTf.Common.fsproj /src/common/
RUN dotnet restore ./src/processing/PricingTf.Processing.fsproj
COPY /src/processing/ /src/processing/
COPY /src/common/ /src/common/
RUN dotnet publish src/processing -c Release -o out

# Build runtime image
FROM mcr.microsoft.com/dotnet/runtime:8.0
WORKDIR /app
COPY --from=build /out .
ENTRYPOINT ["dotnet", "PricingTf.Processing.dll"]
