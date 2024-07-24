FROM --platform=$BUILDPLATFORM mcr.microsoft.com/dotnet/sdk:8.0 AS build
COPY src/common/PricingTf.Common.fsproj src/common/PricingTf.Common.fsproj
COPY src/webapi/PricingTf.WebApi.csproj src/webapi/PricingTf.WebApi.csproj
RUN dotnet restore src/webapi
COPY src/common src/common
COPY src/webapi src/webapi
RUN dotnet publish src/webapi -c Release -o /out

FROM --platform=$BUILDPLATFORM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
COPY --from=build /out .
EXPOSE 8080
ENTRYPOINT ["dotnet", "PricingTf.WebApi.dll"]