$dockerhost = $args[0]

if (!$dockerhost)
{
  echo "Docker host not specified"
  exit 1
}

$mainConsulHostIp = $args[1]

$OldLoc = Get-Location
Set-Location ($MyInvocation.MyCommand.Path | Split-Path)

docker stop consul
docker rm -f consul

# IP address of the host as seen from the outside world
$hostip = Invoke-Command -ScriptBlock { docker-machine ssh $dockerhost "ifconfig eth1 | grep 'inet addr' | cut -d: -f2 | cut -d ' ' -f1" }

# Run Consul on the host's network and bind to the host's public IP so it is visible to other Consul instances we might have on other containers.
# Set the client endpoint to 0.0.0.0 so clients can connect from both within other containers and the outside world.
# If we only expected clients from other containers we could instead set this to $bridgeip (and not expose the port).

if ($mainConsulHostIp)
{
  echo "Running Consul at $hostip, joining $mainConsulHostIp"
  docker run --name=consul --net=host -p 8500:8500 -d consul agent -dev -bind $hostip -client 0.0.0.0 -join $mainConsulHostIp
}
else
{
  echo "Running Consul at $hostip"
  docker run --name=consul --net=host -p 8500:8500 -d consul agent -dev -bind $hostip -client 0.0.0.0
}

Set-Location $OldLoc
