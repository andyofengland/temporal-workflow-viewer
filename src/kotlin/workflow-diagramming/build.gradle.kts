plugins {
    kotlin("jvm") version "1.9.25"
    application
}

group = "com.temporaldashboard"
version = "0.1.0"

repositories {
    mavenCentral()
}

dependencies {
    testImplementation(kotlin("test"))
}

application {
    mainClass.set("com.temporaldashboard.workflowdiagramming.CliKt")
}

tasks.test {
    useJUnitPlatform()
}

kotlin {
    jvmToolchain(11)
}
