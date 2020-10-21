FROM mcr.microsoft.com/dotnet/core/aspnet:3.1 AS base
WORKDIR /app
ARG USERNAME=wutang
ARG USER_UID=1000
ARG USER_GID=$USER_UID

# Create and switch to the user
RUN groupadd --gid $USER_GID $USERNAME \
    && useradd --uid $USER_UID --gid $USER_GID -m $USERNAME

FROM mcr.microsoft.com/dotnet/core/sdk:3.1 AS build
WORKDIR /src
COPY ["LoadProcessor.csproj", "./"]
RUN dotnet restore "LoadProcessor.csproj"
COPY . .
WORKDIR /src
RUN dotnet build "LoadProcessor.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "LoadProcessor.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
USER $USERNAME
ENTRYPOINT ["dotnet", "LoadProcessor.dll"]