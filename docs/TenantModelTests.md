# TenantModelTests
The `TenantModelTests` class is designed to test the functionality of the `TenantModel` class, ensuring that its methods behave as expected under various conditions. This class contains a set of test methods that cover different scenarios, including tenant activation, subscription validation, user limit checks, and status transitions.

## API
* `public void CanActivate_WhenActiveAndNotDeleted_ReturnsTrue`: Tests if a tenant can be activated when its status is active and it has not been deleted.
* `public void CanActivate_WhenDeleted_ReturnsFalse`: Tests if a tenant cannot be activated when it has been deleted.
* `public void CanActivate_WhenStatusIsArchived_ReturnsFalse`: Tests if a tenant cannot be activated when its status is archived.
* `public void CanActivate_WhenSubscriptionExpired_ReturnsFalse`: Tests if a tenant cannot be activated when its subscription has expired.
* `public void CanActivate_WhenSubscriptionExpiryIsNull_ReturnsTrue`: Tests if a tenant can be activated when its subscription expiry date is null.
* `public void IsUserLimitExceeded_WhenCurrentUsageAtExactLimit_ReturnsTrue`: Tests if the user limit is exceeded when the current usage is at the exact limit.
* `public void IsUserLimitExceeded_WhenCurrentUsageOneBelowLimit_ReturnsFalse`: Tests if the user limit is not exceeded when the current usage is one below the limit.
* `public void IsUserLimitExceeded_WhenMaxUsersIsNull_ReturnsFalse`: Tests if the user limit is not exceeded when the maximum number of users is null.
* `public void IsSubscriptionValid_WhenExpiryIsNull_ReturnsTrue`: Tests if a subscription is valid when its expiry date is null.
* `public void IsSubscriptionValid_WhenExpiredYesterday_ReturnsFalse`: Tests if a subscription is not valid when it expired yesterday.
* `public void Delete_SetsIsDeletedTrueAndArchivesStatus`: Tests if deleting a tenant sets its `IsDeleted` property to true and archives its status.
* `public void Restore_ClearsDeletedFlagAndSetsStatusToActive`: Tests if restoring a tenant clears its `IsDeleted` flag and sets its status to active.
* `public void Suspend_TransitionsStatusToSuspended`: Tests if suspending a tenant transitions its status to suspended.
* `public void IsInTrial_WhenStatusIsTrial_ReturnsTrue`: Tests if a tenant is in trial when its status is trial.
* `public void IsInTrial_WhenStatusIsActive_ReturnsFalse`: Tests if a tenant is not in trial when its status is active.

## Usage
The following examples demonstrate how to use the `TenantModelTests` class:
```csharp
// Example 1: Testing tenant activation
var tenantModel = new TenantModel { Status = TenantStatus.Active, IsDeleted = false };
var result = tenantModel.CanActivate();
Assert.IsTrue(result);

// Example 2: Testing subscription validation
var tenantModel = new TenantModel { SubscriptionExpiry = DateTime.Now.AddDays(1) };
var result = tenantModel.IsSubscriptionValid();
Assert.IsTrue(result);
```

## Notes
When using the `TenantModelTests` class, note that the test methods are designed to cover specific scenarios and may not be exhaustive. Additionally, the class does not handle concurrency or thread-safety explicitly, so caution should be exercised when using it in multi-threaded environments. The `IsUserLimitExceeded` and `IsSubscriptionValid` methods rely on the `MaxUsers` and `SubscriptionExpiry` properties being set correctly, respectively. The `Delete`, `Restore`, and `Suspend` methods modify the tenant's status and `IsDeleted` flag, which may have implications for downstream processing or storage.
