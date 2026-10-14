"""Sample Temporal Python workflow fixture for the diagram extractor."""

from __future__ import annotations

import asyncio
from datetime import timedelta


class workflow:
    """Minimal Temporal workflow API stubs for fixtures."""

    @staticmethod
    def defn(fn=None, **kwargs):
        if fn is None:
            return lambda f: f
        return fn

    @staticmethod
    async def execute_activity(activity, *args, **kwargs):
        return None

    @staticmethod
    async def execute_child_workflow(child, *args, **kwargs):
        return None

    @staticmethod
    async def sleep(duration):
        return None

    @staticmethod
    async def wait_condition(fn):
        return None


async def charge_payment(order_id: str) -> None:
    return None


async def ship_order(order_id: str) -> None:
    return None


@workflow.defn
async def order_workflow(order_id: str) -> None:
    await workflow.execute_activity(charge_payment, order_id)
    await workflow.sleep(timedelta(seconds=5))
    await workflow.wait_condition(lambda: True)
    await workflow.execute_child_workflow("notify_customer_workflow", order_id)
    await asyncio.gather(
        workflow.execute_activity(ship_order, order_id),
        workflow.execute_activity(charge_payment, order_id),
    )
    await workflow.execute_activity(ship_order, order_id)
