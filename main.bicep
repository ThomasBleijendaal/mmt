param location string = resourceGroup().location

param namePrefix string = 'mpgcontainers'

param containerRegistryName string = '${namePrefix}cr'
param containerEnvironmentName string = '${namePrefix}-ace'
param containerAppUidName string = '${namePrefix}-uid'

param containerVersion string

resource ContainerAppIdentity 'Microsoft.ManagedIdentity/userAssignedIdentities@2023-07-31-preview' existing = {
  name: containerAppUidName
}

resource ContainerRegistry 'Microsoft.ContainerRegistry/registries@2023-11-01-preview' existing = {
  name: containerRegistryName
}

resource ContainerEnvironment 'Microsoft.App/managedEnvironments@2024-03-01' existing = {
   name: containerEnvironmentName
  
  resource Certificate 'managedCertificates' existing = {
    name: 'mmt.games.bleij.pro-mpgconta-260717083440'
  }
}

resource MttContainerApp 'Microsoft.App/containerApps@2025-10-02-preview' = {
  name: '${namePrefix}-mtt'
  location: location

  identity: {
   type: 'UserAssigned'
    userAssignedIdentities: {
      '${ContainerAppIdentity.id}': {}
    } 
  }

  properties: {
    environmentId: ContainerEnvironment.id
    managedEnvironmentId: ContainerEnvironment.id
    configuration: {
      activeRevisionsMode: 'Single'
      ingress: {
        external: true
        targetPort: 8080
        transport: 'http'
        customDomains: [
          {
            certificateId: ContainerEnvironment::Certificate.id
            bindingType: 'SniEnabled'
            name: 'mmt.games.bleij.pro'
          }
        ]
      }
      registries: [
        {
          server: '${ContainerRegistry.name}.azurecr.io'
          identity: ContainerAppIdentity.id
        }
      ]
      maxInactiveRevisions: 3
    }
    template: {
      revisionSuffix: containerVersion
      terminationGracePeriodSeconds: 30
      scale: {
        minReplicas: 0
        maxReplicas: 1
      }
      containers: [
        {
          image: '${ContainerRegistry.name}.azurecr.io/mtt:${containerVersion}'
          name: containerVersion
          resources: {
            cpu: any('0.25')
            memory: '0.5Gi'
          }
        }
      ]
    }
  }
}
