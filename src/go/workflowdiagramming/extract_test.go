package workflowdiagramming_test

import (
	"encoding/json"
	"os"
	"path/filepath"
	"strings"
	"testing"

	"github.com/andyofengland/temporal-workflow-viewer/go/workflowdiagramming"
)

func TestExtractOrderWorkflow(t *testing.T) {
	fixtures := filepath.Join("examples", "sample_workflows")
	wfs, err := workflowdiagramming.ExtractWorkflows(fixtures)
	if err != nil {
		t.Fatal(err)
	}
	if len(wfs) < 1 {
		t.Fatal("expected at least one workflow")
	}
	var order *workflowdiagramming.ExtractedWorkflow
	for i := range wfs {
		if wfs[i].Name == "OrderWorkflow" {
			order = &wfs[i]
			break
		}
	}
	if order == nil {
		t.Fatal("expected OrderWorkflow")
	}
	labels := map[string]bool{}
	for _, s := range order.Steps {
		labels[s.Label] = true
	}
	if !labels["ChargePayment"] {
		t.Fatalf("expected ChargePayment step, got %#v", order.Steps)
	}
	if !labels["Sleep"] || !labels["Await"] {
		t.Fatalf("expected Sleep/Await, got %#v", order.Steps)
	}
}

func TestWriteDiagramsMetadata(t *testing.T) {
	tmp := t.TempDir()
	result, err := workflowdiagramming.WriteDiagrams(workflowdiagramming.WriteOptions{
		SourceDir:      filepath.Join("examples", "sample_workflows"),
		OutDir:         tmp,
		PackageName:    "sample-go-workflows",
		PackageVersion: "0.1.0",
		CreateZip:      true,
	})
	if err != nil {
		t.Fatal(err)
	}
	if result.Metadata.PackageName != "sample-go-workflows" {
		t.Fatalf("packageName: %s", result.Metadata.PackageName)
	}
	if result.Metadata.Language != "Go" {
		t.Fatalf("language: %s", result.Metadata.Language)
	}
	raw, err := os.ReadFile(filepath.Join(tmp, "workflow-diagrams-metadata.json"))
	if err != nil {
		t.Fatal(err)
	}
	if strings.Contains(string(raw), `"assemblyName"`) {
		t.Fatal("must not write assemblyName")
	}
	var meta workflowdiagramming.WorkflowDiagramsMetadata
	if err := json.Unmarshal(raw, &meta); err != nil {
		t.Fatal(err)
	}
	if len(meta.Workflows) < 1 {
		t.Fatal("expected workflows in metadata")
	}
	for _, w := range meta.Workflows {
		data, err := os.ReadFile(filepath.Join(tmp, w.DiagramFile))
		if err != nil {
			t.Fatal(err)
		}
		text := string(data)
		if !strings.HasPrefix(text, "flowchart TD") {
			t.Fatalf("bad mermaid: %s", text)
		}
	}
	if result.ZipPath == "" {
		t.Fatal("expected zip")
	}
	if _, err := os.Stat(result.ZipPath); err != nil {
		t.Fatal(err)
	}
}
