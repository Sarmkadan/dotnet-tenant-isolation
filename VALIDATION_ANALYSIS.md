# TenantEvent Validation Analysis

## Current State Analysis

### Event Classes (10 total):
1. TenantCreatedEvent
2. TenantActivatedEvent
3. TenantSuspendedEvent
4. TenantDeactivatedEvent
5. TenantReactivatedEvent
6. TenantDeletedEvent
7. TenantConfigurationChangedEvent
8. UserAddedToTenantEvent
9. DataIsolationPolicyChangedEvent
10. FeatureToggledEvent
11. TenantResourceAccessedEvent
12. TenantSubscriptionUpdatedEvent

Note: TenantResourceAccessedEvent and TenantSubscriptionUpdatedEvent are also part of the 10 mentioned.

## Validation Patterns Found

### TenantId Property
- **Location**: Base class `TenantEvent` (line 63)
- **Type**: `Guid` (non-nullable)
- **Setter**: `protected set`
- **Current Validation**: NONE - no validation in base or subclasses
- **Risk**: Can be set to `Guid.Empty` which is invalid

### String Property Validation Patterns

#### Pattern 1: ArgumentException.ThrowIfNullOrEmpty (Required fields)
- **Classes**: TenantCreatedEvent (TenantName, TenantSlug, AdminEmail, IsolationStrategy)
- **Classes**: UserAddedToTenantEvent (NewUserId, UserEmail, Role)
- **Classes**: DataIsolationPolicyChangedEvent (PolicyType)
- **Classes**: FeatureToggledEvent (FeatureName)
- **Classes**: TenantResourceAccessedEvent (ResourceType, ResourceId, Action)
- **Classes**: TenantSubscriptionUpdatedEvent (SubscriptionPlan)
- **Validation**: ✅ Proper null/empty checks
- **Length Check**: ✅ Max length validation (255 chars)
- **Additional Checks**: TenantResourceAccessedEvent has path traversal validation

#### Pattern 2: Length-only validation (Optional nullable fields)
- **Classes**: TenantSuspendedEvent (SuspensionReason)
- **Classes**: TenantDeactivatedEvent (DeactivationReason)
- **Classes**: TenantReactivatedEvent (ReactivationReason)
- **Classes**: TenantDeletedEvent (DeletionReason)
- **Validation**: ❌ No null check - can be null
- **Length Check**: ✅ Max length validation (255 chars)

#### Pattern 3: Dictionary validation
- **Classes**: TenantConfigurationChangedEvent (ChangedProperties)
- **Validation**: ✅ ArgumentNullException for null
- **Length Check**: ✅ Key length validation (255 chars)

#### Pattern 4: No string properties
- **Classes**: TenantActivatedEvent
- **Validation**: ✅ N/A - no string properties to validate

## Issues Identified

### 1. TenantId Validation Missing
- **Problem**: TenantId can be set to Guid.Empty or any invalid value
- **Impact**: Events can be created without a valid tenant association
- **Severity**: HIGH - breaks data integrity

### 2. Inconsistent Null Handling for Reason Fields
- **Problem**: Some event types allow null reason fields while others don't
- **Impact**: Inconsistent API behavior
- **Severity**: MEDIUM - affects usability

### 3. No Centralized Validation
- **Problem**: Validation logic duplicated across multiple classes
- **Impact**: Maintenance burden, potential inconsistencies
- **Severity**: LOW - but could be improved

## Recommended Fixes

### 1. Add TenantId Validation to Base Class
- Add validation in TenantEvent constructor or property setter
- Throw ArgumentException for Guid.Empty
- Document in XML comments

### 2. Unify Null Handling for Reason Fields
- Decide: Should reason fields allow null?
- If yes: Add null checks everywhere
- If no: Remove null checks everywhere and make properties required
- **Recommendation**: Allow null (reason is optional context)

### 3. Centralize Common Validation Logic
- Add validation methods in base class
- Use consistent exception types
- Document all validation rules

### 4. Add XML Documentation
- Document all validation rules in XML comments
- Include <exception> tags for all validation throws
- Document default values and behaviors

## Exception Type Consistency

**Current State**: Mix of ArgumentException and ArgumentNullException

**Recommended**: 
- Use `ArgumentNullException.ThrowIfNull()` for null checks
- Use `ArgumentException.ThrowIfNullOrEmpty()` for null/empty checks  
- Use `ArgumentException` for other validation (length, format, etc.)
- Be consistent across all 10 event classes

## Implementation Plan

1. Add TenantId validation to TenantEvent base class
2. Update all event classes to use consistent validation patterns
3. Add comprehensive XML documentation
4. Ensure all validation throws appropriate exception types
5. Update tests to verify new validation rules