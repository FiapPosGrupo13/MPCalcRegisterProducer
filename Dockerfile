# Usar a imagem oficial do .NET 8 para compilação
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /app

# Copiar arquivos do projeto
COPY . ./

# Restaurar dependências
RUN dotnet restore

# Compilar em modo Release
RUN dotnet publish -c Release -o /out

# Usar a imagem do .NET 8 para runtime
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

# Copiar os arquivos compilados
COPY --from=build /out .

# Expor a porta do serviço
EXPOSE 5022

# Executar a aplicação
ENTRYPOINT ["dotnet", "MPCalcRegisterProducer.dll"]
