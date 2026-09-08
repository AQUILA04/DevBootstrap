# Product branding and release metadata (commercial vs technical)
# Technical project / repo folder: DevBootstrap
# Commercial product name:         StackPilot
#
# Keep these values synchronized with Directory.Build.props.

$script:ProductName = "StackPilot"
$script:ProductVersion = "1.2.4"
$script:ProductTagline = "Pack outils de developpement"
$script:TechnicalName = "DevBootstrap"
$script:CompanyName = "OptimizeSolux"
$script:PublisherName = "OptimizeSolux"
$script:Copyright = "Copyright (c) OptimizeSolux"
$script:Description = "Installateur du pack outils de developpement"
$script:PackageProjectUrl = "https://stackpilot.optimizesolux.com"
$script:RepositoryUrl = "https://github.com/AQUILA04/DevBootstrap"
$script:PrivacyPolicyUrl = "https://stackpilot.optimizesolux.com/privacy.html"
$script:SupportUrl = "mailto:contact.stackpilot@optimizesolux.com"
$script:GitHubMsiUrl = "https://github.com/AQUILA04/DevBootstrap/releases/latest/download/StackPilot-$script:ProductVersion-x64.msi"
$script:GitHubZipUrl = "https://github.com/AQUILA04/DevBootstrap/releases/latest/download/StackPilot-$script:ProductVersion-x64.zip"

# PLACEHOLDER: replace with https://apps.microsoft.com/detail/<PRODUCT_ID> after Partner Center assignment.
$script:MicrosoftStoreUrl = "https://apps.microsoft.com/search?query=StackPilot%20OptimizeSolux"
$script:MicrosoftStoreUrlIsPlaceholder = $true

# Stable WiX UpgradeCode — must match Directory.Build.props / Package.wxs
$script:MsiUpgradeCode = "6DA385A7-4697-43B9-AF87-481959D0AD86"
