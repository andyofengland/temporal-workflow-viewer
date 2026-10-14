package com.example.workflows;

/**
 * Sample Temporal Java workflow fixture for the diagram extractor.
 * Uses SDK-like patterns without requiring the Temporal dependency at extract time.
 */
public class OrderWorkflow implements OrderWorkflowInterface {

    private final OrderActivities activities =
            Workflow.newActivityStub(OrderActivities.class);

    @WorkflowMethod
    public void fulfillOrder(String orderId) {
        activities.chargePayment(orderId);
        Workflow.sleep(java.time.Duration.ofSeconds(5));
        Workflow.await(() -> true);
        OrderWorkflowInterface child = Workflow.newChildWorkflowStub(NotifyCustomerWorkflow.class);
        child.fulfillOrder(orderId);
        Async.function(activities::shipOrder, orderId);
        activities.shipOrder(orderId);
    }
}

interface OrderWorkflowInterface {
    void fulfillOrder(String orderId);
}

interface OrderActivities {
    void chargePayment(String orderId);
    void shipOrder(String orderId);
}

interface NotifyCustomerWorkflow {
}

/** Minimal stubs matching Temporal class/method names the extractor looks for. */
class Workflow {
    static <T> T newActivityStub(Class<T> cls) { return null; }
    static <T> T newChildWorkflowStub(Class<T> cls) { return null; }
    static void sleep(java.time.Duration d) {}
    static void await(java.util.function.Supplier<Boolean> c) {}
}

class Async {
    static <T> void function(java.util.function.Function<T, ?> f, T arg) {}
    static void function(Runnable r) {}
}

@interface WorkflowMethod {}
