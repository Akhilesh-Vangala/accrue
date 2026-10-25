using 'main.bicep'

param appName = 'accrue'
param environment = 'dev'
param sqlAdminLogin = 'accrueadmin'
param sqlAdminPassword = readEnvironmentVariable('ACCRUE_SQL_PASSWORD')
param useSqlFreeOffer = true
param appServiceSku = 'B1'
param seedDemoData = true
param enableApiReference = true
param anthropicApiKey = readEnvironmentVariable('ANTHROPIC_API_KEY', '')
