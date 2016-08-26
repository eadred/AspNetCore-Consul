$network = $args[0]

if(!$network)
{
  echo "Network not specified"
  return 1
}

$OldLoc = Get-Location
Set-Location ($MyInvocation.MyCommand.Path | Split-Path)

# Rebuild the image
docker build -t backend .

# Remove any old container
docker stop backend
docker rm -f backend

# Run the new container
docker run --name=backend --net=$network -d backend

Set-Location $OldLoc
