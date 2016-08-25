$dockerhost = "dockerhost"

docker-machine env $dockerhost | Invoke-Expression
docker build -t publicapi .
docker rm -f publicapi

# Determine the IP address of the host as it will appear to this container
$hostip = Invoke-Command -ScriptBlock { docker-machine ssh $dockerhost "ifconfig docker0 | grep 'inet addr' | cut -d: -f2 | cut -d ' ' -f1" }

docker run --name=publicapi -p 5000:5000 -d -e "CONSUL_HOST=$hostip" publicapi
