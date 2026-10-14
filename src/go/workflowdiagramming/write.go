package workflowdiagramming

import (
	"archive/zip"
	"encoding/json"
	"fmt"
	"os"
	"path/filepath"
	"runtime"
	"strings"
	"time"
)

const generatorName = "github.com/andyofengland/temporal-workflow-viewer/go/workflowdiagramming"
const generatorVersion = "0.1.0"

// WriteOptions configures diagram output.
type WriteOptions struct {
	SourceDir      string
	OutDir         string
	PackageName    string
	PackageVersion string
	CreateZip      bool
}

// WriteResult is the written package summary.
type WriteResult struct {
	OutDir   string
	Metadata WorkflowDiagramsMetadata
	ZipPath  string
}

// WriteDiagrams extracts workflows and writes metadata + mermaid (+ optional zip).
func WriteDiagrams(opts WriteOptions) (*WriteResult, error) {
	sourceDir, err := filepath.Abs(opts.SourceDir)
	if err != nil {
		return nil, err
	}
	outDir, err := filepath.Abs(opts.OutDir)
	if err != nil {
		return nil, err
	}
	if err := os.MkdirAll(outDir, 0o755); err != nil {
		return nil, err
	}

	workflows, err := ExtractWorkflows(sourceDir)
	if err != nil {
		return nil, err
	}

	pkgName := strings.TrimSpace(opts.PackageName)
	if pkgName == "" {
		pkgName = filepath.Base(sourceDir)
	}
	pkgVer := opts.PackageVersion
	if pkgVer == "" {
		pkgVer = "0.0.0"
	}

	meta := WorkflowDiagramsMetadata{
		PackageName:     pkgName,
		PackageVersion:  pkgVer,
		PackagePath:     sourceDir,
		Language:        "Go",
		TargetFramework: runtime.Version(),
		BuildDateUtc:    time.Now().UTC().Format(time.RFC3339Nano),
		Generator:       generatorName + "/" + generatorVersion,
		Workflows:       nil,
	}

	used := map[string]bool{}
	for _, wf := range workflows {
		base := sanitizeNode(wf.Name)
		if base == "" {
			base = "Workflow"
		}
		fileBase := base
		for n := 2; used[strings.ToLower(fileBase)]; n++ {
			fileBase = fmt.Sprintf("%s%d", base, n)
		}
		used[strings.ToLower(fileBase)] = true
		diagramFile := fileBase + ".mermaid"
		if err := os.WriteFile(filepath.Join(outDir, diagramFile), []byte(RenderMermaid(wf.Steps)), 0o644); err != nil {
			return nil, err
		}
		meta.Workflows = append(meta.Workflows, WorkflowEntry{
			Name:        wf.Name,
			DisplayName: wf.DisplayName,
			DiagramFile: diagramFile,
		})
	}

	metaBytes, err := json.MarshalIndent(meta, "", "  ")
	if err != nil {
		return nil, err
	}
	metaBytes = append(metaBytes, '\n')
	if err := os.WriteFile(filepath.Join(outDir, "workflow-diagrams-metadata.json"), metaBytes, 0o644); err != nil {
		return nil, err
	}

	result := &WriteResult{OutDir: outDir, Metadata: meta}
	if opts.CreateZip {
		zipPath := filepath.Join(outDir, "workflow-diagrams.zip")
		if err := writeZip(outDir, zipPath, meta); err != nil {
			return nil, err
		}
		result.ZipPath = zipPath
	}
	return result, nil
}

func writeZip(dir, zipPath string, meta WorkflowDiagramsMetadata) error {
	f, err := os.Create(zipPath)
	if err != nil {
		return err
	}
	defer f.Close()
	zw := zip.NewWriter(f)
	defer zw.Close()

	files := []string{"workflow-diagrams-metadata.json"}
	for _, w := range meta.Workflows {
		files = append(files, w.DiagramFile)
	}
	for _, name := range files {
		data, err := os.ReadFile(filepath.Join(dir, name))
		if err != nil {
			return err
		}
		w, err := zw.Create(name)
		if err != nil {
			return err
		}
		if _, err := w.Write(data); err != nil {
			return err
		}
	}
	return nil
}
