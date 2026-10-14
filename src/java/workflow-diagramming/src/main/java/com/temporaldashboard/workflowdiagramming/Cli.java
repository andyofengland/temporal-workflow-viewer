package com.temporaldashboard.workflowdiagramming;

import java.nio.file.Path;

public final class Cli {
    public static void main(String[] args) throws Exception {
        if (args.length == 0 || hasFlag(args, "-h") || hasFlag(args, "--help")) {
            usage();
            return;
        }

        String source = null;
        String out = null;
        String packageName = null;
        String packageVersion = "0.0.0";
        boolean zip = false;

        for (int i = 0; i < args.length; i++) {
            String a = args[i];
            if ("--out".equals(a)) {
                out = args[++i];
            } else if ("--package-name".equals(a)) {
                packageName = args[++i];
            } else if ("--package-version".equals(a)) {
                packageVersion = args[++i];
            } else if ("--zip".equals(a)) {
                zip = true;
            } else if (a.startsWith("-")) {
                System.err.println("Unknown option: " + a);
                usage();
                System.exit(1);
            } else if (source == null) {
                source = a;
            } else {
                System.err.println("Unexpected argument: " + a);
                usage();
                System.exit(1);
            }
        }

        if (source == null) {
            usage();
            System.exit(1);
        }

        Path sourceDir = Path.of(source);
        Path outDir = out != null ? Path.of(out) : sourceDir.resolve("diagrams");

        Writer.Options opts = new Writer.Options();
        opts.sourceDir = sourceDir;
        opts.outDir = outDir;
        opts.packageName = packageName;
        opts.packageVersion = packageVersion;
        opts.createZip = zip;

        Writer.Result result = Writer.write(opts);
        System.out.printf("Wrote %d workflow diagram(s) to %s%n",
                result.metadata.workflows.size(), result.outDir);
        for (Metadata.WorkflowEntry w : result.metadata.workflows) {
            System.out.printf("  - %s -> %s%n", w.name, w.diagramFile);
        }
        if (result.zipPath != null) {
            System.out.println("Zip: " + result.zipPath);
        }
    }

    private static boolean hasFlag(String[] args, String flag) {
        for (String a : args) {
            if (flag.equals(a)) {
                return true;
            }
        }
        return false;
    }

    private static void usage() {
        System.err.println("Usage: java -jar workflow-diagramming-java.jar <source-dir> [options]");
        System.err.println("   or: mvn -q exec:java -Dexec.args=\"<source-dir> --out diagrams --package-name name --zip\"");
        System.err.println();
        System.err.println("Options:");
        System.err.println("  --out <dir>             Output directory (default: <source-dir>/diagrams)");
        System.err.println("  --package-name <name>   packageName in metadata");
        System.err.println("  --package-version <v>   packageVersion in metadata");
        System.err.println("  --zip                   Also write workflow-diagrams.zip");
    }
}
