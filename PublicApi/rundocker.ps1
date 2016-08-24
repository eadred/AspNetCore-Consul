docker-machine env dockerhost | Invoke-Expression
docker build -t publicapi .
docker rm -f publicapi
docker run --name=publicapi --net=host publicapi
