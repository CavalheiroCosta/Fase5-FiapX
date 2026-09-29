param(
    [Parameter(Mandatory = $true)]
    [string]$CoverageDir
)

$ErrorActionPreference = "Stop"
$threshold = 80

function Test-ProcessorModule([string]$Name) {
    if ([string]::IsNullOrWhiteSpace($Name)) {
        return $false
    }

    $module = $Name
    if ($module.EndsWith(".dll")) {
        $module = $module.Substring(0, $module.Length - 4)
    }

    if ($module.Contains("Tests")) {
        return $false
    }

    return ($module -eq "Processor.Api") -or $module.StartsWith("Processor.")
}

function Get-AttributeInt($Node, [string]$Attribute) {
    if ($null -eq $Node) {
        return 0
    }

    $value = $Node.GetAttribute($Attribute)
    if ([string]::IsNullOrWhiteSpace($value)) {
        return 0
    }

    return [int]$value
}

if (-not (Test-Path -Path $CoverageDir)) {
    Write-Host "::error::Relatório OpenCover não encontrado em $CoverageDir"
    exit 1
}

$reports = @(Get-ChildItem -Path $CoverageDir -Recurse -Filter "coverage.opencover.xml" -File -ErrorAction SilentlyContinue)
if ($reports.Count -eq 0) {
    Write-Host "::error::Relatório OpenCover não encontrado em $CoverageDir"
    exit 1
}

$visited = 0
$total = 0
foreach ($report in $reports) {
    $doc = [xml](Get-Content -Path $report.FullName -Raw)
    foreach ($module in @($doc.SelectNodes("//Module"))) {
        $nameNode = $module.SelectSingleNode("ModuleName")
        $name = if ($null -eq $nameNode) { "" } else { $nameNode.InnerText }
        if (-not (Test-ProcessorModule $name)) {
            continue
        }

        $summaries = @($module.SelectNodes("./Classes/Class/Summary"))
        if ($summaries.Count -eq 0) {
            $moduleSummary = $module.SelectSingleNode("./Summary")
            if ($null -ne $moduleSummary) {
                $summaries = @($moduleSummary)
            }
        }

        foreach ($summary in $summaries) {
            $total += Get-AttributeInt $summary "numSequencePoints"
            $visited += Get-AttributeInt $summary "visitedSequencePoints"
        }
    }
}

if ($total -eq 0) {
    Write-Host "::error::O processor não tem linhas mensuráveis no relatório OpenCover"
    exit 1
}

$rate = [math]::Round(($visited * 100.0) / $total, 2)
Write-Host "Cobertura de linhas do processor: $rate% ($visited/$total)"
if (($visited * 100) -lt ($total * $threshold)) {
    Write-Host "::error::Cobertura de linhas do processor $rate% está abaixo de $threshold%"
    exit 1
}

exit 0
