$keyStoreHost = "keystore"
$primaryHost = "dockerhost"
$secondaryHost = "dockerhost2"
$networkName = "my-net"
$subnet = "10.0.9.0/24"

# Create host for the main Consul instance
# This host will not use the overlay network and is instead used
# to provide the keystore for when we create the swarm hosts
docker-machine create `
  --driver virtualbox --virtualbox-disk-size 10000 `
  $keyStoreHost

docker-machine env $keyStoreHost | Invoke-Expression

# Deploy Consul to the keystore host
Invoke-Command -ScriptBlock {./Consul/rundocker.ps1 $keyStoreHost}

$keyStoreHostIp = Invoke-Command -ScriptBlock {docker-machine ip $keyStoreHost}

# Provision the hosts that will be in our swarm
# For each of them we will also have a local Consul instance joined
# to the Consul cluster for service registration/discovery by containers
# on that host

# For some reason using $keyStoreHostIp directly here caused problems - wasn't able
# to contact the Docker daemon after provisioning
docker-machine create `
  --driver virtualbox --virtualbox-disk-size 10000 `
  --swarm --swarm-master `
  --swarm-discovery="consul://$(docker-machine ip $keyStoreHost):8500" `
  --engine-opt="cluster-store=consul://$(docker-machine ip $keyStoreHost):8500" `
  --engine-opt="cluster-advertise=eth1:2376" `
  $primaryHost

docker-machine env $primaryHost | Invoke-Expression
Invoke-Command -ScriptBlock {./Consul/rundocker.ps1 $primaryHost $keyStoreHostIp}

docker-machine create `
  --driver virtualbox --virtualbox-disk-size 10000 `
  --swarm `
  --swarm-discovery="consul://$(docker-machine ip $keyStoreHost):8500" `
  --engine-opt="cluster-store=consul://$(docker-machine ip $keyStoreHost):8500" `
  --engine-opt="cluster-advertise=eth1:2376" `
  $secondaryHost

docker-machine env $secondaryHost | Invoke-Expression
Invoke-Command -ScriptBlock {./Consul/rundocker.ps1 $secondaryHost $keyStoreHostIp}

# Create the overlay network
docker-machine env --swarm $primaryHost | Invoke-Expression
docker network create --driver overlay --subnet=$subnet $networkName

# Build and run our services
Invoke-Command -ScriptBlock {./Backend/rundocker.ps1 $networkName}
Invoke-Command -ScriptBlock {./PublicApi/rundocker.ps1 $networkName}
