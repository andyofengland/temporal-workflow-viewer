package com.example.workflows

/**
 * Sample Temporal Kotlin workflow fixture for the diagram extractor.
 */
class OrderWorkflow : OrderWorkflowInterface {

    private val activities = Workflow.newActivityStub(OrderActivities::class.java)

    @WorkflowMethod
    override fun fulfillOrder(orderId: String) {
        activities.chargePayment(orderId)
        Workflow.sleep(java.time.Duration.ofSeconds(5))
        Workflow.await { true }
        val child = Workflow.newChildWorkflowStub(NotifyCustomerWorkflow::class.java)
        child.notify(orderId)
        Async.function(activities::shipOrder, orderId)
        activities.shipOrder(orderId)
    }
}

interface OrderWorkflowInterface {
    fun fulfillOrder(orderId: String)
}

interface OrderActivities {
    fun chargePayment(orderId: String)
    fun shipOrder(orderId: String)
}

interface NotifyCustomerWorkflow {
    fun notify(orderId: String)
}

object Workflow {
    fun <T> newActivityStub(cls: Class<T>): T = null as T
    fun <T> newChildWorkflowStub(cls: Class<T>): T = null as T
    fun sleep(d: java.time.Duration) {}
    fun await(c: () -> Boolean) {}
}

object Async {
    fun <T> function(f: (T) -> Any?, arg: T) {}
}

annotation class WorkflowMethod
