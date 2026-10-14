from __future__ import annotations

import json
import tempfile
import unittest
from pathlib import Path

from workflow_diagramming.extract import extract_workflows
from workflow_diagramming.write import write_diagrams

ROOT = Path(__file__).resolve().parents[1]
FIXTURES = ROOT / "examples" / "sample_workflows"


class ExtractTests(unittest.TestCase):
    def test_extract_order_workflow(self) -> None:
        workflows = extract_workflows(FIXTURES)
        self.assertGreaterEqual(len(workflows), 1)
        order = next(w for w in workflows if w.name == "order_workflow")
        labels = [s.label for s in order.steps]
        self.assertIn("charge_payment", labels)
        self.assertIn("Sleep", labels)
        self.assertIn("Wait condition", labels)
        self.assertIn("notify_customer_workflow", labels)
        self.assertIn("Parallel", labels)

    def test_write_metadata_package_name(self) -> None:
        with tempfile.TemporaryDirectory() as tmp:
            meta = write_diagrams(
                FIXTURES,
                tmp,
                package_name="sample-python-workflows",
                package_version="0.1.0",
                create_zip=True,
            )
            self.assertEqual(meta.packageName, "sample-python-workflows")
            self.assertEqual(meta.language, "Python")
            raw = Path(tmp, "workflow-diagrams-metadata.json").read_text(encoding="utf-8")
            self.assertNotIn("assemblyName", raw)
            data = json.loads(raw)
            self.assertEqual(data["packageName"], "sample-python-workflows")
            self.assertTrue(Path(tmp, "workflow-diagrams.zip").is_file())
            for w in data["workflows"]:
                mermaid = Path(tmp, w["diagramFile"]).read_text(encoding="utf-8")
                self.assertTrue(mermaid.startswith("flowchart TD"))
                self.assertIn("Start([Start])", mermaid)
                self.assertIn('End(["Complete"])', mermaid)


if __name__ == "__main__":
    unittest.main()
