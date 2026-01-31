# Workflow Diagram Attributes

This directory contains C# attributes that can be applied to workflow code to automatically generate Mermaid diagrams without needing to parse IL code.

## Available Attributes

### Workflow-Level Attributes

#### `[WorkflowDiagram]`
Applied to the workflow class to provide metadata:
```csharp
[Workflow]
[WorkflowDiagram(DisplayName = "Customer Onboarding", Direction = "TD")]
public class CustomerOnboardingWorkflow
{
    // ...
}
```

### Step-Level Attributes

#### `[WorkflowStep]`
Marks a workflow step/activity:
```csharp
[WorkflowStep("kyc-verification", "KYC Verification", WorkflowStepType.Activity, Order = 1)]
public void PerformKycVerification() { }
```

#### `[WorkflowDecision]`
Marks a decision point:
```csharp
[WorkflowDecision("kyc-decision", "KYC Passed?", Order = 2)]
public void CheckKycResult() { }
```

#### `[WorkflowHumanApproval]`
Marks a human-in-the-loop approval step:
```csharp
[WorkflowHumanApproval("manager-approval", "Manager Approval", Order = 5, ApproverRole = "Line Manager")]
public void WaitForApproval() { }
```

#### `[WorkflowStart]`
Marks the start of the workflow:
```csharp
[WorkflowRun]
[WorkflowStart("Start Onboarding")]
public async Task<OnboardingResult> RunAsync(OnboardingRequest request)
{
    // ...
}
```

#### `[WorkflowEnd]`
Marks the end of the workflow:
```csharp
[WorkflowEnd("onboarding-complete", "Onboarding Complete", IsSuccess = true)]
public OnboardingResult Complete() { }
```

### Transition Attributes

#### `[WorkflowTransition]`
Defines a transition between steps:
```csharp
[WorkflowTransition("kyc-verification", "kyc-decision")]
public void TransitionFromKycToDecision() { }
```

#### `[WorkflowBranch]`
Defines a branch from a decision:
```csharp
[WorkflowBranch("kyc-decision", "Yes", "aml-screening", IsContinuePath = true)]
[WorkflowBranch("kyc-decision", "No", "kyc-failure", IsFailurePath = true)]
public void HandleKycDecision() { }
```

## Usage Patterns

### Pattern 1: Helper Methods with Attributes

Create helper methods that represent workflow steps and apply attributes to them:

```csharp
[Workflow]
[WorkflowDiagram(DisplayName = "Customer Onboarding")]
public class CustomerOnboardingWorkflow
{
    [WorkflowRun]
    [WorkflowStart("Start Onboarding")]
    public async Task<OnboardingResult> RunAsync(OnboardingRequest request)
    {
        await PerformKycVerification();
        var kycResult = await CheckKycResult();
        if (!kycResult) return HandleKycFailure();
        
        await PerformAmlScreening();
        // ... rest of workflow
    }

    [WorkflowStep("kyc-verification", "KYC Verification", WorkflowStepType.Activity, Order = 1)]
    private async Task PerformKycVerification()
    {
        // Implementation
    }

    [WorkflowDecision("kyc-decision", "KYC Passed?", Order = 2)]
    private bool CheckKycResult()
    {
        // Implementation
    }

    [WorkflowEnd("kyc-failure", "KYC Failed", IsFailure = true)]
    private OnboardingResult HandleKycFailure()
    {
        // Implementation
    }
}
```

### Pattern 2: Workflow Configuration Class

Create a separate configuration class with attributes:

```csharp
[WorkflowDiagram(DisplayName = "Customer Onboarding")]
public static class CustomerOnboardingWorkflowSteps
{
    [WorkflowStep("kyc-verification", "KYC Verification", WorkflowStepType.Activity, Order = 1)]
    [WorkflowTransition("Start", "kyc-verification")]
    public static void KycVerification() { }

    [WorkflowDecision("kyc-decision", "KYC Passed?", Order = 2)]
    [WorkflowTransition("kyc-verification", "kyc-decision")]
    [WorkflowBranch("kyc-decision", "Yes", "aml-screening", IsContinuePath = true)]
    [WorkflowBranch("kyc-decision", "No", "kyc-failure", IsFailurePath = true)]
    public static void KycDecision() { }

    [WorkflowStep("aml-screening", "AML Screening", WorkflowStepType.Activity, Order = 3)]
    [WorkflowTransition("kyc-decision", "aml-screening")]
    public static void AmlScreening() { }

    [WorkflowHumanApproval("manager-approval", "Line Manager Approval", Order = 5)]
    [WorkflowTransition("aml-screening", "manager-approval")]
    public static void ManagerApproval() { }

    [WorkflowEnd("onboarding-complete", "Onboarding Complete", IsSuccess = true)]
    [WorkflowTransition("manager-approval", "onboarding-complete")]
    public static void Complete() { }
}
```

### Pattern 3: Attributes on Properties

You can also use properties to represent workflow steps:

```csharp
[Workflow]
public class CustomerOnboardingWorkflow
{
    [WorkflowStep("kyc-verification", "KYC Verification", WorkflowStepType.Activity, Order = 1)]
    private static readonly object KycVerificationStep = new();

    [WorkflowDecision("kyc-decision", "KYC Passed?", Order = 2)]
    private static readonly object KycDecisionStep = new();
}
```

## Best Practices

1. **Use consistent IDs**: Use kebab-case for step IDs (e.g., `"kyc-verification"`)
2. **Set Order explicitly**: Always set the `Order` property to ensure correct sequencing
3. **Define all transitions**: Use `[WorkflowTransition]` or `[WorkflowBranch]` to explicitly define all edges
4. **Use descriptive labels**: Labels should be human-readable and clear
5. **Mark failure/success paths**: Use `IsFailurePath` and `IsSuccessPath` for proper styling

## Example: Complete Workflow

```csharp
using TemporalPoC.Workflows.Attributes;
using Temporalio.Workflows;

[Workflow]
[WorkflowDiagram(DisplayName = "Customer Onboarding", Direction = "TD")]
public class CustomerOnboardingWorkflow
{
    [WorkflowRun]
    [WorkflowStart("Start Onboarding")]
    public async Task<OnboardingResult> RunAsync(OnboardingRequest request)
    {
        // Step 1: KYC Verification
        await PerformKycVerification();
        var kycResult = await CheckKycResult();
        if (!kycResult) return HandleKycFailure();

        // Step 2: AML Screening
        await PerformAmlScreening();
        var amlResult = await CheckAmlResult();
        if (!amlResult) return HandleAmlFailure();

        // Step 3: Credit Check
        await PerformCreditCheck();
        var creditResult = await CheckCreditResult();
        if (!creditResult) return HandleCreditFailure();

        // Step 4: Human Approval
        await WaitForManagerApproval();
        var approved = await CheckApprovalStatus();
        if (!approved) return HandleRejection();

        // Step 5: Create Profile
        await CreateCustomerProfile();
        
        return Complete();
    }

    [WorkflowStep("kyc-verification", "KYC Verification", WorkflowStepType.Activity, Order = 1)]
    [WorkflowTransition("Start", "kyc-verification")]
    private async Task PerformKycVerification() { }

    [WorkflowDecision("kyc-decision", "KYC Passed?", Order = 2)]
    [WorkflowTransition("kyc-verification", "kyc-decision")]
    [WorkflowBranch("kyc-decision", "Yes", "aml-screening", IsContinuePath = true)]
    [WorkflowBranch("kyc-decision", "No", "kyc-failure", IsFailurePath = true)]
    private async Task<bool> CheckKycResult() { return true; }

    [WorkflowEnd("kyc-failure", "KYC Failed", IsFailure = true)]
    private OnboardingResult HandleKycFailure() { return new(); }

    [WorkflowStep("aml-screening", "AML Screening", WorkflowStepType.Activity, Order = 3)]
    [WorkflowTransition("kyc-decision", "aml-screening")]
    private async Task PerformAmlScreening() { }

    [WorkflowDecision("aml-decision", "AML Passed?", Order = 4)]
    [WorkflowTransition("aml-screening", "aml-decision")]
    [WorkflowBranch("aml-decision", "Yes", "credit-check", IsContinuePath = true)]
    [WorkflowBranch("aml-decision", "No", "aml-failure", IsFailurePath = true)]
    private async Task<bool> CheckAmlResult() { return true; }

    [WorkflowEnd("aml-failure", "AML Failed", IsFailure = true)]
    private OnboardingResult HandleAmlFailure() { return new(); }

    [WorkflowStep("credit-check", "Credit Check", WorkflowStepType.Activity, Order = 5)]
    [WorkflowTransition("aml-decision", "credit-check")]
    private async Task PerformCreditCheck() { }

    [WorkflowDecision("credit-decision", "Credit Passed?", Order = 6)]
    [WorkflowTransition("credit-check", "credit-decision")]
    [WorkflowBranch("credit-decision", "Yes", "manager-approval", IsContinuePath = true)]
    [WorkflowBranch("credit-decision", "No", "credit-failure", IsFailurePath = true)]
    private async Task<bool> CheckCreditResult() { return true; }

    [WorkflowEnd("credit-failure", "Credit Failed", IsFailure = true)]
    private OnboardingResult HandleCreditFailure() { return new(); }

    [WorkflowHumanApproval("manager-approval", "Line Manager Approval", Order = 7, ApproverRole = "Line Manager")]
    [WorkflowTransition("credit-decision", "manager-approval")]
    private async Task WaitForManagerApproval() { }

    [WorkflowDecision("approval-decision", "Approved?", Order = 8)]
    [WorkflowTransition("manager-approval", "approval-decision")]
    [WorkflowBranch("approval-decision", "Yes", "create-profile", IsContinuePath = true)]
    [WorkflowBranch("approval-decision", "No", "rejection", IsFailurePath = true)]
    [WorkflowBranch("approval-decision", "Timeout", "timeout", IsFailurePath = true)]
    private async Task<bool> CheckApprovalStatus() { return true; }

    [WorkflowEnd("rejection", "Rejected", IsFailure = true)]
    private OnboardingResult HandleRejection() { return new(); }

    [WorkflowEnd("timeout", "Approval Timeout", IsFailure = true)]
    private OnboardingResult HandleTimeout() { return new(); }

    [WorkflowStep("create-profile", "Create Customer Profile", WorkflowStepType.Activity, Order = 9)]
    [WorkflowTransition("approval-decision", "create-profile")]
    private async Task CreateCustomerProfile() { }

    [WorkflowEnd("onboarding-complete", "Onboarding Complete", IsSuccess = true)]
    [WorkflowTransition("create-profile", "onboarding-complete")]
    private OnboardingResult Complete() { return new(); }
}
```

## Notes

- Attributes are read at compile-time and used by `WorkflowDiagramGenerator` to generate Mermaid diagrams
- The generator uses reflection to find all attributes and builds the diagram structure
- You don't need to implement the methods - they're just markers for diagram generation
- Attributes can be applied to methods, properties, or classes depending on your pattern preference

