FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG SERVICE
WORKDIR /src
COPY Directory.Build.props ./
COPY Conduit.Shared/Conduit.Shared.csproj Conduit.Shared/
COPY Conduit.${SERVICE}/Conduit.${SERVICE}.csproj Conduit.${SERVICE}/
RUN dotnet restore Conduit.${SERVICE}/Conduit.${SERVICE}.csproj
COPY . .
RUN dotnet publish Conduit.${SERVICE}/Conduit.${SERVICE}.csproj -c Release -o /app --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0
ARG SERVICE
RUN mkdir -p /keys /recordings && chown app /keys && chmod 0777 /recordings
WORKDIR /app
COPY --from=build /app .
USER app
ENV SERVICE_DLL=Conduit.${SERVICE}.dll
ENTRYPOINT ["sh", "-c", "exec dotnet \"$SERVICE_DLL\""]
