package com.temporaldashboard.workflowdiagramming

import java.nio.file.Path
import kotlin.system.exitProcess

fun main(args: Array<String>) {
    if (args.isEmpty() || args.contains("-h") || args.contains("--help")) {
        usage()
        return
    }

    var source: String? = null
    var out: String? = null
    var packageName: String? = null
    var packageVersion = "0.0.0"
    var zip = false

    var i = 0
    while (i < args.size) {
        when (val a = args[i]) {
            "--out" -> out = args[++i]
            "--package-name" -> packageName = args[++i]
            "--package-version" -> packageVersion = args[++i]
            "--zip" -> zip = true
            else -> {
                if (a.startsWith("-")) {
                    System.err.println("Unknown option: $a")
                    usage()
                    exitProcess(1)
                }
                if (source == null) source = a
                else {
                    System.err.println("Unexpected argument: $a")
                    usage()
                    exitProcess(1)
                }
            }
        }
        i++
    }

    if (source == null) {
        usage()
        exitProcess(1)
    }

    val sourceDir = Path.of(source)
    val outDir = if (out != null) Path.of(out) else sourceDir.resolve("diagrams")

    val result = Writer.write(
        WriteOptions(
            sourceDir = sourceDir,
            outDir = outDir,
            packageName = packageName,
            packageVersion = packageVersion,
            createZip = zip,
        )
    )

    println("Wrote ${result.metadata.workflows.size} workflow diagram(s) to ${result.outDir}")
    result.metadata.workflows.forEach { w ->
        println("  - ${w.name} -> ${w.diagramFile}")
    }
    result.zipPath?.let { println("Zip: $it") }
}

private fun usage() {
    System.err.println("Usage: ./gradlew run --args=\"<source-dir> --out diagrams --package-name name --zip\"")
    System.err.println()
    System.err.println("Options:")
    System.err.println("  --out <dir>             Output directory (default: <source-dir>/diagrams)")
    System.err.println("  --package-name <name>   packageName in metadata")
    System.err.println("  --package-version <v>   packageVersion in metadata")
    System.err.println("  --zip                   Also write workflow-diagrams.zip")
}
