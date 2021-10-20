FROM mcr.microsoft.com/dotnet/sdk:5.0-focal AS build
ARG userName
ARG nugetUrl
ARG pass
ARG version

WORKDIR /src

# setup ccc nuget repo for pulling common, messages, etc.
RUN dotnet nuget add source $nugetUrl --name ccc --username $userName --password $pass --store-password-in-clear-text

COPY ./CCC.CAS.Workflow4Api/CCC.CAS.Workflow4Api.csproj CCC.CAS.Workflow4Api/

RUN mkdir -p /packages
COPY ./packages/*.nupkg /packages/
RUN ls /packages/CCC.CAS.*.nupkg \
    && dotnet nuget add source /packages --name Local \
    || echo Skipping local nuget for /packages. Ok for CI build.

WORKDIR /src/CCC.CAS.Workflow4Api
RUN dotnet restore

COPY . .
RUN dotnet publish "CCC.CAS.Workflow4Api/CCC.CAS.Workflow4Api.csproj" -c Release -o /app -p:Version=$version,AssemblyVersion=$version


# final, runtime stage copies from build
FROM mcr.microsoft.com/dotnet/aspnet:5.0-focal
WORKDIR /app

ENV ASPNETCORE_ENVIRONMENT Production
EXPOSE 8080

RUN adduser --system --uid 1999 --group appuser
USER 1999

COPY --from=build /app .

ENTRYPOINT ["dotnet", "CCC.CAS.Workflow4Api.dll"]
