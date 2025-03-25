# Usar a imagem oficial do .NET 8 para compilação
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /app

# Copiar apenas o arquivo do projeto primeiro para otimizar cache
COPY MPCalcRegisterProducer/*.csproj ./MPCalcRegisterProducer/

# Entrar no diretório correto antes de rodar restore
WORKDIR /app/MPCalcRegisterProducer

# Restaurar dependências
RUN dotnet restore

# Voltar para a pasta raiz e copiar o restante dos arquivos do projeto
WORKDIR /app
COPY . .

# Publicar o projeto (agora apontando para o arquivo .csproj corretamente)
WORKDIR /app/MPCalcRegisterProducer
RUN dotnet publish -c Release -o /out

# Usar a imagem do .NET 8 para runtime
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

# Copiar os arquivos compilados
COPY --from=build /out .

# Expor a porta do serviço
EXPOSE 5020

# Executar a aplicação
ENTRYPOINT ["dotnet", "MPCalcRegisterProducer.dll"]
