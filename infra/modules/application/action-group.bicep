//=============================================================================
// Action Group
//=============================================================================

//=============================================================================
// Parameters
//=============================================================================

@description('The name of the action group to use')
param name string

@description('Email address to send alerts to')
param alertRecipientEmailAddress string

//=============================================================================
// Resources
//=============================================================================

resource actionGroup 'Microsoft.Insights/actionGroups@2023-01-01' = {
  name: name
  location: 'Global'
  properties: {
    enabled: true
    groupShortName: 'alerts'
    emailReceivers: alertRecipientEmailAddress == '' ? [] : [
      {      
        name: alertRecipientEmailAddress
        emailAddress: alertRecipientEmailAddress
        useCommonAlertSchema: true      
      }
    ]
  }
}

//=============================================================================
// Outputs
//=============================================================================

output id string = actionGroup.id
