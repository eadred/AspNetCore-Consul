docker-machine env dockerhost | Invoke-Expression
docker build -t backend .
docker rm -f backend
docker run --name=backend --net=host backend
