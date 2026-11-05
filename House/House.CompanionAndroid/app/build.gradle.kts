plugins {
    id("com.android.application")
    id("org.jetbrains.kotlin.android")
    id("org.jetbrains.kotlin.plugin.compose")
}

fun readCompanionToken(): String {
    val env = file("../../../SoulCore/.env")
    if (!env.exists()) return ""
    val line = env.readLines().firstOrNull {
        it.trimStart().startsWith("SOULCORE_COMPANION_API_TOKEN=")
    } ?: return ""
    return line.substringAfter("=").trim()
}

fun asJavaStringLiteral(value: String): String {
    val escaped = value
        .replace("\\", "\\\\")
        .replace("\"", "\\\"")
        .replace("\r", "")
        .replace("\n", "")
    return "\"$escaped\""
}

android {
    namespace = "com.soulcore.companion"
    compileSdk = 34

    defaultConfig {
        applicationId = "com.soulcore.companion"
        minSdk = 26
        targetSdk = 34
        versionCode = 2
        versionName = "0.2.1-link"
        testInstrumentationRunner = "androidx.test.runner.AndroidJUnitRunner"
        // Phone default is this PC's Tailscale serve. Loopback stays available from Settings.
        buildConfigField(
            "String",
            "COMPANION_WS_URL",
            asJavaStringLiteral("wss://kaia-reimagined.tailbf9ec2.ts.net:8443/ws")
        )
        buildConfigField(
            "String",
            "COMPANION_TOKEN",
            asJavaStringLiteral(readCompanionToken())
        )
    }

    buildTypes {
        release {
            isMinifyEnabled = false
            proguardFiles(
                getDefaultProguardFile("proguard-android-optimize.txt"),
                "proguard-rules.pro"
            )
        }
    }

    buildFeatures {
        compose = true
        buildConfig = true
    }

    compileOptions {
        sourceCompatibility = JavaVersion.VERSION_17
        targetCompatibility = JavaVersion.VERSION_17
    }
}

kotlin {
    jvmToolchain(17)
    compilerOptions {
        jvmTarget.set(org.jetbrains.kotlin.gradle.dsl.JvmTarget.JVM_17)
    }
}

dependencies {
    val composeBom = platform("androidx.compose:compose-bom:2024.10.01")
    implementation(composeBom)
    androidTestImplementation(composeBom)

    implementation("androidx.core:core-ktx:1.13.1")
    implementation("androidx.activity:activity-compose:1.9.3")
    implementation("androidx.lifecycle:lifecycle-runtime-ktx:2.8.7")
    implementation("androidx.lifecycle:lifecycle-process:2.8.7")
    implementation("androidx.lifecycle:lifecycle-viewmodel-compose:2.8.7")
    implementation("androidx.compose.ui:ui")
    implementation("androidx.compose.ui:ui-tooling-preview")
    implementation("androidx.compose.material3:material3")
    implementation("androidx.compose.material:material-icons-extended")
    implementation("androidx.navigation:navigation-compose:2.8.4")

    implementation("org.jetbrains.kotlinx:kotlinx-coroutines-android:1.8.1")
    implementation("com.squareup.okhttp3:okhttp:4.12.0")

    // Android Keystore-backed EncryptedSharedPreferences for companion API token (FED-149)
    implementation("androidx.security:security-crypto:1.1.0-alpha06")

    debugImplementation("androidx.compose.ui:ui-tooling")
    debugImplementation("androidx.compose.ui:ui-test-manifest")

    testImplementation("junit:junit:4.13.2")
    // android.jar ships org.json as throwing stubs, so JVM unit tests that parse Host
    // responses need a real implementation on the test classpath.
    testImplementation("org.json:json:20240303")
}
