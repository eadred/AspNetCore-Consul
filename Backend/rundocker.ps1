$dockerhost = "dockerhost"

docker-machine env $dockerhost | Invoke-Expression
docker build -t backend .
docker rm -f backend

# Determine the IP address of the host as it will appear to this container
$hostip = Invoke-Command -ScriptBlock { docker-machine ssh $dockerhost "ifconfig docker0 | grep 'inet addr' | cut -d: -f2 | cut -d ' ' -f1" }

docker run --name=backend -d -e "CONSUL_HOST=$hostip" backend
