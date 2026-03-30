$now = Get-Date
$containerVersion = $now.ToString("yyyyMMdd-HHmmss")

$localContainerName = "games/mtt:$containerVersion"
$remoteContainerName = "gamecontainerscr.azurecr.io/mtt:$containerVersion"

docker build -f ./Dockerfile ./ -t $localContainerName

az acr login --name gamecontainerscr

docker tag $localContainerName $remoteContainerName

docker push $remoteContainerName

az deployment group create -f ./main.bicep -g games -p containerVersion=$containerVersion

docker rmi $remoteContainerName
docker rmi $localContainerName
