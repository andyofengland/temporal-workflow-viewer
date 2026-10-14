package sampleworkflows

import "time"

// Minimal Temporal-like API stubs so the fixture parses without the SDK.
type Context interface{}

var workflow = struct {
	ExecuteActivity      func(ctx Context, activity interface{}, args ...interface{}) Future
	ExecuteChildWorkflow func(ctx Context, child interface{}, args ...interface{}) Future
	Sleep                func(ctx Context, d time.Duration) error
	Await                func(ctx Context, condition func() bool) error
	Go                   func(ctx Context, f func(Context))
}{}

type Future interface {
	Get(ctx Context, valuePtr interface{}) error
}

func ChargePayment(ctx Context, orderID string) error { return nil }
func ShipOrder(ctx Context, orderID string) error     { return nil }

// OrderWorkflow is a sample Temporal Go workflow fixture.
func OrderWorkflow(ctx Context, orderID string) error {
	if err := workflow.ExecuteActivity(ctx, ChargePayment, orderID).Get(ctx, nil); err != nil {
		return err
	}
	if err := workflow.Sleep(ctx, 5*time.Second); err != nil {
		return err
	}
	if err := workflow.Await(ctx, func() bool { return true }); err != nil {
		return err
	}
	if err := workflow.ExecuteChildWorkflow(ctx, "NotifyCustomerWorkflow", orderID).Get(ctx, nil); err != nil {
		return err
	}
	workflow.Go(ctx, func(ctx Context) {
		_ = workflow.ExecuteActivity(ctx, ShipOrder, orderID).Get(ctx, nil)
	})
	return workflow.ExecuteActivity(ctx, ShipOrder, orderID).Get(ctx, nil)
}
