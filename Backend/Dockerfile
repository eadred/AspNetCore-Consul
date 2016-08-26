FROM microsoft/dotnet:latest

RUN apt-get update && apt-get install -y dos2unix
RUN apt-get update && apt-get install -y net-tools

COPY . /app

RUN dos2unix /app/run.sh
RUN chmod +x /app/run.sh

WORKDIR /app

RUN ["dotnet", "restore"]

RUN ["dotnet", "build"]

EXPOSE 5100/tcp

ENTRYPOINT ["./run.sh"]
