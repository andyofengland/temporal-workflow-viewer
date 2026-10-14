import fs from "node:fs";
import path from "node:path";
import ts from "typescript";
import type { DiagramStep, ExtractedWorkflow, StepKind } from "./types.js";

const SOURCE_EXTS = new Set([".ts", ".tsx", ".js", ".mjs"]);

/**
 * Scan a directory for Temporal TypeScript workflow functions and extract linear step lists.
 * Detects proxyActivities usage, executeChild, condition, sleep, Promise.all / workflow.promise.all.
 */
export function extractWorkflows(sourceDir: string): ExtractedWorkflow[] {
  const files = listSourceFiles(sourceDir);
  const results: ExtractedWorkflow[] = [];

  for (const file of files) {
    const text = fs.readFileSync(file, "utf8");
    const sourceFile = ts.createSourceFile(
      file,
      text,
      ts.ScriptTarget.Latest,
      true,
      file.endsWith(".tsx") ? ts.ScriptKind.TSX : ts.ScriptKind.TS,
    );
    results.push(...extractFromSourceFile(sourceFile, file));
  }

  return results;
}

function listSourceFiles(dir: string): string[] {
  const out: string[] = [];
  walk(dir, out);
  return out;
}

function walk(dir: string, out: string[]): void {
  let entries: fs.Dirent[];
  try {
    entries = fs.readdirSync(dir, { withFileTypes: true });
  } catch {
    return;
  }
  for (const entry of entries) {
    if (entry.name === "node_modules" || entry.name === "dist" || entry.name === "out") {
      continue;
    }
    const full = path.join(dir, entry.name);
    if (entry.isDirectory()) {
      walk(full, out);
    } else if (SOURCE_EXTS.has(path.extname(entry.name))) {
      out.push(full);
    }
  }
}

function extractFromSourceFile(sf: ts.SourceFile, filePath: string): ExtractedWorkflow[] {
  const workflows: ExtractedWorkflow[] = [];
  const activityNames = collectProxyActivityNames(sf);

  function visit(node: ts.Node): void {
    if (ts.isFunctionDeclaration(node) && node.name && isExported(node) && node.body) {
      maybeAddWorkflow(node.name.text, node.body, activityNames, filePath, workflows);
    }
    if (ts.isVariableStatement(node) && hasExportModifier(node)) {
      for (const decl of node.declarationList.declarations) {
        if (
          ts.isIdentifier(decl.name) &&
          decl.initializer &&
          (ts.isArrowFunction(decl.initializer) || ts.isFunctionExpression(decl.initializer)) &&
          decl.initializer.body &&
          ts.isBlock(decl.initializer.body)
        ) {
          maybeAddWorkflow(decl.name.text, decl.initializer.body, activityNames, filePath, workflows);
        }
      }
    }
    ts.forEachChild(node, visit);
  }

  visit(sf);
  return workflows;
}

function maybeAddWorkflow(
  name: string,
  body: ts.Block,
  activityNames: Set<string>,
  filePath: string,
  workflows: ExtractedWorkflow[],
): void {
  if (!looksLikeTemporalWorkflow(body, activityNames)) {
    return;
  }
  workflows.push({
    name,
    displayName: name,
    steps: collectSteps(body, activityNames),
    sourceFile: filePath,
  });
}

function looksLikeTemporalWorkflow(body: ts.Block, activityNames: Set<string>): boolean {
  let found = false;
  const visit = (node: ts.Node): void => {
    if (found) return;
    if (ts.isCallExpression(node)) {
      const exprText = node.expression.getText();
      if (
        /executeChild$/.test(exprText) ||
        /condition$/.test(exprText) ||
        /(^|\.)sleep$/.test(exprText) ||
        exprText === "Promise.all" ||
        exprText === "workflow.promise.all" ||
        /proxyActivities$/.test(exprText)
      ) {
        found = true;
        return;
      }
      if (ts.isIdentifier(node.expression) && activityNames.has(node.expression.text)) {
        found = true;
        return;
      }
      if (ts.isPropertyAccessExpression(node.expression)) {
        const obj = node.expression.expression.getText();
        const prop = node.expression.name.text;
        if (activityNames.has(`__ns__${obj}`) || activityNames.has(prop)) {
          found = true;
          return;
        }
      }
    }
    ts.forEachChild(node, visit);
  };
  visit(body);
  return found;
}

function collectProxyActivityNames(sf: ts.SourceFile): Set<string> {
  const names = new Set<string>();
  const visit = (node: ts.Node): void => {
    if (ts.isVariableDeclaration(node) && node.initializer && ts.isCallExpression(node.initializer)) {
      const callee = node.initializer.expression.getText();
      if (callee === "proxyActivities" || callee.endsWith(".proxyActivities")) {
        if (ts.isObjectBindingPattern(node.name)) {
          for (const el of node.name.elements) {
            if (ts.isBindingElement(el) && ts.isIdentifier(el.name)) {
              names.add(el.name.text);
            }
          }
        } else if (ts.isIdentifier(node.name)) {
          names.add(`__ns__${node.name.text}`);
        }
      }
    }
    ts.forEachChild(node, visit);
  };
  visit(sf);
  return names;
}

function collectSteps(body: ts.Block, activityNames: Set<string>): DiagramStep[] {
  const steps: DiagramStep[] = [];
  let counter = 0;

  const add = (label: string, kind: StepKind): void => {
    counter += 1;
    steps.push({ id: `step${counter}_${sanitizeId(label)}`, label, kind });
  };

  const visit = (node: ts.Node): void => {
    if (ts.isCallExpression(node)) {
      const callText = node.expression.getText();

      if (/executeChild$/.test(callText)) {
        const arg = node.arguments[0];
        const childName = arg ? stripQuotes(arg.getText()) : "childWorkflow";
        add(childName, "child");
        return;
      }
      if (/condition$/.test(callText)) {
        add("Wait condition", "wait");
        return;
      }
      if (/(^|\.)sleep$/.test(callText)) {
        const arg = node.arguments[0];
        const dur = arg ? stripQuotes(arg.getText()) : "sleep";
        add(`Sleep ${dur}`, "wait");
        return;
      }
      if (callText === "Promise.all" || callText === "workflow.promise.all") {
        add("Parallel", "parallel");
        for (const arg of node.arguments) {
          visit(arg);
        }
        return;
      }

      if (ts.isIdentifier(node.expression) && activityNames.has(node.expression.text)) {
        add(node.expression.text, "activity");
        return;
      }
      if (ts.isPropertyAccessExpression(node.expression)) {
        const obj = node.expression.expression.getText();
        const prop = node.expression.name.text;
        if (activityNames.has(`__ns__${obj}`) || activityNames.has(prop)) {
          add(prop, "activity");
          return;
        }
      }
    }
    ts.forEachChild(node, visit);
  };

  visit(body);
  return steps;
}

function isExported(node: ts.FunctionDeclaration): boolean {
  return !!(node.modifiers && node.modifiers.some((m) => m.kind === ts.SyntaxKind.ExportKeyword));
}

function hasExportModifier(node: ts.VariableStatement): boolean {
  return !!(node.modifiers && node.modifiers.some((m) => m.kind === ts.SyntaxKind.ExportKeyword));
}

function stripQuotes(s: string): string {
  return s.replace(/^['"`]/, "").replace(/['"`]$/, "");
}

function sanitizeId(label: string): string {
  return label.replace(/[^a-zA-Z0-9]/g, "").slice(0, 40) || "step";
}
