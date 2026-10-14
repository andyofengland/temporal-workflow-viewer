import assert from "node:assert/strict";
import fs from "node:fs";
import os from "node:os";
import path from "node:path";
import { describe, it } from "node:test";
import { fileURLToPath } from "node:url";
import { extractWorkflows } from "../src/extract.js";
import { writeDiagrams } from "../src/write.js";

const root = path.join(path.dirname(fileURLToPath(import.meta.url)), "..");
const fixtures = path.join(root, "examples", "sample-workflows");

describe("typescript workflow diagramming", () => {
  it("extracts orderWorkflow steps from fixtures", () => {
    const workflows = extractWorkflows(fixtures);
    assert.ok(workflows.length >= 1);
    const order = workflows.find((w) => w.name === "orderWorkflow");
    assert.ok(order, "expected orderWorkflow");
    const labels = order!.steps.map((s) => s.label);
    assert.ok(labels.includes("charge"));
    assert.ok(labels.some((l) => l.startsWith("Sleep")));
    assert.ok(labels.includes("Wait condition"));
    assert.ok(labels.includes("notifyCustomerWorkflow"));
    assert.ok(labels.includes("ship"));
  });

  it("writes metadata with packageName and mermaid files", async () => {
    const tmp = fs.mkdtempSync(path.join(os.tmpdir(), "wf-diagrams-ts-"));
    try {
      const result = await writeDiagrams({
        sourceDir: fixtures,
        outDir: tmp,
        packageName: "sample-ts-workflows",
        packageVersion: "0.1.0",
        createZip: true,
      });
      assert.equal(result.metadata.packageName, "sample-ts-workflows");
      assert.equal(result.metadata.language, "TypeScript");
      assert.ok(!("assemblyName" in result.metadata));
      const metaPath = path.join(tmp, "workflow-diagrams-metadata.json");
      assert.ok(fs.existsSync(metaPath));
      const meta = JSON.parse(fs.readFileSync(metaPath, "utf8"));
      assert.equal(meta.packageName, "sample-ts-workflows");
      assert.ok(Array.isArray(meta.workflows));
      assert.ok(meta.workflows.length >= 1);
      for (const w of meta.workflows) {
        assert.ok(fs.existsSync(path.join(tmp, w.diagramFile)));
        const mermaid = fs.readFileSync(path.join(tmp, w.diagramFile), "utf8");
        assert.match(mermaid, /^flowchart TD/m);
        assert.match(mermaid, /Start\(\[Start\]\)/);
        assert.match(mermaid, /End\(\["Complete"\]\)/);
      }
      assert.ok(result.zipPath && fs.existsSync(result.zipPath));
    } finally {
      fs.rmSync(tmp, { recursive: true, force: true });
    }
  });
});
