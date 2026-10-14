/** Shared zip metadata contract (packageName primary — no assemblyName). */
export interface WorkflowDiagramsMetadata {
  packageName: string;
  packageVersion?: string;
  packagePath?: string;
  language: "TypeScript" | "Go" | "Java" | "Python" | "Kotlin" | "C#";
  targetFramework?: string;
  buildDateUtc: string;
  generator: string;
  workflows: WorkflowEntry[];
}

export interface WorkflowEntry {
  name: string;
  displayName?: string;
  diagramFile: string;
}

export type StepKind = "activity" | "wait" | "child" | "parallel";

export interface DiagramStep {
  id: string;
  label: string;
  kind: StepKind;
}

export interface ExtractedWorkflow {
  name: string;
  displayName: string;
  steps: DiagramStep[];
  sourceFile: string;
}

export interface ExtractOptions {
  sourceDir: string;
  outDir: string;
  packageName?: string;
  packageVersion?: string;
  createZip?: boolean;
}
