#!/usr/bin/env node
import path from "node:path";
import { writeDiagrams } from "./write.js";

function usage(): never {
  console.error(`Usage: workflow-diagramming-ts <source-dir> [options]

Options:
  --out <dir>             Output directory (default: <source-dir>/diagrams)
  --package-name <name>   packageName in metadata
  --package-version <v>   packageVersion in metadata
  --zip                   Also write workflow-diagrams.zip
  -h, --help              Show help
`);
  process.exit(1);
}

async function main(): Promise<void> {
  const args = process.argv.slice(2);
  if (args.length === 0 || args.includes("-h") || args.includes("--help")) {
    usage();
  }

  let sourceDir: string | undefined;
  let outDir: string | undefined;
  let packageName: string | undefined;
  let packageVersion: string | undefined;
  let createZip = false;

  for (let i = 0; i < args.length; i++) {
    const a = args[i]!;
    if (a === "--out") {
      outDir = args[++i];
    } else if (a === "--package-name") {
      packageName = args[++i];
    } else if (a === "--package-version") {
      packageVersion = args[++i];
    } else if (a === "--zip") {
      createZip = true;
    } else if (a.startsWith("-")) {
      console.error(`Unknown option: ${a}`);
      usage();
    } else if (!sourceDir) {
      sourceDir = a;
    } else {
      console.error(`Unexpected argument: ${a}`);
      usage();
    }
  }

  if (!sourceDir) {
    usage();
  }

  const resolvedOut = outDir ?? path.join(sourceDir, "diagrams");
  const result = await writeDiagrams({
    sourceDir,
    outDir: resolvedOut,
    packageName,
    packageVersion,
    createZip,
  });

  console.log(
    `Wrote ${result.metadata.workflows.length} workflow diagram(s) to ${result.outDir}`,
  );
  for (const w of result.metadata.workflows) {
    console.log(`  - ${w.name} -> ${w.diagramFile}`);
  }
  if (result.zipPath) {
    console.log(`Zip: ${result.zipPath}`);
  }
}

main().catch((err) => {
  console.error(err);
  process.exit(1);
});
