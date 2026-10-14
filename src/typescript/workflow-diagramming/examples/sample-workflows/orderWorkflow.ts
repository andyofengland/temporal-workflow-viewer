/**
 * Sample Temporal TypeScript workflow fixture for the diagram extractor.
 * Uses common SDK patterns without requiring @temporalio/workflow at extract time.
 */

export async function chargePayment(orderId: string): Promise<void> {
  // activity stub — declared via proxyActivities below in the workflow module
  void orderId;
}

export async function shipOrder(orderId: string): Promise<void> {
  void orderId;
}

// Mimic Temporal proxyActivities binding (extractor reads the destructuring names).
declare function proxyActivities<T>(opts: unknown): T;
declare function sleep(ms: string | number): Promise<void>;
declare function condition(fn: () => boolean): Promise<void>;
declare const workflow: {
  executeChild: (name: string, opts?: unknown) => Promise<unknown>;
  promise: { all: <T>(ps: Promise<T>[]) => Promise<T[]> };
};

const { chargePayment: charge, shipOrder: ship } = proxyActivities<{
  chargePayment: (orderId: string) => Promise<void>;
  shipOrder: (orderId: string) => Promise<void>;
}>({ startToCloseTimeout: "1 minute" });

/** Order fulfillment workflow — linear activities + wait + child. */
export async function orderWorkflow(orderId: string): Promise<void> {
  await charge(orderId);
  await sleep("5s");
  await condition(() => true);
  await workflow.executeChild("notifyCustomerWorkflow", { args: [orderId] });
  await ship(orderId);
  await Promise.all([charge(orderId), ship(orderId)]);
}
