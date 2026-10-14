package main

import (
	"flag"
	"fmt"
	"os"
	"path/filepath"

	"github.com/andyofengland/temporal-workflow-viewer/go/workflowdiagramming"
)

func main() {
	dir := flag.String("dir", "", "Source directory to scan (required)")
	out := flag.String("out", "", "Output directory (default: <dir>/diagrams)")
	packageName := flag.String("package-name", "", "packageName in metadata")
	packageVersion := flag.String("package-version", "", "packageVersion in metadata")
	createZip := flag.Bool("zip", false, "Also write workflow-diagrams.zip")
	flag.Parse()

	if *dir == "" {
		fmt.Fprintln(os.Stderr, "Usage: go run ./cmd/extract --dir <source-dir> [--out <dir>] [--package-name name] [--zip]")
		os.Exit(1)
	}

	outDir := *out
	if outDir == "" {
		outDir = filepath.Join(*dir, "diagrams")
	}

	result, err := workflowdiagramming.WriteDiagrams(workflowdiagramming.WriteOptions{
		SourceDir:      *dir,
		OutDir:         outDir,
		PackageName:    *packageName,
		PackageVersion: *packageVersion,
		CreateZip:      *createZip,
	})
	if err != nil {
		fmt.Fprintln(os.Stderr, err)
		os.Exit(1)
	}

	fmt.Printf("Wrote %d workflow diagram(s) to %s\n", len(result.Metadata.Workflows), result.OutDir)
	for _, w := range result.Metadata.Workflows {
		fmt.Printf("  - %s -> %s\n", w.Name, w.DiagramFile)
	}
	if result.ZipPath != "" {
		fmt.Printf("Zip: %s\n", result.ZipPath)
	}
}
