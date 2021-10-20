FROM rulesenginecontainerregistry.azurecr.io/sonarqube-base/dotnet/sdk-5.0-focal:5.16.3-1 AS build
ARG userName
ARG nugetUrl
ARG pass
ARG version
ARG sonarQubeToken

WORKDIR /src

# setup ccc nuget repo for pulling common, messages, etc.
RUN dotnet nuget add source $nugetUrl --name ccc --username $userName --password $pass --store-password-in-clear-text

COPY ./CCC.CAS.Workflow4Api/CCC.CAS.Workflow4Api.csproj CCC.CAS.Workflow4Api/

RUN mkdir -p /packages
COPY ./packages/*.nupkg /packages/
RUN ls /packages/CCC.CAS.*.nupkg \
    && dotnet nuget add source /packages --name Local \
    || echo Skipping local nuget for /packages. Ok for CI or local build.

WORKDIR /src/CCC.CAS.Workflow4Api
RUN dotnet restore

COPY . .

RUN export PATH="$PATH:/root/.dotnet/tools" && \
     dotnet sonarscanner begin /k:"CAS-Workflow4Api" /d:sonar.host.url="https://sonarqube.cccis.com" /d:sonar.login=$sonarQubeToken /v:$version; \
     exit 0

RUN dotnet publish "CCC.CAS.Workflow4Api/CCC.CAS.Workflow4Api.csproj" -c Release -o /app -p:Version=$version,AssemblyVersion=$version

RUN export PATH="$PATH:/root/.dotnet/tools" && \
     dotnet sonarscanner end /d:sonar.login=$sonarQubeToken ; \
     exit 0


# final, runtime stage copies from build
FROM mcr.microsoft.com/dotnet/aspnet:5.0-focal
WORKDIR /app

ENV ASPNETCORE_ENVIRONMENT Production
EXPOSE 8080

RUN adduser --system --uid 1999 --group appuser
USER 1999

COPY --from=build /app .

ENTRYPOINT ["dotnet", "CCC.CAS.Workflow4Api.dll"]
