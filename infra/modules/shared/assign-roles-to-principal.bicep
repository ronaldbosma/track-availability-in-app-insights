//=============================================================================
// Assign roles to principal on Key Vault, Service Bus, Storage Account and
// Event Hubs namespace
//=============================================================================
//=============================================================================
// Parameters
//=============================================================================

@description('The id of the principal that will be assigned the roles')
param principalId string

@description('The flag to determine if the principal is an admin or not')
param isAdmin bool = false

@description('The name of the Key Vault on which to assign roles')
param keyVaultName string

@description('The name of the Storage Account on which to assign roles')
param storageAccountName string = ''

//=============================================================================
// Variables
//=============================================================================

var keyVaultRole = isAdmin 
  ? '00482a5a-887f-4fb3-b363-3b7fe8e74483'    // Key Vault Administrator
  : '4633458b-17de-408a-b874-0445c86b69e6'    // Key Vault Secrets User

var storageAccountRoles = [
  'ba92f5b4-2d11-453d-a403-e96b0029c9fe'      // Storage Blob Data Contributor
  isAdmin 
    ? '69566ab7-960f-475b-8e7c-b3118f30c6bd'  // Storage File Data Privileged Contributor (is able to browse file shares in Azure Portal)
    : '0c867c2a-1d8c-454a-a3db-ab2ea1bdc8bb'  // Storage File Data SMB Share Contributor
  '974c5e8b-45b9-4653-ba55-5f855dd0fb88'      // Storage Queue Data Contributor
  '0a9a7e1f-b9d0-4cc4-a60d-0319b160aaa3'      // Storage Table Data Contributor
]


//=============================================================================
// Existing Resources
//=============================================================================

resource keyVault 'Microsoft.KeyVault/vaults@2023-07-01' existing = {
  name: keyVaultName
}

resource storageAccount 'Microsoft.Storage/storageAccounts@2023-05-01' existing = if (storageAccountName != '') {
  name: storageAccountName
}

//=============================================================================
// Resources
//=============================================================================

// Assign role on Key Vault to the principal

resource assignRolesOnKeyVaultToManagedIdentity 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(principalId, keyVault.id, subscriptionResourceId('Microsoft.Authorization/roleDefinitions', keyVaultRole))
  scope: keyVault
  properties: {
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', keyVaultRole)
    principalId: principalId
  }
}

// Assign roles on Storage Account to the principal

resource assignRolesOnStorageAccountToManagedIdentity 'Microsoft.Authorization/roleAssignments@2022-04-01' = [for role in storageAccountRoles: if (storageAccountName != '') {
  name: guid(principalId, storageAccount.id, subscriptionResourceId('Microsoft.Authorization/roleDefinitions', role))
  scope: storageAccount
  properties: {
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', role)
    principalId: principalId
  }
}]
