import fs from "node:fs";
import path from "node:path";
import { spawnSync } from "node:child_process";
import { fileURLToPath } from "node:url";
import { extractWorkflows } from "./extract.js";
import { renderMermaid, sanitize } from "./mermaid.js";
import type { ExtractOptions, WorkflowDiagramsMetadata } from "./types.js";

const GENERATOR = `@temporal-dashboard/workflow-diagramming-typescript/${pkgVersion()}`;

function pkgVersion(): string {
  try {
    const pkgPath = path.join(path.dirname(fileURLToPath(import.meta.url)), "..", "package.json");
    const pkg = JSON.parse(fs.readFileSync(pkgPath, "utf8")) as { version?: string };
    return pkg.version ?? "0.1.0";
  } catch {
    return "0.1.0";
  }
}

export interface WriteResult {
  outDir: string;
  metadata: WorkflowDiagramsMetadata;
  zipPath?: string;
}

/** Extract workflows and write diagrams/ + metadata (+ optional zip). */
export async function writeDiagrams(options: ExtractOptions): Promise<WriteResult> {
  const sourceDir = path.resolve(options.sourceDir);
  const outDir = path.resolve(options.outDir);
  fs.mkdirSync(outDir, { recursive: true });

  const workflows = extractWorkflows(sourceDir);
  const packageName =
    options.packageName?.trim() || path.basename(sourceDir) || "workflows";

  const metadata: WorkflowDiagramsMetadata = {
    packageName,
    packageVersion: options.packageVersion ?? "0.0.0",
    packagePath: sourceDir,
    language: "TypeScript",
    targetFramework: `node${process.versions.node.split(".")[0]}`,
    buildDateUtc: new Date().toISOString(),
    generator: GENERATOR,
    workflows: [],
  };

  const usedNames = new Set<string>();
  for (const wf of workflows) {
    let base = sanitize(wf.name) || "Workflow";
    let fileBase = base;
    let n = 2;
    while (usedNames.has(fileBase.toLowerCase())) {
      fileBase = `${base}${n}`;
      n += 1;
    }
    usedNames.add(fileBase.toLowerCase());
    const diagramFile = `${fileBase}.mermaid`;
    fs.writeFileSync(path.join(outDir, diagramFile), renderMermaid(wf.steps), "utf8");
    metadata.workflows.push({
      name: wf.name,
      displayName: wf.displayName,
      diagramFile,
    });
  }

  fs.writeFileSync(
    path.join(outDir, "workflow-diagrams-metadata.json"),
    JSON.stringify(metadata, null, 2) + "\n",
    "utf8",
  );

  let zipPath: string | undefined;
  if (options.createZip) {
    zipPath = path.join(outDir, "workflow-diagrams.zip");
    writeZip(outDir, zipPath, metadata);
  }

  return { outDir, metadata, zipPath };
}

function writeZip(dir: string, zipPath: string, metadata: WorkflowDiagramsMetadata): void {
  const files = [
    "workflow-diagrams-metadata.json",
    ...metadata.workflows.map((w) => w.diagramFile),
  ];
  const result = spawnSync("zip", ["-q", "-j", zipPath, ...files], {
    cwd: dir,
    encoding: "utf8",
  });
  if (result.status === 0) {
    return;
  }
  writeStoreZip(dir, zipPath, files);
}

/** Uncompressed ZIP (store method) — accepted by unzip / dashboard upload. */
function writeStoreZip(dir: string, zipPath: string, files: string[]): void {
  const localParts: Buffer[] = [];
  const centralParts: Buffer[] = [];
  let offset = 0;

  for (const name of files) {
    const data = fs.readFileSync(path.join(dir, name));
    const nameBuf = Buffer.from(name, "utf8");
    const localHeader = Buffer.alloc(30);
    localHeader.writeUInt32LE(0x04034b50, 0);
    localHeader.writeUInt16LE(20, 4);
    localHeader.writeUInt16LE(0, 6);
    localHeader.writeUInt16LE(0, 8);
    localHeader.writeUInt16LE(0, 10);
    localHeader.writeUInt16LE(0, 12);
    localHeader.writeUInt32LE(crc32(data), 14);
    localHeader.writeUInt32LE(data.length, 18);
    localHeader.writeUInt32LE(data.length, 22);
    localHeader.writeUInt16LE(nameBuf.length, 26);
    localHeader.writeUInt16LE(0, 28);

    const local = Buffer.concat([localHeader, nameBuf, data]);
    localParts.push(local);

    const central = Buffer.alloc(46);
    central.writeUInt32LE(0x02014b50, 0);
    central.writeUInt16LE(20, 4);
    central.writeUInt16LE(20, 6);
    central.writeUInt16LE(0, 8);
    central.writeUInt16LE(0, 10);
    central.writeUInt16LE(0, 12);
    central.writeUInt16LE(0, 14);
    central.writeUInt32LE(crc32(data), 16);
    central.writeUInt32LE(data.length, 20);
    central.writeUInt32LE(data.length, 24);
    central.writeUInt16LE(nameBuf.length, 28);
    central.writeUInt16LE(0, 30);
    central.writeUInt16LE(0, 32);
    central.writeUInt16LE(0, 34);
    central.writeUInt16LE(0, 36);
    central.writeUInt32LE(0, 38);
    central.writeUInt32LE(offset, 42);
    centralParts.push(Buffer.concat([central, nameBuf]));
    offset += local.length;
  }

  const centralDir = Buffer.concat(centralParts);
  const end = Buffer.alloc(22);
  end.writeUInt32LE(0x06054b50, 0);
  end.writeUInt16LE(0, 4);
  end.writeUInt16LE(0, 6);
  end.writeUInt16LE(files.length, 8);
  end.writeUInt16LE(files.length, 10);
  end.writeUInt32LE(centralDir.length, 12);
  end.writeUInt32LE(offset, 16);
  end.writeUInt16LE(0, 20);

  fs.writeFileSync(zipPath, Buffer.concat([...localParts, centralDir, end]));
}

function crc32(buf: Buffer): number {
  let c = 0xffffffff;
  for (let i = 0; i < buf.length; i++) {
    c ^= buf[i]!;
    for (let k = 0; k < 8; k++) {
      c = c & 1 ? (0xedb88320 ^ (c >>> 1)) : c >>> 1;
    }
  }
  return (c ^ 0xffffffff) >>> 0;
}
