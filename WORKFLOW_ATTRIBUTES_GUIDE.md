# Temporal Workflow Diagramming Attributes - Complete Guide

## Overview

This guide explains how to use the workflow diagramming attributes to automatically generate Mermaid diagrams from your Temporal workflow code. The attributes can be applied to workflow classes and methods to define the workflow structure visually.

---

## Required Namespaces

Add these `using` statements at the top of your workflow file:

```csharp
using Temporalio.Workflows;
using TemporalDashboard.WorkflowDiagramming.Attributes;
```

---

## Attribute Reference

### 1. WorkflowDiagramAttribute

**Purpose**: Marks the workflow class and provides diagram metadata.

**Usage**: Apply to the workflow class (alongside `[Workflow]`)

**Properties**:
- `DisplayName` (string, optional): Display name for the workflow
- `Description` (string, optional): Description of the workflow
- `Direction` (string, default: "TD"): Flowchart direction - "TD" (Top Down), "LR" (Left Right), "TB" (Top Bottom), "BT" (Bottom Top)

**Examples**:
```csharp
[Workflow]
[WorkflowDiagram]
public class MyWorkflow { }

[Workflow]
[WorkflowDiagram(DisplayName = "Customer Onboarding", Direction = "LR")]
public class CustomerOnboardingWorkflow { }
```

---

### 2. WorkflowStartAttribute

**Purpose**: Marks the start of the workflow (applied to the `[WorkflowRun]` method).

**Usage**: Apply to the workflow run method

**Properties**:
- `Label` (string, default: "Start"): Display label for the start node

**Examples**:
```csharp
[WorkflowRun]
[WorkflowStart]
public Task<string> RunAsync() { }

[WorkflowRun]
[WorkflowStart("Begin Process")]
public Task<string> RunAsync() { }
```

---

### 3. WorkflowStepAttribute

**Purpose**: Marks a workflow step/activity.

**Usage**: Apply to methods, properties, or classes (AllowMultiple = true)

**Required Parameters**:
- `id` (string): Unique identifier for this step (used in transitions)
- `label` (string): Display label in the diagram
- `order` (int, optional, default: 0): Execution order (lower numbers first)

**Optional Parameters**:
- `stepType` (WorkflowStepType, default: Activity): Type of step
- `description` (string): Optional description
- `isFailure` (bool): Marks as failure state
- `isSuccess` (bool): Marks as success state
- `isAiPowered` (bool): Indicates AI/ML capabilities (styled differently)

**WorkflowStepType Values**:
- `Activity` - Regular activity/operation (default)
- `Decision` - Decision point (diamond shape)
- `HumanApproval` - Human-in-the-loop approval
- `Start` - Start node
- `End` - End node

**Examples**:
```csharp
// Basic activity step
[WorkflowStep("step1", "Process Data", 1)]
public void ProcessData() { }

// AI-powered step
[WorkflowStep("ai-step", "Analyze with LLM", 2, IsAiPowered = true)]
public void AnalyzeWithAI() { }

// Step with description
[WorkflowStep("validate", "Validate Input", 1, Description = "Validates user input data")]
public void ValidateInput() { }

// Success end step
[WorkflowStep("success", "Complete", 100, StepType = WorkflowStepType.End, IsSuccess = true)]
public void Complete() { }
```

---

### 4. WorkflowDecisionAttribute

**Purpose**: Marks a decision point in the workflow (specialized step type).

**Usage**: Apply to methods

**Required Parameters**:
- `id` (string): Unique identifier for the decision
- `label` (string): Question/condition label
- `order` (int, optional, default: 0): Execution order

**Optional Properties**:
- `description` (string): Description of decision criteria

**Examples**:
```csharp
[WorkflowDecision("decision1", "Is Valid?", 2)]
public void CheckValidity() { }

[WorkflowDecision("decision1", "Is Valid?", 2, Description = "Checks if data meets validation criteria")]
public void CheckValidity() { }
```

---

### 5. WorkflowHumanApprovalAttribute

**Purpose**: Marks a human-in-the-loop approval step.

**Usage**: Apply to methods

**Required Parameters**:
- `id` (string): Unique identifier
- `label` (string): Display label
- `order` (int, optional, default: 0): Execution order

**Optional Properties**:
- `description` (string): What requires approval
- `timeout` (string): Timeout description (e.g., "24 hours")
- `approverRole` (string): Who needs to approve (e.g., "Manager")

**Examples**:
```csharp
[WorkflowHumanApproval("approval1", "Manager Approval", 3)]
public void ManagerApproval() { }

[WorkflowHumanApproval("approval1", "Manager Approval", 3, 
    Description = "Requires manager sign-off", 
    Timeout = "24 hours",
    ApproverRole = "Line Manager")]
public void ManagerApproval() { }
```

---

### 6. WorkflowEndAttribute

**Purpose**: Marks the end/completion of the workflow.

**Usage**: Apply to methods (typically return/end methods)

**Required Parameters**:
- `id` (string): Unique identifier
- `label` (string): Display label

**Optional Parameters**:
- `isSuccess` (bool): Marks as success completion
- `isFailure` (bool): Marks as failure completion

**Examples**:
```csharp
[WorkflowEnd("end", "Complete", true)]
public void Complete() { }

[WorkflowEnd("error", "Error", false)]
public void Error() { }

// Using constructor with isSuccess
[WorkflowEnd("end", "Success", true)]
public void SuccessEnd() { }
```

---

### 7. WorkflowTransitionAttribute

**Purpose**: Defines edges/transitions between workflow steps.

**Usage**: Apply to methods (AllowMultiple = true)

**Required Parameters**:
- `from` (string): Source step ID
- `to` (string): Target step ID

**Optional Properties**:
- `label` (string): Edge label (e.g., "Next", "Continue")
- `condition` (string): Condition description
- `isFailurePath` (bool): Marks as failure path
- `isSuccessPath` (bool): Marks as success path

**Examples**:
```csharp
// Simple transition
[WorkflowTransition("step1", "step2")]
public void Transitions() { }

// Transition with label
[WorkflowTransition("step1", "step2", "Continue")]
public void Transitions() { }

// Multiple transitions on one method
[WorkflowTransition("start", "step1")]
[WorkflowTransition("step1", "step2")]
[WorkflowTransition("step2", "end")]
public void Transitions() { }

// Success path
[WorkflowTransition("validate", "success", "Valid", IsSuccessPath = true)]
public void Transitions() { }
```

**Note**: Do NOT use transitions FROM decision nodes - use `WorkflowBranchAttribute` instead.

---

### 8. WorkflowBranchAttribute

**Purpose**: Defines branches from decision points (use instead of transitions for decisions).

**Usage**: Apply to methods (AllowMultiple = true)

**Required Parameters**:
- `decisionId` (string): The decision step ID this branch belongs to
- `label` (string): Branch label (e.g., "Yes", "No", "Approve", "Reject")
- `targetStepId` (string): Target step ID this branch leads to

**Optional Properties**:
- `isFailurePath` (bool): Marks as failure path
- `isSuccessPath` (bool): Marks as success path
- `isContinuePath` (bool): Marks as continue path

**Examples**:
```csharp
// Decision branches
[WorkflowBranch("decision1", "Yes", "step2")]
[WorkflowBranch("decision1", "No", "error-end", IsFailurePath = true)]
public void DecisionBranches() { }

// Approval branches
[WorkflowBranch("approval-decision", "Approve", "success-end", IsSuccessPath = true)]
[WorkflowBranch("approval-decision", "Reject", "failure-end", IsFailurePath = true)]
public void ApprovalBranches() { }
```

---

## Complete Example Workflow

Here's a complete example showing all attribute types:

```csharp
using Temporalio.Workflows;
using TemporalDashboard.WorkflowDiagramming.Attributes;

[Workflow]
[WorkflowDiagram(DisplayName = "Customer Onboarding", Direction = "TD")]
public class CustomerOnboardingWorkflow
{
    [WorkflowRun]
    [WorkflowStart("Begin Onboarding")]
    public async Task<OnboardingResult> RunAsync(OnboardingRequest request)
    {
        await ValidateRequest();
        var isValid = await CheckEligibility();
        if (isValid)
        {
            await PerformKyc();
            await RequestApproval();
            return new OnboardingResult { Success = true };
        }
        return new OnboardingResult { Success = false };
    }

    // Step 1: Validation
    [WorkflowStep("validate", "Validate Request", 1)]
    [WorkflowTransition("Start", "validate")]
    private async Task ValidateRequest() { }

    // Step 2: Decision
    [WorkflowDecision("eligibility-check", "Is Eligible?", 2)]
    [WorkflowTransition("validate", "eligibility-check")]
    private async Task<bool> CheckEligibility() { return true; }

    // Decision branches
    [WorkflowBranch("eligibility-check", "Yes", "kyc")]
    [WorkflowBranch("eligibility-check", "No", "rejected", IsFailurePath = true)]
    private void EligibilityBranches() { }

    // Step 3: KYC (AI-powered)
    [WorkflowStep("kyc", "KYC Verification", 3, IsAiPowered = true)]
    [WorkflowTransition("kyc", "approval")]
    private async Task PerformKyc() { }

    // Step 4: Human approval
    [WorkflowHumanApproval("approval", "Manager Approval", 4, 
        Description = "Requires manager sign-off",
        Timeout = "24 hours",
        ApproverRole = "Line Manager")]
    [WorkflowTransition("approval", "approved")]
    private async Task RequestApproval() { }

    // Success end
    [WorkflowEnd("approved", "Approved", true)]
    private void Approved() { }

    // Failure end
    [WorkflowEnd("rejected", "Rejected", false)]
    private void Rejected() { }
}
```

---

## Step-by-Step Decoration Instructions

### For Cursor/AI Code Generation:

When asked to decorate a workflow with diagramming attributes, follow these steps:

1. **Add Required Namespaces**:
   ```csharp
   using Temporalio.Workflows;
   using TemporalDashboard.WorkflowDiagramming.Attributes;
   ```

2. **Decorate the Workflow Class**:
   ```csharp
   [Workflow]
   [WorkflowDiagram(DisplayName = "Workflow Name", Direction = "TD")]
   public class YourWorkflow { }
   ```

3. **Decorate the Run Method**:
   ```csharp
   [WorkflowRun]
   [WorkflowStart("Start Label")]
   public Task<ReturnType> RunAsync(Parameters) { }
   ```

4. **For Each Workflow Step**:
   - Identify the step type (Activity, Decision, HumanApproval, etc.)
   - Add appropriate attribute with unique ID, label, and order
   - Example: `[WorkflowStep("step-id", "Step Label", orderNumber)]`

5. **Add Transitions**:
   - Create a method (or use existing) to hold transition attributes
   - Add `[WorkflowTransition("from-id", "to-id")]` for each transition
   - Use `[WorkflowTransition("from-id", "to-id", "label")]` for labeled edges

6. **For Decision Points**:
   - Use `[WorkflowDecision("decision-id", "Question?", order)]` on the decision method
   - Use `[WorkflowBranch("decision-id", "Branch Label", "target-id")]` for each branch
   - Do NOT use transitions from decision nodes

7. **Add End Points**:
   - Use `[WorkflowEnd("end-id", "End Label", isSuccess: true/false)]` on end methods

8. **Special Cases**:
   - AI-powered steps: Add `IsAiPowered = true` to `[WorkflowStep]`
   - Human approvals: Use `[WorkflowHumanApproval]` instead of `[WorkflowStep]`
   - Success/Failure paths: Add `IsSuccessPath = true` or `IsFailurePath = true`

---

## Common Patterns

### Pattern 1: Simple Linear Workflow
```csharp
[Workflow]
[WorkflowDiagram]
public class LinearWorkflow
{
    [WorkflowRun]
    [WorkflowStart]
    public Task<string> RunAsync() { }

    [WorkflowStep("step1", "Step 1", 1)]
    [WorkflowStep("step2", "Step 2", 2)]
    [WorkflowStep("step3", "Step 3", 3)]
    
    [WorkflowTransition("Start", "step1")]
    [WorkflowTransition("step1", "step2")]
    [WorkflowTransition("step2", "step3")]
    [WorkflowTransition("step3", "end")]
    public void Transitions() { }

    [WorkflowEnd("end", "Complete", true)]
    public void End() { }
}
```

### Pattern 2: Workflow with Decision
```csharp
[Workflow]
[WorkflowDiagram]
public class DecisionWorkflow
{
    [WorkflowRun]
    [WorkflowStart]
    public Task<string> RunAsync() { }

    [WorkflowStep("step1", "Initial Step", 1)]
    [WorkflowDecision("decision1", "Is Valid?", 2)]
    [WorkflowStep("step2", "Process Valid", 3)]

    [WorkflowTransition("Start", "step1")]
    [WorkflowTransition("step1", "decision1")]

    [WorkflowBranch("decision1", "Yes", "step2")]
    [WorkflowBranch("decision1", "No", "error", IsFailurePath = true)]

    [WorkflowEnd("end", "Success", true)]
    [WorkflowEnd("error", "Error", false)]
    public void End() { }
}
```

### Pattern 3: Workflow with Parallel Execution
```csharp
[Workflow]
[WorkflowDiagram]
public class ParallelWorkflow
{
    [WorkflowRun]
    [WorkflowStart]
    public Task<string> RunAsync() { }

    [WorkflowStep("step1", "Initial", 1)]
    [WorkflowStep("step2a", "Parallel A", 2)]
    [WorkflowStep("step2b", "Parallel B", 2)]
    [WorkflowStep("step3", "Merge", 3)]

    [WorkflowTransition("Start", "step1")]
    [WorkflowTransition("step1", "step2a")]
    [WorkflowTransition("step1", "step2b")]
    [WorkflowTransition("step2a", "step3")]
    [WorkflowTransition("step2b", "step3")]
    [WorkflowTransition("step3", "end")]

    [WorkflowEnd("end", "Complete", true)]
    public void End() { }
}
```

---

## Best Practices

1. **Use Unique IDs**: Each step must have a unique ID across the workflow
2. **Order Matters**: Use sequential order numbers (1, 2, 3...) for clarity
3. **Start Node**: Always use "Start" as the ID for the start node in transitions
4. **Decision Branches**: Always use `WorkflowBranchAttribute` for decisions, never `WorkflowTransitionAttribute`
5. **End Nodes**: Use descriptive IDs like "success-end", "failure-end", "complete"
6. **Group Transitions**: Place all transition attributes on a single method for organization
7. **Descriptive Labels**: Use clear, descriptive labels that explain what each step does
8. **AI-Powered Steps**: Mark AI/ML steps with `IsAiPowered = true` for visual distinction

---

## Quick Reference Checklist

When decorating a workflow, ensure you have:

- [ ] Added `using TemporalDashboard.WorkflowDiagramming.Attributes;`
- [ ] Applied `[WorkflowDiagram]` to the workflow class
- [ ] Applied `[WorkflowStart]` to the `[WorkflowRun]` method
- [ ] Applied `[WorkflowStep]` or specialized attributes to each workflow step
- [ ] Added `[WorkflowTransition]` attributes for all step-to-step connections
- [ ] Used `[WorkflowBranch]` for decision point branches (not transitions)
- [ ] Applied `[WorkflowEnd]` to end/completion methods
- [ ] Used unique IDs for all steps
- [ ] Set appropriate order numbers for steps
- [ ] Marked success/failure paths where applicable

---

## Troubleshooting

**Problem**: Diagram not generating correctly
- **Solution**: Ensure all step IDs in transitions/branches match the IDs in step attributes

**Problem**: Decision branches not showing
- **Solution**: Use `[WorkflowBranch]` instead of `[WorkflowTransition]` for decision nodes

**Problem**: Steps appearing in wrong order
- **Solution**: Check the `Order` property values - lower numbers appear first

**Problem**: Transitions missing
- **Solution**: Ensure transition attributes reference valid step IDs (case-sensitive)

**Problem**: "Start" node not found
- **Solution**: The start node ID in transitions must be exactly "Start" (capital S)

---

## Notes for AI Code Generation

When generating workflow code with these attributes:

1. **Always include both namespaces** at the top
2. **Decorate the class first** with `[WorkflowDiagram]`
3. **Decorate the run method** with `[WorkflowStart]`
4. **For each logical step**, add the appropriate attribute with:
   - A unique, descriptive ID (kebab-case recommended: "validate-request")
   - A clear label (human-readable: "Validate Request")
   - An order number (sequential: 1, 2, 3...)
5. **Create transitions** by identifying the flow between steps
6. **For conditionals/decisions**, use `[WorkflowDecision]` and `[WorkflowBranch]`
7. **Mark end points** with `[WorkflowEnd]` and appropriate success/failure flags
8. **Group related attributes** - transitions can be on a separate method for clarity

The generator will automatically create Mermaid diagrams from these attributes when the workflow DLL is uploaded to the dashboard.
