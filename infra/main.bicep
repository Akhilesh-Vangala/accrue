// LedgerFlow on Azure: one Windows App Service (API + Angular from wwwroot + Hangfire server and dashboard)
// backed by one Azure SQL database (application tables, reporting views and Hangfire storage).
//
// Deploy into an existing resource group:
//   az deployment group create -g rg-ledgerflow -f infra/main.bicep \
//     -p sqlAdminLogin=ledgerflowadmin sqlAdminPassword='<strong password>'

targetScope = 'resourceGroup'

@description('Short name used to derive resource names.')
@minLength(3)
@maxLength(12)
param appName string = 'ledgerflow'

@description('Deployment environment suffix, e.g. dev or prod.')
@allowed([
  'dev'
  'prod'
])
param environment string = 'dev'

param location string = resourceGroup().location

@description('SQL administrator login the app connects with.')
param sqlAdminLogin string

@secure()
@description('SQL administrator password. Pass it from a secret pipeline variable, never from source control.')
param sqlAdminPassword string

@description('Use the Azure SQL free offer (100k vCore seconds and 32 GB per month, one per subscription). Pauses instead of billing once exhausted.')
param useSqlFreeOffer bool = true

@description('App Service plan SKU. B1 is the cheapest tier with Always On, which Hangfire needs to keep recurring jobs firing. F1 is free but idles out, so recurring jobs only run while the site has traffic.')
@allowed([
  'F1'
  'B1'
  'B2'
  'S1'
  'P0v3'
])
param appServiceSku string = 'B1'

@description('Seed demo users, vendors and invoices on first start.')
param seedDemoData bool = true

@secure()
@description('Anthropic API key for invoice document extraction. Empty hides the import option.')
param anthropicApiKey string = ''

@description('Expose the OpenAPI document and Scalar UI outside Development.')
param enableApiReference bool = true

var suffix = uniqueString(resourceGroup().id, appName, environment)
var names = {
  plan: 'asp-${appName}-${environment}'
  site: 'app-${appName}-${environment}-${take(suffix, 6)}'
  sqlServer: 'sql-${appName}-${environment}-${take(suffix, 6)}'
  database: 'sqldb-${appName}'
}
var tags = {
  application: 'LedgerFlow'
  environment: environment
}

resource sqlServer 'Microsoft.Sql/servers@2023-08-01-preview' = {
  name: names.sqlServer
  location: location
  tags: tags
  properties: {
    administratorLogin: sqlAdminLogin
    administratorLoginPassword: sqlAdminPassword
    minimalTlsVersion: '1.2'
    publicNetworkAccess: 'Enabled'
  }
}

// 0.0.0.0 is Azure's convention for "allow Azure services", which lets App Service reach the server.
resource allowAzureServices 'Microsoft.Sql/servers/firewallRules@2023-08-01-preview' = {
  parent: sqlServer
  name: 'AllowAllWindowsAzureIps'
  properties: {
    startIpAddress: '0.0.0.0'
    endIpAddress: '0.0.0.0'
  }
}

resource database 'Microsoft.Sql/servers/databases@2023-08-01-preview' = {
  parent: sqlServer
  name: names.database
  location: location
  tags: tags
  sku: {
    name: 'GP_S_Gen5'
    tier: 'GeneralPurpose'
    family: 'Gen5'
    capacity: 2
  }
  properties: {
    autoPauseDelay: 60
    minCapacity: json('0.5')
    maxSizeBytes: 34359738368
    useFreeLimit: useSqlFreeOffer
    freeLimitExhaustionBehavior: useSqlFreeOffer ? 'AutoPause' : null
    requestedBackupStorageRedundancy: 'Local'
  }
}

// Windows plan: Telerik Reporting's PDF renderer uses GDI+ on Windows; Linux would need the Skia graphics engine.
resource plan 'Microsoft.Web/serverfarms@2023-12-01' = {
  name: names.plan
  location: location
  tags: tags
  sku: {
    name: appServiceSku
  }
  properties: {
    reserved: false
  }
}

var sqlConnectionString = 'Server=tcp:${sqlServer.properties.fullyQualifiedDomainName},1433;Database=${names.database};User ID=${sqlAdminLogin};Password=${sqlAdminPassword};Encrypt=True;TrustServerCertificate=False;Connection Timeout=60;'

resource site 'Microsoft.Web/sites@2023-12-01' = {
  name: names.site
  location: location
  tags: tags
  identity: {
    type: 'SystemAssigned'
  }
  properties: {
    serverFarmId: plan.id
    httpsOnly: true
    clientAffinityEnabled: false
    siteConfig: {
      netFrameworkVersion: 'v10.0'
      metadata: [
        {
          name: 'CURRENT_STACK'
          value: 'dotnet'
        }
      ]
      use32BitWorkerProcess: appServiceSku == 'F1'
      alwaysOn: appServiceSku != 'F1'
      http20Enabled: true
      minTlsVersion: '1.2'
      ftpsState: 'Disabled'
      healthCheckPath: '/health'
      appSettings: [
        {
          name: 'ASPNETCORE_ENVIRONMENT'
          value: 'Production'
        }
        {
          name: 'Database__MigrateOnStartup'
          value: 'true'
        }
        {
          name: 'Database__Seed'
          value: string(seedDemoData)
        }
        {
          name: 'Hangfire__ServerEnabled'
          value: 'true'
        }
        {
          name: 'OpenApi__Enabled'
          value: string(enableApiReference)
        }
        {
          name: 'Anthropic__ApiKey'
          value: anthropicApiKey
        }
      ]
      connectionStrings: [
        {
          name: 'LedgerFlow'
          type: 'SQLAzure'
          connectionString: sqlConnectionString
        }
      ]
    }
  }
}

output webAppName string = site.name
output webAppUrl string = 'https://${site.properties.defaultHostName}'
output sqlServerName string = sqlServer.name
output databaseName string = database.name
