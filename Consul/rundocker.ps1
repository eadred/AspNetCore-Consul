docker-machine env dockerhost | Invoke-Expression
docker rm -f consul
docker run --name=consul --net=host consul agent -dev -bind 127.0.0.1
