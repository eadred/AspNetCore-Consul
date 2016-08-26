$network = $args[0]

if(!$network)
{
  echo "Network not specified"
  return 1
}

$OldLoc = Get-Location
Set-Location ($MyInvocation.MyCommand.Path | Split-Path)

# Rebuild the image
docker build -t publicapi .

# Remove any old container
docker stop publicapi
docker rm -f publicapi

# Run the new container, exposing port 5000
docker run --name=publicapi -p 5000:5000 --net=$network -d publicapi

Set-Location $OldLoc
