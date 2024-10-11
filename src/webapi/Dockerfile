FROM --platform=$BUILDPLATFORM mcr.microsoft.com/dotnet/sdk:8.0 AS build
COPY src/common/PricingTf.Common.fsproj src/common/PricingTf.Common.fsproj
COPY src/webapi/PricingTf.WebApi.csproj src/webapi/PricingTf.WebApi.csproj
RUN dotnet restore src/webapi
COPY src/common src/common
COPY src/webapi src/webapi
RUN dotnet publish src/webapi -c Release -o /out

FROM --platform=$BUILDPLATFORM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
RUN apt-get update && apt-get install etcd-client -y
WORKDIR /app
COPY --from=build /out .
COPY src/webapi/Scripts /Scripts
RUN chmod +x /Scripts/service-registration.sh
RUN bash /Scripts/service-registration.sh
EXPOSE 8080
RUN bash
ENTRYPOINT ["dotnet", "PricingTf.WebApi.dll"]