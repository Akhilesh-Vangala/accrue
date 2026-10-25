using 'main.bicep'

param appName = 'ledgerflow'
param environment = 'dev'
param sqlAdminLogin = 'ledgerflowadmin'
param sqlAdminPassword = readEnvironmentVariable('LEDGERFLOW_SQL_PASSWORD')
param useSqlFreeOffer = true
param appServiceSku = 'B1'
param seedDemoData = true
param enableApiReference = true
param anthropicApiKey = readEnvironmentVariable('ANTHROPIC_API_KEY', '')
